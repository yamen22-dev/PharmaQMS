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
          // Force change detection to ensure the error message is displayed
          this.cdr.markForCheck();
          this.cdr.detectChanges();
        },
      });
  }

  private extractErrorMessage(error: unknown): string {

    if (error instanceof HttpErrorResponse) {
      // Check if error.error exists and is an object
      if (error.error && typeof error.error === "object") {
        // Try title first
        if ("title" in error.error) {
          const title = (error.error as any).title;
          return title || "An error occurred";
        }

        // Try detail
        if ("detail" in error.error) {
          const detail = (error.error as any).detail;
          return detail || "An error occurred";
        }
      }

      // Fallback: use status text or message
      if (
        error.statusText &&
        error.statusText.toLowerCase() !== "unknown error"
      ) {
        return error.statusText;
      }

      // Last resort based on status
      if (error.status === 401) {
        return "Ongeldige gebruikersnaam of wachtwoord.";
      }
    }

    return "An error occurred. Please try again.";
  }
}
