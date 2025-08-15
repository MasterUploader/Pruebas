/**
 * ConsultaTarjetaComponent (OnPush)
 * -----------------------------------------------------------------------------
 * Angular 20 (standalone) + Material Table/Sort + Reactive Forms.
 *
 * COMPORTAMIENTO ACTUAL (con mejora solicitada):
 *  - Carga tarjetas por agencias (desde sesión/form).
 *  - Filtro por número (formato campo:valor).
 *  - Abre modal al click en una fila.
 *  - "Eliminar" remueve en UI y registra impresión en backend (flujo heredado).
 *  - ✅ NUEVO: Mientras escribes en el modal, el nombre se refleja en la tabla
 *    en tiempo real (solo UI). Si cierras sin imprimir, los cambios se ven en
 *    la grilla hasta que refresques; el backend no cambia.
 *
 * MEJORAS:
 *  - ChangeDetectionStrategy.OnPush para mejor rendimiento.
 *  - Uso de `cdr.markForCheck()` tras reasignaciones que afectan la vista.
 *  - `readonly`, optional chaining, helpers para reducir complejidad (Sonar).
 *  - Predicado de filtro centralizado; constantes para “magic numbers”.
 *  - Unsubscribe centralizado con `Subscription`.
 * -----------------------------------------------------------------------------
 */

import {
  Component,
  OnInit,
  ChangeDetectorRef,
  ViewChild,
  OnDestroy,
  ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';

// Angular Material
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';

// Forms
import { ReactiveFormsModule, FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';

// RxJS
import { Subscription, take } from 'rxjs';

// Servicios / modelos (rutas correctas desde modules/)
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { GetDetalleTarjetasImprimirResponseDto } from '../../../../core/models/getDetalleTarjetasImprimir.model';
import { ModalTarjetaComponent } from '../modal-tarjeta/modal-tarjeta.component';
import { AuthService } from '../../../../core/services/auth.service';

// Pipes standalone usados en el template
import { MaskCardNumberPipe } from '../../../../shared/pipes/mask-card-number.pipe';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';

@Component({
  selector: 'app-consulta-tarjeta',
  // Standalone: importa lo que usa el template
  imports: [
    CommonModule, MatCardModule, MatDialogModule, MatTableModule,
    MatFormFieldModule, ReactiveFormsModule, MatInputModule,
    MatIconModule, MatSortModule, MatMenuModule, MatButtonModule,
    MaskCardNumberPipe, MaskAccountNumberPipe
  ],
  templateUrl: './consulta-tarjeta.component.html',
  styleUrl: './consulta-tarjeta.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConsultaTarjetaComponent implements OnInit, OnDestroy {

  // ────────────────────────────────────────────────────────────────────────────
  // Constantes de configuración
  // ────────────────────────────────────────────────────────────────────────────
  /** BIN usado por el microservicio (mantiene valor actual del proyecto). */
  private readonly BIN = '411052';
  /** Máximo de dígitos para los códigos de agencia. */
  private readonly MAX_DIGITS = 3;
  /** Patrón “solo números”. */
  private readonly ONLY_DIGITS = /^\d+$/;

  // ────────────────────────────────────────────────────────────────────────────
  // Angular Material Table / Sort
  // ────────────────────────────────────────────────────────────────────────────
  @ViewChild(MatSort, { static: true }) sort!: MatSort;

  /** DataSource tipado; con OnPush, reasignar `data` dispara el render. */
  public readonly dataSource = new MatTableDataSource<Tarjeta>([]);

  /** Columnas visibles en la tabla. */
  public readonly displayedColumns: ReadonlyArray<string> = [
    'numero', 'nombre', 'motivo', 'numeroCuenta', 'eliminar'
  ];

  // ────────────────────────────────────────────────────────────────────────────
  // Formulario reactivo
  // ────────────────────────────────────────────────────────────────────────────
  public formularioAgencias!: FormGroup<{
    codigoAgenciaImprime: FormControl<string>;
    codigoAgenciaApertura: FormControl<string>;
  }>;

  // ────────────────────────────────────────────────────────────────────────────
  // Estado de vista / sesión
  // ────────────────────────────────────────────────────────────────────────────
  public activateFilter = '';   // filtro activo (campo:valor)
  public usuarioICBS = '';      // usuario logueado
  public tarjetaSeleccionada: Tarjeta = {
    nombre: '',
    numero: '',
    fechaEmision: '',
    fechaVencimiento: '',
    motivo: '',
    numeroCuenta: ''
  };

  /** Respuesta completa para encabezados de agencias. */
  public getDetalleTarjetasImprimirResponseDto!: GetDetalleTarjetasImprimirResponseDto;

  /** Bolsa de suscripciones (unsubscribe en OnDestroy). */
  private readonly subscription = new Subscription();

  // ────────────────────────────────────────────────────────────────────────────
  // Constructor
  // ────────────────────────────────────────────────────────────────────────────
  constructor(
    private readonly authService: AuthService,
    private readonly datosTarjetaServices: TarjetaService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef,
    private readonly fb: FormBuilder
  ) {
    // Configura el predicado del filtro una vez
    this.configurarFiltros();
  }

  // ────────────────────────────────────────────────────────────────────────────
  // Ciclo de vida
  // ────────────────────────────────────────────────────────────────────────────
  ngOnInit(): void {
    // 1) Formulario con validaciones
    this.formularioAgencias = this.fb.group({
      codigoAgenciaImprime: this.fb.control('', {
        nonNullable: true,
        validators: [Validators.required, Validators.pattern(this.ONLY_DIGITS), Validators.maxLength(this.MAX_DIGITS)]
      }),
      codigoAgenciaApertura: this.fb.control('', {
        nonNullable: true,
        validators: [Validators.required, Validators.pattern(this.ONLY_DIGITS), Validators.maxLength(this.MAX_DIGITS)]
      })
    });

    // 2) Cargar valores de sesión y consultar
    this.withActiveSession(() => {
      const ad = this.authService.currentUserValue?.activeDirectoryData;
      this.usuarioICBS = ad?.usuarioICBS ?? '';

      const codigoAgenciaImprime  = ad?.agenciaImprimeCodigo ?? '';
      const codigoAgenciaApertura = ad?.agenciaAperturaCodigo ?? '';

      // Rellenar formulario y consultar
      this.formularioAgencias.patchValue({ codigoAgenciaImprime, codigoAgenciaApertura });
      this.consultarMicroservicio(codigoAgenciaImprime, codigoAgenciaApertura);

      // Sort listo (static:true)
      this.setSort();
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  // ────────────────────────────────────────────────────────────────────────────
  // Helpers infra / reutilizables
  // ────────────────────────────────────────────────────────────────────────────

  /**
   * Ejecuta `action` si la sesión está activa. Reduce duplicación y complejidad.
   */
  private withActiveSession(action: () => void): void {
    this.subscription.add(
      this.authService.sessionActive$.pipe(take(1)).subscribe(isActive => {
        if (!isActive) { this.authService.logout(); return; }
        action();
      })
    );
  }

  /** Enlaza el MatSort con el DataSource. */
  private setSort(): void {
    this.dataSource.sort = this.sort;
  }

  /**
   * Copia nombres/códigos de agencia desde la respuesta para encabezados y form.
   * Con OnPush, marcamos para verificar la vista.
   */
  private setAgenciasFromResponse(resp: GetDetalleTarjetasImprimirResponseDto): void {
    this.getDetalleTarjetasImprimirResponseDto = resp;
    this.formularioAgencias.patchValue({
      codigoAgenciaApertura: resp.agencia.agenciaAperturaCodigo,
      codigoAgenciaImprime: resp.agencia.agenciaImprimeCodigo
    });
    this.cdr.markForCheck(); // asegura render con OnPush
  }

  // ────────────────────────────────────────────────────────────────────────────
  // Filtro de la tabla
  // ────────────────────────────────────────────────────────────────────────────

  /**
   * Predicado de filtro: acepta cadenas `campo:valor`. Ej.: "numero:1234".
   * Limita a los campos visibles en la grilla.
   */
  private configurarFiltros(): void {
    this.dataSource.filterPredicate = (data: Tarjeta, raw: string): boolean => {
      if (!raw) return true;

      const [field, ...rest] = raw.split(':');
      const term = rest.join(':').toLowerCase().trim();
      if (!field || !term) return true;

      const map: Record<string, string | undefined> = {
        numero: data.numero,
        nombre: data.nombre,
        motivo: data.motivo,
        numeroCuenta: data.numeroCuenta
      };

      const value = (map[field] ?? '').toString().toLowerCase();
      return value.includes(term);
    };
  }

  /**
   * Handler de input para aplicar filtro.
   * Con OnPush, eventos de UI ya disparan CD, no se requiere markForCheck.
   */
  public applyFilterFromInput(evt: Event, field: 'numero' | 'nombre' | 'motivo' | 'numeroCuenta'): void {
    const value = (evt.target as HTMLInputElement)?.value ?? '';
    this.activateFilter = `${field}:${value.trim().toLowerCase()}`;
    this.dataSource.filter = this.activateFilter;
  }

  // ────────────────────────────────────────────────────────────────────────────
  // Consultas / Acciones
  // ────────────────────────────────────────────────────────────────────────────

  /**
   * Invoca al microservicio con BIN + códigos de agencia y carga la grilla.
   * Reasignamos `data` (dispara render con OnPush) y marcamos verificación.
   */
  private consultarMicroservicio(codigoAgenciaImprime: string, codigoAgenciaApertura: string): void {
    this.datosTarjetaServices
      .obtenerDatosTarjeta(this.BIN, codigoAgenciaImprime, codigoAgenciaApertura)
      .pipe(take(1))
      .subscribe({
        next: (response) => {
          this.dataSource.data = response.tarjetas; // reasignación => OnPush renderiza
          this.setAgenciasFromResponse(response);
          this.cdr.markForCheck(); // doble seguridad por Material
        },
        error: (error) => console.error('Error al consultar el microservicio', error)
      });
  }

  /** Dispara la consulta con los códigos del formulario (validado). */
  public actualizarTabla(): void {
    if (this.formularioAgencias.invalid) {
      this.formularioAgencias.markAllAsTouched(); // muestra errores en template
      return;
    }

    this.withActiveSession(() => {
      const agenciaImprimeCodigo  = this.formularioAgencias.get('codigoAgenciaImprime')?.value ?? '';
      const agenciaAperturaCodigo = this.formularioAgencias.get('codigoAgenciaApertura')?.value ?? '';
      this.consultarMicroservicio(agenciaImprimeCodigo, agenciaAperturaCodigo);
    });
  }

  /** Botón "Refrescar". */
  public recargarDatos(): void {
    this.actualizarTabla();
  }

  /**
   * Abre el modal de impresión para la fila seleccionada.
   * - ✅ Suscribe a `nombreCambiado` del modal para actualizar la grilla EN VIVO.
   * - No actualiza la grilla al cerrar (se mantiene comportamiento actual).
   */
  public abrirModal(row: Tarjeta): void {
    this.tarjetaSeleccionada = row;

    const ref = this.dialog.open(ModalTarjetaComponent, {
      data: row,
      width: '720px',
      disableClose: true
    });

    // 👉 Suscripción a cambios de nombre en tiempo real desde el modal
    const cmp = ref.componentInstance;
    if (cmp?.nombreCambiado) {
      const subNombre = cmp.nombreCambiado.subscribe((nuevoNombre: string) => {
        this.updateRowName(row.numero, nuevoNombre); // solo UI
      });
      // Desuscribir al cerrar el modal
      const closeSub = ref.afterClosed().pipe(take(1)).subscribe(() => subNombre.unsubscribe());
      this.subscription.add(subNombre);
      this.subscription.add(closeSub);
    }

    // (Listo para uso futuro si quieres manejar el resultado del modal)
    this.subscription.add(ref.afterClosed().pipe(take(1)).subscribe());
  }

  /**
   * Actualiza el nombre de la fila en la tabla (solo UI).
   * Con OnPush, reasignamos el array para disparar render.
   */
  private updateRowName(numero: string, nuevoNombre: string): void {
    const nueva = this.dataSource.data.map(r =>
      r.numero === numero ? { ...r, nombre: (nuevoNombre ?? '').toUpperCase() } : r
    );
    this.dataSource.data = nueva;  // reasignación => OnPush detecta cambio
    this.cdr.markForCheck();
  }

  /**
   * Evento opcional emitido por el modal; mantiene compatibilidad.
   * (No es necesario si ya usamos la suscripción anterior en abrirModal).
   */
  public onNombreCambiado(nuevoNombre: string): void {
    this.tarjetaSeleccionada.nombre = nuevoNombre;
    this.cdr.markForCheck();
  }

  /**
   * Utilidad para mostrar errores en los mat-form-field con @if en el template.
   */
  public hasFormControlError(
    controlName: keyof ConsultaTarjetaComponent['formularioAgencias']['controls'],
    errorName: string
  ): boolean {
    const control = this.formularioAgencias.get(controlName as string);
    return !!control && control.touched && control.hasError(errorName);
  }

  /**
   * Click en el icono "delete":
   *  - Remueve la fila en memoria (reasignación => OnPush renderiza).
   *  - Llama a guardaEstadoImpresion en backend (flujo heredado).
   */
  public eliminarTarjeta(event: MouseEvent, numeroTarjeta: string): void {
    event.stopPropagation();

    this.withActiveSession(() => {
      // Reasignación del array => OnPush detecta el cambio
      this.dataSource.data = this.dataSource.data.filter(item => item.numero !== numeroTarjeta);
      this.cdr.markForCheck();

      const nombreParaRegistrar =
        this.tarjetaSeleccionada?.nombre?.toUpperCase?.() ||
        this.dataSource.data.find(x => x.numero === numeroTarjeta)?.nombre?.toUpperCase?.() ||
        '';

      this.datosTarjetaServices
        .guardaEstadoImpresion(numeroTarjeta, this.usuarioICBS, nombreParaRegistrar)
        .pipe(take(1))
        .subscribe({
          next: () => {
            // Si quieres forzar un refresh completo:
            // this.actualizarTabla();
          },
          error: (error) => console.error('Error al registrar impresión', error)
        });
    });
  }
}
