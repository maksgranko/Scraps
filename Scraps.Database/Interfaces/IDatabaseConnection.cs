using System.Data;

namespace Scraps.Database
{
    /// <summary>
    /// Интерфейс подключения к базе данных.
    /// </summary>
    public interface IDatabaseConnection
    {
        /// <summary>Строка подключения.</summary>
        string ConnectionString { get; }

        /// <summary>Выполнить SQL без возврата данных.</summary>
        void ExecuteNonQuery(string sql, params object[] parameters);

        /// <summary>Выполнить SQL и вернуть скалярное значение.</summary>
        object ExecuteScalar(string sql, params object[] parameters);

        /// <summary>Выполнить SQL и вернуть DataTable.</summary>
        DataTable GetDataTable(string sql, params object[] parameters);

        /// <summary>Проверить подключение.</summary>
        bool TestConnection();
    }
}
