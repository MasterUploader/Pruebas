[AttributeUsage(AttributeTargets.Property)]
public class SqlColumnDefinitionAttribute : Attribute
{
    public string ColumnName { get; }
    public SqlDataType DataType { get; }
    public int Length { get; }

    public SqlColumnDefinitionAttribute(string columnName, SqlDataType dataType, int length)
    {
        ColumnName = columnName;
        DataType = dataType;
        Length = length;
    }
}
