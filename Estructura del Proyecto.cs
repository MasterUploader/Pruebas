# ===== 0) Requisitos (Angular CLI 20) =====
npm i -g @angular/cli@20

# ===== 1) Crear proyecto Angular 20 (standalone + routing + SCSS) =====
ng new BiBImpresionNG --standalone --routing --style=scss --prefix=app --skip-tests
cd BiBImpresionNG

# ===== 2) Estructura de estilos moderna (SCSS + design tokens) =====
mkdir src\styles
mkdir src\styles\tokens
mkdir src\styles\base
mkdir src\styles\layout
mkdir src\styles\components
mkdir src\styles\legacy

ni src\styles\tokens\_colors.scss -ItemType File -Force   | Out-Null
ni src\styles\tokens\_spacing.scss -ItemType File -Force  | Out-Null
ni src\styles\tokens\_typography.scss -ItemType File -Force | Out-Null
ni src\styles\base\_reset.scss -ItemType File -Force      | Out-Null
ni src\styles\base\_globals.scss -ItemType File -Force    | Out-Null
ni src\styles\layout\_grid.scss -ItemType File -Force     | Out-Null
ni src\styles\components\_button.scss -ItemType File -Force | Out-Null
ni src\styles\components\_table.scss -ItemType File -Force  | Out-Null
ni src\styles\legacy\_map.scss -ItemType File -Force      | Out-Null

# Importar los parciales en styles.scss (orden por capas)
Add-Content src\styles.scss "@use 'styles/tokens/colors';"
Add-Content src\styles.scss "@use 'styles/tokens/spacing';"
Add-Content src\styles.scss "@use 'styles/tokens/typography';"
Add-Content src\styles.scss "@use 'styles/base/reset';"
Add-Content src\styles.scss "@use 'styles/base/globals';"
Add-Content src\styles.scss "@use 'styles/layout/grid';"
Add-Content src\styles.scss "@use 'styles/components/button';"
Add-Content src\styles.scss "@use 'styles/components/table';"
Add-Content src\styles.scss "@use 'styles/legacy/map';"

# ===== 3) Carpeta de assets base =====
mkdir src\assets
mkdir src\assets\img
mkdir src\assets\fonts

# ===== 4) Núcleo de la app (core + shared sin NgModules; enfoque standalone) =====
mkdir src\app\core
mkdir src\app\core\http
mkdir src\app\shared
mkdir src\app\shared\pipes
mkdir src\app\shared\directives
mkdir src\app\shared\components

# Interceptores funcionales (Auth y Error)
ng g interceptor core/http/auth --functional --flat --skip-tests
ng g interceptor core/http/error --functional --flat --skip-tests

# Ejemplos de utilidades compartidas (opcionales)
ng g directive shared/directives/autofocus --standalone --skip-tests
ng g pipe shared/pipes/safeHtml --standalone --skip-tests

# ===== 5) Feature "impresion" (estructura por capas) =====
mkdir src\app\features
mkdir src\app\features\impresion
mkdir src\app\features\impresion\models
mkdir src\app\features\impresion\services
mkdir src\app\features\impresion\pages

# Servicio HTTP
ng g service features/impresion/services/impresion --skip-tests

# Componente de página (standalone) con HTML/SCSS separados
ng g component features/impresion/pages/impresion-page --standalone --skip-tests --inline-style=false --inline-template=false

# Modelo(s) de datos
ni src\app\features\impresion\models\impresion.models.ts -ItemType File -Force | Out-Null

# ===== 6) Enrutamiento base (ruta /impresion) =====
# (El CLI ya creó app.routes.ts y app.config.ts; agregamos la ruta lazy a la página)
$routes = @"
import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'impresion' },
  {
    path: 'impresion',
    loadComponent: () =>
      import('./features/impresion/pages/impresion-page/impresion-page.component')
        .then(m => m.ImpresionPageComponent)
  },
  { path: '**', redirectTo: 'impresion' }
];
"@
Set-Content -Path src\app\app.routes.ts -Value $routes -Encoding UTF8

# ===== 7) Proveer HttpClient con interceptores funcionales =====
$cfg = @"
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/http/auth.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor]))
  ]
};
"@
Set-Content -Path src\app\app.config.ts -Value $cfg -Encoding UTF8

# ===== 8) Arranque =====
npm start
