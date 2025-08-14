import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { Subscription } from 'rxjs';

import { AuthService } from '../../../../app/core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    MatToolbarModule,
    MatIconModule,
    MatCardModule,
    MatButtonModule,
    MatMenuModule
  ],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit, OnDestroy {
  userName: string | null = null;
  private subscription = new Subscription();

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    // Guardamos la suscripción para liberarla luego
    const sub = this.authService.currentUser.subscribe(user => {
      this.userName = user?.activeDirectoryData?.nombreUsuario ?? null;
    });
    this.subscription.add(sub);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  logout(): void {
    this.userName = null;
    this.authService.logout();
  }

  // Si en el futuro usas sidenav, aquí va el toggle
  toggleSidenav(): void {}
}


<mat-toolbar class="navBar" color="primary">
  <mat-toolbar-row class="Info">
    <span class="Info__text">Servicio Impresión Tarjetas Débito</span>

    @if (userName) {
      <div class="button_container">
        <button
          mat-button
          [matMenuTriggerFor]="userMenu"
          class="Info__buttons-user">
          {{ userName }}
        </button>

        <mat-menu #userMenu="matMenu">
          <button mat-menu-item (click)="logout()" class="Info__buttons-logout">
            <mat-icon>logout</mat-icon>
            <span>Logout</span>
          </button>
        </mat-menu>
      </div>
    }
  </mat-toolbar-row>
</mat-toolbar>
