Acá te pasare el código del que partiremos, y las mejoras a aplicar serian:

Que al momento de dar click en el boton de login aparezca un recuadro flotando que tenga un circulo girando y que diga cargando... y que no permita volver a dar click hasta que responda el api, esto reemplazaria lo que ya se tiene para ese caso


import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card'
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormGroup, FormsModule, FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
import { AuthService } from '../../../../core/services/auth.service';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
    selector: 'app-login',
    imports: [MatCardModule, FormsModule, MatFormFieldModule, ReactiveFormsModule, MatSelectModule, MatInputModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule],
    templateUrl: './login.component.html',
    styleUrl: './login.component.css'
})
export class LoginComponent {
  hidePassword = true;
  errorMessage: string = '';
  loginForm: FormGroup;
  isLoading = false;

  constructor(private readonly authService: AuthService, private readonly router: Router, private readonly fb: FormBuilder, private readonly snackBar: MatSnackBar) {
    this.loginForm = this.fb.group({
      userName: ['', Validators.required],
      password: ['', Validators.required]
    });

  }

  login(): void {

    if (this.loginForm.valid) {

      const loginData = this.loginForm.value;

      if (this.loginForm.invalid) {
        return;
      }


      this.isLoading = true;

      this.snackBar.open('Cargando...', '',{
        duration:3000
      });

      this.authService.login(loginData.userName, loginData.password).subscribe({
        next: (data) => {
          this.errorMessage = data.codigo.message;
          this.isLoading = false;
          this.snackBar.dismiss();
          this.router.navigate(['/tarjetas']);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error.codigo.message || 'Ocurrio un error durante el inicio de Sesión';
          this.isLoading = false;
          this.snackBar.dismiss();

          this.snackBar.open(this.errorMessage, 'Cerrar',{
            duration: 5000
          });
        }
      });
    }
  }

  togglePasswordVisibility() {
    this.hidePassword = !this.hidePassword;
  }


}


<!-- Login -->
<div class="login-container">
  <mat-card class="content-form">
    <mat-card-header class="content-title-login">
      <img src="../../assets/logo.png" alt="" class="imgLogo">
      <mat-card-title class="title-login">Iniciar Sesión</mat-card-title>
    </mat-card-header>
    <mat-card-content>
      <form (ngSubmit)="login()" [formGroup]="loginForm" class="form">
        <!--UserName-->
        <mat-form-field appearance="fill" class="full-width">
          <input aria-placeholder="Usuario" matInput placeholder="Introduce tu usuario"
            formControlName="userName" name="userName" id="userName" required autocomplete="username">
          </mat-form-field>
          <!--Password-->
          <mat-form-field appearance="fill" class="full-width inputPass">
            <input [type]="hidePassword ? 'password' : 'text'" matInput placeholder="Introduce tu Contraseña"
              formControlName="password"  name="password" id="password" required autocomplete="current-password">
              <button class="iconPass" type="button" mat-icon-button matSuffix
                (click)="hidePassword = !hidePassword" [attr.aria-label]="'Hide password'"
                [attr.aria-pressed]="hidePassword">
                <mat-icon class="iconPass">{{ hidePassword ? 'visibility_off' : 'visibility' }}</mat-icon>

              </button>
            </mat-form-field>
            <!---Ventana de Carga-->
            @if (isLoading) {
              <mat-progress-spinner mode="indeterminate"></mat-progress-spinner>
            }

            <!--Boton login-->
            <button mat-raised-button color="primary" type="submit" [disabled]="isLoading || !loginForm.valid"
            class="loginNow">Entrar</button>
            @if (errorMessage) {
              <div class="alert alert-danger"> {{this.errorMessage}}</div>
            }
          </form>
        </mat-card-content>
      </mat-card>
    </div>
    <!-- Login -->


            /*.login-container {
    display: flex;
    justify-content: center;
    margin-top: 50px;
}

.full-width {
    width: 100%;
}*/

/* Estilos para el formulario - login */
body {
    background-color: rgb(241, 239, 239);
}

.login-container{
    position: relative;
    /* width: 100%;
    height: auto;
    padding: 100px 0;
    display: flex !important;
    justify-content: center !important;
    align-items: center !important;
    background: black; */

}


/* diseño 2 */
.content-form{
    position: absolute;
    /* bottom: 50%; */
    top: 270px;
    left: 50%;
    transform: translate(-50%, -50%);
    max-width: 400px;
    width: 100%;
    height: 400px;
    background-color: #fff;
    padding: 25px;
    border-radius: 12px;
    box-shadow: 1px 1px 5px rgba(0, 0, 0, 0.349);
}

.content-title-login{
    /* background-color: royalblue;  */
    /* position: relative; */
    display: block;
    width: 100%;
    padding: 30px 0;
    /*height: 120px;*/
    text-align: center;
    display: flex;
    justify-content: center;
    align-items: center;
}

.imgLogo{
    position: absolute;
    top:-60px;
    width: 110px;
    height: 100px;
    display: block;
}

.title-login{
    font-size: 1.5rem;
    font-weight: bold;
}

.form{
    height: 80%;
    display: flex;
    flex-direction: column;
    gap: 20px;/*diseño 2*/
}


.inputPass{
    position: relative;
    display: flex;
}

.iconPass{
    position: absolute;
    top: -10px;
    right: 10px;
    width: 30px;
    height: 30px;
    border: none;
    
}

.full-width{
    border: none !important;
    background-color: #fff !important;
}

.form input{
    width: 100%;
    height: 30px !important;
    /* border:none; */
    /* border-bottom: 1.5px solid #aaaa; */
    outline: none;
}

.loginNow{
    background-color:#e4041c ;
    color:#fff;
    padding: 13px 0;
    border-radius: 10px;
    border: none;
    font-size: 1rem;
    font-weight: bold;
    cursor: pointer;
}

.loginNow:hover{
    box-shadow: 2px 2px 3px rgba(0, 0, 0, 0.349);

}

/* medias querys */
@media only screen and (max-width:992px){
    .content-form{
        max-width: 320px;
        height: 380px;
    }
    .Info__text{
        /* background-color: yellow; */
        width: 60%;
        font-size: 1.3rem;
        margin-top: 2px;
    }
    .form{
        gap:50px
    }
}

@media only screen and (max-width:768px){

    .Info__text{
        /* background-color: yellow; */
        width: 50%;
        font-size: 1rem;
    }

    .Info__buttons-user, .Info__buttons-logout{
        padding: 12px 12px;
        font-size: 1rem;        
    }

}
