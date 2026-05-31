import { CommonModule } from "@angular/common";
import { ChangeDetectorRef, Component, inject, OnInit } from "@angular/core";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { RawMaterialService } from "../../../core/services/raw-material.service";
import {
  LotStatus,
  RawMaterialCategory,
  RawMaterialDetail,
} from "../../../core/models/raw-material.model";

@Component({
  selector: "app-raw-material-detail",
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: "./raw-material-detail.component.html",
  styleUrl: "./raw-material-detail.component.css",
})
export class RawMaterialDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly rawMaterialService = inject(RawMaterialService);

  isLoading = true;
  errorMessage: string | null = null;
    detail: RawMaterialDetail | null = null;

    constructor(private cdr: ChangeDetectorRef) { }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get("id");
    const id = Number(idParam);

    if (!idParam || Number.isNaN(id)) {
      this.errorMessage = "Invalid raw material ID.";
      this.isLoading = false;
      return;
    }

    this.loadDetail(id);
  }

  getCategoryLabel(category: string): string {
    switch (category) {
      case RawMaterialCategory.ActivePharmaceuticalIngredient:
        return "Active pharmaceutical ingredient";
      case RawMaterialCategory.Excipient:
        return "Excipient";
      case RawMaterialCategory.Packaging:
        return "Packaging";
      case RawMaterialCategory.Solvent:
        return "Solvent";
      case RawMaterialCategory.Other:
        return "Other";
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

  getStatusLabel(status: LotStatus): string {
    switch (status) {
      case "Quarantine":
        return "Quarantine";
      case "Released":
        return "Released";
      case "Rejected":
        return "Rejected";
      default:
        return status;
    }
  }

  getStatusClass(status: LotStatus): string {
    switch (status) {
      case "Quarantine":
        return "status-pill status-pill--quarantine";
      case "Released":
        return "status-pill status-pill--released";
      case "Rejected":
        return "status-pill status-pill--rejected";
      default:
        return "status-pill";
    }
  }

  formatSpecLimits(min: number, max: number): string {
    return `${this.formatSpecValue(min)} - ${this.formatSpecValue(max)}`;
  }

  getDescription(text: string | null | undefined): string {
    if (!text) {
      return "No description available.";
    }

    const trimmed = text.trim();
    return trimmed.length ? trimmed : "No description available.";
  }

  private loadDetail(id: number): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.rawMaterialService.getRawMaterialById(id).subscribe({
      next: (detail) => {
        this.detail = detail;
            this.isLoading = false;
            this.cdr.detectChanges();
      },
      error: (error) => {
        if (error?.status === 404) {
          this.errorMessage = "Raw material not found.";
        } else {
          this.errorMessage =
            "The detail view could not be loaded. Please try again later.";
        }
        this.isLoading = false;
      },
    });
  }

  private formatSpecValue(value: number): string {
    return value.toLocaleString("en-GB", {
      minimumFractionDigits: 1,
      maximumFractionDigits: 2,
    });
  }
}
