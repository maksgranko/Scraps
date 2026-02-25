using Scraps.Configs;
using Scraps.Databases;
using System;
using System.Data.SqlClient;

namespace Scraps.Tests
{
    public class DatabaseFixture : IDisposable
    {
        public string DatabaseName { get; }

        public DatabaseFixture()
        {
            DatabaseName = "Scraps_Test_" + Guid.NewGuid().ToString("N");
            ScrapsConfig.DatabaseName = DatabaseName;

            if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
            {
                ScrapsConfig.ConnectionString = MSSQL.ConnectionStringBuilder(DatabaseName);
                if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                    throw new InvalidOperationException("Не удалось автоматически определить SQL Server. Задайте ScrapsConfig.ConnectionString вручную.");
            }

            MSSQL.Initialize(DatabaseName, DatabaseGenerationMode.Full);

            // Таблица с пробелом в названии
            MSSQL.ExecuteNonQuery(
                "IF OBJECT_ID(N'[Таблица 1]','U') IS NULL " +
                "CREATE TABLE [Таблица 1] ([Id] int IDENTITY(1,1) PRIMARY KEY, [Name] nvarchar(50) NOT NULL);");

            // Таблица для импорта без identity
            MSSQL.ExecuteNonQuery(
                "IF OBJECT_ID(N'[ImportTest]','U') IS NULL " +
                "CREATE TABLE [ImportTest] ([Name] nvarchar(50) NOT NULL);");

            MSSQL.ExecuteNonQuery("INSERT INTO [Таблица 1]([Name]) VALUES ('Ivan');");
        }

        public void Dispose()
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(ScrapsConfig.ConnectionString)
                {
                    InitialCatalog = "master"
                };

                using (var conn = new SqlConnection(builder.ToString()))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        $"IF DB_ID(@DbName) IS NOT NULL " +
                        $"BEGIN " +
                        $"ALTER DATABASE [" + DatabaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                        $"DROP DATABASE [" + DatabaseName + "]; " +
                        $"END", conn);
                    cmd.Parameters.AddWithValue("@DbName", DatabaseName);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // ignore cleanup errors
            }
        }
    }
}
