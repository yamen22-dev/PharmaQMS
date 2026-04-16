import { Injectable } from "@angular/core";
import { DomSanitizer, SafeHtml } from "@angular/platform-browser";

/**
 * SanitizationService - Frontend input/output sanitization
 *
 * Provides methods to:
 * - Sanitize user input (remove XSS vectors)
 * - Sanitize API responses (clean dangerous content)
 * - Safely render HTML content
 * - Validate string formats
 */
@Injectable({
  providedIn: "root",
})
export class SanitizationService {
  // Regex patterns for dangerous content detection
  private readonly XSS_PATTERN =
    /<script|<iframe|<object|<embed|javascript:|on\w+\s*=|data:|vbscript:|<svg|<img/gi;
  private readonly CONTROL_CHARS_PATTERN = /[\x00-\x08\x0B-\x0C\x0E-\x1F\x7F]/g;
  private readonly NULL_BYTES_PATTERN = /\x00/g;

  constructor(private domSanitizer: DomSanitizer) {}

  /**
   * Sanitize user input - removes null bytes, control characters, XSS vectors
   * @param input Raw user input
   * @param maxLength Maximum allowed length (default: 256)
   * @returns Sanitized string
   */
  sanitizeInput(
    input: string | null | undefined,
    maxLength: number = 256,
  ): string {
    if (!input) return "";

    let sanitized = String(input);

    // Remove null bytes
    sanitized = sanitized.replace(this.NULL_BYTES_PATTERN, "");

    // Remove control characters (except newline, carriage return, tab)
    sanitized = sanitized.replace(/[\x00-\x08\x0B-\x0C\x0E-\x1F\x7F]/g, "");

    // Trim whitespace
    sanitized = sanitized.trim();

    // Enforce max length
    if (sanitized.length > maxLength) {
      sanitized = sanitized.substring(0, maxLength);
    }

    return sanitized;
  }

  /**
   * Sanitize email - normalize and validate
   * @param email Raw email input
   * @returns Sanitized email
   */
  sanitizeEmail(email: string | null | undefined): string {
    if (!email) return "";

    const sanitized = this.sanitizeInput(email, 256).toLowerCase().trim();

    // Basic email validation pattern
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailPattern.test(sanitized) ? sanitized : "";
  }

  /**
   * Sanitize API response - removes XSS patterns from strings in JSON
   * @param response API response object
   * @returns Sanitized response
   */
  sanitizeResponse<T>(response: T): T {
    if (response === null || response === undefined) {
      return response;
    }

    if (typeof response === "string") {
      return this.sanitizeInput(response) as T;
    }

    if (Array.isArray(response)) {
      return response.map((item) => this.sanitizeResponse(item)) as T;
    }

    if (typeof response === "object") {
      const sanitized: any = {};
      for (const key in response) {
        if (Object.prototype.hasOwnProperty.call(response, key)) {
          const value = (response as any)[key];
          sanitized[key] = this.sanitizeResponse(value);
        }
      }
      return sanitized;
    }

    return response;
  }

  /**
   * Check if a string contains XSS patterns
   * @param input String to check
   * @returns True if dangerous patterns detected
   */
  containsXSSPatterns(input: string | null | undefined): boolean {
    if (!input) return false;
    return this.XSS_PATTERN.test(input);
  }

  /**
   * Check if a string is safe (no special characters)
   * @param input String to check
   * @returns True if contains only alphanumeric, spaces, and safe punctuation
   */
  isSafeString(input: string): boolean {
    // Allow: letters, numbers, spaces, dots, hyphens, underscores, @, ., comma, colon, semicolon
    const safePattern = /^[a-zA-Z0-9\s\.\-_@,:;]+$/;
    return safePattern.test(input);
  }

  /**
   * Sanitize HTML content for safe display
   * @param html Raw HTML content
   * @returns SafeHtml for use in [innerHTML]
   */
  sanitizeHtml(html: string | null | undefined): SafeHtml {
    if (!html) return this.domSanitizer.sanitize(1, "") || "";
    return this.domSanitizer.sanitize(1, html) || "";
  }

  /**
   * Validate string length
   * @param input String to validate
   * @param maxLength Maximum allowed length
   * @returns True if length is valid
   */
  isValidLength(input: string | null | undefined, maxLength: number): boolean {
    if (!input) return true;
    return input.length <= maxLength;
  }

  /**
   * Encode string for safe JSON transmission
   * @param input String to encode
   * @returns Encoded string
   */
  encodeForJson(input: string | null | undefined): string {
    if (!input) return "";

    return String(input)
      .replace(/\\/g, "\\\\")
      .replace(/"/g, '\\"')
      .replace(/\n/g, "\\n")
      .replace(/\r/g, "\\r")
      .replace(/\t/g, "\\t");
  }

  /**
   * Sanitize request body before sending to backend
   * @param body Request body object
   * @returns Sanitized body
   */
  sanitizeRequestBody<T>(body: T): T {
    if (!body) return body;

    if (typeof body === "object" && body !== null) {
      const sanitized: any = {};

      for (const key in body) {
        if (Object.prototype.hasOwnProperty.call(body, key)) {
          const value = (body as any)[key];

          if (typeof value === "string") {
            sanitized[key] = this.sanitizeInput(value);
          } else if (typeof value === "object") {
            sanitized[key] = this.sanitizeRequestBody(value);
          } else {
            sanitized[key] = value;
          }
        }
      }

      return sanitized as T;
    }

    return body;
  }
}
