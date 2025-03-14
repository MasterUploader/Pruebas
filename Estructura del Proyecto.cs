<style>
    /* Estilo del overlay de carga */
    #loadingScreen {
        position: fixed;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.5);
        top: 0;
        left: 0;
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 9999;
        display: none;
    }

    #loadingScreen span {
        background: white;
        padding: 20px;
        font-size: 18px;
        font-weight: bold;
        border-radius: 10px;
    }
</style>

<!-- Ventana de carga -->
<div id="loadingScreen">
    <span>Procesando... Por favor, espere</span>
</div>

<script src="Scripts/jquery-1.7.1.min.js"></script>
<script>
    $(document).ready(function () {
        // Mostrar ventana de carga cuando se hace clic en el botón "Resetear"
        $("#btnResetear").click(function () {
            $("#loadingScreen").fadeIn();
            $(this).prop("disabled", true);
        });

        // Mostrar ventana de carga cuando se selecciona una agencia en el DropDownList
        $("#ddlAgencias").change(function () {
            $("#loadingScreen").fadeIn();
        });

        // Ocultar la ventana de carga después de la respuesta del servidor
        Sys.WebForms.PageRequestManager.getInstance().add_endRequest(function () {
            $("#loadingScreen").fadeOut();
            $("#btnResetear").prop("disabled", false);
        });
    });
</script>
