<asp:SqlDataSource 
    ID="SqlDBGridView" 
    runat="server" 
    ConnectionString="<%$ ConnectionStrings:TuConexion %>" 
    UpdateCommand="UPDATE BCAH96DTA.RSAGE01 
                   SET NOMAGE = @descripcion, 
                       zona = @zona, 
                       marquesina = @marque, 
                       rstbranch = @rstbrch, 
                       NOMBD = @nombd 
                   WHERE CODCCO = @codcco">
    <UpdateParameters>
        <asp:Parameter Name="descripcion" Type="String" />
        <asp:Parameter Name="zona" Type="Int32" />
        <asp:Parameter Name="marque" Type="String" />
        <asp:Parameter Name="rstbrch" Type="String" />
        <asp:Parameter Name="nombd" Type="String" />
        <asp:Parameter Name="codcco" Type="Int32" />
    </UpdateParameters>
</asp:SqlDataSource>


private bool actualizarAgencia(int codcco, string descripcion, int zona, string marque, 
                               string rstbrch, string nombd)
{
    try
    {
        // Asigna los valores a los parámetros del SqlDataSource
        SqlDBGridView.UpdateParameters["descripcion"].DefaultValue = descripcion;
        SqlDBGridView.UpdateParameters["zona"].DefaultValue = zona.ToString();
        SqlDBGridView.UpdateParameters["marque"].DefaultValue = marque;
        SqlDBGridView.UpdateParameters["rstbrch"].DefaultValue = rstbrch;
        SqlDBGridView.UpdateParameters["nombd"].DefaultValue = nombd;
        SqlDBGridView.UpdateParameters["codcco"].DefaultValue = codcco.ToString();

        // Ejecutar actualización
        SqlDBGridView.Update();

        return true; // Si no hay errores, retorna true
    }
    catch (Exception ex)
    {
        error = ex.Message;
        return false;
    }
}
