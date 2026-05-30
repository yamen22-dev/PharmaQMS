import { CommonModule } from "@angular/common";
import { ChangeDetectorRef, Component, inject, OnInit } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, RouterLink } from "@angular/router";
import {
  BmrDetailResponse,
  BmrStepResponse,
  BmrStepStatus,
  ConfirmStepRequest,
} from "../../../core/models/bmr.model";
import { BmrService } from "../../../core/services/bmr.service";
import { AuthService } from "../../../core/services/auth.service";

type UiStep = {
  id: string;
  number: number;
  name: string;
  status: BmrStepStatus;
  remark?: string | null;
  completedBy?: string | null;
  completedAt?: string | null;
  blockReason?: string | null;
};

@Component({
  selector: "app-bmr-step-confirm",
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: "./bmr-step-confirm.component.html",
  styleUrls: ["./bmr-step-confirm.component.css"],
})
export class BmrStepConfirmationComponent implements OnInit {
  readonly BmrStepStatus = BmrStepStatus;

  private readonly route = inject(ActivatedRoute);
  private readonly bmrService = inject(BmrService);
  private readonly authService = inject(AuthService);

  protected bmrId: string | null = null;
  protected canVerifySteps = false;

  batchNumber = "";
  steps: UiStep[] = [];
  isLoading = true;
  errorMessage = "";

  selectedStep: UiStep | null = null;
  stepForm = {
    temperature: 38,
    duration: 45,
    remark: "",
  };

  constructor(private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.bmrId = this.route.snapshot.paramMap.get("id") ?? "";
    this.canVerifySteps = this.authService.hasRole("QAManager");
    this.loadSteps();
  }

  private mapStep(s: BmrStepResponse): UiStep {
    return {
      id: s.id,
      number: s.stepNumber,
      name: s.stepName,
      status: s.status,
      remark: s.deviationNote ?? null,
      completedBy: s.verifiedById ?? s.enteredById ?? null,
      completedAt: s.verifiedAt ?? s.enteredAt ?? null,
      blockReason: null,
    };
  }

  loadSteps(): void {
    if (!this.bmrId) {
      this.errorMessage = "Invalid BMR id.";
      this.isLoading = false;
      return;
    }

    this.isLoading = true;
    this.errorMessage = "";

    this.bmrService.getBmrById(this.bmrId).subscribe({
      next: (data: BmrDetailResponse) => {
        this.batchNumber = data.batchNumber;
        // map backend step shape to UI-friendly shape
        this.steps = (data.steps || []).map((s: BmrStepResponse) =>
          this.mapStep(s),
        );
        // resolve display names for entered/verified users
        this.steps.forEach((st) => {
          const userId = st.completedBy;
          if (userId) {
            this.authService.getUserNameById(userId).subscribe((name) => {
              st.completedBy = name;
              this.cdr.detectChanges();
            });
          }
        });
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = "Failed to load steps.";
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  getStatusBadgeClass(status: BmrStepStatus | string): string {
    switch (status) {
      case BmrStepStatus.Open:
      case "Open":
      case "0":
        return "bg-blue-100 text-blue-700";
      case BmrStepStatus.AwaitingVerification:
      case "AwaitingVerification":
      case "1":
        return "bg-amber-100 text-amber-700";
      case BmrStepStatus.Verified:
      case "Verified":
      case "2":
        return "bg-green-100 text-green-700";
      default:
        return "bg-slate-100 text-slate-600";
    }
  }

  getStatusLabel(status: BmrStepStatus | string): string {
    switch (status) {
      case BmrStepStatus.Open:
      case "Open":
      case "0":
        return "Open";
      case BmrStepStatus.AwaitingVerification:
      case "AwaitingVerification":
      case "1":
        return "Awaiting verification";
      case BmrStepStatus.Verified:
      case "Verified":
      case "2":
        return "Verified";
      default:
        return String(status);
    }
  }

  openStepForm(step: UiStep): void {
    if (step.status !== BmrStepStatus.Open) {
      return;
    }

    this.selectedStep = step;
    this.stepForm = {
      temperature: 38,
      duration: 45,
      remark: step.remark || "",
    };
  }

  closeForm(): void {
    this.selectedStep = null;
    this.stepForm = {
      temperature: 38,
      duration: 45,
      remark: "",
    };
  }

  submitStep(): void {
    if (!this.selectedStep || !this.bmrId) return;

    const confirmReq: ConfirmStepRequest = {
      EnteredData: JSON.stringify({
        temperature: this.stepForm.temperature,
        duration: this.stepForm.duration,
      }),
      DeviationNote: this.stepForm.remark || undefined,
    };

    this.bmrService
      .confirmStep(this.bmrId, this.selectedStep.id, confirmReq)
      .subscribe({
        next: () => {
          alert("Step executed successfully!");
          this.closeForm();
          this.loadSteps();
        },
        error: (err) => {
          console.error(err);
          alert("Failed to execute step.");
        },
      });
  }
}
