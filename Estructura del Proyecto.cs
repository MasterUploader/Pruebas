Al mover a esta posición app.UseMiddleware<LoggingMiddleware>();

var app = builder.Build();

app.UseMiddleware<LoggingMiddleware>();

Me genera este archivo de Log que antes no generaba, lo que indica que rompe el flujo actual del programa.
Con nombre d7155f02-979b-46b9-8829-13f7f0c7fa56_swagger.json_20250807_163608.txt
---------------------------Inicio de Log-------------------------
Inicio: 2025-08-07 10:36:38
-------------------------------------------------------------------

---------------------------Enviroment Info-------------------------
Inicio: 2025-08-07 10:36:38
-------------------------------------------------------------------
Application: MS_BAN_38_UTH_RECAUDACION_PAGOS
Environment: Development
ContentRoot: C:\Git\MS_BAN_38_UTH_RecaudacionPagos\MS_BAN_38_UTH_RecaudacionPagos\MS_BAN_38_UTH_RECAUDACION_PAGOS
Execution ID: 0HNELQ2A6JC39:00000005
Client IP: ::1
User Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36
Machine Name: HNCSTG015243WAP
OS: Microsoft Windows NT 10.0.20348.0
Host: localhost:7266
Distribución: N/A
  -- Extras del HttpContext --
    Scheme              : https
    Protocol            : HTTP/2
    Method              : GET
    Path                : /swagger/v1/swagger.json
    Query               : 
    ContentType         : N/A
    ContentLength       : N/A
    ClientPort          : 59025
    LocalIp             : ::1
    LocalPort           : 7266
    ConnectionId        : 0HNELQ2A6JC39
    Referer             : https://localhost:7266/swagger/index.html
----------------------------------------------------------------------
---------------------------Enviroment Info-------------------------
Fin: 2025-08-07 10:36:38
-------------------------------------------------------------------

-------------------------------------------------------------------------------
Controlador: Desconocido
Action: Desconocido
Inicio: 2025-08-07 10:36:38
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------

----------------------------------Request Info---------------------------------
Inicio: 2025-08-07 10:36:38
-------------------------------------------------------------------------------
Método: GET
URL: /swagger/v1/swagger.json
Cuerpo:
  (Sin cuerpo en la solicitud)
----------------------------------Request Info---------------------------------
Fin: 2025-08-07 10:36:38
-------------------------------------------------------------------------------

----------------------------------Response Info---------------------------------
Inicio: 2025-08-07 10:36:38
-------------------------------------------------------------------------------
Código Estado: 200
Cuerpo:

                              {
                                "openapi": "3.0.4",
                                "info": {
                                  "title": "MS_BAN_38_UTH_RECAUDACION_PAGOS",
                                  "description": "API para gestión de pagos mediante Ginih.",
                                  "contact": {
                                    "name": "Ginih",
                                    "url": "https://api.com",
                                    "email": "soporte@api.com"
                                  },
                                  "license": {
                                    "name": "License",
                                    "url": "https://api.com"
                                  },
                                  "version": "v1"
                                },
                                "paths": {
                                  "/Companies/GetCompanies": {
                                    "post": {
                                      "tags": [
                                        "Companies"
                                      ],
                                      "summary": "Acciones relacionadas con las empresas habilitados para realizar acciones API.\r\nBAN_MS_38_M_3_UTH_RECAUDACION_PAGOS_CONSULTA_CUENTAS",
                                      "requestBody": {
                                        "description": "Objeto de Tipo Dto",
                                        "content": {
                                          "application/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetCompaniesDto"
                                            }
                                          },
                                          "text/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetCompaniesDto"
                                            }
                                          },
                                          "application/*+json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetCompaniesDto"
                                            }
                                          }
                                        }
                                      },
                                      "responses": {
                                        "200": {
                                          "description": "OK"
                                        }
                                      }
                                    }
                                  },
                                  "/Login/login": {
                                    "post": {
                                      "tags": [
                                        "Login"
                                      ],
                                      "summary": "Acciones relacionadas con los usuarios habilitados para realizar acciones API y en el portal\r\nImportante saber: Las operaciones realizadas por el API son realizadas con un JWT que tiene una duración de 5 minutos.\r\nLa empresa proporcionará acceso al portal para tener el usuario y contraseña que le permitirá obtener un refresh-token que debe ser usado para obtener los tokens de corta duración.",
                                      "responses": {
                                        "200": {
                                          "description": "OK"
                                        }
                                      }
                                    }
                                  },
                                  "/Payments/GetPaymentsID": {
                                    "post": {
                                      "tags": [
                                        "Payments"
                                      ],
                                      "summary": "Controlador que permite colectar dinero de cliente para la empresa. Permite pagar a entidades, por ejemplo, cuentas por cobrar/pagar o pagos de monto libre asignados a un cliente.\r\nEste utiliza el Id para realizar el pago.\r\nBAN_MS_38_M_5_UTH_RECAUDACION_PAGOS_OBTIENE_PAGO_POR_ID",
                                      "requestBody": {
                                        "description": "",
                                        "content": {
                                          "application/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetPaymentsIDDto"
                                            }
                                          },
                                          "text/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetPaymentsIDDto"
                                            }
                                          },
                                          "application/*+json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetPaymentsIDDto"
                                            }
                                          }
                                        }
                                      },
                                      "responses": {
                                        "200": {
                                          "description": "OK"
                                        }
                                      }
                                    }
                                  },
                                  "/Payments/GetPayments": {
                                    "post": {
                                      "tags": [
                                        "Payments"
                                      ],
                                      "summary": "Es el objeto que permite colectar dinero de cliente para la empresa. Permite pagar a entidades, por ejemplo, cuentas por cobrar/pagar o pagos de monto libre asignados a un cliente.\r\nBAN_MS_38_M_6_UTH_RECAUDACION_PAGOS_OBTIENE_PAGOS",
                                      "requestBody": {
                                        "description": "",
                                        "content": {
                                          "application/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetPaymentsDto"
                                            }
                                          },
                                          "text/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetPaymentsDto"
                                            }
                                          },
                                          "application/*+json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetPaymentsDto"
                                            }
                                          }
                                        }
                                      },
                                      "responses": {
                                        "200": {
                                          "description": "OK"
                                        }
                                      }
                                    }
                                  },
                                  "/Payments/PostPayments": {
                                    "post": {
                                      "tags": [
                                        "Payments"
                                      ],
                                      "summary": "Es el controlador que permite colectar dinero de cliente para la empresa. Permite pagar a entidades, por ejemplo, cuentas por cobrar/pagar o pagos de monto libre asignados a un cliente.\r\nLlama al programa de Pago.\r\nBAN_MS_38_M_4_UTH_RECAUDACION_PAGOS_REALIZA_PAGO",
                                      "requestBody": {
                                        "description": "",
                                        "content": {
                                          "application/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/PostPaymentDto"
                                            }
                                          },
                                          "text/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/PostPaymentDto"
                                            }
                                          },
                                          "application/*+json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/PostPaymentDto"
                                            }
                                          }
                                        }
                                      },
                                      "responses": {
                                        "200": {
                                          "description": "OK"
                                        }
                                      }
                                    }
                                  },
                                  "/Receivables/GetReceivablesID": {
                                    "post": {
                                      "tags": [
                                        "Receivables"
                                      ],
                                      "summary": "El objeto receivable (Cuentas por cobrar) permite crear, actualizar, obtener y eliminar objetos receivable o cuentas por cobrar para luego ser pagadas.\r\nBAN_MS_38_M_2_UTH_RECAUDACION_PAGOS_CONSULTA_CUENTAS_POR_ID",
                                      "requestBody": {
                                        "description": "",
                                        "content": {
                                          "application/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetReceivablesIdDto"
                                            }
                                          },
                                          "text/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetReceivablesIdDto"
                                            }
                                          },
                                          "application/*+json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetReceivablesIdDto"
                                            }
                                          }
                                        }
                                      },
                                      "responses": {
                                        "200": {
                                          "description": "OK"
                                        }
                                      }
                                    }
                                  },
                                  "/Receivables/GetReceivables": {
                                    "post": {
                                      "tags": [
                                        "Receivables"
                                      ],
                                      "summary": "El objeto receivable (Cuentas por cobrar) permite crear, actualizar, obtener y eliminar objetos receivable o cuentas por cobrar para luego ser pagadas.\r\nBAN_MS_38_M_3_UTH_RECAUDACION_PAGOS_CONSULTA_CUENTAS",
                                      "requestBody": {
                                        "description": "",
                                        "content": {
                                          "application/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetReceivablesDto"
                                            }
                                          },
                                          "text/json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetReceivablesDto"
                                            }
                                          },
                                          "application/*+json": {
                                            "schema": {
                                              "$ref": "#/components/schemas/GetReceivablesDto"
                                            }
                                          }
                                        }
                                      },
                                      "responses": {
                                        "200": {
                                          "description": "OK"
                                        }
                                      }
                                    }
                                  }
                                },
                                "components": {
                                  "schemas": {
                                    "CamposObligatoriosModel": {
                                      "type": "object",
                                      "properties": {
                                        "guid": {
                                          "type": "string",
                                          "description": "Código unico de la petición.",
                                          "nullable": true
                                        },
                                        "fecha": {
                                          "type": "string",
                                          "description": "Fecha de Ejecución de la petición.",
                                          "nullable": true
                                        },
                                        "hora": {
                                          "type": "string",
                                          "description": "Hora de Ejecución de la petición.",
                                          "nullable": true
                                        },
                                        "cajero": {
                                          "type": "string",
                                          "description": "Cajero que ejecuta la petición.",
                                          "nullable": true
                                        },
                                        "banco": {
                                          "type": "string",
                                          "description": "Id del Banco.",
                                          "nullable": true
                                        },
                                        "usuario": {
                                          "type": "string",
                                          "description": "Usuario que ejecuta la petición.",
                                          "nullable": true
                                        },
                                        "sucursal": {
                                          "type": "string",
                                          "description": "Sucursal desde donde se ejecuta la petición.",
                                          "nullable": true
                                        },
                                        "terminal": {
                                          "type": "string",
                                          "description": "Terminal que ejecuta la petición.",
                                          "nullable": true
                                        }
                                      },
                                      "additionalProperties": false,
                                      "description": "Clase DTO CamposObligatoriosModel."
                                    },
                                    "GetCompaniesDto": {
                                      "type": "object",
                                      "properties": {
                                        "camposObligatoriosModel": {
                                          "$ref": "#/components/schemas/CamposObligatoriosModel"
                                        },
                                        "limit": {
                                          "type": "string",
                                          "description": "Limite de datos de consulta a devolver.",
                                          "nullable": true
                                        },
                                        "nextToken": {
                                          "type": "string",
                                          "description": "Token que representa el conjunto de datos siguientes de la busca, va relacionado con el limite.",
                                          "nullable": true
                                        }
                                      },
                                      "additionalProperties": false,
                                      "description": "Clase DTO GetCompaniesDto."
                                    },
                                    "GetPaymentsDto": {
                                      "required": [
                                        "reference"
                                      ],
                                      "type": "object",
                                      "properties": {
                                        "CamposObligatoriosModel": {
                                          "$ref": "#/components/schemas/CamposObligatoriosModel"
                                        },
                                        "reference": {
                                          "minLength": 1,
                                          "type": "string",
                                          "description": "Referencia de pago."
                                        }
                                      },
                                      "additionalProperties": false,
                                      "description": "Clase DTO GetPaymentsDto."
                                    },
                                    "GetPaymentsIDDto": {
                                      "required": [
                                        "id"
                                      ],
                                      "type": "object",
                                      "properties": {
                                        "CamposObligatoriosModel": {
                                          "$ref": "#/components/schemas/CamposObligatoriosModel"
                                        },
                                        "id": {
                                          "minLength": 1,
                                          "type": "string",
                                          "description": "Id de pago."
                                        }
                                      },
                                      "additionalProperties": false,
                                      "description": "Clase DTO GetPaymentsIDDto."
                                    },
                                    "GetReceivablesDto": {
                                      "required": [
                                        "companyId",
                                        "customerId"
                                      ],
                                      "type": "object",
                                      "properties": {
                                        "CamposObligatoriosModel": {
                                          "$ref": "#/components/schemas/CamposObligatoriosModel"
                                        },
                                        "companyId": {
                                          "minLength": 1,
                                          "type": "string",
                                          "description": "Id de la compañia"
                                        },
                                        "customerId": {
                                          "minLength": 1,
                                          "type": "string",
                                          "description": "Id del cliente."
                                        },
                                        "limit": {
                                          "type": "string",
                                          "description": "Limite de petición.",
                                          "nullable": true
                                        },
                                        "nextToken": {
                                          "type": "string",
                                          "description": "Token del siguiente elemento de la lista filtrada.",
                                          "nullable": true
                                        }
                                      },
                                      "additionalProperties": false,
                                      "description": "Clase DTO GetReceivablesDto."
                                    },
                                    "GetReceivablesIdDto": {
                                      "required": [
                                        "id"
                                      ],
                                      "type": "object",
                                      "properties": {
                                        "CamposObligatoriosModel": {
                                          "$ref": "#/components/schemas/CamposObligatoriosModel"
                                        },
                                        "id": {
                                          "minLength": 1,
                                          "type": "string",
                                          "description": "Id de consulta."
                                        }
                                      },
                                      "additionalProperties": false,
                                      "description": "Clase DTO GetReceivablesIdDto."
                                    },
                                    "PostPaymentDto": {
                                      "type": "object",
                                      "properties": {
                                        "CamposObligatoriosModel": {
                                          "$ref": "#/components/schemas/CamposObligatoriosModel"
                                        }
                                      },
                                      "additionalProperties": false,
                                      "description": "Clase DTO PostPaymentDto."
                                    }
                                  }
                                }
                              }
----------------------------------Response Info---------------------------------
Fin: 2025-08-07 10:36:38
-------------------------------------------------------------------------------
-------------------------------------------------------------------------------
Controlador: Desconocido
Action: Desconocido
Fin: 2025-08-07 10:36:38
-------------------------------------------------------------------------------


[Tiempo Total de Ejecución]: 107 ms
---------------------------Fin de Log-------------------------
Final: 2025-08-07 10:36:38
-------------------------------------------------------------------

  Ademas del log de Companies que es el que estoy ejecutando lo genero con el nombre así:

6a3e41a5-6b4d-42a7-b07a-e1f01cf0e7a6_GetCompanies_20250807_163652.txt

Faltando el valor extra

