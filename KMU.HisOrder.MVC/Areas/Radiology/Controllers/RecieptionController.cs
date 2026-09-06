using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Areas.Radiology.Models;
using KMU.HisOrder.MVC.Areas.Radiology.Services;
using System.Transactions;

namespace KMU.HisOrder.MVC.Areas.Radiology.Controllers
{
    [Area("Radiology")]
    public class RecieptionController : Controller
    {
        private readonly KMUContext _context;
        private readonly OrthancWorklistService _worklistService;

        public RecieptionController(KMUContext context, OrthancWorklistService worklistService)
        {
            _context = context;
            _worklistService = worklistService;
        }

        public async Task<IActionResult> Index()
        {
            // Get today's date for filtering
            var today = DateOnly.FromDateTime(DateTime.Now);
            
            // Fetch radiology exam requests with related data (exclude cancelled)
            var requests = await _context.radiologyexamrequests
                .Where(r => r.ordereddate == today && r.status != "CANCELLED")
                .OrderByDescending(r => r.createdat)
                .Select(r => new
                {
                    r.id,
                    r.orderid,
                    r.patientid,
                    r.inhospid,
                    r.roomid,
                    r.item_id,
                    r.seq_no,
                    r.status,
                    r.ordereddate,
                    r.createdat
                })
                .ToListAsync();

            // Get unique patient IDs
            var patientIds = requests.Select(r => r.patientid).Distinct().ToList();
            var patients = await _context.KmuCharts
                .Where(p => patientIds.Contains(p.ChrHealthId))
                .ToDictionaryAsync(p => p.ChrHealthId);

            // Get unique room IDs (filter out nulls)
            var roomIds = requests.Where(r => r.roomid.HasValue).Select(r => r.roomid.Value).Distinct().ToList();
            var rooms = await _context.rooms
                .Where(r => roomIds.Contains(r.roomid))
                .ToDictionaryAsync(r => r.roomid);

            // Get all item IDs with their GroupCode to determine modality
            var allItemIds = requests
                .Where(r => !string.IsNullOrEmpty(r.item_id))
                .Select(r => r.item_id)
                .ToList();
            var bodyPartsWithModality = await _context.KmuNonMedicines
                .Where(nm => allItemIds.Contains(nm.ItemId))
                .Select(nm => new { nm.ItemId, nm.ItemName, nm.GroupCode })
                .ToListAsync();
            
            var bodyParts = bodyPartsWithModality.ToDictionary(bp => bp.ItemId, bp => bp.ItemName);
            var bodyPartModality = bodyPartsWithModality.ToDictionary(
                bp => bp.ItemId, 
                bp => bp.GroupCode == "C2" ? "XRAY" : bp.GroupCode == "C1" ? "CT" : "OTHER"
            );

            // Build view models
            // For PENDING: Group by inhospid + modality (so CT and XRAY exams appear as separate rows)
            // For others: Group by orderid
            var viewModelList = new List<dynamic>();

            foreach (var request in requests)
            {
                string groupKey;
                string displayModality;
                
                if (request.status == "PENDING")
                {
                    // Determine modality from body part
                    displayModality = bodyPartModality.ContainsKey(request.item_id) 
                        ? bodyPartModality[request.item_id] 
                        : "N/A";
                    groupKey = $"PENDING_{request.inhospid}_{displayModality}";
                }
                else
                {
                    groupKey = request.orderid;
                    displayModality = request.roomid.HasValue && rooms.ContainsKey(request.roomid.Value) 
                        ? rooms[request.roomid.Value].modality 
                        : "N/A";
                }
                
                // Store modality info with each request for grouping
                var enrichedRequest = new 
                { 
                    Request = request, 
                    GroupKey = groupKey,
                    DisplayModality = displayModality
                };
                
                if (!viewModelList.Any(vm => vm.GroupKey == groupKey))
                {
                    viewModelList.Add(enrichedRequest);
                }
            }

            // Now build the final view model by grouping
            var viewModel = requests
                .Select(r => new 
                { 
                    Request = r,
                    GroupKey = r.status == "PENDING" 
                        ? $"PENDING_{r.inhospid}_{(bodyPartModality.ContainsKey(r.item_id) ? bodyPartModality[r.item_id] : "N/A")}"
                        : r.orderid,
                    DisplayModality = r.status == "PENDING"
                        ? (bodyPartModality.ContainsKey(r.item_id) ? bodyPartModality[r.item_id] : "N/A")
                        : (r.roomid.HasValue && rooms.ContainsKey(r.roomid.Value) ? rooms[r.roomid.Value].modality : "N/A")
                })
                .GroupBy(x => x.GroupKey)
                .Select(g =>
                {
                    var firstRequest = g.First().Request;
                    var displayModality = g.First().DisplayModality;
                    var patient = patients.ContainsKey(firstRequest.patientid) ? patients[firstRequest.patientid] : null;
                    var room = firstRequest.roomid.HasValue && rooms.ContainsKey(firstRequest.roomid.Value) ? rooms[firstRequest.roomid.Value] : null;
                    
                    // Get all body parts for this group
                    var orderBodyParts = g.Where(r => !string.IsNullOrEmpty(r.Request.item_id))
                        .Select(r => bodyParts.ContainsKey(r.Request.item_id) ? bodyParts[r.Request.item_id] : r.Request.item_id)
                        .ToList();

                    var age = patient?.ChrBirthDate.HasValue == true
                        ? DateTime.Now.Year - patient.ChrBirthDate.Value.Year
                        : (int?)null;

                    return new
                    {
                        OrderId = firstRequest.orderid,
                        InhospId = firstRequest.inhospid,
                        PatientId = firstRequest.patientid,
                        PatientName = patient != null
                            ? $"{patient.ChrPatientFirstname} {patient.ChrPatientMidname} {patient.ChrPatientLastname}".Trim()
                            : "Unknown",
                        Phone = patient?.ChrMobilePhone ?? "N/A",
                        Modality = displayModality,
                        RoomNumber = room?.room_number ?? "N/A",
                        BodyParts = string.Join(", ", orderBodyParts),
                        Age = age,
                        Sex = patient?.ChrSex ?? "N/A",
                        OrderDate = firstRequest.ordereddate,
                        CreatedAt = firstRequest.createdat,
                        Status = firstRequest.status ?? "PENDING",
                        TicketNumber = firstRequest.seq_no,
                        ExamCount = g.Count(),
                        GroupKey = g.Key
                    };
                }).ToList();

            // Calculate stats
            ViewBag.NewOrders = viewModel.Count(v => v.Status == "PENDING");
            ViewBag.Scheduled = viewModel.Count(v => v.Status == "SCHEDULED");
            ViewBag.Completed = viewModel.Count(v => v.Status == "COMPLETED");
            ViewBag.Finalized = viewModel.Count(v => v.Status == "FINALIZED");

            // Get pending order count for badge (group by inhospid for pending)
            var pendingOrderCount = await _context.radiologyexamrequests
                .Where(r => r.status == "PENDING" && r.ordereddate == today)
                .Select(r => r.inhospid)
                .Distinct()
                .CountAsync();
            ViewBag.PendingCount = pendingOrderCount;

            return View(viewModel);
        }

        public IActionResult NewWalkInRequest()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> SearchPatient(string patientId)
        {
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return Json(new { success = false, message = "Patient ID is required" });
            }

            var patient = await _context.KmuCharts
                .Where(p => p.ChrHealthId == patientId)
                .Select(p => new PatientSearchResult
                {
                    ChrHealthId = p.ChrHealthId,
                    FullName = $"{p.ChrPatientFirstname} {p.ChrPatientMidname} {p.ChrPatientLastname}".Trim(),
                    ChrSex = p.ChrSex,
                    ChrBirthDate = p.ChrBirthDate,
                    Age = p.ChrBirthDate.HasValue
                        ? DateTime.Now.Year - p.ChrBirthDate.Value.Year
                        : (int?)null,
                    ChrMobilePhone = p.ChrMobilePhone,
                    ChrAddress = p.ChrAddress
                })
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return Json(new { success = false, message = "Patient not found" });
            }

            return Json(new { success = true, data = patient });
        }

        [HttpGet]
        public async Task<IActionResult> SearchPatients(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Json(new { success = false, message = "Search term is required" });
            }

            searchTerm = searchTerm.ToLower();

            var patients = await _context.KmuCharts
                .Where(p =>
                    p.ChrHealthId.ToLower().Contains(searchTerm) ||
                    p.ChrPatientFirstname.ToLower().Contains(searchTerm) ||
                    p.ChrPatientMidname.ToLower().Contains(searchTerm) ||
                    p.ChrPatientLastname.ToLower().Contains(searchTerm) ||
                    (p.ChrMobilePhone != null && p.ChrMobilePhone.Contains(searchTerm))
                )
                .Take(20)
                .Select(p => new PatientSearchResult
                {
                    ChrHealthId = p.ChrHealthId,
                    FullName = $"{p.ChrPatientFirstname} {p.ChrPatientMidname} {p.ChrPatientLastname}".Trim(),
                    ChrSex = p.ChrSex,
                    //ChrBirthDate = p.ChrBirthDate,
                    Age = p.ChrBirthDate.HasValue
                        ? DateTime.Now.Year - p.ChrBirthDate.Value.Year
                        : (int?)null,
                    ChrMobilePhone = p.ChrMobilePhone,
                    ChrAddress = p.ChrAddress
                })
                .ToListAsync();

            return Json(new { success = true, data = patients });
        }

        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            var departments = await _context.KmuDepartments
                .Where(d => d.DptStatus == "Y" && d.DptCategory == "RD")
                .OrderBy(d => d.DptName)
                .Select(d => new
                {
                    code = d.DptCode,
                    name = d.DptName
                })
                .ToListAsync();

            return Json(departments);
        }

        [HttpGet]
        public async Task<IActionResult> GetRooms(string dptCode = null)
        {
            var query = _context.rooms
                .Where(r => r.IsActive);

            if (!string.IsNullOrEmpty(dptCode))
            {
                query = query.Where(r => r.department == dptCode);
            }

            var rooms = await query
                .OrderBy(r => r.modality)
                .ThenBy(r => r.room_number)
                .Select(r => new RoomListItem
                {
                    RoomId = r.roomid,
                    RoomNumber = r.room_number,
                    Department = r.department,
                    Modality = r.modality,
                    Description = r.description
                })
                .ToListAsync();

            return Json(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> GetBodyParts(string dptCode = null)
        {
            var query = _context.KmuNonMedicines
                .Where(nm => nm.ItemType == "6" && nm.Status == '1');

            if (!string.IsNullOrEmpty(dptCode))
            {
                if (dptCode == "1800")
                {
                    query = query.Where(nm => nm.GroupCode == "C2");
                }
                else
                {
                    query = query.Where(nm => nm.GroupCode == "C1");
                }
            }

            var bodyParts = await query
                .OrderBy(nm => nm.GroupCode)
                .ThenBy(nm => nm.ShowSeq)
                .ThenBy(nm => nm.ItemName)
                .Select(nm => new BodyPartListItem
                {
                    ItemId = nm.ItemId,
                    ItemName = nm.ItemName,
                    GroupCode = nm.GroupCode ?? "OTHER"
                })
                .ToListAsync();

            return Json(bodyParts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateWalkInRequest([FromBody] WalkInRequestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request data" });
            }

            try
            {
                var patient = await _context.KmuCharts
                    .FirstOrDefaultAsync(p => p.ChrHealthId == model.PatientId);

                if (patient == null)
                {
                    return Json(new { success = false, message = "Patient not found" });
                }

                var room = await _context.rooms
                    .FirstOrDefaultAsync(r => r.roomid == model.RoomId);

                if (room == null)
                {
                    return Json(new { success = false, message = "Room not found" });
                }

                var orderedDate = DateOnly.FromDateTime(DateTime.Now);

                // Check for duplicate exam requests (same patient, room, body part, and date)
                var duplicateExams = new List<string>();
                foreach (var bodyPartId in model.BodyPartIds)
                {
                    var existingRequest = await _context.radiologyexamrequests
                        .FirstOrDefaultAsync(r => 
                            r.patientid == model.PatientId &&
                            r.roomid == model.RoomId &&
                            r.item_id == bodyPartId &&
                            r.ordereddate == orderedDate);

                    if (existingRequest != null)
                    {
                        var bodyPartName = await _context.KmuNonMedicines
                            .Where(nm => nm.ItemId == bodyPartId)
                            .Select(nm => nm.ItemName)
                            .FirstOrDefaultAsync();
                        duplicateExams.Add(bodyPartName ?? bodyPartId);
                    }
                }

                if (duplicateExams.Any())
                {
                    var duplicateList = string.Join(", ", duplicateExams);
                    return Json(new 
                    { 
                        success = false, 
                        message = $"Duplicate exam request detected. The following exam(s) already exist for this patient in this room today: {duplicateList}" 
                    });
                }

                // Generate sequence number once for all exams in this order
                int seqNo = await GenerateSequenceNumber(room.modality, orderedDate);

                // Generate sequential order ID in format RAD-{number}
                string orderId = await GenerateOrderId();

                // Get body part names for display
                var bodyPartNames = await _context.KmuNonMedicines
                    .Where(nm => model.BodyPartIds.Contains(nm.ItemId))
                    .Select(nm => nm.ItemName)
                    .ToListAsync();
                string bodyPartNamesStr = string.Join(", ", bodyPartNames);

                // Create one row per body part (exam)
                var createdRequests = new List<radiologyexamrequest>();
                
                foreach (var bodyPartId in model.BodyPartIds)
                {
                    var request = new radiologyexamrequest
                    {
                        orderid = orderId,
                        orderplanid = null,
                        inhospid = null,
                        patientid = model.PatientId,
                        roomid = model.RoomId,
                        item_id = bodyPartId,
                        seq_no = seqNo,
                        sourcetype = "WALKIN",
                        requestedby = model.RequestedBy ?? User.Identity?.Name ?? "system",
                        remark = model.Remark,
                        status = "SCHEDULED",
                        ordereddate = orderedDate,
                        createdat = DateTime.Now
                    };

                    _context.radiologyexamrequests.Add(request);
                    createdRequests.Add(request);
                }

                await _context.SaveChangesAsync();

                // Generate DICOM identifiers for each request (now that we have IDs from database)
                foreach (var request in createdRequests)
                {
                    request.accessionnumber = $"{request.orderid}-{request.id}";
                    request.studyinstanceuid = GenerateStudyInstanceUID(request.id);
                }

                // Save the updated identifiers
                await _context.SaveChangesAsync();

                // Generate and upload worklists to Orthanc for each exam
                foreach (var request in createdRequests)
                {
                    try
                    {
                        await _worklistService.GenerateAndUploadWorklistAsync(request);
                    }
                    catch (Exception worklistEx)
                    {
                        // Log but don't fail the entire request if worklist upload fails
                        Console.WriteLine($"Warning: Failed to generate/upload worklist for request ID {request.id}: {worklistEx.Message}");
                    }
                }

                // Calculate patient age
                int? age = null;
                if (patient.ChrBirthDate.HasValue)
                {
                    var today = DateOnly.FromDateTime(DateTime.Now);
                    age = today.Year - patient.ChrBirthDate.Value.Year;
                    if (patient.ChrBirthDate.Value > today.AddYears(-age.Value))
                    {
                        age--;
                    }
                }

                var ticket = new
                {
                    orderId = orderId,
                    requestIds = createdRequests.Select(r => r.id).ToList(),
                    patientId = patient.ChrHealthId,
                    patientName = $"{patient.ChrPatientFirstname} {patient.ChrPatientMidname} {patient.ChrPatientLastname}".Trim(),
                    patientSex = patient.ChrSex ?? "N/A",
                    patientAge = age?.ToString() ?? "N/A",
                    modality = room.modality,
                    roomNumber = room.room_number,
                    ticketNumber = seqNo,
                    orderedDate = orderedDate.ToString("yyyy-MM-dd"),
                    printDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    bodyPart = bodyPartNamesStr,
                    examCount = createdRequests.Count
                };

                return Json(new { success = true, data = ticket });
            }
            catch (Exception ex)
            {
                // Log the full exception for debugging
                Console.WriteLine($"Error in CreateWalkInRequest: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error creating request: {ex.Message}" });
            }
        }

        private async Task<int> GenerateSequenceNumber(string modality, DateOnly orderedDate)
        {
            // Get all requests for this modality on this date by joining with rooms
            var maxSeqNo = await _context.radiologyexamrequests
                .Where(r => r.roomid.HasValue)
                .Join(_context.rooms,
                    request => request.roomid.Value,
                    room => room.roomid,
                    (request, room) => new { request, room })
                .Where(x => x.room.modality == modality && x.request.ordereddate == orderedDate)
                .MaxAsync(x => (int?)x.request.seq_no);

            return (maxSeqNo ?? 0) + 1;
        }

        private async Task<string> GenerateOrderId()
        {
            // Get all existing order IDs that start with "RAD-"
            var existingOrderIds = await _context.radiologyexamrequests
                .Where(r => r.orderid != null && r.orderid.StartsWith("RAD-"))
                .Select(r => r.orderid)
                .Distinct()
                .ToListAsync();

            int maxNumber = 0;
            
            // Parse the numbers from existing order IDs
            foreach (var orderId in existingOrderIds)
            {
                var parts = orderId.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                {
                    if (number > maxNumber)
                    {
                        maxNumber = number;
                    }
                }
            }

            // Return next sequential number
            return $"RAD-{maxNumber + 1}";
        }

        /// <summary>
        /// Generate a unique Study Instance UID for DICOM
        /// Format: {root}.{timestamp}.{requestId}
        /// </summary>
        private string GenerateStudyInstanceUID(int requestId)
        {
            // Use your institution's OID root (this is an example)
            // You should register your own OID at: https://www.iana.org/assignments/enterprise-numbers
            string root = "1.2.826.0.1.3680043.8.498";
            
            // Generate unique timestamp (includes milliseconds)
            string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            
            // Combine root + timestamp + request ID for guaranteed uniqueness
            return $"{root}.{timestamp}.{requestId}";
        }

        [HttpGet]
        public IActionResult PrintTicket(int id)
        {
            var request = _context.radiologyexamrequests
                .Where(r => r.id == id)
                .FirstOrDefault();

            if (request == null)
            {
                return NotFound();
            }

            // Get all exams with the same orderid
            var allExamsInOrder = _context.radiologyexamrequests
                .Where(r => r.orderid == request.orderid)
                .ToList();

            var patient = _context.KmuCharts
                .FirstOrDefault(p => p.ChrHealthId == request.patientid);

            var room = request.roomid.HasValue 
                ? _context.rooms.FirstOrDefault(r => r.roomid == request.roomid.Value)
                : null;

            // Get all body part names from all exams in this order
            var allItemIds = allExamsInOrder
                .Where(r => !string.IsNullOrEmpty(r.item_id))
                .Select(r => r.item_id)
                .ToList();

            var bodyParts = _context.KmuNonMedicines
                .Where(nm => allItemIds.Contains(nm.ItemId))
                .Select(nm => nm.ItemName)
                .ToList();
            
            string bodyPartNames = bodyParts.Any() ? string.Join(", ", bodyParts) : "N/A";

            // Calculate patient age
            string age = "N/A";
            if (patient?.ChrBirthDate.HasValue == true)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                var calculatedAge = today.Year - patient.ChrBirthDate.Value.Year;
                if (patient.ChrBirthDate.Value > today.AddYears(-calculatedAge))
                {
                    calculatedAge--;
                }
                age = calculatedAge.ToString();
            }

            var ticket = new TicketViewModel
            {
                RequestId = request.id,
                OrderId = request.orderid ?? "N/A",
                PatientId = request.patientid,
                PatientName = patient != null
                    ? $"{patient.ChrPatientFirstname} {patient.ChrPatientMidname} {patient.ChrPatientLastname}".Trim()
                    : "Unknown",
                PatientSex = patient?.ChrSex ?? "N/A",
                PatientAge = age,
                Modality = room?.modality ?? "N/A",
                RoomNumber = room?.room_number ?? "N/A",
                TicketNumber = request.seq_no ?? 0,
                OrderedDate = request.ordereddate ?? DateOnly.FromDateTime(DateTime.Now),
                PrintDate = DateTime.Now.ToString("yyyy-MM-dd:HH"),
                BodyPart = bodyPartNames
            };

            return View(ticket);
        }

        /// <summary>
        /// Print ticket by Order ID (for scheduled exams from index page)
        /// </summary>
        [HttpGet]
        public IActionResult PrintTicketByOrderId(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                return NotFound("Order ID is required");
            }

            // Get first exam request from this order to get the ID
            var firstRequest = _context.radiologyexamrequests
                .Where(r => r.orderid == orderId)
                .OrderBy(r => r.id)
                .FirstOrDefault();

            if (firstRequest == null)
            {
                return NotFound("Order not found");
            }

            // Redirect to existing PrintTicket action
            return RedirectToAction("PrintTicket", new { id = firstRequest.id });
        }

        /// <summary>
        /// Show scheduling form for a pending order - grouped by inhospid and modality
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SchedulePendingExam(string inhospId, string modality)
        {
            // Get all pending exams for this inpatient
            var allExamRequests = await _context.radiologyexamrequests
                .Where(r => r.inhospid == inhospId && r.status == "PENDING")
                .ToListAsync();

            if (!allExamRequests.Any())
            {
                return NotFound("No pending exams found for this inpatient.");
            }

            // Get body part info with GroupCode to determine modality
            var itemIds = allExamRequests.Select(r => r.item_id).ToList();
            var bodyPartsWithModality = await _context.KmuNonMedicines
                .Where(nm => itemIds.Contains(nm.ItemId))
                .Select(nm => new { nm.ItemId, nm.ItemName, nm.GroupCode })
                .ToListAsync();
            
            var bodyParts = bodyPartsWithModality.ToDictionary(bp => bp.ItemId, bp => bp.ItemName);
            var bodyPartModality = bodyPartsWithModality.ToDictionary(
                bp => bp.ItemId,
                bp => bp.GroupCode == "C2" ? "XRAY" : bp.GroupCode == "C1" ? "CT" : "OTHER"
            );

            // Filter exams by the specified modality
            var examRequests = allExamRequests
                .Where(r => bodyPartModality.ContainsKey(r.item_id) && bodyPartModality[r.item_id] == modality)
                .ToList();

            if (!examRequests.Any())
            {
                return NotFound($"No pending exams found for modality {modality}.");
            }

            var firstRequest = examRequests.First();
            
            // Get patient info
            var patient = await _context.KmuCharts
                .FirstOrDefaultAsync(p => p.ChrHealthId == firstRequest.patientid);

            // Normalize modality token and find matching rooms using case-insensitive substring checks
            var normalizedModality = (modality ?? string.Empty).ToUpperInvariant();

            var availableRoomsQuery = _context.rooms.Where(r => r.IsActive && r.modality != null);

            if (normalizedModality == "XRAY")
            {
                // match common X-Ray labels (XRAY, X-RAY, X-Ray 1, etc.)
                availableRoomsQuery = availableRoomsQuery.Where(r =>
                    r.modality.ToUpper().Contains("X-RAY") ||
                    r.modality.ToUpper().Contains("XRAY") ||
                    r.modality.ToUpper().Contains("X RAY"));
            }
            else if (normalizedModality == "CT")
            {
                // match CT variations
                availableRoomsQuery = availableRoomsQuery.Where(r =>
                    r.modality.ToUpper().Contains("CT"));
            }
            else
            {
                // fallback: match by token anywhere in modality string
                availableRoomsQuery = availableRoomsQuery.Where(r =>
                    r.modality.ToUpper().Contains(normalizedModality));
            }

            var availableRooms = await availableRoomsQuery
                .Select(r => new { r.roomid, r.modality, r.room_number })
                .ToListAsync();

            var viewModel = new
            {
                InhospId = inhospId,
                Modality = modality,
                PatientId = firstRequest.patientid,
                PatientName = patient != null
                    ? $"{patient.ChrPatientFirstname} {patient.ChrPatientMidname} {patient.ChrPatientLastname}".Trim()
                    : "Unknown",
                Exams = examRequests.Select(r => new
                {
                    RequestId = r.id,
                    ItemId = r.item_id,
                    BodyPart = bodyParts.ContainsKey(r.item_id) ? bodyParts[r.item_id] : "Unknown"
                }).ToList(),
                AvailableRooms = availableRooms
            };

            return View(viewModel);
        }

        /// <summary>
        /// Process scheduling of pending exams (assign rooms, generate tickets)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcessScheduling([FromBody] SchedulePendingViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.InhospId))
                {
                    return Json(new { success = false, message = "Inhosp ID is required" });
                }

                // Get all pending requests for this inpatient
                var allRequests = await _context.radiologyexamrequests
                    .Where(r => r.inhospid == model.InhospId && r.status == "PENDING")
                    .ToListAsync();

                if (!allRequests.Any())
                {
                    return Json(new { success = false, message = "No pending exams found" });
                }

                // Get body part modality info to filter by modality
                var itemIds = allRequests.Select(r => r.item_id).ToList();
                var bodyPartsWithModality = await _context.KmuNonMedicines
                    .Where(nm => itemIds.Contains(nm.ItemId))
                    .Select(nm => new { nm.ItemId, nm.GroupCode })
                    .ToListAsync();
                
                var bodyPartModality = bodyPartsWithModality.ToDictionary(
                    bp => bp.ItemId,
                    bp => bp.GroupCode == "C2" ? "XRAY" : bp.GroupCode == "C1" ? "CT" : "OTHER"
                );

                // Filter requests by the specified modality
                var requests = allRequests
                    .Where(r => bodyPartModality.ContainsKey(r.item_id) && bodyPartModality[r.item_id] == model.Modality)
                    .ToList();

                if (!requests.Any())
                {
                    return Json(new { success = false, message = $"No pending exams found for modality {model.Modality}" });
                }

                var orderedDate = DateOnly.FromDateTime(DateTime.Now);
                var tickets = new List<object>();

                // Generate a single OrderId for this modality group
                string generatedOrderId = await GenerateOrderId();

                // Assign the generated OrderId to all exams in this modality group
                foreach (var request in requests)
                {
                    request.orderid = generatedOrderId;
                }

                // Group exams by assigned room
                var examsByRoom = model.RoomAssignments.GroupBy(ra => ra.RoomId);

                foreach (var roomGroup in examsByRoom)
                {
                    int roomId = roomGroup.Key;
                    
                    // Get room info
                    var room = await _context.rooms.FirstOrDefaultAsync(r => r.roomid == roomId);
                    if (room == null) continue;

                    // Generate sequence number for this modality
                    int seqNo = await GenerateSequenceNumber(room.modality, orderedDate);

                    // Update each exam request in this room
                    foreach (var assignment in roomGroup)
                    {
                        var request = requests.FirstOrDefault(r => r.id == assignment.RequestId);
                        if (request == null) continue;

                        request.roomid = roomId;
                        request.seq_no = seqNo;
                        request.status = "SCHEDULED";
                        request.ordereddate = orderedDate;
                    }

                    await _context.SaveChangesAsync();

                    // Generate DICOM identifiers for each exam
                    foreach (var assignment in roomGroup)
                    {
                        var request = requests.FirstOrDefault(r => r.id == assignment.RequestId);
                        if (request == null) continue;

                        request.accessionnumber = $"{request.orderid}-{request.id}";
                        request.studyinstanceuid = GenerateStudyInstanceUID(request.id);
                    }

                    await _context.SaveChangesAsync();

                    // Generate worklists
                    foreach (var assignment in roomGroup)
                    {
                        var request = requests.FirstOrDefault(r => r.id == assignment.RequestId);
                        if (request == null) continue;

                        try
                        {
                            await _worklistService.GenerateAndUploadWorklistAsync(request);
                        }
                        catch (Exception worklistEx)
                        {
                            Console.WriteLine($"Warning: Failed to generate/upload worklist for request ID {request.id}: {worklistEx.Message}");
                        }
                    }

                    // Create ticket info for this room
                    tickets.Add(new
                    {
                        roomNumber = room.room_number,
                        modality = room.modality,
                        ticketNumber = seqNo,
                        examCount = roomGroup.Count()
                    });
                }

                return Json(new
                {
                    success = true,
                    message = "Exams scheduled successfully",
                    tickets = tickets
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessScheduling: {ex.Message}");
                return Json(new { success = false, message = $"Error scheduling exams: {ex.Message}" });
            }
        }

        /// <summary>
        /// View radiology report (placeholder for future implementation)
        /// </summary>
        [HttpGet]
        public IActionResult ViewReport(string orderId)
        {
            // TODO: Implement report viewing in future phase
            TempData["Message"] = "Report viewing feature coming soon!";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// View exam details (placeholder for future implementation)
        /// </summary>
        [HttpGet]
        public IActionResult ViewDetails(string orderId)
        {
            // TODO: Implement details viewing in future phase
            TempData["Message"] = "Details viewing feature coming soon!";
            return RedirectToAction("Index");
        }
    }

    // ViewModel for scheduling pending exams
    public class SchedulePendingViewModel
    {
        public string InhospId { get; set; }
        public string Modality { get; set; }
        public List<RoomAssignment> RoomAssignments { get; set; }
    }

    public class RoomAssignment
    {
        public int RequestId { get; set; }
        public int RoomId { get; set; }
    }
}

