// src/app/core/services/lot.service.ts

import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  LotSummary,
  LotDetail,
  CreateLotRequest,
  ChangeLotStatusRequest,
  LotStatusChangedResponse,
} from "../models/lot.model";

@Injectable({ providedIn: "root" })
export class LotService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  // UC-02e
  getLots(filters?: {
    status?: string;
    rawMaterialId?: number;
  }): Observable<LotSummary[]> {
    let params = new HttpParams();
    if (filters?.status) params = params.set("status", filters.status);
    if (filters?.rawMaterialId != null)
      params = params.set("rawMaterialId", filters.rawMaterialId.toString());
    return this.http.get<LotSummary[]>(`${this.base}/lots`, { params });
  }

  // UC-02f
  getLotById(id: number): Observable<LotDetail> {
    return this.http.get<LotDetail>(`${this.base}/lots/${id}`);
  }

  // UC-02d
  createLot(
    rawMaterialId: number,
    request: CreateLotRequest,
  ): Observable<LotDetail> {
    return this.http.post<LotDetail>(
      `${this.base}/raw-materials/${rawMaterialId}/lots`,
      request,
    );
  }

  // UC-02g
  changeLotStatus(
    id: number,
    request: ChangeLotStatusRequest,
  ): Observable<LotStatusChangedResponse> {
    return this.http.post<LotStatusChangedResponse>(
      `${this.base}/lots/${id}/status`,
      request,
    );
  }
}
