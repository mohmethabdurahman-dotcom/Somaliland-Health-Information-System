using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using KMU.HisOrder.MVC.Areas.Radiology.Models;
using Microsoft.Extensions.Options;

namespace KMU.HisOrder.MVC.Areas.Radiology.Services
{
    public sealed class AIAssistService : IAIAssistService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AIAssistOptions _options;
        private readonly string _orthancBaseUrl;
        private readonly bool _orthancAuthConfigured;
        private readonly ILogger<AIAssistService> _logger;

        public AIAssistService(
            IHttpClientFactory httpClientFactory,
            IOptions<AIAssistOptions> options,
            IConfiguration configuration,
            ILogger<AIAssistService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _orthancBaseUrl = configuration["OrthancSettings:ApiBaseUrl"]?.TrimEnd('/')
                ?? "http://localhost:8042";
            _orthancAuthConfigured = OrthancCredentialsConfigured(configuration);
            _logger = logger;

            _logger.LogInformation(
                "AIAssistService initialized: OpenAI key configured={KeyConfigured}, model={Model}, maxImages={MaxImages}, imageDetail={ImageDetail}",
                !string.IsNullOrWhiteSpace(_options.OpenAIApiKey),
                _options.Model,
                _options.MaxImages,
                NormalizeImageDetail(_options.ImageDetail));
        }

        private static bool OrthancCredentialsConfigured(IConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration["OrthancSettings:HttpUsername"]))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ORTHANC_HTTP_USERNAME"));
        }

        public async Task<AIAssistResult> GenerateReportAsync(
            string studyInstanceUid,
            string modality,
            int? patientAge,
            string patientSex,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.OpenAIApiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI API key is not configured. Set AIAssist:OpenAIApiKey in appsettings.Development.json or user secrets.");
            }

            _logger.LogInformation(
                "AI Assist starting: model={Model}, maxImages={MaxImages}, imageDetail={ImageDetail}, keyConfigured=true",
                _options.Model,
                _options.MaxImages,
                NormalizeImageDetail(_options.ImageDetail));

            var normalizedStudyUid = studyInstanceUid.Trim();
            OrthancInstanceFetchResult fetchResult;
            try
            {
                fetchResult = await FetchOrthancInstanceIdsAsync(normalizedStudyUid, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Orthanc unreachable at {BaseUrl} for study {StudyUid}", _orthancBaseUrl, normalizedStudyUid);
                throw new InvalidOperationException(
                    $"Cannot reach Orthanc at {_orthancBaseUrl}. Ensure Orthanc is running and OrthancSettings:ApiBaseUrl is correct.",
                    ex);
            }

            if (fetchResult.InstanceIds.Count == 0)
            {
                _logger.LogWarning(
                    "AI Assist found no Orthanc instances for study {StudyUid} (series={SeriesCount}, sop={SopCount}, resolved={ResolvedCount}, orthancBase={BaseUrl})",
                    normalizedStudyUid,
                    fetchResult.SeriesCount,
                    fetchResult.SopCount,
                    fetchResult.ResolvedCount,
                    _orthancBaseUrl);
                throw new AIAssistUnavailableException(
                    OrthancAccessHelper.BuildNoInstancesMessage(normalizedStudyUid, _orthancBaseUrl, fetchResult),
                    fetchResult);
            }

            var maxImages = Math.Max(1, _options.MaxImages);
            var selectedIds = fetchResult.InstanceIds.Take(maxImages).ToList();
            _logger.LogInformation(
                "AI Assist using {SelectedCount} of {TotalCount} Orthanc instances for study {StudyUid}",
                selectedIds.Count,
                fetchResult.InstanceIds.Count,
                normalizedStudyUid);

            var imageBase64List = await FetchRenderedJpegsAsync(selectedIds, cancellationToken);
            if (imageBase64List.Count == 0)
            {
                throw new AIAssistUnavailableException(
                    $"Found {fetchResult.SopCount} SOP(s) and resolved {fetchResult.ResolvedCount} Orthanc instance ID(s), but none could be rendered as JPEG. " +
                    "Verify Orthanc rendering is enabled and instances are image types.",
                    fetchResult);
            }

            _logger.LogInformation(
                "AI Assist rendered {RenderedCount}/{RequestedCount} JPEG(s) for study {StudyUid}",
                imageBase64List.Count,
                selectedIds.Count,
                normalizedStudyUid);

            var (impression, narrative) = await CallOpenAiVisionAsync(
                modality,
                patientAge,
                patientSex,
                imageBase64List,
                cancellationToken);

            return new AIAssistResult
            {
                Impression = impression,
                Narrative = narrative,
                ImagesAnalyzed = imageBase64List.Count,
                Disclaimer = _options.Disclaimer
            };
        }

        private async Task<OrthancInstanceFetchResult> FetchOrthancInstanceIdsAsync(
            string studyInstanceUid,
            CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("OrthancInternal");
            _logger.LogInformation(
                "AI Assist resolving Orthanc instances for study {StudyUid} via {BaseUrl} (serverAuth={AuthConfigured})",
                studyInstanceUid,
                _orthancBaseUrl,
                _orthancAuthConfigured);

            return await OrthancAccessHelper.FetchInstanceIdsAsync(
                client,
                studyInstanceUid,
                _orthancAuthConfigured,
                _logger,
                cancellationToken);
        }

        private async Task<List<string>> FetchRenderedJpegsAsync(
            IReadOnlyList<string> instanceIds,
            CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("OrthancInternal");
            var images = new List<string>();

            foreach (var instanceId in instanceIds)
            {
                try
                {
                    using var request = new HttpRequestMessage(
                        HttpMethod.Get,
                        $"instances/{Uri.EscapeDataString(instanceId)}/rendered");
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/jpeg"));

                    using var response = await client.SendAsync(request, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "Orthanc rendered fetch failed for instance {InstanceId}: {Status}",
                            instanceId,
                            response.StatusCode);
                        continue;
                    }

                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    if (bytes.Length > 0)
                    {
                        images.Add(Convert.ToBase64String(bytes));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch rendered image for instance {InstanceId}", instanceId);
                }
            }

            return images;
        }

        private async Task<(string Impression, string Narrative)> CallOpenAiVisionAsync(
            string modality,
            int? patientAge,
            string patientSex,
            IReadOnlyList<string> imageBase64List,
            CancellationToken cancellationToken)
        {
            var primary = await SendOpenAiVisionAttemptAsync(
                modality,
                patientAge,
                patientSex,
                imageBase64List,
                simplified: false,
                cancellationToken);

            if (primary.Success)
            {
                return (primary.Impression!, primary.Narrative!);
            }

            var retryImageCount = Math.Clamp(_options.RetryMaxImages, 1, imageBase64List.Count);
            var retryImages = imageBase64List.Take(retryImageCount).ToList();

            _logger.LogWarning(
                "OpenAI primary attempt unusable (refusal={IsRefusal}, empty={IsEmpty}); retrying with simplified prompt and {RetryCount} image(s)",
                primary.IsRefusal,
                primary.IsEmpty,
                retryImages.Count);

            var retry = await SendOpenAiVisionAttemptAsync(
                modality,
                patientAge,
                patientSex,
                retryImages,
                simplified: true,
                cancellationToken);

            if (retry.Success)
            {
                return (retry.Impression!, retry.Narrative!);
            }

            var fallbackText = CoalesceNonEmpty(primary.Refusal, primary.RawContent, retry.Refusal, retry.RawContent);
            var fallback = TryBuildRefusalFallback(fallbackText);
            if (fallback.HasValue)
            {
                _logger.LogWarning(
                    "OpenAI returned no parseable JSON; using disclaimer fallback from partial text ({Length} chars)",
                    fallbackText?.Length ?? 0);
                return fallback.Value;
            }

            throw new InvalidOperationException(BuildUserFacingRefusalMessage(primary, retry));
        }

        private async Task<OpenAiVisionAttemptResult> SendOpenAiVisionAttemptAsync(
            string modality,
            int? patientAge,
            string patientSex,
            IReadOnlyList<string> imageBase64List,
            bool simplified,
            CancellationToken cancellationToken)
        {
            var imageDetail = NormalizeImageDetail(_options.ImageDetail);
            var systemPrompt = BuildSystemPrompt(simplified);
            var userPrompt = BuildUserPrompt(modality, patientAge, patientSex, simplified);

            var contentParts = new List<object> { new { type = "text", text = userPrompt } };
            foreach (var b64 in imageBase64List)
            {
                contentParts.Add(new
                {
                    type = "image_url",
                    image_url = new { url = $"data:image/jpeg;base64,{b64}", detail = imageDetail }
                });
            }

            var maxTokens = Math.Clamp(_options.MaxTokens, 256, 4096);
            var requestBody = new
            {
                model = _options.Model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = contentParts }
                },
                max_tokens = maxTokens,
                response_format = new { type = "json_object" }
            };

            _logger.LogInformation(
                "OpenAI vision request: model={Model}, images={ImageCount}, imageDetail={ImageDetail}, simplified={Simplified}, max_tokens={MaxTokens}",
                _options.Model,
                imageBase64List.Count,
                imageDetail,
                simplified,
                maxTokens);

            var client = _httpClientFactory.CreateClient("OpenAI");
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, JsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAIApiKey);

            string responseText;
            try
            {
                using var response = await client.SendAsync(request, cancellationToken);
                responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "OpenAI API error {Status}: {Body}",
                        (int)response.StatusCode,
                        Truncate(responseText, 500));
                    throw new InvalidOperationException(
                        BuildOpenAiHttpErrorMessage(response.StatusCode, responseText));
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OpenAI HTTP request failed");
                throw new InvalidOperationException("Cannot reach OpenAI API. Check network connectivity and firewall rules.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OpenAI request timed out");
                throw new InvalidOperationException("OpenAI request timed out. Try again with fewer images or check connectivity.", ex);
            }

            return EvaluateOpenAiCompletion(responseText);
        }

        private OpenAiVisionAttemptResult EvaluateOpenAiCompletion(string responseText)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseText);
                if (!doc.RootElement.TryGetProperty("choices", out var choicesEl)
                    || choicesEl.ValueKind != JsonValueKind.Array
                    || choicesEl.GetArrayLength() == 0)
                {
                    _logger.LogError(
                        "OpenAI completion missing choices. Body: {Body}",
                        Truncate(responseText, 500));
                    return OpenAiVisionAttemptResult.Failed(empty: true);
                }

                var firstChoice = choicesEl[0];
                var finishReason = firstChoice.TryGetProperty("finish_reason", out var finishEl)
                    ? finishEl.GetString()
                    : null;

                if (!firstChoice.TryGetProperty("message", out var messageEl))
                {
                    _logger.LogError(
                        "OpenAI choice missing message (finish_reason={FinishReason}). Body: {Body}",
                        finishReason,
                        Truncate(responseText, 500));
                    return OpenAiVisionAttemptResult.Failed(empty: true);
                }

                var refusal = messageEl.TryGetProperty("refusal", out var refusalEl)
                    ? refusalEl.GetString()
                    : null;

                var messageContent = ExtractMessageContent(messageEl);
                var combinedText = CoalesceNonEmpty(refusal, messageContent);

                if (!string.IsNullOrWhiteSpace(refusal) || LooksLikeRefusal(messageContent))
                {
                    _logger.LogWarning(
                        "OpenAI refusal detected (finish_reason={FinishReason}): {Refusal}",
                        finishReason,
                        Truncate(combinedText ?? refusal ?? messageContent ?? string.Empty, 300));
                    return OpenAiVisionAttemptResult.Failed(
                        refusal: combinedText ?? refusal,
                        rawContent: messageContent,
                        isRefusal: true);
                }

                if (string.IsNullOrWhiteSpace(messageContent))
                {
                    _logger.LogError(
                        "OpenAI empty message content (finish_reason={FinishReason}). Body: {Body}",
                        finishReason,
                        Truncate(responseText, 500));
                    return OpenAiVisionAttemptResult.Failed(
                        empty: true,
                        rawContent: refusal);
                }

                var parsed = ParseAiResponse(messageContent);
                if (!string.IsNullOrWhiteSpace(parsed.Impression) || !string.IsNullOrWhiteSpace(parsed.Narrative))
                {
                    return OpenAiVisionAttemptResult.Succeeded(parsed.Impression, parsed.Narrative, messageContent);
                }

                if (LooksLikeRefusal(messageContent))
                {
                    return OpenAiVisionAttemptResult.Failed(
                        refusal: messageContent,
                        rawContent: messageContent,
                        isRefusal: true);
                }

                var partialFallback = TryBuildRefusalFallback(messageContent);
                if (partialFallback.HasValue)
                {
                    return OpenAiVisionAttemptResult.Succeeded(
                        partialFallback.Value.Impression,
                        partialFallback.Value.Narrative,
                        messageContent);
                }

                return OpenAiVisionAttemptResult.Failed(rawContent: messageContent);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse OpenAI response. Body: {Body}", Truncate(responseText, 500));
                return OpenAiVisionAttemptResult.Failed(empty: true);
            }
        }

        private static string BuildSystemPrompt(bool simplified)
        {
            if (simplified)
            {
                return "You are a radiology documentation assistant in a licensed hospital RIS. "
                    + "Attached images are de-identified PACS renders with no patient names or identifiers. "
                    + "Describe only visible anatomy and imaging findings in neutral, observational language for a radiologist to edit. "
                    + "Do not identify any person. Do not provide treatment advice. This is a draft worksheet, not a final report. "
                    + "Always respond with the JSON object requested by the user.";
            }

            return "You are a radiology report drafting assistant used in a licensed hospital RIS, similar to clinical note summarization workflows. "
                + "Images are de-identified DICOM renders from PACS (no names, medical record numbers, or burned-in identifiers). "
                + "Your role is to help a board-certified radiologist prepare preliminary DRAFT report sections only—not to diagnose, treat, or identify any person. "
                + "Use neutral, observational language; note limitations; avoid definitive diagnosis wording. "
                + "Only do what the user prompt asks. Respond with the JSON object format specified by the user.";
        }

        private static string BuildUserPrompt(
            string modality,
            int? patientAge,
            string patientSex,
            bool simplified)
        {
            var ageGroup = ToAgeGroup(patientAge);
            var sexText = string.IsNullOrWhiteSpace(patientSex) ? "not specified" : patientSex.Trim();

            const string jsonShape = "{\"impression\":\"...\",\"narrative\":\"...\"}";

            if (simplified)
            {
                return "Modality: " + modality + ". Age group: " + ageGroup + ". Sex: " + sexText + " (de-identified demographics only).\n\n"
                    + "Review the de-identified projection image(s). Draft preliminary text for licensed radiologist review:\n"
                    + "1. impression — 1-3 sentences of key visible observations (use \"may represent\" / \"suggest\" when uncertain; plain text, no HTML).\n"
                    + "2. narrative — report body in simple HTML (<p>, <ul>, <li>): technique, findings, limitations.\n\n"
                    + "Respond ONLY with valid JSON (no markdown fences):\n"
                    + jsonShape;
            }

            return "Modality: " + modality + ". Age group: " + ageGroup + ". Sex: " + sexText + " (de-identified demographics only).\n\n"
                + "Review the attached de-identified DICOM-rendered images. Produce draft preliminary report text for radiologist review:\n"
                + "1. impression — 1-3 sentences summarizing key observations (uncertainty allowed; no definitive diagnosis language; plain text, no HTML).\n"
                + "2. narrative — detailed body in simple HTML (<p>, <ul>, <li>) covering technique, findings, and limitations.\n\n"
                + "Respond ONLY with valid JSON (no markdown fences):\n"
                + jsonShape + "\n\n"
                + "If image quality limits assessment, state that clearly in both fields.";
        }

        private static string ToAgeGroup(int? patientAge)
        {
            if (!patientAge.HasValue)
            {
                return "not specified";
            }

            if (patientAge.Value < 18)
            {
                return "pediatric";
            }

            if (patientAge.Value < 65)
            {
                return "adult";
            }

            return "older adult";
        }

        private static string NormalizeImageDetail(string? detail)
        {
            var normalized = (detail ?? "low").Trim().ToLowerInvariant();
            return normalized is "low" or "high" or "auto" ? normalized : "low";
        }

        private static bool LooksLikeRefusal(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var lower = text.ToLowerInvariant();
            return lower.Contains("can't assist", StringComparison.Ordinal)
                || lower.Contains("cannot assist", StringComparison.Ordinal)
                || lower.Contains("unable to assist", StringComparison.Ordinal)
                || lower.Contains("unable to analyze", StringComparison.Ordinal)
                || lower.Contains("not able to assist", StringComparison.Ordinal)
                || (lower.Contains("i'm sorry", StringComparison.Ordinal) && lower.Contains("can't", StringComparison.Ordinal))
                || (lower.Contains("i am sorry", StringComparison.Ordinal) && lower.Contains("cannot", StringComparison.Ordinal));
        }

        private (string Impression, string Narrative)? TryBuildRefusalFallback(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var trimmed = text.Trim();
            if (!LooksLikeRefusal(trimmed))
            {
                var parsed = TryParseAsReport(trimmed);
                if (parsed.HasValue)
                {
                    return parsed;
                }
            }

            var encoded = System.Net.WebUtility.HtmlEncode(trimmed);
            var impression =
                "Preliminary AI draft could not be completed automatically; radiologist review required.";
            var narrative =
                $"<p><strong>AI assist note:</strong> {encoded}</p><p>{System.Net.WebUtility.HtmlEncode(_options.Disclaimer)}</p>";
            return (impression, narrative);
        }

        private (string Impression, string Narrative)? TryParseAsReport(string text)
        {
            var parsed = ParseAiResponse(text);
            if (!string.IsNullOrWhiteSpace(parsed.Impression) || !string.IsNullOrWhiteSpace(parsed.Narrative))
            {
                return parsed;
            }

            return null;
        }

        private static string BuildUserFacingRefusalMessage(
            OpenAiVisionAttemptResult primary,
            OpenAiVisionAttemptResult retry)
        {
            if (primary.IsRefusal || retry.IsRefusal)
            {
                return "AI Assist could not produce an automated draft for these images (content policy). "
                    + "Please review the study in the viewer and complete the report manually.";
            }

            return "AI Assist received an empty response from OpenAI. "
                + "Please review the study in the viewer and complete the report manually, or try again with fewer images.";
        }

        private static string? CoalesceNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private sealed class OpenAiVisionAttemptResult
        {
            public bool Success { get; private init; }
            public string? Impression { get; private init; }
            public string? Narrative { get; private init; }
            public string? RawContent { get; private init; }
            public string? Refusal { get; private init; }
            public bool IsRefusal { get; private init; }
            public bool IsEmpty { get; private init; }

            public static OpenAiVisionAttemptResult Succeeded(
                string impression,
                string narrative,
                string? rawContent = null) =>
                new()
                {
                    Success = true,
                    Impression = impression,
                    Narrative = narrative,
                    RawContent = rawContent
                };

            public static OpenAiVisionAttemptResult Failed(
                string? refusal = null,
                string? rawContent = null,
                bool isRefusal = false,
                bool empty = false) =>
                new()
                {
                    Success = false,
                    Refusal = refusal,
                    RawContent = rawContent,
                    IsRefusal = isRefusal,
                    IsEmpty = empty
                };
        }

        private static string? ExtractMessageContent(JsonElement messageEl)
        {
            if (!messageEl.TryGetProperty("content", out var contentEl))
            {
                return null;
            }

            if (contentEl.ValueKind == JsonValueKind.String)
            {
                return contentEl.GetString();
            }

            if (contentEl.ValueKind == JsonValueKind.Array)
            {
                var parts = new StringBuilder();
                foreach (var part in contentEl.EnumerateArray())
                {
                    if (!part.TryGetProperty("type", out var typeEl))
                    {
                        continue;
                    }

                    var type = typeEl.GetString();
                    if (type == "text" && part.TryGetProperty("text", out var textEl))
                    {
                        parts.Append(textEl.GetString());
                    }
                    else if (type == "output_text" && part.TryGetProperty("text", out var outputTextEl))
                    {
                        parts.Append(outputTextEl.GetString());
                    }
                }

                return parts.Length > 0 ? parts.ToString() : null;
            }

            return null;
        }

        private static string BuildOpenAiHttpErrorMessage(System.Net.HttpStatusCode statusCode, string responseText)
        {
            var status = (int)statusCode;
            var detail = TryExtractOpenAiErrorMessage(responseText);

            return statusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized =>
                    string.IsNullOrWhiteSpace(detail)
                        ? "OpenAI API key is invalid or unauthorized (401). Check AIAssist:OpenAIApiKey."
                        : $"OpenAI API key is invalid or unauthorized (401): {detail}",
                System.Net.HttpStatusCode.TooManyRequests =>
                    string.IsNullOrWhiteSpace(detail)
                        ? "OpenAI rate limit exceeded (429). Wait and try again."
                        : $"OpenAI rate limit exceeded (429): {detail}",
                _ =>
                    string.IsNullOrWhiteSpace(detail)
                        ? $"OpenAI request failed ({status})."
                        : $"OpenAI request failed ({status}): {detail}"
            };
        }

        private static string TryExtractOpenAiErrorMessage(string responseText)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseText);
                if (doc.RootElement.TryGetProperty("error", out var errorEl)
                    && errorEl.TryGetProperty("message", out var messageEl))
                {
                    return messageEl.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                // ignore
            }

            return string.Empty;
        }

        private static (string Impression, string Narrative) ParseAiResponse(string rawContent)
        {
            var trimmed = rawContent.Trim();

            if (trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                trimmed = Regex.Replace(trimmed, "^```(?:json)?\\s*", "", RegexOptions.IgnoreCase);
                trimmed = Regex.Replace(trimmed, "\\s*```$", "");
                trimmed = trimmed.Trim();
            }

            var jsonCandidate = ExtractJsonObject(trimmed) ?? trimmed;

            try
            {
                using var json = JsonDocument.Parse(jsonCandidate);
                var root = json.RootElement;
                var impression = root.TryGetProperty("impression", out var impEl)
                    ? impEl.GetString() ?? string.Empty
                    : string.Empty;
                var narrative = root.TryGetProperty("narrative", out var narEl)
                    ? narEl.GetString() ?? string.Empty
                    : string.Empty;

                if (!string.IsNullOrWhiteSpace(impression) || !string.IsNullOrWhiteSpace(narrative))
                {
                    return (impression.Trim(), narrative.Trim());
                }
            }
            catch (JsonException)
            {
                // fall through to plain-text handling
            }

            return ParsePlainTextFallback(trimmed);
        }

        private static string? ExtractJsonObject(string text)
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }

            return text[start..(end + 1)];
        }

        private static (string Impression, string Narrative) ParsePlainTextFallback(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return (string.Empty, string.Empty);
            }

            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var impression = lines.Length > 0 ? lines[0] : text;
            var narrativeBody = lines.Length > 1
                ? string.Join(Environment.NewLine, lines.Skip(1))
                : text;
            var narrative = $"<p>{System.Net.WebUtility.HtmlEncode(narrativeBody).Replace("\n", "</p><p>", StringComparison.Ordinal)}</p>";
            return (impression.Trim(), narrative);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value[..maxLength] + "...";
        }
    }
}
