import { CommonModule } from "@angular/common";
import { ChangeDetectorRef, Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, Router, RouterModule } from "@angular/router";

import { QcService } from "../../../core/services/qc.service";
import {
  QcResult,
  QcTestDetail,
  QcTestParameter,
  SubmitQcTestResultsRequest,
} from "../../../core/models/qc-test.model";

@Component({
  selector: "app-qc-detail",
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: "./qc-detail.component.html",
  styleUrls: ["./qc-detail.component.css"],
})
export class QcDetailComponent implements OnInit {
  private readonly qcService = inject(QcService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  test: QcTestDetail | null = null;
  loading = true;
  submitting = false;
  errorMessage = "";
  statusMessage = "";
  password = "";
  measuredValues: Record<number, number | null> = {};

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get("id"));
    if (!Number.isFinite(id)) {
      this.errorMessage = "QC-test kon niet worden geladen.";
      this.loading = false;
      return;
    }

    this.load(id);
  }

  cancel(): void {
    this.router.navigate(["/qc"]);
  }

  saveResults(): void {
    if (!this.test || !this.canSubmit) {
      return;
    }

    this.errorMessage = "";
    this.statusMessage = "";
    this.submitting = true;

    const payload: SubmitQcTestResultsRequest = {
      password: this.password,
      parameters: this.test.parameters.map((parameter) => ({
        parameterId: parameter.id,
        measuredValue: this.measuredValues[parameter.id] ?? 0,
      })),
    };

    this.qcService.submitQcTestResults(this.test.id, payload).subscribe({
      next: (response) => {
        this.statusMessage = response.message;
        this.test = {
          ...this.test!,
          status: response.status,
          testObjectLabel: response.testObjectLabel,
          testObject: response.testObjectLabel,
          parameters: response.parameters,
        };
        this.measuredValues = this.toMeasuredValueMap(response.parameters);
        this.submitting = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.submitting = false;
        this.errorMessage =
          error?.error?.error ??
          "Resultaten opslaan is mislukt. Controleer de invoer en probeer opnieuw.";
        this.cdr.detectChanges();
      },
    });
  }

  updateMeasuredValue(
    parameterId: number,
    value: string | number | null,
  ): void {
    if (value === "" || value === null || value === undefined) {
      this.measuredValues[parameterId] = null;
      return;
    }

    const parsedValue = typeof value === "number" ? value : Number(value);
    this.measuredValues[parameterId] = Number.isFinite(parsedValue)
      ? parsedValue
      : null;
  }

  resultLabel(parameter: QcTestParameter): string {
    const measuredValue = this.currentMeasuredValue(parameter);
    if (measuredValue == null) {
      return "Nog niet ingevuld";
    }

    return this.isWithinSpecification(parameter) ? "Geslaagd" : "OOS";
  }

  resultClass(parameter: QcTestParameter): string {
    const measuredValue = this.currentMeasuredValue(parameter);
    if (measuredValue == null) {
      return "bg-slate-100 text-slate-600";
    }

    return this.isWithinSpecification(parameter)
      ? "bg-emerald-100 text-emerald-700"
      : "bg-rose-100 text-rose-700";
  }

  get canSubmit(): boolean {
    if (!this.test || this.submitting) {
      return false;
    }

    if (!this.password.trim()) {
      return false;
    }

    return this.test.parameters.every((parameter) =>
      Number.isFinite(this.measuredValues[parameter.id] ?? NaN),
    );
  }

  get overallStatusLabel(): string {
    if (!this.test) {
      return "In behandeling";
    }

    if (!this.areAllValuesEntered()) {
      return "In behandeling";
    }

    return this.allValuesWithinSpecification() ? "Goedgekeurd" : "OOS";
  }

  get overallStatusClass(): string {
    if (!this.test) {
      return "bg-slate-100 text-slate-600";
    }

    if (!this.areAllValuesEntered()) {
      return "bg-amber-100 text-amber-800";
    }

    return this.allValuesWithinSpecification()
      ? "bg-emerald-100 text-emerald-700"
      : "bg-rose-100 text-rose-700";
  }

  get statusMessageClass(): string {
    if (this.test?.status === "OOS") {
      return "border-rose-200 bg-rose-50 text-rose-700";
    }

    return "border-emerald-200 bg-emerald-50 text-emerald-800";
  }

  private load(id: number): void {
    this.qcService.getQcTest(id).subscribe({
      next: (detail) => {
        this.test = detail;
        this.measuredValues = this.toMeasuredValueMap(detail.parameters);
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMessage = "QC-test kon niet worden geladen.";
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private toMeasuredValueMap(
    parameters: QcTestParameter[],
  ): Record<number, number | null> {
    return parameters.reduce<Record<number, number | null>>(
      (accumulator, parameter) => {
        accumulator[parameter.id] = parameter.measuredValue;
        return accumulator;
      },
      {},
    );
  }

  private currentMeasuredValue(parameter: QcTestParameter): number | null {
    return this.measuredValues[parameter.id] ?? parameter.measuredValue;
  }

  private isWithinSpecification(parameter: QcTestParameter): boolean {
    const measuredValue = this.currentMeasuredValue(parameter);
    return (
      measuredValue != null &&
      measuredValue >= parameter.min &&
      measuredValue <= parameter.max
    );
  }

  private areAllValuesEntered(): boolean {
    return (
      this.test?.parameters.every((parameter) =>
        Number.isFinite(this.currentMeasuredValue(parameter) ?? NaN),
      ) ?? false
    );
  }

  private allValuesWithinSpecification(): boolean {
    return (
      this.test?.parameters.every((parameter) =>
        this.isWithinSpecification(parameter),
      ) ?? false
    );
  }
}
