import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from "./core/guards/guest.guard";
import { roleGuard } from "./core/guards/role.guard";

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
    path: "raw-materials",
    canActivate: [authGuard, roleGuard(["QAManager", "WarehouseOperator"])],
    loadComponent: () =>
      import("./features/raw-materials/raw-materials.component").then(
        (m) => m.RawMaterialsComponent,
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
