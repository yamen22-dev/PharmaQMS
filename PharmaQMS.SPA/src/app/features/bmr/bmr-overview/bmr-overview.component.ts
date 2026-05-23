import { CommonModule } from "@angular/common";

import { Component, OnInit, inject } from "@angular/core";
import { RouterModule } from "@angular/router";
import { FormsModule } from "@angular/forms";
import { BmrService } from "../../../core/services/bmr.service";
import { BmrSummaryResponse } from "../../../core/models/bmr.model";

@Component({
  selector: "app-bmr-overview",
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: './bmr-overview.component.html',
})
export class BmrOverviewComponent implements OnInit {
  ngOnInit(): void {
      throw new Error("Method not implemented.");
  }
  private readonly bmrService = inject(BmrService);
  bmrSummaries: BmrSummaryResponse[] = [];
  filtered: BmrSummaryResponse[] = [];
  loading = true;
}