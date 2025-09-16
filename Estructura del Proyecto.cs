namespace Adquirencia.Models.Db2;

/// <summary>
/// Vista/tabla de comercio (IADQCOM): concentra identidad del comercio,
/// parámetros de tasas (intercambio, descuento, IVA) y configuración de liquidación.
/// </summary>
/// <remarks>
/// Este DTO está pensado para usarse con RestUtilities.QueryBuilder en
/// consultas tipadas (Select&lt;T&gt;, Where&lt;T&gt;), mapeando 1:1 los campos del PF.
/// </remarks>
public class IADQCOM()
{
    /// <summary>No. CLAVE DE COMERCIO (DEC 15,0). Identificador principal del comercio.</summary>
    public long ADQCOME { get; set; }

    /// <summary>NOMBRE DE COMERCIO (CHAR 30).</summary>
    public string ADQNACO { get; set; } = string.Empty;

    /// <summary>CATEGORIA DEL COMERCIO (DEC 4,0). Agrupa reglas de negocio y reportes.</summary>
    public int ADQCATC { get; set; }

    /// <summary>DIRECCION COMERCIO (CHAR 40).</summary>
    public string ADQDIRC { get; set; } = string.Empty;

    /// <summary>POBLACION (CHAR 13).</summary>
    public string ADQPOBC { get; set; } = string.Empty;

    /// <summary>CODIGO POSTAL (CHAR 10).</summary>
    public string ADQCODP { get; set; } = string.Empty;

    /// <summary>PAIS COMERCIO (CHAR 3). Usualmente código ISO/Nacional.</summary>
    public string ADQPAIO { get; set; } = string.Empty;

    /// <summary>DB FACTOR CUOTA INTERCAMBIO (CHAR 1). Bandera lado débito.</summary>
    public string ADQDFCI { get; set; } = string.Empty;

    /// <summary>DB VALOR CUOTA INTERCAMBIO (DEC 5,2). Monto/tasa aplicada en débito.</summary>
    public decimal ADQDVCI { get; set; }

    /// <summary>CR FACTOR CUOTA INTERCAMBIO (CHAR 1). Bandera lado crédito.</summary>
    public string ADQCFCI { get; set; } = string.Empty;

    /// <summary>CR VALOR CUOTA INTERCAMBIO (DEC 5,2). Monto/tasa aplicada en crédito.</summary>
    public decimal ADQCVCI { get; set; }

    /// <summary>DB FACTOR TASA DESCUENTO (CHAR 1). Bandera lado débito.</summary>
    public string ADQDFTD { get; set; } = string.Empty;

    /// <summary>DB VALOR TASA DESCUENTO (DEC 5,2).</summary>
    public decimal ADQDVTD { get; set; }

    /// <summary>CR FACTOR TASA DESCUENTO (CHAR 1). Bandera lado crédito.</summary>
    public string ADQCFTD { get; set; } = string.Empty;

    /// <summary>CR VALOR TASA DESCUENTO (DEC 5,2).</summary>
    public decimal ADQCVTD { get; set; }

    /// <summary>FACTOR IVA (DEC 5,2). Factor/porcentaje de IVA aplicable.</summary>
    public decimal ADQFAIV { get; set; }

    /// <summary>GIRO DEL COMERCIO (DEC 4,0). Clasificación económica para reglas.</summary>
    public int ADQGICO { get; set; }

    /// <summary>CUENTA DEPOSITO COMERCIO (CHAR 20). Cuenta de liquidación (settlement).</summary>
    public string ADQCTDE { get; set; } = string.Empty;

    /// <summary>RUC CONTRIBUYENTE (CHAR 22). Identificador fiscal.</summary>
    public string ADQRUCC { get; set; } = string.Empty;

    /// <summary>DESCRIPCION (CHAR 30). Texto libre de referencia.</summary>
    public string ADQDES1 { get; set; } = string.Empty;

    /// <summary>DESCRIPCION (CHAR 30). Texto libre de referencia.</summary>
    public string ADQDES2 { get; set; } = string.Empty;

    /// <summary>DESCRIPCION (CHAR 30). Texto libre de referencia.</summary>
    public string ADQDES3 { get; set; } = string.Empty;

    /// <summary>COMERCIO GENERADO POR PROSA (DEC 5,0). Código interno de entidad.</summary>
    public int ADQENPR { get; set; }

    /// <summary>ESTATUS DEL COMERCIO (CHAR 1). 'A' activo, otros: inactivo/bloqueado.</summary>
    public string ADQESTA { get; set; } = string.Empty;

    /// <summary>CLIF DEL CLIENTE ICBS (CHAR 10). Identificador cliente en core.</summary>
    public string ADQCIF { get; set; } = string.Empty;

    /// <summary>Monto mínimo facturación mensual (DEC 14,2).</summary>
    public decimal ADQMOMI { get; set; }

    /// <summary>Cobro por incumplimiento (DEC 14,2).</summary>
    public decimal ADQCOIM { get; set; }

    /// <summary>Costos por mantenimiento y papel (DEC 14,2).</summary>
    public decimal ADQCOMA { get; set; }

    /// <summary>Monto 1 (DEC 14,2). Campo libre para parametrización.</summary>
    public decimal ADQMTO1 { get; set; }

    /// <summary>Monto 2 (DEC 14,2). Campo libre para parametrización.</summary>
    public decimal ADQMTO2 { get; set; }

    /// <summary>Monto 3 (DEC 14,2). Campo libre para parametrización.</summary>
    public decimal ADQMTO3 { get; set; }

    /// <summary>Cantidad 1 (DEC 10,0). Auxiliar de conteos/umbrales.</summary>
    public long ADQCAN1 { get; set; }

    /// <summary>Cantidad 2 (DEC 10,0). Auxiliar de conteos/umbrales.</summary>
    public long ADQCAN2 { get; set; }

    /// <summary>Cantidad 3 (DEC 10,0). Auxiliar de conteos/umbrales.</summary>
    public long ADQCAN3 { get; set; }

    /// <summary>INDICADOR DE CORTE (CHAR 1). Comportamiento de multicorte.</summary>
    public string ADQINCO { get; set; } = string.Empty;

    /// <summary>CLIENTE DE INTERNET (DEC 10,0). Identificador de cliente digital.</summary>
    public long ADQCLI { get; set; }

    /// <summary>DB VALOR CUOTA INTERCAMBIO (DEC 5,2). Variante adicional parametrizable.</summary>
    public decimal ADQMDVC { get; set; }

    /// <summary>CR VALOR CUOTA INTERCAMBIO (DEC 5,2). Variante adicional parametrizable.</summary>
    public decimal ADQMCVC { get; set; }

    /// <summary>DB VALOR TASA DESCUENTO (DEC 5,2). Variante adicional parametrizable.</summary>
    public decimal ADQMDVT { get; set; }

    /// <summary>CR VALOR TASA DESCUENTO (DEC 5,2). Variante adicional parametrizable.</summary>
    public decimal ADQMCVT { get; set; }

    /// <summary>FACTOR IVA (DEC 5,2). Campo duplicado en layout para configuraciones alternativas.</summary>
    public decimal ADQMFAI { get; set; }
}
