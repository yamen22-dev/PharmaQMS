import { ChangeDetectorRef, Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { Router } from "@angular/router";

import { QcService } from "../../../core/services/qc.service";
import {
  CreateQcTestParameter,
  CreateQcTestRequest,
  QcEligibleObject,
  QcEligibleObjectsResponse,
} from "../../../core/models/qc-test.model";

@Component({
  selector: "app-qc-create",
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: "./qc-create.component.html",
  styleUrl: "./qc-create.component.css",
})
export class QcCreateComponent implements OnInit {
  private readonly qcService = inject(QcService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly objectTypes: Array<"Lot" | "Batch"> = ["Lot", "Batch"];

  testObjectType: "Lot" | "Batch" = "Batch";
  selectedTestObjectId: number | null = null;
  password = "";

  parameters: CreateQcTestParameter[] = [
    { name: "", min: 0, max: 0, unit: "" },
  ];

  lotOptions: QcEligibleObject[] = [];
  batchOptions: QcEligibleObject[] = [];

  loading = true;
  submitting = false;
  errorMessage = "";

  ngOnInit(): void {
    this.qcService.getEligibleObjects().subscribe({
      next: (response: QcEligibleObjectsResponse) => {
        this.lotOptions = Array.isArray(response?.lots) ? response.lots : [];
        this.batchOptions = Array.isArray(response?.batches)
          ? response.batches
          : [];

        this.resetSelectedObject();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.errorMessage =
          "Testobjecten konden niet worden geladen. Probeer het later opnieuw.";
        this.cdr.detectChanges();
      },
    });
  }

  get testObjectOptions(): QcEligibleObject[] {
    return this.testObjectType === "Lot" ? this.lotOptions : this.batchOptions;
  }

  get canSubmit(): boolean {
    if (this.submitting || this.selectedTestObjectId == null) {
      return false;
    }

    if (!this.password.trim()) {
      return false;
    }

    if (!this.parameters.length) {
      return false;
    }

    return this.parameters.every(
      (p) =>
        p.name.trim().length > 0 &&
        p.unit.trim().length > 0 &&
        Number.isFinite(p.min) &&
        Number.isFinite(p.max) &&
        p.min < p.max,
    );
  }

  onTestObjectTypeChanged(): void {
    this.resetSelectedObject();
  }

  addParameter(): void {
    this.parameters = [
      ...this.parameters,
      { name: "", min: 0, max: 0, unit: "" },
    ];
  }

  removeParameter(index: number): void {
    if (this.parameters.length === 1) {
      return;
    }

    this.parameters = this.parameters.filter((_, i) => i !== index);
  }

  cancel(): void {
    this.router.navigate(["/qc"]);
  }

  createQcTest(): void {
    if (!this.canSubmit || this.selectedTestObjectId == null) {
      return;
    }

    this.errorMessage = "";
    this.submitting = true;

    const payload: CreateQcTestRequest = {
      testObjectType: this.testObjectType,
      testObjectId: this.selectedTestObjectId,
      parameters: this.parameters.map((p) => ({
        name: p.name.trim(),
        min: p.min,
        max: p.max,
        unit: p.unit.trim(),
      })),
      password: this.password,
    };

    this.qcService.createQcTest(payload).subscribe({
      next: () => {
        this.router.navigate(["/qc"]);
      },
      error: (error) => {
        this.submitting = false;
        this.errorMessage =
          error?.error?.error ??
          "QC-test aanmaken is mislukt. Controleer de invoer.";
        this.cdr.detectChanges();
      },
    });
  }

  private resetSelectedObject(): void {
    const firstOption = this.testObjectOptions[0];
    this.selectedTestObjectId = firstOption?.testObjectId ?? null;
  }
}
