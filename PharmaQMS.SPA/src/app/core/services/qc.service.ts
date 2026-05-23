import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  QcTestSummary,
  QcTestDetail,
  CreateQcTestRequest,
  QcEligibleObjectsResponse,
} from "../models/qc-test.model";

@Injectable({ providedIn: "root" })
export class QcService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getQcTests(): Observable<QcTestSummary[]> {
    return this.http.get<QcTestSummary[]>(`${this.base}/qc-tests`);
  }

  getQcTest(id: number): Observable<QcTestDetail> {
    return this.http.get<QcTestDetail>(`${this.base}/qc-tests/${id}`);
  }

  createQcTest(request: CreateQcTestRequest): Observable<QcTestSummary> {
    return this.http.post<QcTestSummary>(`${this.base}/qc-tests`, request);
  }

  getEligibleObjects(): Observable<QcEligibleObjectsResponse> {
    return this.http.get<QcEligibleObjectsResponse>(
      `${this.base}/qc-tests/eligible-objects`,
    );
  }
}
