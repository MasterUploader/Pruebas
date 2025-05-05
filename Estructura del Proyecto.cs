using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using IBM.Data.DB2.iSeries;


/*
 * autor harold.coello 08052014
 * Se utilizan los datasource programaticamente para hacer las conexiones a la base de datos esto porque
 * hay problemas con el client access en el servidor donde se utilizara, por tanto, es necesario usar los controles
 * y definir las consultas para las operaciones de mantenimiento ABM
 * 
 */

public partial class mant_videos : System.Web.UI.Page
{
    private string query = string.Empty;
    private string error = string.Empty;
    

    protected void Page_Load(object sender, EventArgs e)
    {
        

        if (Session["usuario"] == null)
            Response.Redirect("Default.aspx");
        else
        {   
            //scriptManagerPanelGrid.RegisterAsyncPostBackControl(DBDataSource1);
            this.lblError.Visible = false;

            if (Session["video_subido"] != null) //Si se subio un video 
            {
                //this.lblError.Text = Session["video_subido"].ToString();
                //this.lblError.Visible = true;
            }

        }

        if (!(this.IsPostBack))
        {
            ddlFiltroAgencia.SelectedValue = "0";
        }
    }
    
    protected void btnAgregarNuevo_Click(object sender, EventArgs e)
    {
        Response.Redirect("AgregarNvoVideo.aspx");
    }

    //al momento de eliminar el video de la BD
    protected void gvLstaVideos_RowDeleted(object sender, GridViewDeletedEventArgs e)
    {
        //Sin implementar
    }


    /*
     * Se utiliza este evento debido a que la eliminacion del registro se debe a una rutina definida ubicado
     * dentro de un UpdatePanel para que una vez eliminado solo se actualice el contenido del gridView
     */
    protected void gvLstaVideos_RowDeleting(object sender, GridViewDeleteEventArgs e)
    {
        //celda 4 6
        string tmpNombre = string.Empty;
        string tmpAgencia = string.Empty;
        int codvideo = -1;
        TableCell celda = null;
        
        GridViewRow tmpFila = this.gvLstaVideos.Rows[e.RowIndex];

        celda = tmpFila.Cells[4];
        codvideo = int.Parse(celda.Text); //Recuperando el codigo del video en cuestion

        celda = tmpFila.Cells[5]; //Recuperando el nombre del video 
        tmpNombre = celda.Text;

        //Recuperando el nombre de la agencia
        tmpAgencia = Convert.ToString(gvLstaVideos.DataKeys[e.RowIndex].Value);



        eliminarArchivoDServer(codvideo, tmpAgencia);
        eliminarEntradadeDB(codvideo, tmpNombre, tmpAgencia);

        e.Cancel = true;
        

        //LJ7
        this.lblError.Text = "";
        this.lblError.Visible = false;


        //updPanelGrid.Update();
        Response.Redirect("mant_videos.aspx");       
    }


    //Elimina archivo del contenedor ubicado en el servidor 
    private bool eliminarArchivoDServer(int codVideo, string tmpAgencia) //LJ7
    {
        string nombreArchivo = getNombreArchivo(codVideo);
        string rutaArchivo = "\\\\HNCSTG010095WAP\\Marquesin\\" + nombreArchivo;
        


        if (tmpAgencia == "General")
        {
            if (System.IO.File.Exists(@rutaArchivo))
            {
                try
                {
                    System.IO.File.Delete(@rutaArchivo); //Eliminando el archivo del directorio
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
            else
                return false;
        }
        else
        {
            if (getDependientes(nombreArchivo) == 1)
            {
                if (System.IO.File.Exists(@rutaArchivo))
                {
                    try
                    {
                        System.IO.File.Delete(@rutaArchivo); //Eliminando el archivo del directorio
                        return true;
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                }
                else
                    return false;
            }

            return true;
        }

     }

    /*
     * Elimina el registro de la base de datos del video, con el codigo seteado
     */
    private bool eliminarEntradadeDB(int codvideo, string tmpNombre, string tmpAgencia) //LJ7
    {

        if (tmpAgencia == "General")
        {
            query = "DELETE FROM BCAH96DTA.MANTVIDEO WHERE NOMBRE = (SELECT NOMBRE FROM BCAH96DTA.MANTVIDEO WHERE CODVIDEO = " + codvideo + ")";

            DBDataSource1.DeleteCommand = query;

            if (DBDataSource1.Delete() > 0)
                return true;

            return false;
        }
        else
        {
            query = "DELETE FROM BCAH96DTA.MANTVIDEO WHERE CODVIDEO = " + codvideo;

            DBDataSource1.DeleteCommand = query;

            if (DBDataSource1.Delete() > 0)
                return true;

            return false;
        }
    }


    private string getRutaVideo(int codVideo)
    {
        query = "SELECT RUTA FROM BCAH96DTA.MANTVIDEO WHERE CODVIDEO = " + codVideo;
        DBDataSource1.SelectCommand = query;

        DataView tmpGV = (DataView)DBDataSource1.Select(DataSourceSelectArguments.Empty);
        object objNvoID = tmpGV.Table.Rows[0][0];

        if (objNvoID != null && !objNvoID.ToString().Equals(String.Empty))
            return objNvoID.ToString();
        else
            return string.Empty;               

    }


    private string getNombreArchivo(int codVideo)
    {
        query = "SELECT NOMBRE FROM BCAH96DTA.MANTVIDEO WHERE CODVIDEO = " + codVideo;
        DBDataSource1.SelectCommand = query;

        DataView tmpGV = (DataView)DBDataSource1.Select(DataSourceSelectArguments.Empty);
        object objNvoID = tmpGV.Table.Rows[0][0];

        if (objNvoID != null && !objNvoID.ToString().Equals(String.Empty))
            return objNvoID.ToString();
        else
            return string.Empty;

    }


    protected void btnVolver_Click(object sender, EventArgs e)
    {
        Response.Redirect("MnuPrincipal.aspx");
    }

    protected void gvLstaVideos_RowUpdating(object sender, GridViewUpdateEventArgs e)
    {
        if (e.RowIndex > -1)
        {
            string agencia = string.Empty;
            string ageslc = string.Empty;
            string estado = string.Empty;
            string tmpNombre = string.Empty;
            int codVideo = 0;
            int seq = 0;

            ageslc = this.ddlFiltroAgencia.SelectedValue;

            GridViewRow tmpFila = this.gvLstaVideos.Rows[e.RowIndex];
            TableCell celda = null;

            //Recuperando el codigo de la agencia            
            agencia = ((DropDownList)(tmpFila.Cells[1].Controls[1])).SelectedValue.ToString(); //celda.Text;
                        
            //Recuperando el numero de secuencia
            string tmpSeq = ((TextBox)(tmpFila.Cells[2].Controls[0])).Text; //Se recupera temporalmente el codigo de secuencia en una variable de tipo string

            celda = tmpFila.Cells[5]; //Recuperando el nombre del video 
            tmpNombre = celda.Text;

            try
            {
                seq = int.Parse(tmpSeq);
            }
            catch (Exception ex)
            {
                seq = 0;
            }

            //Recuperando el codigo del video 
            celda = tmpFila.Cells[4];
            codVideo = int.Parse(celda.Text);

            //Recuperando el estado del video
            estado = ((DropDownList)(tmpFila.Cells[6].Controls[1])).SelectedValue.ToString();


            if (actualizarVideo(agencia, seq, codVideo, estado, tmpNombre, ageslc))
                lblError.Text = "Repositorio de Video Actualizado";
            else
                lblError.Text = error;

            lblError.Visible = true;

            Response.Redirect("mant_videos.aspx");         
        }
       
    }

    private bool actualizarVideo(string agencia, int seq, int codVideo, string estado, string nombre, string selecc)
    {
        if (agencia == "0" && selecc == "0")
        {
            query = "UPDATE BCAH96DTA.MANTVIDEO " +
                "SET SEQ = " + seq + ", estado ='" + estado + "' " +
                "WHERE NOMBRE = '" + nombre + "'";
        }
        else
        {
            query = "UPDATE BCAH96DTA.MANTVIDEO " +
                "SET CODCCO ='" + agencia + "', SEQ = " + seq + ", estado ='" + estado + "' " +
                "WHERE CODVIDEO = " + codVideo;
        }
        

        DBDataSource1.UpdateCommand = query;

        try
        {
            DBDataSource1.Update();
            return true;
            
        }catch(Exception ex)
        {
            error = "Error en la actualizacion: " + ex.Message;
            return false;
        }
                
    }
    protected void ddlFiltroAgencia_SelectedIndexChanged(object sender, EventArgs e)
    {
        this.lblError.Text = "";
        this.lblError.Visible = false;
    }

    protected void gvLstaVideos_PageIndexChanged(object sender, EventArgs e)
    {
        this.lblError.Text = "";
        this.lblError.Visible = false;
    }

    private int getDependientes(string nombre)
    {
        int ret = 0;

        
        query = "SELECT COUNT(*) FROM BCAH96DTA.MANTVIDEO WHERE NOMBRE = '" + nombre + "'";
        DBDataSource1.SelectCommand = query;

        try
        {
            DataView tmpGV = (DataView)DBDataSource1.Select(DataSourceSelectArguments.Empty);
            object dep = tmpGV.Table.Rows[0][0];
            ret = (int) (dep);
        }catch(Exception ex)
        {
            ret = 0;
        }
                
       return ret;

    }
    protected void ddlZonas_SelectedIndexChanged(object sender, EventArgs e)
    {
        this.lblError.Text = "";
        this.lblError.Visible = false;
        ddlFiltroAgencia.SelectedIndex = 0;
    }
    protected void gvLstaVideos_RowEditing(object sender, GridViewEditEventArgs e)
    {
      
    }
   
}




<%@ language="C#" autoeventwireup="true" codefile="mant_videos.aspx.cs" inherits="mant_videos" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <link href="Content/style.css" rel="stylesheet" />
    <script src="Scripts/jsAcciones.js"></script>
    <title>Mantenimiento de Videos</title> 
    </head>
<body>
    <form id="form1" runat="server">

    <div class ="container">
        <header style="width:auto; height:100px; background-image:url('Images/header.jpg'); border-radius:10px; -moz-border-radius:5px 5px 5px 5px;-webkit-border-radius:5px 5px 5px 5px">
            <div style="float:right; margin-right:5px;">
               <a href="javascript:logout();" style="color:white;">Cerrar Sesi&oacute;n [<%Response.Write(Session["usuario"].ToString());%>]</a>         
            </div>
        </header>  

        <div style="margin-top:10px;">
             <h2 id="tituloPrincipal" style="color:red;">Mantenimiento de Videos</h2>
        </div>

        <div class="separador"></div>

        <a href="AgregarNvoVideo.aspx" style="margin-top:10px;"><img alt="btn_agregar_video" src="Images/agregar_video.png" border="0px"/></a>

         <fieldset>
            <legend >Filtro</legend>
                <asp:Label ID="lblFiltro" runat="server" Text="Agencia:   "></asp:Label>
                <asp:DropDownList ID="ddlFiltroAgencia" runat="server" DataSourceID="DBDataSource2" DataTextField="NOMAGE" DataValueField="CODCCO" OnSelectedIndexChanged="ddlFiltroAgencia_SelectedIndexChanged" AutoPostBack="True" OnLoad="Page_Load">
                </asp:DropDownList>
            
        &nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;

            </fieldset>

        <div class="separador"></div>

                <asp:GridView ID="gvLstaVideos" runat="server" AllowPaging="True" AutoGenerateColumns="False" AutoGenerateDeleteButton="True" AutoGenerateEditButton="True" BackColor="White" BorderColor="#CC9966" BorderStyle="None" BorderWidth="1px" CellPadding="4" DataKeyNames="AGENCIA,CODVIDEO" DataSourceID="DBDataSource4" OnPageIndexChanged="gvLstaVideos_PageIndexChanged" OnRowDeleted="gvLstaVideos_RowDeleted" OnRowDeleting="gvLstaVideos_RowDeleting" OnRowEditing="gvLstaVideos_RowEditing" OnRowUpdating="gvLstaVideos_RowUpdating" Width="948px">
                    <Columns>
                        <asp:TemplateField HeaderText="AGENCIA" SortExpression="AGENCIA">
                            <EditItemTemplate>
                                <asp:DropDownList ID="DropDownList2" runat="server" DataSourceID="DBDataSourceAgencias" DataTextField="NOMAGE" DataValueField="CODCCO">
                                </asp:DropDownList>
                                <asp:SqlDataSource ID="DBDataSourceAgencias" runat="server" ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" 
                                    SelectCommand="SELECT CODCCO, NOMAGE, IPSER, NOMSER FROM BCAH96DTA.RSAGE01 WHERE (MARQUESINA = 'SI') ORDER BY NOMAGE">
                                    
                                  

                                </asp:SqlDataSource>
                            </EditItemTemplate>
                            <ItemTemplate>
                                <asp:Label ID="Label2" runat="server" Text='<%# Bind("AGENCIA") %>'></asp:Label>
                            </ItemTemplate>
                            <HeaderStyle HorizontalAlign="Left" Width="100px" />
                            <ItemStyle HorizontalAlign="Left" Width="100px" />
                        </asp:TemplateField>
                        <asp:BoundField DataField="SEQ" HeaderText="NO. SEQ" SortExpression="SEQ">
                        <HeaderStyle HorizontalAlign="Left" Width="70px" />
                        <ItemStyle HorizontalAlign="Left" Width="70px" />
                        </asp:BoundField>
                        <asp:BoundField DataField="RUTA" HeaderText="RUTA" SortExpression="RUTA" Visible="False" />
                        <asp:BoundField DataField="CODVIDEO" HeaderText="CÓDIGO" ReadOnly="True" SortExpression="CODVIDEO">
                        <HeaderStyle HorizontalAlign="Left" Width="80px" />
                        <ItemStyle HorizontalAlign="Left" Width="80px" />
                        </asp:BoundField>
                        <asp:BoundField DataField="NOMBRE" HeaderText="NOMBRE" ReadOnly="True" SortExpression="NOMBRE">
                        <HeaderStyle HorizontalAlign="Left" />
                        <ItemStyle HorizontalAlign="Left" />
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
                            <HeaderStyle HorizontalAlign="Center" Width="100px" />
                            <ItemStyle HorizontalAlign="Center" Width="100px" />
                        </asp:TemplateField>
                    </Columns>
                    <FooterStyle BackColor="#FFFFCC" ForeColor="#330099" />
                    <HeaderStyle BackColor="#990000" Font-Bold="True" ForeColor="#FFFFCC" />
                    <PagerStyle BackColor="#FFFFCC" ForeColor="#330099" HorizontalAlign="Center" />
                    <RowStyle BackColor="White" ForeColor="#330099" />
                    <SelectedRowStyle BackColor="#FFCC66" Font-Bold="True" ForeColor="#663399" />
                    <SortedAscendingCellStyle BackColor="#FEFCEB" />
                    <SortedAscendingHeaderStyle BackColor="#AF0101" />
                    <SortedDescendingCellStyle BackColor="#F6F0C0" />
                    <SortedDescendingHeaderStyle BackColor="#7E0000" />
                </asp:GridView>
            <br />

        <asp:Button 
            ID="btnVolver" 
            runat="server" 
            OnClick="btnVolver_Click" 
            Text="Volver" />


        <asp:SqlDataSource 
            ID="DBDataSource4" 
            runat="server" 
            ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" 
            ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" 
            SelectCommand="SELECT A.CODCCO As ctocto, A.NOMAGE AS AGENCIA, ZONA AS Zone, 
CASE ZONA WHEN 1 THEN 'CENTRO SUR' WHEN 2 THEN 'NOR OCCIDENTE' WHEN 3 THEN 'NOR ORIENTE' END AS ZONA,
B.SEQ, B.RUTA, B.CODVIDEO, B.NOMBRE, B.ESTADO
 FROM BCAH96DTA.MANTVIDEO B, BCAH96DTA.RSAGE01 A WHERE B.CODCCO = A.CODCCO" 
            FilterExpression="ctocto = '{0}'"
            UpdateCommand="UPDATE BCAH96DTA.MANTVIDEO
                          SET NOMBRE = ?, ESTADO = ?
                          WHERE CODVIDEO = ?" >  

            <FilterParameters>
                <asp:ControlParameter
                     Name="CODCCO" 
                    ControlID="ddlFiltroAgencia" 
                    PropertyName="SelectedValue" />
            </FilterParameters>          

        </asp:SqlDataSource>
        <asp:SqlDataSource 
            ID="DBDataSource1" 
            runat="server" 
            ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" 
            ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" 
            SelectCommand="SELECT RUTA FROM BCAH96DTA.MANTVIDEO">
        </asp:SqlDataSource>


        <asp:SqlDataSource 
            ID="DBDataSource2" 
            runat="server" 
            ConnectionString="<%$ ConnectionStrings:ConnectionString2 %>" 
            ProviderName="<%$ ConnectionStrings:ConnectionString2.ProviderName %>" 
            SelectCommand="SELECT CODCCO, NOMAGE, NOMSER, IPSER, HORSERV, ZONA as Zone FROM BCAH96DTA.RSAGE01 WHERE (MARQUESINA = 'SI') ORDER BY NOMAGE">
        </asp:SqlDataSource>

        <br />


        <p>
             <asp:Label ID="lblError" runat="server" Text="lblError" ForeColor="Red"></asp:Label>
        </p>
        
    </div>

    </form>
    
</body>
</html>




