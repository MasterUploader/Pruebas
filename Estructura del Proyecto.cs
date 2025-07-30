Necesito que este metodo utilice las librerias RestUtilities.Connections y RestUtilities.QueryBuilder

        public async Task<RespuestaListarComercioDto> ListarComercioAsync(ListarComerciosDto listarComercio)
        {

            try
            {
                connection.Open();

                int totalElements = 0;
                int totalPages = 0;
                var listaDeComercios = new List<RespuestaListarComercioDto.Content>();
                decimal codigoRespuesta = 0;
                string descripcionRespuesta = "";
                if (listarComercio.Size == "")
                {
                    listarComercio.Size = "1";
                }
                else if (listarComercio.Size == "0")
                {
                    listarComercio.Size = "1";
                }
                if (listarComercio.Page == "")
                {
                    listarComercio.Page = "0";
                }

                //PRIMER CONSULTA PARA CONTAR NUMERO TOTAL DE ELEMTOS //pqr01cli
                string countQuery = "SELECT COUNT(*) FROM IS4TECHDTA.PQR01L01 WHERE 1=1";

                //SEGUNDA CONSULTA OBTENER DATOS PAGINADOS
                string dataQuery = "SELECT * FROM IS4TECHDTA.PQR01L01 WHERE 1=1";

                if (!string.IsNullOrEmpty(listarComercio.Search) && int.TryParse(listarComercio.Search, out _))
                {
                    // dataQuery += " AND CLINRO LIKE ?";
                    // countQuery += " AND CLINRO LIKE ?";

                    dataQuery += " AND (LOWER(CLINRO) LIKE LOWER(?))";
                    countQuery += " AND (LOWER(CLINRO) LIKE LOWER(?)  )";
                }
                else if (!string.IsNullOrEmpty(listarComercio.Search))
                {
                    // dataQuery += " AND CLINOM LIKE ?";
                    // countQuery += " AND CLINOM LIKE ?";

                    dataQuery += " AND (LOWER(CLINOM) LIKE LOWER(?))";
                    countQuery += " AND (LOWER(CLINOM) LIKE LOWER(?))";
                }

                if (!string.IsNullOrEmpty(listarComercio.Status))
                {
                    dataQuery += " AND CLISTS = ?";
                    countQuery += " AND CLISTS = ?";
                }

                //CONSULTA COUNT
                await using (OleDbCommand countCommand = new OleDbCommand(countQuery, connection.Connect.OleDbConnection))
                {
                    if (!string.IsNullOrEmpty(listarComercio.Search))
                    {
                        countCommand.Parameters.AddWithValue("?", '%' + listarComercio.Search + '%');
                    }

                    if (!string.IsNullOrEmpty(listarComercio.Status))
                    {
                        countCommand.Parameters.AddWithValue("?", listarComercio.Status);
                    }
                    totalElements = (int)countCommand.ExecuteScalar()!;
                }

                if (totalElements == 0)
                {
                    RespuestaListarComercioDto respuestaListarComercioDto = new()
                    {
                        ResponseCode = 1,
                        ResponseDescription = "Busqueda no obtuvo datos"
                    };
                    return respuestaListarComercioDto;
                }

                //Calculamos el total de paginas basados en el numero total de elementos y el tamaño de pagina
                totalPages = (int)Math.Ceiling((double)totalElements / Convert.ToInt32(listarComercio.Size));

                if (Convert.ToInt32(listarComercio.Size) >= totalElements)
                {
                    listarComercio.Page = "0";
                    totalPages = 1;

                }
                else if (Convert.ToInt32(listarComercio.Page) < 0 || Convert.ToInt32(listarComercio.Page) >= totalPages)
                {
                    listarComercio.Page = Convert.ToString(totalPages - 1);
                }

                int rowsToSkip = Convert.ToInt32(listarComercio.Page) * Convert.ToInt32(listarComercio.Size);
                int rowsToFetch = Convert.ToInt32(listarComercio.Size);

                if (!string.IsNullOrEmpty(listarComercio.Search) && int.TryParse(listarComercio.Search, out _))
                {
                    dataQuery += $" ORDER BY CLINRO";
                    dataQuery += $" OFFSET {rowsToSkip} ROWS FETCH FIRST {rowsToFetch} ROWS ONLY";
                }
                else if (!string.IsNullOrEmpty(listarComercio.Search))
                {
                    dataQuery += $" ORDER BY CLINOM";
                    dataQuery += $" OFFSET {rowsToSkip} ROWS FETCH FIRST {rowsToFetch} ROWS ONLY";
                }
                else
                {
                    dataQuery += $" OFFSET {rowsToSkip} ROWS FETCH FIRST {rowsToFetch} ROWS ONLY";
                }

                //Ejecutamos la segunda consulta para obtener los datos paginados
                await using (OleDbCommand dataCommand = new OleDbCommand(dataQuery, connection.Connect.OleDbConnection))
                {
                    if (!string.IsNullOrEmpty(listarComercio.Search))
                    {
                        dataCommand.Parameters.AddWithValue("?", '%' + listarComercio.Search + '%');
                    }

                    if (!string.IsNullOrEmpty(listarComercio.Status))
                    {
                        dataCommand.Parameters.AddWithValue("?", listarComercio.Status);
                    }

                    using (OleDbDataReader reader = dataCommand.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                var comercios = new RespuestaListarComercioDto.Content
                                {
                                    Name = reader.GetString(reader.GetOrdinal("CLINOM")),
                                    Cif = reader.GetString(reader.GetOrdinal("CLINRO")),
                                    Status = reader.GetString(reader.GetOrdinal("CLISTS"))
                                };
                                listaDeComercios.Add(comercios);
                            }
                            codigoRespuesta = 0;
                            descripcionRespuesta = "Comercios Listados Correctamente";
                        }
                        else
                        {
                            codigoRespuesta = 1;
                            descripcionRespuesta = "No se pudieron Listar los Comercios";
                        }
                    }
                }

                _respuestaListarComercioDto.Contents = listaDeComercios.ToArray();
                _respuestaListarComercioDto.FirstPage = Convert.ToInt32(listarComercio.Page) == 0;
                _respuestaListarComercioDto.LastPage = Convert.ToInt32(listarComercio.Page) == totalPages - 1;
                _respuestaListarComercioDto.Page = Convert.ToInt32(listarComercio.Page);
                _respuestaListarComercioDto.TotalPages = totalPages;
                _respuestaListarComercioDto.TotalElements = totalElements;
                _respuestaListarComercioDto.ResponseCode = (int)codigoRespuesta;
                _respuestaListarComercioDto.ResponseDescription = descripcionRespuesta;


                return _respuestaListarComercioDto;
            }
            catch (Exception ex)
            {
                RespuestaListarComercioDto respuestaListarComercioDto = new()
                {
                    ResponseCode = 666,
                    ResponseDescription = ex.Message
                };
                return respuestaListarComercioDto;
            }
        }
