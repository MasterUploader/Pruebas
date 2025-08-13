
Me parece bien solo que cuando sea 

var q = Query.From<IniciarSesionDto>("U")
.Select(u=> u.email)


No se use u=> u.email, sino que infiera de aca <IniciarSesionDto>("U"), que sera u.email?

Es posible solo dimelo no cambien nada aun.
