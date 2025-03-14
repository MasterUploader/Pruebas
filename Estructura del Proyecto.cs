<script src="Scripts/jquery-1.7.1.min.js"></script>
<script>
    $(document).ready(function () {
        // Mostrar ventana de carga cuando se presiona el botón Resetear
        $("#btnResetear").click(function () {
            $("#loadingScreen").fadeIn();
            $(this).prop("disabled", true);
        });

        // Mostrar ventana de carga cuando se selecciona una sucursal en el DropDownList
        $("#ddlAgencias").change(function () {
            $("#loadingScreen").fadeIn();
        });

        // Ocultar la ventana de carga cuando la respuesta del servidor llega
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            $("#loadingScreen").fadeOut();
            $("#btnResetear").prop("disabled", false);
        });
    });
</script>
