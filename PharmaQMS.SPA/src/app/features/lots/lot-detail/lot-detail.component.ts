import { ChangeDetectorRef, Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule, ActivatedRoute } from "@angular/router";
import { LotService } from "../../../core/services/lot.service";
import { AuthService } from "../../../core/services/auth.service";
import { LotDetail, LotStatus } from "../../../core/models/lot.model";

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

  lot: LotDetail | null = null;
  loading = true;

  constructor(private cdr: ChangeDetectorRef) {}

  get canChangeStatus(): boolean {
    return this.authService.hasRole("QAManager");
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
