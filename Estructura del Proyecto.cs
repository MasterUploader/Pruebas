<asp:ScriptManager runat="server" />

<div id="loadingScreen">
    <span>Procesando... Por favor, espere</span>
</div>

<asp:UpdatePanel ID="upPanel" runat="server">
    <ContentTemplate>

        <!-- DropDownList con AutoPostBack activado -->
        <asp:DropDownList ID="ddlAgencias" runat="server" AutoPostBack="True"
            DataSourceID="DB2dataSourceAgencias"
            DataTextField="NOMAGE"
            DataValueField="DatosAgencia"
            OnSelectedIndexChanged="ddlAgencias_SelectedIndexChanged">
        </asp:DropDownList>

        <!-- Botón para resetear -->
        <asp:Button ID="btnResetear" runat="server" Text="Resetear" OnClick="btnResetear_Click" />

        <!-- Etiqueta para mensajes -->
        <asp:Label ID="lblMensaje" runat="server" ForeColor="Green" />

        <!-- SqlDataSource para cargar agencias -->
        <asp:SqlDataSource ID="DB2dataSourceAgencias" runat="server"
            ConnectionString="<%$ ConnectionStrings:ConnectionStringGood %>"
            ProviderName="IBM.Data.DB2"
            SelectCommand="SELECT NOMAGE, IPSER || '|' || NOMBD AS DatosAgencia FROM BCAMISOTA.RSAGE01 WHERE CODCCO > 0 ORDER BY CODCCO">
        </asp:SqlDataSource>

    </ContentTemplate>
    <Triggers>
        <asp:AsyncPostBackTrigger ControlID="ddlAgencias" EventName="SelectedIndexChanged" />
        <asp:AsyncPostBackTrigger ControlID="btnResetear" EventName="Click" />
    </Triggers>
</asp:UpdatePanel>
