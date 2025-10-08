// ------------ DESCRIPCIONES (idénticas al RPG) ------------
string des1 = Trunc(nombreComercio, 40);                         // AL1
string des2 = Trunc($"{codigoComercio}-{terminal}", 40);         // AL2

string tipCta = infoCta.EsAhorro ? "AHO" : infoCta.EsCheques ? "CHE" : "CTE";
string base3  = $"{EtiquetaConcepto(naturalezaCliente)}-{idUnico}-{tipCta}";   // AL3 base
string al3GL  = Trunc($"{base3}-{glDec}", 40);                                  // AL3 con cuenta GL
string al3Sin = Trunc(base3, 40);                                               // AL3 sin cuenta

// … y en el return, SOLO cambia estas asignaciones:

if (naturalezaCliente == "C")
{
    // Cliente a CR → GL a DB
    return new IntLotesParamsDto
    {
        // … (todo lo que ya tenías)
        DesDB1 = des1, DesDB2 = des2, DesDB3 = al3GL,   // lado debitado = GL, lleva -CuentaGL
        DesCR1 = des1, DesCR2 = des2, DesCR3 = al3Sin,  // lado acreditado = Cliente, SIN cuenta
        // … (resto igual)
    };
}
else
{
    // Cliente a DB → GL a CR
    return new IntLotesParamsDto
    {
        // … (todo lo que ya tenías)
        DesDB1 = des1, DesDB2 = des2, DesDB3 = al3Sin,  // lado debitado = Cliente, SIN cuenta
        DesCR1 = des1, DesCR2 = des2, DesCR3 = al3GL,   // lado acreditado = GL, lleva -CuentaGL
        // … (resto igual)
    };
}
