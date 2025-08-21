Ahora vamos a validar el servicio de impresión, este es el codigo, solo dime si es posible del modal tomar la posición del numero de cuenta y del nombre en tarjeta para pasarlo al servicio y que imprima manteniendo la proporcion de lo que esta sobre la imagen?

import { Injectable } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Injectable({
  providedIn: 'root'
})
export class ImpresionService {
  constructor(private readonly sanitizer: DomSanitizer) { }

  imprimirTarjeta(datosSeleccionados: any, tipoDiseño: boolean): boolean {
    const htmlImprimir = this.sanitizer.sanitize(1, this.generarVistaPreviaImpresion(datosSeleccionados, tipoDiseño));
    const printWindow = window.open('', '_blank', 'width=600,height=400');
    let impresionExitosa = false;

    if (printWindow && htmlImprimir) {
      printWindow.document.write(htmlImprimir);
      printWindow.document.close();
      printWindow.focus();

      printWindow.onafterprint = () => {
        console.log('OnAfterPrint');
        impresionExitosa = true;
        printWindow.close();
      }

      printWindow.onbeforeunload = () => {
        if (!impresionExitosa) {

          console.log('OnBeforeLoad');
          impresionExitosa = false;
        }
      }

      printWindow.print();
    } else {
      // Manejo de caso en que la ventana emergente ha sido bloqueada por el navegador
      alert("La ventana de impresión no se pudo abrir. Por favor, verifica si las ventanas emergentes están bloqueadas.");
      impresionExitosa = false;
    }
    return impresionExitosa;

  }

  /* imprimirDatosSeleccionados(datosSeleccionados: any): void {
     const vistaPreviaHtml = this.generarVistaPreviaImpresion(datosSeleccionados);
     const ventanaImpresion = window.open('', '_blank', 'width=600,height=400');

     if (ventanaImpresion) { // Verifica que la ventana no sea null

       ventanaImpresion.document.body.innerHTML = this.sanitizer.sanitize(SecurityContext.HTML, vistaPreviaHtml) || '';

       ventanaImpresion.document.close();
       ventanaImpresion.focus();
       ventanaImpresion.print();
       ventanaImpresion.close();

     } else {
       // Manejo de caso en que la ventana emergente ha sido bloqueada por el navegador
       alert("La ventana de impresión no se pudo abrir. Por favor, verifica si las ventanas emergentes están bloqueadas.");
     }
   }*/

  generarVistaPreviaImpresion(tarjeta: any, tipoDiseño: boolean): SafeHtml {
    let cadenaNombresApellidos: string = tarjeta.nombre;
    let nombres: string = '';
    let apellidos: string = '';
    let numeroCuenta: string = tarjeta.numeroCuenta;

    let cadenaLimpia = cadenaNombresApellidos.replace(/\s+/g, ' ');
    let partes = cadenaLimpia.split(' ');

    if (partes.length >= 4) {
      nombres = `${partes[0]} ${partes[1]}`;
      apellidos = `${partes[2]} ${partes.slice(3).join(' ')}`;

    } else if (partes.length === 3) {
      if (nombres.length <= 16 && apellidos.length <= 16) {
        nombres = partes[0];
        apellidos = `${partes[1]} ${partes[2]}`;
      }

      if (nombres.length <= 16 && apellidos.length >= 16) {
        nombres = `${partes[0]} ${partes[1]}`;
        apellidos = partes[2];
      }

    } else if (partes.length === 2) {
      nombres = partes[0];
      apellidos = partes[1];
    }
    nombres = nombres.toUpperCase();
    apellidos = apellidos.toUpperCase();

    const htmlContent = `
  <style>
  body {
    margin: 0;

  }
  .principal{
    position: absolute;
    width: 207.87404194px;
    height: 321.25988299px;
    font-family: 'Arial', sans-serif;
    box-sizing: border-box;
  }
  .internas1{
    position: absolute;
    top: 190px;
    font-size: 8pt;
    right: 20px;
    font-family: sans-serif;
    color: black;
  justify-content: end;

  }
  .internas2{
    position: absolute;
    top: 205px;
    right: 20px;
    font-size: 8pt;
    font-family:  sans-serif;
    color: black;
    justify-content: end;

  }
  .internas3{
    position: absolute;
    top: 220px;
    right: 20px;
    font-size: 8pt;
    font-family:  sans-serif;
    color: black;
    justify-content: end;

  }


  </style>
  <div class="principal">
  <div class = "internas1">
  <b>${nombres}</b>
 </div>
 <div class = "internas2">
 <b> ${apellidos}</b>
</div>
<div class="internas3">
<b>${numeroCuenta}</b>
</div>
  </div>

  `;

    const htmlContent2 = `
  <style>
  .contenedor{
    position: relative;
    display: flex;
    justify-content: center;
    align-items: center;

  }
  *{
    margin: 0;
    padding: 0;
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
  }

  .nombres {
    position: absolute;
    top: 160px;
    font-size: 15pt;
    color: black;
    right: 80px;
    text-align: start;
  }

  .apellidos {
    position: absolute;
    top: 185px;
    font-size: 15pt;
    color: blck;
    right: 80px;
    text-align: start;
  }

  .cuenta{
    position: absolute;
    top: 290px;
    font-size: 20pt;
    right: 80px;
    color: black;
  }
  </style>
  <div class="contenedor">
  <div class="content-imagen-tarjeta">


    <div class="nombres">
    <b>${nombres}</b>
    </div>
    <div class="apellidos">
   <b> ${apellidos}</b>
    </div>
    <div class="cuenta">
    <b>${numeroCuenta}</b>
    </div>
  </div>


</div>
`;


    const htmlContent3 = `
<style>
body {
  margin: 0;

}
.principal{
  position: absolute;
  width: 207.87404194px;
  height: 321.25988299px;
  font-family: 'Arial', sans-serif;
  box-sizing: border-box;
}
.internas1{
  position: absolute;
  top: 65%;
    left:50%;
    color: black;
    text-align: center;
    max-width: 90%;
    transform: translate(-50%);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
  font-size: 7pt;
  font-family:  sans-serif;

}

.internas3{
  position: absolute;
    top: 70%;
    left: 50%;
    transform: translate(-50%);
    font-size: 7pt;
    text-align: center;
    max-width: 80%;
    color: black;
  font-family:  sans-serif;
  justify-content: end;

}


</style>
<div class="principal">
<div class = "internas1">
<b>${tarjeta.nombre}</b>
</div>
<div class="internas3">
<b>${numeroCuenta}</b>
</div>
</div>

`;

    // Sanitizar y retornar el HTML como SafeHtml
    if (tipoDiseño) {
      return this.sanitizer.bypassSecurityTrustHtml(htmlContent3);
    } else {
      return this.sanitizer.bypassSecurityTrustHtml(htmlContent);
    }
  }
}
