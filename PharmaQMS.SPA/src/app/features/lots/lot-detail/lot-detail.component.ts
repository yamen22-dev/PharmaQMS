import { Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule, ActivatedRoute } from "@angular/router";
import { LotService } from "../../../core/services/lot.service";
import { AuthService } from "../../../core/services/auth.service";
import { LotDetail, LotStatus } from "../../../core/models/lot.model";

@Component({
  selector: "app-lot-detail",
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <div class="page-header" *ngIf="lot">
      <div>
        <div class="breadcrumb">
          <a routerLink="/lots">Lots</a>
          <span> › </span>
          <span>{{ lot.lotNumber }}</span>
        </div>
        <h1>Lot detail — {{ lot.lotNumber }}</h1>
      </div>
      <a
        *ngIf="canChangeStatus"
        [routerLink]="['/lots', lot.id, 'status']"
        class="btn btn-secondary"
      >
        Status wijzigen
      </a>
    </div>

    <div class="detail-grid" *ngIf="lot; else loadingTpl">
      <!-- Lotgegevens -->
      <div class="card">
        <h2 class="card-title">Lotgegevens</h2>
        <dl class="detail-list">
          <dt>Lotnummer</dt>
          <dd>{{ lot.lotNumber }}</dd>
          <dt>Grondstof</dt>
          <dd>{{ lot.rawMaterialName }}</dd>
          <dt>Leverancier</dt>
          <dd>{{ lot.supplier }}</dd>
          <dt>API</dt>
          <dd>{{ lot.pharmaceuticalApi }}</dd>
          <dt>Hoeveelheid</dt>
          <dd>{{ lot.quantity }} {{ lot.unit }}</dd>
          <dt>Ontvangstdatum</dt>
          <dd>{{ lot.receivedDateUtc | date: "dd-MM-yyyy" }}</dd>
          <dt>Status</dt>
          <dd>
            <span class="badge" [ngClass]="badgeClass(lot.status)">
              {{ statusLabel(lot.status) }}
            </span>
          </dd>
        </dl>
      </div>

      <!-- QC-tests (placeholder voor Sprint 4) -->
      <div class="card">
        <h2 class="card-title">QC-tests</h2>
        <p class="placeholder-text">
          QC-testresultaten worden beschikbaar na implementatie van de QC-module
          (Sprint 4).
        </p>
        <a routerLink="/quality-control" class="btn btn-outline"
          >Naar QC-module</a
        >
      </div>
    </div>

    <ng-template #loadingTpl>
      <p class="loading-text" *ngIf="loading">Laden...</p>
      <p class="error-text" *ngIf="!loading">Lot niet gevonden.</p>
    </ng-template>
  `,
})
export class LotDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly lotService = inject(LotService);
  private readonly authService = inject(AuthService);

  lot: LotDetail | null = null;
  loading = true;

  get canChangeStatus(): boolean {
    return this.authService.hasRole("QAManager");
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
