using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.Extensions;

namespace KMU.HisOrder.MVC.Areas.Radiology.Middleware
{
    public sealed class OrthancProxyMiddleware
    {
        private static readonly Regex DicomUidRegex = new("^[0-2](\\.(0|[1-9][0-9]*))+$", RegexOptions.Compiled);
        private static readonly Regex OrthancInstanceIdRegex = new("^[A-Za-z0-9\\-]{8,128}$", RegexOptions.Compiled);
        private static readonly HashSet<string> AllowedPrefixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "dicom-web/studies",
            "dicom-web/series",
            "dicom-web/instances",
            "instances",
            "tools/find"
        };

        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<OrthancProxyMiddleware> _logger;
        private readonly string _orthancApiBaseUrl;
        private readonly bool _useServerOrthancAuth;

        public OrthancProxyMiddleware(
            RequestDelegate next,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<OrthancProxyMiddleware> logger)
        {
            _next = next;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _orthancApiBaseUrl = configuration["OrthancSettings:ApiBaseUrl"]?.TrimEnd('/')
                ?? "http://localhost:8042";
            _useServerOrthancAuth = !string.IsNullOrWhiteSpace(configuration["OrthancSettings:HttpUsername"]);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/Radiology/Stream", out var remainder))
            {
                await _next(context);
                return;
            }

            if (!IsSessionValid(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Unauthorized imaging request." });
                return;
            }

            var orthancPath = remainder.Value?.TrimStart('/') ?? string.Empty;
            if (!IsAllowedOrthancPath(orthancPath))
            {
                _logger.LogWarning("Blocked invalid path: {Path}", orthancPath);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid imaging path." });
                return;
            }

            using var requestMessage = CreateProxyHttpRequest(context, orthancPath, _useServerOrthancAuth);
            var client = _httpClientFactory.CreateClient("OrthancProxy");

            try
            {
                // ✅ CRITICAL: Read full response content, not just headers
                using var responseMessage = await client.SendAsync(
                    requestMessage,
                    HttpCompletionOption.ResponseContentRead,  // ← Changed from ResponseHeadersRead
                    context.RequestAborted);

                context.Response.StatusCode = (int)responseMessage.StatusCode;

                // ✅ Copy headers BEFORE copying body
                CopyResponseHeaders(responseMessage, context.Response, orthancPath);

                // ✅ Copy response body with error handling
                await using var sourceStream = await responseMessage.Content.ReadAsStreamAsync(context.RequestAborted);
                await using var destinationStream = context.Response.Body;

                // ✅ Ensure destination is writable
                if (!context.Response.HasStarted)
                {
                    await sourceStream.CopyToAsync(destinationStream, context.RequestAborted);
                    await destinationStream.FlushAsync(context.RequestAborted);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Proxy request cancelled: {Path}", orthancPath);
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Proxy failed for path: {Path}", orthancPath);
                if (!context.Response.HasStarted)
                {
                    context.Response.StatusCode = StatusCodes.Status502BadGateway;
                    await context.Response.WriteAsJsonAsync(new { message = "Failed to stream imaging data." });
                }
            }
        }

        private static bool IsSessionValid(HttpContext context)
        {
            var isAuthenticated = context.User?.Identity?.IsAuthenticated == true;
            var sessionUser = context.Session.GetString("user_idno");
            var hasLoginDto = context.Session.TryGetValue("LoginDTO", out var loginDtoBytes)
                && loginDtoBytes is { Length: > 0 };
            return isAuthenticated && (!string.IsNullOrWhiteSpace(sessionUser) || hasLoginDto);
        }

        private static bool IsAllowedOrthancPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var normalized = Uri.UnescapeDataString(path).Replace('\\', '/').TrimStart('/');
            if (normalized.Contains("..", StringComparison.Ordinal)) return false;
            if (!AllowedPrefixes.Any(prefix => normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (normalized.StartsWith("instances/", StringComparison.OrdinalIgnoreCase))
            {
                var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || !parts[0].Equals("instances", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!OrthancInstanceIdRegex.IsMatch(parts[1]))
                    return false;

                if (parts.Length == 2)
                    return true;

                // Allow only known read-only Orthanc instance sub-resources.
                return parts[2].Equals("rendered", StringComparison.OrdinalIgnoreCase)
                    || parts[2].Equals("preview", StringComparison.OrdinalIgnoreCase)
                    || parts[2].Equals("file", StringComparison.OrdinalIgnoreCase)
                    || parts[2].Equals("frames", StringComparison.OrdinalIgnoreCase);
            }

            var tokens = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (token.Contains('.') && (token.Length > 64 || !DicomUidRegex.IsMatch(token)))
                    return false;
            }
            return true;
        }

        private HttpRequestMessage CreateProxyHttpRequest(
            HttpContext context,
            string orthancPath,
            bool useServerOrthancAuth)
        {
            var orthancUri = $"{_orthancApiBaseUrl.TrimEnd('/')}/{orthancPath}{context.Request.QueryString}";
            var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), orthancUri);

            foreach (var header in context.Request.Headers)
            {
                if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                    (useServerOrthancAuth &&
                     (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
                      header.Key.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase))))
                {
                    continue;
                }

                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())
                    && context.Request.ContentLength > 0)
                {
                    requestMessage.Content ??= new StreamContent(context.Request.Body);
                    requestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            if (context.Request.ContentLength > 0 && requestMessage.Content == null)
            {
                requestMessage.Content = new StreamContent(context.Request.Body);
            }

            requestMessage.Headers.Host = new Uri(_orthancApiBaseUrl).Authority;
            return requestMessage;
        }

        // ✅ FIXED: CopyResponseHeaders that doesn't strip Content-Length for DICOM-WEB
        private static void CopyResponseHeaders(HttpResponseMessage source, HttpResponse destination, string orthancPath)
        {
            var isDicomWebPath = orthancPath.Contains("dicom-web/", StringComparison.OrdinalIgnoreCase);
            var isRenderedPath = orthancPath.EndsWith("/rendered", StringComparison.OrdinalIgnoreCase);
            var isMetadataPath = isDicomWebPath && !isRenderedPath;

            // Forward response headers
            foreach (var header in source.Headers)
            {
                // Skip hop-by-hop headers
                if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                    header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // For DICOM-WEB, filter Content-Disposition to prevent download prompts
                if (isDicomWebPath && header.Key.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase))
                {
                    var value = string.Join("; ", header.Value);
                    if (!value.Contains("attachment", StringComparison.OrdinalIgnoreCase))
                    {
                        destination.Headers[header.Key] = header.Value.ToArray();
                    }
                }
                else if (!destination.Headers.ContainsKey(header.Key))
                {
                    destination.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // Forward content headers (CRITICAL: includes Content-Length, Content-Type)
            foreach (var header in source.Content.Headers)
            {
                // For DICOM-WEB, filter Content-Disposition
                if (isDicomWebPath && header.Key.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase))
                {
                    var value = string.Join("; ", header.Value);
                    if (!value.Contains("attachment", StringComparison.OrdinalIgnoreCase))
                    {
                        destination.Headers[header.Key] = header.Value.ToArray();
                    }
                }
                // ✅ CRITICAL: DO NOT skip Content-Length — browser needs it!
                else if (!destination.Headers.ContainsKey(header.Key))
                {
                    destination.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // Force correct Content-Type for DICOM-WEB if not already set
            if (isMetadataPath && string.IsNullOrEmpty(destination.ContentType))
            {
                destination.ContentType = "application/json";
            }
            else if (isRenderedPath && string.IsNullOrEmpty(destination.ContentType))
            {
                destination.ContentType = "image/jpeg";
            }
        }
    }
}