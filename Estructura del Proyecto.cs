sb.Append($" FETCH FIRST {_limit.Value} ROWS ONLY");

private string GetLimitClause()
{
    return _limit.HasValue ? $" FETCH FIRST {_limit.Value} ROWS ONLY" : string.Empty;
}

sb.Append(GetLimitClause());
