Revisa si el guard esta bien y si asegura que cuando la sesion venza se cierre el login

import { CanActivateFn, Router, RouterModule } from '@angular/router';
import { NgModule , inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { routes } from '../../app.routes';

export const authGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  if(authService.sessionIsActive()){
    return true;
  }else{
    router.navigateByUrl('/login');
    return false;
  }
};

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule{}
