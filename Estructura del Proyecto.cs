async logout(): Promise<void> {
  const token = this.getAccessToken();

  if (token && this.sessionIsActive()) {
    try {
      await firstValueFrom(
        this.http.post(`${this.apiUrl}/api/Auth/Logout`, {}, { headers: { Authorization: `Bearer ${token}` } })
      );
      console.log('Logout registrado en backend.');
    } catch (err) {
      console.warn('No se pudo registrar logout en backend', err);
    }
  }

  if (this.isBrowser) localStorage.clear();
  this.currentUserSubject.next(null);
  this.sessionActive.next(false);

  await this.router.navigate(['/login']);
}
