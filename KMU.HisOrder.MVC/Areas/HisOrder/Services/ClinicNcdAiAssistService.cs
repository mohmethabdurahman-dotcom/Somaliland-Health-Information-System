using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using KMU.HisOrder.MVC.Areas.HisOrder.Dtos;
using KMU.HisOrder.MVC.Areas.HisOrder.Models;
using KMU.HisOrder.MVC.Areas.Radiology.Models;
using Microsoft.Extensions.Options;

namespace KMU.HisOrder.MVC.Areas.HisOrder.Services
{
    public sealed class ClinicNcdAiAssistService : IClinicNcdAiAssistService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        private static readonly HashSet<string> NcdDeptCodes = new(StringComparer.Ordinal)
        {
            "3000", "3001", "3002", "6000", "6001", "6002", "6003"
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly INcdAiContextService _ncdContextService;
        private readonly INonMedAiContextService _nonMedContextService;
        private readonly PromptComposer _promptComposer;
        private readonly AIAssistOptions _options;
        private readonly ILogger<ClinicNcdAiAssistService> _logger;

        public ClinicNcdAiAssistService(
            IHttpClientFactory httpClientFactory,
            INcdAiContextService ncdContextService,
            INonMedAiContextService nonMedContextService,
            PromptComposer promptComposer,
            IOptions<AIAssistOptions> options,
            ILogger<ClinicNcdAiAssistService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _ncdContextService = ncdContextService;
            _nonMedContextService = nonMedContextService;
            _promptComposer = promptComposer;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ClinicNcdAiAssistResult> GenerateAsync(
            ClinicNcdAiAssistRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(_options.OpenAIApiKey))
            {
                throw new InvalidOperationException(
                    "OpenAI API key is not configured. Set AIAssist:OpenAIApiKey or OPENAI_API_KEY.");
            }

            var deptCode = (request.DeptCode ?? string.Empty).Trim();
            if (!NcdDeptCodes.Contains(deptCode))
            {
                throw new InvalidOperationException("NCD AI Assist is only available for NCD departments.");
            }

            var healthId = (request.HealthId ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(healthId))
            {
                throw new InvalidOperationException("Health ID is required for NCD AI Assist.");
            }

            var ncdContext = await _ncdContextService.GetLatestContextAsync(healthId, cancellationToken);
            var nonMedOrders = await ResolveNonMedOrdersAsync(request, cancellationToken);
            var runtimeContext = BuildRuntimeContext(request, ncdContext, nonMedOrders);
            var systemPrompt = _promptComposer.ComposeNcdSystemPrompt();
            var userPrompt = _promptComposer.ComposeNcdUserPrompt(runtimeContext);

            var model = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model;
            var maxTokens = Math.Clamp(_options.MaxTokens, 256, 4096);

            var requestBody = new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = maxTokens
            };

            _logger.LogInformation(
                "NCD AI Assist request: model={Model}, healthId={HealthId}, dept={Dept}, ncdEntries={NcdCount}, nonMedOrders={NonMedCount}",
                model,
                healthId,
                deptCode,
                ncdContext.Count,
                nonMedOrders.Count);

            var client = _httpClientFactory.CreateClient("OpenAI");
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody, JsonOptions),
                    Encoding.UTF8,
                    "application/json")
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.OpenAIApiKey);

            string responseText;
            try
            {
                using var response = await client.SendAsync(httpRequest, cancellationToken);
                responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "OpenAI API error {Status} for NCD AI Assist: {Body}",
                        (int)response.StatusCode,
                        Truncate(responseText, 500));
                    throw new InvalidOperationException(BuildOpenAiHttpErrorMessage(response.StatusCode, responseText));
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OpenAI HTTP request failed for NCD AI Assist");
                throw new InvalidOperationException(
                    "Cannot reach OpenAI API. Check network connectivity and firewall rules.", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "OpenAI request timed out for NCD AI Assist");
                throw new InvalidOperationException("OpenAI request timed out. Try again.", ex);
            }

            var content = ExtractAssistantContent(responseText);
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("OpenAI returned an empty response for NCD AI Assist.");
            }

            return new ClinicNcdAiAssistResult { Content = content.Trim() };
        }

        private async Task<IReadOnlyList<string>> ResolveNonMedOrdersAsync(
            ClinicNcdAiAssistRequestDto request,
            CancellationToken cancellationToken)
        {
            var clientOrders = (request.NonMedOrders ?? Array.Empty<string>())
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => o.Trim())
                .ToList();

            var inhospid = (request.Inhospid ?? string.Empty).Trim();
            var healthId = (request.HealthId ?? string.Empty).Trim();
            var dbOrders = await _nonMedContextService.GetOrderSummariesAsync(inhospid, healthId, cancellationToken);

            if (clientOrders.Count == 0)
            {
                return dbOrders;
            }

            var merged = new List<string>(clientOrders);
            var keys = new HashSet<string>(
                clientOrders.Select(NormalizeNonMedOrderKey),
                StringComparer.OrdinalIgnoreCase);

            foreach (var dbOrder in dbOrders)
            {
                if (!keys.Contains(NormalizeNonMedOrderKey(dbOrder)))
                {
                    merged.Add(dbOrder);
                }
            }

            return merged;
        }

        private static string NormalizeNonMedOrderKey(string summary)
        {
            if (string.IsNullOrWhiteSpace(summary))
            {
                return string.Empty;
            }

            var parts = summary.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                return $"{parts[0]}|{parts[1]}|{parts[2]}";
            }

            return summary.Trim();
        }

        private static string BuildRuntimeContext(
            ClinicNcdAiAssistRequestDto request,
            IReadOnlyList<NcdAiContextEntry> ncdContext,
            IReadOnlyList<string> nonMedOrders)
        {
            var sb = new StringBuilder();

            var ageText = request.PatientAge.HasValue
                ? request.PatientAge.Value.ToString()
                : "not specified";
            var sexText = string.IsNullOrWhiteSpace(request.PatientSex)
                ? "not specified"
                : request.PatientSex.Trim();

            sb.AppendLine($"Patient Age: {ageText}");
            sb.AppendLine($"Gender: {sexText}");
            sb.AppendLine();

            sb.AppendLine("Physical Signs:");
            sb.AppendLine(SerializePhysicalSigns(request.PhysicalSigns));
            sb.AppendLine();

            sb.AppendLine("ICD-10 Codes:");
            if (request.IcdCodes != null && request.IcdCodes.Count > 0)
            {
                foreach (var code in request.IcdCodes.Where(c => !string.IsNullOrWhiteSpace(c)))
                {
                    sb.AppendLine("  - " + code.Trim());
                }
            }
            sb.AppendLine();

            sb.AppendLine("Medications:");
            if (request.Medications != null && request.Medications.Count > 0)
            {
                foreach (var med in request.Medications.Where(m => !string.IsNullOrWhiteSpace(m)))
                {
                    sb.AppendLine("  - " + med.Trim());
                }
            }
            sb.AppendLine();

            sb.AppendLine("Non-medical orders (labs, imaging, materials, etc.):");
            if (nonMedOrders != null && nonMedOrders.Count > 0)
            {
                foreach (var order in nonMedOrders.Where(o => !string.IsNullOrWhiteSpace(o)))
                {
                    sb.AppendLine("  - " + order.Trim());
                }
            }
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(request.ManagementHtml))
            {
                sb.AppendLine("Management notes (care plan, raw HTML):");
                sb.AppendLine(request.ManagementHtml.Trim());
                sb.AppendLine();
            }

            if (ncdContext.Count > 0)
            {
                sb.AppendLine("NCD form data (from patient record, latest per item):");
                var byCategory = ncdContext.GroupBy(e => e.Category);
                foreach (var group in byCategory)
                {
                    sb.AppendLine($"[{group.Key}]");
                    foreach (var entry in group)
                    {
                        var datePart = entry.RecordedDate.HasValue
                            ? $" (recorded {entry.RecordedDate:yyyy-MM-dd})"
                            : string.Empty;
                        sb.AppendLine($"  - {entry.ItemName}: {entry.Answer}{datePart}");
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine("Clinical Notes (raw HTML):");
            sb.AppendLine(string.IsNullOrWhiteSpace(request.ClinicRemarkHtml)
                ? string.Empty
                : request.ClinicRemarkHtml.Trim());

            return sb.ToString().TrimEnd();
        }

        private static string SerializePhysicalSigns(object physicalSigns)
        {
            if (physicalSigns == null)
            {
                return "[]";
            }

            try
            {
                return JsonSerializer.Serialize(physicalSigns, JsonOptions);
            }
            catch
            {
                return "[]";
            }
        }

        private static string ExtractAssistantContent(string responseText)
        {
            using var doc = JsonDocument.Parse(responseText);
            if (!doc.RootElement.TryGetProperty("choices", out var choicesEl)
                || choicesEl.ValueKind != JsonValueKind.Array
                || choicesEl.GetArrayLength() == 0)
            {
                return string.Empty;
            }

            var firstChoice = choicesEl[0];
            if (!firstChoice.TryGetProperty("message", out var messageEl))
            {
                return string.Empty;
            }

            if (messageEl.TryGetProperty("content", out var contentEl))
            {
                if (contentEl.ValueKind == JsonValueKind.String)
                {
                    return contentEl.GetString() ?? string.Empty;
                }

                if (contentEl.ValueKind == JsonValueKind.Array)
                {
                    var parts = new StringBuilder();
                    foreach (var part in contentEl.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var textEl))
                        {
                            parts.Append(textEl.GetString());
                        }
                    }

                    return parts.ToString();
                }
            }

            return string.Empty;
        }

        private static string BuildOpenAiHttpErrorMessage(System.Net.HttpStatusCode status, string body)
        {
            var detail = Truncate(body, 200);
            return (int)status switch
            {
                401 => string.IsNullOrWhiteSpace(detail)
                    ? "OpenAI API key is invalid or unauthorized (401)."
                    : $"OpenAI API key is invalid or unauthorized (401): {detail}",
                429 => string.IsNullOrWhiteSpace(detail)
                    ? "OpenAI rate limit exceeded (429). Wait and try again."
                    : $"OpenAI rate limit exceeded (429): {detail}",
                _ => string.IsNullOrWhiteSpace(detail)
                    ? $"OpenAI request failed ({(int)status})."
                    : $"OpenAI request failed ({(int)status}): {detail}"
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, maxLength) + "...";
        }
    }
}
