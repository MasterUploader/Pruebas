.SelectCase<USUADMIN>(
    CaseWhenBuilder<USUADMIN>.When(x => x.TIPO == "A").Then("'Administrador'")
                             .When(x => x.TIPO == "U").Then("'Usuario'")
                             .Else("'Otro'"),
    "DESCRIPCION"
)
