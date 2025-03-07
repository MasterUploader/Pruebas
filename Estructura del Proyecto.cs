<asp:SqlDataSource 
    ID="SqlDBGridView" 
    runat="server" 
    ConnectionString="<%$ ConnectionStrings:TuConexion %>" 
    SelectCommand="SELECT CODCCO, NOMAGE FROM BCAH96DTA.RSAGE01"
    UpdateCommand="UPDATE BCAH96DTA.RSAGE01 
                   SET NOMAGE = @Nombre 
                   WHERE CODCCO = @CODCCO">
    
    <UpdateParameters>
        <asp:Parameter Name="Nombre" Type="String" />
        <asp:Parameter Name="CODCCO" Type="Int32" />
    </UpdateParameters>
</asp:SqlDataSource>
