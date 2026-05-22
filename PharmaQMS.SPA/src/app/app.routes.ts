import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from "./core/guards/guest.guard";
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: "login",
    canActivate: [guestGuard],
    loadComponent: () =>
      import("./features/auth/login/login.component").then(
        (m) => m.LoginComponent,
      ),
  },
  {
    path: "dashboard",
    canActivate: [authGuard],
    loadComponent: () =>
      import("./features/dashboard/dashboard.component").then(
        (m) => m.DashboardComponent,
      ),
  },
  {
    path: "raw-materials/new",
    canActivate: [authGuard],
    loadComponent: () =>
      import("./features/raw-materials/raw-material-form/raw-material-form.component").then(
        (m) => m.RawMaterialFormComponent,
      ),
  },
  {
    path: "raw-materials",
    canActivate: [authGuard],
    loadComponent: () =>
      import("./features/raw-materials/raw-materials.component").then(
        (m) => m.RawMaterialsComponent,
      ),
  },
  {
    path: "raw-materials/:id",
    canActivate: [authGuard],
    loadComponent: () =>
      import("./features/raw-materials/raw-material-detail/raw-material-detail.component").then(
        (m) => m.RawMaterialDetailComponent,
      ),
  },
  {
    path: "",
    pathMatch: "full",
    redirectTo: "dashboard",
  },
  {
    path: "lots",
    canActivate: [authGuard],
    children: [
      {
        path: "",
        loadComponent: () =>
          import("./features/lots/lots-overview/lots-overview.component").then(
            (m) => m.LotsOverviewComponent,
          ),
      },
      {
        path: ":id",
        loadComponent: () =>
          import("./features/lots/lot-detail/lot-detail.component").then(
            (m) => m.LotDetailComponent,
          ),
      },
      {
        path: ":id/status",
        canActivate: [roleGuard(["QAManager"])],
        loadComponent: () =>
          import("./features/lots/lot-status/lot-status.component").then(
            (m) => m.LotStatusComponent,
          ),
      },
    ],
  },
  {
    // Lot aanmaken vanuit grondstof-context
    path: "raw-materials/:rawMaterialId/lots/new",
    canActivate: [authGuard, roleGuard(["QAManager", "WarehouseOperator"], "/raw-materials")],
    loadComponent: () =>
      import("./features/lots/lot-form/lot-form.component").then(
        (m) => m.LotFormComponent,
      ),
  },
  {
    path: "**",
    redirectTo: "dashboard",
  },
];
