---------------------------Inicio de Log-------------------------
Inicio: 2025-06-30 17:17:41
-------------------------------------------------------------------

---------------------------Enviroment Info-------------------------
Inicio: 2025-06-30 17:17:41
-------------------------------------------------------------------
Application: MS_BAN_38_UTH_RECAUDACION_PAGOS
Environment: Development
ContentRoot: C:\Git\MS_BAN_38_UTH_RecaudacionPagos\MS_BAN_38_UTH_RecaudacionPagos\MS_BAN_38_UTH_RECAUDACION_PAGOS
Execution ID: 0HNDO5GVHTG0C:00000007
Client IP: ::1
User Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36
Machine Name: HNCSTG015243WAP
OS: Microsoft Windows NT 10.0.20348.0
Host: localhost:7266
Distribución: N/A
  -- Extras del HttpContext --
    Scheme              : https
    Protocol            : HTTP/2
    Method              : POST
    Path                : /Companies/GetCompanies
    Query               : 
    ContentType         : application/json-patch+json
    ContentLength       : 48
    ClientPort          : 59662
    LocalIp             : ::1
    LocalPort           : 7266
    ConnectionId        : 0HNDO5GVHTG0C
    Referer             : https://localhost:7266/swagger/index.html
----------------------------------------------------------------------
---------------------------Enviroment Info-------------------------
Fin: 2025-06-30 17:17:41
-------------------------------------------------------------------

-------------------------------------------------------------------------------
Controlador: Companies
Action: GetPayments
Inicio: 2025-06-30 17:17:41
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------

----------------------------------Request Info---------------------------------
Inicio: 2025-06-30 17:17:41
-------------------------------------------------------------------------------
Método: POST
URL: /Companies/GetCompanies
Cuerpo:

                              {
                                "limit": "string",
                                "nextToken": "string"
                              }
----------------------------------Request Info---------------------------------
Fin: 2025-06-30 17:17:41
-------------------------------------------------------------------------------

============= INICIO HTTP CLIENT =============
TraceId       : 0HNDO5GVHTG0C:00000007
Método        : GET
URL           : https://collection-api.ginih-sandbox.com/v2/companies?limit=string&NextToken=string
Código Status : 400
Duración (ms) : 1049
Encabezados   :
Authorization: eyJraWQiOiJmK3pHeDBkQnFrS1BJVFoxK25FQ0haS3FhaDRSdWZOejBMV25WMDJrTFlBPSIsImFsZyI6IlJTMjU2In0.eyJzdWIiOiJlMzNhZGFhNy02NzViLTQ1MzAtYWJhMi0wYTExMjQ2NWYzMGYiLCJjb2duaXRvOmdyb3VwcyI6WyJBcGkiXSwiZW1haWxfdmVyaWZpZWQiOnRydWUsImFkZHJlc3MiOnsiZm9ybWF0dGVkIjoiU1BTIn0sImNvZ25pdG86cHJlZmVycmVkX3JvbGUiOiJhcm46YXdzOmlhbTo6NDc1OTU4MzA3NjI1OnJvbGVcL3VzLWVhc3QtMV9ZN1pDQ2E4dkEtQXBpR3JvdXBSb2xlIiwiaXNzIjoiaHR0cHM6XC9cL2NvZ25pdG8taWRwLnVzLWVhc3QtMS5hbWF6b25hd3MuY29tXC91cy1lYXN0LTFfWTdaQ0NhOHZBIiwicGhvbmVfbnVtYmVyX3ZlcmlmaWVkIjp0cnVlLCJjb2duaXRvOnVzZXJuYW1lIjoiZGF2aXZpZW5kYXFhIiwib3JpZ2luX2p0aSI6ImVhOTYzOTU4LTY0MWYtNDExMy1hNDcxLTU4MWZlNWMwODY1NSIsImNvZ25pdG86cm9sZXMiOlsiYXJuOmF3czppYW06OjQ3NTk1ODMwNzYyNTpyb2xlXC91cy1lYXN0LTFfWTdaQ0NhOHZBLUFwaUdyb3VwUm9sZSJdLCJhdWQiOiIzM2NqYTZldHJxZWM5ZGV2Zm1kbjkxNmdzaiIsImV2ZW50X2lkIjoiZWFiOTVmYzUtYmI1Ny00YWMxLTgyMWYtOTBiNzY0ZDYyMjM1IiwidG9rZW5fdXNlIjoiaWQiLCJhdXRoX3RpbWUiOjE3NTEzMjM5NzIsIm5hbWUiOiJEYXZpdmllbmRhIiwicGhvbmVfbnVtYmVyIjoiKzUwNDk5OTk5OTk5IiwiZXhwIjoxNzUxMzI2MzY5LCJpYXQiOjE3NTEzMjU0NjksImZhbWlseV9uYW1lIjoiVXNlciIsImp0aSI6IjFjMzZiMTFjLTBmZDQtNDI2Yi1hMzY5LTRmMjQ0OWUwNDVlNyIsImVtYWlsIjoiY2xhdWRpby5lcmF6b0BkYXZpdmllbmRhLmNvbS5obiJ9.n6vFxWSZxNcfI4bDDnizOB70Jr2kb_vZOhk8u8Z-oOIlKlItU_61VSLa4X9RH3ILQddPA0B2sVMyRvw9sZVCgAo2krOtPbfWibN6jMxh3ju6MR8-NSrTu37oeWfqXBik8Mym1ikO2ZVn6QIz1pwMmcjMReLz3E1UtdXKo8_f0kErgdTutt7LhRJYpdaQpBvrsMLinn6wEswT84f5-uXs196DUYD5mwuMHxCXzEngl46rhgvF5992y7Vg110fYehrfn4NZoVyPOPG0WH6Ponr2SnQhhcV9LuraPeBCmGTwplMTtmeaRxP4fZ7k8dOm7F1la_pY4T45Uxre-VRNIwosg
traceparent: 00-eb9d210e75dddca56a59ed2e2d95863b-dc7dd230ff6da5fa-00

Respuesta:
{"status":"error","message":"Error when retrieve the data, see the result for more information","code":{"code":114,"name":"invalid_request"},"metadata":{"items":0,"hasMore":false,"nextToken":null},"timestamp":"2025-06-30T23:17:50.945Z"}
============= FIN HTTP CLIENT =============

----------------------------------Response Info---------------------------------
Inicio: 2025-06-30 17:18:10
-------------------------------------------------------------------------------
Código Estado: 400
Cuerpo:

                              {
                                "status": "BadRequest",
                                "message": "La consulta no devolvio valores",
                                "data": [],
                                "code": {
                                  "value": 0,
                                  "name": "invalid_request",
                                  "code": "114"
                                },
                                "timestamp": "2025-06-30T23:17:50.945+00:00",
                                "metadata": {
                                  "items": 0,
                                  "hasMore": false
                                },
                                "error": "1",
                                "mensaje": "Proceso ejecutado InSatisfactoriamente"
                              }
----------------------------------Response Info---------------------------------
Fin: 2025-06-30 17:18:10
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------
Controlador: Companies
Action: GetPayments
Fin: 2025-06-30 17:18:10
-------------------------------------------------------------------------------


[Tiempo Total de Ejecución]: 28720 ms
---------------------------Fin de Log-------------------------
Final: 2025-06-30 17:18:10
-------------------------------------------------------------------

