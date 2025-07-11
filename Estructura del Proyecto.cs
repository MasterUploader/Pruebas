var content = JsonSerializer.Serialize(postPaymentDtoFinal, new JsonSerializerOptions
{
    PropertyNamingPolicy = null, // Respeta exactamente los nombres definidos
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
});
