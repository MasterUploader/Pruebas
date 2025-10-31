Ahora necesito la interfac y la clase servicio, ademas del endpoint Health, para que haga esta consulta:

#### Verificación de Estado:
```bash
# HTTP
curl -X GET "http://localhost:8080/davivienda-tnp/api/v1/health"

# HTTPS (omitir verificación de certificado para certificados auto-firmados)
curl -X GET "https://localhost:8443/davivienda-tnp/api/v1/health" -k

igual que con el endpoint de PaymentAuthorization que maneje los errores.
