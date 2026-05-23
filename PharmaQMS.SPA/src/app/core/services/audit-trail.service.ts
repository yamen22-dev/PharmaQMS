import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { map, Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { AuditTrailEntry } from "../models/audit-trail.model";

@Injectable({ providedIn: "root" })
export class AuditTrailService {
  private readonly http = inject(HttpClient);

  getAuditTrail(): Observable<AuditTrailEntry[]> {
    return this.http
      .get<unknown>(`${environment.apiBaseUrl}/audit-trail`)
      .pipe(map((payload) => this.mapEntries(payload)));
  }

  private mapEntries(payload: unknown): AuditTrailEntry[] {
    if (Array.isArray(payload)) {
      return payload.map((item) => this.mapEntry(item));
    }

    if (payload && typeof payload === "object") {
      const data = payload as { items?: unknown[]; data?: unknown[] };
      const items = data.items ?? data.data;
      if (Array.isArray(items)) {
        return items.map((item) => this.mapEntry(item));
      }
    }

    return [];
  }

  private mapEntry(item: unknown): AuditTrailEntry {
    const source = item as Record<string, unknown>;

    return {
      id: this.asNumber(source["id"]) ?? 0,
      tijdstip: this.asString(source["tijdstip"]) || "",
      gebruikerId: this.asString(source["gebruikerId"]) || "",
      actie: this.asString(source["actie"]) || "",
      entiteitType: this.asString(source["entiteitType"]) || "",
      entiteitId: this.asString(source["entiteitId"]) || "",
      oudWaarde: this.asNullableString(source["oudWaarde"]),
      nieuweWaarde: this.asNullableString(source["nieuweWaarde"]),
      ipAdres: this.asString(source["ipAdres"]) || "",
    };
  }

  private asString(value: unknown): string {
    return typeof value === "string" ? value : "";
  }

  private asNullableString(value: unknown): string | null {
    return typeof value === "string" && value.length > 0 ? value : null;
  }

  private asNumber(value: unknown): number | null {
    return typeof value === "number" && Number.isFinite(value) ? value : null;
  }
}