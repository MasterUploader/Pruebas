var paramGuid = command.CreateParameter();
paramGuid.ParameterName = "HDP00GUID";
paramGuid.OleDbType = OleDbType.Char;
paramGuid.Size = 100;
paramGuid.Value = guid.PadRight(100);
command.Parameters.Add(paramGuid);
