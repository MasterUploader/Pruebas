<asp:ScriptManager runat="server" />

<div id="loadingScreen">
    <span>Procesando... Por favor, espere</span>
</div>

<asp:UpdatePanel ID="upPanel" runat="server">
    <ContentTemplate>
        <asp:DropDownList ID="ddlAgencias" runat="server" AutoPostBack="True"
            DataSourceID="DB2dataSourceAgencias" 
            DataTextField="NOMAGE" 
            DataValueField="DatosAgencia"
            OnSelectedIndexChanged="ddlAgencias_SelectedIndexChanged">
        </asp:DropDownList>

        <asp:Button ID="btnResetear" runat="server" Text="Resetear" OnClick="btnResetear_Click" />
        
        <asp:Label ID="lblMensaje" runat="server" ForeColor="Green" />
    </ContentTemplate>
    <Triggers>
        <asp:AsyncPostBackTrigger ControlID="btnResetear" EventName="Click" />
    </Triggers>
</asp:UpdatePanel>


<script src="Scripts/jquery-1.7.1.min.js"></script>
<script>
    $(document).ready(function () {
        // Cuando se presiona el botón Resetear, mostrar la pantalla de carga
        $("#btnResetear").click(function () {
            $("#loadingScreen").fadeIn();
            $(this).prop("disabled", true);
        });

        // Cuando se selecciona una agencia en el DropDownList
        $("#ddlAgencias").change(function () {
            $("#loadingScreen").fadeIn();
        });

        // Detectar cuando finaliza una solicitud AJAX en ASP.NET y ocultar la pantalla de carga
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            $("#loadingScreen").fadeOut();
            $("#btnResetear").prop("disabled", false);
        });
    });
</script>
