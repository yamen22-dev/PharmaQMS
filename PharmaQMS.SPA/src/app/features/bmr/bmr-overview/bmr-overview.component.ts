import { CommonModule } from "@angular/common";

import { ChangeDetectorRef, Component, OnInit, inject } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { BmrService } from "../../../core/services/bmr.service";
import { BmrStatus, BmrSummaryResponse } from "../../../core/models/bmr.model";

@Component({
  selector: "app-bmr-overview",
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: "./bmr-overview.component.html",
  styleUrl: "./bmr-overview.component.css",
})
export class BmrOverviewComponent implements OnInit {
  private readonly bmrService = inject(BmrService);
  private readonly router = inject(Router);

  searchText = "";
  selectedStatus: BmrStatus | "all" = "all";

  bmrList: BmrSummaryResponse[] = [];

  isLoading = true;
  constructor(private readonly cdr: ChangeDetectorRef) {}

  statusLabels: Record<number, string> = {
    0: "In Progress",
    1: "Completed",
    2: "Closed",
    3: "In QC",
  };

  ngOnInit(): void {
    this.bmrService.getBmrs({ Page: 1, PageSize: 10 }).subscribe({
      next: (response) => {
        this.bmrList = response.items; // ← PageResponse<T> wrapper
        this.isLoading = false;
        this.cdr.detectChanges(); // Force update after async data load
      },
    });
  }

  get totalBatches(): number {
    return this.bmrList.length;
  }

  get inProgress(): number {
    return this.bmrList.filter((x) => x.status === BmrStatus.InProgress).length;
  }

  get inQc(): number {
    return this.bmrList.filter((x) => x.status === BmrStatus.InQc).length;
  }

  get completed(): number {
    return this.bmrList.filter((x) => x.status === BmrStatus.Completed).length;
  }

  get rejected(): number {
    return this.bmrList.filter((x) => x.status === BmrStatus.Rejected).length;
  }

  get filteredBmrList(): BmrSummaryResponse[] {
    const search = this.searchText.trim().toLowerCase();
    return this.bmrList.filter((item) => {
      const matchesSearch =
        !search ||
        item.batchNumber.toLowerCase().includes(search) ||
        item.recipeName.toLowerCase().includes(search); // ← RecipeName not productName

      const matchesStatus =
        this.selectedStatus === "all" || item.status === this.selectedStatus;

      return matchesSearch && matchesStatus;
    });
  }

  getStatusClass(status: BmrStatus): string {
    switch (status) {
      case BmrStatus.InProgress:
        return "bg-blue-50 text-blue-700 border-blue-100";
      case BmrStatus.InQc:
        return "bg-violet-50 text-violet-700 border-violet-100";
      case BmrStatus.Completed:
        return "bg-lime-50 text-lime-700 border-lime-100";
      case BmrStatus.Rejected:
        return "bg-red-50 text-red-700 border-red-100";
      default:
        return "bg-slate-50 text-slate-700 border-slate-200";
    }
  }

  getProgressClass(status: BmrStatus): string {
    switch (status) {
      case BmrStatus.InProgress:
        return "bg-blue-600";
      case BmrStatus.InQc:
        return "bg-violet-600";
      case BmrStatus.Completed:
        return "bg-lime-600";
      case BmrStatus.Rejected:
        return "bg-red-400";
      default:
        return "bg-slate-500";
    }
  }

  getProgress(item: BmrSummaryResponse): number {
    return item.totalSteps === 0
      ? 0
      : Math.round((item.completedSteps / item.totalSteps) * 100);
  }

  newBmr(): void {
    this.router.navigate(["/bmr/new"]);
  }
  // trackByBatch(index: number, item: BmrSummaryResponse): string {
  //   return item.batchNumber; // or a unique ID
  // }
}