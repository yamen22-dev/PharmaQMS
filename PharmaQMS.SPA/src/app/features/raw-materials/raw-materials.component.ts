import { CommonModule } from "@angular/common";
import { ChangeDetectorRef, Component, inject, OnInit } from "@angular/core";
import { RouterLink } from "@angular/router";
import { finalize } from "rxjs";
import { RawMaterialService } from "../../core/services/raw-material.service";
import {
  RawMaterialCategory,
  RawMaterialOverview,
} from "../../core/models/raw-material.model";
import { AuthService } from "../../core/services/auth.service";
import { AuthResponse } from "../../core/models/auth-response.model";
import { FormsModule } from "@angular/forms";
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
                categoryFilter === 'all' ||
                material.category === categoryFilter;   // <-- make sure both sides are the same type

            // ---------- Text search ----------
            const name = (material.name ?? '').toLowerCase();
            const api = (material.pharmaceuticalApi ?? '').toLowerCase();

            const matchesTerm =
                !term ||                                   // empty search → everything matches
                name.includes(term) ||                     // name contains the term
                api.includes(term);                        // pharmaceutical API contains the term

            // Keep the item only when **both** conditions are true
            return matchesCategory && matchesTerm;
        });

        // Re‑calculate any statistics that depend on the filtered view
        this.computeStats();
    }
}
