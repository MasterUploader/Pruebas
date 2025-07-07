// Estatus de proceso basado en opcode
string statusProceso = response.OpCode == "1308" ? "RECIBIDA" : "RECH-DENEG";
param.AddOleDbParameter(command, "ISTSPRO", OleDbType.Char, statusProceso);

// Código de error (opcode)
param.AddOleDbParameter(command, "IERR", OleDbType.Char, response.OpCode ?? " ");

// Mensaje de error (processMsg)
param.AddOleDbParameter(command, "IERRDSC", OleDbType.Char, response.ProcessMsg ?? " ");
