import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import {
  CreateRawMaterialRequest,
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
}
