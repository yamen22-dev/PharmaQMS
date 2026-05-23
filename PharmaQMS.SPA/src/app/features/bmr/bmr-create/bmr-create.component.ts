import {
  ChangeDetectorRef,
  Component,
  DestroyRef,
  inject,
  OnInit,
} from "@angular/core";
import { Router } from "@angular/router";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { forkJoin } from "rxjs";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";

import { BmrService } from "../../../core/services//bmr.service";
import { MasterRecipeService } from "../../../core/services/masterrecipes.service";
import { ProductionLineService } from "../../../core/services/productionline.service";
import { LotService } from "../../../core/services/lot.service";

import { MasterRecipeSummaryResponse } from "../../../core/models/masterrecipe.model";
import { ProductionLineResponse } from "../../../core/models/productionLine.model";
import { LotResponse } from "../../../core/models/lot.model";

@Component({
  selector: "app-bmr-create",
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: "./bmr-create.component.html",
  styleUrl: "./bmr-create.component.css",
})
export class BmrCreateComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  masterRecipes: MasterRecipeSummaryResponse[] = [];
  productionLines: ProductionLineResponse[] = [];
  availableLots: LotResponse[] = [];

  selectedMasterRecipeId = "";
  batchSize: number | null = null;
  selectedProductionLineId = "";
  selectedLotIds = new Set<number>();

  isLoading = false;
  isSubmitting = false;

  plannedBmrDate = new Date().toISOString().slice(0, 10);
  constructor(
    private readonly masterRecipeService: MasterRecipeService,
    private readonly productionLineService: ProductionLineService,
    private readonly lotService: LotService,
    private readonly bmrService: BmrService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.isLoading = true;

    forkJoin({
      recipes: this.masterRecipeService.getApprovedRecipes(),
      lines: this.productionLineService.getProductionLines(),
      lots: this.lotService.getReleasedLots(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ recipes, lines, lots }) => {
          this.masterRecipes = recipes;
          this.productionLines = lines;
          this.availableLots = lots;
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.isLoading = false;
          this.cdr.detectChanges();
          alert("Failed to load data. Please try again later.");
        },
      });
  }

  toggleLot(lotId: number): void {
    if (this.selectedLotIds.has(lotId)) {
      this.selectedLotIds.delete(lotId);
    } else {
      this.selectedLotIds.add(lotId);
    }
  }

  isLotSelected(lotId: number): boolean {
    return this.selectedLotIds.has(lotId);
  }

  get isFormValid(): boolean {
    return (
      !!this.selectedMasterRecipeId &&
      !!this.batchSize &&
      this.batchSize > 0 &&
      !!this.selectedProductionLineId &&
      this.selectedLotIds.size > 0
    );
  }

  cancel(): void {
    this.router.navigate(["/bmr"]);
  }

  createAndStart(): void {
    if (!this.isFormValid || this.isSubmitting) return;

    this.isSubmitting = true;

    this.bmrService
      .createBmr({
        MasterRecipeId: this.selectedMasterRecipeId,
        BatchSize: this.batchSize!,
        ProductionLineId: this.selectedProductionLineId,
        LotIds: Array.from(this.selectedLotIds),
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (bmr) => {
          alert("BMR created successfully!");
          this.router.navigate(["/bmr", bmr.id]);
        },
        error: () => {
          this.isSubmitting = false;
          this.cdr.markForCheck();
          alert("Failed to create BMR. Please try again later.");
        },
      });
  }
  back(): void {
    this.router.navigate(["/bmr"]);
  }
}
