import { CommonModule } from "@angular/common";
import { ChangeDetectorRef, Component, inject, OnInit } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import {
  BmrDetailResponse,
  BmrStepResponse,
  BmrStepStatus,
  VerifyStepRequest,
} from "../../../core/models/bmr.model";
import { BmrService } from "../../../core/services/bmr.service";
import { AuthService } from "../../../core/services/auth.service";
import { AuthStorageService } from "../../../core/services/auth-storage.service";
type StepExecutionData = {
  performedBy: string | null;
  timestamp: string | null;
  temperature?: number | null;
  duration?: number | null;
  remark?: string | null;
};

@Component({
  selector: "app-bmr-step-verification",
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: "./bmr-step-verification.component.html",
  styleUrls: ["./bmr-step-verification.component.css"],
})
export class BmrStepVerificationComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly bmrService = inject(BmrService);
  private readonly authService = inject(AuthService);
  private readonly authStorage = inject(AuthStorageService);
  private readonly cdr = inject(ChangeDetectorRef);

  batchNumber = "";
  stepTitle = "";
  stepData: StepExecutionData | null = null;
  verifier = { name: "", role: "" };
  verificationPassword = "";
  isLoading = true;
  errorMessage = "";
  verificationMessage = "";
  verificationMessageType: "success" | "error" | "" = "";

  private bmrId: string | null = null;
  private targetStepId: string | null = null;

  ngOnInit(): void {
    this.bmrId = this.route.snapshot.paramMap.get("bmrId");
    this.targetStepId = this.route.snapshot.paramMap.get("stepId");
    this.verifier.name = this.authStorage.getSessionDisplayName();
    this.verifier.role =
      this.authService.getSession()?.roles?.[0] ?? "Onbekende rol";

    if (!this.bmrId) {
      this.errorMessage = "Ongeldige BMR-id.";
      this.isLoading = false;
      return;
    }

    this.bmrService.getBmrById(this.bmrId).subscribe({
      next: (data: BmrDetailResponse) => {
        this.batchNumber = data.batchNumber;
        const steps = data.steps || [];
        let s: BmrStepResponse | undefined = this.targetStepId
          ? steps.find((st) => st.id === this.targetStepId)
          : undefined;

        if (!s) {
          s = steps.find(
            (st) => st.status === BmrStepStatus.AwaitingVerification,
          );
        }

        if (!s) {
          this.errorMessage =
            "Geen stappen gevonden die verificatie nodig hebben.";
          this.isLoading = false;
          this.cdr.detectChanges();
          return;
        }

        this.targetStepId = s.id;
        this.stepTitle = `Stap ${s.stepNumber}: ${s.stepName}`;
        const entered = s.enteredData
          ? (() => {
              try {
                return JSON.parse(s.enteredData as string);
              } catch {
                return null;
              }
            })()
          : null;

        this.stepData = {
          performedBy: null,
          timestamp: s.enteredAt ?? null,
          temperature: entered?.temperature ?? null,
          duration: entered?.duration ?? null,
          remark: s.deviationNote ?? null,
        };

        const performerId = s.enteredById ?? null;
        if (performerId) {
          this.authService.getUserNameById(performerId).subscribe((name) => {
            if (this.stepData) this.stepData.performedBy = name;
            this.cdr.detectChanges();
          });
        }

        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.errorMessage = "Kon stapgegevens niet laden.";
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  cancel(): void {
    if (this.bmrId) {
      this.router.navigate(["/bmr", this.bmrId]);
    }
  }

  get detailLink(): string[] | null {
    return this.bmrId ? ["/bmr", this.bmrId] : null;
  }

  get stepsLink(): string[] | null {
    return this.bmrId ? ["/bmr", this.bmrId, "steps"] : null;
  }

  confirmVerification(): void {
    if (!this.bmrId || !this.targetStepId) return;
    this.verificationMessage = "";
    this.verificationMessageType = "";

    if (!this.verificationPassword.trim()) {
      this.verificationMessage = "Vul je wachtwoord in om te verifiëren.";
      this.verificationMessageType = "error";
      this.cdr.detectChanges();
      return;
    }

    const req: VerifyStepRequest = { Password: this.verificationPassword };

    this.bmrService.verifyStep(this.bmrId, this.targetStepId, req).subscribe({
      next: () => {
        this.verificationMessage = "Verificatie geslaagd.";
        this.verificationMessageType = "success";
        this.verificationPassword = "";
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error(err);
        this.verificationMessage =
          "Verificatie mislukt. Controleer je wachtwoord.";
        this.verificationMessageType = "error";
        this.cdr.detectChanges();
      },
    });
  }
}
