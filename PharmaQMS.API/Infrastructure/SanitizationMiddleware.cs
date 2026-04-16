using System.Text;
using System.Text.Json;

namespace PharmaQMS.API.Infrastructure;

/// <summary>
/// Middleware that sanitizes HTTP request and response bodies.
/// Prevents XSS and injection attacks at the HTTP level.
/// </summary>
public class SanitizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SanitizationMiddleware> _logger;

    public SanitizationMiddleware(RequestDelegate next, ILogger<SanitizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log incoming request info
        _logger.LogDebug("Sanitization middleware processing request: {Method} {Path}",
            context.Request.Method, context.Request.Path);

        // Store original body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Only process POST, PUT, PATCH requests with JSON content
            if (ShouldSanitizeRequest(context.Request))
            {
                context.Request.EnableBuffering();

                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        try
                        {
                            // Parse JSON and sanitize
                            using (var doc = JsonDocument.Parse(body))
                            {
                                var sanitized = SanitizeJsonDocument(doc.RootElement);
                                var sanitizedBody = JsonSerializer.Serialize(sanitized);

                                // Replace body with sanitized version
                                var sanitizedBytes = Encoding.UTF8.GetBytes(sanitizedBody);
                                context.Request.Body = new MemoryStream(sanitizedBytes);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning("Failed to parse request JSON: {Message}", ex.Message);
                            context.Request.Body.Position = 0;
                        }
                    }

                    // Reset position for next middleware
                    context.Request.Body.Position = 0;
                }
            }

            // Use a response wrapper to capture and sanitize response
            using (var memoryStream = new MemoryStream())
            {
                context.Response.Body = memoryStream;

                await _next(context);

                // Sanitize response if it's JSON
                if (ShouldSanitizeResponse(context.Response))
                {
                    memoryStream.Position = 0;
                    using (var reader = new StreamReader(memoryStream))
                    {
                        var response = await reader.ReadToEndAsync();

                        if (!string.IsNullOrWhiteSpace(response))
                        {
                            try
                            {
                                using (var doc = JsonDocument.Parse(response))
                                {
                                    var sanitized = SanitizeJsonDocument(doc.RootElement);
                                    var sanitizedResponse = JsonSerializer.Serialize(sanitized);
                                    var sanitizedBytes = Encoding.UTF8.GetBytes(sanitizedResponse);

                                    await originalBodyStream.WriteAsync(sanitizedBytes, 0, sanitizedBytes.Length);
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogWarning("Failed to parse response JSON: {Message}", ex.Message);
                                memoryStream.Position = 0;
                                await memoryStream.CopyToAsync(originalBodyStream);
                            }
                        }
                    }
                }
                else
                {
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBodyStream);
                }
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool ShouldSanitizeRequest(HttpRequest request)
    {
        return request.Method switch
        {
            "POST" or "PUT" or "PATCH" => request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false,
            _ => false
        };
    }

    private static bool ShouldSanitizeResponse(HttpResponse response)
    {
        return response.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    private static Dictionary<string, object?> SanitizeJsonDocument(JsonElement element)
    {
        var result = new Dictionary<string, object?>();

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                result[prop.Name] = SanitizeJsonValue(prop.Value);
            }
        }

        return result;
    }

    private static object? SanitizeJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => SanitizationService.SanitizeInput(element.GetString() ?? string.Empty),
            JsonValueKind.Number => element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray()
                .Select(SanitizeJsonValue)
                .ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => SanitizeJsonValue(p.Value)),
            _ => null
        };
    }
}

/// <summary>
/// Extension method to register sanitization middleware.
/// </summary>
public static class SanitizationMiddlewareExtensions
{
    public static IApplicationBuilder UseSanitization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SanitizationMiddleware>();
    }
}
