import { Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { LotService } from "../../../core/services/lot.service";
import { RawMaterialService } from "../../../core/services/raw-material.service";
import { LotSummary, LotStatus } from "../../../core/models/lot.model";

@Component({
  selector: "app-lots-overview",
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: '../lot-form/lot-form.component.html',
})
export class LotsOverviewComponent implements OnInit {
  private readonly lotService = inject(LotService);

  lots: LotSummary[] = [];
  filtered: LotSummary[] = [];
  rawMaterialNames: string[] = [];
  loading = true;

  searchQuery = "";
  selectedStatus = "";
  selectedRawMaterial = "";
  selectedRawMaterialId: number | null = null;

  ngOnInit(): void {
    this.lotService.getLots().subscribe({
      next: (data) => {
        this.lots = data;
        this.rawMaterialNames = [
          ...new Set(data.map((l) => l.rawMaterialName)),
        ];
        this.applyFilters();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  applyFilters(): void {
    this.filtered = this.lots.filter((l) => {
      const matchSearch = this.searchQuery
        ? l.lotNumber.toLowerCase().includes(this.searchQuery.toLowerCase())
        : true;
      const matchStatus = this.selectedStatus
        ? l.status === this.selectedStatus
        : true;
      const matchRm = this.selectedRawMaterial
        ? l.rawMaterialName === this.selectedRawMaterial
        : true;
      return matchSearch && matchStatus && matchRm;
    });

    const selected = this.filtered[0];
    this.selectedRawMaterialId = selected?.rawMaterialId ?? null;
  }

  countByStatus(status: LotStatus): number {
    return this.lots.filter((l) => l.status === status).length;
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
