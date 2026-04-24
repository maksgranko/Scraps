using OfficeOpenXml;
using Scraps.Tests.Setup;
using Scraps.Configs;
using Scraps.Databases;
using Scraps.Import;
using Scraps.Localization;
using Scraps.Security;
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
            RoleManager.InitializeFromDb();
            var roleName = "rp_" + Guid.NewGuid().ToString("N");
            var table = "Таблица 1";

            var roleId = MSSQL.Roles.Create(roleName);
            try
            {
                MSSQL.RolePermissions.Set(roleName, table, PermissionFlags.Read | PermissionFlags.Export);

                var byName = MSSQL.RolePermissions.GetByRoleName(roleName);
                Assert.NotEmpty(byName);
                Assert.Contains(byName, p => p.TableName == table && (p.Flags & PermissionFlags.Read) == PermissionFlags.Read);

                var byId = MSSQL.RolePermissions.GetByRoleId(roleId);
                Assert.NotEmpty(byId);

                MSSQL.RolePermissions.Delete(roleName, table);
                var afterDeleteByName = MSSQL.RolePermissions.GetByRoleId(roleId);
                Assert.DoesNotContain(afterDeleteByName, p => p.TableName == table);

                MSSQL.RolePermissions.Set(roleId, table, PermissionFlags.Read);
                MSSQL.RolePermissions.Delete(roleId, table);
                var afterDeleteById = MSSQL.RolePermissions.GetByRoleId(roleId);
                Assert.DoesNotContain(afterDeleteById, p => p.TableName == table);

                MSSQL.RolePermissions.Set(roleName, table, PermissionFlags.Read | PermissionFlags.Write);
                MSSQL.RolePermissions.DeleteAllForRole(roleName);
                Assert.Empty(MSSQL.RolePermissions.GetByRoleId(roleId));
            }
            finally
            {
                MSSQL.Roles.Delete(roleName);
            }
        }

        [DbFact]
        public void RoleManager_PermissionsApi_Works()
        {
            RoleManager.InitializeFromDb();
            var roleName = "rm_" + Guid.NewGuid().ToString("N");
            try
            {
                RoleManager.CreateRole(roleName, ("Таблица 1", PermissionFlags.Read | PermissionFlags.Write));
                Assert.True(RoleManager.CheckAccess(roleName, "Таблица 1", PermissionFlags.Read));

                RoleManager.RemovePermission(roleName, "Таблица 1");
                Assert.False(RoleManager.CheckAccess(roleName, "Таблица 1", PermissionFlags.Read));
            }
            finally
            {
                if (RoleManager.RoleExists(roleName))
                    RoleManager.DeleteRole(roleName);
            }
        }

        [DbFact]
        public void DataApi_Overloads_And_FkJoin_Works()
        {
            RoleManager.Initialize(new[] { new Role("R1", "Users", PermissionFlags.Read) });

            var dtByRole = MSSQL.GetTableData("Users");
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
            var translated = TranslationManager.Translate(MSSQL.GetTableData("Users"), "Users");
            Assert.True(translated.Columns.Contains("Логин"));

            var generic = MSSQL.GetNx2Dictionary("Таблица 1", "Id", "Name", o => Convert.ToInt32(o), o => o?.ToString() ?? "");
            Assert.True(generic.Count > 0);

            var usersCount = Convert.ToInt32(MSSQL.ExecuteScalar("SELECT COUNT(1) FROM [Users]"));
            Assert.True(usersCount >= 0);
        }

        [DbFact]
        public void GetTableData_ExpandForeignKeys_Works()
        {
            // Простой expand без опций
            var expanded = MSSQL.GetTableData("Users", expandForeignKeys: true);
            Assert.NotNull(expanded);
            Assert.True(expanded.Columns.Count >= 2);

            // Expand с baseColumns
            var expandedCols = MSSQL.GetTableData("Users", expandForeignKeys: true, expandOptions: null, "Login");
            Assert.NotNull(expandedCols);
            Assert.True(expandedCols.Columns.Count >= 1);

            // Expand с отключенным AutoResolveDisplayColumn — JOIN есть, но alias-колонки не добавляются
            var noDisplay = MSSQL.GetTableData("Users", expandForeignKeys: true,
                new ExpandForeignKeysOptions { AutoResolveDisplayColumn = false, IncludeReferenceAllColumns = false },
                "Login", "Role");
            Assert.NotNull(noDisplay);
        }

        [Fact]
        public void ResolveDisplayColumn_CaseInsensitive_And_ExcludesPk()
        {
            // Для таблицы Roles ожидаем RoleName (case-insensitive match на "name" из preferred)
            var display = MSSQL.ResolveDisplayColumn("Roles", excludeColumn: "RoleID");
            Assert.Equal("RoleName", display);

            // Явный override должен работать даже если это PK
            ScrapsConfig.ForeignKeyDisplayColumnOverrides["Roles"] = "RoleID";
            var overridden = MSSQL.ResolveDisplayColumn("Roles", excludeColumn: "RoleID");
            Assert.Equal("RoleID", overridden);
            ScrapsConfig.ForeignKeyDisplayColumnOverrides.Remove("Roles");

            // Case-insensitive preferred: "NAME" должен найти "RoleName"
            var caseInsensitive = MSSQL.ResolveDisplayColumn("Roles", excludeColumn: "RoleID", preferred: new[] { "NAME" });
            Assert.Equal("RoleName", caseInsensitive);
        }

        [DbFact]
        public void RowEditor_AddRow_WithAutoFk_CreatesLookup()
        {
            // Добавляем пользователя с ролью "ТестоваяРоль" (которой нет в БД)
            var result = Scraps.Databases.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Databases.Utilities.TableRows.Values.Create("Login", "testuser_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", "ТестоваяРоль"),
                strictFk: false);

            Assert.True(result.Success, result.Error);
            Assert.NotNull(result.RowId);
        }

        [DbFact]
        public void RowEditor_AddRow_StrictFk_Missing_Throws()
        {
            // strict=true, роль "НесуществующаяРоль123" не найдена → ошибка
            var result = Scraps.Databases.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Databases.Utilities.TableRows.Values.Create("Login", "testuser2_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", "НесуществующаяРоль123"),
                strictFk: true);

            Assert.False(result.Success);
            Assert.Contains("не найдено", result.Error);
        }

        [DbFact]
        public void RowEditor_AddRow_TypeMismatch_Throws()
        {
            // Role — string-поле (если смотреть через DisplayColumn), но если передать int...
            // На самом деле Users.Role — int (RoleID), так что этот тест проверяет вставку int
            var result = Scraps.Databases.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Databases.Utilities.TableRows.Values.Create("Login", "testuser3_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", 99999),
                strictFk: true);

            // 99999 не существует → ошибка
            Assert.False(result.Success);
        }

        [DbFact]
        public void RowEditor_UpdateRow_ChangesFk()
        {
            // Сначала создаём пользователя
            var login = "updateuser_" + Guid.NewGuid().ToString("N");
            var addResult = Scraps.Databases.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Databases.Utilities.TableRows.Values.Create("Login", login, "Password", "pass", "Role", "default"),
                strictFk: false);
            Assert.True(addResult.Success, addResult.Error);

            // Обновляем роль на новую
            var updateResult = Scraps.Databases.Utilities.TableRows.RowEditor.UpdateRow("Users", "Login", login,
                Scraps.Databases.Utilities.TableRows.Values.Create("Role", "НоваяРольДляUpdate"),
                strictFk: false);

            Assert.True(updateResult.Success, updateResult.Error);
        }

        [Fact]
        public void Values_Create_BuildsDictionary()
        {
            var dict = Scraps.Databases.Utilities.TableRows.Values.Create("A", 1, "B", "two", "C", null);
            Assert.Equal(3, dict.Count);
            Assert.Equal(1, dict["A"]);
            Assert.Equal("two", dict["B"]);
            Assert.Null(dict["C"]);
        }

        [DbFact]
        public void RowEditor_AddRow_StrictFk_Existing_Succeeds()
        {
            // default роль существует после GenerateIfNotExists
            MSSQL.GenerateIfNotExists();
            var result = Scraps.Databases.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Databases.Utilities.TableRows.Values.Create("Login", "strictok_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", "default"),
                strictFk: true);
            Assert.True(result.Success, result.Error);
            Assert.NotNull(result.RowId);
        }

        [DbFact]
        public void RowEditor_AddRow_WithExistingRole_Strict_Succeeds()
        {
            MSSQL.GenerateIfNotExists();
            // Роль default создаётся при генерации
            var result = Scraps.Databases.Utilities.TableRows.RowEditor.AddRow("Users",
                Scraps.Databases.Utilities.TableRows.Values.Create("Login", "existingrole_" + Guid.NewGuid().ToString("N"), "Password", "pass", "Role", "default"),
                strictFk: true);
            Assert.True(result.Success, result.Error);
            Assert.NotNull(result.RowId);
        }

        [DbFact]
        public void GenerateIfNotExists_Parameterless_Works()
        {
            MSSQL.GenerateIfNotExists();
            var users = MSSQL.GetTableData("Users");
            Assert.NotNull(users);
        }

        [DbFact]
        public void ImportService_LoadExcel_ValidateImport_ImportSafe_Works()
        {
            // LoadExcelToDataTable
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

            // ValidateImport + ImportToTable
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
            // UserSession APIs with direct forwarding methods
            var login = "reload_" + Guid.NewGuid().ToString("N");
            UserSession.Register(login, "Pass1!Ab", "default", loginAfterRegistration: false);
            try
            {
                UserSession.Login(login, "Pass1!Ab");
                var status = UserSession.GetUserStatus(login);
                Assert.False(string.IsNullOrWhiteSpace(status));

                UserSession.Reload();
                Assert.Equal(login, UserSession.UserLogin);
            }
            finally
            {
                MSSQL.Users.Delete(login);
                UserSession.Logout();
            }

            // ParseFirstSQLServer early-return path + overload ConnectionStringBuilder(server,db)
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
                MSSQL.Users.Delete(registerLogin);
            }
        }
    }
}


