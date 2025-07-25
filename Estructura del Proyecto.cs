/// <summary>
/// Crea una nueva instancia de <see cref="CaseWhenBuilder"/> y agrega un <c>WHEN</c> inicial basado en una expresión lambda tipada.
/// </summary>
/// <typeparam name="T">Tipo de entidad sobre la cual se basa la condición.</typeparam>
/// <param name="condition">Expresión booleana que representa la condición para el <c>WHEN</c> (por ejemplo: <c>x => x.TIPO == "A"</c>).</param>
/// <returns>Instancia de <see cref="CaseWhenBuilder"/> con el <c>WHEN</c> inicial agregado.</returns>
public static CaseWhenBuilder Start<T>(Expression<Func<T, bool>> condition)
{
    return new CaseWhenBuilder().When(condition);
}
