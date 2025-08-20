import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Evita acceder a /login si ya hay sesión activa.
 * Si la sesión está activa, redirige a /tarjetas.
 * Si no, permite ver el login.
 */
export const loginGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.sessionIsActive()
    ? router.createUrlTree(['/tarjetas'])
    : true;
};

import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './modules/auth/components/login/login.component';
import { ConsultaTarjetaComponent } from './modules/tarjetas/components/consulta-tarjeta/consulta-tarjeta.component';
import { authGuard } from './core/guards/auth.guard';
import { loginGuard } from './core/guards/login.guard'; // ⬅️ importa el nuevo guard

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [loginGuard], title: 'Iniciar sesión' },
  { path: 'tarjetas', component: ConsultaTarjetaComponent, canActivate: [authGuard], title: 'Tarjetas' },
  { path: '', pathMatch: 'full', redirectTo: 'tarjetas' },
  { path: '**', redirectTo: 'tarjetas' }
];

export const AppRoutingModule = RouterModule.forRoot(routes);
