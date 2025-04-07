protected void ddlFiltroAgencia_DataBound(object sender, EventArgs e)
{
    if (!IsPostBack)
    {
        // Insertar item al inicio
        ddlFiltroAgencia.Items.Insert(0, new ListItem("-- Seleccione Agencia --", "0"));

        // Si quieres seleccionar ese por defecto
        ddlFiltroAgencia.SelectedIndex = 0;
    }
}
