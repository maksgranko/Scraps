using Scraps.Configs;
using Scraps.Database;
using Scraps.Database.LocalFiles;
using Scraps.Database.LocalFiles.Sql;
using Scraps.Database.MSSQL.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Scraps.Tests.Setup
{
    public class LocalTestSetup : ITestDatabaseSetup
    {
        private string _dbPath;

        public string ProviderName => "LocalFiles";
        public bool IsAvailable => true;

        public string CreateDatabase()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "Scraps_Test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dbPath);

            ScrapsConfig.LocalDataPath = _dbPath;
            ScrapsConfig.DatabaseProvider = DatabaseProvider.LocalFiles;
            ScrapsConfig.DatabaseName = "TestDb";
            ScrapsConfig.ConnectionString = _dbPath;

            DatabaseProviderFactory.Reset();
            DatabaseProviderFactory.Register(DatabaseProvider.LocalFiles, () => new LocalDatabase());

            return _dbPath;
        }

        public void Initialize(DatabaseGenerationMode mode)
        {
            var db = new LocalDatabase();
            db.Initialize(DatabaseGenerationOptions.ForDatabase("TestDb", mode));
        }

        public void ExecuteNonQuery(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return;

            // Используем SQL-эмулятор LocalDatabase
            var conn = new LocalDatabaseConnection();
            conn.ExecuteNonQuery(sql);
        }

        public void DropDatabase(string databaseName, string connectionString)
        {
            try
            {
                if (!string.IsNullOrEmpty(_dbPath) && Directory.Exists(_dbPath))
                    Directory.Delete(_dbPath, true);
            }
            catch { }
        }

        public string BuildConnectionString(string databaseName)
        {
            return _dbPath ?? ScrapsConfig.LocalDataPath;
        }

        public IEnumerable<string> FindDatabasesByPrefix(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) yield break;
            var parent = Path.GetTempPath();
            foreach (var dir in Directory.GetDirectories(parent, prefix + "*"))
                yield return dir;
        }

        public void CleanupDatabases(IEnumerable<string> databaseNames)
        {
            foreach (var name in databaseNames ?? Enumerable.Empty<string>())
            {
                try
                {
                    if (Directory.Exists(name))
                        Directory.Delete(name, true);
                }
                catch { }
            }
        }
    }
}
