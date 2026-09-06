using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KMU.HisOrder.MVC.Models;
using KMU.HisOrder.MVC.Models.BloodBank;
using Microsoft.EntityFrameworkCore;

namespace KMU.HisOrder.MVC.Areas.BloodBank.Models
{
    public class BloodBankService
    {
        private readonly KMUContext _context;

        public BloodBankService(KMUContext context)
        {
            _context = context;
        }

        public static int GetExpiryDays(string componentType)
        {
            var c = BloodBankLookups.NormalizeComponent(componentType);
            return c switch
            {
                BloodComponents.WholeBlood => 35,
                BloodComponents.PackedRbc => 42,
                BloodComponents.Platelets => 5,
                BloodComponents.Ffp => 365,
                BloodComponents.Cryoprecipitate => 365,
                _ => 42
            };
        }

        public static string FormatDonorFullName(string firstName, string? middleName, string lastName)
        {
            return string.Join(" ", new[] { firstName?.Trim(), middleName?.Trim(), lastName?.Trim() }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        public static string FormatDonorFullName(BloodBankDonor donor) =>
            FormatDonorFullName(donor.FirstName, donor.MiddleName, donor.LastName);

        public static string DeriveOverallResult(string hiv, string hepB, string hepC, string syphilis)
        {
            var tests = new[] { hiv, hepB, hepC, syphilis };
            return tests.All(t => t == TestResults.Pass) ? ScreeningResults.Passed : ScreeningResults.Failed;
        }

        public async Task<bool> CanEditDonorAsync(long donorId)
        {
            var donor = await _context.BloodBankDonors.FindAsync(donorId);
            if (donor == null || donor.Status == DonorStatuses.Deferred)
                return false;
            return !await _context.BloodBankBloodUnits.AnyAsync(u => u.DonorId == donorId);
        }

        public async Task<string> GenerateBagSerialAsync()
        {
            var prefix = $"BB-{DateTime.Now:yyyyMMdd}-";
            var count = await _context.BloodBankBloodUnits
                .CountAsync(u => u.SerialNumber.StartsWith(prefix));
            return $"{prefix}{(count + 1):D4}";
        }

        public async Task<BloodBankPatientPanelViewModel?> GetPatientPanelAsync(string patientId, long? requestId = null)
        {
            var chart = await _context.KmuCharts.FirstOrDefaultAsync(c => c.ChrHealthId == patientId);
            if (chart == null) return null;

            var panel = new BloodBankPatientPanelViewModel
            {
                PatientId = chart.ChrHealthId.Trim(),
                PatientName = FormatDonorFullName(chart.ChrPatientFirstname, chart.ChrPatientMidname, chart.ChrPatientLastname),
                Phone = chart.ChrMobilePhone ?? "",
                Address = chart.ChrAddress ?? ""
            };

            BloodBankRequest? request = null;
            if (requestId.HasValue)
                request = await _context.BloodBankRequests.FindAsync(requestId.Value);
            else
                request = await _context.BloodBankRequests
                    .Where(r => r.PatientId == patientId && r.Status == RequestStatuses.Pending)
                    .OrderByDescending(r => r.RequestDateTime)
                    .FirstOrDefaultAsync();

            if (request != null)
            {
                panel.RequestId = request.RequestId;
                panel.Inhospid = request.Inhospid;
                panel.Ward = request.Ward ?? "";
                panel.BedLocation = request.BedLocation ?? "";
                panel.RequestingDoctor = request.RequestingDoctorName;
                panel.RequestDateTime = request.RequestDateTime;
                panel.BloodType = request.BloodType;
                panel.ComponentType = request.ComponentType;
                panel.UnitsRequested = request.UnitsRequested;
                panel.UrgencyLevel = request.UrgencyLevel;
                panel.PatientType = request.PatientType;
            }
            else
            {
                var reg = await _context.Registrations
                    .Where(r => r.RegHealthId == patientId)
                    .OrderByDescending(r => r.RegDate)
                    .FirstOrDefaultAsync();
                if (reg != null)
                {
                    panel.Inhospid = reg.Inhospid;
                    panel.Ward = "";
                    panel.BedLocation = reg.RegBedNo ?? "";
                    panel.PatientType = "OPD";
                }
            }

            return panel;
        }

        public async Task<BloodBankPatientPanelViewModel?> GetPatientPanelByRequestAsync(long requestId)
        {
            var request = await _context.BloodBankRequests.FindAsync(requestId);
            if (request == null) return null;
            return await GetPatientPanelAsync(request.PatientId, requestId);
        }

        /// <summary>Stage 1 — donor only, status Registered. No blood unit.</summary>
        public async Task<(bool Success, string Message, long? DonorId)> RegisterDonorAsync(
            DonorRegistrationViewModel model, string userId, string userName)
        {
            if (model.DonationType == DonationTypes.Directed && string.IsNullOrWhiteSpace(model.PatientId))
                return (false, "Directed donation requires a HIS patient ID.", null);

            if (model.DonationType == DonationTypes.Directed && model.RequestId.HasValue)
            {
                var linkedRequest = await _context.BloodBankRequests.FindAsync(model.RequestId.Value);
                if (linkedRequest == null || linkedRequest.Status != RequestStatuses.Pending)
                    return (false, "Selected HIS blood request is not pending.", null);
                if (!string.Equals(linkedRequest.PatientId.Trim(), model.PatientId?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return (false, "Patient ID does not match the selected HIS request.", null);
            }

            var donor = new BloodBankDonor
            {
                FirstName = model.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim(),
                LastName = model.LastName.Trim(),
                Age = model.Age,
                JobDescription = string.IsNullOrWhiteSpace(model.JobDescription) ? null : model.JobDescription.Trim(),
                Gender = model.Gender,
                Phone = model.Phone.Trim(),
                NationalId = string.IsNullOrWhiteSpace(model.NationalId) ? null : model.NationalId.Trim(),
                Address = BuildDonorAddress(model),
                DonorNotes = model.RefugeeFlag ? "Refugee" : null,
                BloodType = model.BloodType,
                DonationType = model.DonationType,
                PatientId = model.PatientId?.Trim(),
                Inhospid = model.Inhospid,
                RequestId = model.RequestId,
                Status = DonorStatuses.Registered,
                ScreeningResult = ScreeningResults.Pending,
                CreateUser = userId,
                CreateDate = DateTime.Now
            };
            _context.BloodBankDonors.Add(donor);
            await _context.SaveChangesAsync();
            donor.ScreeningNotes = BuildPreDonationNotes(model);
            await _context.SaveChangesAsync();

            return (true, "Donor registered. Proceed to screening.", donor.DonorId);
        }

        public async Task<(bool Success, string Message)> UpdateDonorAsync(
            DonorRegistrationViewModel model, string userId)
        {
            if (!model.DonorId.HasValue)
                return (false, "Donor ID is required.");
            if (!await CanEditDonorAsync(model.DonorId.Value))
                return (false, "This donor can no longer be edited (bag already assigned or deferred).");

            var donor = await _context.BloodBankDonors.FindAsync(model.DonorId.Value);
            if (donor == null)
                return (false, "Donor not found.");

            if (model.DonationType == DonationTypes.Directed && string.IsNullOrWhiteSpace(model.PatientId))
                return (false, "Directed donation requires a HIS patient ID.");

            donor.FirstName = model.FirstName.Trim();
            donor.MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim();
            donor.LastName = model.LastName.Trim();
            donor.Age = model.Age;
            donor.JobDescription = string.IsNullOrWhiteSpace(model.JobDescription) ? null : model.JobDescription.Trim();
            donor.Gender = model.Gender;
            donor.Phone = model.Phone.Trim();
            donor.NationalId = string.IsNullOrWhiteSpace(model.NationalId) ? null : model.NationalId.Trim();
            donor.Address = BuildDonorAddress(model);
            if (model.RefugeeFlag)
                donor.DonorNotes = string.IsNullOrWhiteSpace(donor.DonorNotes) ? "Refugee" : donor.DonorNotes;
            donor.BloodType = model.BloodType;
            donor.DonationType = model.DonationType;
            donor.PatientId = model.PatientId?.Trim();
            donor.Inhospid = model.Inhospid;
            donor.RequestId = model.RequestId;
            donor.PreDonationMalariaResult = null;
            donor.ScreeningNotes = BuildPreDonationNotes(model);
            donor.ModifyUser = userId;
            donor.ModifyDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return (true, "Donor record updated.");
        }

        private static string? BuildDonorAddress(DonorRegistrationViewModel model)
        {
            var line = (model.Address ?? "").Trim();
            if (string.IsNullOrWhiteSpace(line))
                return null;
            if (!string.IsNullOrWhiteSpace(model.AreaCode))
                return $"{model.AreaCode.Trim()} — {line}";
            return line;
        }

        private static string? BuildRegistrationVitalsNotes(DonorRegistrationViewModel model)
        {
            var parts = new[]
            {
                string.IsNullOrWhiteSpace(model.PreDonationBloodPressure) ? null : $"BP: {model.PreDonationBloodPressure}"
            }.Where(s => s != null).ToList();
            return parts.Count == 0 ? null : string.Join(" | ", parts);
        }

        private static string? BuildScreeningPreDonationNotes(ScreeningFormViewModel model)
        {
            var parts = new[]
            {
                string.IsNullOrWhiteSpace(model.Hemoglobin) ? null : $"Hb: {model.Hemoglobin} g/dL",
                string.IsNullOrWhiteSpace(model.Wbc) ? null : $"WBC: {model.Wbc}",
                string.IsNullOrWhiteSpace(model.Rbc) ? null : $"RBC: {model.Rbc}",
                string.IsNullOrWhiteSpace(model.Platelet) ? null : $"Platelet: {model.Platelet}"
            }.Where(s => s != null).ToList();
            return parts.Count == 0 ? null : string.Join(" | ", parts);
        }

        private static string? BuildPreDonationNotes(DonorRegistrationViewModel model)
        {
            return BuildRegistrationVitalsNotes(model);
        }

        /// <summary>Logs a donor deferred at pre-donation screening (before infectious disease testing).</summary>
        public async Task<(bool Success, string Message)> LogPreDonationDeferralAsync(
            PreDonationDeferralViewModel model, string userId)
        {
            var flags = model.DeferralFlags != null && model.DeferralFlags.Length > 0
                ? string.Join("; ", model.DeferralFlags)
                : "—";
            var notes = $"Pre-donation deferral. Reason: {model.DeferralReason ?? "Clinical deferral"}. " +
                        $"Flags: {flags}. Vitals — Hb: {model.Hemoglobin ?? "—"}, BP: {model.BloodPressure ?? "—"}, " +
                        $"WBC: {model.Wbc ?? "—"}, RBC: {model.Rbc ?? "—"}, Platelet: {model.Platelet ?? "—"}.";

            var donor = new BloodBankDonor
            {
                FirstName = model.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(model.MiddleName) ? null : model.MiddleName.Trim(),
                LastName = model.LastName.Trim(),
                Age = model.Age,
                JobDescription = string.IsNullOrWhiteSpace(model.JobDescription) ? null : model.JobDescription.Trim(),
                Gender = model.Gender,
                Phone = model.Phone.Trim(),
                BloodType = "—",
                DonationType = DonationTypes.Volunteer,
                Status = DonorStatuses.Deferred,
                ScreeningResult = ScreeningResults.Failed,
                ScreeningNotes = notes,
                CreateUser = userId,
                CreateDate = DateTime.Now
            };
            _context.BloodBankDonors.Add(donor);
            await _context.SaveChangesAsync();
            return (true, "Donor deferral logged for compliance.");
        }

        public async Task<List<ScreeningQueueItemViewModel>> GetScreeningQueueAsync()
        {
            var donorRows = await _context.BloodBankDonors
                .Where(d => d.Status == DonorStatuses.Registered || d.Status == DonorStatuses.Screening)
                .OrderBy(d => d.CreateDate)
                .ToListAsync();

            var donors = donorRows.Select(d => new ScreeningQueueItemViewModel
            {
                DonorId = d.DonorId,
                BloodBankDonorId = BloodBankIds.FormatDonor(d.DonorId),
                FullName = FormatDonorFullName(d),
                Phone = d.Phone,
                BloodType = d.BloodType,
                DonationType = d.DonationType,
                RegisteredAt = d.CreateDate,
                IsLegacyUnit = false
            }).ToList();

            var legacyUnitRows = await _context.BloodBankBloodUnits
                .Include(u => u.Donor)
                .Where(u => u.Status == UnitStatuses.PendingScreening)
                .OrderBy(u => u.CreateDate)
                .ToListAsync();

            var legacyUnits = legacyUnitRows.Select(u => new ScreeningQueueItemViewModel
            {
                DonorId = u.DonorId,
                BloodBankDonorId = BloodBankIds.FormatDonor(u.DonorId),
                FullName = FormatDonorFullName(u.Donor),
                Phone = u.Donor.Phone,
                BloodType = u.BloodType,
                DonationType = u.Donor.DonationType,
                RegisteredAt = u.CreateDate,
                IsLegacyUnit = true,
                UnitId = u.UnitId,
                SerialNumber = u.SerialNumber
            }).ToList();

            return donors.Concat(legacyUnits).OrderBy(x => x.RegisteredAt).ToList();
        }

        /// <summary>Stage 2 — screening on donor (or legacy unit).</summary>
        public async Task<(bool Success, string Message, long? ScreeningId)> RecordScreeningAsync(
            ScreeningFormViewModel model, string userId, string userName)
        {
            var overall = DeriveOverallResult(
                model.HivResult, model.HepBResult, model.HepCResult, model.SyphilisResult);
            model.OverallResult = overall;

            BloodBankDonor? donor = null;
            BloodBankBloodUnit? unit = null;

            if (model.UnitId.HasValue)
            {
                unit = await _context.BloodBankBloodUnits.Include(u => u.Donor).FirstOrDefaultAsync(u => u.UnitId == model.UnitId);
                if (unit == null) return (false, "Unit not found.", null);
                donor = unit.Donor;
            }
            else
            {
                donor = await _context.BloodBankDonors.FindAsync(model.DonorId);
                if (donor == null) return (false, "Donor not found.", null);
            }

            var preDonationNotes = BuildScreeningPreDonationNotes(model);
            var combinedNotes = string.Join(" | ", new[] { preDonationNotes, model.Notes }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var screening = new BloodBankScreeningRecord
            {
                DonorId = donor.DonorId,
                UnitId = unit?.UnitId,
                StaffUserId = userId,
                StaffName = userName,
                ScreeningDateTime = DateTime.Now,
                HivResult = model.HivResult,
                HepBResult = model.HepBResult,
                HepCResult = model.HepCResult,
                SyphilisResult = model.SyphilisResult,
                MalariaResult = "N/A",
                OverallResult = overall,
                Notes = combinedNotes,
                CreateUser = userId,
                CreateDate = DateTime.Now
            };
            _context.BloodBankScreeningRecords.Add(screening);

            donor.Status = overall == ScreeningResults.Passed ? DonorStatuses.Passed : DonorStatuses.Failed;
            donor.ScreeningResult = overall;
            var regNotes = donor.ScreeningNotes;
            donor.ScreeningNotes = string.Join(" | ", new[] { regNotes, preDonationNotes, model.Notes }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            donor.ModifyUser = userId;
            donor.ModifyDate = DateTime.Now;

            if (unit != null)
            {
                unit.ScreeningResult = overall;
                unit.ModifyUser = userId;
                unit.ModifyDate = DateTime.Now;
                if (overall == ScreeningResults.Passed)
                    unit.Status = UnitStatuses.Available;
                else
                    unit.Status = UnitStatuses.Quarantined;
            }

            await _context.SaveChangesAsync();
            return (true, overall == ScreeningResults.Passed
                ? "Screening passed. Donor moved to bag assignment queue."
                : "Screening failed. Donor quarantined.", screening.ScreeningId);
        }

        public async Task<ScreeningCertificateViewModel?> GetScreeningCertificateAsync(long screeningId)
        {
            var screening = await _context.BloodBankScreeningRecords
                .Include(s => s.Donor)
                .Include(s => s.Unit)
                .FirstOrDefaultAsync(s => s.ScreeningId == screeningId);
            if (screening?.Donor == null) return null;

            var donor = screening.Donor;
            var assignedUnit = await _context.BloodBankBloodUnits
                .Where(u => u.DonorId == donor.DonorId)
                .OrderByDescending(u => u.CreateDate)
                .FirstOrDefaultAsync();

            return new ScreeningCertificateViewModel
            {
                ScreeningId = screening.ScreeningId,
                DonorId = donor.DonorId,
                BloodBankDonorId = BloodBankIds.FormatDonor(donor.DonorId),
                DonorName = FormatDonorFullName(donor),
                BloodType = donor.BloodType,
                DonationType = donor.DonationType,
                PatientId = donor.PatientId,
                SerialNumber = screening.Unit?.SerialNumber,
                BagSerialNumber = assignedUnit?.SerialNumber,
                ScreeningDateTime = screening.ScreeningDateTime,
                StaffName = screening.StaffName,
                OverallResult = screening.OverallResult,
                HivResult = screening.HivResult,
                HepBResult = screening.HepBResult,
                HepCResult = screening.HepCResult,
                SyphilisResult = screening.SyphilisResult,
                MalariaResult = screening.MalariaResult,
                Notes = screening.Notes
            };
        }

        public async Task<List<BagAssignmentQueueItemViewModel>> GetBagAssignmentQueueAsync()
        {
            var passedDonorIds = await _context.BloodBankBloodUnits
                .Select(u => u.DonorId)
                .Distinct()
                .ToListAsync();

            var donors = await _context.BloodBankDonors
                .Where(d => d.Status == DonorStatuses.Passed && !passedDonorIds.Contains(d.DonorId))
                .OrderBy(d => d.CreateDate)
                .ToListAsync();

            return donors.Select(d => new BagAssignmentQueueItemViewModel
            {
                DonorId = d.DonorId,
                BloodBankDonorId = BloodBankIds.FormatDonor(d.DonorId),
                FullName = FormatDonorFullName(d),
                Phone = d.Phone,
                BloodType = d.BloodType,
                DonationType = d.DonationType,
                RegisteredAt = d.CreateDate,
                PatientId = d.PatientId
            }).ToList();
        }

        /// <summary>Stage 3 — assign bag serial and create inventory unit.</summary>
        public async Task<(bool Success, string Message, long? UnitId)> AssignBagAsync(
            BagAssignmentFormViewModel model, string userId, string userName)
        {
            var donor = await _context.BloodBankDonors.FindAsync(model.DonorId);
            if (donor == null) return (false, "Donor not found.", null);
            if (donor.Status != DonorStatuses.Passed)
                return (false, "Only screening-passed donors can receive bag assignment.", null);

            if (await _context.BloodBankBloodUnits.AnyAsync(u => u.DonorId == donor.DonorId))
                return (false, "This donor already has a blood unit assigned.", null);

            var serial = model.UseGeneratedSerial
                ? await GenerateBagSerialAsync()
                : model.SerialNumber?.Trim();
            if (string.IsNullOrWhiteSpace(serial))
                return (false, "Bag serial number is required.", null);

            if (await _context.BloodBankBloodUnits.AnyAsync(u => u.SerialNumber == serial))
                return (false, "This bag serial number already exists.", null);

            var collectionDate = model.CollectionDate;
            var expiry = collectionDate.AddDays(GetExpiryDays(model.ComponentType));

            var unit = new BloodBankBloodUnit
            {
                SerialNumber = serial,
                DonorId = donor.DonorId,
                BloodType = donor.BloodType,
                ComponentType = BloodBankLookups.NormalizeComponent(model.ComponentType),
                Status = UnitStatuses.Available,
                Quantity = 1,
                VolumeMl = model.VolumeMl,
                StorageLocation = model.StorageLocation?.Trim(),
                DonationDate = collectionDate,
                ExpiryDate = expiry,
                ScreeningResult = ScreeningResults.Passed,
                PatientId = donor.PatientId,
                RequestId = donor.RequestId,
                CreateUser = userId,
                CreateDate = DateTime.Now
            };
            _context.BloodBankBloodUnits.Add(unit);
            await _context.SaveChangesAsync();
            return (true, $"Unit {serial} entered into inventory as Available.", unit.UnitId);
        }

        public static string FormatWardBed(string? ward, string? bed)
        {
            var w = string.IsNullOrWhiteSpace(ward) ? "—" : ward.Trim();
            var b = string.IsNullOrWhiteSpace(bed) ? "—" : bed.Trim();
            return $"{w} / {b}";
        }

        public const string OutsideHospitalDestination = "Outside hospital";
        public const string OutsideHospitalPrefix = "Outside hospital —";

        /// <summary>Formats manual dispense to another hospital: Outside hospital — {hospital} / {ward} [/ bed].</summary>
        public static string BuildManualOutsideDestination(string? hospital, string? ward, string? bed)
        {
            var h = hospital?.Trim() ?? "";
            var w = ward?.Trim() ?? "";
            if (string.IsNullOrEmpty(h) || string.IsNullOrEmpty(w))
                return "";
            var main = $"{OutsideHospitalPrefix} {h} / {w}";
            var b = bed?.Trim();
            return string.IsNullOrEmpty(b) ? main : $"{main} / {b}";
        }

        public static bool IsManualOutsideHospital(DispenseFormViewModel model)
        {
            var key = model.ManualDestinationKey?.Trim() ?? "";
            if (string.Equals(key, OutsideHospitalDestination, StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(key, "Other-outside", StringComparison.OrdinalIgnoreCase))
                return true;
            return model.WardDestination?.StartsWith(OutsideHospitalPrefix, StringComparison.Ordinal) == true;
        }

        public async Task<List<PendingBloodRequestSearchItem>> SearchPendingBloodRequestsAsync(
            string? term,
            int take = 25,
            string? bloodType = null,
            string? wardFilter = null)
        {
            var q = _context.BloodBankRequests
                .Where(r => r.Status == RequestStatuses.Pending)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(bloodType))
                q = q.Where(r => r.BloodType == bloodType);

            if (!string.IsNullOrWhiteSpace(wardFilter))
            {
                var w = wardFilter.Trim();
                q = q.Where(r => r.Ward != null && r.Ward.Contains(w));
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim();
                q = q.Where(r =>
                    r.PatientId.Contains(t)
                    || r.PatientName.Contains(t)
                    || (r.Ward != null && r.Ward.Contains(t))
                    || (r.RequestingDoctorName != null && r.RequestingDoctorName.Contains(t)));
            }

            return await q
                .OrderByDescending(r => r.UrgencyLevel == UrgencyLevels.Emergency)
                .ThenByDescending(r => r.UrgencyLevel == UrgencyLevels.Urgent)
                .ThenByDescending(r => r.RequestDateTime)
                .Take(take)
                .Select(r => new PendingBloodRequestSearchItem
                {
                    RequestId = r.RequestId,
                    PatientId = r.PatientId,
                    PatientName = r.PatientName,
                    Inhospid = r.Inhospid,
                    Ward = r.Ward,
                    BedLocation = r.BedLocation,
                    RequestingDoctorName = r.RequestingDoctorName,
                    BloodType = r.BloodType,
                    ComponentType = r.ComponentType,
                    UnitsRequested = r.UnitsRequested,
                    UrgencyLevel = r.UrgencyLevel,
                    RequestDateTime = r.RequestDateTime
                })
                .ToListAsync();
        }

        public async Task<DispensePanelViewModel?> BuildDispensePanelAsync(long requestId, string staffName)
        {
            var request = await _context.BloodBankRequests.FindAsync(requestId);
            if (request == null) return null;

            request.ComponentType = BloodBankLookups.NormalizeComponent(request.ComponentType);

            var panel = await GetPatientPanelByRequestAsync(requestId) ?? new BloodBankPatientPanelViewModel();
            var now = DateTime.Now;
            var reqPatientId = request.PatientId.Trim();
            var reqComponent = BloodBankLookups.NormalizeComponent(request.ComponentType);

            var directedDonors = await _context.BloodBankDonors
                .Where(d => d.DonationType == DonationTypes.Directed
                    && d.PatientId == reqPatientId
                    && d.Status == DonorStatuses.Passed)
                .Select(d => new DirectedDonorOptionViewModel
                {
                    DonorId = d.DonorId,
                    FullName = FormatDonorFullName(d),
                    Phone = d.Phone,
                    BloodType = d.BloodType,
                    ScreeningResult = d.ScreeningResult,
                    DonorPatientId = d.PatientId,
                    IsVerifiedPatientLinkage = true
                })
                .ToListAsync();

            foreach (var d in directedDonors)
            {
                var donorUnits = (await _context.BloodBankBloodUnits
                    .Where(u => u.DonorId == d.DonorId
                        && u.Status == UnitStatuses.Available
                        && u.ScreeningResult == ScreeningResults.Passed
                        && u.ExpiryDate >= now)
                    .ToListAsync())
                    .OrderByDescending(u => BloodBankLookups.IsExactBloodTypeMatch(request.BloodType, u.BloodType))
                    .ThenByDescending(u => u.CreateDate)
                    .ToList();

                var matchingUnit = donorUnits
                    .FirstOrDefault(u => BloodBankLookups.NormalizeComponent(u.ComponentType) == reqComponent
                        && BloodBankLookups.IsCompatibleRbc(request.BloodType, u.BloodType));

                if (matchingUnit != null)
                {
                    d.UnitId = matchingUnit.UnitId;
                    d.SerialNumber = matchingUnit.SerialNumber;
                    d.DonationDate = matchingUnit.DonationDate;
                    d.ComponentType = BloodBankLookups.NormalizeComponent(matchingUnit.ComponentType);
                    d.IsExactBloodTypeMatch = BloodBankLookups.IsExactBloodTypeMatch(request.BloodType, matchingUnit.BloodType);
                    d.IsAvailable = true;
                }
                else
                {
                    var wrongComponent = donorUnits.FirstOrDefault(u =>
                        BloodBankLookups.IsCompatibleRbc(request.BloodType, u.BloodType));
                    d.IsAvailable = false;
                    d.UnavailableReason = donorUnits.Count == 0
                        ? "No bag assigned yet — complete screening and bag assignment first."
                        : wrongComponent != null
                            ? $"Unit {wrongComponent.SerialNumber} is {BloodBankLookups.NormalizeComponent(wrongComponent.ComponentType)}, not {reqComponent}."
                            : "No compatible unit available for this request.";
                }

                var scr = await _context.BloodBankScreeningRecords
                    .Where(s => s.DonorId == d.DonorId)
                    .OrderByDescending(s => s.ScreeningDateTime)
                    .FirstOrDefaultAsync();
                if (scr != null) d.ScreeningDate = scr.ScreeningDateTime;
            }

            var candidateUnits = await _context.BloodBankBloodUnits
                .Include(u => u.Donor)
                .Where(u => u.Status == UnitStatuses.Available
                    && u.ScreeningResult == ScreeningResults.Passed
                    && u.ExpiryDate >= now)
                .OrderBy(u => u.ExpiryDate)
                .ToListAsync();

            var volunteerUnits = candidateUnits
                .Where(u => u.Donor.DonationType == DonationTypes.Volunteer
                    && BloodBankLookups.NormalizeComponent(u.ComponentType) == reqComponent
                    && BloodBankLookups.IsCompatibleRbc(request.BloodType, u.BloodType))
                .OrderBy(u => BloodBankLookups.IsExactBloodTypeMatch(request.BloodType, u.BloodType) ? 0 : 1)
                .ThenBy(u => u.ExpiryDate)
                .ToList();

            var cards = volunteerUnits.Select((u, idx) =>
            {
                var days = (int)(u.ExpiryDate.Date - now.Date).TotalDays;
                var exact = BloodBankLookups.IsExactBloodTypeMatch(request.BloodType, u.BloodType);
                var isDirected = u.Donor.DonationType == DonationTypes.Directed
                    && string.Equals(u.PatientId?.Trim(), reqPatientId, StringComparison.OrdinalIgnoreCase);
                return new VolunteerUnitCardViewModel
                {
                    UnitId = u.UnitId,
                    SerialNumber = u.SerialNumber,
                    BloodType = u.BloodType,
                    ComponentType = BloodBankLookups.NormalizeComponent(u.ComponentType),
                    VolumeMl = u.VolumeMl,
                    ExpiryDate = u.ExpiryDate,
                    DaysUntilExpiry = days,
                    ExpiringSoon = days <= 7,
                    StorageLocation = u.StorageLocation,
                    DonorReference = $"Donor #{u.DonorId}",
                    IsDirectedForRequestPatient = isDirected,
                    IsExactBloodTypeMatch = exact,
                    CompatibilityLabel = exact ? "Exact match" : "Compatible",
                    IsRecommended = idx == 0
                };
            }).ToList();

            var wardBed = FormatWardBed(request.Ward, request.BedLocation);
            var availableDirected = directedDonors.Count(d => d.IsAvailable);

            return new DispensePanelViewModel
            {
                RequestId = requestId,
                Request = request,
                PatientPanel = panel,
                DirectedDonors = directedDonors,
                VolunteerUnits = cards,
                IsHisLinkedRequest = true,
                LockedPatientName = request.PatientName,
                LockedWardBed = wardBed,
                LockedUnitsRequested = request.UnitsRequested,
                WardDestination = wardBed,
                CollectorName = staffName?.Trim() ?? "",
                DispensingStaffName = staffName,
                SourceMode = availableDirected > 0 ? "Directed" : "Volunteer",
                CompatibleDonorTypesSummary = BloodBankLookups.DescribeRbcCompatibility(request.BloodType),
                AvailableDirectedCount = availableDirected,
                AvailableVolunteerCount = cards.Count
            };
        }

        public async Task<(bool Success, string Message)> DispenseUnitsAsync(
            DispensePanelViewModel model, string userId, string userName)
        {
            var request = await _context.BloodBankRequests.FindAsync(model.RequestId);
            if (request == null) return (false, "Request not found.");
            if (request.Status != RequestStatuses.Pending)
                return (false, "Request is not pending.");

            model.WardDestination = FormatWardBed(request.Ward, request.BedLocation);
            model.PatientPanel.PatientName = request.PatientName;
            model.PatientPanel.PatientId = request.PatientId;
            model.PatientPanel.Ward = request.Ward ?? "";
            model.PatientPanel.BedLocation = request.BedLocation ?? "";

            List<long> unitIds;
            var reqComponent = BloodBankLookups.NormalizeComponent(request.ComponentType);
            if (model.SourceMode == "Directed")
            {
                if (!model.SelectedDirectedDonorId.HasValue)
                    return (false, "Select a directed donor.");
                var directedUnits = await _context.BloodBankBloodUnits
                    .Where(u => u.DonorId == model.SelectedDirectedDonorId
                        && u.Status == UnitStatuses.Available
                        && u.ScreeningResult == ScreeningResults.Passed
                        && u.ExpiryDate >= DateTime.Now)
                    .ToListAsync();
                var unit = directedUnits.FirstOrDefault(u =>
                    BloodBankLookups.NormalizeComponent(u.ComponentType) == reqComponent
                    && BloodBankLookups.IsCompatibleRbc(request.BloodType, u.BloodType));
                if (unit == null)
                    return (false, "No compatible unit available for the selected directed donor.");
                unitIds = new List<long> { unit.UnitId };
            }
            else
            {
                unitIds = model.SelectedUnitIds?.Distinct().ToList() ?? new List<long>();
            }

            if (unitIds.Count == 0)
                return (false, "Select at least one blood unit.");
            if (unitIds.Count != request.UnitsRequested)
                return (false, $"Select exactly {request.UnitsRequested} unit(s) for this request.");
            if (string.IsNullOrWhiteSpace(model.CollectorName))
                return (false, "Collector name is required.");
            var directedDonorSelected = model.SourceMode == "Directed" && model.SelectedDirectedDonorId.HasValue;
            if (!model.Confirmed && !directedDonorSelected)
                return (false, "Confirm dispense before submitting.");

            var panel = model.PatientPanel;
            var serials = new List<string>();

            foreach (var unitId in unitIds)
            {
                var unit = await _context.BloodBankBloodUnits
                    .Include(u => u.Donor)
                    .FirstOrDefaultAsync(u => u.UnitId == unitId);
                if (unit == null) return (false, $"Unit {unitId} not found.");
                if (unit.Status != UnitStatuses.Available && unit.Status != UnitStatuses.Reserved)
                    return (false, $"Unit {unit.SerialNumber} is not available.");
                if (unit.ScreeningResult != ScreeningResults.Passed)
                    return (false, $"Unit {unit.SerialNumber} has not passed screening.");
                if (unit.ExpiryDate < DateTime.Now)
                    return (false, $"Unit {unit.SerialNumber} has expired.");
                if (BloodBankLookups.NormalizeComponent(unit.ComponentType) != reqComponent)
                    return (false, $"Unit {unit.SerialNumber} is {unit.ComponentType}, not {request.ComponentType}.");
                if (!BloodBankLookups.IsCompatibleRbc(request.BloodType, unit.BloodType))
                    return (false, $"Unit {unit.SerialNumber} ({unit.BloodType}) is not compatible with patient type {request.BloodType}.");

                if (model.SourceMode == "Directed" && unit.Donor != null
                    && unit.Donor.DonationType == DonationTypes.Directed
                    && !string.Equals(unit.Donor.PatientId?.Trim(), request.PatientId.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return (false, $"Unit {unit.SerialNumber} is a directed donation for a different patient.");
                }

                serials.Add(unit.SerialNumber);
                unit.Status = UnitStatuses.Dispensed;
                unit.ModifyUser = userId;
                unit.ModifyDate = DateTime.Now;
            }

            var dispense = new BloodBankDispenseRecord
            {
                UnitId = unitIds[0],
                RequestId = model.RequestId,
                PatientId = panel.PatientId,
                Inhospid = panel.Inhospid,
                PatientName = panel.PatientName,
                CollectorName = model.CollectorName.Trim(),
                WardDestination = model.WardDestination?.Trim() ?? panel.Ward,
                UnitsReleased = unitIds.Count,
                SerialNumbers = string.Join(", ", serials),
                DispenseDateTime = DateTime.Now,
                DispensingStaffId = userId,
                DispensingStaffName = userName,
                CreateUser = userId,
                CreateDate = DateTime.Now
            };
            _context.BloodBankDispenseRecords.Add(dispense);

            request.Status = RequestStatuses.Fulfilled;
            request.ModifyUser = userId;
            request.ModifyDate = DateTime.Now;

            await WritePatientEventAsync(
                panel.PatientId,
                panel.Inhospid,
                PatientEventTypes.BloodDispensed,
                $"Blood dispensed — {unitIds.Count} unit(s) — {request.ComponentType} — {userName} — {DateTime.Now:g}. Serials: {string.Join(", ", serials)}.",
                userId);

            await _context.SaveChangesAsync();
            return (true, "Blood dispensed successfully.");
        }

        public async Task<(bool Success, string Message)> DispenseUnitAsync(
            DispenseFormViewModel model, string userId, string userName)
        {
            var panel = model.PatientPanel;
            var multi = new DispensePanelViewModel
            {
                RequestId = model.RequestId ?? panel.RequestId ?? 0,
                PatientPanel = panel,
                CollectorName = model.CollectorName,
                WardDestination = model.WardDestination,
                SourceMode = "Volunteer",
                SelectedUnitIds = new List<long> { model.UnitId },
                Confirmed = true
            };
            if (multi.RequestId == 0)
            {
                return await DispenseSingleUnitWithoutRequestAsync(model, userId, userName);
            }
            return await DispenseUnitsAsync(multi, userId, userName);
        }

        private async Task<(bool Success, string Message)> DispenseSingleUnitWithoutRequestAsync(
            DispenseFormViewModel model, string userId, string userName)
        {
            var unit = await _context.BloodBankBloodUnits.FindAsync(model.UnitId);
            if (unit == null) return (false, "Unit not found.");
            if (unit.Status != UnitStatuses.Available && unit.Status != UnitStatuses.Reserved)
                return (false, "Unit is not available for dispense.");
            if (unit.ScreeningResult != ScreeningResults.Passed)
                return (false, "Unit has not passed screening.");

            var panel = model.PatientPanel;
            var sourceDonorId = model.SourceDonorId ?? unit.DonorId;
            _context.BloodBankDispenseRecords.Add(new BloodBankDispenseRecord
            {
                UnitId = unit.UnitId,
                RequestId = model.RequestId,
                SourceDonorId = sourceDonorId,
                DonationSource = model.DonationSource?.Trim(),
                PatientId = panel.PatientId,
                Inhospid = panel.Inhospid,
                PatientName = panel.PatientName,
                CollectorName = model.CollectorName,
                WardDestination = model.WardDestination,
                UnitsReleased = 1,
                SerialNumbers = unit.SerialNumber,
                DispenseDateTime = DateTime.Now,
                DispensingStaffId = userId,
                DispensingStaffName = userName,
                CreateUser = userId,
                CreateDate = DateTime.Now
            });

            if (sourceDonorId > 0)
            {
                var donor = await _context.BloodBankDonors.FindAsync(sourceDonorId);
                if (donor != null)
                {
                    donor.LastDonationDate = DateTime.Now;
                    donor.ModifyUser = userId;
                    donor.ModifyDate = DateTime.Now;
                }
            }

            unit.Status = UnitStatuses.Dispensed;
            unit.ModifyUser = userId;
            unit.ModifyDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, "Blood dispensed successfully.");
        }

        public async Task<(bool Success, string Message, long? RequestId)> CreateManualRequestAsync(
            BloodRequestFormViewModel model, string userId)
        {
            var api = new HisBloodRequestApiModel
            {
                PatientId = model.PatientId.Trim(),
                Inhospid = model.Inhospid?.Trim() ?? "",
                PatientName = model.PatientName.Trim(),
                Ward = model.Ward,
                BedLocation = model.BedLocation,
                BloodType = model.BloodType,
                ComponentType = model.ComponentType,
                UnitsRequested = model.UnitsRequested,
                UrgencyLevel = model.UrgencyLevel,
                PatientType = model.PatientType,
                RequestingDoctorId = model.RequestingDoctorId,
                RequestingDoctorName = model.RequestingDoctorName
            };
            return await CreateRequestFromApiAsync(api, userId);
        }

        public async Task<(bool Success, string Message, long? RequestId)> CreateRequestFromApiAsync(
            HisBloodRequestApiModel model, string userId)
        {
            if (model.Orderplanid.HasValue && await _context.BloodBankRequests.AnyAsync(r => r.Orderplanid == model.Orderplanid))
                return (false, "Request already exists for this order.", null);

            var request = new BloodBankRequest
            {
                Orderplanid = model.Orderplanid,
                PatientId = model.PatientId.Trim(),
                Inhospid = (model.Inhospid ?? "").Trim(),
                PatientName = model.PatientName.Trim(),
                Ward = model.Ward,
                BedLocation = model.BedLocation,
                RequestingDoctorId = Truncate(model.RequestingDoctorId, 7),
                RequestingDoctorName = model.RequestingDoctorName,
                BloodType = model.BloodType,
                ComponentType = model.ComponentType,
                UnitsRequested = model.UnitsRequested,
                UrgencyLevel = model.UrgencyLevel,
                PatientType = model.PatientType,
                Status = RequestStatuses.Pending,
                RequestDateTime = DateTime.Now,
                CreateUser = Truncate(userId, 7),
                CreateDate = DateTime.Now
            };
            _context.BloodBankRequests.Add(request);
            await _context.SaveChangesAsync();

            await WritePatientEventAsync(
                request.PatientId,
                request.Inhospid,
                PatientEventTypes.DoctorBloodRequest,
                $"Blood request — {request.BloodType} {request.ComponentType} ×{request.UnitsRequested} — {request.UrgencyLevel}.",
                userId);

            return (true, "Blood request created.", request.RequestId);
        }

        public async Task WritePatientEventAsync(string patientId, string inhospid, string eventType, string description, string userId)
        {
            _context.BloodBankPatientEvents.Add(new BloodBankPatientEvent
            {
                PatientId = patientId,
                Inhospid = inhospid,
                EventType = eventType,
                EventDescription = description,
                EventDateTime = DateTime.Now,
                CreateUser = userId,
                CreateDate = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<BloodBankPatientEvent>> GetPatientEventsAsync(string inhospid)
        {
            return await _context.BloodBankPatientEvents
                .Where(e => e.Inhospid == inhospid)
                .OrderByDescending(e => e.EventDateTime)
                .ToListAsync();
        }

        public IQueryable<BloodBankBloodUnit> QueryInventory(InventoryListViewModel filters)
        {
            var q = _context.BloodBankBloodUnits.Include(u => u.Donor).AsQueryable();

            if (!string.IsNullOrWhiteSpace(filters.FilterBloodType))
                q = q.Where(u => u.BloodType == filters.FilterBloodType);
            if (!string.IsNullOrWhiteSpace(filters.FilterComponent))
                q = q.Where(u => u.ComponentType == filters.FilterComponent);
            if (!string.IsNullOrWhiteSpace(filters.FilterStatus))
                q = q.Where(u => u.Status == filters.FilterStatus);
            if (filters.ExpiryFrom.HasValue)
                q = q.Where(u => u.ExpiryDate >= filters.ExpiryFrom.Value);
            if (filters.ExpiryTo.HasValue)
                q = q.Where(u => u.ExpiryDate <= filters.ExpiryTo.Value);
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                var term = filters.SearchTerm.Trim();
                if (BloodBankIds.TryParseUnitId(term, out var unitId))
                {
                    q = q.Where(u => u.UnitId == unitId);
                }
                else if (BloodBankIds.TryParseDonorId(term, out var donorId))
                {
                    q = q.Where(u => u.DonorId == donorId);
                }
                else
                {
                    q = q.Where(u =>
                        u.SerialNumber.Contains(term) ||
                        u.BloodType.Contains(term) ||
                        (u.Donor != null && (
                            (u.Donor.FirstName + " " + u.Donor.LastName).Contains(term) ||
                            u.Donor.Phone.Contains(term) ||
                            u.Donor.DonorId.ToString() == term)));
                }
            }

            return q.OrderBy(u => u.ExpiryDate);
        }

        public async Task<(bool Success, string Message)> ApproveUnitAsync(long unitId, string userId)
        {
            var unit = await _context.BloodBankBloodUnits.FindAsync(unitId);
            if (unit == null) return (false, "Unit not found.");
            unit.Status = UnitStatuses.Available;
            unit.ModifyUser = userId;
            unit.ModifyDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, "Unit approved.");
        }

        public async Task<(bool Success, string Message)> RejectUnitAsync(long unitId, string userId)
        {
            var unit = await _context.BloodBankBloodUnits.FindAsync(unitId);
            if (unit == null) return (false, "Unit not found.");
            unit.Status = UnitStatuses.Rejected;
            unit.ModifyUser = userId;
            unit.ModifyDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, "Unit rejected.");
        }

        public async Task<UnitHistoryViewModel?> GetUnitHistoryDetailAsync(long unitId)
        {
            var unit = await _context.BloodBankBloodUnits
                .Include(u => u.Donor)
                .FirstOrDefaultAsync(u => u.UnitId == unitId);
            if (unit == null) return null;

            var donor = unit.Donor;
            BloodBankRequest? request = null;
            if (unit.RequestId.HasValue)
                request = await _context.BloodBankRequests.FindAsync(unit.RequestId.Value);
            else if (donor?.RequestId != null)
                request = await _context.BloodBankRequests.FindAsync(donor.RequestId.Value);

            var screenings = await _context.BloodBankScreeningRecords
                .Where(s => s.UnitId == unitId || (s.DonorId != null && s.DonorId == unit.DonorId))
                .OrderByDescending(s => s.ScreeningDateTime)
                .ToListAsync();

            var dispense = await _context.BloodBankDispenseRecords
                .Include(d => d.Request)
                .FirstOrDefaultAsync(d => d.UnitId == unitId);

            var hisPatientId = dispense?.PatientId ?? request?.PatientId ?? donor?.PatientId;
            var hisInhospid = dispense?.Inhospid ?? request?.Inhospid ?? donor?.Inhospid;
            var hisRequestId = dispense?.RequestId ?? request?.RequestId ?? donor?.RequestId;
            var hisOrderplanid = request?.Orderplanid;

            var donorProfile = donor != null
                ? await GetDonorHistoryProfileAsync(donor.DonorId, null)
                : null;

            var hisMatchesDirected = donor != null
                && !string.IsNullOrWhiteSpace(donor.PatientId)
                && !string.IsNullOrWhiteSpace(hisPatientId)
                && string.Equals(donor.PatientId.Trim(), hisPatientId.Trim(), StringComparison.OrdinalIgnoreCase);

            BloodBankPatientPanelViewModel? panel = null;
            if (!string.IsNullOrWhiteSpace(hisPatientId))
                panel = await GetPatientPanelAsync(hisPatientId, hisRequestId);

            var timeline = BuildUnitTimeline(unit, donor, screenings, dispense, request);

            return new UnitHistoryViewModel
            {
                Unit = unit,
                BloodBankUnitId = BloodBankIds.FormatUnit(unit.UnitId),
                BloodBankDonorId = BloodBankIds.FormatDonor(unit.DonorId),
                BloodBankRequestId = hisRequestId.HasValue ? BloodBankIds.FormatRequest(hisRequestId.Value) : null,
                Donor = donor,
                DonorLifetimeVisits = donorProfile?.LifetimeVisits ?? (donor != null ? 1 : 0),
                DonorSuccessfulDonations = donorProfile?.SuccessfulDonations ?? 0,
                HisPatientId = hisPatientId,
                HisInhospid = hisInhospid,
                HisRequestId = hisRequestId,
                HisOrderplanid = hisOrderplanid,
                HisPatientMatchesDirectedDonor = hisMatchesDirected,
                LinkedRequest = request,
                HisPatientPanel = panel,
                Screenings = screenings,
                Dispense = dispense,
                Timeline = timeline
            };
        }

        private static List<UnitHistoryTimelineEvent> BuildUnitTimeline(
            BloodBankBloodUnit unit,
            BloodBankDonor? donor,
            List<BloodBankScreeningRecord> screenings,
            BloodBankDispenseRecord? dispense,
            BloodBankRequest? request)
        {
            var events = new List<UnitHistoryTimelineEvent>();

            if (donor != null)
            {
                events.Add(new UnitHistoryTimelineEvent
                {
                    Kind = "donor",
                    Title = "Donor registered",
                    Subtitle = $"{donor.FirstName} {donor.LastName} · {BloodBankIds.FormatDonor(donor.DonorId)}",
                    When = donor.CreateDate,
                    ReferenceId = BloodBankIds.FormatDonor(donor.DonorId),
                    HisReference = string.IsNullOrWhiteSpace(donor.PatientId) ? null : $"HIS patient {donor.PatientId}",
                    IsComplete = true
                });
            }

            foreach (var s in screenings.OrderBy(x => x.ScreeningDateTime))
            {
                var passed = s.OverallResult == ScreeningResults.Passed;
                events.Add(new UnitHistoryTimelineEvent
                {
                    Kind = "screening",
                    Title = passed ? "Screening passed" : "Screening failed",
                    Subtitle = $"{s.StaffName} · {BloodBankIds.FormatScreening(s.ScreeningId)}",
                    When = s.ScreeningDateTime,
                    ReferenceId = BloodBankIds.FormatScreening(s.ScreeningId),
                    IsComplete = passed,
                    IsFailed = !passed
                });
            }

            events.Add(new UnitHistoryTimelineEvent
            {
                Kind = "unit",
                Title = "Blood unit recorded",
                Subtitle = $"Bag {unit.SerialNumber} · {BloodBankIds.FormatUnit(unit.UnitId)}",
                When = unit.DonationDate,
                ReferenceId = BloodBankIds.FormatUnit(unit.UnitId),
                IsComplete = true
            });

            if (request != null)
            {
                events.Add(new UnitHistoryTimelineEvent
                {
                    Kind = "his-request",
                    Title = "Linked HIS blood request",
                    Subtitle = $"{request.PatientName} · {BloodBankIds.FormatRequest(request.RequestId)}",
                    When = request.RequestDateTime,
                    ReferenceId = BloodBankIds.FormatRequest(request.RequestId),
                    HisReference = request.Orderplanid.HasValue
                        ? $"HIS order #{request.Orderplanid}"
                        : $"HIS patient {request.PatientId}",
                    IsComplete = request.Status != RequestStatuses.Pending
                });
            }

            if (unit.ModifyDate.HasValue && unit.Status is UnitStatuses.Available or UnitStatuses.Dispensed or UnitStatuses.Expired or UnitStatuses.Quarantined)
            {
                events.Add(new UnitHistoryTimelineEvent
                {
                    Kind = "status",
                    Title = $"Unit status: {unit.Status}",
                    Subtitle = unit.ScreeningResult,
                    When = unit.ModifyDate.Value,
                    IsComplete = unit.Status == UnitStatuses.Dispensed || unit.Status == UnitStatuses.Available
                });
            }

            if (dispense != null)
            {
                events.Add(new UnitHistoryTimelineEvent
                {
                    Kind = "dispense",
                    Title = "Dispensed to patient",
                    Subtitle = $"{dispense.PatientName} → {dispense.WardDestination}",
                    When = dispense.DispenseDateTime,
                    HisReference = $"HIS {dispense.PatientId} · {dispense.Inhospid}",
                    IsComplete = true
                });
            }

            return events.OrderBy(e => e.When).ToList();
        }

        public async Task ExpireOverdueUnitsAsync()
        {
            var now = DateTime.Now;
            var expired = await _context.BloodBankBloodUnits
                .Where(u => u.ExpiryDate < now && u.Status == UnitStatuses.Available)
                .ToListAsync();
            foreach (var u in expired)
                u.Status = UnitStatuses.Expired;
            if (expired.Count > 0)
                await _context.SaveChangesAsync();
        }

        public static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            return new string(phone.Where(char.IsDigit).ToArray());
        }

        public async Task<List<DonorListItemViewModel>> ListDonorsAsync()
        {
            var recent = await _context.BloodBankDonors
                .OrderByDescending(d => d.CreateDate)
                .Take(200)
                .ToListAsync();

            var snapshot = await _context.BloodBankDonors
                .AsNoTracking()
                .Select(d => new { d.Phone, d.Status, d.ScreeningResult })
                .ToListAsync();

            var donorsByPhone = snapshot
                .GroupBy(x => NormalizePhone(x.Phone))
                .Where(g => g.Key.Length > 0)
                .ToDictionary(g => g.Key, g => g.ToList());

            return recent.Select(d =>
            {
                var key = NormalizePhone(d.Phone);
                var visits = 1;
                var successful = d.Status == DonorStatuses.Passed || d.ScreeningResult == ScreeningResults.Passed ? 1 : 0;
                if (donorsByPhone.TryGetValue(key, out var group) && group.Count > 0)
                {
                    visits = group.Count;
                    successful = group.Count(x =>
                        x.Status == DonorStatuses.Passed || x.ScreeningResult == ScreeningResults.Passed);
                }

                return new DonorListItemViewModel
                {
                    DonorId = d.DonorId,
                    FullName = FormatDonorFullName(d),
                    Phone = d.Phone,
                    BloodType = d.BloodType,
                    DonationType = d.DonationType,
                    Status = d.Status,
                    RegisteredAt = d.CreateDate,
                    PatientId = d.PatientId,
                    LifetimeVisits = visits,
                    SuccessfulDonations = successful
                };
            }).ToList();
        }

        public async Task<List<DonorHistorySearchHitViewModel>> SearchDonorsMrFindAsync(
            string? donorId,
            string? phone,
            string? name,
            string? nationalId = null,
            string? bloodType = null,
            string? status = null,
            bool repeatOnly = false)
        {
            if (string.IsNullOrWhiteSpace(donorId)
                && string.IsNullOrWhiteSpace(phone)
                && string.IsNullOrWhiteSpace(name)
                && string.IsNullOrWhiteSpace(nationalId)
                && string.IsNullOrWhiteSpace(bloodType)
                && string.IsNullOrWhiteSpace(status)
                && !repeatOnly)
                return new List<DonorHistorySearchHitViewModel>();

            var q = _context.BloodBankDonors.AsQueryable();

            if (!string.IsNullOrWhiteSpace(donorId))
            {
                var term = donorId.Trim();
                if (BloodBankIds.TryParseDonorId(term, out var bbId))
                    q = q.Where(d => d.DonorId == bbId);
                else if (long.TryParse(term, out var numId))
                    q = q.Where(d => d.DonorId == numId);
                else
                    q = q.Where(d => d.DonorId.ToString().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                var p = phone.Trim();
                q = q.Where(d => d.Phone.Contains(p));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var n = name.Trim();
                q = q.Where(d => (d.FirstName + " " + (d.MiddleName ?? "") + " " + d.LastName).Contains(n));
            }

            if (!string.IsNullOrWhiteSpace(nationalId))
            {
                var nid = nationalId.Trim();
                q = q.Where(d => d.NationalId != null && d.NationalId.Contains(nid));
            }

            if (!string.IsNullOrWhiteSpace(bloodType))
            {
                var bt = bloodType.Trim().ToUpperInvariant();
                q = q.Where(d => d.BloodType.ToUpper() == bt);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim();
                q = q.Where(d => d.Status == st);
            }

            if (repeatOnly)
                q = q.Where(d => d.LastDonationDate != null);

            var matched = await q.OrderByDescending(d => d.LastDonationDate ?? d.CreateDate).Take(300).ToListAsync();
            var hits = BuildHistorySearchHits(matched, 100);
            if (repeatOnly)
                hits = hits.Where(h => h.LifetimeVisits > 1).ToList();
            return hits;
        }

        /// <summary>Quick search for external donor modal (name, phone, national ID, blood type).</summary>
        public async Task<List<ExternalDonorSearchItemViewModel>> SearchExternalDonorsAsync(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<ExternalDonorSearchItemViewModel>();

            var term = query.Trim();
            var q = _context.BloodBankDonors.AsQueryable();
            var upper = term.ToUpperInvariant();

            q = q.Where(d =>
                (d.FirstName + " " + (d.MiddleName ?? "") + " " + d.LastName).Contains(term)
                || d.Phone.Contains(term)
                || (d.NationalId != null && d.NationalId.Contains(term))
                || d.BloodType.ToUpper() == upper);

            var list = await q.OrderByDescending(d => d.LastDonationDate ?? d.CreateDate).Take(25).ToListAsync();
            return list.Select(d => new ExternalDonorSearchItemViewModel
            {
                DonorId = d.DonorId,
                BloodBankDonorId = BloodBankIds.FormatDonor(d.DonorId),
                FullName = FormatDonorFullName(d),
                BloodType = d.BloodType,
                Phone = d.Phone,
                NationalId = d.NationalId,
                IsRepeatDonor = d.LastDonationDate.HasValue,
                LastDonationDate = d.LastDonationDate,
                LifetimeVisits = 1
            }).ToList();
        }

        public async Task<BloodBankDonor?> GetDonorByNationalIdAsync(string nationalId)
        {
            if (string.IsNullOrWhiteSpace(nationalId)) return null;
            var nid = nationalId.Trim();
            return await _context.BloodBankDonors
                .FirstOrDefaultAsync(d => d.NationalId == nid);
        }

        public static (string First, string? Middle, string Last) SplitDonorName(string fullName)
        {
            var parts = (fullName ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return ("—", null, "—");
            if (parts.Length == 1) return (parts[0], null, parts[0]);
            if (parts.Length == 2) return (parts[0], null, parts[1]);
            return (parts[0], string.Join(" ", parts.Skip(1).Take(parts.Length - 2)), parts[^1]);
        }

        /// <summary>Register walk-in donor or return existing by national ID.</summary>
        public async Task<(bool Success, string Message, long? DonorId, bool IsExisting)> RegisterExternalDonorQuickAsync(
            ExternalDonorQuickRegisterViewModel model, string userId)
        {
            if (string.IsNullOrWhiteSpace(model.DonorName))
                return (false, "Donor name is required.", null, false);
            if (!BloodBankLookups.BloodTypes.Contains(model.BloodType))
                return (false, "Invalid blood type.", null, false);

            if (!string.IsNullOrWhiteSpace(model.NationalId))
            {
                var existing = await GetDonorByNationalIdAsync(model.NationalId);
                if (existing != null)
                    return (true, $"Donor already registered: {FormatDonorFullName(existing)}.", existing.DonorId, true);
            }

            var (first, middle, last) = SplitDonorName(model.DonorName);
            var phone = string.IsNullOrWhiteSpace(model.PhoneNumber) ? "—" : model.PhoneNumber.Trim();
            var gender = string.IsNullOrWhiteSpace(model.Gender) ? "Other" : model.Gender.Trim();

            var donor = new BloodBankDonor
            {
                FirstName = first,
                MiddleName = middle,
                LastName = last,
                Gender = gender,
                Phone = phone,
                BloodType = model.BloodType,
                DonationType = DonationTypes.WalkIn,
                NationalId = string.IsNullOrWhiteSpace(model.NationalId) ? null : model.NationalId.Trim(),
                Email = model.Email?.Trim(),
                Address = model.Address?.Trim(),
                DonorNotes = model.Notes?.Trim(),
                Status = DonorStatuses.Registered,
                ScreeningResult = ScreeningResults.Pending,
                CreateUser = userId,
                CreateDate = DateTime.Now
            };
            _context.BloodBankDonors.Add(donor);
            await _context.SaveChangesAsync();
            return (true, "Donor registered.", donor.DonorId, false);
        }

        public async Task<(bool Success, string Message)> AssignUnitDonorAsync(
            long unitId, long donorId, string userId)
        {
            var unit = await _context.BloodBankBloodUnits.FindAsync(unitId);
            if (unit == null) return (false, "Unit not found.");
            if (unit.Status == UnitStatuses.Dispensed)
                return (false, "Unit is already dispensed.");

            var donor = await _context.BloodBankDonors.FindAsync(donorId);
            if (donor == null) return (false, "Donor not found.");

            if (!string.Equals(unit.BloodType, donor.BloodType, StringComparison.OrdinalIgnoreCase))
                return (false, $"Blood type mismatch: unit is {unit.BloodType}, donor is {donor.BloodType}.");

            unit.DonorId = donorId;
            unit.ModifyUser = userId;
            unit.ModifyDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return (true, "Donor linked to blood unit.");
        }

        public async Task<List<DonorHistorySearchHitViewModel>> SearchDonorHistoryAsync(string? query)
        {
            var term = query?.Trim();
            if (string.IsNullOrWhiteSpace(term)) return new List<DonorHistorySearchHitViewModel>();

            List<BloodBankDonor> matched;
            var phoneTerm = NormalizePhone(term);

            if (BloodBankIds.TryParseDonorId(term, out var bbDonorId))
            {
                var anchor = await _context.BloodBankDonors.FindAsync(bbDonorId);
                if (anchor == null) return new List<DonorHistorySearchHitViewModel>();
                matched = await GetDonorsByPhoneKeyAsync(anchor.Phone);
            }
            else if (long.TryParse(term, out var donorId))
            {
                var anchor = await _context.BloodBankDonors.FindAsync(donorId);
                if (anchor == null) return new List<DonorHistorySearchHitViewModel>();
                matched = await GetDonorsByPhoneKeyAsync(anchor.Phone);
            }
            else if (phoneTerm.Length >= 4)
            {
                var candidates = await _context.BloodBankDonors
                    .Where(d => d.Phone.Contains(term))
                    .OrderByDescending(d => d.CreateDate)
                    .Take(100)
                    .ToListAsync();
                matched = candidates
                    .Where(d => NormalizePhone(d.Phone).Contains(phoneTerm, StringComparison.Ordinal))
                    .ToList();
                if (matched.Count == 0)
                    matched = candidates;
            }
            else
            {
                matched = await _context.BloodBankDonors
                    .Where(d => (d.FirstName + " " + d.LastName).Contains(term))
                    .OrderByDescending(d => d.CreateDate)
                    .Take(50)
                    .ToListAsync();
            }

            return BuildHistorySearchHits(matched);
        }

        private async Task<List<BloodBankDonor>> GetDonorsByPhoneKeyAsync(string phone)
        {
            var key = NormalizePhone(phone);
            if (string.IsNullOrEmpty(key))
                return new List<BloodBankDonor>();

            var exact = await _context.BloodBankDonors
                .Where(d => d.Phone == phone)
                .OrderByDescending(d => d.CreateDate)
                .ToListAsync();
            if (exact.Count > 0 && exact.All(d => NormalizePhone(d.Phone) == key))
                return exact;

            var all = await _context.BloodBankDonors.AsNoTracking().ToListAsync();
            return all.Where(d => NormalizePhone(d.Phone) == key)
                .OrderByDescending(d => d.CreateDate)
                .ToList();
        }

        public async Task<DonorHistoryProfileViewModel?> GetDonorHistoryProfileAsync(long? donorId, string? phone)
        {
            BloodBankDonor? anchor = null;

            if (donorId.HasValue)
                anchor = await _context.BloodBankDonors.FindAsync(donorId.Value);
            if (anchor == null && !string.IsNullOrWhiteSpace(phone))
            {
                var trimmed = phone.Trim();
                anchor = await _context.BloodBankDonors
                    .Where(d => d.Phone == trimmed)
                    .OrderByDescending(d => d.CreateDate)
                    .FirstOrDefaultAsync();
                if (anchor == null)
                {
                    var key = NormalizePhone(trimmed);
                    var candidates = await _context.BloodBankDonors
                        .Where(d => d.Phone.Contains(trimmed))
                        .OrderByDescending(d => d.CreateDate)
                        .Take(20)
                        .ToListAsync();
                    anchor = candidates.FirstOrDefault(d => NormalizePhone(d.Phone) == key)
                        ?? candidates.FirstOrDefault();
                }
            }

            if (anchor == null) return null;

            var group = await GetDonorsByPhoneKeyAsync(anchor.Phone);
            if (group.Count == 0)
                group = new List<BloodBankDonor> { anchor };

            var donorIds = group.Select(d => d.DonorId).ToList();
            var units = await _context.BloodBankBloodUnits
                .Where(u => donorIds.Contains(u.DonorId))
                .OrderByDescending(u => u.DonationDate)
                .ToListAsync();

            var screenings = await _context.BloodBankScreeningRecords
                .Where(s => s.DonorId != null && donorIds.Contains(s.DonorId.Value))
                .OrderByDescending(s => s.ScreeningDateTime)
                .ToListAsync();

            var visits = new List<DonorHistoryVisitViewModel>();
            foreach (var d in group)
            {
                var unit = units.FirstOrDefault(u => u.DonorId == d.DonorId);
                var screening = screenings.FirstOrDefault(s => s.DonorId == d.DonorId);
                visits.Add(new DonorHistoryVisitViewModel
                {
                    DonorId = d.DonorId,
                    RegisteredAt = d.CreateDate,
                    DonationType = d.DonationType,
                    Status = d.Status,
                    ScreeningResult = d.ScreeningResult,
                    PatientId = d.PatientId,
                    BagSerial = unit?.SerialNumber,
                    UnitStatus = unit?.Status,
                    ScreeningId = screening?.ScreeningId,
                    CanPrintCertificate = screening != null && screening.OverallResult == ScreeningResults.Passed
                });
            }

            var unitIds = units.Select(u => u.UnitId).ToList();
            var dispenses = unitIds.Count == 0
                ? new List<BloodBankDispenseRecord>()
                : await _context.BloodBankDispenseRecords
                    .Where(d => unitIds.Contains(d.UnitId))
                    .OrderByDescending(d => d.DispenseDateTime)
                    .ToListAsync();

            var dispenseHistory = dispenses.Select(d =>
            {
                var u = units.FirstOrDefault(x => x.UnitId == d.UnitId);
                return new DonorDispenseHistoryItemViewModel
                {
                    DispenseId = d.DispenseId,
                    SerialNumber = u?.SerialNumber ?? d.SerialNumbers,
                    RecipientName = d.PatientName,
                    WardDestination = d.WardDestination,
                    DonationSource = d.DonationSource,
                    DispenseDateTime = d.DispenseDateTime,
                    BloodType = u?.BloodType ?? ""
                };
            }).ToList();

            var latest = group[0];
            return new DonorHistoryProfileViewModel
            {
                PrimaryDonorId = latest.DonorId,
                FullName = FormatDonorFullName(latest),
                Gender = latest.Gender,
                Phone = latest.Phone,
                BloodType = latest.BloodType,
                NationalId = latest.NationalId,
                Email = latest.Email,
                LifetimeVisits = group.Count,
                SuccessfulDonations = CountSuccessfulDonations(group),
                FailedScreenings = group.Count(d => d.Status == DonorStatuses.Failed || d.ScreeningResult == ScreeningResults.Failed),
                DeferredVisits = group.Count(d => d.Status == DonorStatuses.Deferred),
                UnitsCollected = units.Count,
                UnitsInInventory = units.Count(u => u.Status == UnitStatuses.Available || u.Status == UnitStatuses.Reserved),
                FirstVisit = group.Min(d => d.CreateDate),
                LastVisit = group.Max(d => d.CreateDate),
                Visits = visits,
                DispenseHistory = dispenseHistory
            };
        }

        private static int CountSuccessfulDonations(IEnumerable<BloodBankDonor> donors) =>
            donors.Count(d =>
                d.Status == DonorStatuses.Passed
                || d.ScreeningResult == ScreeningResults.Passed);

        private static List<DonorHistorySearchHitViewModel> BuildHistorySearchHits(List<BloodBankDonor> matched, int maxResults = 25)
        {
            if (matched.Count == 0) return new List<DonorHistorySearchHitViewModel>();

            return matched
                .GroupBy(d => NormalizePhone(d.Phone).Length > 0 ? NormalizePhone(d.Phone) : $"id:{d.DonorId}")
                .Select(g =>
                {
                    var latest = g.OrderByDescending(d => d.CreateDate).First();
                    return new DonorHistorySearchHitViewModel
                    {
                        PrimaryDonorId = latest.DonorId,
                        BloodBankDonorId = BloodBankIds.FormatDonor(latest.DonorId),
                        FullName = FormatDonorFullName(latest),
                        Phone = latest.Phone,
                        NationalId = latest.NationalId,
                        Gender = latest.Gender,
                        BloodType = latest.BloodType,
                        LifetimeVisits = g.Count(),
                        SuccessfulDonations = CountSuccessfulDonations(g),
                        LastVisit = g.Max(d => d.CreateDate),
                        LastStatus = latest.Status
                    };
                })
                .OrderByDescending(h => h.LastVisit)
                .Take(maxResults)
                .ToList();
        }

        public async Task<List<HisOrderBloodBankRequestSummaryDto>> GetVisitBloodBankRequestsAsync(string inhospid, string patientId)
        {
            if (string.IsNullOrWhiteSpace(inhospid) || string.IsNullOrWhiteSpace(patientId))
                return new List<HisOrderBloodBankRequestSummaryDto>();

            var pid = patientId.Trim();
            var requests = await _context.BloodBankRequests
                .Where(r => r.Inhospid == inhospid && r.PatientId == pid)
                .OrderByDescending(r => r.RequestDateTime)
                .ToListAsync();

            if (requests.Count == 0)
                return new List<HisOrderBloodBankRequestSummaryDto>();

            var requestIds = requests.Select(r => r.RequestId).ToList();
            var dispenseStats = await _context.BloodBankDispenseRecords
                .Where(d => d.RequestId.HasValue && requestIds.Contains(d.RequestId.Value))
                .GroupBy(d => d.RequestId!.Value)
                .Select(g => new { RequestId = g.Key, Count = g.Count(), Units = g.Sum(x => x.UnitsReleased) })
                .ToDictionaryAsync(x => x.RequestId, x => x);

            return requests.Select(r =>
            {
                dispenseStats.TryGetValue(r.RequestId, out var stats);
                return new HisOrderBloodBankRequestSummaryDto
                {
                    RequestId = r.RequestId,
                    Orderplanid = r.Orderplanid,
                    Status = r.Status,
                    BloodType = r.BloodType,
                    ComponentType = r.ComponentType,
                    UnitsRequested = r.UnitsRequested,
                    UrgencyLevel = r.UrgencyLevel,
                    RequestingDoctorName = r.RequestingDoctorName,
                    RequestDateTime = r.RequestDateTime,
                    RejectReason = r.RejectReason,
                    DispenseCount = stats?.Count ?? 0,
                    UnitsReleased = stats?.Units ?? 0
                };
            }).ToList();
        }

        public async Task<HisOrderBloodBankRequestDetailsDto?> GetVisitBloodBankRequestDetailsAsync(
            long requestId, string inhospid, string patientId)
        {
            if (string.IsNullOrWhiteSpace(inhospid) || string.IsNullOrWhiteSpace(patientId))
                return null;

            var request = await _context.BloodBankRequests.FindAsync(requestId);
            if (request == null)
                return null;

            if (!string.Equals(request.Inhospid, inhospid, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(request.PatientId.Trim(), patientId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var dispenses = await _context.BloodBankDispenseRecords
                .Where(d => d.RequestId == requestId)
                .OrderByDescending(d => d.DispenseDateTime)
                .Select(d => new HisOrderBloodBankDispenseDto
                {
                    DispenseId = d.DispenseId,
                    DispenseDateTime = d.DispenseDateTime,
                    DispensingStaffName = d.DispensingStaffName,
                    CollectorName = d.CollectorName,
                    WardDestination = d.WardDestination,
                    UnitsReleased = d.UnitsReleased,
                    SerialNumbers = d.SerialNumbers
                })
                .ToListAsync();

            return new HisOrderBloodBankRequestDetailsDto
            {
                RequestId = request.RequestId,
                Orderplanid = request.Orderplanid,
                PatientId = request.PatientId,
                Inhospid = request.Inhospid,
                PatientName = request.PatientName,
                Status = request.Status,
                BloodType = request.BloodType,
                ComponentType = request.ComponentType,
                UnitsRequested = request.UnitsRequested,
                UrgencyLevel = request.UrgencyLevel,
                RequestingDoctorName = request.RequestingDoctorName,
                Ward = request.Ward,
                BedLocation = request.BedLocation,
                RequestDateTime = request.RequestDateTime,
                ModifyDate = request.ModifyDate,
                RejectReason = request.RejectReason,
                Dispenses = dispenses,
                BloodBankDetailsUrl = $"/BloodBank/Requests/Details/{request.RequestId}"
            };
        }

        private static string Truncate(string? value, int maxLength)
        {
            var text = value?.Trim() ?? "";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength);
        }
    }
}
