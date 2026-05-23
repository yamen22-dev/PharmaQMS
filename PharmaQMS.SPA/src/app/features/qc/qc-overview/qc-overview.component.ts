import { ChangeDetectorRef, Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { RouterModule } from "@angular/router";
import { QcService } from "../../../core/services/qc.service";
import { QcTestSummary, QcResult } from "../../../core/models/qc-test.model";

@Component({
  selector: "app-qc-overview",
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: "./qc-overview.component.html",
  styleUrls: ["./qc-overview.component.css"],
})
export class QcOverviewComponent implements OnInit {
  private readonly qcService = inject(QcService);
  private readonly cdr = inject(ChangeDetectorRef);
  tests: QcTestSummary[] = [];
  filtered: QcTestSummary[] = [];
  loading = true;

  searchQuery = "";
  selectedResult = "";

  ngOnInit(): void {
    this.qcService.getQcTests().subscribe({
      next: (data) => {
        this.tests = this.normalizeTests(data);
        this.applyFilters();
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => (this.loading = false),
    });
  }

  applyFilters(): void {
    const source = Array.isArray(this.tests) ? this.tests : [];

    const search = this.searchQuery.trim().toLowerCase();
    this.filtered = source.filter((t) => {
      const testObject = (t.testObject ?? "").toLowerCase();
      const testObjectType = (t.testObjectType ?? "").toLowerCase();
      const createdBy = (t.createdBy ?? "").toLowerCase();
      const status = this.statusLabel(t.status);

      const matchSearch = search
        ? testObject.includes(search) ||
          testObjectType.includes(search) ||
          createdBy.includes(search)
        : true;

      const matchResult = this.selectedResult
        ? status === this.selectedResult
        : true;

      return matchSearch && matchResult;
    });
  }

  private normalizeTests(data: unknown): QcTestSummary[] {
    if (Array.isArray(data)) {
      return data;
    }

    if (data && typeof data === "object") {
      const payload = data as {
        tests?: QcTestSummary[];
        items?: QcTestSummary[];
        data?: QcTestSummary[];
      };

      if (Array.isArray(payload.tests)) {
        return payload.tests;
      }

      if (Array.isArray(payload.items)) {
        return payload.items;
      }

      if (Array.isArray(payload.data)) {
        return payload.data;
      }
    }

    return [];
  }

  countByResult(r: QcResult): number {
    return this.tests.filter((t) => this.statusLabel(t.status) === r).length;
  }

  badgeClass(test: QcResult): string {
    const map: Record<QcResult, string> = {
      Passed:
        "bg-green-100 text-green-700 font-semibold px-2 py-0.5 rounded-full",
      "In progress":
        "bg-yellow-100 text-yellow-800 font-semibold px-2 py-0.5 rounded-full",
      OOS: "bg-red-100 text-red-700 font-semibold px-2 py-0.5 rounded-full",
    };
    return map[test];
  }

  statusLabel(status: QcResult | number | string | null | undefined): QcResult {
    const normalized =
      typeof status === "string" ? status.trim().toLowerCase() : status;

    switch (normalized) {
      case 0:
      case 1:
      case "0":
      case "1":
      case "in behandeling":
      case "in progress":
        return "In progress";
      case 2:
      case "2":
      case "approved":
      case "passed":
        return "Passed";
      case 3:
      case "3":
      case "rejected":
      case "oos":
        return "OOS";
      default:
        return "In progress";
    }
  }

  testObjectLabel(test: QcTestSummary): string {
    return test.testObject?.trim() || `${test.testObjectType} #${test.id}`;
  }
}
