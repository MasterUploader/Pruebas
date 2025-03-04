using System;
using System.Threading;

namespace Logging.Helpers
{
    /// <summary>
    /// Gestiona los niveles de indentación de los logs para mejorar la legibilidad.
    /// </summary>
    public static class LogScope
    {
        // Usa un `AsyncLocal<int>` para mantener la indentación en hilos independientes
        private static readonly AsyncLocal<int> _currentLevel = new AsyncLocal<int>();

        /// <summary>
        /// Obtiene el nivel de indentación actual.
        /// </summary>
        public static int CurrentLevel => _currentLevel.Value;

        /// <summary>
        /// Aumenta el nivel de indentación cuando un método es ejecutado.
        /// </summary>
        public static void IncreaseIndentation()
        {
            _currentLevel.Value += 4; // Aumenta 4 espacios por cada nivel de profundidad
        }

        /// <summary>
        /// Reduce el nivel de indentación cuando un método finaliza su ejecución.
        /// </summary>
        public static void DecreaseIndentation()
        {
            _currentLevel.Value = Math.Max(0, _currentLevel.Value - 4); // Asegura que no sea negativo
        }
    }
}
