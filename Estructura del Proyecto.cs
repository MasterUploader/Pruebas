import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { loginGuard } from './login.guard';
import { AuthService } from '../services/auth.service';

describe('loginGuard', () => {
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

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
    routerSpy.createUrlTree.and.returnValue({} as any);

    const result = TestBed.runInInjectionContext(() => loginGuard());
    expect(routerSpy.createUrlTree).toHaveBeenCalledWith(['/tarjetas']);
    expect(result).toBe(routerSpy.createUrlTree.calls.mostRecent().returnValue);
  });

  it('debe permitir ver /login si NO hay sesión', () => {
    authServiceSpy.sessionIsActive.and.returnValue(false);
    const result = TestBed.runInInjectionContext(() => loginGuard());
    expect(result).toBeTrue();
    expect(routerSpy.createUrlTree).not.toHaveBeenCalled();
  });
});
