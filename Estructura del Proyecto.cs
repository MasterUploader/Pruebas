GuidToStringSafe(resp.Data.Id)


    private static string GuidToStringSafe(Guid id)
    => id == Guid.Empty ? string.Empty : id.ToString();

private static string GuidToStringSafe(Guid? id)
    => id?.ToString() ?? string.Empty;
