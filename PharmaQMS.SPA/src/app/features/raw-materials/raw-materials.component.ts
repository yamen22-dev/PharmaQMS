import { CommonModule } from "@angular/common";
import { ChangeDetectorRef, Component, inject, OnInit } from "@angular/core";
import { RouterLink } from "@angular/router";
import { finalize } from "rxjs";
import { RawMaterialService } from "../../core/services/raw-material.service";
import {
  LotStatus,
  RawMaterialCategory,
  RawMaterialOverview,
} from "../../core/models/raw-material.model";
import { AuthService } from "../../core/services/auth.service";
import { AuthResponse } from "../../core/models/auth-response.model";
import { FormsModule } from "@angular/forms";
import { LotService } from "../../core/services/lot.service";
import { LotSummary } from "../../core/models/lot.model";
@Component({
  selector: "app-raw-materials",
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: "./raw-materials.component.html",
  styleUrl: "./raw-materials.component.css",
})
export class RawMaterialsComponent implements OnInit {
  private readonly rawMaterialService = inject(RawMaterialService);
  private readonly authService = inject(AuthService);

  protected readonly session$ = this.authService.session$;
  protected readonly allowedRoles = ["QAManager", "WarehouseOperator"];

  rawMaterials: RawMaterialOverview[] = [];
  filteredMaterials: RawMaterialOverview[] = [];
  isLoading = false;
  isLoadingLots = false;
  errorMessage: string | null = null;
  searchTerm = "";
  selectedCategory = "all";
  canCreate = false;
  stats = {
    totalRegistered: 0,
    activeSubstances: 0,
    excipients: 0,
    lotsInQuarantine: 0,
  };

  categoryOptions: Array<{ value: string; label: string }> = [];

  constructor(private cdr: ChangeDetectorRef) {
    this.categoryOptions = [
      { value: "all", label: "Alle categorieen" },
      ...Object.values(RawMaterialCategory).map((value) => ({
        value,
        label: this.getCategoryLabel(value),
      })),
    ];
  }

  ngOnInit(): void {
    this.authService.session$.subscribe((session) => {
      this.canCreate = this.canCreateRawMaterial(session);
    });
    this.loadRawMaterials();
    this.lotService.getLots().subscribe({
      next: (data) => {
        this.lots = data;
        this.rawMaterialNames = [
          ...new Set(data.map((l) => l.rawMaterialName)),
        ];
        this.applyFilters_lot();
        this.isLoadingLots = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isLoadingLots = false;
      },
    });
  }

  canCreateRawMaterial(session: AuthResponse | null): boolean {
    return Array.isArray(session?.roles)
      ? session.roles.some((role) => this.allowedRoles.includes(role))
      : false;
  }

  onSearchInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.searchTerm = value;
    this.applyFilters();
  }

  onCategoryChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedCategory = value;
    this.applyFilters();
  }

  getCategoryLabel(category: string): string {
    switch (category) {
      case RawMaterialCategory.ActivePharmaceuticalIngredient:
        return "Werkzame stof";
      case RawMaterialCategory.Excipient:
        return "Hulpstof";
      case RawMaterialCategory.Packaging:
        return "Verpakking";
      case RawMaterialCategory.Solvent:
        return "Oplosmiddel";
      case RawMaterialCategory.Other:
        return "Overig";
      default:
        return category;
    }
  }

  getCategoryClass(category: string): string {
    switch (category) {
      case RawMaterialCategory.ActivePharmaceuticalIngredient:
        return "category-pill category-pill--active";
      case RawMaterialCategory.Excipient:
        return "category-pill category-pill--excipient";
      case RawMaterialCategory.Packaging:
        return "category-pill category-pill--packaging";
      case RawMaterialCategory.Solvent:
        return "category-pill category-pill--solvent";
      default:
        return "category-pill category-pill--other";
    }
  }

  formatPharmaceuticalApi(value: string): string {
    return value?.trim().length ? value : "-";
  }

  private loadRawMaterials(): void {
    this.selectedCategory = "all";
    this.isLoading = true;
    this.errorMessage = null;

    this.rawMaterialService
      .getRawMaterials()
      .pipe(
        finalize(() => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }),
      )
      .subscribe({
        next: (materials) => {
          this.rawMaterials = Array.isArray(materials) ? materials : [];
          this.computeStats();
          this.applyFilters();
          this.cdr.detectChanges();
        },
        error: () => {
          this.errorMessage =
            "Het overzicht kon niet worden geladen. Probeer het later opnieuw.";
        },
      });
  }

  private computeStats(): void {
    this.stats = {
      totalRegistered: this.rawMaterials.length,
      activeSubstances: this.rawMaterials.filter(
        (material) =>
          material.category ===
          RawMaterialCategory.ActivePharmaceuticalIngredient,
      ).length,
      excipients: this.rawMaterials.filter(
        (material) => material.category === RawMaterialCategory.Excipient,
      ).length,
      lotsInQuarantine: this.rawMaterials.reduce(
        (total, material) => total + (material.quarantineLots || 0),
        0,
      ),
    };
  }

  public applyFilters(): void {
    // Normalise the search term once
    const term = this.searchTerm.trim().toLowerCase();

    // The value coming from the <select> (e.g. "all", "ActivePharmaceuticalIngredient", …)
    const categoryFilter = this.selectedCategory;

    // Build the filtered array
    this.filteredMaterials = this.rawMaterials.filter((material) => {
      // ---------- Category check ----------
      // If the user chose "all" we accept every category,
      // otherwise we compare the stored category with the selected one.
      const matchesCategory =
        categoryFilter === "all" || material.category === categoryFilter; // <-- make sure both sides are the same type

      // ---------- Text search ----------
      const name = (material.name ?? "").toLowerCase();
      const api = (material.pharmaceuticalApi ?? "").toLowerCase();

      const matchesTerm =
        !term || // empty search → everything matches
        name.includes(term) || // name contains the term
        api.includes(term); // pharmaceutical API contains the term

      // Keep the item only when **both** conditions are true
      return matchesCategory && matchesTerm;
    });

    // Re‑calculate any statistics that depend on the filtered view
    this.computeStats();
  }
  private readonly lotService = inject(LotService);

  lots: LotSummary[] = [];
  filtered: LotSummary[] = [];
  rawMaterialNames: string[] = [];

  searchQuery = "";
  selectedStatus = "";
  selectedRawMaterial = "";
  selectedRawMaterialId: number | null = null;

  applyFilters_lot(): void {
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

    // Zoek het id op basis van de geselecteerde grondstof naam
    this.selectedRawMaterialId = this.selectedRawMaterial
      ? (this.lots.find((l) => l.rawMaterialName === this.selectedRawMaterial)
          ?.rawMaterialId ?? null)
      : null;
  }

  countByStatus(status: LotStatus): number {
    return this.lots.filter((l) => l.status === status).length;
  }

  onsearchInput_lot(event: Event): void {
    this.searchQuery = (event.target as HTMLInputElement).value;
    this.applyFilters_lot();
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

