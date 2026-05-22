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
  template: `
    <div class="breadcrumb" *ngIf="lot">
      <a routerLink="/lots">Lots</a>
      <span> › </span>
      <a [routerLink]="['/lots', lot.id]">{{ lot.lotNumber }}</a>
      <span> › </span>
      <span>Status wijzigen</span>
    </div>
    <h1>Kwaliteitsstatus wijzigen</h1>

    <div class="card form-card" *ngIf="lot; else loadingTpl">
      <!-- Audit Trail banner -->
      <div class="warning-banner">
        Statuswijzigingen worden vastgelegd in de Audit Trail en zijn niet
        omkeerbaar zonder nieuwe wijziging.
      </div>

      <form [formGroup]="form" (ngSubmit)="onSubmit()">
        <!-- Huidige toestand (readonly) -->
        <div class="field-row">
          <span class="field-label">Lot</span>
          <span class="field-value">{{ lot.lotNumber }}</span>
        </div>
        <div class="field-row">
          <span class="field-label">Huidige status</span>
          <span class="badge" [ngClass]="badgeClass(lot.status)">
            {{ statusLabel(lot.status) }}
          </span>
        </div>

        <!-- Nieuwe status -->
        <div class="form-group mt">
          <label for="newStatus">NIEUWE STATUS</label>
          <select
            id="newStatus"
            formControlName="newStatus"
            [class.input-error]="isInvalid('newStatus')"
          >
            <option value="">Selecteer nieuwe status...</option>
            <option *ngFor="let opt of allowedTransitions" [value]="opt.value">
              {{ opt.label }}
            </option>
          </select>
          <span class="error-msg" *ngIf="isInvalid('newStatus')">
            Selecteer een nieuwe status.
          </span>
        </div>

        <!-- Reden -->
        <div class="form-group">
          <label for="reason">REDEN VOOR STATUSWIJZIGING</label>
          <textarea
            id="reason"
            formControlName="reason"
            rows="4"
            placeholder="Beschrijf de reden voor deze wijziging (verplicht)..."
            [class.input-error]="isInvalid('reason')"
          ></textarea>
          <span class="error-msg" *ngIf="isInvalid('reason')">
            Reden is verplicht.
          </span>
        </div>

        <!-- Elektronische handtekening -->
        <div class="form-group">
          <label for="password">ELEKTRONISCHE HANDTEKENING (WACHTWOORD)</label>
          <input
            id="password"
            type="password"
            formControlName="password"
            placeholder="Bevestig met uw wachtwoord"
            [class.input-error]="isInvalid('password')"
          />
          <span class="error-msg" *ngIf="isInvalid('password')">
            Wachtwoord is verplicht.
          </span>
        </div>

        <!-- Serverfout -->
        <div class="error-banner" *ngIf="serverError">{{ serverError }}</div>

        <div class="form-actions">
          <button
            type="button"
            class="btn btn-secondary"
            (click)="onCancel()"
            [disabled]="submitting"
          >
            Annuleren
          </button>
          <button type="submit" class="btn btn-primary" [disabled]="submitting">
            {{ submitting ? "Bezig..." : "Statuswijziging bevestigen" }}
          </button>
        </div>
      </form>
    </div>

    <ng-template #loadingTpl>
      <p class="loading-text" *ngIf="loading">Laden...</p>
    </ng-template>
  `,
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

  form = this.fb.group({
    newStatus: ["", Validators.required],
    reason: ["", [Validators.required, Validators.minLength(10)]],
    password: ["", Validators.required],
  });

  // Toegestane overgangen vanuit Quarantine (Alt-C)
  get allowedTransitions(): { value: LotStatus; label: string }[] {
    if (this.lot?.status !== "Quarantine") return [];
    return [
      { value: "Released", label: "Goedgekeurd" },
      { value: "Rejected", label: "Afgekeurd" },
    ];
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get("id"));
    this.lotService.getLotById(id).subscribe({
      next: (data) => {
        this.lot = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;
    this.serverError = null;

    const { newStatus, reason, password } = this.form.getRawValue();

    this.lotService
      .changeLotStatus(this.lot!.id, {
        newStatus: newStatus as LotStatus,
        reason: reason!,
        password: password!,
      })
      .subscribe({
        next: () => this.router.navigate(["/lots", this.lot!.id]),
        error: (err) => {
          this.submitting = false;
          if (err.status === 401) {
            // Alt-B: onjuiste elektronische handtekening
            this.serverError = "Elektronische handtekening incorrect.";
          } else if (err.status === 422) {
            // Alt-C: niet-toegestane overgang
            this.serverError =
              err.error?.message ?? "Deze statusovergang is niet toegestaan.";
          } else {
            this.serverError = "Er is een fout opgetreden. Probeer opnieuw.";
          }
        },
      });
  }

  onCancel(): void {
    this.router.navigate(["/lots", this.lot?.id]);
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
