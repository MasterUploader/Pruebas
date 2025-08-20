import { BehaviorSubject, Observable, tap, catchError, throwError } from 'rxjs'; // <-- tap

// ...

login(userName: string, password: string): Observable<LoginResponse> {
  return this.http
    .post<LoginResponse>(`${this.apiUrl}/api/Auth/Login`, { user: userName, password })
    .pipe(
      tap(response => {                             // <-- tap para side-effects
        if (this.isBrowser) {                       // <-- reutiliza flag ya calculada
          const expiresAt = new Date(response.token.expiration);

          localStorage.setItem('currentUser', JSON.stringify(response));
          localStorage.setItem('token', response.token.token);
          localStorage.setItem('expiresAt', expiresAt.getTime().toString());

          this.sessionActive.next(true);
          this.currentUserSubject.next(response);
          this.agenciaAperturaCodigo = response.activeDirectoryData.agenciaAperturaCodigo;
          this.agenciaImprimeCodigo = response.activeDirectoryData.agenciaImprimeCodigo;
          this.usuarioICBS = response.activeDirectoryData.usuarioICBS;

          this.router.navigate(['/tarjetas']);
        }
      }),
      catchError((err: HttpErrorResponse) => throwError(() => this.toUserFacingError(err)))
    );
}
