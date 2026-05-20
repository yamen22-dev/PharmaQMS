import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from "./core/guards/guest.guard";

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
    path: "**",
    redirectTo: "dashboard",
  },
];
