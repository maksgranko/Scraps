using OfficeOpenXml;
using Scraps.Configs;
using Db = Scraps.Database.Current;
using Scraps.Database.MSSQL;
using Scraps.Import;
using Scraps.Localization;
using Scraps.Security;
using Scraps.Tests.Setup;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Xunit;
using Scraps.Database;

namespace Scraps.Tests.Integration
{
    [Collection("Db")]
    public class CoverageDbTests
    {
        [DbFact]
        public void RolePermissions_FullCrud_Works()
        {
            var roleName = "rp_" + Guid.NewGuid().ToString("N");
            var table = "Таблица 1";

            var roleId = Db.Roles.Create(roleName);
            try
            {
                Db.RolePermissions.Set(roleName, table, PermissionFlags.Read | PermissionFlags.Export);

                var byName = Db.RolePermissions.GetByRoleName(roleName);
                Assert.NotEmpty(byName);
                Assert.Contains(byName, p => p.TableName == table && (p.Flags & PermissionFlags.Read) == PermissionFlags.Read);

                var byId = Db.RolePermissions.GetByRoleId(roleId);
                Assert.NotEmpty(byId);

                Db.RolePermissions.Delete(roleName, table);
                var afterDeleteByName = Db.RolePermissions.GetByRoleId(roleId);
                Assert.DoesNotContain(afterDeleteByName, p => p.TableName == table);

                Db.RolePermissions.Set(roleId, table, PermissionFlags.Read);
                Db.RolePermissions.Delete(roleId, table);
                var afterDeleteById = Db.RolePermissions.GetByRoleId(roleId);
                Assert.DoesNotContain(afterDeleteById, p => p.TableName == table);

                Db.RolePermissions.Set(roleName, table, PermissionFlags.Read | PermissionFlags.Write);
                Db.RolePermissions.DeleteAllForRole(roleName);
                Assert.Empty(Db.RolePermissions.GetByRoleId(roleId));
            }
            finally
            {
                Db.Roles.Delete(roleName);
            }
        }

        [DbFact]
        public void Roles_PermissionsApi_Works()
        {
            var roleName = "rm_" + Guid.NewGuid().ToString("N");
            try
            {
                var roleId = Db.Roles.Create(roleName);
                Db.RolePermissions.Set(roleId, "Таблица 1", PermissionFlags.Read | PermissionFlags.Write);
                Assert.True(Db.Roles.CheckAccess(roleName, "Таблица 1", PermissionFlags.Read));

                Db.RolePermissions.Delete(roleId, "Таблица 1");
                Assert.False(Db.Roles.CheckAccess(roleName, "Таблица 1", PermissionFlags.Read));
            }
            finally
            {
                if (Db.Roles.GetRoleIdByName(roleName) != null)
                    Db.Roles.Delete(roleName);
            }
        }

        [DbFact]
        public void DataApi_Overloads_And_FkJoin_Works()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // FK JOIN and ExecuteScalar are MSSQL-specific

            var dtByRole = Db.GetTableData("Users");
            Assert.NotNull(dtByRole);

            var fkFromDefaultCtor = new MSSQL.ForeignKeyJoin
            {
                BaseColumn = "Role",
                ReferenceTable = "Roles",
                ReferenceColumn = "RoleID",
                ReferenceColumns = new[] { "RoleName" },
                AliasPrefix = "Role Alias"
            };

            var viaDefaultConn = MSSQL.GetTableData("Users", new[] { fkFromDefaultCtor }, "Login", "Role");
            Assert.NotNull(viaDefaultConn);
            Assert.True(viaDefaultConn.Columns.Count >= 2);

            var withFkByRole = MSSQL.GetTableData("Users", new[] { fkFromDefaultCtor }, "Login");
            Assert.NotNull(withFkByRole);
            Assert.True(withFkByRole.Columns.Count >= 1);

            var withFk = MSSQL.GetTableData(
                tableName: "Users",
                connectionString: ScrapsConfig.ConnectionString,
                foreignKeys: new[]
                {
                    new MSSQL.ForeignKeyJoin("Role", "Roles", "RoleID", "RoleName")
                    {
                        AliasPrefix = "Role Alias"
                    }
                },
                baseColumns: new[] { "Login", "Role" });
            Assert.NotNull(withFk);
            Assert.True(withFk.Columns.Count >= 2);
            TranslationManager.Translations[TranslationManager.ColumnKey("Users", "Login")] = "Логин";
            var translated = TranslationManager.Translate(Db.GetTableData("Users"), "Users");
            Assert.True(translated.Columns.Contains("Логин"));

            var generic = MSSQL.GetNx2Dictionary("Таблица 1", "Id", "Name", o => Convert.ToInt32(o), o => o?.ToString() ?? "");
            Assert.True(generic.Count > 0);

            var usersCount = Convert.ToInt32(MSSQL.ExecuteScalar("SELECT COUNT(1) FROM [Users]"));
            Assert.True(usersCount >= 0);
        }

        [DbFact]
        public void GetTableData_ExpandForeignKeys_Works()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // expandForeignKeys is MSSQL-specific

            var expanded = MSSQL.GetTableData("Users", expandForeignKeys: true);
            Assert.NotNull(expanded);
            Assert.True(expanded.Columns.Count >= 2);

            var expandedCols = MSSQL.GetTableData("Users", expandForeignKeys: true, expandOptions: null, "Login");
            Assert.NotNull(expandedCols);
            Assert.True(expandedCols.Columns.Count >= 1);

            var noDisplay = MSSQL.GetTableData("Users", expandForeignKeys: true,
                new ExpandForeignKeysOptions { AutoResolveDisplayColumn = false, IncludeReferenceAllColumns = false },
                "Login", "Role");
            Assert.NotNull(noDisplay);
        }

        [DbFact]
        public void ResolveDisplayColumn_CaseInsensitive_And_ExcludesPk()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // LocalForeignKeyProvider does not support excludeColumn/preferred

            var display = Db.ResolveDisplayColumn("Roles");
            Assert.Equal("RoleName", display);

            ScrapsConfig.ForeignKeyDisplayColumnOverrides["Roles"] = "RoleID";
            var overridden = Db.ResolveDisplayColumn("Roles");
            Assert.Equal("RoleID", overridden);
            ScrapsConfig.ForeignKeyDisplayColumnOverrides.Remove("Roles");

            var caseInsensitive = MSSQL.ResolveDisplayColumn("Roles", excludeColumn: "RoleID", preferred: new[] { "NAME" });
            Assert.Equal("RoleName", caseInsensitive);
        }

        [DbFact]
        public void RowEditor_AddRow_WithAutoFk_CreatesLookup()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // LocalRowEditor does not support FK auto-creation

            var result = Scraps.Database.MSSQL.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Database.MSSQL.Utilities.TableRows.Values.Create("Login", "testuser_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", "ТестоваяРоль"),
                strictFk: false);

            Assert.True(result.Success, result.Error);
            Assert.NotNull(result.RowId);
        }

        [DbFact]
        public void RowEditor_AddRow_StrictFk_Missing_Throws()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // LocalRowEditor does not enforce FK checks

            var result = Scraps.Database.MSSQL.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Database.MSSQL.Utilities.TableRows.Values.Create("Login", "testuser2_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", "НесуществующаяРоль123"),
                strictFk: true);

            Assert.False(result.Success);
            Assert.Contains("не найдено", result.Error);
        }

        [DbFact]
        public void RowEditor_AddRow_TypeMismatch_Throws()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // LocalRowEditor does not enforce FK checks

            var result = Scraps.Database.MSSQL.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Database.MSSQL.Utilities.TableRows.Values.Create("Login", "testuser3_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", 99999),
                strictFk: true);

            Assert.False(result.Success);
        }

        [DbFact]
        public void RowEditor_UpdateRow_ChangesFk()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // LocalRowEditor does not support FK update logic

            var login = "updateuser_" + Guid.NewGuid().ToString("N");
            var addResult = Scraps.Database.MSSQL.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Database.MSSQL.Utilities.TableRows.Values.Create("Login", login, "Password", "pass", "Role", "default"),
                strictFk: false);
            Assert.True(addResult.Success, addResult.Error);

            var updateResult = Scraps.Database.MSSQL.Utilities.TableRows.RowEditor.UpdateRow("Users", "Login", login,
                Scraps.Database.MSSQL.Utilities.TableRows.Values.Create("Role", "НоваяРольДляUpdate"),
                strictFk: false);

            Assert.True(updateResult.Success, updateResult.Error);
        }

        [Fact]
        public void Values_Create_BuildsDictionary()
        {
            var dict = Scraps.Database.MSSQL.Utilities.TableRows.Values.Create("A", 1, "B", "two", "C", null);
            Assert.Equal(3, dict.Count);
            Assert.Equal(1, dict["A"]);
            Assert.Equal("two", dict["B"]);
            Assert.Null(dict["C"]);
        }

        [DbFact]
        public void RowEditor_AddRow_StrictFk_Existing_Succeeds()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // GenerateIfNotExists is MSSQL-specific

            MSSQL.GenerateIfNotExists();
            var result = Scraps.Database.MSSQL.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Database.MSSQL.Utilities.TableRows.Values.Create("Login", "strictok_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", "default"),
                strictFk: true);
            Assert.True(result.Success, result.Error);
            Assert.NotNull(result.RowId);
        }

        [DbFact]
        public void RowEditor_AddRow_WithExistingRole_Strict_Succeeds()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // GenerateIfNotExists is MSSQL-specific

            MSSQL.GenerateIfNotExists();
            var result = Scraps.Database.MSSQL.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Database.MSSQL.Utilities.TableRows.Values.Create("Login", "existingrole_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", "default"),
                strictFk: true);
            Assert.True(result.Success, result.Error);
            Assert.NotNull(result.RowId);
        }

        [DbFact]
        public void GenerateIfNotExists_Parameterless_Works()
        {
            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // GenerateIfNotExists is MSSQL-specific

            MSSQL.GenerateIfNotExists();
            var users = Db.GetTableData("Users");
            Assert.NotNull(users);
        }

        [DbFact]
        public void ImportService_LoadExcel_ValidateImport_ImportSafe_Works()
        {
            var file = Path.Combine(Path.GetTempPath(), "scraps_excel_" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var excel = new ExcelPackage())
                {
                    var ws = excel.Workbook.Worksheets.Add("Sheet1");
                    ws.Cells[1, 1].Value = "Name";
                    ws.Cells[2, 1].Value = "ExcelUser";
                    excel.SaveAs(new FileInfo(file));
                }

                var excelDt = DataImportService.LoadExcelToDataTable(file);
                Assert.Single(excelDt.Columns);
                Assert.Equal("ExcelUser", excelDt.Rows[0][0]?.ToString());
            }
            finally
            {
                if (File.Exists(file)) File.Delete(file);
            }

            var importDt = new DataTable();
            importDt.Columns.Add("Name");
            importDt.Rows.Add("SafeImportUser");

            var valid = DataImportService.ValidateImport(importDt, "ImportTest", out var errors, checkCount: false, checkColumns: true, checkTypes: true);
            Assert.True(valid);
            Assert.Empty(errors);

            var inserted = DataImportService.ImportToTable("ImportTest", importDt);
            Assert.Equal(1, inserted);
        }

        [DbFact]
        public void UserSession_And_DbDiscovery_ExtraPaths_Work()
        {
            var login = "reload_" + Guid.NewGuid().ToString("N");
            UserSession.Register(login, "Pass1!Ab", "default", loginAfterRegistration: false);
            try
            {
                UserSession.Login(login, "Pass1!Ab");
                var status = UserSession.GetUserRole(login);
                Assert.False(string.IsNullOrWhiteSpace(status));

                UserSession.Reload();
                Assert.Equal(login, UserSession.UserLogin);
            }
            finally
            {
                Db.Users.Delete(login);
                UserSession.Logout();
            }

            if (TestDatabaseConfig.Provider == DatabaseProvider.LocalFiles)
                return; // ParseFirstSQLServer and ConnectionStringBuilder are MSSQL-specific

            var prevConn = ScrapsConfig.ConnectionString;
            try
            {
                ScrapsConfig.ConnectionString = "Data Source=.;Initial Catalog=master;Integrated Security=True;Encrypt=False;TrustServerCertificate=True;";
                var parsed = MSSQL.ParseFirstSQLServer("master");
                Assert.Equal(ScrapsConfig.ConnectionString, parsed);

                var cs = MSSQL.ConnectionStringBuilder(".", "master");
                Assert.Contains("Data Source=.", cs);
                Assert.Contains("Initial Catalog=master", cs);
            }
            finally
            {
                ScrapsConfig.ConnectionString = prevConn;
            }

            var registerLogin = "register_" + Guid.NewGuid().ToString("N");
            UserSession.Register(registerLogin, "Pass1!Ab", "default");
            try
            {
                Assert.Equal(registerLogin, UserSession.UserLogin);
            }
            finally
            {
                UserSession.Logout();
                Db.Users.Delete(registerLogin);
            }
        }
    }
}
