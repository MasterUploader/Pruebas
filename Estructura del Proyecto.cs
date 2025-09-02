# Limpia contenido sin eliminar la carpeta base
Get-ChildItem -Path "C:\$Recycle.Bin\*" -Force | ForEach-Object {
    Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
