using Scraps.Databases;
using Scraps.Databases.Utilities;
using Scraps.Diagnostics;
using Scraps.Import;
using Scraps.Localization;
using Scraps.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using Xunit;

namespace Scraps.Tests
{
    public class CoverageUtilityTests
    {
        [Fact]
        public void DatabaseGenerationOptions_Factories_Work()
        {
            var def = DatabaseGenerationOptions.Default();
            Assert.NotNull(def);
            Assert.True(def.ApplyUsersMappingToScrapsConfig);

            var forDb = DatabaseGenerationOptions.ForDatabase("Db1", DatabaseGenerationMode.Standard);
            Assert.Equal("Db1", forDb.DatabaseName);
            Assert.Equal(DatabaseGenerationMode.Standard, forDb.Mode);

            Assert.Equal(DatabaseGenerationMode.None, DatabaseGenerationOptions.None("Db2").Mode);
            Assert.Equal(DatabaseGenerationMode.Simple, DatabaseGenerationOptions.Simple("Db2").Mode);
            Assert.Equal(DatabaseGenerationMode.Standard, DatabaseGenerationOptions.Standard("Db2").Mode);
            Assert.Equal(DatabaseGenerationMode.Full, DatabaseGenerationOptions.Full("Db2").Mode);
        }

        [Fact]
        public void TranslationManager_ColumnAndListApi_Works()
        {
            TranslationManager.TableTranslations.Clear();
            TranslationManager.ColumnTranslations.Clear();

            TranslationManager.TableTranslations["Users"] = "Пользователи";
            TranslationManager.ColumnTranslations["Users"] = new Dictionary<string, string>
            {
                ["Login"] = "Логин"
            };

            Assert.Equal("Логин", TranslationManager.TranslateColumnName("Users", "Login"));
            Assert.Equal("Login", TranslationManager.UntranslateColumnName("Users", "Логин"));

            var untranslated = TranslationManager.UntranslateTableList(new[] { "Пользователи" });
            Assert.Single(untranslated);
            Assert.Equal("Users", untranslated[0]);
        }

        [Fact]
        public void ScrapsLog_AllMembers_Work()
        {
            var messages = new List<string>();
            ScrapsLog.Sink = s => messages.Add(s);

            ScrapsLog.Enabled = false;
            ScrapsLog.Log("silent");
            Assert.Empty(messages);

            ScrapsLog.Enabled = true;
            Assert.True(ScrapsLog.Enabled);
            Assert.NotNull(ScrapsLog.Sink);

            ScrapsLog.Log("msg1");
            ScrapsLog.Log("msg2", new InvalidOperationException("boom"));

            Assert.True(messages.Count >= 2);
            Assert.Contains("msg1", messages[0]);
            Assert.Contains("msg2", string.Join("\n", messages));
        }

        [Fact]
        public void VirtualTableRegistry_RegisterMany_TryGetQuery_Remove_Works()
        {
            VirtualTableRegistry.Clear();
            VirtualTableRegistry.RegisterMany(new Dictionary<string, string>
            {
                ["V1"] = "SELECT 1",
                ["V2"] = "SELECT 2"
            });

            Assert.True(VirtualTableRegistry.TryGetQuery("V1", out var q1));
            Assert.Equal("SELECT 1", q1);

            Assert.True(VirtualTableRegistry.Remove("V1"));
            Assert.False(VirtualTableRegistry.Remove(""));
            Assert.False(VirtualTableRegistry.TryGetQuery("V1", out _));
        }

        [Fact]
        public void RoleAndPermissions_UtilityApi_Works()
        {
            var tp = TablePermission.FromBooleans("T", canRead: true, canWrite: false, canDelete: true, canExport: false, canImport: true);
            Assert.Equal(PermissionFlags.Read | PermissionFlags.Delete | PermissionFlags.Import, tp.Flags);

            var role = new Role("R", ("T1", PermissionFlags.Read));
            role.WithPermission("T2", PermissionFlags.Export);
            Assert.True(role.HasPermission("T1", PermissionFlags.Read));
            Assert.True(role.HasPermission("T2", PermissionFlags.Export));

            var print = RoleManager.PrintPermissions(PermissionFlags.Read | PermissionFlags.Write);
            Assert.Contains("Read", print);
            Assert.Contains("Write", print);

            var missing = RoleManager.PrintRolePermissions("missing");
            Assert.Contains("not found", missing, StringComparison.OrdinalIgnoreCase);

            RoleManager.Initialize(new[] { new Role("X", "A", PermissionFlags.Read) });
            Assert.Contains("Role: X", RoleManager.PrintAllRoles());

            RoleManager.AddRole(new Role("Y", "B", PermissionFlags.Export));
            Assert.NotNull(RoleManager.GetRole("Y"));
        }

        [Fact]
        public void LoadCsvToDataTable_SimpleOverload_Works()
        {
            var path = Path.Combine(Path.GetTempPath(), "scraps_csv_" + Guid.NewGuid().ToString("N") + ".csv");
            try
            {
                File.WriteAllText(path, "Id;Name\n1;Ivan\n2;Petr");
                var dt = DataImportService.LoadCsvToDataTable(path, ';');
                Assert.Equal(2, dt.Rows.Count);
                Assert.Equal("Ivan", dt.Rows[0]["Name"].ToString());
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void Core_PrivateDiscoveryMethods_CanBeInvoked()
        {
            var type = typeof(MSSQL);
            var testServer = type.GetMethod("TestServer", BindingFlags.NonPublic | BindingFlags.Static);
            var testSequential = type.GetMethod("TestServersSequential", BindingFlags.NonPublic | BindingFlags.Static);
            var getSources = type.GetMethod("TryGetSqlDataSources", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(testServer);
            Assert.NotNull(testSequential);
            Assert.NotNull(getSources);

            // 127.0.0.1,1 usually fails immediately (connection refused), avoiding slow DNS lookups.
            var resultSingle = testServer.Invoke(null, new object[] { "127.0.0.1,1", "master" });
            Assert.Null(resultSingle);

            // Empty input covers fast-return branch without network calls.
            var resultSeq = testSequential.Invoke(null, new object[] { new string[0], "master" });
            Assert.Null(resultSeq);

            var sources = getSources.Invoke(null, null);
            Assert.True(sources == null || sources is DataTable);
        }
    }
}
