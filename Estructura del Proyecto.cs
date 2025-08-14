Entregame el codigo con los cambios

import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideRouter, withHashLocation } from '@angular/router';
import { AuthInterceptor } from './core/services/auth.interceptor';
import { AppRoutingModule, routes } from './app.routes';
import { provideClientHydration } from '@angular/platform-browser';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { LocationStrategy, HashLocationStrategy } from '@angular/common';

//Nuevo
import { provideHttpClient, withInterceptors } from '@angular/common/http';

export const appConfig: ApplicationConfig = {
  providers: [provideRouter(routes, withHashLocation()),
  provideClientHydration(),
  provideAnimationsAsync(),
  provideHttpClient(withFetch()),
  importProvidersFrom(HttpClientModule, AppRoutingModule),
  {
    provide: HTTP_INTERCEPTORS,
    useClass: AuthInterceptor,
    multi: true,
  },
  { provide: LocationStrategy, useClass: HashLocationStrategy },]
};
