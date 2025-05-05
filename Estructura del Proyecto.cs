<%@ Page Language="C#" AutoEventWireup="true" CodeFile="mant_msg.aspx.cs" Inherits="mant_msg" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>Mantenimiento de Mensajes</title>
    <link href="Content/style.css" rel="stylesheet" />
    <script src="Scripts/jsAcciones.js"></script>
</head>

<body>
    <form id="form1" runat="server">
    <div class="container">

         <header style="width:auto; height:100px; background-image:url('Images/header.jpg'); border-radius:10px; -moz-border-radius:5px 5px 5px 5px;-webkit-border-radius:5px 5px 5px 5px">
            <div style="float:right; margin-right:5px;">
               <a href="javascript:logout();" style="color:white;">Cerrar Sesi&oacute;n [<%Response.Write(Session["usuario"].ToString());%>]</a>         
            </div>
        </header>
        
        <div style="margin-top:10px;">
             <h2 id="tituloPrincipal" style="color:red;">Mantenimiento de Mensajes</h2>
        </div>

        <p>
            <asp:Button ID="btnAgregarNvoMsj" runat="server" Text="Agregar Nuevo Mensaje" OnClick="btnAgregarNvoMsj_Click" />
        </p> 

        <div class="separador"></div>

        <fieldset>
            <legend>Filtro</legend>

            <asp:Label ID="lblFiltroAgencia" runat="server" Text="Agencia:"></asp:Label>

            <asp:DropDownList ID="ddlAgencias" runat="server" DataSourceID="DBDataSourceFiltroAgencia" DataTextField="NOMAGE" DataValueField="CODCCO" AutoPostBack="True" OnSelectedIndexChanged="ddlAgencias_SelectedIndexChanged" OnLoad="Page_Load">
            </asp:DropDownList>

        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;

            </fieldset>

        <div class="separador"></div>
            
                <asp:GridView ID="gvMantMsjs" runat="server" AutoGenerateColumns="False" CellPadding="4" DataKeyNames="CODIGO" DataSourceID="DBDataSourceGridView" ForeColor="#333333" GridLines="None" OnRowDeleting="gvMantMsjs_RowDeleting" OnRowUpdating="gvMantMsjs_RowUpdating" Width="950px" AllowPaging="True">
                    <AlternatingRowStyle BackColor="White" />
                    <Columns>
                        <asp:CommandField ShowEditButton="True" ButtonType="Image" EditImageUrl="~/Images/editar_2.png" EditText="" CancelImageUrl="~/Images/cancelar.png" UpdateImageUrl="~/Images/aceptar.png">
                        <ItemStyle Width="50px" />
                        </asp:CommandField>
                        <asp:CommandField ShowDeleteButton="True" ButtonType="Image" DeleteImageUrl="~/Images/eliminar.png">
                        <ItemStyle Width="50px" />
                        </asp:CommandField>
                        <asp:BoundField DataField="CODIGO" HeaderText="CODIGO" ReadOnly="True" SortExpression="CODIGO">
                        <HeaderStyle HorizontalAlign="Left" Width="80px" />
                        <ItemStyle HorizontalAlign="Left" Width="80px" />
                        </asp:BoundField>
                        <asp:BoundField DataField="SEQ" HeaderText="SEQ" SortExpression="SEQ">
                        <HeaderStyle HorizontalAlign="Left" Width="30px" />
                        <ItemStyle HorizontalAlign="Left" Width="30px" />
                        </asp:BoundField>
                        <asp:BoundField DataField="MENSAJE" HeaderText="MENSAJE" SortExpression="MENSAJE">
                        <HeaderStyle HorizontalAlign="Left" />
                        <ItemStyle HorizontalAlign="Left" Wrap="True" />
                        </asp:BoundField>
                        <asp:TemplateField HeaderText="ESTADO" SortExpression="ESTADO">
                            <EditItemTemplate>
                                <asp:DropDownList ID="DropDownList1" runat="server">
                                    <asp:ListItem Value="A">Activo</asp:ListItem>
                                    <asp:ListItem Value="I">Inactivo</asp:ListItem>
                                </asp:DropDownList>
                            </EditItemTemplate>
                            <ItemTemplate>
                                <asp:Label ID="Label1" runat="server" Text='<%# Bind("ESTADO") %>'></asp:Label>
                            </ItemTemplate>
                            <HeaderStyle HorizontalAlign="Left" Width="50px" />
                            <ItemStyle HorizontalAlign="Left" Width="50px" />
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
            
        <asp:SqlDataSource 
            ID="DBDataSourceOpeMant" 
            runat="server" 
            ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" 
            ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" 
            SelectCommand="SELECT 1 AS Expr1 FROM BCAH96DTA.MANTMSG">
        </asp:SqlDataSource>

        <asp:SqlDataSource 
            ID="DBDataSourceGridView" 
            runat="server" 
            ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" 
            ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" 
            SelectCommand="SELECT B.CODCCO AS ctocto, B.NOMAGE AS AGENCIA, 
A.CODMSG AS CODIGO, A.SEQ, A.MENSAJE, A.ESTADO FROM BCAH96DTA.MANTMSG A, BCAH96DTA.RSAGE01 B WHERE A.CODCCO = B.CODCCO AND A.CODMSG &gt; 0 "
           FilterExpression="ctocto = '{0}'">

            <FilterParameters>
                <asp:ControlParameter Name="CODCCO" ControlID="ddlAgencias" PropertyName="SelectedValue" />
            </FilterParameters> 

        </asp:SqlDataSource>

        <asp:SqlDataSource ID="DBDataSourceFiltroAgencia" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" SelectCommand="SELECT CODCCO, NOMAGE, ZONA as Zone  FROM BCAH96DTA.RSAGE01 WHERE (MARQUESINA = 'SI') 
ORDER BY NOMAGE">

        </asp:SqlDataSource>

          <br />

          <p>
            <asp:Button ID="btnVolver" runat="server" Text="Volver" OnClick="btnVolver_Click" />
        </p>
        
        <p>
            <asp:Label ID="lblError" runat="server" Text="lblError" ForeColor="Red" Visible="False"></asp:Label>
        </p>
    </div>

    </form>

</body>
</html>





using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class mant_msg : System.Web.UI.Page
{
    string query = string.Empty;
    private string error = string.Empty;

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Session["usuario"] == null)
            Response.Redirect("Default.aspx");
        if (!(this.IsPostBack))
        {   
           ddlAgencias.SelectedValue = "0";
        }
    }
    protected void btnVolver_Click(object sender, EventArgs e)
    {
        Response.Redirect("MnuPrincipal.aspx");
    }
    


    /*Elimina el mensaje de la base de datos con el codigo de mensaje enviado*/
    private bool eliminarEntradadeDB(int codMsg)
    {
        query = "DELETE FROM BCAH96DTA.MANTMSG WHERE CODMSG = " + codMsg;

        DBDataSourceOpeMant.DeleteCommand = query;

        if (DBDataSourceOpeMant.Delete() > 0)
            return true;
        else
            return false;        
    }


    protected void btnAgregarNvoMsj_Click(object sender, EventArgs e)
    {
        Response.Redirect("AgregarNvoMensaje.aspx");
    }

    /*
     * Ejecuta la operacion de UPDATE en la base de datos
     */
    private bool actualizarMensaje(string agencia, int codMsg, int sequencia, string mensaje, string estado)
    {
        //agencia = agencia.Equals("0") ? "000" : agencia;

        query = "UPDATE BCAH96DTA.MANTMSG " +
                "SET SEQ= " + sequencia + ",MENSAJE = '" + mensaje + "',ESTADO='" + estado + "' " +
                "WHERE CODMSG = " + codMsg + " AND CODCCO = '" + agencia + "'";

        DBDataSourceOpeMant.UpdateCommand = query;

        try
        {
            DBDataSourceOpeMant.Update();
            return true;    
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
        
    }
        
    protected void gvMantMsjs_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e.RowIndex > -1)
        {
            string agencia = string.Empty;
            string mensaje = string.Empty;
            string estado = string.Empty;
            int codMsg = 0;
            int seq = 0;

            GridViewRow tmpFila = this.gvMantMsjs.Rows[e.RowIndex];
            TableCell celda = null;

            //Recuperando el codigo de la agencia
            agencia = this.ddlAgencias.SelectedValue;
            //celda = tmpFila.Cells[1];
            //agencia = getCentroCosto(celda.Text);

            //Recuperando el codigo del mensaje
            celda = tmpFila.Cells[2];
            codMsg = int.Parse(celda.Text);

            string tmpSeq = ((TextBox)(tmpFila.Cells[3].Controls[0])).Text; //Se recupera temporalmente el codigo de secuencia en una variable de tipo string

            //Recuperando el mensaje actualizado o a cambiar
            mensaje = ((TextBox)(tmpFila.Cells[4].Controls[0])).Text;

            //Recuperando el mensaje actualizado
            estado = ((DropDownList)(tmpFila.Cells[5].Controls[1])).SelectedValue.ToString();

            try
            {
                seq = int.Parse(tmpSeq);
            }
            catch (Exception ex)
            {
                seq = 0;
            }
            

            if (actualizarMensaje(agencia, codMsg, seq, mensaje, estado))
                lblError.Text = "Repositorio de mensajes actualizado";
            else
                lblError.Text = error;

            lblError.Visible = true;


            e.Cancel = true;
            Response.Redirect("mant_msg.aspx");
        }
    }
    protected void gvMantMsjs_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        string tmpNombre = string.Empty;
        int codMsg = -1;

        GridViewRow tmpFila = this.gvMantMsjs.Rows[e.RowIndex];

        TableCell celda = tmpFila.Cells[4];
        tmpNombre = celda.Text; //Recuperando el nombre del video en cuestion

        celda = tmpFila.Cells[2];
        codMsg = int.Parse(celda.Text); //Recuperando el numero unico del video

        eliminarEntradadeDB(codMsg);
        //updPanelGridView.Update();

        e.Cancel = true;
        Response.Redirect("mant_msg.aspx");
    }

    private string getCentroCosto(string agencia)
    {
        query = "SELECT CODCCO FROM BCAH96DTA.RSAGE01 WHERE NOMAGE = '" + agencia + "'";
        DBDataSourceOpeMant.SelectCommand = query;

        DataView tmpDV = (DataView)DBDataSourceOpeMant.Select(DataSourceSelectArguments.Empty);
        object objNvoID = tmpDV.Table.Rows[0][0];

        if (objNvoID != null && !objNvoID.ToString().Equals(String.Empty))
            return objNvoID.ToString();
        else
            return string.Empty;

    }
    protected void ddlAgencias_SelectedIndexChanged(object sender, EventArgs e)
    {
       
    }
    
}
