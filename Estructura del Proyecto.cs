Fijate que la logica de Agregar antigua acá la tengo, para que te bases en ella y no crees los nuevos metodos desde cero


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
