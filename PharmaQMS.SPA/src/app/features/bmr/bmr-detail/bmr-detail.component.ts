import { CommonModule } from "@angular/common";
import { ChangeDetectorRef, Component, inject, OnInit } from "@angular/core";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import {
  BmrDetailResponse,
  BmrStatus,
  BmrStepStatus,
} from "../../../core/models/bmr.model";
import { BmrService } from "../../../core/services/bmr.service";
@Component({
  selector: "app-bmr-detail",
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: "./bmr-detail.component.html",
  styleUrl: "./bmr-detail.component.css",
})
export class BmrDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly bmrService = inject(BmrService);
  private readonly router = inject(Router);
  constructor(private cdr: ChangeDetectorRef) {}

  detail: BmrDetailResponse | null = null;
  isLoading = true;
  errorMessage: string | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get("id");

    if (!id) {
      this.errorMessage = "Invalid BMR id.";
      this.isLoading = false;
      return;
    }

    this.loadDetail(id);
  }

  getStatusLabel(status: BmrStatus): string {
    if (status === null || status === undefined) {
      return "-";
    }
    switch (status) {
      case BmrStatus.InProgress:
        return "In progress";
      case BmrStatus.InQc:
        return "In QC";
      case BmrStatus.Completed:
        return "Completed";
      case BmrStatus.Rejected:
        return "Rejected";
      default:
        return status;
    }
  }

  getStatusClass(status: BmrStatus): string {
    switch (status) {
      case BmrStatus.InProgress:
        return "border-blue-300 text-blue-600";
      case BmrStatus.InQc:
        return "border-yellow-300 text-yellow-600";
      case BmrStatus.Completed:
        return "border-green-300 text-green-600";
      case BmrStatus.Rejected:
        return "border-red-300 text-red-600";
      default:
        return "status-pill";
    }
  }

  formatDate(value: string | null | undefined): string {
    if (!value) {
      return "-";
    }

    return new Date(value).toLocaleString("en-GB");
  }

  get totalSteps(): number {
    return this.detail?.steps?.length ?? 0;
  }

  get completedSteps(): number {
    return (
      this.detail?.steps?.filter((s) => s.status === BmrStepStatus.Verified)
        .length ?? 0
    );
  }

  back(): void {
    this.router.navigate(["/bmr"]);
  }

  private loadDetail(id: string): void {
    this.isLoading = true;
    this.errorMessage = null;

    this.bmrService.getBmrById(id).subscribe({
      next: (data) => {
        this.detail = data;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        if (error?.status === 404) {
          this.errorMessage = "BMR not found.";
        } else {
          this.errorMessage =
            "Could not load BMR details. Please try again later.";
        }

        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }
}
