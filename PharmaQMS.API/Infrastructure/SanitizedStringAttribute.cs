using System.ComponentModel.DataAnnotations;

namespace PharmaQMS.API.Infrastructure;

/// <summary>
/// Custom validation attribute that sanitizes string input and validates it is safe.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SanitizedStringAttribute : ValidationAttribute
{
    private readonly int _maxLength;
    private readonly bool _allowHtml;

    public SanitizedStringAttribute(int maxLength = 256, bool allowHtml = false)
    {
        _maxLength = maxLength;
        _allowHtml = allowHtml;
    }

    public override bool IsValid(object? value)
    {
        if (value == null)
            return true;

        if (value is not string strValue)
            return false;

        // Check length before sanitization
        if (strValue.Length > _maxLength)
        {
            ErrorMessage = $"Field must not exceed {_maxLength} characters.";
            return false;
        }

        // Sanitize and check against original
        var sanitized = SanitizationService.SanitizeInput(strValue);

        // If allowing HTML, just ensure it's not too different after sanitization
        // Otherwise, disallow any significant changes that indicate potentially dangerous content
        if (!_allowHtml && strValue != sanitized)
        {
            // Check for common XSS patterns
            if (strValue.Contains("<") || strValue.Contains(">") || strValue.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Field contains potentially dangerous characters.";
                return false;
            }
        }

        return true;
    }
}
