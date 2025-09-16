namespace Adquirencia.Models.Db2;

/// <summary>
/// Maestro de terminales por comercio (BCAH96DTA.ADQ03TER).
/// Centraliza identidad de la terminal, relación con el comercio y parámetros
/// de cálculo (intercambio, tasa de descuento, IVA) aplicables al nivel terminal.
/// </summary>
/// <remarks>
/// Pensado para usarse con RestUtilities.QueryBuilder en consultas tipadas
/// (Select&lt;T&gt;, Where&lt;T&gt;). Los campos CHAR se exponen como string y los NUMERIC/DECIMAL
/// se mapean a tipos .NET apropiados (int/long/decimal) para evitar pérdida de precisión.
/// </remarks>
public class ADQ03TER()
{
    /// <summary>ARCHIVO (CHAR 17). Identificador lógico del archivo de origen.</summary>
    public string A03FILE { get; set; } = string.Empty;

    /// <summary>NO. SECUENCIA (NUM 7). Secuencia del registro.</summary>
    public int A03SECU { get; set; }

    /// <summary>TIPO DE REGISTRO (CHAR 2). Clasifica la fila en la interfaz.</summary>
    public string A03TIPR { get; set; } = string.Empty;

    /// <summary>FECHA DATOS (CHAR 8). Marca de vigencia/auditoría (texto).</summary>
    public string A03FEDA { get; set; } = string.Empty;

    /// <summary>FIID ENTIDAD (CHAR 4). Identificador de entidad/fiid.</summary>
    public string A03FIID { get; set; } = string.Empty;

    /// <summary>No ENTI. OTORGA PROSA (NUM 5). Código interno de entidad otorgante.</summary>
    public int A03ENPR { get; set; }

    /// <summary>CLAVE DEL COMERCIO (NUM 15). Relación con el maestro de comercios.</summary>
    public long A03COME { get; set; }

    /// <summary>IND. MULTICORTE (CHAR 1). Comportamiento de cortes en liquidaciones.</summary>
    public string A03CORT { get; set; } = string.Empty;

    /// <summary>IDENTIFI. TERMINAL (CHAR 16). Identificador único de la terminal POS.</summary>
    public string A03TERM { get; set; } = string.Empty;

    /// <summary>NOMBRE COMERCIO (CHAR 30). Denominación asociada a la terminal.</summary>
    public string A03NACO { get; set; } = string.Empty;

    /// <summary>FAMILIA DEL COMERCIO (NUM 4). Agrupador funcional/reporting.</summary>
    public int A03CATC { get; set; }

    /// <summary>DIRECCION DEL COMERCIO (CHAR 40). Domicilio relacionado a la terminal.</summary>
    public string A03DIRC { get; set; } = string.Empty;

    /// <summary>POBLACION DEL COMERCIO (CHAR 13). Ciudad/población.</summary>
    public string A03POBC { get; set; } = string.Empty;

    /// <summary>CODIGO POSTAL (CHAR 10).</summary>
    public string A03CODP { get; set; } = string.Empty;

    /// <summary>PAIS ORIGEN TX (CHAR 3). País de origen de la transacción.</summary>
    public string A03PAIO { get; set; } = string.Empty;

    /// <summary>DB FACTOR CUOTA INTERCAMBIO (CHAR 1). Indicador lado débito.</summary>
    public string A03DFCI { get; set; } = string.Empty;

    /// <summary>DB VALOR CUOTA INTERCAMBIO (DEC 4,2). Monto/tasa aplicada en débito.</summary>
    public decimal A03DVCI { get; set; }

    /// <summary>CR FACTOR CUOTA INTERCAMBIO (CHAR 1). Indicador lado crédito.</summary>
    public string A03CFCI { get; set; } = string.Empty;

    /// <summary>CR VALOR CUOTA INTERCAMBIO (DEC 4,2). Monto/tasa aplicada en crédito.</summary>
    public decimal A03CVCI { get; set; }

    /// <summary>DB FACTOR TASA DESCUENTO (CHAR 1). Indicador lado débito.</summary>
    public string A03DFTD { get; set; } = string.Empty;

    /// <summary>DB VALOR TASA DESCUENTO (DEC 4,2).</summary>
    public decimal A03DVTD { get; set; }

    /// <summary>CR FACTOR TASA DESCUENTO (CHAR 1). Indicador lado crédito.</summary>
    public string A03CFTD { get; set; } = string.Empty;

    /// <summary>CR VALOR TASA DESCUENTO (DEC 4,2).</summary>
    public decimal A03CVTD { get; set; }

    /// <summary>FACTOR DE IVA (DEC 4,2). Factor/porcentaje de IVA a nivel terminal.</summary>
    public decimal A03FAIV { get; set; }

    /// <summary>GIRO DEL COMERCIO (NUM 4). Clasificación económica.</summary>
    public int A03GICO { get; set; }

    /// <summary>ESPACIOS 1 (CHAR 166). Relleno para compatibilidad/expansión.</summary>
    public string A03FIL1 { get; set; } = string.Empty;
}
