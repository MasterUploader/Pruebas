Ahora necesito mejorar el navbar, el cual el nombre hace que el titulo no se mantenga centrado, y el menu que despliega para el logout aparece con una barra de desplazamiento cuando no la necesita, te dejo el codigo que considero que puedes requerir:


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
  private readonly subscription = new Subscription();

  constructor(private readonly authService: AuthService) {}

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


body {
    background-color: rgb(241, 239, 239);
    margin: 0;
    height: 100vh;
}


.navBar {
    width: 100%;
    height: 30px;
    background-color: #bd0909 !important;
    color: #fff;
}

.Info {
    width: 100%;
    display: flex;
    justify-content: space-evenly;
     align-items: center;
}

.Info__text {

    width: 80%;
    height: auto;
    font-size: 1.3rem;
    letter-spacing: 1.5px;
    text-align: center;
}

.Info__buttons-user,
.Info__buttons-logout {
    background-color: transparent;
    border: none;
    padding: 12px 15px;
    color: #fff !important;
    font-size: 0.8rem;
    cursor: pointer;
    transition: all .2s ease;

}

.Info__buttons-user:hover,
.Info__buttons-logout:hover {
    background-color: salmon;

}

.Info__buttons-logout {
    color: black !important;
    margin-left: 22px;
}

/* medias querys */
@media only screen and (max-width:992px) {

    .Info__text {
        width: 60%;
        font-size: 1.3rem;
        margin-top: 2px;
    }

}

@media only screen and (max-width:768px) {
    .navBar {
        width: 100%;

    }

    .Info__text {
        width: 50%;
        font-size: 1rem;
    }


}
