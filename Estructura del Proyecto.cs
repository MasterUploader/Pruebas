using System.Text.Json;

// JSON entrante (10 chars en "Codigo")
var json = """
{
  "Codigo": "ABCDEFGHIJ",
  "Descripcion": "  Texto con espacios  "
}
""";

var dto = JsonSerializer.Deserialize<MiRespuestaDto>(json);

// Resultado:
// dto.Codigo == "ABCDEFGH"   // truncado a 8
// dto.Descripcion == "Texto con" // 12 máx; sin Trim => mantiene espacios al inicio/fin si los hubiera
