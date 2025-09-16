namespace Adquirencia.Models.Db2;

/// <summary>
/// Representa el maestro de comercios <c>BCAH96DTA.ADQ02COM</c>.
/// Centraliza metadatos de identificación, ubicación, naturaleza contable
/// y parámetros de cálculo (intercambio, tasa de descuento, IVA).
/// </summary>
public class ADQ02COM()
{
    /// <summary>ARCHIVO (CHAR 17). Identificador lógico del archivo de origen.</summary>
    public string A02FILE { get; set; } = string.Empty;

    /// <summary>NO. SECUENCIA (NUM 7). Secuencia del registro en el archivo.</summary>
    public int A02SECU { get; set; }

    /// <summary>TIPO DE REGISTRO (CHAR 2). Clasifica el tipo de fila en la interfaz.</summary>
    public string A02TIPR { get; set; } = string.Empty;

    /// <summary>FECHA DATOS (CHAR 8). Usado para auditoría y vigencia de datos (formato texto).</summary>
    public string A02FEDA { get; set; } = string.Empty;

    /// <summary>FIID ENTIDAD (CHAR 4). Identificador de la entidad/fiid adquirente o emisora.</summary>
    public string A02FIID { get; set; } = string.Empty;

    /// <summary>No ENTI. OTORGA PROSA (NUM 5). Código interno de entidad otorgante.</summary>
    public int A02ENPR { get; set; }

    /// <summary>
    /// CLAVE DEL COMERCIO (NUM 15). Identificador del comercio.
    /// </summary>
    /// <remarks>Se usa <see cref="long"/> para soportar hasta 15 dígitos sin pérdida.</remarks>
    public long A02COME { get; set; }

    /// <summary>IND. MULTICORTE (CHAR 1). Indicador funcional de cortes múltiples en liquidaciones.</summary>
    public string A02CORT { get; set; } = string.Empty;

    /// <summary>ESPACIOS 1 (CHAR 16). Relleno reservado para expansiones/compatibilidad.</summary>
    public string A02FIL1 { get; set; } = string.Empty;

    /// <summary>NOMBRE COMERCIO (CHAR 30). Denominación comercial registrada.</summary>
    public string A02NACO { get; set; } = string.Empty;

    /// <summary>FAMILIA DEL COMERCIO (NUM 4). Agrupador para reglas y reportes.</summary>
    public int A02CATC { get; set; }

    /// <summary>DIRECCION DEL COMERCIO (CHAR 40). Domicilio principal declarado.</summary>
    public string A02DIRC { get; set; } = string.Empty;

    /// <summary>POBLACION DEL COMERCIO (CHAR 13). Ciudad o población.</summary>
    public string A02POBC { get; set; } = string.Empty;

    /// <summary>CODIGO POSTAL (CHAR 10). Código postal del domicilio.</summary>
    public string A02CODP { get; set; } = string.Empty;

    /// <summary>PAIS ORIGEN TX (CHAR 3). País ISO/Nacional para origen de transacción.</summary>
    public string A02PAIO { get; set; } = string.Empty;

    /// <summary>DB FACTOR CUOTA INTERCAMBIO (CHAR 1). Indicador del lado débito para CI.</summary>
    public string A02DFCI { get; set; } = string.Empty;

    /// <summary>DB VALOR CUOTA INTERCAMBIO (DEC 4,2). Valor aplicado en débito para CI.</summary>
    public decimal A02DVCI { get; set; }

    /// <summary>CR FACTOR CUOTA INTERCAMBIO (CHAR 1). Indicador del lado crédito para CI.</summary>
    public string A02CFCI { get; set; } = string.Empty;

    /// <summary>CR VALOR CUOTA INTERCAMBIO (DEC 4,2). Valor aplicado en crédito para CI.</summary>
    public decimal A02CVCI { get; set; }

    /// <summary>DB FACTOR TASA DESCUENTO (CHAR 1). Indicador del lado débito para TD.</summary>
    public string A02DFTD { get; set; } = string.Empty;

    /// <summary>DB VALOR TASA DESCUENTO (DEC 4,2). Tasa de descuento en débito.</summary>
    public decimal A02DVTD { get; set; }

    /// <summary>CR FACTOR TASA DESCUENTO (CHAR 1). Indicador del lado crédito para TD.</summary>
    public string A02CFTD { get; set; } = string.Empty;

    /// <summary>CR VALOR TASA DESCUENTO (DEC 4,2). Tasa de descuento en crédito.</summary>
    public decimal A02CVTD { get; set; }

    /// <summary>FACTOR DE IVA (DEC 4,2). Factor impuesto para cálculo de IVA.</summary>
    public decimal A02FAIV { get; set; }

    /// <summary>GIRO DEL COMERCIO (NUM 4). Clasificación del giro para reglas de negocio.</summary>
    public int A02GICO { get; set; }

    /// <summary>CUENTA DEPOSITO (CHAR 20). Cuenta destino para abonos/settlement.</summary>
    public string A02CTDE { get; set; } = string.Empty;

    /// <summary>RUC CONTRIBUYENTE (CHAR 22). Identificador fiscal del comercio.</summary>
    public string A02RUCC { get; set; } = string.Empty;

    /// <summary>ESPACIOS 2 (CHAR 124). Campo de relleno para compatibilidad futura.</summary>
    public string A02FIL2 { get; set; } = string.Empty;
}
