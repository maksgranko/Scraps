using Scraps.Configs;
using System;
using System.Data;

namespace Scraps.Database.Local
{
    /// <summary>
    /// Подключение к файловому хранилищу JSON.
    /// SQL-операции не поддерживаются (NotSupportedException).
    /// </summary>
    public class LocalDatabaseConnection : IDatabaseConnection
    {
        public string ConnectionString => ScrapsConfig.LocalDataPath;

        public void ExecuteNonQuery(string sql, params object[] parameters)
        {
            throw new NotSupportedException("ExecuteNonQuery не поддерживается в LocalDatabase.");
        }

        public object ExecuteScalar(string sql, params object[] parameters)
        {
            throw new NotSupportedException("ExecuteScalar не поддерживается в LocalDatabase.");
        }

        public DataTable GetDataTable(string sql, params object[] parameters)
        {
            throw new NotSupportedException("GetDataTable с SQL не поддерживается в LocalDatabase.");
        }

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
