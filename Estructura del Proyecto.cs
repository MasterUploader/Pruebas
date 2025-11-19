import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router, ActivatedRoute, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'ui-breadcrumb',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
  <nav class="breadcrumb container" aria-label="breadcrumb">
    <a routerLink="/inicio">Inicio</a>
    <ng-container *ngFor="let c of crumbs()">
      <span>/</span>
      <a *ngIf="!c.last" [routerLink]="c.url">{{c.label}}</a>
      <span *ngIf="c.last" class="current">{{c.label}}</span>
    </ng-container>
  </nav>
  `
})
export class BreadcrumbComponent {
  private readonly router = inject(Router);
  private readonly rootRoute = inject(ActivatedRoute);
  private readonly _crumbs = signal<{label:string; url:string; last:boolean}[]>([]);
  crumbs = this._crumbs.asReadonly();

  constructor(){
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => {
        const acc: {label:string; url:string; last:boolean}[] = [];
        let route: ActivatedRoute | null = this.rootRoute.root;
        let url = '';

        while (route) {
          const childRoute: ActivatedRoute | null = (route.firstChild as ActivatedRoute | null);
          if (!childRoute) break;
          route = childRoute;

          const rc = childRoute.routeConfig;
          if (!rc) continue;

          const path: string = rc.path ?? '';
          url += path ? `/${path}` : '';

          const data: Record<string, unknown> = (rc.data ?? {}) as Record<string, unknown>;
          const label: string = ((data['breadcrumb'] as string | undefined) ?? path) || 'Inicio';

          if (label) acc.push({ label, url, last: false });
        }

        if (acc.length) acc[acc.length - 1].last = true;
        this._crumbs.set(acc);
      });
  }
}
