Me piden este APi:


# API de Pagos DaviviendaTNP - Guía del Consumidor

## 📋 **Descripción General**

La API de Pagos DaviviendaTNP es una API REST ligera para procesar transacciones de autorización de pagos. Esta guía proporciona toda la información necesaria para integrar y consumir los servicios de la API.

## 🌐 **Información Base de la API**

### **URLs Base:**
- **HTTP**: `http://[ip-servidor]:[puerto]/davivienda-tnp/api/v1`
- **HTTPS**: `https://[ip-servidor]:[puerto]/davivienda-tnp/api/v1`

### **Puertos por Defecto:**
- **HTTP**: 8080
- **HTTPS**: 8443

### **Tipo de Contenido:**
- **Petición**: `application/json`
- **Respuesta**: `application/json`

### **Autenticación:**
- Actualmente no requiere autenticación
- Versiones futuras pueden incluir autenticación basada en API key o tokens

---

## 🔍 **Endpoints Disponibles**

### 1. **Verificación de Estado** (GET)
**Propósito**: Verificar si el servicio de la API está funcionando y saludable.

**Endpoint**: `GET /health`

**Petición**: No requiere cuerpo

**Ejemplo de Respuesta**:
```json
{
  "status": "UP",
  "service": "DaviviendaTNP Payment API"
}
```

**Códigos de Estado HTTP**:
- `200 OK`: Servicio saludable
- `5xx`: Error del servicio

---

### 2. **Autorización Manual** (POST)
**Propósito**: Procesar transacciones de autorización de pagos de forma manual.

**Endpoint**: `POST /authorization/manual`

**Cabeceras de Petición**:
```
Content-Type: application/json
```

**Estructura del Cuerpo de Petición**:
```json
{
  "GetAuthorizationManual": {
    "pMerchantID": "string",
    "pTerminalID": "string", 
    "pPrimaryAccountNumber": "string",
    "pDateExpiration": "string",
    "pCVV2": "string",
    "pAmount": "string",
    "pSystemsTraceAuditNumber": "string"
  }
}
```

**Descripción de Campos**:
- `pMerchantID`: Identificador del comercio (requerido)
- `pTerminalID`: Identificador del terminal (requerido)
- `pPrimaryAccountNumber`: Número de tarjeta de crédito/débito (requerido)
- `pDateExpiration`: Fecha de expiración de la tarjeta en formato MMAA (requerido)
- `pCVV2`: Código de verificación de la tarjeta (requerido)
- `pAmount`: Monto de la transacción (requerido)
- `pSystemsTraceAuditNumber`: Número único de traza de la transacción (requerido)

**Estructura del Cuerpo de Respuesta**:
```json
{
  "GetAuthorizationManualResponse": {
    "GetAuthorizationManualResult": {
      "responseCode": "string",
      "authorizationCode": "string",
      "transactionId": "string",
      "message": "string",
      "timestamp": "string"
    }
  }
}
```

**Descripción de Campos de Respuesta**:
- `responseCode`: Código de respuesta de la transacción (00 = aprobada)
- `authorizationCode`: Código de autorización si fue aprobada
- `transactionId`: Identificador único de la transacción
- `message`: Mensaje de respuesta legible
- `timestamp`: Marca de tiempo del procesamiento de la transacción

---

## 🔧 **Ejemplos de Integración**

### **Ejemplos con cURL**

#### Verificación de Estado:
```bash
# HTTP
curl -X GET "http://localhost:8080/davivienda-tnp/api/v1/health"

# HTTPS (omitir verificación de certificado para certificados auto-firmados)
curl -X GET "https://localhost:8443/davivienda-tnp/api/v1/health" -k
```

#### Autorización de Pago:
```bash
curl -X POST "http://localhost:8080/davivienda-tnp/api/v1/authorization/manual" \
  -H "Content-Type: application/json" \
  -d '{
    "GetAuthorizationManual": {
      "pMerchantID": "12345678",
      "pTerminalID": "TERM001",
      "pPrimaryAccountNumber": "4111111111111111",
      "pDateExpiration": "1225",
      "pCVV2": "123",
      "pAmount": "10000",
      "pSystemsTraceAuditNumber": "000001"
    }
  }'
```

### **Ejemplo en JavaScript/Node.js**:
```javascript
const axios = require('axios');

async function procesarPago() {
  try {
    const response = await axios.post(
      'http://localhost:8080/davivienda-tnp/api/v1/authorization/manual',
      {
        GetAuthorizationManual: {
          pMerchantID: "12345678",
          pTerminalID: "TERM001", 
          pPrimaryAccountNumber: "4111111111111111",
          pDateExpiration: "1225",
          pCVV2: "123",
          pAmount: "10000",
          pSystemsTraceAuditNumber: "000001"
        }
      },
      {
        headers: {
          'Content-Type': 'application/json'
        }
      }
    );
    
    console.log('Pago procesado:', response.data);
  } catch (error) {
    console.error('Pago falló:', error.response?.data || error.message);
  }
}
```

### **Ejemplo en Python**:
```python
import requests
import json

def procesar_pago():
    url = "http://localhost:8080/davivienda-tnp/api/v1/authorization/manual"
    
    payload = {
        "GetAuthorizationManual": {
            "pMerchantID": "12345678",
            "pTerminalID": "TERM001",
            "pPrimaryAccountNumber": "4111111111111111", 
            "pDateExpiration": "1225",
            "pCVV2": "123",
            "pAmount": "10000",
            "pSystemsTraceAuditNumber": "000001"
        }
    }
    
    headers = {
        'Content-Type': 'application/json'
    }
    
    try:
        response = requests.post(url, json=payload, headers=headers)
        response.raise_for_status()
        
        result = response.json()
        print("Pago procesado:", json.dumps(result, indent=2))
        
    except requests.exceptions.RequestException as e:
        print(f"Pago falló: {e}")
```

### **Ejemplo en Java**:
```java
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.net.URI;

public class ClientePagos {
    
    public void procesarPago() {
        String url = "http://localhost:8080/davivienda-tnp/api/v1/authorization/manual";
        
        String json = """
            {
              "GetAuthorizationManual": {
                "pMerchantID": "12345678",
                "pTerminalID": "TERM001",
                "pPrimaryAccountNumber": "4111111111111111",
                "pDateExpiration": "1225",
                "pCVV2": "123", 
                "pAmount": "10000",
                "pSystemsTraceAuditNumber": "000001"
              }
            }
            """;
        
        HttpClient client = HttpClient.newHttpClient();
        HttpRequest request = HttpRequest.newBuilder()
            .uri(URI.create(url))
            .header("Content-Type", "application/json")
            .POST(HttpRequest.BodyPublishers.ofString(json))
            .build();
            
        try {
            HttpResponse<String> response = client.send(request, 
                HttpResponse.BodyHandlers.ofString());
                
            System.out.println("Estado: " + response.statusCode());
            System.out.println("Respuesta: " + response.body());
            
        } catch (Exception e) {
            System.err.println("Pago falló: " + e.getMessage());
        }
    }
}
```

---

## ⚠️ **Manejo de Errores**

### **Códigos de Estado HTTP**:
- `200 OK`: Petición procesada exitosamente
- `400 Bad Request`: Formato de petición inválido o campos requeridos faltantes
- `404 Not Found`: Endpoint no encontrado
- `405 Method Not Allowed`: Método HTTP no soportado para el endpoint
- `500 Internal Server Error`: Error de procesamiento del servidor

### **Formato de Respuesta de Error**:
```json
{
  "error": "Descripción del error",
  "status": 400,
  "timestamp": 1698765432000
}
```

### **Escenarios Comunes de Error**:

#### Campos Requeridos Faltantes:
```json
{
  "error": "Campo requerido faltante o vacío: pMerchantID",
  "status": 400,
  "timestamp": 1698765432000
}
```

#### Formato JSON Inválido:
```json
{
  "error": "Formato JSON inválido: Carácter inesperado en posición 15",
  "status": 400, 
  "timestamp": 1698765432000
}
```

---

## 🔐 **Consideraciones de Seguridad**

### **Uso de HTTPS**:
- **Producción**: Siempre usar HTTPS en ambientes de producción
- **Desarrollo**: HTTP aceptable para pruebas locales
- **Certificados**: Asegurar validación apropiada de certificados SSL en producción

### **Seguridad de Datos**:
- **Cumplimiento PCI**: Asegurar cumplimiento PCI DSS al manejar datos de tarjetas
- **Datos Sensibles**: Nunca registrar o almacenar información sensible de pagos
- **Seguridad de Red**: Usar conexiones de red seguras y VPNs cuando sea aplicable

### **Mejores Prácticas**:
- Implementar timeouts de petición (recomendado: 30 segundos)
- Usar manejo apropiado de errores y lógica de reintentos
- Validar todos los datos de entrada antes de enviar peticiones
- Monitorear tiempos de respuesta y disponibilidad de la API

---

## 📊 **Pruebas**

### **Números de Tarjeta de Prueba**:
Para propósitos de prueba, usar estos números de tarjeta de prueba:
- **Visa**: 4111111111111111
- **Mastercard**: 5555555555554444
- **American Express**: 378282246310005

### **Datos de Prueba**:
- **CVV2**: Cualquier número de 3 dígitos (123, 456, 789)
- **Expiración**: Cualquier fecha futura en formato MMAA (1225, 0126)
- **Monto**: Cualquier número positivo (10000 = $100.00)

### **Pruebas de Verificación de Estado**:
```bash
# Probar si la API está disponible
curl -f "http://localhost:8080/davivienda-tnp/api/v1/health" || echo "API está caída"
```

---

## 🌐 **Configuración de Ambiente**

### **Ambiente de Desarrollo**:
```
URL Base: http://localhost:8080/davivienda-tnp/api/v1
Protocolo: HTTP
Certificado: No requerido
```

### **Ambiente de Producción**:
```
URL Base: https://[servidor-produccion]/davivienda-tnp/api/v1
Protocolo: HTTPS
Certificado: Certificado SSL válido requerido
```

---

## 📞 **Soporte y Contacto**

Para soporte de la API, asistencia de integración, o preguntas técnicas:

- **Documentación Técnica**: Este documento
- **Reporte de Problemas**: Contactar administrador del sistema
- **Soporte de Integración**: Contactar equipo de desarrollo

---

## 📝 **Registro de Cambios**

### **Versión 1.0**
- Lanzamiento inicial de la API
- Endpoint de verificación de estado
- Endpoint de autorización manual
- Manejo básico de errores
- Soporte HTTP y HTTPS

---

**Última Actualización**: Octubre 2025  
**Versión de API**: 1.0  
**Versión del Documento**: 1.0

Y esto es lo que tengo hasta el momento:


using Microsoft.AspNetCore.Mvc;
using Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

namespace Pagos_Davivienda_TNP.Controllers;

/// <summary>
/// 
/// </summary>
[Route("davivienda-tnp/api/v1")]
[ApiController]
public class PagosDaviviendaTnpController : ControllerBase
{
    /// <summary>
    /// Verificar si el servicio de la API está funcionando y saludable.
    /// </summary>
    /// <returns></returns>
    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var response = new
        {
            status = "UP",
            service = "DaviviendaTNP Payment API"
        };

        return Ok(response);
    }

    /// <summary>
    /// Procesar transacciones de autorización de pagos de forma manual.
    /// </summary>
    /// <param name="requestAuthorizationManualDto"></param>
    /// <returns></returns>
    [HttpPost("authorization/manual")]
    public async Task<IActionResult> GetAuthorizationManual([FromBody] RequestGetauthorizationManual requestAuthorizationManualDto )
    {

        var response = new ResponseAuthorizationManualDto
        {
            ResponseCode = "00",
            AuthorizationCode = "123456",
            TransactionId = "TXN789012",
            Message = "Transacción aprobada",
            Timestamp = DateTime.UtcNow
        };

        return Ok(response);

    }
}


using Newtonsoft.Json;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Modelo raíz para la solicitud de autorización manual
/// </summary>
public class RequestGetauthorizationManual
{
    public GetauthorizationManualDto GetAuthorizationManual { get; set; } = new();
}

/// <summary>
/// Modelo de datos para la solicitud de autorización manual
/// </summary>
public class GetauthorizationManualDto
{
    /// <summary>
    /// Identificador del comercio (requerido)
    /// </summary>
    [JsonRequired]
    [JsonProperty("pMerchantID")]
    public string PMerchantID { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del terminal (requerido)
    /// </summary>
    [JsonRequired]
    [JsonProperty("pTerminalID")]
    public string PTerminalID { get; set; } = string.Empty;

    /// <summary>
    /// Número de tarjeta de crédito/débito (requerido)
    /// </summary>
    [JsonRequired]
    [JsonProperty("pPrimaryAccountNumber")]
    public string PPrimaryAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de expiración de la tarjeta en formato MMAA (requerido)
    /// </summary>
    [JsonRequired]
    [JsonProperty("pDateExpiration")]
    public string PDateExpiration { get; set; } = string.Empty;

    /// <summary>
    /// Código de verificación de la tarjeta (requerido)
    /// </summary>
    [JsonRequired]
    [JsonProperty("pCVV2")]
    public string PCVV2 { get; set; } = string.Empty;

    /// <summary>
    /// Monto de la transacción (requerido)
    /// </summary>
    [JsonRequired]
    [JsonProperty("pAmount")]
    public string PAmount { get; set; } = string.Empty;

    /// <summary>
    /// Número único de traza de la transacción (requerido)
    /// </summary>
    [JsonRequired]
    [JsonProperty("pSystemsTraceAuditNumber")]
    public string PSystemsTraceAuditNumber { get; set; } = string.Empty;
}


using Newtonsoft.Json;

namespace Pagos_Davivienda_TNP.Models.Dtos.GetAuthorizationManual;

/// <summary>
/// Modelo de datos para la respuesta de autorización manual
/// </summary>
public class ResponseAuthorizationManualDto
{
    /// <summary>
    /// Código de respuesta de la transacción (00 = aprobada)
    /// </summary>
    [JsonProperty("responseCode")]
    public string ResponseCode { get; set; } = string.Empty;

    /// <summary>
    /// Código de autorización si fue aprobada
    /// </summary>
    [JsonProperty("authorizationCode")]
    public string AuthorizationCode { get; set; } = string.Empty;

    /// <summary>
    /// Identificador único de la transacción
    /// </summary>
    [JsonProperty("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje de respuesta legible
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Marca de tiempo del procesamiento de la transacción
    /// </summary>
    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; } 
}

