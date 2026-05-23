// bmr.service.ts
import { Injectable } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import {
  BmrDetailResponse,
  BmrListQuery,
  BmrSummaryResponse,
  ConfirmStepRequest,
  CreateBmrRequest,
  PageResponse,
  VerifyStepRequest,
} from "../models/bmr.model";
import { environment } from "../../../environments/environment.development";

@Injectable({
  providedIn: "root",
})
export class BmrService {
  private readonly baseUrl = environment.apiBaseUrl + "/bmr";

  constructor(private http: HttpClient) {}

  /**
   * Get a paginated list of BMRs, optionally filtered by status.
   */
  getBmrs(query: BmrListQuery): Observable<PageResponse<BmrSummaryResponse>> {
    let params = new HttpParams()
      .set("Page", query.Page)
      .set("PageSize", query.PageSize);

    if (query.Status !== undefined) {
      params = params.set("Status", query.Status);
    }

    return this.http.get<PageResponse<BmrSummaryResponse>>(this.baseUrl, {
      params,
    });
  }

  /**
   * Get full detail of a single BMR by its ID.
   */
  getBmrById(id: string): Observable<BmrDetailResponse> {
    return this.http.get<BmrDetailResponse>(`${this.baseUrl}/${id}`);
  }

  /**
   * Create a new BMR from a master recipe.
   */
  createBmr(request: CreateBmrRequest): Observable<BmrDetailResponse> {
    return this.http.post<BmrDetailResponse>(`${this.baseUrl}/new`, request);
  }

  /**
   * Confirm (enter data for) a step in a BMR.
   * Moves the step to AwaitingVerification.
   */
  confirmStep(
    bmrId: string,
    stepId: string,
    request: ConfirmStepRequest,
  ): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/${bmrId}/steps/${stepId}/confirm`,
      request,
    );
  }

  /**
   * Verify a step in a BMR using a password.
   * Moves the step to Verified.
   */
  verifyStep(
    bmrId: string,
    stepId: string,
    request: VerifyStepRequest,
  ): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/${bmrId}/steps/${stepId}/verify`,
      request,
    );
  }

  /**
   * Complete a BMR (all steps verified).
   */
  completeBmr(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/complete`, {});
  }

  /**
   * Reject a BMR.
   */
  rejectBmr(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/reject`, {});
  }
}
