<mat-form-field appearance="fill" class="nombre-input">
  <mat-label>Nombre:</mat-label>
  <input
    placeholder="Nombre en Tarjeta"
    matInput
    formControlName="nombre"
    (input)="form.get('nombre')?.setValue((form.get('nombre')?.value || '').toUpperCase(), { emitEvent: true })"
    maxlength="26"
    autocomplete="off" />

  <mat-hint align="end">{{ (form.get('nombre')?.value?.length || 0) }}/26</mat-hint>

  @if (form.get('nombre')?.hasError('required') && form.get('nombre')?.touched) {
    <mat-error>El nombre es obligatorio.</mat-error>
  }
  @if (form.get('nombre')?.hasError('minlength') && form.get('nombre')?.touched) {
    <mat-error>Mínimo 10 caracteres.</mat-error>
  }
  @if (form.get('nombre')?.hasError('maxlength') && form.get('nombre')?.touched) {
    <mat-error>Máximo 26 caracteres.</mat-error>
  }
  @if (form.get('nombre')?.hasError('pattern') && form.get('nombre')?.touched) {
    <mat-error>Solo letras y espacios en mayúsculas.</mat-error>
  }
</mat-form-field>
