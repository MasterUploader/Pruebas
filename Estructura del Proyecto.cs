<script src="Scripts/jquery-1.7.1.min.js"></script>
<script>
    $(document).ready(function () {
        iniciarEventos();
    });

    // Detectar cuando una solicitud AJAX (UpdatePanel) finaliza
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
        $("#loadingScreen").fadeOut(); // Ocultar la pantalla de carga
        $("#btnResetear").prop("disabled", false); // Reactivar el botón
        iniciarEventos(); // Volver a adjuntar eventos después del PostBack
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
