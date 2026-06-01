import { CommonModule } from "@angular/common";
import { ChangeDetectorRef, Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { Observable, of } from "rxjs";
import { catchError, finalize, map, timeout } from "rxjs/operators";
import { AuditTrailEntry } from "../../core/models/audit-trail.model";
import { BmrStatus, BmrSummaryResponse } from "../../core/models/bmr.model";
import { LotStatus, LotSummary } from "../../core/models/lot.model";
import { QcResult, QcTestSummary } from "../../core/models/qc-test.model";
import {
  RawMaterialCategory,
  RawMaterialOverview,
} from "../../core/models/raw-material.model";
import { AuthService } from "../../core/services/auth.service";
import { AuditTrailService } from "../../core/services/audit-trail.service";
import { BmrService } from "../../core/services/bmr.service";
import { LotService } from "../../core/services/lot.service";
import { QcService } from "../../core/services/qc.service";
import { RawMaterialService } from "../../core/services/raw-material.service";

type DashboardSection = "all" | "materials" | "lots" | "bmr" | "qc" | "audit";
type BucketTone = "blue" | "green" | "amber" | "rose" | "slate" | "violet";

interface DashboardSectionOption {
  id: DashboardSection;
  label: string;
  description: string;
}

interface SummaryCard {
  id: DashboardSection;
  label: string;
  value: string;
  description: string;
  tone: BucketTone;
}

interface DistributionBucket {
  label: string;
  count: number;
  percent: number;
  tone: BucketTone;
  detail: string;
}

interface TimelineItem {
  id: string;
  source: DashboardSection;
  title: string;
  subtitle: string;
  detail: string;
  timestampLabel: string;
  route?: string[];
  tone: BucketTone;
  sortKey: number;
}

@Component({
  selector: "app-dashboard",
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: "./dashboard.component.html",
  styleUrl: "./dashboard.component.css",
})
export class DashboardComponent implements OnInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly authService = inject(AuthService);
  private readonly rawMaterialService = inject(RawMaterialService);
  private readonly lotService = inject(LotService);
  private readonly bmrService = inject(BmrService);
  private readonly qcService = inject(QcService);
  private readonly auditTrailService = inject(AuditTrailService);
  private readonly router = inject(Router);

  protected readonly session$ = this.authService.session$;
  protected readonly sectionOptions: DashboardSectionOption[] = [
    {
      id: "all",
      label: "All operations",
      description: "Cross-module pulse of the plant.",
    },
    {
      id: "materials",
      label: "Raw materials",
      description: "Category mix and stock pressure.",
    },
    {
      id: "lots",
      label: "Lots",
      description: "Lot status mix and recent receipts.",
    },
    {
      id: "bmr",
      label: "BMR",
      description: "Batch progress and production flow.",
    },
    {
      id: "qc",
      label: "Quality control",
      description: "QC results and test activity.",
    },
    {
      id: "audit",
      label: "Audit trail",
      description: "Recorded changes and recent actions.",
    },
  ];

  protected selectedSection: DashboardSection = "all";
  protected timelineSearch = "";
  protected loading = true;
  protected errorMessage: string | null = null;
  protected summaryCards: SummaryCard[] = [];
  protected distributionBuckets: DistributionBucket[] = [];
  protected timelineItems: TimelineItem[] = [];

  private rawMaterials: RawMaterialOverview[] = [];
  private lots: LotSummary[] = [];
  private bmrs: BmrSummaryResponse[] = [];
  private qcTests: QcTestSummary[] = [];
  private auditEntries: AuditTrailEntry[] = [];

  private readonly dataRequestTimeoutMs = 8000;
  private pendingRequests = 0;

  ngOnInit(): void {
    this.loadDashboard();
  }

  selectSection(section: DashboardSection): void {
    this.selectedSection = section;
    this.rebuildDerivedState();
  }

  logout(): void {
    this.authService
      .logout()
      .subscribe(() => void this.router.navigateByUrl("/login"));
  }

  get selectedSectionOption(): DashboardSectionOption {
    return (
      this.sectionOptions.find(
        (option) => option.id === this.selectedSection,
      ) ?? this.sectionOptions[0]
    );
  }

  get selectedTimelineItems(): TimelineItem[] {
    const search = this.timelineSearch.trim().toLowerCase();

    return this.timelineItems.filter((item) => {
      if (!search) {
        return true;
      }

      return [item.title, item.subtitle, item.detail, item.timestampLabel]
        .join(" ")
        .toLowerCase()
        .includes(search);
    });
  }

  trackBySection(_: number, option: DashboardSectionOption): DashboardSection {
    return option.id;
  }

  trackByTimeline(_: number, item: TimelineItem): string {
    return item.id;
  }

  private loadDashboard(): void {
    this.errorMessage = null;
    this.pendingRequests = 5;
    this.loading = true;

    this.loadWithFallback(
      this.rawMaterialService.getRawMaterials(),
      [] as RawMaterialOverview[],
    )
      .pipe(finalize(() => this.markRequestComplete()))
      .subscribe({
        next: (items) => {
          this.rawMaterials = items;
          this.rebuildDerivedState();
        },
        error: () => {
          this.errorMessage =
            "The dashboard could not load all activity data right now.";
        },
      });

    this.loadWithFallback(this.lotService.getLots(), [] as LotSummary[])
      .pipe(finalize(() => this.markRequestComplete()))
      .subscribe({
        next: (items) => {
          this.lots = items;
          this.rebuildDerivedState();
        },
        error: () => {
          this.errorMessage =
            "The dashboard could not load all activity data right now.";
        },
      });

    this.loadWithFallback(
      this.bmrService
        .getBmrs({ Page: 1, PageSize: 25 })
        .pipe(map((response) => response.items ?? [])),
      [] as BmrSummaryResponse[],
    )
      .pipe(finalize(() => this.markRequestComplete()))
      .subscribe({
        next: (items) => {
          this.bmrs = items;
          this.rebuildDerivedState();
        },
        error: () => {
          this.errorMessage =
            "The dashboard could not load all activity data right now.";
        },
      });

    this.loadWithFallback(this.qcService.getQcTests(), [] as QcTestSummary[])
      .pipe(finalize(() => this.markRequestComplete()))
      .subscribe({
        next: (items) => {
          this.qcTests = items;
          this.rebuildDerivedState();
        },
        error: () => {
          this.errorMessage =
            "The dashboard could not load all activity data right now.";
        },
      });

    this.loadWithFallback(
      this.auditTrailService.getAuditTrail(),
      [] as AuditTrailEntry[],
    )
      .pipe(finalize(() => this.markRequestComplete()))
      .subscribe({
        next: (items) => {
          this.auditEntries = items;
          this.rebuildDerivedState();
        },
        error: () => {
          this.errorMessage =
            "The dashboard could not load all activity data right now.";
        },
      });
  }

  private markRequestComplete(): void {
    this.pendingRequests = Math.max(0, this.pendingRequests - 1);

    if (this.pendingRequests === 0) {
      this.rebuildDerivedState();
      this.loading = false;
      this.cdr.detectChanges();
    }
  }

  private loadWithFallback<T>(
    source$: Observable<T>,
    fallback: T,
  ): Observable<T> {
    return source$.pipe(
      timeout(this.dataRequestTimeoutMs),
      catchError(() => of(fallback)),
    );
  }

  private rebuildDerivedState(): void {
    this.summaryCards = this.buildSummaryCards();
    this.distributionBuckets = this.buildDistributionBuckets();
    this.timelineItems = this.buildTimelineItems();
  }

  private buildSummaryCards(): SummaryCard[] {
    const lotPressure = this.rawMaterials.reduce(
      (total, material) =>
        total + (material.activeLots ?? 0) + (material.quarantineLots ?? 0),
      0,
    );

    return [
      {
        id: "materials",
        label: "Raw materials",
        value: this.rawMaterials.length.toString(),
        description: `${lotPressure} linked lots across the inventory`,
        tone: "blue",
      },
      {
        id: "lots",
        label: "Lots",
        value: this.lots.length.toString(),
        description: `${this.countLotsByStatus("Released")} released, ${this.countLotsByStatus("Quarantine")} in quarantine`,
        tone: "green",
      },
      {
        id: "bmr",
        label: "BMR batches",
        value: this.bmrs.length.toString(),
        description: `${this.countBmrByStatus(BmrStatus.Completed)} completed, ${this.countBmrByStatus(BmrStatus.InQc)} in QC`,
        tone: "violet",
      },
      {
        id: "qc",
        label: "QC tests",
        value: this.qcTests.length.toString(),
        description: `${this.countQcByResult("Passed")} passed, ${this.countQcByResult("OOS")} OOS`,
        tone: "amber",
      },
      {
        id: "audit",
        label: "Audit events",
        value: this.auditEntries.length.toString(),
        description: `Latest activity ${this.formatTimestamp(this.auditEntries[0]?.tijdstip)}`,
        tone: "rose",
      },
    ];
  }

  private buildDistributionBuckets(): DistributionBucket[] {
    if (this.selectedSection === "materials") {
      const categories = Object.values(RawMaterialCategory);
      return this.withPercentages(
        categories.map((category) => ({
          label: this.getCategoryLabel(category),
          count: this.rawMaterials.filter(
            (material) => material.category === category,
          ).length,
          tone: this.getCategoryTone(category),
          detail: category,
        })),
      );
    }

    if (this.selectedSection === "lots") {
      return this.withPercentages([
        {
          label: "Released",
          count: this.countLotsByStatus("Released"),
          tone: "green",
          detail: "Available for downstream use",
        },
        {
          label: "Quarantine",
          count: this.countLotsByStatus("Quarantine"),
          tone: "amber",
          detail: "Awaiting QA disposition",
        },
        {
          label: "Rejected",
          count: this.countLotsByStatus("Rejected"),
          tone: "rose",
          detail: "Blocked from release",
        },
      ]);
    }

    if (this.selectedSection === "bmr") {
      return this.withPercentages([
        {
          label: "In progress",
          count: this.countBmrByStatus(BmrStatus.InProgress),
          tone: "blue",
          detail: "Steps are still being entered",
        },
        {
          label: "In QC",
          count: this.countBmrByStatus(BmrStatus.InQc),
          tone: "violet",
          detail: "Waiting on quality review",
        },
        {
          label: "Completed",
          count: this.countBmrByStatus(BmrStatus.Completed),
          tone: "green",
          detail: "Finished records",
        },
        {
          label: "Rejected",
          count: this.countBmrByStatus(BmrStatus.Rejected),
          tone: "rose",
          detail: "Rejected by QA",
        },
      ]);
    }

    if (this.selectedSection === "qc") {
      return this.withPercentages([
        {
          label: "Passed",
          count: this.countQcByResult("Passed"),
          tone: "green",
          detail: "Within specification",
        },
        {
          label: "In progress",
          count: this.countQcByResult("In progress"),
          tone: "blue",
          detail: "Waiting for analyst input",
        },
        {
          label: "OOS",
          count: this.countQcByResult("OOS"),
          tone: "rose",
          detail: "Out-of-specification",
        },
      ]);
    }

    if (this.selectedSection === "audit") {
      return this.withPercentages([
        {
          label: "Create",
          count: this.countAuditByAction("Create"),
          tone: "blue",
          detail: "New records",
        },
        {
          label: "Status change",
          count: this.countAuditByAction("StatusChange"),
          tone: "violet",
          detail: "Workflow transitions",
        },
        {
          label: "Delete",
          count: this.countAuditByAction("Delete"),
          tone: "rose",
          detail: "Removed records",
        },
        {
          label: "Other",
          count: this.auditEntries.filter(
            (entry) =>
              !["Create", "StatusChange", "Delete"].includes(
                entry.actie.trim(),
              ),
          ).length,
          tone: "slate",
          detail: "Remaining audit events",
        },
      ]);
    }

    return this.withPercentages([
      {
        label: "Raw materials",
        count: this.rawMaterials.length,
        tone: "blue",
        detail: "Master inventory",
      },
      {
        label: "Lots",
        count: this.lots.length,
        tone: "green",
        detail: "Received and released lots",
      },
      {
        label: "BMR batches",
        count: this.bmrs.length,
        tone: "violet",
        detail: "Manufacturing records",
      },
      {
        label: "QC tests",
        count: this.qcTests.length,
        tone: "amber",
        detail: "Quality control checks",
      },
      {
        label: "Audit events",
        count: this.auditEntries.length,
        tone: "rose",
        detail: "Recorded changes",
      },
    ]);
  }

  private buildTimelineItems(): TimelineItem[] {
    if (this.selectedSection === "materials") {
      const materials = [...this.rawMaterials].sort((left, right) => {
        const leftPressure =
          (left.activeLots ?? 0) + (left.quarantineLots ?? 0);
        const rightPressure =
          (right.activeLots ?? 0) + (right.quarantineLots ?? 0);
        return (
          rightPressure - leftPressure || left.name.localeCompare(right.name)
        );
      });

      return materials.slice(0, 6).map((material, index) => ({
        id: `material-${material.id}`,
        source: "materials" as DashboardSection,
        title: material.name,
        subtitle: this.getCategoryLabel(material.category),
        detail: `${material.activeLots ?? 0} active lots · ${material.quarantineLots ?? 0} quarantine lots`,
        timestampLabel: "Live stock",
        route: ["/raw-materials", String(material.id)],
        tone: this.getCategoryTone(material.category),
        sortKey: materials.length - index,
      }));
    }

    if (this.selectedSection === "lots") {
      return [...this.lots]
        .sort(
          (left, right) =>
            this.parseDate(right.receivedDateUtc).getTime() -
            this.parseDate(left.receivedDateUtc).getTime(),
        )
        .slice(0, 8)
        .map((lot, index) => ({
          id: `lot-${lot.id}`,
          source: "lots" as DashboardSection,
          title: lot.lotNumber,
          subtitle: lot.rawMaterialName,
          detail: `${lot.status} · ${lot.quantity} ${lot.unit}`,
          timestampLabel: this.formatTimestamp(lot.receivedDateUtc),
          route: ["/lots", String(lot.id)],
          tone: this.getLotTone(lot.status),
          sortKey: this.parseDate(lot.receivedDateUtc).getTime() - index,
        }));
    }

    if (this.selectedSection === "bmr") {
      return [...this.bmrs]
        .sort(
          (left, right) =>
            this.parseDate(right.createdAt as unknown as string).getTime() -
            this.parseDate(left.createdAt as unknown as string).getTime(),
        )
        .slice(0, 8)
        .map((bmr, index) => ({
          id: `bmr-${bmr.id}`,
          source: "bmr" as DashboardSection,
          title: bmr.batchNumber,
          subtitle: `${bmr.recipeName} · ${bmr.productionLineName}`,
          detail: `${bmr.completedSteps}/${bmr.totalSteps} steps · ${this.getBmrStatusLabel(bmr.status)}`,
          timestampLabel: this.formatTimestamp(
            bmr.createdAt as unknown as string,
          ),
          route: ["/bmr", bmr.id],
          tone: this.getBmrTone(bmr.status),
          sortKey:
            this.parseDate(bmr.createdAt as unknown as string).getTime() -
            index,
        }));
    }

    if (this.selectedSection === "qc") {
      return [...this.qcTests]
        .sort(
          (left, right) =>
            this.parseDate(right.createdAt).getTime() -
            this.parseDate(left.createdAt).getTime(),
        )
        .slice(0, 8)
        .map((test, index) => ({
          id: `qc-${test.id}`,
          source: "qc" as DashboardSection,
          title: test.testObject,
          subtitle: test.testObjectType,
          detail: `${test.createdBy} · ${test.status}`,
          timestampLabel: this.formatTimestamp(test.createdAt),
          route: ["/qc", String(test.id)],
          tone: this.getQcTone(test.status),
          sortKey: this.parseDate(test.createdAt).getTime() - index,
        }));
    }

    if (this.selectedSection === "audit") {
      return [...this.auditEntries]
        .sort(
          (left, right) =>
            this.parseDate(right.tijdstip).getTime() -
            this.parseDate(left.tijdstip).getTime(),
        )
        .slice(0, 8)
        .map((entry, index) => ({
          id: `audit-${entry.id}`,
          source: "audit" as DashboardSection,
          title: `${entry.actie} ${entry.entiteitType}`.trim(),
          subtitle: `Record ${entry.entiteitId}`,
          detail: `${entry.gebruikerId} · ${entry.ipAdres || "Unknown IP"}`,
          timestampLabel: this.formatTimestamp(entry.tijdstip),
          route: ["/audit-trail"],
          tone: this.getAuditTone(entry.actie),
          sortKey: this.parseDate(entry.tijdstip).getTime() - index,
        }));
    }

    const allItems: TimelineItem[] = [
      ...this.lots.map((lot) => ({
        id: `all-lot-${lot.id}`,
        source: "lots" as DashboardSection,
        title: `Lot ${lot.lotNumber}`,
        subtitle: lot.rawMaterialName,
        detail: `${lot.status} · ${lot.quantity} ${lot.unit}`,
        timestampLabel: this.formatTimestamp(lot.receivedDateUtc),
        route: ["/lots", String(lot.id)],
        tone: this.getLotTone(lot.status),
        sortKey: this.parseDate(lot.receivedDateUtc).getTime(),
      })),
      ...this.bmrs.map((bmr) => ({
        id: `all-bmr-${bmr.id}`,
        source: "bmr" as DashboardSection,
        title: `BMR ${bmr.batchNumber}`,
        subtitle: `${bmr.recipeName} · ${bmr.productionLineName}`,
        detail: `${bmr.completedSteps}/${bmr.totalSteps} steps · ${this.getBmrStatusLabel(bmr.status)}`,
        timestampLabel: this.formatTimestamp(
          bmr.createdAt as unknown as string,
        ),
        route: ["/bmr", bmr.id],
        tone: this.getBmrTone(bmr.status),
        sortKey: this.parseDate(bmr.createdAt as unknown as string).getTime(),
      })),
      ...this.qcTests.map((test) => ({
        id: `all-qc-${test.id}`,
        source: "qc" as DashboardSection,
        title: test.testObject,
        subtitle: test.testObjectType,
        detail: `${test.createdBy} · ${test.status}`,
        timestampLabel: this.formatTimestamp(test.createdAt),
        route: ["/qc", String(test.id)],
        tone: this.getQcTone(test.status),
        sortKey: this.parseDate(test.createdAt).getTime(),
      })),
      ...this.auditEntries.map((entry) => ({
        id: `all-audit-${entry.id}`,
        source: "audit" as DashboardSection,
        title: `${entry.actie} ${entry.entiteitType}`.trim(),
        subtitle: `Record ${entry.entiteitId}`,
        detail: `${entry.gebruikerId} · ${entry.ipAdres || "Unknown IP"}`,
        timestampLabel: this.formatTimestamp(entry.tijdstip),
        route: ["/audit-trail"],
        tone: this.getAuditTone(entry.actie),
        sortKey: this.parseDate(entry.tijdstip).getTime(),
      })),
    ];

    return allItems
      .sort((left, right) => right.sortKey - left.sortKey)
      .slice(0, 10);
  }

  private withPercentages(
    buckets: Array<Omit<DistributionBucket, "percent">>,
  ): DistributionBucket[] {
    const total = buckets.reduce((sum, bucket) => sum + bucket.count, 0);

    return buckets.map((bucket) => ({
      ...bucket,
      percent: total === 0 ? 0 : Math.round((bucket.count / total) * 100),
    }));
  }

  private countLotsByStatus(status: LotStatus): number {
    return this.lots.filter((lot) => lot.status === status).length;
  }

  private countBmrByStatus(status: BmrStatus): number {
    return this.bmrs.filter((bmr) => bmr.status === status).length;
  }

  private countQcByResult(result: QcResult): number {
    return this.qcTests.filter((test) => test.status === result).length;
  }

  private countAuditByAction(action: string): number {
    const target = action.trim().toLowerCase();
    return this.auditEntries.filter(
      (entry) => entry.actie.trim().toLowerCase() === target,
    ).length;
  }

  private getCategoryLabel(category: string): string {
    switch (category) {
      case RawMaterialCategory.ActivePharmaceuticalIngredient:
        return "API";
      case RawMaterialCategory.Excipient:
        return "Excipient";
      case RawMaterialCategory.Packaging:
        return "Packaging";
      case RawMaterialCategory.Solvent:
        return "Solvent";
      default:
        return "Other";
    }
  }

  private getCategoryTone(category: string): BucketTone {
    switch (category) {
      case RawMaterialCategory.ActivePharmaceuticalIngredient:
        return "blue";
      case RawMaterialCategory.Excipient:
        return "green";
      case RawMaterialCategory.Packaging:
        return "amber";
      case RawMaterialCategory.Solvent:
        return "violet";
      default:
        return "slate";
    }
  }

  private getLotTone(status: LotStatus): BucketTone {
    switch (status) {
      case "Released":
        return "green";
      case "Quarantine":
        return "amber";
      case "Rejected":
        return "rose";
      default:
        return "slate";
    }
  }

  private getBmrTone(status: BmrStatus): BucketTone {
    switch (status) {
      case BmrStatus.Completed:
        return "green";
      case BmrStatus.InQc:
        return "violet";
      case BmrStatus.Rejected:
        return "rose";
      default:
        return "blue";
    }
  }

  private getQcTone(result: QcResult): BucketTone {
    switch (result) {
      case "Passed":
        return "green";
      case "OOS":
        return "rose";
      default:
        return "amber";
    }
  }

  private getAuditTone(action: string): BucketTone {
    const normalized = action.trim().toLowerCase();

    if (normalized === "create") {
      return "blue";
    }

    if (normalized === "statuschange") {
      return "violet";
    }

    if (normalized === "delete") {
      return "rose";
    }

    return "slate";
  }

  private getBmrStatusLabel(status: BmrStatus): string {
    switch (status) {
      case BmrStatus.Completed:
        return "Completed";
      case BmrStatus.Rejected:
        return "Rejected";
      case BmrStatus.InQc:
        return "In QC";
      default:
        return "In progress";
    }
  }

  private parseDate(value: string | Date | null | undefined): Date {
    if (!value) {
      return new Date(0);
    }

    const parsed = new Date(value);
    return Number.isNaN(parsed.getTime()) ? new Date(0) : parsed;
  }

  private formatTimestamp(value: string | Date | null | undefined): string {
    const parsed = this.parseDate(value);
    if (parsed.getTime() === 0) {
      return "Live";
    }

    return parsed.toLocaleString("en-GB", {
      day: "2-digit",
      month: "short",
      hour: "2-digit",
      minute: "2-digit",
    });
  }
}
