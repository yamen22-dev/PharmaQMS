import { ChangeDetectorRef, Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule, ActivatedRoute } from "@angular/router";
import { LotService } from "../../../core/services/lot.service";
import { AuthService } from "../../../core/services/auth.service";
import { LotDetail, LotStatus } from "../../../core/models/lot.model";
import { QcService } from "../../../core/services/qc.service";
import { QcResult, QcTestSummary } from "../../../core/models/qc-test.model";

@Component({
  selector: "app-lot-detail",
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: "./lot-detail.component.html",
  styleUrl: "./lot-detail.component.css",
})
export class LotDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly lotService = inject(LotService);
  private readonly authService = inject(AuthService);
  private readonly qcService = inject(QcService);

  lot: LotDetail | null = null;
  lotQcTests: QcTestSummary[] = [];
  loading = true;
  qcLoading = true;

  constructor(private cdr: ChangeDetectorRef) {}

  get canChangeStatus(): boolean {
    return this.authService.hasRole("QAManager");
  }

  get canCreateQcTest(): boolean {
    return (
      this.authService.hasRole("QAManager") ||
      this.authService.hasRole("QCAnalyst")
    );
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get("id"));

    this.lotService.getLotById(id).subscribe({
      next: (data) => {
        this.lot = data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });

    this.qcService.getQcTests().subscribe({
      next: (tests) => {
        this.lotQcTests = tests.filter(
          (test) =>
            test.testObjectType.toLowerCase() === "lot" &&
            test.testObjectId === id,
        );
        this.qcLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.qcLoading = false;
        this.cdr.detectChanges();
      },
    });
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

  qcStatusLabel(status: QcResult): string {
    return status;
  }

  qcBadgeClass(status: QcResult): string {
    const map: Record<QcResult, string> = {
      Passed: "qc-badge qc-badge--green",
      "In progress": "qc-badge qc-badge--amber",
      OOS: "qc-badge qc-badge--red",
    };
    return map[status];
  }
}
