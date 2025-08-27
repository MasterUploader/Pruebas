1. Crear un usuario de servicio en el servidor destino

En el servidor donde está la carpeta compartida, crea un usuario local o de dominio dedicado (ej: svcWebUpload).

Dale solo permisos NTFS mínimos (lectura/escritura) sobre la carpeta compartida, no sobre todo el servidor.

En el compartido de red (\\ServidorDestino\CarpetaCompartida), dale también permisos de Share → Change (modificar) y Read.


> ⚠️ Importante: No uses Everyone ni Usuarios autenticados. Solo ese usuario de servicio.




---

2. Configurar IIS para usar ese usuario

En el servidor donde corre IIS:

Abre IIS Manager → selecciona tu sitio → clic en Advanced Settings.

En la sección Application Pool, identifica el App Pool que usa tu sitio.

Abre Application Pools → selecciona ese pool → Advanced Settings.

Cambia la propiedad Identity a Custom Account y asigna el usuario de servicio (svcWebUpload) que creaste.

Reinicia el App Pool.
    
