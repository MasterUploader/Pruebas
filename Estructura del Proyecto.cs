<mat-form-field appearance="fill" class="full-width">
  <mat-label>Usuario</mat-label>
  <input
    matInput
    formControlName="userName"
    placeholder="Introduce tu usuario"
    autocomplete="username"
    required
  />

  @if (loginForm.controls['userName'].hasError('required')) {
    <mat-error>El usuario es obligatorio</mat-error>
  }
  @else if (loginForm.controls['userName'].hasError('minlength')) {
    <mat-error>El usuario debe tener al menos 3 caracteres</mat-error>
  }
  @else if (loginForm.controls['userName'].hasError('maxlength')) {
    <mat-error>El usuario no puede superar los 20 caracteres</mat-error>
  }
</mat-form-field>
