using System.Text.RegularExpressions;

namespace PharmaQMS.API.Infrastructure;

/// <summary>
/// Service for sanitizing input and output data to prevent XSS, injection attacks, and other security issues.
/// Follows OWASP guidelines for data sanitization.
/// </summary>
public class SanitizationService
{
    /// <summary>
    /// Sanitizes string input by removing potentially dangerous characters and trimming whitespace.
    /// </summary>
    public static string SanitizeInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Trim whitespace
        var trimmed = input.Trim();

        // Remove null bytes
        trimmed = trimmed.Replace("\0", string.Empty);

        // Remove control characters except common whitespace
        trimmed = Regex.Replace(trimmed, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", string.Empty);

        return trimmed;
    }

    /// <summary>
    /// Sanitizes email addresses by trimming and converting to lowercase.
    /// </summary>
    public static string SanitizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var sanitized = SanitizeInput(email);
        return sanitized.ToLowerInvariant();
    }

    /// <summary>
    /// Validates that a string does not exceed a maximum length.
    /// </summary>
    public static bool IsValidLength(string input, int maxLength)
    {
        return string.IsNullOrEmpty(input) || input.Length <= maxLength;
    }

    /// <summary>
    /// Encodes strings for safe JSON output (prevents XSS in responses).
    /// </summary>
    public static string EncodeForJson(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // System.Text.Json already handles encoding by default, but this is for explicit control
        return System.Text.Json.JsonEncodedText.Encode(input).ToString();
    }

    /// <summary>
    /// Removes sensitive fields from an object for safe output.
    /// </summary>
    public static Dictionary<string, object?> RemoveSensitiveFields(Dictionary<string, object?> data, string[] sensitiveFields)
    {
        var result = new Dictionary<string, object?>(data);

        foreach (var field in sensitiveFields)
        {
            if (result.ContainsKey(field))
                result.Remove(field);
        }

        return result;
    }

    /// <summary>
    /// Validates if a string contains only safe characters (alphanumeric, spaces, and basic punctuation).
    /// </summary>
    public static bool IsSafeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        // Allow alphanumeric, spaces, and common punctuation
        return Regex.IsMatch(input, @"^[a-zA-Z0-9\s\-_.@(),]*$");
    }
}
