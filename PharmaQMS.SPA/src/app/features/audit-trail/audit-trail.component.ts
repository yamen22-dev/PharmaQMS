import { CommonModule } from "@angular/common";
import { ChangeDetectorRef, Component, OnInit, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { forkJoin, of } from "rxjs";
import { AuditTrailEntry } from "../../core/models/audit-trail.model";
import { AuthService } from "../../core/services/auth.service";
import { AuditTrailService } from "../../core/services/audit-trail.service";

interface AuditTrailViewModel extends AuditTrailEntry {
  gebruikerNaam: string;
}

@Component({
  selector: "app-audit-trail",
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: "./audit-trail.component.html",
  styleUrl: "./audit-trail.component.css",
})
export class AuditTrailComponent implements OnInit {
  private readonly auditTrailService = inject(AuditTrailService);
  private readonly authService = inject(AuthService);
  private readonly cdr = inject(ChangeDetectorRef);

  loading = true;
  entries: AuditTrailViewModel[] = [];
  filteredEntries: AuditTrailViewModel[] = [];
  paginatedEntries: AuditTrailViewModel[] = [];
  selectedEntry: AuditTrailViewModel | null = null;

  searchQuery = "";
  selectedEntityType = "";
  selectedAction = "";
  fromDate = "";
  toDate = "";
  currentPage = 1;
  pageSize = 10;

  entityTypes: string[] = [];
  actions: string[] = [];

  ngOnInit(): void {
    this.loadAuditTrail();
  }

  loadAuditTrail(): void {
    this.loading = true;

    this.auditTrailService.getAuditTrail().subscribe({
      next: (entries) => {
        const normalizedEntries = this.normalizeEntries(entries);
        const uniqueUserIds = [
          ...new Set(
            normalizedEntries
              .map((entry) => entry.gebruikerId.trim())
              .filter((userId) => userId.length > 0),
          ),
        ];

        const lookups = uniqueUserIds.map((userId) =>
          this.authService.getUserNameById(userId),
        );

        const resolvedUsers$ = lookups.length ? forkJoin(lookups) : of([]);

        resolvedUsers$.subscribe((userNames) => {
          const userDisplayMap = new Map<string, string>();

          uniqueUserIds.forEach((userId, index) => {
            userDisplayMap.set(userId, userNames[index] ?? userId);
          });

          this.entries = normalizedEntries.map((entry) => ({
            ...entry,
            gebruikerNaam:
              userDisplayMap.get(entry.gebruikerId.trim()) || entry.gebruikerId,
          }));

          this.entityTypes = this.uniqueSorted(
            this.entries.map((entry) => entry.entiteitType),
          );
          this.actions = this.uniqueSorted(
            this.entries.map((entry) => entry.actie),
          );

          this.applyFilters();
          this.loading = false;
          this.cdr.detectChanges();
        });
      },
      error: () => {
        this.entries = [];
        this.filteredEntries = [];
        this.loading = false;
      },
    });
  }

  applyFilters(): void {
    const search = this.searchQuery.trim().toLowerCase();
    const from = this.startOfDay(this.fromDate);
    const to = this.endOfDay(this.toDate);

    this.filteredEntries = this.entries.filter((entry) => {
      const entryDate = new Date(entry.tijdstip);

      const matchesEntityType = this.selectedEntityType
        ? entry.entiteitType === this.selectedEntityType
        : true;
      const matchesAction = this.selectedAction
        ? entry.actie === this.selectedAction
        : true;
      const matchesDateFrom = from ? entryDate >= from : true;
      const matchesDateTo = to ? entryDate <= to : true;
      const matchesSearch = search
        ? [
            entry.gebruikerNaam,
            entry.gebruikerId,
            entry.actie,
            entry.entiteitType,
            entry.entiteitId,
            entry.oudWaarde ?? "",
            entry.nieuweWaarde ?? "",
          ]
            .join(" ")
            .toLowerCase()
            .includes(search)
        : true;

      return (
        matchesEntityType &&
        matchesAction &&
        matchesDateFrom &&
        matchesDateTo &&
        matchesSearch
      );
    });

    this.currentPage = 1;
    this.updatePagination();
  }

  changePage(page: number): void {
    const nextPage = Math.min(Math.max(1, page), this.totalPages);
    if (nextPage === this.currentPage) {
      return;
    }

    this.currentPage = nextPage;
    this.updatePagination();
  }

  changePageSize(pageSize: number): void {
    this.pageSize = pageSize;
    this.currentPage = 1;
    this.updatePagination();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredEntries.length / this.pageSize));
  }

  get pageStart(): number {
    return this.filteredEntries.length === 0
      ? 0
      : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(
      this.currentPage * this.pageSize,
      this.filteredEntries.length,
    );
  }

  private updatePagination(): void {
    const start = (this.currentPage - 1) * this.pageSize;
    this.paginatedEntries = this.filteredEntries.slice(
      start,
      start + this.pageSize,
    );
    this.selectedEntry = this.paginatedEntries[0] ?? null;
  }

  selectEntry(entry: AuditTrailViewModel): void {
    this.selectedEntry = entry;
  }

  badgeClass(action: string): string {
    const normalized = action.trim().toLowerCase();

    if (normalized === "create") {
      return "badge badge-create";
    }

    if (normalized === "statuschange") {
      return "badge badge-status";
    }

    if (normalized === "delete") {
      return "badge badge-delete";
    }

    return "badge badge-neutral";
  }

  formatValue(value: string | null): string {
    return value?.trim() || "No value";
  }

  trackById(_: number, entry: AuditTrailViewModel): number {
    return entry.id;
  }

  private normalizeEntries(entries: AuditTrailEntry[]): AuditTrailEntry[] {
    return [...entries].sort(
      (left, right) =>
        new Date(right.tijdstip).getTime() - new Date(left.tijdstip).getTime(),
    );
  }

  private uniqueSorted(values: string[]): string[] {
    return [
      ...new Set(values.map((value) => value.trim()).filter(Boolean)),
    ].sort((left, right) => left.localeCompare(right, "en"));
  }

  private startOfDay(value: string): Date | null {
    if (!value) {
      return null;
    }

    const date = new Date(value);
    date.setHours(0, 0, 0, 0);
    return date;
  }

  private endOfDay(value: string): Date | null {
    if (!value) {
      return null;
    }

    const date = new Date(value);
    date.setHours(23, 59, 59, 999);
    return date;
  }
}
