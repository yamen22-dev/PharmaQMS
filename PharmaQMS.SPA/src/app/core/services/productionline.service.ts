import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { inject } from "@angular/core/primitives/di";
import { environment } from "../../../environments/environment.development";
import { ProductionLineResponse } from "../models/productionLine.model";
import { Observable } from "rxjs";

@Injectable({ providedIn: "root" })
export class ProductionLineService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getProductionLines(): Observable<ProductionLineResponse[]> {
    return this.http.get<ProductionLineResponse[]>(
      this.baseUrl + "/bmr/production-lines",
    );
  }
}
