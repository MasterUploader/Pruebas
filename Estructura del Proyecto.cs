Ademas deje el if así, no se si este bien?

if (context.Items.TryGetValue("LogFileName", out var existingObj) && existingObj is string existingPath && context.Items.TryGetValue("LogCustomPart", out var partObj) && partObj is string part && !string.IsNullOrWhiteSpace(part) && !existingPath.Contains($"{part}", StringComparison.OrdinalIgnoreCase))
{
    context.Items.Remove("LogFileName");
}
