import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Si está activa la sesión, puede pasar
  if (auth.sessionIsActive()) return true;

  // Mejor devolver UrlTree (no navegar imperativamente)
  return router.createUrlTree(['/login']);
};
