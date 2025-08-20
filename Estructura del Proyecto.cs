import { Component, OnInit, ViewChild } from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSort, Sort } from '@angular/material/sort';
import { MatTableDataSource } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { TarjetaService } from '../../../../app/core/services/tarjeta.service';
import { Tarjeta } from '../../models/tarjeta.model';
import { ConfirmDialogComponent } from '../confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-consulta-tarjeta',
  templateUrl: './consulta-tarjeta.component.html',
  styleUrls: ['./consulta-tarjeta.component.css']
})
export class ConsultaTarjetaComponent implements OnInit {
  displayedColumns: string[] = ['codigo', 'nombre', 'acciones'];
  dataSource = new MatTableDataSource<Tarjeta>([]);
  totalRegistros = 0;
  pageSize = 10;
  pageIndex = 0;
  isLoading = false;

  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor(
    private readonly tarjetaService: TarjetaService,
    private readonly dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.loadTarjetas();
  }

  /** Carga de datos con paginación y ordenamiento */
  loadTarjetas(): void {
    this.isLoading = true;
    this.tarjetaService.getTarjetas(this.pageIndex, this.pageSize).subscribe({
      next: (resp) => {
        this.dataSource.data = resp.items;
        this.totalRegistros = resp.total;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  /** Evento al cambiar de página */
  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadTarjetas();
  }

  /** Evento al cambiar el orden */
  onSortChange(sortState: Sort): void {
    if (!sortState.active || sortState.direction === '') {
      return;
    }
    // ⚠️ Aquí puedes implementar ordenamiento en backend si lo necesitas
    this.dataSource.data = this.dataSource.data.sort((a, b) => {
      const valueA = (a as any)[sortState.active];
      const valueB = (b as any)[sortState.active];
      const factor = sortState.direction === 'asc' ? 1 : -1;
      return valueA < valueB ? -1 * factor : valueA > valueB ? 1 * factor : 0;
    });
  }

  /** Confirmación antes de eliminar */
  confirmDelete(tarjeta: Tarjeta): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '350px',
      data: {
        title: 'Confirmar eliminación',
        message: `¿Seguro que deseas eliminar la tarjeta ${tarjeta.codigo}?`
      }
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result === true) {
        this.deleteTarjeta(tarjeta);
      }
    });
  }

  /** Elimina la tarjeta en backend */
  private deleteTarjeta(tarjeta: Tarjeta): void {
    this.isLoading = true;
    this.tarjetaService.deleteTarjeta(tarjeta.codigo).subscribe({
      next: () => {
        this.loadTarjetas();
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }
}



import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-confirm-dialog',
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="onCancel()">Cancelar</button>
      <button mat-raised-button color="warn" (click)="onConfirm()">Eliminar</button>
    </mat-dialog-actions>
  `
})
export class ConfirmDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<ConfirmDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { title: string; message: string }
  ) {}

  onCancel(): void {
    this.dialogRef.close(false);
  }

  onConfirm(): void {
    this.dialogRef.close(true);
  }
}
