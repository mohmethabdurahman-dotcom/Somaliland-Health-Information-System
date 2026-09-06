using Renci.SshNet;
using KMU.HisOrder.MVC.Models;
using Microsoft.EntityFrameworkCore;
using FellowOakDicom; // Updated for fo-dicom
using FellowOakDicom.IO;

namespace KMU.HisOrder.MVC.Areas.Radiology.Services
{
    public class OrthancWorklistService
    {
        private readonly IConfiguration _configuration;
        private readonly KMUContext _context;
        private readonly ILogger<OrthancWorklistService> _logger;
        private readonly string _localWorklistPath;
        private readonly bool _enabled;
        private readonly bool _deleteLocalAfterUpload;
        private readonly bool _createLocalOnly;

        public OrthancWorklistService(
            IConfiguration configuration,
            KMUContext context,
            ILogger<OrthancWorklistService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;

            _localWorklistPath = _configuration["OrthancSettings:WorklistLocalPath"] ?? @"/var/orthanc/worklists";
            _localWorklistPath = Path.GetFullPath(_localWorklistPath);
            _enabled = _configuration.GetValue<bool>("OrthancSettings:Enabled", true);
            _deleteLocalAfterUpload = _configuration.GetValue<bool>("OrthancSettings:DeleteLocalAfterUpload", false);

            // New flag: when true, generate local .wl only and do not attempt SFTP upload.
            _createLocalOnly = _configuration.GetValue<bool>("OrthancSettings:CreateLocalWorklistOnly", false);

            // Always ensure the local worklist folder exists (create if missing).
            try
            {
                if (!Directory.Exists(_localWorklistPath))
                {
                    Directory.CreateDirectory(_localWorklistPath);
                    _logger.LogInformation("Created Orthanc local worklist folder: {WorklistPath}", _localWorklistPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create or access local worklist folder: {WorklistPath}", _localWorklistPath);
                // swallow or rethrow depending on desired behavior; keep service usable.
            }

            _logger.LogInformation("Orthanc local worklist path: {WorklistPath}", _localWorklistPath);
            _logger.LogInformation("Delete local .wl after upload: {DeleteLocalAfterUpload}", _deleteLocalAfterUpload);
            _logger.LogInformation("Create local worklist only: {CreateLocalOnly}", _createLocalOnly);
        }

        /// <summary>
        /// Creates a native DICOM .wl file from the exam request data
        /// </summary>
        public async Task<string> GenerateWorklistAsync(radiologyexamrequest request)
        {
            try
            {
                if (!_enabled && !_createLocalOnly) return null;

                var patient = await _context.KmuCharts.FirstOrDefaultAsync(p => p.ChrHealthId == request.patientid);
                var room = await _context.rooms.FirstOrDefaultAsync(r => r.roomid == request.roomid);
                var bodyPart = await _context.KmuNonMedicines.Where(nm => nm.ItemId == request.item_id).Select(nm => nm.ItemName).FirstOrDefaultAsync();

                if (patient == null || room == null) return null;

                // 1. Initialize Dataset with mandatory Worklist UIDs
                var dataset = new DicomDataset(DicomTransferSyntax.ExplicitVRLittleEndian);
                dataset.Add(DicomTag.SOPClassUID, DicomUID.ModalityWorklistInformationModelFind);
                dataset.Add(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID());

                // 2. Patient Identification
                dataset.Add(DicomTag.PatientName, $"{patient.ChrPatientLastname}^{patient.ChrPatientFirstname}");
                dataset.Add(DicomTag.PatientID, request.patientid);
                dataset.Add(DicomTag.PatientBirthDate, patient.ChrBirthDate?.ToString("yyyyMMdd") ?? "");
                dataset.Add(DicomTag.PatientSex, patient.ChrSex ?? "O");

                // 3. Procedure / Study Info
                dataset.Add(DicomTag.AccessionNumber, request.accessionnumber);
                dataset.Add(DicomTag.StudyInstanceUID, request.studyinstanceuid);
                dataset.Add(DicomTag.RequestedProcedureDescription, bodyPart ?? "Radiology Exam");
                dataset.Add(DicomTag.RequestedProcedureID, request.id.ToString());

                // 4. Scheduled Procedure Step Sequence (Crucial for Modalities)
                var spsStep = new DicomDataset();
                spsStep.Add(DicomTag.Modality, room.modality);
                spsStep.Add(DicomTag.ScheduledStationAETitle, $"{room.modality}_ROOM_{room.room_number}");
                spsStep.Add(DicomTag.ScheduledProcedureStepStartDate, request.ordereddate?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd"));
                spsStep.Add(DicomTag.ScheduledProcedureStepStartTime, request.createdat.ToString("HHmmss"));
                spsStep.Add(DicomTag.ScheduledPerformingPhysicianName, request.requestedby ?? "RADIOLOGY");
                spsStep.Add(DicomTag.ScheduledProcedureStepDescription, bodyPart ?? "Radiology Exam");
                spsStep.Add(DicomTag.ScheduledProcedureStepID, request.seq_no?.ToString() ?? "1");

                dataset.Add(new DicomSequence(DicomTag.ScheduledProcedureStepSequence, spsStep));

                // 5. Save locally as .wl
                string fileName = $"{request.orderid}_{request.id}.wl";
                string localFilePath = Path.Combine(_localWorklistPath, fileName);

                var dicomFile = new DicomFile(dataset);
                await dicomFile.SaveAsync(localFilePath);

                _logger.LogInformation($"Native DICOM Worklist generated: {fileName}");
                return localFilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating DICOM worklist for ID: {request.id}");
                throw;
            }
        }

        /// <summary>
        /// Uploads the binary .wl file to the Linux Orthanc server
        /// </summary>
        public async Task<bool> UploadWorklistAsync(string localFilePath)
        {
            try
            {
                if (!_enabled || !File.Exists(localFilePath)) return false;

                var host = _configuration["OrthancSettings:SftpHost"];
                var port = _configuration.GetValue<int>("OrthancSettings:SftpPort", 22);
                var username = _configuration["OrthancSettings:SftpUsername"];
                var password = _configuration["OrthancSettings:SftpPassword"];
                var remotePath = _configuration["OrthancSettings:WorklistRemotePath"];

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username)) return false;

                // Ensure remote path ends with slash
                string normalizedRemotePath = remotePath.EndsWith("/") ? remotePath : remotePath + "/";

                await Task.Run(() =>
                {
                    using (var sftp = new SftpClient(host, port, username, password))
                    {
                        sftp.Connect();
                        using (var fileStream = File.OpenRead(localFilePath))
                        {
                            string remoteFilePath = normalizedRemotePath + Path.GetFileName(localFilePath);
                            sftp.UploadFile(fileStream, remoteFilePath, true);
                        }
                        sftp.Disconnect();
                    }
                });

                _logger.LogInformation($"Worklist successfully uploaded: {Path.GetFileName(localFilePath)}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SFTP Upload failed for: {localFilePath}");
                return false;
            }
        }

        public async Task<bool> GenerateAndUploadWorklistAsync(radiologyexamrequest request)
        {
            try
            {
                var localFilePath = await GenerateWorklistAsync(request);
                if (string.IsNullOrEmpty(localFilePath)) return false;

                // If configured to only create the local worklist, skip upload step.
                if (_createLocalOnly)
                {
                    _logger.LogInformation("CreateLocalWorklistOnly flag set - skipping upload for {File}", localFilePath);
                    return true;
                }

                bool success = await UploadWorklistAsync(localFilePath);

                // Optional cleanup: keep local .wl files by default for audit/troubleshooting
                if (success && _deleteLocalAfterUpload && File.Exists(localFilePath))
                {
                    File.Delete(localFilePath);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in process for request ID: {request.id}");
                return false;
            }
        }

        public async Task<bool> DeleteWorklistAsync(string fileName)
        {
            try
            {
                if (!_enabled) return false;

                var host = _configuration["OrthancSettings:SftpHost"];
                var port = _configuration.GetValue<int>("OrthancSettings:SftpPort", 22);
                var username = _configuration["OrthancSettings:SftpUsername"];
                var password = _configuration["OrthancSettings:SftpPassword"];
                var remotePath = _configuration["OrthancSettings:WorklistRemotePath"];

                string normalizedRemotePath = remotePath.EndsWith("/") ? remotePath : remotePath + "/";

                await Task.Run(() =>
                {
                    using (var sftp = new SftpClient(host, port, username, password))
                    {
                        sftp.Connect();
                        string remoteFilePath = normalizedRemotePath + fileName;
                        if (sftp.Exists(remoteFilePath))
                        {
                            sftp.DeleteFile(remoteFilePath);
                        }
                        sftp.Disconnect();
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting worklist file: {fileName}");
                return false;
            }
        }
    }
}