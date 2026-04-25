using Scraps.Configs;
using Scraps.Database;
using Scraps.Security;
using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Scraps.Tests.DatabaseLayer
{
    /// <summary>
    /// Fake database для тестирования DatabaseProviderFactory и Database static facade.
    /// </summary>
    public class FakeDatabase : DatabaseBase
    {
        public override DatabaseProvider Provider => DatabaseProvider.LocalFiles;

        public bool TestConnectionCalled { get; private set; }
        public bool InitializeCalled { get; private set; }

        public override bool TestConnection()
        {
            TestConnectionCalled = true;
            return base.TestConnection();
        }

        public override void Initialize(DatabaseGenerationOptions options)
        {
            InitializeCalled = true;
        }
    }

    public class DatabaseProviderFactoryTests
    {
        public DatabaseProviderFactoryTests()
        {
            DatabaseProviderFactory.Reset();
        }

        #region --- Register ---

        [Fact]
        public void Register_AddsProvider()
        {
            var provider = (DatabaseProvider)999;
            DatabaseProviderFactory.Register(provider, () => new FakeDatabase());
            var db = DatabaseProviderFactory.Create(provider);
            Assert.NotNull(db);
            Assert.IsType<FakeDatabase>(db);
        }

        [Fact]
        public void Register_OverwritesExistingProvider()
        {
            var provider = (DatabaseProvider)998;
            DatabaseProviderFactory.Register(provider, () => new FakeDatabase());
            DatabaseProviderFactory.Register(provider, () => new FakeDatabase());
            var db = DatabaseProviderFactory.Create(provider);
            Assert.NotNull(db);
        }

        #endregion

        #region --- Create ---

        [Fact]
        public void Create_ReturnsInstance()
        {
            var provider = (DatabaseProvider)997;
            DatabaseProviderFactory.Register(provider, () => new FakeDatabase());
            var db = DatabaseProviderFactory.Create(provider);
            Assert.NotNull(db);
            Assert.IsType<FakeDatabase>(db);
        }

        [Fact]
        public void Create_UnregisteredProvider_Throws()
        {
            var provider = (DatabaseProvider)996;
            var ex = Assert.Throws<InvalidOperationException>(() => DatabaseProviderFactory.Create(provider));
            Assert.Contains("не зарегистрирован", ex.Message);
        }

        #endregion

        #region --- Current ---

        [Fact]
        public void Current_WhenNone_Throws()
        {
            var prev = ScrapsConfig.DatabaseProvider;
            try
            {
                ScrapsConfig.DatabaseProvider = DatabaseProvider.None;
                DatabaseProviderFactory.Reset();
                var ex = Assert.Throws<InvalidOperationException>(() => DatabaseProviderFactory.Current);
                Assert.Contains("DatabaseProvider не установлен", ex.Message);
            }
            finally
            {
                ScrapsConfig.DatabaseProvider = prev;
                DatabaseProviderFactory.Reset();
            }
        }

        [Fact]
        public void Current_LazyInitializes()
        {
            var prev = ScrapsConfig.DatabaseProvider;
            try
            {
                var provider = (DatabaseProvider)995;
                ScrapsConfig.DatabaseProvider = provider;
                DatabaseProviderFactory.Reset();
                DatabaseProviderFactory.Register(provider, () => new FakeDatabase());
                var db1 = DatabaseProviderFactory.Current;
                var db2 = DatabaseProviderFactory.Current;
                Assert.NotNull(db1);
                Assert.Same(db1, db2); // caching
            }
            finally
            {
                ScrapsConfig.DatabaseProvider = prev;
                DatabaseProviderFactory.Reset();
            }
        }

        [Fact]
        public void Current_AfterReset_Reinitializes()
        {
            var prev = ScrapsConfig.DatabaseProvider;
            try
            {
                var provider = (DatabaseProvider)994;
                ScrapsConfig.DatabaseProvider = provider;
                DatabaseProviderFactory.Reset();
                DatabaseProviderFactory.Register(provider, () => new FakeDatabase());
                var db1 = DatabaseProviderFactory.Current;
                DatabaseProviderFactory.Reset();
                var db2 = DatabaseProviderFactory.Current;
                Assert.NotNull(db2);
                Assert.NotSame(db1, db2);
            }
            finally
            {
                ScrapsConfig.DatabaseProvider = prev;
                DatabaseProviderFactory.Reset();
            }
        }

        #endregion

        #region --- Reset ---

        [Fact]
        public void Reset_ClearsCurrent()
        {
            var prev = ScrapsConfig.DatabaseProvider;
            try
            {
                var provider = (DatabaseProvider)993;
                ScrapsConfig.DatabaseProvider = provider;
                DatabaseProviderFactory.Reset();
                DatabaseProviderFactory.Register(provider, () => new FakeDatabase());
                var db1 = DatabaseProviderFactory.Current;
                DatabaseProviderFactory.Reset();
                var db2 = DatabaseProviderFactory.Current;
                Assert.NotSame(db1, db2);
            }
            finally
            {
                ScrapsConfig.DatabaseProvider = prev;
                DatabaseProviderFactory.Reset();
            }
        }

        [Fact]
        public void Current_ConcurrentAccess_ReturnsSameInstance()
        {
            var prev = ScrapsConfig.DatabaseProvider;
            try
            {
                var provider = (DatabaseProvider)990;
                ScrapsConfig.DatabaseProvider = provider;
                DatabaseProviderFactory.Reset();
                DatabaseProviderFactory.Register(provider, () => new FakeDatabase());

                IDatabase[] results = new IDatabase[100];
                System.Threading.Tasks.Parallel.For(0, 100, i =>
                {
                    results[i] = DatabaseProviderFactory.Current;
                });

                Assert.All(results, db => Assert.Same(results[0], db));
            }
            finally
            {
                ScrapsConfig.DatabaseProvider = prev;
                DatabaseProviderFactory.Reset();
            }
        }

        #endregion
    }

    public class DatabaseBaseTests
    {
        [Fact]
        public void TestConnection_DelegatesToConnection()
        {
            var fake = new FakeDatabase();
            fake.Connection = new FakeConnection();
            Assert.True(fake.TestConnection());
            Assert.True(fake.TestConnectionCalled);
        }

        [Fact]
        public void Initialize_Overridable()
        {
            var fake = new FakeDatabase();
            fake.Initialize(DatabaseGenerationMode.Full);
            Assert.True(fake.InitializeCalled);
        }

        [Fact]
        public void Properties_DefaultToNull()
        {
            var fake = new FakeDatabase();
            Assert.Null(fake.Connection);
            Assert.Null(fake.Schema);
            Assert.Null(fake.Data);
            Assert.Null(fake.Users);
            Assert.Null(fake.Roles);
            Assert.Null(fake.RolePermissions);
            Assert.Null(fake.RowEditor);
            Assert.Null(fake.VirtualTables);
            Assert.Null(fake.ForeignKeys);
        }

        [Fact]
        public void Provider_Overridden()
        {
            var fake = new FakeDatabase();
            Assert.Equal(DatabaseProvider.LocalFiles, fake.Provider);
        }

        private class FakeConnection : IDatabaseConnection
        {
            public string ConnectionString => "fake";
            public bool TestConnection() => true;
            public void ExecuteNonQuery(string sql, params object[] parameters) { }
            public object ExecuteScalar(string sql, params object[] parameters) => null;
            public DataTable GetDataTable(string sql, params object[] parameters) => new DataTable();
        }
    }

    public class DatabaseFacadeTests
    {
        private readonly DatabaseProvider _prevProvider;

        public DatabaseFacadeTests()
        {
            _prevProvider = ScrapsConfig.DatabaseProvider;
            DatabaseProviderFactory.Reset();
        }

        private void DisposeFixture()
        {
            ScrapsConfig.DatabaseProvider = _prevProvider;
            DatabaseProviderFactory.Reset();
        }

        private FakeDatabase SetupFake()
        {
            var fake = new FakeDatabase();
            var provider = (DatabaseProvider)992;
            ScrapsConfig.DatabaseProvider = provider;
            DatabaseProviderFactory.Reset();
            DatabaseProviderFactory.Register(provider, () => fake);
            return fake;
        }

        #region --- Connection forwarding ---

        [Fact]
        public void TestConnection_ForwardsToConnection()
        {
            var fake = SetupFake();
            fake.Connection = new FakeConnection();
            Assert.True(global::Scraps.Database.Database.TestConnection());
        }

        [Fact]
        public void ExecuteNonQuery_ForwardsToConnection()
        {
            var fake = SetupFake();
            var conn = new FakeConnection();
            fake.Connection = conn;
            global::Scraps.Database.Database.ExecuteNonQuery("SELECT 1");
            Assert.Equal("SELECT 1", conn.LastSql);
        }

        [Fact]
        public void ExecuteScalar_ForwardsToConnection()
        {
            var fake = SetupFake();
            var conn = new FakeConnection();
            fake.Connection = conn;
            conn.ScalarResult = 42;
            Assert.Equal(42, global::Scraps.Database.Database.ExecuteScalar("SELECT 1"));
        }

        [Fact]
        public void GetDataTable_ForwardsToConnection()
        {
            var fake = SetupFake();
            var conn = new FakeConnection();
            fake.Connection = conn;
            var dt = global::Scraps.Database.Database.GetDataTable("SELECT 1");
            Assert.NotNull(dt);
        }

        #endregion

        #region --- Schema forwarding ---

        [Fact]
        public void GetTables_ForwardsToSchema()
        {
            var fake = SetupFake();
            var schema = new FakeSchema();
            fake.Schema = schema;
            global::Scraps.Database.Database.GetTables();
            Assert.True(schema.GetTablesCalled);
        }

        [Fact]
        public void GetTableColumns_ForwardsToSchema()
        {
            var fake = SetupFake();
            var schema = new FakeSchema();
            fake.Schema = schema;
            global::Scraps.Database.Database.GetTableColumns("T");
            Assert.Equal("T", schema.LastTable);
        }

        [Fact]
        public void GetTableSchema_ForwardsToSchema()
        {
            var fake = SetupFake();
            var schema = new FakeSchema();
            fake.Schema = schema;
            global::Scraps.Database.Database.GetTableSchema("T");
            Assert.Equal("T", schema.LastTable);
        }

        #endregion

        #region --- Data forwarding ---

        [Fact]
        public void GetTableData_ForwardsToData()
        {
            var fake = SetupFake();
            var data = new FakeData();
            fake.Data = data;
            global::Scraps.Database.Database.GetTableData("T");
            Assert.Equal("T", data.LastTable);
        }

        [Fact]
        public void GetTableDataExpanded_ForwardsToData()
        {
            var fake = SetupFake();
            var data = new FakeData();
            fake.Data = data;
            global::Scraps.Database.Database.GetTableDataExpanded("T", new List<ForeignKeyJoin>());
            Assert.Equal("T", data.LastTable);
        }

        [Fact]
        public void FindByColumn_ForwardsToData()
        {
            var fake = SetupFake();
            var data = new FakeData();
            fake.Data = data;
            global::Scraps.Database.Database.FindByColumn("T", "C", "V");
            Assert.Equal("T", data.LastTable);
            Assert.Equal("C", data.LastColumn);
            Assert.Equal("V", data.LastValue);
        }

        [Fact]
        public void ApplyTableChanges_ForwardsToData()
        {
            var fake = SetupFake();
            var data = new FakeData();
            fake.Data = data;
            var dt = new DataTable();
            global::Scraps.Database.Database.ApplyTableChanges("T", dt);
            Assert.Equal("T", data.LastTable);
            Assert.Same(dt, data.LastDataTable);
        }

        [Fact]
        public void BulkInsert_ForwardsToData()
        {
            var fake = SetupFake();
            var data = new FakeData();
            fake.Data = data;
            var dt = new DataTable();
            global::Scraps.Database.Database.BulkInsert("T", dt);
            Assert.Equal("T", data.LastTable);
            Assert.Same(dt, data.LastDataTable);
        }

        #endregion

        #region --- Users forwarding ---

        [Fact]
        public void GetUserByLogin_ForwardsToUsers()
        {
            var fake = SetupFake();
            var users = new FakeUsers();
            fake.Users = users;
            global::Scraps.Database.Database.GetUserByLogin("admin");
            Assert.Equal("admin", users.LastLogin);
        }

        [Fact]
        public void CreateUser_ForwardsToUsers()
        {
            var fake = SetupFake();
            var users = new FakeUsers();
            fake.Users = users;
            global::Scraps.Database.Database.CreateUser("admin", "pass", "role");
            Assert.Equal("admin", users.LastLogin);
            Assert.Equal("pass", users.LastPassword);
            Assert.Equal("role", users.LastRole);
        }

        [Fact]
        public void DeleteUser_ForwardsToUsers()
        {
            var fake = SetupFake();
            var users = new FakeUsers();
            fake.Users = users;
            global::Scraps.Database.Database.DeleteUser("admin");
            Assert.Equal("admin", users.LastLogin);
        }

        [Fact]
        public void ChangeUserPassword_ForwardsToUsers()
        {
            var fake = SetupFake();
            var users = new FakeUsers();
            fake.Users = users;
            global::Scraps.Database.Database.ChangeUserPassword("admin", "newpass");
            Assert.Equal("admin", users.LastLogin);
            Assert.Equal("newpass", users.LastPassword);
        }

        #endregion

        #region --- Roles forwarding ---

        [Fact]
        public void GetRoleIdByName_ForwardsToRoles()
        {
            var fake = SetupFake();
            var roles = new FakeRoles();
            fake.Roles = roles;
            global::Scraps.Database.Database.GetRoleIdByName("admin");
            Assert.Equal("admin", roles.LastName);
        }

        [Fact]
        public void CreateRole_ForwardsToRoles()
        {
            var fake = SetupFake();
            var roles = new FakeRoles();
            fake.Roles = roles;
            global::Scraps.Database.Database.CreateRole("admin");
            Assert.Equal("admin", roles.LastName);
        }

        [Fact]
        public void DeleteRole_ForwardsToRoles()
        {
            var fake = SetupFake();
            var roles = new FakeRoles();
            fake.Roles = roles;
            global::Scraps.Database.Database.DeleteRole("admin");
            Assert.Equal("admin", roles.LastName);
        }

        #endregion

        #region --- RowEditor forwarding ---

        [Fact]
        public void AddRow_ForwardsToRowEditor()
        {
            var fake = SetupFake();
            var editor = new FakeRowEditor();
            fake.RowEditor = editor;
            var values = new Dictionary<string, object>();
            global::Scraps.Database.Database.AddRow("T", values);
            Assert.Equal("T", editor.LastTable);
            Assert.Same(values, editor.LastValues);
        }

        [Fact]
        public void UpdateRow_ForwardsToRowEditor()
        {
            var fake = SetupFake();
            var editor = new FakeRowEditor();
            fake.RowEditor = editor;
            var values = new Dictionary<string, object>();
            global::Scraps.Database.Database.UpdateRow("T", "Id", 1, values);
            Assert.Equal("T", editor.LastTable);
            Assert.Same(values, editor.LastValues);
        }

        #endregion

        #region --- ForeignKeys forwarding ---

        [Fact]
        public void GetForeignKeys_ForwardsToFK()
        {
            var fake = SetupFake();
            var fk = new FakeForeignKeys();
            fake.ForeignKeys = fk;
            global::Scraps.Database.Database.GetForeignKeys("T");
            Assert.Equal("T", fk.LastTable);
        }

        [Fact]
        public void GetForeignKeyLookup_ForwardsToFK()
        {
            var fake = SetupFake();
            var fk = new FakeForeignKeys();
            fake.ForeignKeys = fk;
            global::Scraps.Database.Database.GetForeignKeyLookup("T", "C");
            Assert.Equal("T", fk.LastTable);
            Assert.Equal("C", fk.LastColumn);
        }

        [Fact]
        public void ResolveDisplayColumn_ForwardsToFK()
        {
            var fake = SetupFake();
            var fk = new FakeForeignKeys();
            fake.ForeignKeys = fk;
            global::Scraps.Database.Database.ResolveDisplayColumn("T");
            Assert.Equal("T", fk.LastTable);
        }

        #endregion

        #region --- VirtualTables forwarding ---

        [Fact]
        public void RegisterVirtualTable_ForwardsToVT()
        {
            var fake = SetupFake();
            var vt = new FakeVirtualTables();
            fake.VirtualTables = vt;
            global::Scraps.Database.Database.RegisterVirtualTable("V", "SELECT 1");
            Assert.Equal("V", vt.LastName);
            Assert.Equal("SELECT 1", vt.LastQuery);
        }

        [Fact]
        public void GetVirtualTableData_ForwardsToVT()
        {
            var fake = SetupFake();
            var vt = new FakeVirtualTables();
            fake.VirtualTables = vt;
            global::Scraps.Database.Database.GetVirtualTableData("V");
            Assert.Equal("V", vt.LastName);
        }

        #endregion

        #region --- Initialize forwarding ---

        [Fact]
        public void Initialize_ForwardsToBase()
        {
            var fake = SetupFake();
            global::Scraps.Database.Database.Initialize(new DatabaseGenerationOptions { Mode = DatabaseGenerationMode.Full });
            Assert.True(fake.InitializeCalled);
        }

        #endregion

        #region --- Null sub-service handling ---

        [Fact]
        public void TestConnection_WhenConnectionNull_ReturnsFalse()
        {
            var fake = SetupFake();
            Assert.False(global::Scraps.Database.Database.TestConnection());
        }

        #endregion

        #region --- Guard expressions (null provider capabilities) ---

        [Fact]
        public void GetTables_SchemaNull_Throws()
        {
            var fake = SetupFake();
            fake.Schema = null;
            var ex = Assert.Throws<InvalidOperationException>(() => global::Scraps.Database.Database.GetTables());
            Assert.Contains("IDatabaseSchema", ex.Message);
        }

        [Fact]
        public void GetTableData_DataNull_Throws()
        {
            var fake = SetupFake();
            fake.Data = null;
            var ex = Assert.Throws<InvalidOperationException>(() => global::Scraps.Database.Database.GetTableData("T"));
            Assert.Contains("IDatabaseData", ex.Message);
        }

        [Fact]
        public void GetUserByLogin_UsersNull_Throws()
        {
            var fake = SetupFake();
            fake.Users = null;
            var ex = Assert.Throws<InvalidOperationException>(() => global::Scraps.Database.Database.GetUserByLogin("u"));
            Assert.Contains("IDatabaseUsers", ex.Message);
        }

        [Fact]
        public void GetRoleIdByName_RolesNull_Throws()
        {
            var fake = SetupFake();
            fake.Roles = null;
            var ex = Assert.Throws<InvalidOperationException>(() => global::Scraps.Database.Database.GetRoleIdByName("r"));
            Assert.Contains("IDatabaseRoles", ex.Message);
        }

        [Fact]
        public void AddRow_RowEditorNull_Throws()
        {
            var fake = SetupFake();
            fake.RowEditor = null;
            var ex = Assert.Throws<InvalidOperationException>(() => global::Scraps.Database.Database.AddRow("T", new Dictionary<string, object>()));
            Assert.Contains("IRowEditor", ex.Message);
        }

        [Fact]
        public void GetForeignKeys_ForeignKeysNull_Throws()
        {
            var fake = SetupFake();
            fake.ForeignKeys = null;
            var ex = Assert.Throws<InvalidOperationException>(() => global::Scraps.Database.Database.GetForeignKeys("T"));
            Assert.Contains("IForeignKeyProvider", ex.Message);
        }

        [Fact]
        public void RegisterVirtualTable_VirtualTablesNull_Throws()
        {
            var fake = SetupFake();
            fake.VirtualTables = null;
            var ex = Assert.Throws<InvalidOperationException>(() => global::Scraps.Database.Database.RegisterVirtualTable("V", "SELECT 1"));
            Assert.Contains("IVirtualTableRegistry", ex.Message);
        }

        #endregion

        #region --- Fake implementations ---

        private class FakeConnection : IDatabaseConnection
        {
            public string ConnectionString => "fake";
            public string LastSql { get; private set; }
            public object ScalarResult { get; set; }

            public bool TestConnection() => true;
            public void ExecuteNonQuery(string sql, params object[] parameters) => LastSql = sql;
            public object ExecuteScalar(string sql, params object[] parameters) { LastSql = sql; return ScalarResult; }
            public DataTable GetDataTable(string sql, params object[] parameters) { LastSql = sql; return new DataTable(); }
        }

        private class FakeSchema : IDatabaseSchema
        {
            public bool GetTablesCalled { get; private set; }
            public string LastTable { get; private set; }

            public List<string> GetTables(bool includeViews = false, bool includeSystem = false)
            {
                GetTablesCalled = true;
                return new List<string>();
            }

            public List<string> GetTableColumns(string tableName)
            {
                LastTable = tableName;
                return new List<string>();
            }

            public DataTable GetTableSchema(string tableName)
            {
                LastTable = tableName;
                return new DataTable();
            }

            public bool IsIdentityColumn(string tableName, string columnName) => false;
            public bool IsNullableColumn(string tableName, string columnName) => false;
        }

        private class FakeData : IDatabaseData
        {
            public string LastTable { get; private set; }
            public string LastColumn { get; private set; }
            public object LastValue { get; private set; }
            public DataTable LastDataTable { get; private set; }

            public DataTable GetTableData(string tableName, params string[] columns) { LastTable = tableName; return new DataTable(); }
            public DataTable GetTableData(string tableName, string connectionString, params string[] columns) { LastTable = tableName; return new DataTable(); }
            public DataTable GetTableDataExpanded(string tableName, IEnumerable<ForeignKeyJoin> foreignKeys, params string[] baseColumns) { LastTable = tableName; return new DataTable(); }
            public DataTable FindByColumn(string tableName, string columnName, object value, bool exactMatch = true) { LastTable = tableName; LastColumn = columnName; LastValue = value; return new DataTable(); }
            public void ApplyTableChanges(string tableName, DataTable changes) { LastTable = tableName; LastDataTable = changes; }
            public void BulkInsert(string tableName, DataTable data) { LastTable = tableName; LastDataTable = data; }
            public Dictionary<string, string> GetNx2Dictionary(string tableName, string keyColumn, string valueColumn) => new Dictionary<string, string>();
            public List<string> GetNx1List(string tableName, string columnName, bool distinct = true, bool sort = true) => new List<string>();
        }

        private class FakeUsers : IDatabaseUsers
        {
            public string LastLogin { get; private set; }
            public string LastPassword { get; private set; }
            public string LastRole { get; private set; }

            public DataRow GetByLogin(string login) { LastLogin = login; return null; }
            public string GetUserStatus(string login) { LastLogin = login; return null; }
            public void Create(string login, string password, string role) { LastLogin = login; LastPassword = password; LastRole = role; }
            public void Delete(string login) { LastLogin = login; }
            public void ChangePassword(string login, string newPassword) { LastLogin = login; LastPassword = newPassword; }
            public void ChangeRole(string login, string newRole) { LastLogin = login; LastRole = newRole; }
        }

        private class FakeRoles : IDatabaseRoles
        {
            public string LastName { get; private set; }

            public string GetRoleNameById(int roleId) => null;
            public int? GetRoleIdByName(string roleName) { LastName = roleName; return null; }
            public int Create(string roleName) { LastName = roleName; return -1; }
            public void Delete(string roleName) { LastName = roleName; }
            public void Rename(string oldName, string newName) { LastName = oldName; }
            public List<RoleInfo> GetAll() => new List<RoleInfo>();
        }

        private class FakeRowEditor : IRowEditor
        {
            public string LastTable { get; private set; }
            public Dictionary<string, object> LastValues { get; private set; }

            public AddEditResult AddRow(string tableName, Dictionary<string, object> values, bool strictFk = true, params ChildInsert[] children)
            {
                LastTable = tableName;
                LastValues = values;
                return null;
            }

            public AddEditResult UpdateRow(string tableName, string idColumn, object idValue, Dictionary<string, object> values, bool strictFk = true)
            {
                LastTable = tableName;
                LastValues = values;
                return null;
            }
        }

        private class FakeForeignKeys : IForeignKeyProvider
        {
            public string LastTable { get; private set; }
            public string LastColumn { get; private set; }

            public List<ForeignKeyInfo> GetForeignKeys(string tableName) { LastTable = tableName; return new List<ForeignKeyInfo>(); }
            public DataTable GetForeignKeyLookup(string tableName, string fkColumn) { LastTable = tableName; LastColumn = fkColumn; return new DataTable(); }
            public List<LookupItem> GetForeignKeyLookupItems(string tableName, string fkColumn) { LastTable = tableName; LastColumn = fkColumn; return new List<LookupItem>(); }
            public string ResolveDisplayColumn(string tableName, string idColumn = "ID") { LastTable = tableName; return null; }
            public TableEditMetadata GetTableEditMetadata(string tableName) { LastTable = tableName; return null; }
        }

        private class FakeVirtualTables : IVirtualTableRegistry
        {
            public string LastName { get; private set; }
            public string LastQuery { get; private set; }

            public void Register(string name, string sql, PermissionFlags required = PermissionFlags.Read) { LastName = name; LastQuery = sql; }
            public void Register(string name, string sql, IDictionary<string, PermissionFlags> rolePermissions) { LastName = name; LastQuery = sql; }
            public void Remove(string name) { }
            public void Clear() { }
            public List<string> GetNames() => new List<string>();
            public bool TryGetQuery(string name, out string sql, out IDictionary<string, PermissionFlags> permissions)
            {
                LastName = name;
                sql = null;
                permissions = null;
                return false;
            }
            public DataTable GetData(string name, string roleName = null, PermissionFlags required = PermissionFlags.Read) { LastName = name; return null; }
        }

        #endregion
    }
}
