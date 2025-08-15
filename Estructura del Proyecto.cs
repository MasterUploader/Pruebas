<button
  mat-raised-button
  type="submit"
  class="loginNow custom-login-btn"
  [disabled]="isLoading || loginForm.invalid">
  Entrar
</button>


/* Mantener color específico del botón de login */
.custom-login-btn {
  background-color: #e91e63 !important; /* Ejemplo: color rosa Material */
  color: white !important;
}

.custom-login-btn:hover {
  background-color: #d81b60 !important; /* Un poco más oscuro al pasar el mouse */
}

.custom-login-btn:disabled {
  background-color: #ccc !important; /* Gris cuando está deshabilitado */
  color: #666 !important;
}
