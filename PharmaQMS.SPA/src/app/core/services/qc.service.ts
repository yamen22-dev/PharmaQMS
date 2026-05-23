import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { Observable } from "rxjs";
import { map } from "rxjs/operators";
import { environment } from "../../../environments/environment";
import {
  QcTestSummary,
  QcTestDetail,
  CreateQcTestRequest,
  QcEligibleObjectsResponse,
  SubmitQcTestResultsRequest,
  SubmitQcTestResultsResponse,
} from "../models/qc-test.model";

@Injectable({ providedIn: "root" })
export class QcService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getQcTests(): Observable<QcTestSummary[]> {
    return this.http
      .get<unknown>(`${this.base}/qc-tests`)
      .pipe(map((payload) => this.mapSummaryList(payload)));
  }

  getQcTest(id: number): Observable<QcTestDetail> {
    return this.http
      .get<unknown>(`${this.base}/qc-tests/${id}`)
      .pipe(map((payload) => this.mapDetail(payload)));
  }

  createQcTest(request: CreateQcTestRequest): Observable<QcTestSummary> {
    return this.http.post<QcTestSummary>(`${this.base}/qc-tests`, request);
  }

  submitQcTestResults(
    id: number,
    request: SubmitQcTestResultsRequest,
  ): Observable<SubmitQcTestResultsResponse> {
    return this.http
      .post<unknown>(`${this.base}/qc-tests/${id}/results`, request)
      .pipe(map((payload) => this.mapSubmitResponse(payload)));
  }

  getEligibleObjects(): Observable<QcEligibleObjectsResponse> {
    return this.http.get<QcEligibleObjectsResponse>(
      `${this.base}/qc-tests/eligible-objects`,
    );
  }

  private mapSummaryList(payload: unknown): QcTestSummary[] {
    if (Array.isArray(payload)) {
      return payload.map((item) => this.mapSummary(item));
    }

    if (payload && typeof payload === "object") {
      const data = payload as {
        items?: unknown[];
        tests?: unknown[];
        data?: unknown[];
      };
      const items = data.items ?? data.tests ?? data.data;
      if (Array.isArray(items)) {
        return items.map((item) => this.mapSummary(item));
      }
    }

    return [];
  }

  private mapDetail(payload: unknown): QcTestDetail {
    const detail = payload as Record<string, unknown>;
    const summary = this.mapSummary(detail);
    return {
      ...summary,
      testObjectLabel:
        this.asString(detail["testObjectLabel"]) || summary.testObject,
      parameters: Array.isArray(detail["parameters"])
        ? detail["parameters"].map((item) => this.mapParameter(item))
        : [],
    };
  }

  private mapSummary(item: unknown): QcTestSummary {
    const source = item as Record<string, unknown>;
    const testObjectType = this.asString(source["testObjectType"]) || "QC";
    const testObjectId = this.asNumber(source["testObjectId"]);
    const testObjectLabel =
      this.asString(source["testObject"]) ||
      this.asString(source["testObjectLabel"]) ||
      (testObjectId != null
        ? `${testObjectType} #${testObjectId}`
        : testObjectType);

    return {
      id: this.asNumber(source["id"]) ?? 0,
      testObject: testObjectLabel,
      testObjectType,
      createdAt: this.asString(source["createdAt"]) || "",
      createdBy: this.asString(source["createdBy"]) || "",
      status: this.normalizeStatus(source["status"]),
    };
  }

  private mapParameter(item: unknown) {
    const source = item as Record<string, unknown>;
    return {
      id: this.asNumber(source["id"]) ?? 0,
      name: this.asString(source["name"]) || "",
      unit: this.asString(source["unit"]) || "",
      min: this.asNumber(source["min"]) ?? 0,
      max: this.asNumber(source["max"]) ?? 0,
      measuredValue: this.asNumber(source["measuredValue"]),
      isWithinSpecification: Boolean(source["isWithinSpecification"]),
    };
  }

  private mapSubmitResponse(payload: unknown): SubmitQcTestResultsResponse {
    const source = payload as Record<string, unknown>;
    return {
      id: this.asNumber(source["id"]) ?? 0,
      testObjectType: this.asString(source["testObjectType"]) || "",
      testObjectId: this.asNumber(source["testObjectId"]) ?? 0,
      testObjectLabel: this.asString(source["testObjectLabel"]) || "",
      status: this.normalizeStatus(source["status"]),
      message: this.asString(source["message"]) || "",
      coaStarted: Boolean(source["coaStarted"]),
      notificationQueued: Boolean(source["notificationQueued"]),
      parameters: Array.isArray(source["parameters"])
        ? source["parameters"].map((item) => this.mapParameter(item))
        : [],
    };
  }

  private normalizeStatus(status: unknown): QcTestSummary["status"] {
    if (typeof status === "number") {
      switch (status) {
        case 2:
          return "Passed";
        case 3:
          return "OOS";
        default:
          return "In progress";
      }
    }

    switch ((this.asString(status) || "").trim().toLowerCase()) {
      case "0":
      case "1":
        return "In progress";
      case "2":
        return "Passed";
      case "3":
        return "OOS";
      case "approved":
      case "passed":
        return "Passed";
      case "rejected":
      case "oos":
        return "OOS";
      default:
        return "In progress";
    }
  }

  private asString(value: unknown): string {
    return typeof value === "string" ? value : "";
  }

  private asNumber(value: unknown): number | null {
    return typeof value === "number" && Number.isFinite(value) ? value : null;
  }
}
