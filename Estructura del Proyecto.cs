Tengo estos codigos de la versión antigua

using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Administracion : System.Web.UI.Page
{
    string qry = string.Empty;
    string _error = string.Empty;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["usuario"] == null)
            Response.Redirect("Default.aspx");
    }
    protected void btnAgregarNuevo_Click(object sender, EventArgs e)
    {
        Response.Redirect("AgregarNvoUser.aspx");
    }

    protected void btnMenu_Click(object sender, EventArgs e)
    {
        Response.Redirect("Mnu_Configuracion.aspx");
    }

    protected void gvUsuarios_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        if (e.RowIndex > -1)
        {
            string usuario = string.Empty;

            TableCell celda = null;
            GridViewRow tmpFila = this.gvUsuarios.Rows[e.RowIndex];

            celda = tmpFila.Cells[2];
            usuario = celda.Text;

            if (eliminarEntradadeDB(usuario))
            {
                string msg = "Se ha eliminado el usuario: " + usuario + "!";
                ClientScript.RegisterStartupScript(this.GetType(), "Exito", "alert('" + msg + "');", true);
            }            
        }

        Response.Redirect("Administracion.aspx");
    }

    private bool eliminarEntradadeDB(string usuario)
    {
        if (!validarUnicoUsuario())
        {
            qry = "DELETE FROM BCAH96DTA.USUADMIN WHERE usuario = '" + usuario + "'";

            DBDataSourceOperaciones.DeleteCommand = qry;

            if (DBDataSourceOperaciones.Delete() > 0)
                return true;
            else
                return false;
        }
        else
        {
            _error = "El usuario que intenta eliminar es el único en el sistema, debe existir al menos un usuario";            
            return false;
        }

    }

    protected void gvUsuarios_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e.RowIndex > -1)
        {
            string usuario = string.Empty; //Usuario
            string estado = string.Empty; //Estado del usuario
            string perfil = string.Empty; //Tipo de usuario

            GridViewRow tmpFila = this.gvUsuarios.Rows[e.RowIndex];
            TableCell celda = null;

            celda = tmpFila.Cells[2];
            usuario = celda.Text;

            perfil = ((DropDownList)(tmpFila.Cells[3].Controls[1])).SelectedValue.ToString(); //celda.Text;
            estado = ((DropDownList)(tmpFila.Cells[4].Controls[1])).SelectedValue.ToString(); //celda.Text;            

            if (ActualizarUsuario(usuario, estado, perfil))
            {
                string msg = "Se ha editado el usuario: " + usuario + "!";
                ClientScript.RegisterStartupScript(this.GetType(), "Exito", "alert('" + msg + "');", true);
            }            
        }
    }

    private bool ActualizarUsuario(string usuario, string estado, string perfil)
    {
        if (estado.Equals("I") || !estado.Equals("1"))
        {
            if (!validarUnicoUsuario())
            {
                qry = "UPDATE BCAH96DTA.USUADMIN " +
                      "SET estado= '" + estado + "', TIPUSU = '" + perfil + "'" +
                      "WHERE usuario = '" + usuario + "'";

                //DBDataSourceOperaciones.UpdateCommand = qry;
                this.DBDataSourceUsuarios.UpdateCommand = qry;
                try
                {
                    if (DBDataSourceUsuarios.Update() > 0)
                        return true;
                    else
                    {
                        _error = "Fallo en la actualización";
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _error = ex.Message;
                    return false;
                }
            }
            else
            {
                this._error = "No puede modificar a Inactivo el usuario dado que es el unico usuario en el sistema, agregue más para poder editar el actual";
                return false;
            }
        }
        else
        {
            qry = "UPDATE BCAH96DTA.USUADMIN " +
                      "SET estado= '" + estado + "', TIPUSU = '" + perfil + "'" +
                      "WHERE usuario = '" + usuario + "'";

            DBDataSourceOperaciones.UpdateCommand = qry;

            try
            {
                if (DBDataSourceUsuarios.Update() > 0)
                    return true;
                else
                {
                    _error = "Fallo en la actualización";
                    return false;
                }
            }
            catch (Exception ex)
            {
                _error = ex.Message;
                return false;
            }
        }
    }

    private bool validarUnicoUsuario()
    {
        qry = "SELECT count(*) FROM BCAH96DTA.USUADMIN";

        DBDataSourceOperaciones.SelectCommand = qry;
        DataView tmpDV = (DataView)DBDataSourceOperaciones.Select(DataSourceSelectArguments.Empty);

        object hayUsuarios = tmpDV.Table.Rows[0][0];

        if (hayUsuarios != null && !hayUsuarios.ToString().Equals(string.Empty))
        {
            if (int.Parse(hayUsuarios.ToString()) > 1)
                return false;
            else
                return true;
        }
        else
            return true;
    }
}

Estos metodos hay que integrarlos en la clase UsuarioService y que se consuman desde el controlador, ademas de que las peticiones sql deben hacerse usando la linreria RestUtilities.Connections y RestUtilities.QueryBuilder.
