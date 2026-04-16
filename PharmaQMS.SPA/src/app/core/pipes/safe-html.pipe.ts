import { Pipe, PipeTransform } from "@angular/core";
import { SafeHtml } from "@angular/platform-browser";
import { SanitizationService } from "../services/sanitization.service";

/**
 * SafeHtml Pipe - Safe HTML rendering in templates
 *
 * Usage in template:
 * <div [innerHTML]="content | safeHtml"></div>
 *
 * Sanitizes HTML content to prevent XSS while allowing safe formatting
 */
@Pipe({
  name: "safeHtml",
  standalone: true,
})
export class SafeHtmlPipe implements PipeTransform {
  constructor(private sanitizer: SanitizationService) {}

  transform(html: string | null | undefined): SafeHtml {
    return this.sanitizer.sanitizeHtml(html);
  }
}
