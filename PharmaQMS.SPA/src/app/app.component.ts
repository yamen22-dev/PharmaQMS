import { AsyncPipe, CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

type NavigationItem = {
  route: string;
  label: string;
  icon: string;
  allowedRoles?: string[];
};

const NAVIGATION_ITEMS: NavigationItem[] = [
  {
    route: "/dashboard",
    label: "Dashboard",
    icon: "📊",
  },
  {
    route: "/raw-materials",
    label: "Raw Materials & Lots",
    icon: "🏭",
    allowedRoles: ["QAManager", "WarehouseOperator"],
  },
  {
    route: "/bmr",
    label: "BMR",
    icon: "📝",
    allowedRoles: ["QAManager", "ProductionAnalyst"],
  },
  {
    route: "/qc",
    label: "Quality Control",
    icon: "✓",
    allowedRoles: ["QAManager", "QCAnalyst"],
  },
  {
    route: "/audit-trail",
    label: "Audit Trail",
    icon: "🔍",
    allowedRoles: ["QAManager", "Viewer"],
  },
];

@Component({
  selector: "app-root",
  standalone: true,
  imports: [CommonModule, RouterLink, RouterOutlet, AsyncPipe],
  templateUrl: "./app.component.html",
  styleUrl: "./app.component.css",
})
export class AppComponent {
  protected readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly session$ = this.authService.session$;
  protected readonly navigationItems = NAVIGATION_ITEMS;

  protected logout(): void {
    this.authService
      .logout()
      .subscribe(() => void this.router.navigateByUrl("/login"));
  }

  protected canShowNavigationItem(item: NavigationItem): boolean {
    if (!item.allowedRoles?.length) {
      return true;
    }

    return item.allowedRoles.some((role) => this.authService.hasRole(role));
  }
}
