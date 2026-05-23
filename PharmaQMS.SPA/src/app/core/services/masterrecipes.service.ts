import { Injectable, inject } from "@angular/core";
import { HttpClient, HttpParams } from "@angular/common/http";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { MasterRecipeSummaryResponse } from "../models/masterrecipe.model";
@Injectable({
  providedIn: "root",
})
export class MasterRecipeService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getApprovedRecipes(): Observable<MasterRecipeSummaryResponse[]> {
    return this.http.get<MasterRecipeSummaryResponse[]>(
      `${this.base}/bmr/master-recipe-approved`,
    );
  }
}
