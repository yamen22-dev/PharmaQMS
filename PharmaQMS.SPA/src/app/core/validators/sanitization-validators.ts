import {
  AbstractControl,
  ValidationErrors,
  ValidatorFn,
  AsyncValidatorFn,
} from "@angular/forms";
import { Observable } from "rxjs";
import { inject } from "@angular/core";
import { SanitizationService } from "../services/sanitization.service";

/**
 * Custom Angular Form Validators with Sanitization
 *
 * Usage in reactive forms:
 * new FormControl('', [sanitizedString(), sanitizedEmail()])
 * new FormControl('', [sanitizedString(maxLength: 256)])
 */

/**
 * Validator: Ensure no XSS patterns in input
 * @param maxLength Maximum allowed length (default: 256)
 * @returns ValidatorFn
 */
export function sanitizedString(maxLength: number = 256): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const sanitizer = inject(SanitizationService);
    const value = control.value as string;

    // Check for XSS patterns
    if (sanitizer.containsXSSPatterns(value)) {
      return {
        xssPattern: {
          value: "Input contains potentially dangerous characters",
        },
      };
    }

    // Check length
    if (!sanitizer.isValidLength(value, maxLength)) {
      return {
        maxLength: { requiredLength: maxLength, actualLength: value.length },
      };
    }

    return null;
  };
}

/**
 * Validator: Email format with sanitization
 * @returns ValidatorFn
 */
export function sanitizedEmail(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const sanitizer = inject(SanitizationService);
    const sanitized = sanitizer.sanitizeEmail(control.value as string);

    if (!sanitized) {
      return { invalidEmail: { value: control.value } };
    }

    return null;
  };
}

/**
 * Validator: Safe string (alphanumeric + safe punctuation only)
 * @returns ValidatorFn
 */
export function safeString(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const sanitizer = inject(SanitizationService);

    if (!sanitizer.isSafeString(control.value as string)) {
      return {
        unsafeCharacters: {
          value: "Input contains special characters that are not allowed",
        },
      };
    }

    return null;
  };
}

/**
 * Validator: No control characters or null bytes
 * @returns ValidatorFn
 */
export function noControlCharacters(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const value = control.value as string;
    const controlCharPattern = /[\x00-\x1F\x7F]/g;

    if (controlCharPattern.test(value)) {
      return {
        controlCharacters: {
          value: "Input contains invalid control characters",
        },
      };
    }

    return null;
  };
}

/**
 * Validator: Minimum length with sanitization applied first
 * @param minLength Minimum length required
 * @returns ValidatorFn
 */
export function sanitizedMinLength(minLength: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const sanitizer = inject(SanitizationService);
    const sanitized = sanitizer.sanitizeInput(control.value as string);

    if (sanitized.length < minLength) {
      return {
        minLength: {
          requiredLength: minLength,
          actualLength: sanitized.length,
        },
      };
    }

    return null;
  };
}

/**
 * Validator: Maximum length with sanitization applied first
 * @param maxLength Maximum length allowed
 * @returns ValidatorFn
 */
export function sanitizedMaxLength(maxLength: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) {
      return null;
    }

    const sanitizer = inject(SanitizationService);
    const sanitized = sanitizer.sanitizeInput(
      control.value as string,
      maxLength,
    );

    if (sanitized.length > maxLength) {
      return {
        maxLength: {
          requiredLength: maxLength,
          actualLength: sanitized.length,
        },
      };
    }

    return null;
  };
}
