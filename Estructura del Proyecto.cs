Ahora este es el codigo antiguo de otra opción, necesito migrarlo a la versión nueva

<%@ Page Language="C#" AutoEventWireup="true" CodeFile="AgregarNvoUser.aspx.cs" Inherits="AgregarNvoUser" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <link href="Content/style.css" rel="stylesheet" />
    <script src="Scripts/jsAcciones.js"></script>
    <title>Agregar Nuevo Usuario</title>
    <style type="text/css">
        .auto-style2 {
            width: 108px;
        }
        .auto-style3 {
            width: 159px;
        }
    </style>
</head>
<body>

    <div class="container">

          <header style="width:auto; height:100px; background-image:url('Images/header.jpg'); border-radius:10px; -moz-border-radius:5px 5px 5px 5px;-webkit-border-radius:5px 5px 5px 5px">
            <div style="float:right; margin-right:5px;">
               <a href="javascript:logout();" style="color:white;">Cerrar Sesi&oacute;n [<%Response.Write(Session["usuario"].ToString());%>]</a>         
            </div>
        </header>

         <div style="margin-top:10px;">
             <h2 id="tituloPrincipal" style="color:red;">Agregar Nuevo Usuario</h2>
        </div>

        <div class="separador"></div>

        <form id="form1" runat="server">
         
        <table id="agrgarVideo">
            <tr>
                <td class="auto-style2">
                    <asp:Label ID="lblAgencia" runat="server" Text="Usuario:" Font-Names="Calibri"></asp:Label>
                </td>
                <td>
                    <asp:TextBox ID="txtUsuario" runat="server" MaxLength="32" Width="230px"></asp:TextBox>
                </td>
            </tr>

            <tr>
                <td class="auto-style2">
                    <asp:Label ID="lblRotulo" runat="server" Text="Tipo Usuario:" Font-Names="Calibri"></asp:Label>
                </td>
                <td>
                    <asp:DropDownList ID="ddlTipoUsuario" runat="server">
                        <asp:ListItem Value="1">Administrador</asp:ListItem>
                        <asp:ListItem Value="2">Admin. Videos</asp:ListItem>
                        <asp:ListItem Value="3">Admin. Mensajes</asp:ListItem>
                    </asp:DropDownList>
                </td>
            </tr>

            <tr>
                <td class="auto-style2">
                    <asp:Label ID="lblEstado" runat="server" Text="Estado" Font-Names="Calibri"></asp:Label>
                </td>
                <td>
                    <asp:DropDownList ID="ddlEstado" runat="server">
                        <asp:ListItem Value="A">Activo</asp:ListItem>
                        <asp:ListItem Value="I">Inactivo</asp:ListItem>
                    </asp:DropDownList>
                </td>               
            </tr>

        </table>

        <table>
            <tr>
                <td class="auto-style3">
                    <asp:Label ID="lblClave" runat="server" Text="Clave:" Font-Names="Calibri" ForeColor="#0066FF"></asp:Label>
                </td>

                <td>
                    <asp:TextBox ID="txtClave" runat="server" MaxLength="10" TextMode="Password" Width="186px"></asp:TextBox>
                </td>
            </tr>

            <tr>
                <td class="auto-style3">

                    <asp:Label ID="lblConfirmarClave" runat="server" Font-Names="Calibri" ForeColor="#0066FF" Text="Confirmar Clave:"></asp:Label>

                </td>
                <td>

                    <asp:TextBox ID="txtConfirmarClave" runat="server" MaxLength="10" TextMode="Password" Width="184px"></asp:TextBox>

                </td>
            </tr>

        </table>

        
    


    <div id="inner">

         <p style="margin-left: 280px">
        <asp:Button ID="btnAceptar" runat="server" Text="Aceptar" OnClick="btnAceptar_Click" />
        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
            <asp:Button ID="btnCancelar" runat="server" Text="Cancelar" OnClick="btnCancelar_Click"  />
        &nbsp;<asp:SqlDataSource ID="DB2DataSource3" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" SelectCommand="SELECT MAX(CODVIDEO) AS Expr1 FROM BCAH96DTA.MANTVIDEO"></asp:SqlDataSource>
    </p>


    </div>

     <p>
        <asp:Label ID="lblError" runat="server" Text="lblError" ForeColor="Red" Visible="False"></asp:Label>
    </p>

    </form>
        

    </div>

    
    
</body>
</html>



using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class AgregarNvoUser : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["usuario"] == null)
            Response.Redirect("Default.aspx");
    }
    protected void btnAceptar_Click(object sender, EventArgs e)
    {
        if (this.txtUsuario.Text.Equals(string.Empty)) //El usuario esta vacio ?
        {
            this.lblError.Text = "Debe Ingresar Usuario";
            this.lblError.Visible = true;
            return;
        }            
        else
        {
            if (this.txtClave.Text.Equals(string.Empty)) //Alguna de los campos de las claves esta vacio?
            {
                this.lblError.Text = "Las Claves no pueden estar vacias";
                this.lblError.Visible = true;
                return;
            }
            else
            {
                if (!this.txtClave.Text.Equals(this.txtConfirmarClave.Text)) //Las claves no coinciden?
                {
                    this.lblError.Text = "Las Claves Ingresadas no Coinciden";
                    this.lblError.Visible = true;
                    return;
                }
                else //Si todo va bien 
                {
                    string claveEncriptada = this.encriptarClave(this.txtClave.Text);

                    string qry = "insert into bcah96dta.USUADMIN values('" + this.txtUsuario.Text +  
                                                                        "','" + claveEncriptada + "'," + ddlTipoUsuario.SelectedValue.ToString() + ",'" + 
                                                                        this.ddlEstado.SelectedValue.ToString() + "')";
                    DB2DataSource3.InsertCommand = qry;

                    try
                    {
                        DB2DataSource3.Insert();
                        this.txtUsuario.Text = string.Empty;
                        this.txtClave.Text = string.Empty;
                        this.txtConfirmarClave.Text = string.Empty;

                        string msg = "Se ha agregado exitosamente el usuario: " + this.txtUsuario.Text + "!";
                        ClientScript.RegisterStartupScript(this.GetType(), "Exito", "alert('" + msg + "');", true);                        
                        
                    }
                    catch (Exception ex)
                    {
                        this.lblError.Text = ex.Message;
                        this.lblError.Visible = true;
                    }
                }
            }
        }
    }
    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        Response.Redirect("Administracion.aspx");
    }

    private string encriptarClave(string clave)
    {
        OperacionesVarias opVarias = new OperacionesVarias();
        return opVarias.encriptarCadena(clave);        
    }
}
