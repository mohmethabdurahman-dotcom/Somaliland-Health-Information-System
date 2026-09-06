using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KMU.HisOrder.MVC.Areas.Radiology.Models;

namespace KMU.HisOrder.MVC.Areas.Radiology.Services
{
    /// <summary>
    /// Shared Orthanc REST/DICOMweb access aligned with radiology-interpretation.js loadViewer.
    /// </summary>
    public static class OrthancAccessHelper
    {
        private static readonly Regex OrthancInstanceIdFromUrlRegex = new(
            @"/instances/([0-9a-fA-F\-]{8,128})",
            RegexOptions.Compiled);

        public static async Task<OrthancInstanceFetchResult> FetchInstanceIdsAsync(
            HttpClient client,
            string studyInstanceUid,
            bool orthancAuthConfigured,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var diagnostics = new OrthancFetchDiagnostics();
            var encodedStudy = Uri.EscapeDataString(studyInstanceUid);

            // Step 1: DICOMweb (same URLs as loadViewer in radiology-interpretation.js).
            var dicomWeb = await FetchViaDicomWebAsync(
                client,
                encodedStudy,
                studyInstanceUid,
                diagnostics,
                logger,
                cancellationToken);

            if (dicomWeb.InstanceIds.Count > 0)
            {
                return Finalize(dicomWeb, diagnostics, orthancAuthConfigured);
            }

            // Step 2: tools/find study + native REST instance UUIDs.
            var viaRest = await FetchViaStudyRestAsync(
                client,
                studyInstanceUid,
                diagnostics,
                logger,
                cancellationToken);

            if (viaRest.InstanceIds.Count > 0)
            {
                return Finalize(
                    MergeDicomWeb(dicomWeb, viaRest),
                    diagnostics,
                    orthancAuthConfigured);
            }

            // Step 3: Resolve SOP UIDs from DICOMweb via tools/find (browser viewer path).
            var resolvedFromSops = new List<string>();
            if (dicomWeb.SopInstanceUids.Count > 0)
            {
                resolvedFromSops = await ResolveInstanceIdsFromSopsAsync(
                    client,
                    dicomWeb.SopInstanceUids,
                    diagnostics,
                    logger,
                    cancellationToken);
            }

            logger.LogInformation(
                "Orthanc fetch summary for study {StudyUid}: series={SeriesCount}, sop={SopCount}, nativeDicomWeb={NativeDicomWeb}, restStudyFound={RestStudyFound}, resolvedFromSop={ResolvedFromSop}, dicomWebHttp={DicomWebStatus}, findHttp={FindStatus}, findCount={FindCount}, restInstancesHttp={RestInstancesStatus}, firstHttp={FirstFailure}",
                studyInstanceUid,
                dicomWeb.SeriesCount,
                dicomWeb.SopInstanceUids.Count,
                dicomWeb.NativeInstanceIds.Count,
                viaRest.StudyFoundViaRest,
                resolvedFromSops.Count,
                diagnostics.DicomWebSeriesStatus,
                diagnostics.ToolsFindStudyStatus,
                diagnostics.ToolsFindStudyCount,
                diagnostics.RestInstancesStatus,
                diagnostics.FirstFailureStatus);

            return Finalize(
                new DicomWebFetchSlice
                {
                    SeriesCount = dicomWeb.SeriesCount,
                    SopInstanceUids = dicomWeb.SopInstanceUids,
                    NativeInstanceIds = dicomWeb.NativeInstanceIds,
                    InstanceIds = resolvedFromSops,
                    StudyFoundViaRest = viaRest.StudyFoundViaRest
                },
                diagnostics,
                orthancAuthConfigured);
        }

        private sealed class DicomWebFetchSlice
        {
            public int SeriesCount { get; init; }
            public List<string> SopInstanceUids { get; init; } = new();
            public List<string> NativeInstanceIds { get; init; } = new();
            public List<string> InstanceIds { get; init; } = new();
            public bool StudyFoundViaRest { get; init; }
        }

        private static OrthancInstanceFetchResult Finalize(
            DicomWebFetchSlice slice,
            OrthancFetchDiagnostics diagnostics,
            bool orthancAuthConfigured)
        {
            var ids = slice.InstanceIds.Count > 0
                ? slice.InstanceIds
                : slice.NativeInstanceIds;

            return new OrthancInstanceFetchResult
            {
                InstanceIds = ids,
                SeriesCount = slice.SeriesCount,
                SopCount = slice.SopInstanceUids.Count,
                NativeFromDicomWebCount = slice.NativeInstanceIds.Count,
                ResolvedCount = ids.Count,
                StudyFoundViaRest = slice.StudyFoundViaRest,
                DicomWebSeriesStatus = diagnostics.DicomWebSeriesStatus,
                ToolsFindStudyStatus = diagnostics.ToolsFindStudyStatus,
                ToolsFindStudyCount = diagnostics.ToolsFindStudyCount,
                RestInstancesStatus = diagnostics.RestInstancesStatus,
                FirstFailureStatus = diagnostics.FirstFailureStatus,
                OrthancAuthConfigured = orthancAuthConfigured
            };
        }

        private static DicomWebFetchSlice MergeDicomWeb(DicomWebFetchSlice dicomWeb, DicomWebFetchSlice rest)
        {
            return new DicomWebFetchSlice
            {
                SeriesCount = Math.Max(dicomWeb.SeriesCount, rest.SeriesCount),
                SopInstanceUids = dicomWeb.SopInstanceUids,
                NativeInstanceIds = dicomWeb.NativeInstanceIds,
                InstanceIds = rest.InstanceIds,
                StudyFoundViaRest = rest.StudyFoundViaRest
            };
        }

        private static async Task<DicomWebFetchSlice> FetchViaDicomWebAsync(
            HttpClient client,
            string encodedStudy,
            string studyInstanceUid,
            OrthancFetchDiagnostics diagnostics,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var sopUids = new List<string>();
            var nativeIds = new List<string>();

            using var seriesResponse = await SendDicomWebGetAsync(
                client,
                $"dicom-web/studies/{encodedStudy}/series",
                cancellationToken);
            diagnostics.DicomWebSeriesStatus = seriesResponse.StatusCode;
            if (!seriesResponse.IsSuccessStatusCode)
            {
                diagnostics.RecordFailure(seriesResponse.StatusCode);
                logger.LogWarning(
                    "DICOMweb series returned {Status} for study {StudyUid}",
                    seriesResponse.StatusCode,
                    studyInstanceUid);
                return new DicomWebFetchSlice();
            }

            var seriesList = await ReadDicomWebElementsAsync(
                await seriesResponse.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken);

            logger.LogInformation(
                "DICOMweb series count {SeriesCount} for study {StudyUid}",
                seriesList.Count,
                studyInstanceUid);

            if (seriesList.Count == 0)
            {
                return new DicomWebFetchSlice();
            }

            await CollectInstancesFromDicomWebStudyAsync(
                client,
                encodedStudy,
                sopUids,
                nativeIds,
                cancellationToken);

            if (sopUids.Count == 0 && nativeIds.Count == 0)
            {
                foreach (var series in seriesList)
                {
                    var seriesUid = TryGetDicomWebUid(series, "0020000E", "SeriesInstanceUID");
                    if (string.IsNullOrWhiteSpace(seriesUid))
                    {
                        continue;
                    }

                    using var scopedResponse = await SendDicomWebGetAsync(
                        client,
                        $"dicom-web/studies/{encodedStudy}/series/{Uri.EscapeDataString(seriesUid)}/instances",
                        cancellationToken);
                    if (!scopedResponse.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    await ParseDicomWebInstancesAsync(
                        await scopedResponse.Content.ReadAsStreamAsync(cancellationToken),
                        sopUids,
                        nativeIds,
                        cancellationToken);
                }
            }

            var distinctNative = nativeIds.Distinct(StringComparer.Ordinal).ToList();
            var distinctSop = sopUids.Distinct(StringComparer.Ordinal).ToList();

            logger.LogInformation(
                "DICOMweb for study {StudyUid}: sop={SopCount}, nativeInstanceIds={NativeCount}",
                studyInstanceUid,
                distinctSop.Count,
                distinctNative.Count);

            return new DicomWebFetchSlice
            {
                SeriesCount = seriesList.Count,
                SopInstanceUids = distinctSop,
                NativeInstanceIds = distinctNative,
                InstanceIds = distinctNative
            };
        }

        private static async Task CollectInstancesFromDicomWebStudyAsync(
            HttpClient client,
            string encodedStudy,
            List<string> sopUids,
            List<string> nativeIds,
            CancellationToken cancellationToken)
        {
            using var instancesResponse = await SendDicomWebGetAsync(
                client,
                $"dicom-web/studies/{encodedStudy}/instances",
                cancellationToken);
            if (!instancesResponse.IsSuccessStatusCode)
            {
                return;
            }

            await ParseDicomWebInstancesAsync(
                await instancesResponse.Content.ReadAsStreamAsync(cancellationToken),
                sopUids,
                nativeIds,
                cancellationToken);
        }

        private static async Task<DicomWebFetchSlice> FetchViaStudyRestAsync(
            HttpClient client,
            string studyInstanceUid,
            OrthancFetchDiagnostics diagnostics,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var (studyIds, findStatus) = await FindResourceIdsAsync(
                client,
                "Study",
                "StudyInstanceUID",
                studyInstanceUid,
                diagnostics,
                cancellationToken);

            if (studyIds.Count == 0)
            {
                var (fallbackIds, fallbackStatus) = await FindResourceIdsAsync(
                    client,
                    "Study",
                    "0020,000D",
                    studyInstanceUid,
                    diagnostics,
                    cancellationToken);
                studyIds = fallbackIds;
                if (!findStatus.HasValue && fallbackStatus.HasValue)
                {
                    findStatus = fallbackStatus;
                }
            }

            diagnostics.ToolsFindStudyStatus = findStatus;
            diagnostics.ToolsFindStudyCount = studyIds.Count;

            if (studyIds.Count == 0)
            {
                logger.LogWarning(
                    "Orthanc tools/find returned no study for StudyInstanceUID {StudyUid} (status={Status})",
                    studyInstanceUid,
                    findStatus);
                return new DicomWebFetchSlice();
            }

            var orthancStudyId = studyIds[0];
            logger.LogInformation(
                "Orthanc tools/find located study {OrthancStudyId} for StudyInstanceUID {StudyUid}",
                orthancStudyId,
                studyInstanceUid);

            var instanceIds = await GetRestInstanceIdsForStudyAsync(
                client,
                orthancStudyId,
                diagnostics,
                logger,
                cancellationToken);

            return new DicomWebFetchSlice
            {
                InstanceIds = instanceIds,
                StudyFoundViaRest = true
            };
        }

        private static async Task<List<string>> GetRestInstanceIdsForStudyAsync(
            HttpClient client,
            string orthancStudyId,
            OrthancFetchDiagnostics diagnostics,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var instanceIds = new List<string>();

            using (var instancesResponse = await client.GetAsync(
                $"studies/{Uri.EscapeDataString(orthancStudyId)}/instances",
                cancellationToken))
            {
                diagnostics.RestInstancesStatus = instancesResponse.StatusCode;
                if (instancesResponse.IsSuccessStatusCode)
                {
                    instanceIds.AddRange(await DeserializeOrthancIdListAsync(
                        await instancesResponse.Content.ReadAsStreamAsync(cancellationToken),
                        cancellationToken));
                }
                else
                {
                    diagnostics.RecordFailure(instancesResponse.StatusCode);
                    logger.LogWarning(
                        "Orthanc GET studies/{{id}}/instances failed {Status}",
                        instancesResponse.StatusCode);
                }
            }

            if (instanceIds.Count > 0)
            {
                return instanceIds;
            }

            var seriesIds = await GetSeriesIdsForStudyAsync(client, orthancStudyId, cancellationToken);
            foreach (var seriesId in seriesIds)
            {
                using var seriesInstancesResponse = await client.GetAsync(
                    $"series/{Uri.EscapeDataString(seriesId)}/instances",
                    cancellationToken);
                if (seriesInstancesResponse.IsSuccessStatusCode)
                {
                    instanceIds.AddRange(await DeserializeOrthancIdListAsync(
                        await seriesInstancesResponse.Content.ReadAsStreamAsync(cancellationToken),
                        cancellationToken));
                    continue;
                }

                using var seriesDetailResponse = await client.GetAsync(
                    $"series/{Uri.EscapeDataString(seriesId)}",
                    cancellationToken);
                if (!seriesDetailResponse.IsSuccessStatusCode)
                {
                    continue;
                }

                instanceIds.AddRange(await DeserializeOrthancIdListAsync(
                    await seriesDetailResponse.Content.ReadAsStreamAsync(cancellationToken),
                    cancellationToken,
                    "Instances"));
            }

            return instanceIds;
        }

        private static async Task<List<string>> GetSeriesIdsForStudyAsync(
            HttpClient client,
            string orthancStudyId,
            CancellationToken cancellationToken)
        {
            using var studyDetailResponse = await client.GetAsync(
                $"studies/{Uri.EscapeDataString(orthancStudyId)}",
                cancellationToken);
            if (studyDetailResponse.IsSuccessStatusCode)
            {
                var fromStudyObject = await DeserializeOrthancIdListAsync(
                    await studyDetailResponse.Content.ReadAsStreamAsync(cancellationToken),
                    cancellationToken,
                    "Series");
                if (fromStudyObject.Count > 0)
                {
                    return fromStudyObject;
                }
            }

            using var seriesResponse = await client.GetAsync(
                $"studies/{Uri.EscapeDataString(orthancStudyId)}/series",
                cancellationToken);
            if (!seriesResponse.IsSuccessStatusCode)
            {
                return new List<string>();
            }

            return await DeserializeOrthancIdListAsync(
                await seriesResponse.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken);
        }

        private static async Task<List<string>> ResolveInstanceIdsFromSopsAsync(
            HttpClient client,
            IReadOnlyList<string> sopInstanceUids,
            OrthancFetchDiagnostics diagnostics,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var orthancIds = new List<string>();
            var unresolved = 0;
            foreach (var sop in sopInstanceUids)
            {
                var id = await FindResourceIdAsync(
                    client, "Instance", "SOPInstanceUID", sop, diagnostics, cancellationToken);
                if (string.IsNullOrWhiteSpace(id))
                {
                    id = await FindResourceIdAsync(
                        client, "Instance", "0008,0018", sop, diagnostics, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(id))
                {
                    orthancIds.Add(id);
                }
                else
                {
                    unresolved++;
                }
            }

            if (unresolved > 0)
            {
                logger.LogInformation(
                    "tools/find resolved {ResolvedCount}/{TotalCount} SOPInstanceUID(s); {UnresolvedCount} unresolved",
                    orthancIds.Count,
                    sopInstanceUids.Count,
                    unresolved);
            }

            return orthancIds;
        }

        public static string BuildNoInstancesMessage(
            string studyInstanceUid,
            string orthancBaseUrl,
            OrthancInstanceFetchResult fetchResult)
        {
            if (IsUnauthorized(fetchResult))
            {
                if (!fetchResult.OrthancAuthConfigured)
                {
                    return "Orthanc returned HTTP 401 Unauthorized and OrthancSettings:HttpUsername is empty. " +
                           "Set HttpUsername and HttpPassword (or ORTHANC_HTTP_USERNAME / ORTHANC_HTTP_PASSWORD environment variables) " +
                           "to match RegisteredUsers in orthanc.json. " +
                           $"OHIF at {orthancBaseUrl}/ohif may still work via browser-cached Basic auth. " +
                           BuildDiagnosticSuffix(fetchResult);
                }

                return "Orthanc returned HTTP 401 Unauthorized. Verify OrthancSettings:HttpUsername and HttpPassword " +
                       "match your Orthanc RegisteredUsers. " +
                       BuildDiagnosticSuffix(fetchResult);
            }

            if (fetchResult.SopCount > 0 && fetchResult.ResolvedCount == 0)
            {
                return $"DICOMweb returned {fetchResult.SopCount} SOPInstanceUID(s) for study {studyInstanceUid}, but tools/find could not resolve Orthanc instance IDs. " +
                       BuildDiagnosticSuffix(fetchResult);
            }

            if (fetchResult.StudyFoundViaRest && fetchResult.InstanceIds.Count == 0)
            {
                return $"Orthanc located study {studyInstanceUid} via tools/find but returned 0 instances on REST. " +
                       BuildDiagnosticSuffix(fetchResult);
            }

            return $"No DICOM instances found in Orthanc for study {studyInstanceUid}. " +
                   $"Verify the study exists at OrthancSettings:ApiBaseUrl={orthancBaseUrl}. " +
                   BuildDiagnosticSuffix(fetchResult);
        }

        public static object BuildDiagnosticsDto(OrthancInstanceFetchResult fetchResult)
        {
            return new
            {
                seriesCount = fetchResult.SeriesCount,
                sopCount = fetchResult.SopCount,
                nativeFromDicomWeb = fetchResult.NativeFromDicomWebCount,
                resolvedCount = fetchResult.ResolvedCount,
                studyFoundViaRest = fetchResult.StudyFoundViaRest,
                dicomWebSeriesHttp = fetchResult.DicomWebSeriesStatus.HasValue
                    ? (int)fetchResult.DicomWebSeriesStatus.Value
                    : (int?)null,
                toolsFindStudyHttp = fetchResult.ToolsFindStudyStatus.HasValue
                    ? (int)fetchResult.ToolsFindStudyStatus.Value
                    : (int?)null,
                toolsFindStudyCount = fetchResult.ToolsFindStudyCount,
                restInstancesHttp = fetchResult.RestInstancesStatus.HasValue
                    ? (int)fetchResult.RestInstancesStatus.Value
                    : (int?)null,
                firstFailureHttp = fetchResult.FirstFailureStatus.HasValue
                    ? (int)fetchResult.FirstFailureStatus.Value
                    : (int?)null,
                serverAuthConfigured = fetchResult.OrthancAuthConfigured
            };
        }

        private static bool IsUnauthorized(OrthancInstanceFetchResult fetchResult)
        {
            return fetchResult.FirstFailureStatus == HttpStatusCode.Unauthorized
                || fetchResult.DicomWebSeriesStatus == HttpStatusCode.Unauthorized
                || fetchResult.ToolsFindStudyStatus == HttpStatusCode.Unauthorized
                || fetchResult.RestInstancesStatus == HttpStatusCode.Unauthorized;
        }

        private static string BuildDiagnosticSuffix(OrthancInstanceFetchResult fetchResult)
        {
            static string Code(HttpStatusCode? s) => s.HasValue ? ((int)s.Value).ToString() : "n/a";

            return $"[diag: dicomWebSeries={fetchResult.SeriesCount} (HTTP {Code(fetchResult.DicomWebSeriesStatus)}), " +
                   $"toolsFindStudy={fetchResult.ToolsFindStudyCount} (HTTP {Code(fetchResult.ToolsFindStudyStatus)}), " +
                   $"restInstancesHttp={Code(fetchResult.RestInstancesStatus)}, " +
                   $"sop={fetchResult.SopCount}, nativeDicomWeb={fetchResult.NativeFromDicomWebCount}, " +
                   $"resolved={fetchResult.ResolvedCount}, firstHttp={Code(fetchResult.FirstFailureStatus)}, " +
                   $"serverAuth={fetchResult.OrthancAuthConfigured}]";
        }

        private static async Task<HttpResponseMessage> SendDicomWebGetAsync(
            HttpClient client,
            string relativePath,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/dicom+json"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return await client.SendAsync(request, cancellationToken);
        }

        private static async Task<List<JsonElement>> ReadDicomWebElementsAsync(
            Stream stream,
            CancellationToken cancellationToken)
        {
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root.EnumerateArray().ToList();
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                return new List<JsonElement> { root };
            }

            return new List<JsonElement>();
        }

        private static async Task ParseDicomWebInstancesAsync(
            Stream stream,
            List<string> sopUids,
            List<string> nativeIds,
            CancellationToken cancellationToken)
        {
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;
            var items = root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray()
                : new[] { root }.AsEnumerable();

            foreach (var inst in items)
            {
                var sop = TryGetDicomWebUid(inst, "00080018", "SOPInstanceUID");
                if (!string.IsNullOrWhiteSpace(sop))
                {
                    sopUids.Add(sop);
                }

                var nativeId = TryExtractOrthancInstanceId(inst);
                if (!string.IsNullOrWhiteSpace(nativeId))
                {
                    nativeIds.Add(nativeId);
                }
            }
        }

        private static string TryGetDicomWebUid(JsonElement element, string tag, string propertyName)
        {
            if (element.TryGetProperty(tag, out var tagEl)
                && tagEl.TryGetProperty("Value", out var valueEl)
                && valueEl.ValueKind == JsonValueKind.Array
                && valueEl.GetArrayLength() > 0)
            {
                var uid = valueEl[0].GetString();
                if (!string.IsNullOrWhiteSpace(uid))
                {
                    return uid;
                }
            }

            if (element.TryGetProperty(propertyName, out var propEl))
            {
                return propEl.GetString() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string? TryExtractOrthancInstanceId(JsonElement element)
        {
            foreach (var value in EnumerateJsonStringValues(element))
            {
                var match = OrthancInstanceIdFromUrlRegex.Match(value);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return null;
        }

        private static IEnumerable<string> EnumerateJsonStringValues(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    var s = element.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        yield return s;
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var child in element.EnumerateArray())
                    {
                        foreach (var v in EnumerateJsonStringValues(child))
                        {
                            yield return v;
                        }
                    }
                    break;
                case JsonValueKind.Object:
                    foreach (var prop in element.EnumerateObject())
                    {
                        foreach (var v in EnumerateJsonStringValues(prop.Value))
                        {
                            yield return v;
                        }
                    }
                    break;
            }
        }

        private static async Task<string> FindResourceIdAsync(
            HttpClient client,
            string level,
            string queryTag,
            string queryValue,
            OrthancFetchDiagnostics diagnostics,
            CancellationToken cancellationToken)
        {
            var (ids, _) = await FindResourceIdsAsync(
                client, level, queryTag, queryValue, diagnostics, cancellationToken);
            return ids.Count > 0 ? ids[0] : string.Empty;
        }

        private static async Task<(List<string> Ids, HttpStatusCode? Status)> FindResourceIdsAsync(
            HttpClient client,
            string level,
            string queryTag,
            string queryValue,
            OrthancFetchDiagnostics diagnostics,
            CancellationToken cancellationToken)
        {
            var findBody = JsonSerializer.Serialize(new
            {
                Level = level,
                Query = new Dictionary<string, string>
                {
                    [queryTag] = queryValue
                }
            });

            using var findRequest = new HttpRequestMessage(HttpMethod.Post, "tools/find")
            {
                Content = new StringContent(findBody, Encoding.UTF8, "application/json")
            };
            findRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var findResponse = await client.SendAsync(findRequest, cancellationToken);
            if (!findResponse.IsSuccessStatusCode)
            {
                diagnostics.RecordFailure(findResponse.StatusCode);
                return (new List<string>(), findResponse.StatusCode);
            }

            var ids = await DeserializeOrthancIdListAsync(
                await findResponse.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken);
            return (ids, findResponse.StatusCode);
        }

        private static async Task<List<string>> DeserializeOrthancIdListAsync(
            Stream stream,
            CancellationToken cancellationToken,
            string? objectPropertyName = null)
        {
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            if (!string.IsNullOrEmpty(objectPropertyName)
                && root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(objectPropertyName, out var propertyEl))
            {
                return ExtractOrthancIdsFromElement(propertyEl);
            }

            return ExtractOrthancIdsFromElement(root);
        }

        private static List<string> ExtractOrthancIdsFromElement(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var ids = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        var id = item.GetString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            ids.Add(id);
                        }
                    }
                    else if (item.ValueKind == JsonValueKind.Object
                        && item.TryGetProperty("ID", out var idEl)
                        && idEl.ValueKind == JsonValueKind.String)
                    {
                        var id = idEl.GetString();
                        if (!string.IsNullOrWhiteSpace(id))
                        {
                            ids.Add(id);
                        }
                    }
                }

                return ids;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var single = element.GetString();
                return string.IsNullOrWhiteSpace(single) ? new List<string>() : new List<string> { single };
            }

            return new List<string>();
        }
    }
}
