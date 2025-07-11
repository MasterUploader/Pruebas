Console.WriteLine($"[DEBUG] Ruta carpeta controlador: {controllerDirectory}");

if (!Directory.Exists(controllerDirectory))
{
    Directory.CreateDirectory(controllerDirectory);
    Console.WriteLine("[DEBUG] Carpeta creada");
}
else
{
    Console.WriteLine("[DEBUG] Carpeta ya existía");
}
