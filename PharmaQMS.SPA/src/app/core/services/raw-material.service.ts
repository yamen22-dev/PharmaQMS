import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { map, Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  CreateRawMaterialRequest,
  LotStatus,
  RawMaterialDetail,
  RawMaterialOverview,
  RawMaterialResponse,
} from "../models/raw-material.model";

@Injectable({ providedIn: "root" })
export class RawMaterialService {
  private readonly http = inject(HttpClient);

  createRawMaterial(
    request: CreateRawMaterialRequest,
  ): Observable<RawMaterialResponse> {
    return this.http.post<RawMaterialResponse>(
      `${environment.apiBaseUrl}/raw-materials`,
      request,
    );
  }

  getRawMaterials(): Observable<RawMaterialOverview[]> {
    return this.http
      .get<RawMaterialOverview[]>(`${environment.apiBaseUrl}/raw-materials`)
      .pipe(
        map((items: any) => {
          console.log(items, "raw-materials service.");
          const list = this.normalizeCollection(items);
          return list.map((item: any) => ({
            id: item.id ?? item.Id ?? 0,
            name: item.name ?? item.Name ?? "",
            pharmaceuticalApi:
              item.pharmaceuticalApi ?? item.PharmaceuticalApi ?? "",
            category: item.category ?? item.Category ?? "",
            unit: item.unit ?? item.Unit ?? "",
            activeLots: item.activeLots ?? item.ActiveLots ?? 0,
            quarantineLots: item.quarantineLots ?? item.QuarantineLots ?? 0,
          }));
        }),
      );
  }

  getRawMaterialById(id: number): Observable<RawMaterialDetail> {
    return this.http
      .get<RawMaterialDetail>(`${environment.apiBaseUrl}/raw-materials/${id}`)
      .pipe(
        map((item: any) => {
          const lotsSource =
            item?.lots ??
            item?.Lots ??
            item?.lots?.$values ??
            item?.Lots?.$values ??
            [];
          const lots = this.normalizeCollection(lotsSource).map((lot: any) => ({
            id: lot.id ?? lot.Id ?? 0,
            lotNumber: lot.lotNumber ?? lot.LotNumber ?? "",
            status: (lot.status ?? lot.Status ?? "") as LotStatus,
          }));

          return {
            id: item?.id ?? item?.Id ?? 0,
            name: item?.name ?? item?.Name ?? "",
            pharmaceuticalApi:
              item?.pharmaceuticalApi ?? item?.PharmaceuticalApi ?? "",
            category: item?.category ?? item?.Category ?? "",
            unit: item?.unit ?? item?.Unit ?? "",
            supplier: item?.supplier ?? item?.Supplier ?? "",
            minSpecificationLimit:
              item?.minSpecificationLimit ?? item?.MinSpecificationLimit ?? 0,
            maxSpecificationLimit:
              item?.maxSpecificationLimit ?? item?.MaxSpecificationLimit ?? 0,
            notes: item?.notes ?? item?.Notes ?? "",
            lots,
          } as RawMaterialDetail;
        }),
      );
  }

  private normalizeCollection(source: any): any[] {
    if (Array.isArray(source)) {
      return source;
    }

    if (!source || typeof source !== "object") {
      return [];
    }

    const candidates = [
      source.items,
      source.Items,
      source.data,
      source.Data,
      source.value,
      source.Value,
      source.result,
      source.Result,
      source.$values,
      source.Values,
    ];

    for (const candidate of candidates) {
      if (Array.isArray(candidate)) {
        return candidate;
      }
    }

    return [];
  }
}
