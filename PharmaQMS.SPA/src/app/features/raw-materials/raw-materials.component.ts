import { CommonModule } from "@angular/common";
import { Component, inject, ViewChild } from "@angular/core";
import { RawMaterialFormComponent } from "./raw-material-form/raw-material-form.component";
import { RawMaterialService } from "../../core/services/raw-material.service";
import {
  CreateRawMaterialRequest,
  RawMaterialResponse,
} from "../../core/models/raw-material.model";
import { AuthService } from "../../core/services/auth.service";
import { AuthResponse } from "../../core/models/auth-response.model";

@Component({
  selector: "app-raw-materials",
  standalone: true,
  imports: [CommonModule, RawMaterialFormComponent],
  templateUrl: "./raw-materials.component.html",
  styleUrl: "./raw-materials.component.css",
})
export class RawMaterialsComponent {
  @ViewChild(RawMaterialFormComponent) form!: RawMaterialFormComponent;

  private readonly rawMaterialService = inject(RawMaterialService);
  private readonly authService = inject(AuthService);
  private successHideTimeoutId: number | null = null;

  protected readonly session$ = this.authService.session$;
  protected readonly allowedRoles = ["QAManager", "WarehouseOperator"];

  successMessage: string | null = null;
  showSuccessAlert = false;
  showForm = false;

  canCreateRawMaterial(session: AuthResponse | null): boolean {
    return Array.isArray(session?.roles)
      ? session.roles.some((role) => this.allowedRoles.includes(role))
      : false;
  }

  openNewRawMaterialForm(): void {
    this.showForm = true;
  }

  onFormSubmit(request: CreateRawMaterialRequest): void {
    if (this.successHideTimeoutId !== null) {
      window.clearTimeout(this.successHideTimeoutId);
      this.successHideTimeoutId = null;
    }

    this.rawMaterialService.createRawMaterial(request).subscribe({
      next: (response: RawMaterialResponse) => {
        this.showSuccessAlert = true;
        this.successMessage = `Raw material "${response.name}" registered successfully!`;
        this.showForm = false;

        this.form.onSubmitSuccess(response);

        this.successHideTimeoutId = window.setTimeout(() => {
          this.showSuccessAlert = false;
          this.successMessage = null;
          this.successHideTimeoutId = null;
        }, 5000);
      },
      error: (error) => {
        this.form.onSubmitError(error);
      },
    });
  }

  closeSuccessAlert(): void {
    if (this.successHideTimeoutId !== null) {
      window.clearTimeout(this.successHideTimeoutId);
      this.successHideTimeoutId = null;
    }

    this.showSuccessAlert = false;
    this.successMessage = null;
  }
}
