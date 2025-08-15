// modal-tarjeta.component.ts
import { Component, /* ... */, OnInit } from '@angular/core';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { trigger, transition, style, animate, query, group } from '@angular/animations';

// ... (tus imports actuales)

@Component({
  // ...
  standalone: true,
  imports: [
    // tus imports…
    BrowserAnimationsModule
  ],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css',
  animations: [
    // Cross-fade entre diseño 1 y 2
    trigger('crossFade', [
      transition('* <=> *', [
        group([
          // el que entra
          query(':enter', [
            style({ opacity: 0, position: 'absolute', inset: 0 }),
            animate('180ms ease-out', style({ opacity: 1 }))
          ], { optional: true }),
          // el que sale
          query(':leave', [
            style({ opacity: 1, position: 'absolute', inset: 0 }),
            animate('180ms ease-in', style({ opacity: 0 }))
          ], { optional: true })
        ])
      ])
    ])
  ]
})
export class ModalTarjetaComponent implements OnInit {
  // ... tu código actual

  ngOnInit(): void {
    // Precarga la otra imagen para evitar parpadeo
    const img1 = new Image(); img1.src = '/assets/TarjetaDiseño2.png';
    const img2 = new Image(); img2.src = '/assets/Tarjeta3.PNG';

    // tu lógica ya existente…
    // (no quites lo que ya tienes aquí)
  }

  // ... resto igual
}


<!-- Dentro de content-imagen-tarjeta, deja la <img> como la tienes -->

<!-- STAGE con cross-fade (ocupa el mismo espacio; evita saltos) -->
<div class="design-stage" [@crossFade]="disenoSeleccionado">
  <!-- Diseño 1 -->
  <ng-container *ngIf="disenoSeleccionado === 'unaFila'">
    <div class="nombres-una-fila"><b>{{tarjeta.nombre}}</b></div>
    <div class="cuenta-una-fila"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
  </ng-container>

  <!-- Diseño 2 -->
  <ng-container *ngIf="disenoSeleccionado === 'dosFilas'">
    <div class="nombres"><b>{{nombres}}</b></div>
    <div class="apellidos"><b>{{apellidos}}</b></div>
    <div class="cuenta"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
  </ng-container>
</div>


/* Contenedor de la imagen (ya lo tienes) */
.content-imagen-tarjeta {
  position: relative;
  width: 207.87404194px;
  height: 321.25988299px;
}

/* Stage donde se apilan los diseños durante el cross-fade */
.design-stage {
  position: absolute;
  inset: 0;           /* ocupa exactamente el área de la tarjeta */
  overflow: hidden;   /* por si algo se sale durante la transición */
}

/* Asegúrate que tus clases (nombres/apellidos/cuenta) siguen siendo absolute
   respecto a .content-imagen-tarjeta o .design-stage, como ya lo tenías */
