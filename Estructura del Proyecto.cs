Validemos que este codigo funcione correctamente

import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './modules/auth/components/login/login.component';
import { ConsultaTarjetaComponent } from './modules/tarjetas/components/consulta-tarjeta/consulta-tarjeta.component';
import { authGuard } from './core/guards/auth.guard';



export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'tarjetas', component: ConsultaTarjetaComponent, canActivate: [authGuard], title: 'Tarjetas' },
  { path: '', component: ConsultaTarjetaComponent, canActivate: [authGuard] },
  { path: '**', component: ConsultaTarjetaComponent, canActivate: [authGuard] }

];

export const AppRoutingModule = RouterModule.forRoot(routes);
