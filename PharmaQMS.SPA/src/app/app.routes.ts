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
    canActivate: [
      authGuard,
      roleGuard(["QAManager", "WarehouseOperator"], "/raw-materials"),
    ],
    loadComponent: () =>
      import("./features/lots/lot-form/lot-form.component").then(
        (m) => m.LotFormComponent,
      ),
  },
  // BMR routes
  {
    path: "bmr",
    canActivate: [authGuard],
    children: [
      {
        path: "",
        loadComponent: () =>
          import("./features/bmr/bmr-overview/bmr-overview.component").then(
            (m) => m.BmrOverviewComponent,
          ),
      },
      {
        path: "new",
        canActivate: [roleGuard(["QAManager", "ProductionOperator"], "/bmr")],
        loadComponent: () =>
          import("./features/bmr/bmr-create/bmr-create.component").then(
            (m) => m.BmrCreateComponent,
          ),
      },
      {
        path: ":id",
        loadComponent: () =>
          import("./features/bmr/bmr-detail/bmr-detail.component").then(
            (m) => m.BmrDetailComponent,
          ),
      },
      {
        path: ":id/steps",
        canActivate: [roleGuard(["QAManager", "ProductionOperator"], "/bmr")],
        loadComponent: () =>
          import("./features/bmr/bmr-step-confirm/bmr-step-confirm.component").then(
            (m) => m.BmrStepConfirmationComponent,
          ),
      },
      {
        path: ":bmrId/steps/:stepId/verify",
        canActivate: [roleGuard(["QAManager"], "/bmr")],
        loadComponent: () =>
          import("./features/bmr/bmr-step-verification/bmr-step-verification.component").then(
            (m) => m.BmrStepVerificationComponent,
          ),
      },
    ],
  },
  {
    path: "qc",
    canActivate: [authGuard],
    children: [
      {
        path: "",
        loadComponent: () =>
          import("./features/qc/qc-overview/qc-overview.component").then(
            (m) => m.QcOverviewComponent,
          ),
      },
      {
        path: "new",
        canActivate: [roleGuard(["QCAnalyst", "QAManager"], "/qc")],
        loadComponent: () =>
          import("./features/qc/qc-create/qc-create.component").then(
            (m) => m.QcCreateComponent,
          ),
      },
      {
        path: ":id",
        canActivate: [roleGuard(["QCAnalyst", "QAManager"], "/qc")],
        loadComponent: () =>
          import("./features/qc/qc-detail/qc-detail.component").then(
            (m) => m.QcDetailComponent,
          ),
      },
    ],
  },
  {
    path: "**",
    redirectTo: "dashboard",
  },
];
