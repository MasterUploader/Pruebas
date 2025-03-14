<script src="Scripts/jquery-1.7.1.min.js"></script>
<script>
    $(document).ready(function () {
        iniciarEventos(); // Activar eventos al cargar la página
    });

    // Detectar cuando el UpdatePanel ha sido actualizado (PostBack parcial)
    Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
        iniciarEventos(); // Reactivar eventos después de cada PostBack
        $("#loadingScreen").fadeOut(); // Ocultar pantalla de carga al finalizar el proceso
        $("#btnResetear").prop("disabled", false); // Reactivar botón Resetear
    });

    function iniciarEventos() {
        // Mostrar pantalla de carga cuando se selecciona una sucursal
        $("#ddlAgencias").off("change").on("change", function () {
            $("#loadingScreen").fadeIn();
        });

        // Mostrar pantalla de carga cuando se presiona el botón Resetear
        $("#btnResetear").off("click").on("click", function () {
            $("#loadingScreen").fadeIn();
            $(this).prop("disabled", true);
        });
    }
</script>
