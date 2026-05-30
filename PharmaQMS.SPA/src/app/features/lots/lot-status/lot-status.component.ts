// src/app/features/lots/lot-status/lot-status.component.ts

import { Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule, ActivatedRoute, Router } from "@angular/router";
import { ReactiveFormsModule, FormBuilder, Validators } from "@angular/forms";
import { LotService } from "../../../core/services/lot.service";
import { LotDetail, LotStatus } from "../../../core/models/lot.model";

@Component({
  selector: "app-lot-status",
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: "./lot-status.component.html",
  styleUrl: "./lot-status.component.css",
})
export class LotStatusComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly lotService = inject(LotService);
  private readonly fb = inject(FormBuilder);

  lot: LotDetail | null = null;
  loading = true;
  submitting = false;
  serverError: string | null = null;

  lotNumber = "";
  allowedTransitions: { value: LotStatus; label: string }[] = [];

  form = this.fb.group({
    newStatus: this.fb.control<LotStatus | "">("", {
      nonNullable: true,
      validators: [Validators.required],
    }),
    reason: this.fb.control("", {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(10)],
    }),
    password: this.fb.control("", {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get("id");
    const id = Number(idParam);

    if (!idParam || Number.isNaN(id)) {
      this.loading = false;
      this.serverError = "Invalid lot id.";
      return;
    }

    this.lotService.getLotById(id).subscribe({
      next: (data) => {
        this.lot = data;
        this.allowedTransitions = this.getAllowedTransitions(data.status);
        this.lotNumber = data.lotNumber;
        const firstTransition = this.allowedTransitions[0]?.value ?? "";
        this.form.patchValue({
          newStatus: firstTransition,
        });

        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.serverError = "Lot not found.";
      },
    });
  }

  private getAllowedTransitions(
    status: LotStatus,
  ): { value: LotStatus; label: string }[] {
    if (status !== "Quarantine") return [];
    return [
      { value: "Released", label: "Released" },
      { value: "Rejected", label: "Rejected" },
    ];
  }

  onSubmit(): void {
    if (!this.lot) {
      this.serverError = "Lot not loaded.";
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    if (!raw.newStatus) {
      this.form.get("newStatus")?.markAsTouched();
      return;
    }

    this.submitting = true;
    this.serverError = null;

    this.lotService
      .changeLotStatus(this.lot.id, {
        newStatus: raw.newStatus,
        reason: raw.reason,
        password: raw.password,
      })
      .subscribe({
        next: () => {
          alert("Status updated successfully. You will be returned to lot details.");
          this.router.navigate(["/lots", this.lot!.id]);
        },
        error: (err) => {
          this.submitting = false;

          if (err.status === 401) {
            this.serverError = "Electronic signature incorrect.";
            alert(
              "Electronic signature incorrect. You will be returned to lot details.",
            );
            this.router.navigate(["/lots", this.lot!.id]);
          } else if (err.status === 422) {
            this.serverError =
              err.error?.message ?? "This status transition is not allowed.";
            alert(this.serverError + " You will be returned to lot details.");
            this.router.navigate(["/lots", this.lot!.id]);
          } else {
            this.serverError = "An error occurred. Please try again.";
            alert(this.serverError + " You will be returned to lot details.");
            this.router.navigate(["/lots", this.lot!.id]);
          }
        },
      });
  }

  onCancel(): void {
    if (this.lot) {
      this.router.navigate(["/lots", this.lot.id]);
    } else {
      this.router.navigate(["/lots"]);
    }
  }

  isInvalid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl?.touched);
  }

  statusLabel(status: LotStatus): string {
    const map: Record<LotStatus, string> = {
      Quarantine: "Quarantine",
      Released: "Released",
      Rejected: "Rejected",
    };
    return map[status];
  }

  badgeClass(status: LotStatus): string {
    const map: Record<LotStatus, string> = {
      Quarantine: "badge-orange",
      Released: "badge-green",
      Rejected: "badge-red",
    };
    return map[status];
  }
}
