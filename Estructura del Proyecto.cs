@if (isLoading) {
  <div class="overlay" role="alert" aria-live="assertive">
    <div class="overlay-content" role="dialog" aria-label="Autenticando">
      <mat-progress-spinner
        mode="indeterminate"
        diameter="48"
      ></mat-progress-spinner>
      <div class="overlay-text">Autenticando</div>
    </div>
  </div>
      }

@if (errorMessage) {
  <div
    class="inline-error mat-mdc-form-field-error"
    role="alert"
  >
    <mat-icon aria-hidden="true">error</mat-icon>
    <span>{{ errorMessage }}</span>
  </div>
  }
