@model SitiosIntranet.Web.Models.UserLogin

@{
    ViewData["Title"] = "Iniciar Sesión";
    Layout = null; // Si luego tienes un _LoginLayout, lo puedes usar aquí
}

<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width" />
    <title>@ViewData["Title"]</title>
    <link rel="stylesheet" href="~/css/bootstrap.min.css" />
</head>
<body class="bg-light">

    <div class="container">
        <div class="row justify-content-center align-items-center vh-100">
            <div class="col-md-4">
                <div class="card shadow">
                    <div class="card-header text-center bg-primary text-white">
                        <h5 class="mb-0">Iniciar Sesión</h5>
                    </div>
                    <div class="card-body">
                        <form asp-action="Login" method="post">
                            <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

                            <div class="mb-3">
                                <label asp-for="Username" class="form-label">Usuario</label>
                                <input asp-for="Username" class="form-control" autocomplete="username" />
                                <span asp-validation-for="Username" class="text-danger"></span>
                            </div>

                            <div class="mb-3">
                                <label asp-for="Password" class="form-label">Contraseña</label>
                                <input asp-for="Password" class="form-control" type="password" autocomplete="current-password" />
                                <span asp-validation-for="Password" class="text-danger"></span>
                            </div>

                            <div class="d-grid">
                                <button type="submit" class="btn btn-primary">Ingresar</button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src="~/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/jquery.min.js"></script>
    <script src="~/js/jquery.validate.min.js"></script>
    <script src="~/js/jquery.validate.unobtrusive.min.js"></script>
</body>
</html>
