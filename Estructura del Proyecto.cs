Quiero mejorar este forma porque se vuelve engorrosa y repetitiva
var insert3 = new InsertQueryBuilder("Customers")
    .IntoColumns("CustomerName", "ContactName", "Address", "City", "PostalCode", "Country")
    .Values(("CustomerName", "Cardinal"), ("ContactName", "Tom B. Erichsen"), ("Address", "Skagen 21"), ("City", "Stavanger"), ("PostalCode", "4006"), ("Country", "Norway"))
    .Values(("CustomerName", "Greasy Burger"), ("ContactName", "Per Olsen"), ("Address", "Gateveien 15"), ("City", "Sandnes"), ("PostalCode", "4306"), ("Country", "Norway"))
    .Values(("CustomerName", "Tasty Tee"), ("ContactName", "Finn Egan"), ("Address", "Streetroad 19B"), ("City", "Liverpool"), ("PostalCode", "L1 0AA"), ("Country", "UK"))
    .Build();
	
	
Quiero que sea algo como:
var insert3 = new InsertQueryBuilder("Customers")
    .IntoColumns("CustomerName", "ContactName", "Address", "City", "PostalCode", "Country")
	.ListValues(
	("Cardinal", "Tom B. Erichsen", "Skagen 21", "Stavanger", "4006", "Norway"),
	("Greasy Burger", "Per Olsen", "Gateveien 15", "Sandnes", "4306", "Norway")		
	)
    .Build();
	
De esta manera emplear menos codigo, si hay una forma más optima indicamelo.

