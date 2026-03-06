using Scraps.Configs;
using Scraps.Databases;
using System;

namespace Scraps.Tests
{
    internal static class DbTestEnvironment
    {
        private static readonly Lazy<(bool isAvailable, string reason)> State =
            new Lazy<(bool isAvailable, string reason)>(Detect);

        public static bool IsAvailable => State.Value.isAvailable;
        public static string Reason => State.Value.reason;

        private static (bool, string) Detect()
        {
            try
            {
                var dbName = string.IsNullOrWhiteSpace(ScrapsConfig.DatabaseName)
                    ? "master"
                    : ScrapsConfig.DatabaseName;

                if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                    ScrapsConfig.ConnectionString = MSSQL.ConnectionStringBuilder(dbName);

                if (string.IsNullOrWhiteSpace(ScrapsConfig.ConnectionString))
                    return (false, "SQL Server connection string is not available.");

                if (!MSSQL.CheckConnection())
                    return (false, "SQL Server is not reachable.");

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, "SQL Server is not available: " + ex.Message);
            }
        }
    }
}
