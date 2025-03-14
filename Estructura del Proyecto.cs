<script src="Scripts/jquery-1.7.1.min.js"></script>
<script>
    $(document).ready(function () {
        // Mostrar pantalla de carga cuando el usuario selecciona una sucursal
        $("#ddlAgencias").change(function () {
            $("#loadingScreen").fadeIn(); // Se activa ANTES del PostBack
        });

        // Mostrar pantalla de carga cuando se presiona el botón Resetear
        $("#btnResetear").click(function () {
            $("#loadingScreen").fadeIn();
            $(this).prop("disabled", true);
        });

        // Ocultar la ventana de carga cuando la respuesta del servidor llega
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            $("#loadingScreen").fadeOut();
            $("#btnResetear").prop("disabled", false);
        });
    });
</script>
