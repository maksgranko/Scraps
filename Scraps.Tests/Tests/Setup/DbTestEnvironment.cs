using Scraps.Tests.Setup;
using System;

namespace Scraps.Tests.Setup
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
                var setup = TestDatabaseSetupFactory.Create();
                if (!setup.IsAvailable)
                    return (false, $"{setup.ProviderName} is not available.");
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, "Database is not available: " + ex.Message);
            }
        }
    }
}
