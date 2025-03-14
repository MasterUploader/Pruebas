<script src="Scripts/jquery-1.7.1.min.js"></script>
<script>
    $(document).ready(function () {
        iniciarEventos();
    });

    // Detectar actualizaciones en el UpdatePanel y reactivar eventos
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
        iniciarEventos(); // Volver a adjuntar eventos después de cada PostBack
        $("#loadingScreen").fadeOut();
        $("#btnResetear").prop("disabled", false);
    });

    function iniciarEventos() {
        // Mostrar pantalla de carga cuando el usuario cambia de sucursal
        $("#ddlAgencias").change(function () {
            $("#loadingScreen").fadeIn();
        });

        // Mostrar pantalla de carga cuando se presiona el botón Resetear
        $("#btnResetear").click(function () {
            $("#loadingScreen").fadeIn();
            $(this).prop("disabled", true);
        });
    }
</script>
