import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { loginGuard } from './login.guard';
import { AuthService } from '../services/auth.service';

describe('loginGuard', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  // Mocks mínimos para la firma del guard
  const mockRoute = {} as ActivatedRouteSnapshot;
  const mockState = { url: '/login' } as RouterStateSnapshot;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['sessionIsActive']);
    routerSpy = jasmine.createSpyObj('Router', ['createUrlTree']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    });
  });

  it('debe redirigir a /tarjetas si la sesión está activa', () => {
    authServiceSpy.sessionIsActive.and.returnValue(true);
    const fakeTree = {} as any;
    routerSpy.createUrlTree.and.returnValue(fakeTree);

    const result = TestBed.runInInjectionContext(() => loginGuard(mockRoute, mockState));

    expect(routerSpy.createUrlTree).toHaveBeenCalledWith(['/tarjetas']);
    expect(result).toBe(fakeTree);
  });

  it('debe permitir ver /login si NO hay sesión', () => {
    authServiceSpy.sessionIsActive.and.returnValue(false);

    const result = TestBed.runInInjectionContext(() => loginGuard(mockRoute, mockState));

    expect(result).toBeTrue();
    expect(routerSpy.createUrlTree).not.toHaveBeenCalled();
  });
});
