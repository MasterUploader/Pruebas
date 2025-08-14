Corrige el error 2, entragame el codigo ya corregido 

  import { Component, OnDestroy, OnInit, viewChild } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenav } from '@angular/material/sidenav';
import { MatCardModule } from '@angular/material/card';
import { AuthService } from '../../../../app/core/services/auth.service';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';

import { Subscription } from 'rxjs';

@Component({
    selector: 'app-navbar',
    imports: [MatToolbarModule, MatIconModule, MatSidenavModule, MatSidenav, MatCardModule, MatButtonModule, MatMenuModule],
    templateUrl: './navbar.component.html',
    styleUrl: './navbar.component.css'
})
export class NavbarComponent implements OnInit, OnDestroy {
  userName: string | null = null;
  toggleSidenav() { }
  private subscription: Subscription = new Subscription();

  constructor(private authService: AuthService) { }

  ngOnInit(): void {
    this.authService.currentUser.subscribe(user => {
      this.userName = user?.activeDirectoryData.nombreUsuario || null;
    });
  }
  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  logout(): void {
    this.userName = null;
    this.authService.logout();
  }
}






<mat-toolbar class="navBar" color="primary">
  <mat-toolbar-row class="Info">
    <span class="Info__text">Servicio Impresión Tarjetas Débito</span>
    @if (userName Info__buttons) {
      <div class="button_container">
        <button mat-button [matMenuTriggerFor]="userMenu" class="Info__buttons-user"> {{userName}}</button>
        <mat-menu #userMenu="matMenu">
          <button mat-menu-icon (click)="logout()" class="Info__buttons-logout">Logout</button>
        </mat-menu>
      </div>
    }
  </mat-toolbar-row>
</mat-toolbar>
