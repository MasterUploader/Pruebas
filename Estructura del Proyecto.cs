<%@ page language="C#" autoeventwireup="true" codefile="AdminAgencias.aspx.cs" inherits="AdminAgencias" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <link href="Content/style.css" rel="stylesheet" />
    <script src="Scripts/jsAcciones.js"></script>
    <title>Mantenimiento de Agencias</title>
</head>
<body>
    <form id="form1" runat="server">

        <div class="container">
            <header style="width: auto; height: 100px; background-image: url('Images/header.jpg'); border-radius: 10px; -moz-border-radius: 5px 5px 5px 5px; -webkit-border-radius: 5px 5px 5px 5px">
                <div style="float: right; margin-right: 5px;">
                    <a href="javascript:logout();" style="color: white;">Cerrar Sesi&oacute;n [<%Response.Write(Session["usuario"].ToString());%>]</a>
                </div>
            </header>

            <div style="margin-top: 10px;">
                <h2 id="tituloPrincipal" style="color: red;">Mantenimiento de Agencias</h2>
            </div>

            <div class="separador"></div>

            <asp:Button ID="btnAgregarAgencia" runat="server" Text="Agregar Agencia" OnClick="btnAgregarAgencia_Click" />
            <fieldset>
                <legend>Filtro</legend>
            </fieldset>

            <asp:GridView ID="gvAgencias" 
                runat="server" 
                AllowPaging="True" 
                AllowSorting="True" 
                AutoGenerateColumns="False"
                 CellPadding="4"  
                ForeColor="#333333" GridLines="None"
                 Width="946px" 
                OnRowDeleting="gvAgencias_RowDeleting" 
                OnRowUpdating="gvAgencias_RowUpdating" 
                OnRowEditing="gvAgencias_RowEditing" 
                OnRowCancelingEditing="gvAgencias_RowCancelingEdit" >
                <alternatingrowstyle backcolor="White" />
                <columns>
                    <asp:CommandField ShowEditButton="True"  EditText="Editar"/>
                    <asp:CommandField ShowDeleteButton="True" DeleteText="Eliminar"/>

                    <asp:BoundField DataField="CODCCO" HeaderText="CTRO COSTO" ReadOnly="True" SortExpression="CODCCO">
                        <headerstyle horizontalalign="Left" />
                        <itemstyle width="120px" />
                    </asp:BoundField>


                    <asp:TemplateField HeaderText="NOMBRE" SortExpression="NOMAGE">
                        <itemtemplate>
                            <asp:Label ID="lblNombre" runat="server" Text='<%# Bind("NOMAGE") %>'></asp:Label>
                        </itemtemplate>
                        <edititemtemplate>
                            <asp:TextBox ID="txtNombre" runat="server" Text='<%# Bind("NOMAGE") %>'></asp:TextBox>
                        </edititemtemplate>
                    </asp:TemplateField>









                    <asp:TemplateField HeaderText="ZONA" SortExpression="ZONA">
                        <edititemtemplate>
                            <asp:DropDownList ID="DropDownList1" runat="server">
                                <asp:ListItem Value="1">CENTRO SUR</asp:ListItem>
                                <asp:ListItem Value="2">NOR OCCIDENTE</asp:ListItem>
                                <asp:ListItem Value="3">NOR ORIENTE</asp:ListItem>
                            </asp:DropDownList>
                        </edititemtemplate>
                        <itemtemplate>
                            <asp:Label ID="Label1" runat="server" Text='<%# Bind("ZONA") %>'></asp:Label>
                        </itemtemplate>
                        <headerstyle horizontalalign="Left" width="120px" />
                        <itemstyle width="120px" />
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="MARQUESINA?" SortExpression="MARQUESINA">
                        <edititemtemplate>
                            <asp:DropDownList ID="DropDownList2" runat="server">
                                <asp:ListItem Value="SI">APLICA</asp:ListItem>
                                <asp:ListItem Value="NO">NO APLICA</asp:ListItem>
                            </asp:DropDownList>
                        </edititemtemplate>
                        <itemtemplate>
                            <asp:Label ID="Label2" runat="server" Text='<%# Bind("MARQUESINA") %>'></asp:Label>
                        </itemtemplate>
                        <headerstyle horizontalalign="Left" width="130px" />
                        <itemstyle width="130px" />
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="RSTBRANCH?" SortExpression="RSTBRANCH">
                        <edititemtemplate>
                            <asp:DropDownList ID="DropDownList3" runat="server">
                                <asp:ListItem Value="SI">APLICA</asp:ListItem>
                                <asp:ListItem Value="NO">NO APLICA</asp:ListItem>
                            </asp:DropDownList>
                        </edititemtemplate>
                        <itemtemplate>
                            <asp:Label ID="Label3" runat="server" Text='<%# Bind("RSTBRANCH") %>'></asp:Label>
                        </itemtemplate>
                        <headerstyle horizontalalign="Left" width="130px" />
                        <itemstyle width="130px" />
                    </asp:TemplateField>


                    <asp:TemplateField HeaderText="Nombre BD" SortExpression="NOMBD">
                        <itemtemplate>
                            <asp:Label ID="lblNomBd" runat="server" Text='<%# Bind("NOMBD") %>'></asp:Label>
                        </itemtemplate>
                        <edititemtemplate>
                            <asp:TextBox ID="txtNomBd" runat="server" Text='<%# Bind("NOMBD") %>'></asp:TextBox>
                        </edititemtemplate>
                    </asp:TemplateField>




                    <asp:TemplateField HeaderText="Nombre Server" SortExpression="NOMSER">
                        <itemtemplate>
                            <asp:Label ID="lblNServer" runat="server" Text='<%# Bind("NOMSER") %>'></asp:Label>
                        </itemtemplate>
                        <edititemtemplate>
                            <asp:TextBox ID="txtNServer" runat="server" Text='<%# Bind("NOMSER") %>'></asp:TextBox>
                        </edititemtemplate>
                    </asp:TemplateField>



                    <asp:TemplateField HeaderText="IP Servidor" SortExpression="IPSER">
                        <itemtemplate>
                            <asp:Label ID="lblIPServ" runat="server" Text='<%# Bind("IPSER") %>'></asp:Label>
                        </itemtemplate>
                        <edititemtemplate>
                            <asp:TextBox ID="txtIpServ" runat="server" Text='<%# Bind("IPSER") %>'></asp:TextBox>
                        </edititemtemplate>
                    </asp:TemplateField>


                </columns>
                <footerstyle backcolor="#990000" font-bold="True" forecolor="White" />
                <headerstyle backcolor="#990000" font-bold="True" forecolor="White" />
                <pagerstyle backcolor="#FFCC66" forecolor="#333333" horizontalalign="Center" />
                <rowstyle backcolor="#FFFBD6" forecolor="#333333" />
                <selectedrowstyle backcolor="#FFCC66" font-bold="True" forecolor="Navy" />
                <sortedascendingcellstyle backcolor="#FDF5AC" />
                <sortedascendingheaderstyle backcolor="#4D0000" />
                <sorteddescendingcellstyle backcolor="#FCF6C0" />
                <sorteddescendingheaderstyle backcolor="#820000" />
            </asp:GridView>


                <asp:SqlDataSource ID="SqlDBOperacionesMant" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" SelectCommand="SELECT CODCCO, NOMAGE, NOMSER, IPSER, HORSERV, NOMBD, ZONA, MARQUESINA, RSTBRANCH FROM BCAH96DTA.RSAGE01"></asp:SqlDataSource>
                <asp:SqlDataSource ID="SqlDBAgencias" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" SelectCommand="SELECT CODCCO, NOMAGE FROM BCAH96DTA.RSAGE01"></asp:SqlDataSource>
            </div>

            <p>
                <asp:Button ID="btnMenu" runat="server" Text="Volver" OnClick="btnMenu_Click" />
            </p>

            <p>
                <asp:Label ID="lblError" runat="server" Text="lblError" ForeColor="Red" Visible="False"></asp:Label>
            </p>

 

    </form>

</body>
</html>



using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using IBM.Data.DB2.iSeries;

using System.Data;

public partial class AdminAgencias : System.Web.UI.Page
{
    string query = string.Empty;
    string error = string.Empty;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["usuario"] == null)
            Response.Redirect("Default.aspx");

        if (!IsPostBack) BindGrid();
    }
    protected void btnAgregarAgencia_Click(object sender, EventArgs e)
    {
        Response.Redirect("AgregarNvaAgencia.aspx");
    }

    protected void btnMenu_Click(object sender, EventArgs e)
    {
        Response.Redirect("Mnu_Configuracion.aspx");
    }


    protected void gvAgencias_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        if (e.RowIndex > -1)
        {
            string string_codcco = gvAgencias.DataKeys[e.RowIndex].Value.ToString();
            decimal codcco = decimal.Parse(string_codcco);

            try
            {
                if (eliminarEntradaDB(codcco))
                    ClientScript.RegisterStartupScript(this.GetType(), "Exito", "alert('" + error + "');", true);
                else
                    ClientScript.RegisterStartupScript(this.GetType(), "Error", "alert('" + error + "');", true);
            }
            catch (Exception ex)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "Error", "alert('" + ex.Message.ToString() + "');", true);

            }
        }
        Response.Redirect("AdminAgencias.aspx");
    }

    private bool eliminarEntradaDB(decimal codcco)
    {
        query = "DELETE FROM BCAH96DTA.RSAGE01 WHERE CODCCO = " + codcco;


        using (iDB2Connection conn = ConexionAS400.ObtenerConexion())
        {
            try
            {
                using (iDB2Command cmd = new iDB2Command(query, conn))
                {
                    cmd.Parameters.Add(new iDB2Parameter("CODCCO", iDB2DbType.iDB2Decimal) { Value = codcco });

                    int filasAfectadas = cmd.ExecuteNonQuery();

                    if (filasAfectadas > 0)
                    {
                        error = "Agencia eliminada Exitosamente";
                        return true;
                    }
                    else
                    {
                        error = "Agencia no se pudo Elimar";
                        return false;
                    }

                }
            }
            catch (Exception ex)
            {

                error = ex.Message.ToString();
                return false;
            }
        }
    }


    protected void gvAgencias_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        string msg = string.Empty;
        if (e.RowIndex > -1)
        {
            string codigo = string.Empty;
            string nombre = string.Empty;
            decimal zona = 0;
            string marq = string.Empty;
            string rstbrch = string.Empty;
            string baseDatos = string.Empty;
            string nomServ = string.Empty;
            string ipServ = string.Empty;

            GridViewRow tmpFila = this.gvAgencias.Rows[e.RowIndex];
            Console.WriteLine(e.RowIndex);
            TableCell celda = null;

            //Recuperando codigo de agencia
            celda = tmpFila.Cells[2];
            codigo = celda.Text;

            //Recuperando el nombre o descripción del centro de costo que se esta editando
            TextBox txtNombre = (TextBox)gvAgencias.Rows[e.RowIndex].FindControl("txtNombre");
            nombre = txtNombre != null ? txtNombre.Text : string.Empty;

            //Recuperando la zona editada
            DropDownList ddlZona = (DropDownList)gvAgencias.Rows[e.RowIndex].FindControl("DropDownList1");
            zona = ddlZona != null ? decimal.Parse(ddlZona.SelectedValue) : 0;

            //Recuperando si aplica para marquesina
            DropDownList ddlMarq = (DropDownList)gvAgencias.Rows[e.RowIndex].FindControl("DropDownList2");
            marq = ddlMarq != null ? ddlMarq.SelectedValue : string.Empty;

            //Recuperando si aplica para reseteo de intentos fallidos de contrasenia
            DropDownList ddlBranch = (DropDownList)gvAgencias.Rows[e.RowIndex].FindControl("DropDownList3");
            rstbrch = ddlBranch != null ? ddlBranch.SelectedValue : string.Empty;

            //Recumeramos el valor de la base de Datos
            TextBox txtBaseDatos = (TextBox)gvAgencias.Rows[e.RowIndex].FindControl("txtNomBd");
            baseDatos = txtBaseDatos != null ? txtBaseDatos.Text : string.Empty;


            //Recumeramos el valor de la base de Datos
            TextBox txtNomServ = (TextBox)gvAgencias.Rows[e.RowIndex].FindControl("txtNServer");
            nomServ = txtNomServ != null ? txtNomServ.Text : string.Empty;

            //Recumeramos el valor de la base de Datos
            TextBox txtIpServ = (TextBox)gvAgencias.Rows[e.RowIndex].FindControl("txtIpServ");
            ipServ = txtIpServ != null ? txtIpServ.Text : string.Empty;

            if (string.IsNullOrEmpty(nombre) || ddlZona == null || ddlMarq == null || ddlBranch == null || string.IsNullOrEmpty(baseDatos) || string.IsNullOrEmpty(nomServ) || string.IsNullOrEmpty(ipServ))
            {
                error = "Campos de tabla no fueron capturados exitosamente.";
                ClientScript.RegisterStartupScript(this.GetType(), "Exito", "alert('" + error + "');", true);
                return;
            }
            decimal codCCO;
            if (!decimal.TryParse(codigo, out codCCO))
            {
                ClientScript.RegisterStartupScript(this.GetType(), "Exito", "alert('Valor de Centro costo incorrecto');", true);
                return;
            }


            this.actualizarAgencia(codCCO, nombre, zona, marq, rstbrch, baseDatos, nomServ, ipServ);

            //if (this.actualizarAgencia(int.Parse(codigo), nombre, baseDatos, nomServ, ipServ))
            //    ClientScript.RegisterStartupScript(this.GetType(), "Exito", "alert('" + error + "');", true);
            //else
            //    ClientScript.RegisterStartupScript(this.GetType(), "Error", "alert('" + error + "');", true);

        }
    }

    protected void gvAgencias_RowEditing(object sender, GridViewEditEventArgs e)
    {
        try
        {
            gvAgencias.EditIndex = e.NewEditIndex;
            BindGrid();
        }
        catch (Exception ex)
        {
            ClientScript.RegisterStartupScript(this.GetType(), "Error", "alert(' Error en edición:   {ex.Message}  ');", true);

        }
    }

    protected void gvAgencias_RowCancelingEdit(object sender, GridViewCancelEditEventArgs editEventArgs)
    {
        try
        {
            gvAgencias.EditIndex = -1;
            BindGrid();
        }
        catch (Exception ex)
        {
            ClientScript.RegisterStartupScript(this.GetType(), "Error", "alert(' Error al cancelar:   {ex.Message}  ');", true);
        }

    }

    private void actualizarAgencia(decimal codcco, string nombre, decimal zona, string marquesina, string rstBranch, string baseDatos, string nomServ, string ipServ)
    {

        using (iDB2Connection conn = ConexionAS400.ObtenerConexion())
        {
            try
            {
                query = @"UPDATE BCAH96DTA.RSAGE01  
                         SET NOMAGE = ? ,
                         ZONA = ?,
                         MARQUESINA = ?,
                         RSTBRANCH = ?,
                         NOMBD = ?,
                         NOMSER = ?,
                         IPSER = ?
                        WHERE CODCCO = ?";

                using (iDB2Command cmd = new iDB2Command(query, conn))
                {
                    cmd.Parameters.Add(new iDB2Parameter("NOMAGE", iDB2DbType.iDB2Numeric) { Value = nombre });
                    cmd.Parameters.Add(new iDB2Parameter("ZONA", iDB2DbType.iDB2Integer) { Value = zona });
                    cmd.Parameters.Add(new iDB2Parameter("MARQUESINA", iDB2DbType.iDB2VarChar) { Value = marquesina });
                    cmd.Parameters.Add(new iDB2Parameter("RSTBRANCH", iDB2DbType.iDB2VarChar) { Value = rstBranch });
                    cmd.Parameters.Add(new iDB2Parameter("NOMBD", iDB2DbType.iDB2VarChar) { Value = baseDatos });
                    cmd.Parameters.Add(new iDB2Parameter("NOMSER", iDB2DbType.iDB2VarChar) { Value = nomServ });
                    cmd.Parameters.Add(new iDB2Parameter("IPSER", iDB2DbType.iDB2VarChar) { Value = ipServ });
                    cmd.Parameters.Add(new iDB2Parameter("CODCCO", iDB2DbType.iDB2Numeric) { Value = codcco });

                    int filasAfectadas = cmd.ExecuteNonQuery();
                    
                }
            }
            catch (Exception ex)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "Error", "alert(' Error al cargar datos:  " + ex.Message + "  ');", true);
            }
        }
    }


    private void BindGrid()
    {
        using (iDB2Connection conn = ConexionAS400.ObtenerConexion())
        {
            try
            {
                query = "SELECT CODCCO, NOMAGE, NOMBD, NOMSER, IPSER, CASE ZONA WHEN 1 THEN 'CENTRO SUR' WHEN 2 THEN 'NOR OCCIDENTE' WHEN 3 THEN 'NOR ORIENTE' END AS ZONA, CASE MARQUESINA WHEN 'SI' THEN 'APLICA' WHEN 'NO' THEN 'NO APLICA' END AS MARQUESINA, CASE RSTBRANCH WHEN 'SI' THEN 'APLICA' WHEN 'NO' THEN 'NO APLICA' END AS RSTBRANCH FROM BCAH96DTA.RSAGE01 ORDER BY CODCCO";

                using (iDB2DataAdapter adapter = new iDB2DataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    gvAgencias.DataSource = dt;
                    gvAgencias.DataBind();
                }
            }
            catch (Exception ex)
            {
                ClientScript.RegisterStartupScript(this.GetType(), "Error", "alert(' Error al cargar datos:  " + ex.Message + "  ');", true);
            }

            gvAgencias.EditIndex = -1;
            BindGrid();

        }

    }


}



<%@ page language="C#" autoeventwireup="true" codefile="AgregarNvaAgencia.aspx.cs" inherits="AgregarNvaAgencia" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <link href="Content/style.css" rel="stylesheet" />
    <script src="Scripts/jsAcciones.js"></script>
    <title>Agregar Nueva Agencia</title>

    <script type="text/javascript">


</script>

</head>
<body>

    <div class="container">
        <header style="width: auto; height: 100px; background-image: url('Images/header.jpg'); border-radius: 10px; -moz-border-radius: 5px 5px 5px 5px; -webkit-border-radius: 5px 5px 5px 5px">
            <div style="float: right; margin-right: 5px;">
                <a href="javascript:logout();" style="color: white;">Cerrar Sesi&oacute;n [<%Response.Write(Session["usuario"].ToString());%>]</a>
            </div>
        </header>

        <div style="margin-top: 10px;">
            <h2 id="tituloPrincipal" style="color: red;">Agregar Nueva Agencia</h2>
        </div>

        <div class="separador"></div>

        <form id="form1" runat="server">

            <div>
                <table>
                    <tr>
                        <td>
                            <asp:Label ID="lblAgencia" runat="server" Text="Centro de Costo:" ForeColor="Black"></asp:Label>
                        </td>

                        <td colspan="3">
                            <asp:TextBox ID="txtCtroCosto" runat="server" MaxLength="3" Width="46px"></asp:TextBox>
                        </td>

                    </tr>

                    <tr>
                        <td>
                            <asp:Label ID="lblRotulo" runat="server" Text="Nombre:" ForeColor="Black"></asp:Label>
                        </td>
                        <td colspan="3">
                            <asp:TextBox ID="txtNombre" runat="server" MaxLength="40" Width="313px"></asp:TextBox>
                        </td>

                    </tr>

                    <tr>
                        <td>
                            <asp:Label ID="lblZona" runat="server" Text="Zona:" ForeColor="Black"></asp:Label>
                        </td>
                        <td>
                            <asp:DropDownList ID="ddlZona" runat="server">
                                <asp:ListItem Value="1">CENTRO SUR</asp:ListItem>
                                <asp:ListItem Value="2">NOR OCCIDENTE</asp:ListItem>
                                <asp:ListItem Value="3">NOR ORIENTE</asp:ListItem>
                            </asp:DropDownList>
                        </td>

                        <td>&nbsp;</td>

                        <td>&nbsp;</td>
                    </tr>

                    <tr>
                        <td>IP SERVER:</td>
                        <td>
                            <asp:TextBox ID="txtIPServer" runat="server" MaxLength="20"></asp:TextBox>
                        </td>
                    </tr>
                    <tr>
                        <td>Nombre SERVER:</td>
                        <td>
                            <asp:TextBox ID="txtNombreServer" runat="server" MaxLength="18"></asp:TextBox>
                        </td>
                    </tr>

                    <tr>
                        <td>Nombre Base de Datos:</td>
                        <td>
                            <asp:TextBox ID="txtNombreBaseDatos" runat="server" MaxLength="18"></asp:TextBox>
                        </td>
                    </tr>

                    <tr>
                        <td></td>
                    </tr>

                    <tr>
                        <td>Aplica Marquesina?:</td>
                        <td>
                            <asp:CheckBox ID="chkAplicaMarq" runat="server" />
                        </td>
                        <td>Aplica Reinicio Branch?:</td>
                        <td>
                            <asp:CheckBox ID="chkAplicaRstBRCH" runat="server" />
                        </td>
                    </tr>


                </table>

            </div>


            <div id="inner">

                <p style="margin-left: 280px">
                    &nbsp;<asp:Button ID="btnAceptar" runat="server" Text="Aceptar" OnClick="btnAceptar_Click" />
                    &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;
        &nbsp;<asp:Button ID="btnCancelar" runat="server" Text="Cancelar" OnClick="btnCancelar_Click" />
                    <asp:SqlDataSource ID="SqlDB2Operaciones" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" SelectCommand="SELECT CODCCO, NOMAGE, NOMSER, IPSER, HORSERV, NOMBD, ZONA, MARQUESINA, RSTBRANCH FROM BCAH96DTA.RSAGE01 WHERE (CODCCO = 0)"></asp:SqlDataSource>
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
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using IBM.Data.DB2.iSeries;


public partial class AgregarNvaAgencia : System.Web.UI.Page
{
    string qry = String.Empty;


    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["usuario"] == null)
            Response.Redirect("Default.aspx");
    }
    protected void btnCancelar_Click(object sender, EventArgs e)
    {
        Response.Redirect("AdminAgencias.aspx");
    }

    protected void btnAceptar_Click(object sender, EventArgs e)
    {

        if (!validarCampos())
        {
            this.lblError.Visible = true;
            return;
        }      

        //Si no existe el centro de costo insertar la nueva agencia
        if (!existeCentroCosto(int.Parse(this.txtCtroCosto.Text)))
        {
            //Si la agencia aplica para BRANCH TELLER entonces obligar a que llene los campos de IP y nombre de SERVER
            if (chkAplicaRstBRCH.Checked)
            {
                //Valido que la IP y NOMBRE de server no esten vacios
                if (this.txtIPServer.Text.Equals(String.Empty) || this.txtNombreServer.Text.Equals(String.Empty) || this.txtNombreBaseDatos.Text.Equals(String.Empty))
                {
                    this.lblError.Text = "Si la agencia aplica para el reset de intentos de BRANCH, favor llenar IP Y nombre de server";
                    this.lblError.Visible = true;
                    return;
                }
            }
            guardarEnTabla(int.Parse(this.txtCtroCosto.Text), this.txtNombre.Text, int.Parse(this.ddlZona.SelectedValue), this.txtIPServer.Text, this.txtNombreServer.Text, this.txtNombreBaseDatos.Text);
            Response.Redirect("AdminAgencias.aspx");
        }
    }

    private bool validarCampos()
    {
        if (this.txtCtroCosto.Text.Equals(String.Empty))
        {
            this.lblError.Text = "Centro de costo obligatorio";
            return false;
        }

        if (this.txtNombre.Text.Equals(String.Empty))
        {
            this.lblError.Text = "Nombre Obligatorio de la Agencia";
            return false;
        }

        return true;
    }

    private bool existeCentroCosto(int ctocto)
    {
        qry = "SELECT 1 FROM BCAH96DTA.RSAGE01 " +
                "WHERE CODCCO = " + ctocto;

        object existe = null;
        this.SqlDB2Operaciones.SelectCommand = qry;
        
        try
        {
            DataView tmpDV = (DataView)SqlDB2Operaciones.Select(DataSourceSelectArguments.Empty);
            existe = tmpDV.Table.Rows[0][0].ToString();
            if (existe.ToString().Equals("1"))
                return true;
            else
                return false;
        }
        catch (Exception ex)
        {
            return false;
        }
                 
    }

    private bool guardarEnTabla(int codcco, string nombre, int zona, string ipServer, string nombreServer, string baseDatos)
    {
        string aplicaMarq = "NO";
        string aplicaRSTBRCH = "NO";

        if (this.chkAplicaMarq.Checked)
            aplicaMarq = "SI";
        if (this.chkAplicaRstBRCH.Checked)
            aplicaRSTBRCH = "SI";

        if (aplicaRSTBRCH.Equals("SI"))
        {
            qry = "INSERT INTO BCAH96DTA.RSAGE01 (CODCCO, NOMAGE, NOMSER, IPSER, MARQUESINA, RSTBRANCH, ZONA, NOMBD) " +
                  " VALUES(" + codcco + ",'" + nombre + "','" + nombreServer + "','" + ipServer + "','" + aplicaMarq + "','" + aplicaRSTBRCH + "'," + zona + " , '" + baseDatos + "')";
        }
        else
        {
            qry = "INSERT INTO BCAH96DTA.RSAGE01 (CODCCO, NOMAGE, NOMSER, IPSER, MARQUESINA, RSTBRANCH, ZONA, NOMBD) " +
                  " VALUES(" + codcco + ",'" + nombre + "','" + nombreServer + "','" + ipServer + "','" + aplicaMarq + "','" + aplicaRSTBRCH + "'," + zona + " , '" + baseDatos + "')";
            //qry = "INSERT INTO BCAH96DTA.RSAGE01 (CODCCO, NOMAGE, ZONA, MARQUESINA, RSTBRANCH, NOMBD) " +
            //      "VALUES(" + codcco + ",'" + nombre + "','" + zona + "','" + aplicaMarq + "','" + aplicaRSTBRCH + "' , '" + baseDatos + "')";
        }

        this.SqlDB2Operaciones.InsertCommand = qry;

        try
        {
            this.SqlDB2Operaciones.Insert();
        }
        catch (Exception ex)
        {
            this.lblError.Text = ex.Message;
            return false;
        }

        return true;
    }
    
}
