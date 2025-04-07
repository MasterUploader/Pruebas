<asp:DropDownList ID="ddlAgencias" runat="server"
    DataSourceID="DBDataSourceFiltroAgencia"
    DataTextField="NOMAGE"
    DataValueField="CODCCO"
    AutoPostBack="True"
    OnSelectedIndexChanged="ddlAgencias_SelectedIndexChanged"
    OnDataBound="ddlAgencias_DataBound">
</asp:DropDownList>


// Esto asegura que se seleccione "0" si está disponible
protected void ddlAgencias_DataBound(object sender, EventArgs e)
{
    if (!IsPostBack)
    {
        ListItem item = ddlAgencias.Items.FindByValue("0");
        if (item != null)
        {
            ddlAgencias.SelectedValue = "0";
        }
    }
}

// Este se ejecuta cuando el usuario cambia la agencia en pantalla (opcional)
protected void ddlAgencias_SelectedIndexChanged(object sender, EventArgs e)
{
    // Si necesitas que cambie el contenido del GridView cuando cambia la agencia
    gvMantMsjs.DataBind();
}
