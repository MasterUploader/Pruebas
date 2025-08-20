Tengo este codigo en un sitio en angular 20

  <div class="overlay" *ngIf="isLoading" role="alert" aria-live="assertive">
    <div class="overlay-content" role="dialog" aria-label="Autenticando">
      <mat-progress-spinner
        mode="indeterminate"
        diameter="48"
      ></mat-progress-spinner>
      <div class="overlay-text">Autenticando</div>
    </div>
  </div>


  <div
          class="inline-error mat-mdc-form-field-error"
          *ngIf="errorMessage"
          role="alert"
        >
          <mat-icon aria-hidden="true">error</mat-icon>
          <span>{{ errorMessage }}</span>
        </div>

Me dice que las lineas 

<div class="overlay" *ngIf="isLoading" role="alert" aria-live="assertive">

y 

<div class="inline-error mat-mdc-form-field-error"
          *ngIf="errorMessage"
          role="alert"
        >

el *ngIf esta deprecado, como puedo corregir ese codigo en particular
