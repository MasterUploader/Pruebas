Falto la parte de mantenimiento de usuarios, acá te dejo el codigo antiguo

<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Administracion.aspx.cs" Inherits="Administracion" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <link href="Content/style.css" rel="stylesheet" />
    <script src="Scripts/jsAcciones.js"></script>
    <title>Administraci&oacute;n de Usuarios</title> 
</head>
<body>

    <div class="container">
        <header style="width:auto; height:100px; background-image:url('Images/header.jpg'); border-radius:10px; -moz-border-radius:5px 5px 5px 5px;-webkit-border-radius:5px 5px 5px 5px">
            <div style="float:right; margin-right:5px;">
               <a href="javascript:logout();" style="color:white;">Cerrar Sesi&oacute;n [<%Response.Write(Session["usuario"].ToString());%>]</a>         
            </div>
        </header>

         <div style="margin-top:10px;">
             <h2 id="tituloPrincipal" style="color:red;">Administraci&oacute;n de Usuarios</h2>
        </div>


        <form id="form1" runat="server">
        
            <p>
                <asp:Button ID="btnAgregarNuevo" runat="server" Text="Agregar Nuevo Usuario" OnClick="btnAgregarNuevo_Click"/>
            </p>

            <div class="separador"></div>

            <asp:GridView ID="gvUsuarios" runat="server" CellPadding="4" DataSourceID="DBDataSourceUsuarios" ForeColor="#333333" GridLines="None" Width="934px" AutoGenerateColumns="False" DataKeyNames="USUARIO" OnRowDeleting="gvUsuarios_RowDeleting" OnRowUpdating="gvUsuarios_RowUpdating">
                    <AlternatingRowStyle BackColor="White" />
                    <Columns>
                        <asp:CommandField ShowEditButton="True" />
                        <asp:CommandField ShowDeleteButton="True" />
                        <asp:BoundField DataField="USUARIO" HeaderText="Usuario" ReadOnly="True" SortExpression="USUARIO" >
                        <HeaderStyle HorizontalAlign="Left" />
                        </asp:BoundField>
                        <asp:TemplateField HeaderText="Tipo Usuario" SortExpression="TIPUSU">
                            <EditItemTemplate>
                                <asp:DropDownList ID="DropDownList1" runat="server">
                                    <asp:ListItem Value="1">Administrador</asp:ListItem>
                                    <asp:ListItem Value="2">Admin. Videos</asp:ListItem>
                                    <asp:ListItem Value="3">Admin. Mensajes</asp:ListItem>
                                </asp:DropDownList>
                            </EditItemTemplate>
                            <ItemTemplate>
                                <asp:Label ID="Label1" runat="server" Text='<%# Bind("TIPUSU") %>'></asp:Label>
                            </ItemTemplate>
                            <HeaderStyle HorizontalAlign="Left" />
                        </asp:TemplateField>
                        <asp:TemplateField HeaderText="Estado" SortExpression="ESTADO">
                            <EditItemTemplate>
                                <asp:DropDownList ID="DropDownList2" runat="server">
                                    <asp:ListItem Value="A">Activo</asp:ListItem>
                                    <asp:ListItem Value="I">Inactivo</asp:ListItem>
                                </asp:DropDownList>
                            </EditItemTemplate>
                            <ItemTemplate>
                                <asp:Label ID="Label2" runat="server" Text='<%# Bind("ESTADO") %>'></asp:Label>
                            </ItemTemplate>
                            <HeaderStyle HorizontalAlign="Left" />
                        </asp:TemplateField>
                    </Columns>
                    <FooterStyle BackColor="#990000" Font-Bold="True" ForeColor="White" />
                    <HeaderStyle BackColor="#990000" Font-Bold="True" ForeColor="White" />
                    <PagerStyle BackColor="#FFCC66" ForeColor="#333333" HorizontalAlign="Center" />
                    <RowStyle BackColor="#FFFBD6" ForeColor="#333333" />
                    <SelectedRowStyle BackColor="#FFCC66" Font-Bold="True" ForeColor="Navy" />
                    <SortedAscendingCellStyle BackColor="#FDF5AC" />
                    <SortedAscendingHeaderStyle BackColor="#4D0000" />
                    <SortedDescendingCellStyle BackColor="#FCF6C0" />
                    <SortedDescendingHeaderStyle BackColor="#820000" />
                </asp:GridView>

                <asp:SqlDataSource ID="DBDataSourceUsuarios" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" SelectCommand="SELECT USUARIO, CASE WHEN TIPUSU = 1 THEN 'Administrador' WHEN TIPUSU = 2 THEN 'Admin. Videos' WHEN TIPUSU = 3 THEN 'Admin. Mensaje' END AS TiPUsu, CASE WHEN ESTADO = 'A' THEN 'Activo' WHEN ESTADO = 'I' THEN 'Inactivo' END AS Estado FROM BCAH96DTA.USUADMIN WHERE (USUARIO &lt;&gt; 'usrmar')"></asp:SqlDataSource>
                <asp:SqlDataSource ID="DBDataSourceOperaciones" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" SelectCommand="SELECT COUNT(*) AS Expr1 FROM BCAH96DTA.RSAGE01"></asp:SqlDataSource>

             <p>
                 <asp:Button ID="btnMenu" runat="server" Text="Volver" OnClick="btnMenu_Click" />
             </p>

            <p>
                <asp:Label ID="lblError" runat="server" Text="lblError" ForeColor="Red" Visible="False"></asp:Label>
            </p>


        </form>

    </div>

</body>
</html>

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
