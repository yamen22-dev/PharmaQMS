import { CommonModule } from "@angular/common";
import { HttpErrorResponse } from "@angular/common/http";
import { Component, ChangeDetectorRef, inject } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router } from "@angular/router";
import { finalize } from "rxjs";
import { AuthService } from "../../../core/services/auth.service";

@Component({
  selector: "app-login",
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: "./login.component.html",
  styleUrl: "./login.component.css",
})
export class LoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  errorMessage: string | null = null;
  isSubmitting = false;
  submitted = false;

  readonly form = this.formBuilder.nonNullable.group({
    email: ["", [Validators.required, Validators.email]],
    password: ["", [Validators.required]],
  });

  submit(): void {
    this.submitted = true;
    this.errorMessage = null;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.authService
      .login(this.form.getRawValue())
      .pipe(
        finalize(() => {
          this.isSubmitting = false;
        }),
      )
      .subscribe({
        next: () => {
          void this.router.navigateByUrl("/dashboard");
        },
        error: (error: unknown) => {
          console.error("Login error:", error);
          this.errorMessage = this.extractErrorMessage(error);
          console.log("Extracted error message:", this.errorMessage);
          console.log("errorMessage property:", this.errorMessage);
          // Force change detection to ensure the error message is displayed
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
      });
  }

  private extractErrorMessage(error: unknown): string {
    console.log("=== ERROR EXTRACTION START ===");
    console.log("Error:", error);
    console.log("Error type:", error?.constructor?.name);

    if (error instanceof HttpErrorResponse) {
      console.log("✓ HttpErrorResponse detected");
      console.log("Status code:", error.status);
      console.log("Status text:", error.statusText);
      console.log("Error body:", error.error);

      // Check if error.error exists and is an object
      if (error.error && typeof error.error === "object") {
        console.log("✓ error.error is an object");
        console.log("Keys in error.error:", Object.keys(error.error));

        // Try title first
        if ("title" in error.error) {
          const title = (error.error as any).title;
          console.log("✓ Found title:", title);
          return title || "An error occurred";
        }

        // Try detail
        if ("detail" in error.error) {
          const detail = (error.error as any).detail;
          console.log("✓ Found detail:", detail);
          return detail || "An error occurred";
        }
      }

      // Fallback: use status text or message
      if (
        error.statusText &&
        error.statusText.toLowerCase() !== "unknown error"
      ) {
        console.log("✓ Using statusText:", error.statusText);
        return error.statusText;
      }

      // Last resort based on status
      if (error.status === 401) {
        console.log("✓ Using default 401 message");
        return "Ongeldige gebruikersnaam of wachtwoord.";
      }
    }

    console.log("=== ERROR EXTRACTION END ===");
    return "An error occurred. Please try again.";
  }
}
