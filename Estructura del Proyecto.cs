var rowsObj = command.ExecuteNonQuery();
if (rowsObj is not int rows)
{
    throw new InvalidOperationException("ExecuteNonQuery no devolvió un valor entero válido.");
}

return rows > 0;
