import { CommonModule, isPlatformBrowser } from '@angular/common';
import { Component, ElementRef, QueryList, ViewChildren, ChangeDetectorRef, PLATFORM_ID, inject } from '@angular/core';
import { SidebarService } from '../../core/services/sidebar.service';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { combineLatest, Subscription } from 'rxjs';
import { IconComponent } from '../../components/icon/icon.component';

type NavItem = {
  name: string;
  icon: string;
  path?: string;
  new?: boolean;
  subItems?: { name: string; path: string; new?: boolean }[];
};

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    IconComponent,
  ],
  templateUrl: './app-sidebar.component.html',
})
export class AppSidebarComponent {

  // Main nav items - customized for R2.ShopNet
  navItems: NavItem[] = [
    {
      icon: "dashboard",
      name: "Dashboard",
      path: "/dashboard",
    },
    {
      icon: "person",
      name: "Users",
      subItems: [
        { name: "All Users", path: "/users" },
        { name: "Roles & Permissions", path: "/users/roles" },
      ],
    },
    {
      icon: "category",
      name: "Catalog",
      subItems: [
        { name: "Products", path: "/catalog/products" },
        { name: "Categories", path: "/catalog/categories" },
        { name: "Inventory", path: "/catalog/inventory" },
      ],
    },
    {
      icon: "shopping_bag",
      name: "Orders",
      subItems: [
        { name: "All Orders", path: "/orders" },
        { name: "Pending", path: "/orders/pending" },
        { name: "Completed", path: "/orders/completed" },
      ],
    },
    {
      icon: "calendar_month",
      name: "Reports",
      path: "/reports",
    },
  ];

  // Others nav items
  othersItems: NavItem[] = [
    {
      icon: "analytics",
      name: "Analytics",
      path: "/analytics",
    },
    {
      icon: "settings",
      name: "Settings",
      subItems: [
        { name: "General", path: "/settings" },
        { name: "Configuration", path: "/settings/configuration" },
      ],
    },
  ];

  openSubmenu: string | null | number = null;
  subMenuHeights: { [key: string]: number } = {};
  @ViewChildren('subMenu') subMenuRefs!: QueryList<ElementRef>;

  readonly isExpanded$;
  readonly isMobileOpen$;
  readonly isHovered$;

  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);
  private subscription: Subscription = new Subscription();

  constructor(
    public sidebarService: SidebarService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.isExpanded$ = this.sidebarService.isExpanded$;
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
    this.isHovered$ = this.sidebarService.isHovered$;
  }

  ngOnInit() {
    // Subscribe to router events
    this.subscription.add(
      this.router.events.subscribe(event => {
        if (event instanceof NavigationEnd) {
          this.setActiveMenuFromRoute(this.router.url);
        }
      })
    );

    // Subscribe to combined observables to manage submenu state
    this.subscription.add(
      combineLatest([this.isExpanded$, this.isMobileOpen$, this.isHovered$]).subscribe(
        ([isExpanded, isMobileOpen, isHovered]) => {
          if (!isExpanded && !isMobileOpen && !isHovered) {
            this.cdr.detectChanges();
          }
        }
      )
    );

    // Initial load
    this.setActiveMenuFromRoute(this.router.url);
  }

  ngOnDestroy() {
    // Clean up subscriptions
    this.subscription.unsubscribe();
  }

  isActive(path: string): boolean {
    return this.router.url === path;
  }

  toggleSubmenu(section: string, index: number) {
    const key = `${section}-${index}`;

    if (this.openSubmenu === key) {
      this.openSubmenu = null;
      this.subMenuHeights[key] = 0;
    } else {
      this.openSubmenu = key;

      setTimeout(() => {
        const el = document.getElementById(key);
        if (el) {
          this.subMenuHeights[key] = el.scrollHeight;
          this.cdr.detectChanges();
        }
      });
    }
  }

  onSidebarMouseEnter() {
    this.isExpanded$.subscribe(expanded => {
      if (!expanded) {
        this.sidebarService.setHovered(true);
      }
    }).unsubscribe();
  }

  private setActiveMenuFromRoute(currentUrl: string) {
    const menuGroups = [
      { items: this.navItems, prefix: 'main' },
      { items: this.othersItems, prefix: 'others' },
    ];

    menuGroups.forEach(group => {
      group.items.forEach((nav, i) => {
        if (nav.subItems) {
          nav.subItems.forEach(subItem => {
            if (currentUrl === subItem.path) {
              const key = `${group.prefix}-${i}`;
              this.openSubmenu = key;

              if (this.isBrowser) {
                setTimeout(() => {
                  const el = document.getElementById(key);
                  if (el) {
                    this.subMenuHeights[key] = el.scrollHeight;
                    this.cdr.detectChanges();
                  }
                });
              }
            }
          });
        }
      });
    });
  }

  onSubmenuClick() {
    this.isMobileOpen$.subscribe(isMobile => {
      if (isMobile) {
        this.sidebarService.setMobileOpen(false);
      }
    }).unsubscribe();
  }
}
