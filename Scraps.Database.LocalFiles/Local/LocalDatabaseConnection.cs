using Scraps.Configs;
using Scraps.Database.LocalFiles.Sql;
using System;
using System.Data;

namespace Scraps.Database.LocalFiles
{
    /// <summary>
    /// Подключение к файловому хранилищу JSON с SQL-эмулятором.
    /// </summary>
    public class LocalDatabaseConnection : IDatabaseConnection
    {
        /// <summary>Строка подключения (путь к папке с JSON-файлами).</summary>
        public string ConnectionString => ConnectionStringBuilder();

        /// <summary>Сформировать строку подключения (путь к папке данных).</summary>
        public string ConnectionStringBuilder(string value = null)
        {
            return string.IsNullOrWhiteSpace(value)
                ? ScrapsConfig.LocalDataPath
                : value;
        }

        /// <summary>Выполнить SQL-запрос без возврата данных через SQL-эмулятор.</summary>
        public void ExecuteNonQuery(string sql, params object[] parameters)
        {
            SqlExecutor.ExecuteNonQuery(sql, parameters);
        }

        /// <summary>Выполнить SQL-запрос и вернуть первое значение первой строки.</summary>
        public object ExecuteScalar(string sql, params object[] parameters)
        {
            return SqlExecutor.ExecuteScalar(sql, parameters);
        }

        /// <summary>Выполнить SQL-запрос и вернуть результат в виде DataTable.</summary>
        public DataTable GetDataTable(string sql, params object[] parameters)
        {
            return SqlExecutor.ExecuteQuery(sql, parameters);
        }

        /// <summary>Проверить доступность хранилища (создаёт папку при необходимости).</summary>
        public bool TestConnection()
        {
            try
            {
                var path = ScrapsConfig.LocalDataPath;
                if (string.IsNullOrWhiteSpace(path))
                    return false;
                if (!System.IO.Directory.Exists(path))
                    System.IO.Directory.CreateDirectory(path);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
