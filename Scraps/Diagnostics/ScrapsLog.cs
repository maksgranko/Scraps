using System;

namespace Scraps.Diagnostics
{
    /// <summary>
    /// Простой логгер для отладки.
    /// </summary>
    public static class ScrapsLog
    {
        /// <summary>
        /// Включить/выключить логирование.
        /// </summary>
        public static bool Enabled { get; set; } = false;

        /// <summary>
        /// Делегат вывода логов (например Console.WriteLine или свой логгер).
        /// </summary>
        public static Action<string> Sink { get; set; }

        /// <summary>
        /// Логировать сообщение.
        /// </summary>
        public static void Log(string message)
        {
            if (!Enabled) return;
            Sink?.Invoke(message);
        }

        /// <summary>
        /// Логировать сообщение с исключением.
        /// </summary>
        public static void Log(string message, Exception ex)
        {
            if (!Enabled) return;
            Sink?.Invoke($"{message} | {ex}");
        }
    }
}




