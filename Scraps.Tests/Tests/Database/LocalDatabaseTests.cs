using Scraps.Configs;
using Scraps.Database;
using Scraps.Database.Local;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Xunit;

namespace Scraps.Tests.DatabaseLayer
{
    public class LocalDatabaseTests : IDisposable
    {
        private readonly string _tempPath;
        private readonly string _prevLocalDataPath;
        private readonly DatabaseProvider _prevProvider;

        public LocalDatabaseTests()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), "scraps_local_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempPath);

            _prevLocalDataPath = ScrapsConfig.LocalDataPath;
            _prevProvider = ScrapsConfig.DatabaseProvider;

            ScrapsConfig.LocalDataPath = _tempPath;
            ScrapsConfig.DatabaseProvider = DatabaseProvider.LocalFiles;
            DatabaseProviderFactory.Reset();
        }

        public void Dispose()
        {
            ScrapsConfig.LocalDataPath = _prevLocalDataPath;
            ScrapsConfig.DatabaseProvider = _prevProvider;
            DatabaseProviderFactory.Reset();

            try
            {
                if (Directory.Exists(_tempPath))
                    Directory.Delete(_tempPath, true);
            }
            catch { }
        }

        #region --- LocalDatabaseSchema ---

        [Fact]
        public void Schema_CreateTable_GetTables_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Products", new Dictionary<string, string>
            {
                ["ProductID"] = "Int32",
                ["Name"] = "String",
                ["Price"] = "Decimal"
            });

            var tables = schema.GetTables();
            Assert.Contains("Products", tables);

            var columns = schema.GetTableColumns("Products");
            Assert.Contains("ProductID", columns);
            Assert.Contains("Name", columns);
            Assert.Contains("Price", columns);
        }

        [Fact]
        public void Schema_TableExists_Works()
        {
            var schema = new LocalDatabaseSchema();
            Assert.False(schema.TableExists("Missing"));

            schema.CreateTable("Exists", new Dictionary<string, string> { ["ID"] = "Int32" });
            Assert.True(schema.TableExists("Exists"));
        }

        [Fact]
        public void Schema_DropTable_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("ToDelete", new Dictionary<string, string> { ["ID"] = "Int32" });
            Assert.True(schema.TableExists("ToDelete"));

            schema.DropTable("ToDelete");
            Assert.False(schema.TableExists("ToDelete"));
        }

        [Fact]
        public void Schema_GetTableSchema_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Orders", new Dictionary<string, string>
            {
                ["OrderID"] = "Int32",
                ["Total"] = "Double"
            });

            var dt = schema.GetTableSchema("Orders");
            Assert.Equal("Orders", dt.TableName);
            Assert.Equal(typeof(int), dt.Columns["OrderID"].DataType);
            Assert.Equal(typeof(double), dt.Columns["Total"].DataType);
        }

        [Fact]
        public void Schema_CreateTable_Duplicate_Throws()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Dup", new Dictionary<string, string> { ["ID"] = "Int32" });
            Assert.Throws<InvalidOperationException>(() => schema.CreateTable("Dup", new Dictionary<string, string> { ["ID"] = "Int32" }));
        }

        #endregion

        #region --- LocalDatabaseData ---

        [Fact]
        public void Data_GetTableData_ReturnsEmptyTable()
        {
            new LocalDatabaseSchema().CreateTable("Items", new Dictionary<string, string> { ["ID"] = "Int32", ["Name"] = "String" });
            var data = new LocalDatabaseData();
            var dt = data.GetTableData("Items");
            Assert.NotNull(dt);
            Assert.Equal(0, dt.Rows.Count);
            Assert.Equal(2, dt.Columns.Count);
        }

        [Fact]
        public void Data_FindByColumn_ExactMatch_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Books", new Dictionary<string, string> { ["ID"] = "Int32", ["Title"] = "String" });
            var data = new LocalDatabaseData();

            var dt = data.GetTableData("Books");
            var row = dt.NewRow();
            row["ID"] = 1;
            row["Title"] = "C# in Depth";
            dt.Rows.Add(row);
            data.ApplyTableChanges("Books", dt);

            var found = data.FindByColumn("Books", "Title", "C# in Depth", exactMatch: true);
            Assert.Single(found.Rows);
            Assert.Equal("C# in Depth", found.Rows[0]["Title"]);

            var notFound = data.FindByColumn("Books", "Title", "Missing", exactMatch: true);
            Assert.Empty(notFound.Rows);
        }

        [Fact]
        public void Data_FindByColumn_PartialMatch_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Movies", new Dictionary<string, string> { ["ID"] = "Int32", ["Title"] = "String" });
            var data = new LocalDatabaseData();

            var dt = data.GetTableData("Movies");
            var row = dt.NewRow();
            row["ID"] = 1;
            row["Title"] = "The Matrix";
            dt.Rows.Add(row);
            data.ApplyTableChanges("Movies", dt);

            var found = data.FindByColumn("Movies", "Title", "Mat", exactMatch: false);
            Assert.Single(found.Rows);
        }

        [Fact]
        public void Data_BulkInsert_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Logs", new Dictionary<string, string> { ["ID"] = "Int32", ["Message"] = "String" });
            var data = new LocalDatabaseData();

            var bulk = new DataTable();
            bulk.Columns.Add("ID", typeof(int));
            bulk.Columns.Add("Message", typeof(string));
            bulk.Rows.Add(1, "First");
            bulk.Rows.Add(2, "Second");

            data.BulkInsert("Logs", bulk);

            var dt = data.GetTableData("Logs");
            Assert.Equal(2, dt.Rows.Count);
        }

        [Fact]
        public void Data_GetNx2Dictionary_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Categories", new Dictionary<string, string> { ["CategoryID"] = "Int32", ["CategoryName"] = "String" });
            var data = new LocalDatabaseData();

            var dt = data.GetTableData("Categories");
            dt.Rows.Add(1, "Books");
            dt.Rows.Add(2, "Movies");
            data.ApplyTableChanges("Categories", dt);

            var dict = data.GetNx2Dictionary("Categories", "CategoryID", "CategoryName");
            Assert.Equal("Books", dict["1"]);
            Assert.Equal("Movies", dict["2"]);
        }

        [Fact]
        public void Data_GetNx1List_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Tags", new Dictionary<string, string> { ["TagName"] = "String" });
            var data = new LocalDatabaseData();

            var dt = data.GetTableData("Tags");
            dt.Rows.Add("C#");
            dt.Rows.Add("Java");
            dt.Rows.Add("C#");
            data.ApplyTableChanges("Tags", dt);

            var list = data.GetNx1List("Tags", "TagName", distinct: true, sort: true);
            Assert.Equal(2, list.Count);
            Assert.Equal("C#", list[0]);
            Assert.Equal("Java", list[1]);
        }

        #endregion

        #region --- LocalDatabaseUsers ---

        [Fact]
        public void Users_Create_And_GetByLogin_Works()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Simple());
            var users = new LocalDatabaseUsers();

            users.Create("ivan", "pass123", "admin");
            var row = users.GetByLogin("ivan");
            Assert.NotNull(row);
            Assert.Equal("ivan", row["Login"]);
            Assert.Equal("admin", row["Role"]);
        }

        [Fact]
        public void Users_GetByLogin_Missing_Throws()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Simple());
            var users = new LocalDatabaseUsers();
            Assert.Throws<InvalidOperationException>(() => users.GetByLogin("missing"));
        }

        [Fact]
        public void Users_ChangePassword_Works()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Simple());
            var users = new LocalDatabaseUsers();
            users.Create("petr", "oldpass", "user");

            users.ChangePassword("petr", "newpass");
            var row = users.GetByLogin("petr");
            Assert.Equal("newpass", row["Password"]);
        }

        [Fact]
        public void Users_Delete_Works()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Simple());
            var users = new LocalDatabaseUsers();
            users.Create("delete_me", "pass", "user");
            Assert.NotNull(users.GetByLogin("delete_me"));

            users.Delete("delete_me");
            Assert.Throws<InvalidOperationException>(() => users.GetByLogin("delete_me"));
        }

        [Fact]
        public void Users_AutoIncrement_UserID()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Simple());
            var users = new LocalDatabaseUsers();
            users.Create("u1", "p1", "r1");
            users.Create("u2", "p2", "r2");

            var row1 = users.GetByLogin("u1");
            var row2 = users.GetByLogin("u2");
            Assert.NotEqual(row1["UserID"], row2["UserID"]);
        }

        #endregion

        #region --- LocalDatabaseRoles ---

        [Fact]
        public void Roles_Create_And_GetByName_Works()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Standard());
            var roles = new LocalDatabaseRoles();

            var id = roles.Create("Manager");
            Assert.True(id > 0);

            var foundId = roles.GetRoleIdByName("Manager");
            Assert.Equal(id, foundId);
        }

        [Fact]
        public void Roles_Create_Duplicate_Throws()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Standard());
            var roles = new LocalDatabaseRoles();
            roles.Create("Unique");
            Assert.Throws<InvalidOperationException>(() => roles.Create("Unique"));
        }

        [Fact]
        public void Roles_Delete_Works()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Standard());
            var roles = new LocalDatabaseRoles();
            roles.Create("ToRemove");
            Assert.NotNull(roles.GetRoleIdByName("ToRemove"));

            roles.Delete("ToRemove");
            Assert.Null(roles.GetRoleIdByName("ToRemove"));
        }

        [Fact]
        public void Roles_Rename_Works()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Standard());
            var roles = new LocalDatabaseRoles();
            roles.Create("OldName");

            roles.Rename("OldName", "NewName");
            Assert.Null(roles.GetRoleIdByName("OldName"));
            Assert.NotNull(roles.GetRoleIdByName("NewName"));
        }

        [Fact]
        public void Roles_GetAll_ReturnsList()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Standard());
            var roles = new LocalDatabaseRoles();
            var all = roles.GetAll();
            Assert.NotEmpty(all);
        }

        #endregion

        #region --- LocalDatabaseRolePermissions ---

        [Fact]
        public void RolePermissions_Set_And_GetByRoleName_Works()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Full());
            var roles = new LocalDatabaseRoles();
            var perms = new LocalDatabaseRolePermissions();

            var roleId = roles.Create("PermRole");
            perms.Set("PermRole", "Products", PermissionFlags.Read | PermissionFlags.Write);

            var list = perms.GetByRoleName("PermRole");
            Assert.Single(list);
            Assert.Equal("Products", list[0].TableName);
            Assert.Equal(PermissionFlags.Read | PermissionFlags.Write, list[0].Flags);
        }

        [Fact]
        public void RolePermissions_Delete_Works()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Full());
            var roles = new LocalDatabaseRoles();
            var perms = new LocalDatabaseRolePermissions();

            roles.Create("DelPermRole");
            perms.Set("DelPermRole", "Orders", PermissionFlags.Read);
            Assert.Single(perms.GetByRoleName("DelPermRole"));

            perms.Delete("DelPermRole", "Orders");
            Assert.Empty(perms.GetByRoleName("DelPermRole"));
        }

        [Fact]
        public void RolePermissions_DeleteAllForRole_Works()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Full());
            var roles = new LocalDatabaseRoles();
            var perms = new LocalDatabaseRolePermissions();

            roles.Create("AllPermRole");
            perms.Set("AllPermRole", "A", PermissionFlags.Read);
            perms.Set("AllPermRole", "B", PermissionFlags.Write);
            Assert.Equal(2, perms.GetByRoleName("AllPermRole").Count);

            perms.DeleteAllForRole("AllPermRole");
            Assert.Empty(perms.GetByRoleName("AllPermRole"));
        }

        #endregion

        #region --- LocalRowEditor ---

        [Fact]
        public void RowEditor_AddRow_Works()
        {
            new LocalDatabaseSchema().CreateTable("Students", new Dictionary<string, string> { ["StudentID"] = "Int32", ["Name"] = "String" });
            var editor = new LocalRowEditor();

            var result = editor.AddRow("Students", new Dictionary<string, object>
            {
                ["Name"] = "Anna"
            });

            Assert.True(result.Success);
            Assert.NotNull(result.RowId);
        }

        [Fact]
        public void RowEditor_AddRow_WithChildInsert_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Parents", new Dictionary<string, string> { ["ParentID"] = "Int32", ["Name"] = "String" });
            schema.CreateTable("Children", new Dictionary<string, string> { ["ChildID"] = "Int32", ["ParentID"] = "Int32", ["Name"] = "String" });

            var editor = new LocalRowEditor();
            var result = editor.AddRow("Parents", new Dictionary<string, object> { ["Name"] = "Parent1" }, strictFk: true,
                new ChildInsert("Children", new Dictionary<string, object> { ["Name"] = "Child1" }));

            Assert.True(result.Success);
        }

        [Fact]
        public void RowEditor_UpdateRow_Works()
        {
            new LocalDatabaseSchema().CreateTable("Workers", new Dictionary<string, string> { ["WorkerID"] = "Int32", ["Name"] = "String" });
            var editor = new LocalRowEditor();

            editor.AddRow("Workers", new Dictionary<string, object> { ["Name"] = "OldName" });
            var data = new LocalDatabaseData().GetTableData("Workers");
            var id = data.Rows[0]["WorkerID"];

            var result = editor.UpdateRow("Workers", "WorkerID", id, new Dictionary<string, object> { ["Name"] = "NewName" });
            Assert.True(result.Success);

            var updated = new LocalDatabaseData().GetTableData("Workers");
            Assert.Equal("NewName", updated.Rows[0]["Name"]);
        }

        [Fact]
        public void RowEditor_UpdateRow_Missing_Throws()
        {
            new LocalDatabaseSchema().CreateTable("Empty", new Dictionary<string, string> { ["ID"] = "Int32" });
            var editor = new LocalRowEditor();
            var result = editor.UpdateRow("Empty", "ID", 999, new Dictionary<string, object> { ["X"] = "Y" });
            Assert.False(result.Success);
        }

        #endregion

        #region --- LocalForeignKeyProvider ---

        [Fact]
        public void FK_GetForeignKeys_ByConvention_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Roles", new Dictionary<string, string> { ["RoleID"] = "Int32", ["RoleName"] = "String" });
            schema.CreateTable("Users", new Dictionary<string, string> { ["UserID"] = "Int32", ["Login"] = "String", ["RoleID"] = "Int32" });

            var fk = new LocalForeignKeyProvider();
            var keys = fk.GetForeignKeys("Users");
            Assert.Single(keys);
            Assert.Equal("RoleID", keys[0].ColumnName);
            Assert.Equal("Roles", keys[0].ReferenceTable);
        }

        [Fact]
        public void FK_ResolveDisplayColumn_Preferred_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Books", new Dictionary<string, string> { ["BookID"] = "Int32", ["Title"] = "String", ["Price"] = "Decimal" });

            var fk = new LocalForeignKeyProvider();
            var display = fk.ResolveDisplayColumn("Books");
            Assert.Equal("Title", display);
        }

        [Fact]
        public void FK_GetTableEditMetadata_Works()
        {
            var schema = new LocalDatabaseSchema();
            schema.CreateTable("Authors", new Dictionary<string, string> { ["AuthorID"] = "Int32", ["Name"] = "String" });
            schema.CreateTable("Books", new Dictionary<string, string> { ["BookID"] = "Int32", ["Title"] = "String", ["AuthorID"] = "Int32" });

            var fk = new LocalForeignKeyProvider();
            var meta = fk.GetTableEditMetadata("Books");
            Assert.Equal("Books", meta.TableName);
            Assert.Contains(meta.Columns, c => c.ColumnName == "AuthorID" && c.IsForeignKey);
            Assert.Contains(meta.Columns, c => c.ColumnName == "Title" && !c.IsForeignKey);
        }

        #endregion

        #region --- LocalDatabase static helpers ---

        [Fact]
        public void GenerateIfNotExists_CreatesUsersAndRoles()
        {
            LocalDatabase.GenerateIfNotExists(DatabaseGenerationOptions.Full());

            Assert.True(LocalDatabase.TableExists("Users"));
            Assert.True(LocalDatabase.TableExists("Roles"));
            Assert.True(LocalDatabase.TableExists("RolePermissions"));
        }

        [Fact]
        public void Initialize_CreatesDefaultUser()
        {
            var db = new LocalDatabase();
            db.Initialize(DatabaseGenerationOptions.Simple());

            var users = new LocalDatabaseUsers();
            var admin = users.GetByLogin("admin");
            Assert.NotNull(admin);
        }

        #endregion
    }
}
