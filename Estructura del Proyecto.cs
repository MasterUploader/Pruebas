Reverti a este codigo que funciona, agregale solo las mejoras: que sean 40 caracteres en dos filas, que permita minimo 2 nombres, y que muestre error cuando se den y que no permita el avance del flujo de impresión mientras esten:

import { Component, Input, Output, OnInit, Inject, EventEmitter, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar'
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatSelectModule } from '@angular/material/select';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { ImpresionService } from '../../../../core/services/impresion.service';
import { ConfirmacionDialogoComponent } from '../../../../modules/variados/components/confirmacion-dialogo/confirmacion-dialogo.component';
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ConsultaTarjetaComponent } from '../consulta-tarjeta/consulta-tarjeta.component';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';
import { Subscription } from 'rxjs';
import { NumericDictionary, toUpper } from 'lodash';

@Component({
  selector: 'app-modal-tarjeta',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatInputModule, MatSelectModule, FormsModule, MaskAccountNumberPipe],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css'
})
export class ModalTarjetaComponent implements OnInit, OnDestroy {

  private subscription: Subscription = new Subscription();
  private imprime: boolean;

  @Output() nombreCambiado = new EventEmitter<string>();
  @Input() datosTarjeta: Tarjeta = {
    nombre: '',
    numero: '',
    fechaEmision: '',
    fechaVencimiento: '',
    motivo: '',
    numeroCuenta: ''
  }

  nombreCompleto: string = '';
  nombres: string = '';
  apellidos: string = '';
  numeroCuenta: String = ''
  usuarioICBS: string = "";
  nombreMandar: string = "";
  disenoSeleccionado: string = 'dosFilas';
  maxCaracteresFila: number = 13; //Maximo de filas para el caso en el que la tarjeta se divide en nombres arriba y abajo
  nombreValidoParaImprimir = false;

  constructor(
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    public dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private sanitizer: DomSanitizer,
    private impresionService: ImpresionService,
    private tarjetaService: TarjetaService,
    private cdr: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public tarjeta: Tarjeta) {
    this.actualizarNombre(tarjeta.nombre);
    this.nombreCompleto = tarjeta.nombre;
    this.imprime = false;
  }

  ngOnInit(): void {

    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.usuarioICBS = this.authService.currentUserValue?.activeDirectoryData.usuarioICBS!;

        this.actualizarNombre(this.tarjeta.nombre);

        this.cdr.detectChanges();
        this.cdr.markForCheck();

      } else {
        this.authService.logout();
      }
    }));

  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  imprimir(datosParaImprimir: Tarjeta): void {
    let impresionExitosa = false;

    this.subscription.add(this.authService.sessionActive$.subscribe(isActive => {
      if (isActive) {
        this.tarjetaService.validaImpresion(this.tarjeta.numero).subscribe({
          next: (respuesta) => {
            if (respuesta.imprime) {
              this.imprime = respuesta.imprime;
            } else {
              this.imprime = respuesta.imprime;
            }
          }
        });

        if (!this.imprime) {
          const tipoDiseño = this.disenoSeleccionado === "unaFila" ? true : false; //If Rernario porque ahorita solo hay dos diseños, si es una fila o son dos filas

          impresionExitosa = this.impresionService.imprimirTarjeta(datosParaImprimir, tipoDiseño);
          if(impresionExitosa){
            this.tarjetaService.guardaEstadoImpresion(this.tarjeta.numero, this.usuarioICBS, this.tarjeta.nombre.toUpperCase()).subscribe({
              next: (respuesta) => {
                this.cerrarModal();
                window.location.reload();
              },
              error: (error) => {
                console.log('error', error);
              }
            });            
          }         

        } else {
          this.cerrarModal();
        }

      } else {
        this.authService.logout();
      }
    }));


  }

  cerrarModal(): void {
    this.dialogRef.close();
  }

  emitirNombreCambiado(): void {
    this.nombreMandar = this.tarjeta.nombre.toUpperCase();
    this.nombreCambiado.emit(this.nombreMandar);
  }

  dividirYActualizarNombre(nombreCompleto: string) {
    this.actualizarNombre(nombreCompleto);
    this.emitirNombreCambiado();
  }

  dividirNombreCompleto(nombreCompleto: string) {
    let cadenaNombresApellidos: string = nombreCompleto.toUpperCase();
    let partes = cadenaNombresApellidos.split(' ');

    if (partes.length >= 4) {
      this.nombres = `${partes[0]} ${partes[1]}`;
      this.apellidos = `${partes[2]} ${partes.slice(3).join(' ')}`;

    } else if (partes.length === 3) {
      if (this.nombres.length <= 16 && this.apellidos.length <= 16) {
        this.nombres = partes[0];
        this.apellidos = `${partes[1]} ${partes[2]}`;
      }

      if (this.nombres.length <= 16 && this.apellidos.length >= 16) {
        this.nombres = `${partes[0]} ${partes[1]}`;
        this.apellidos = partes[2];
      }

    } else if (partes.length === 2) {
      this.nombres = partes[0];
      this.apellidos = partes[1];
    }
    this.nombres = this.nombres.toUpperCase();
    this.apellidos = this.apellidos.toUpperCase();

  }

  // validarEntrada(event: Event) {
  //   const input = event.target as HTMLInputElement;

  //   this.actualizarNombre(input.value);


  // }

  actualizarNombre(nombre: string) {
    let nombreActualizado = nombre.toUpperCase();

    nombreActualizado = this.validarNombre(nombreActualizado);
    this.tarjeta.nombre = nombreActualizado;

    if (this.disenoSeleccionado === 'dosFilas') {
      this.dividirNombreCompleto(nombreActualizado);
    }

    this.emitirNombreCambiado();
  }

  validarNombre(nombre: string): string {
    let nombreValido = nombre.replace(/[^A-Z\s]/g, ''); //Eliminar caracteres no permitidos.
    nombreValido = nombreValido.replace(/\s+/g, ' '); //Elinación de Espacios, solo permite un espacio

    return nombreValido;
  }

  prevenirNumeroCaracteres(event: KeyboardEvent){
    const regex = /^[a-zA-Z\s]*$/;

    if(!regex.test(event.key)){
      event.preventDefault();
    }
  }

  cambiarDiseno() {
    this.actualizarNombre(this.tarjeta.nombre);

  }

  validarEntrada(event: Event): void {
    const input = (event.target as HTMLInputElement);
    let valor = input.value.toUpperCase();

    valor = valor.replace(/[^A-ZÑ\s]/g, '').replace(/\s{2,}/g, ' ');

    input.value = valor;
    this.tarjeta.nombre = valor;

    this.nombreValidoParaImprimir = valor.trim().length >= 10;
  }

}



<h1 mat-dialog-title> Detalle Tarjeta</h1>
<div mat-dialog-content id="contenidoImprimir">
  <div class="contenedor">
    <div class="content-imagen-tarjeta">
      <img [src]="disenoSeleccionado === 'unaFila' ? '/assets/TarjetaDiseño2.png' : '/assets/Tarjeta3.PNG'"
        alt="imagen tarjeta" class="imagen-tarjeta no-imprimir">
    </div>
    <!-- Diseño para una fila-->
    <div *ngIf="disenoSeleccionado === 'unaFila'" class="nombre-completo">
      <div class="nombres-una-fila">
        <b>{{tarjeta.nombre}}</b>
      </div>

      <!-- Numero de Cuenta-->
      <div class="cuenta-una-fila"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
    </div>

    <!-- Diseño para dos filas-->
    <div *ngIf="disenoSeleccionado === 'dosFilas'" class="nombre-completo">
      <div class="nombres">
        <b>{{nombres}}</b>
      </div>
      <div class="apellidos">
        <b>{{apellidos}}</b>
      </div>

      <!-- Numero de Cuenta-->
      <div class="cuenta"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
    </div>


  </div>

  <div mat-dialog-actions class="action-buttons">

    <!--Selector de Diseño de Tarjeta-->
    <mat-form-field appearance="fill" class="diseño-input">
      <mat-label>Diseño</mat-label>
      <mat-select [(value)]="disenoSeleccionado" (selectionChange)="cambiarDiseno()">

        <!--Diseño 1 Primera Tarjeta Vertical Abril 2024-->
        <mat-option value="unaFila">Diseño 1</mat-option>
        <!-- Diseño 2 Segunda Tarjeta Vertical Noviembre de 2024-->
        <mat-option value="dosFilas">Diseño 2</mat-option>
      </mat-select>

    </mat-form-field>


    <mat-form-field appearance="fill" class="nombre-input">
      <mat-label>Nombre:</mat-label>
      <input placeholder="Nombre en Tarjeta" matInput [(ngModel)]="tarjeta.nombre" (input)="validarEntrada($event) "
        (keypress)="prevenirNumeroCaracteres($event)" maxlength="26">
    </mat-form-field>



    <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)"[disabled]="!nombreValidoParaImprimir">Imprimir</button>
    <span class="spacer"></span>
    <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">Cerrar</button>
  </div>



      /* Modal */
/* Estilo base para el fondo oscuro del modal */
.modal {
    display: none;
    /* Hidden by default */
    position: fixed;
    /* Stay in place */
    z-index: 1;
    /* Sit on top */
    left: 0;
    top: 0;
    width: 100%;
    /* Full width */
    height: 100%;
    /* Full height */
    overflow: auto;
    /* Enable scroll if needed */
    background-position: center;
  }
  
  /* Estilo para la caja de contenido del modal */
  .modal-content {
    background-color: #fefefe;
    margin: 15% auto;
    /* 15% from the top and centered */
    padding: 20px;
    border: 1px solid #888;
    /* width: 207.87404194px;/* Could be more or less, depending on screen size */
    /* height: 380px; */
  
    width: 400px;
    height: 600px;
  
    /* background-image: url("/assets/tarjeta.png"); */
  
    background-size: 87404194px 321.25988299px;
    
  
    background-repeat: no-repeat;
    background-size: cover;
  }
  
  
  
  .contenedor{
    position: relative;
    display: flex;
    justify-content: center;
    align-items: center;
  
  }
  
  @media print {
    .no-imprimir{
      display: none;
    }
  }
  
  .content-imagen-tarjeta {
    width: 207.87404194px;
    height: 321.25988299px;
    display: flex;
    align-content: center;
    justify-content: center;
    align-items: center;
    position: relative;
  }
  
  .imagen-tarjeta {
    width: 100%;
    height: 100%;
    object-fit: contain;
  }

  .nombres-una-fila {
    position: absolute;
    top: 60%;
    left:50%;
    font-size: 6pt;
    color: white;
    text-align: center;
    max-width: 90%;    
    transform: translate(-50%);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  }

  .cuenta-una-fila{
    position: absolute;
    top: 67%;
    left: 50%;
    transform: translate(-50%);
    font-size: 7pt;
    text-align: center;
    max-width: 80%;
    color: white;
  }
  
  
  .nombres {
    position: absolute;
    top: 170px;
    font-size: 6pt;
    color: white;
    right: 140px;
    justify-content: end;
  }
  
  .apellidos {
    position: absolute;
    top: 180px;
    font-size: 6pt;
    color: white;  
    right: 140px;
    justify-content: end;
  }
  
  .cuenta{
    position: absolute;
    top: 210px;
    font-size: 7pt;  
    right: 150px;
    color: white;
  }
  
  
  .modal-footer {
    padding: 10px;
    display: flex;
    flex-direction: column;
    justify-content: space-around;
    height: 100px;
  }
  
  .mat-dialog-actions {
    align-items: center;
    justify-content: space-between;
    display: flex;
    flex-wrap: wrap;
  }
  
  .action-buttons .flex-container {
    display: flex;
    justify-content: space-between;
    align-items: center;
    width: 100%;
  }
  
  .nombre-input {
    flex-grow: 1;
    margin-right: 20px;
    width: 100%;
  }
  
  .spacer {
    flex: 1;
  }
  
  .imprimir-btn {
    background-color: #4CAF50;
    color: white;
  }
  
  .imprimir-btn:hover {
    background-color: #45a049;
  }
  
  .cerrar-btn {
    background-color: #f44336;
    color: white;
  }
  
  .cerrar-btn:hover {
    background-color: #da190b;
  }
  
  .nombre-input{
    text-transform: uppercase;
  }
