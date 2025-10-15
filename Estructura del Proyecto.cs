private static string GuidToStringSafe(Guid? id)
    => (id.HasValue && id.Value != Guid.Empty) ? id.Value.ToString() : string.Empty;
