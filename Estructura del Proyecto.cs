// app.config.ts
import { ApplicationConfig } from '@angular/core';
import { provideRouter, withHashLocation } from '@angular/router';
import { routes } from './app.routes';

import { provideClientHydration } from '@angular/platform-browser';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { LocationStrategy, HashLocationStrategy } from '@angular/common';

// ✅ HttpClient moderno (sin HttpClientModule)
import {
  provideHttpClient,
  withFetch,
  withInterceptorsFromDi, // toma interceptores registrados vía DI
  HTTP_INTERCEPTORS
} from '@angular/common/http';

import { AuthInterceptor } from './core/services/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    // Router standalone con hash (#)
    provideRouter(routes, withHashLocation()),

    // Hidratación/animaciones (opcional)
    provideClientHydration(),
    provideAnimationsAsync(),

    // ✅ HttpClient (sin HttpClientModule) + uso de fetch + interceptores DI
    provideHttpClient(
      withFetch(),
      withInterceptorsFromDi()
    ),

    // Interceptor basado en clase (se inyecta vía DI)
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },

    // Estrategia de navegación con hash
    { provide: LocationStrategy, useClass: HashLocationStrategy }
  ]
};
