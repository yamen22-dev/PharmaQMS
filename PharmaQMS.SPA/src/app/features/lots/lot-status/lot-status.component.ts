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
      this.serverError = "Ongeldige lot-id.";
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
        this.serverError = "Lot niet gevonden.";
      },
    });
  }

  private getAllowedTransitions(
    status: LotStatus,
  ): { value: LotStatus; label: string }[] {
    if (status !== "Quarantine") return [];
    return [
      { value: "Released", label: "Goedgekeurd" },
      { value: "Rejected", label: "Afgekeurd" },
    ];
  }

  onSubmit(): void {
    if (!this.lot) {
      this.serverError = "Lot niet geladen.";
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
          alert("Status succesvol gewijzigd. U wordt teruggestuurd naar de lotdetails.");
          this.router.navigate(["/lots", this.lot!.id]);
        },
        error: (err) => {
          this.submitting = false;

          if (err.status === 401) {
            this.serverError = "Elektronische handtekening incorrect.";
            alert("Elektronische handtekening incorrect. U wordt teruggestuurd naar de lotdetails.");
            this.router.navigate(["/lots", this.lot!.id]);
          } else if (err.status === 422) {
            this.serverError =
              err.error?.message ?? "Deze statusovergang is niet toegestaan.";
              alert(this.serverError + " U wordt teruggestuurd naar de lotdetails.");
              this.router.navigate(["/lots", this.lot!.id]);
          } else {
            this.serverError = "Er is een fout opgetreden. Probeer opnieuw.";
            alert(this.serverError + " U wordt teruggestuurd naar de lotdetails.");
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
      Quarantine: "Quarantaine",
      Released: "Goedgekeurd",
      Rejected: "Afgekeurd",
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
