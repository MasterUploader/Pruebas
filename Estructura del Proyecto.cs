<asp:DropDownList ID="ddlFiltroAgencia" runat="server"
    DataSourceID="DBDataSource2"
    DataTextField="NOMAGE"
    DataValueField="CODCCO"
    AutoPostBack="True"
    OnSelectedIndexChanged="ddlFiltroAgencia_SelectedIndexChanged"
    OnDataBound="ddlFiltroAgencia_DataBound">
</asp:DropDownList>



protected void ddlFiltroAgencia_DataBound(object sender, EventArgs e)
{
    if (!IsPostBack)
    {
        ListItem item = ddlFiltroAgencia.Items.FindByValue("0");
        if (item != null)
        {
            ddlFiltroAgencia.SelectedValue = "0";
        }
    }
}

protected void ddlFiltroAgencia_SelectedIndexChanged(object sender, EventArgs e)
{
    // Si necesitas recargar algo (por ejemplo, un GridView), hazlo aquí:
    rvLstVideos.DataBind(); // solo si tienes el GridView que depende del filtro
}
