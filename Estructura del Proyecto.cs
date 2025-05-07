public bool MarqCheck
{
    get => Marquesina == "SI";
    set => Marquesina = value ? "SI" : "NO";
}

public bool RstCheck
{
    get => RstBranch == "SI";
    set => RstBranch = value ? "SI" : "NO";
}
