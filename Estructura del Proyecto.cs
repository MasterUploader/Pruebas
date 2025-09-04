case "data source":
case "server":
    if (value.Contains(":"))
    {
        var ipPort = value.Split(':');          // ip:puerto
        info.Ip = ipPort[0];                    // IP siempre disponible

        // ✔ Solo asigna puerto si el parseo fue exitoso
        if (ipPort.Length > 1 &&
            int.TryParse(ipPort[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var port))
        {
            info.Port = port;                   // Puerto válido detectado
        }
        // else: se mantiene el valor actual (por defecto 0) para evitar datos incorrectos
    }
    else
    {
        info.Ip = value;                        // Solo IP, sin puerto
    }
    break;

case "port":
    // ✔ Solo asigna si el valor es un entero válido
    if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedPort))
    {
        info.Port = parsedPort;
    }
    // else: ignora y conserva el valor actual (0 u otro previamente establecido)
    break;
