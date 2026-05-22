import { Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule, ActivatedRoute, Router } from "@angular/router";
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  FormGroup,
} from "@angular/forms";
import { LotService } from "../../../core/services/lot.service";
import { RawMaterialService } from "../../../core/services/raw-material.service";
import { finalize } from "rxjs/internal/operators/finalize";
import { CreateLotRequest } from "../../../core/models/lot.model";
import { catchError } from "rxjs/internal/operators/catchError";
import { of } from "rxjs/internal/observable/of";
import { RawMaterialDetail } from "../../../core/models/raw-material.model";

@Component({
  selector: "app-lot-form",
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: "./lot-form.component.html",
  styleUrl: "./lot-form.component.css",
})
export class LotFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly lotService = inject(LotService);
  private readonly rawMaterialService = inject(RawMaterialService);

  /** ID of the raw material (grondstof) we are on */
  rawMaterialId!: number;

  /** UI state */
  submitting = false;
  isLoading = false; // kept for compatibility with the template
  serverError: string | null = null;
  submitSuccess: string | null = null;
  submitError: string | null = null;

  /** Reactive form */
  lotForm: FormGroup = this.fb.group({
    lotNumber: ["", [Validators.required, Validators.pattern(/^[A-Z0-9\-]+$/)]],
    supplier: ["", Validators.required], // will be pre‑filled but editable
    quantity: ["", [Validators.required, Validators.min(0.01)]],
    expiryDate: ["", Validators.required],
    receivedDate: ["", Validators.required],
    purchaseOrderNumber: ["", Validators.required],
    analysisCertificate: [""], // optional – could be a file reference later
  });

  /* ------------------------------------------------------------------ */
  /* Convenience getters for the template                                 */
  /* ------------------------------------------------------------------ */
  get f() {
    return this.lotForm.controls;
  }

  /** Returns true if a control has been touched and is invalid */
  isInvalid(field: string): boolean {
    const ctrl = this.lotForm.get(field);
    return !!(ctrl && ctrl.invalid && ctrl.touched);
  }

  ngOnInit(): void {
    // 1️⃣ Grab the raw‑material id from the URL (e.g. /raw-materials/42/detail)
    const idStr = this.route.snapshot.paramMap.get("rawMaterialId");
    this.rawMaterialId = idStr ? Number(idStr) : 0;

    // 2️⃣ Load the raw material to pre‑fill the supplier field
    if (this.rawMaterialId) {
      this.rawMaterialService.getRawMaterialById(this.rawMaterialId).subscribe({
        next: (rm: RawMaterialDetail) => {
          this.lotForm.patchValue({ supplier: rm.supplier ?? "" });
        },
        error: (err) => {
          console.warn(
            "Could not load raw material – supplier left blank",
            err,
          );
        },
      });
    }
  }

  /* ------------------------------------------------------------------ */
  /* Form submission                                                      */
  /* ------------------------------------------------------------------ */
  onSubmit(): void {
    if (this.lotForm.invalid) {
      this.lotForm.markAllAsTouched();
      return;
    }

    // Reset UI state
    this.submitting = true;
    this.isLoading = true;
    this.serverError = null;
    this.submitSuccess = null;
    this.submitError = null;
    const form = this.lotForm.getRawValue();

    // Build the DTO that will be sent to the backend
    const payload: CreateLotRequest = {
      rawMaterialId: this.rawMaterialId,
      lotNumber: form.lotNumber!,
      supplier: form.supplier!,
      quantity: Number(form.quantity!),
      expiryDateUtc: new Date(form.expiryDate!).toISOString(), // backend expects ISO string
      receivedDateUtc: new Date(form.receivedDate!).toISOString(),
      purchaseOrderNumber: form.purchaseOrderNumber!,
      analysisCertificate: form.analysisCertificate ?? null,
      // NOTE: status is NOT sent – the backend forces it to 'Quarantaine'
    };

    // Call the service – the backend will do all validation & audit logging
    this.lotService
      .createLot(this.rawMaterialId, payload)
      .pipe(
        catchError((err) => {
          // Backend error handling
          this.submitting = false;
          this.isLoading = false;

          if (err.status === 409) {
            // Duplicate lot number – message defined by the backend
            this.serverError =
              err.error?.message ||
              "Dit lotnummer bestaat al voor de geselecteerde grondstof.";
            this.submitError = "Er is een fout opgetreden. Probeer opnieuw.";
          } else if (err.status === 400 && err.error?.message) {
            // Other validation errors (e.g. expiry in the past)
            this.serverError = err.error.message;
            this.submitError = err.error.message;
          } else {
            this.submitError = "Er is een fout opgetreden. Probeer opnieuw.";
            this.serverError = "Er is een fout opgetreden. Probeer opnieuw.";
          }
          return of(null); // swallow so the observable completes
        }),
        finalize(() => {
          // Ensure flags are cleared even if something unexpected happens
          this.submitting = false;
          this.isLoading = false;
        }),
      )
      .subscribe({
        next: (createdLot) => {
          // Success – navigate to the newly created lot detail page
          this.submitSuccess = "Lot succesvol aangemaakt";

          this.router.navigate(["/raw-materials", this.rawMaterialId]);
        },
      });
  }

  /* ------------------------------------------------------------------ */
  /* Cancel navigation                                                    */
  /* ------------------------------------------------------------------ */
  onCancel(): void {
    this.router.navigate(["/raw-materials", this.rawMaterialId]);
  }
}
