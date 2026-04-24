# PLAN.MD — Рефакторинг архитектуры Scraps

## Цель

Разделить монолитную зависимость от `MSSQL.*` на модульную архитектуру:
- `Scraps.Database` — единый интерфейс (`IDatabase*`) и фасад `Database.*`
- `Scraps.Database.MSSQL` — реализация MSSQL + legacy static API `MSSQL.*` (без изменений для backward compatibility)
- Все остальные проекты работают через `Scraps.Database`, не зависят от конкретной БД

---

## Итоговая структура модулей

### Scraps.Core
```
Scraps.Core/
  Configs/
    ScrapsConfig.cs          (+ DatabaseProvider enum, LocalDataPath)
    DatabaseGenerationMode.cs
    DatabaseGenerationOptions.cs
  Data/
    DataTables/
      Parser.cs
      Search.cs
    Parsers/
      Csv.cs
      DelimitedTable.cs
      Nx.cs
      Internal/
        DataTableParserHelpers.cs
  Diagnostics/
    ScrapsLog.cs
  Localization/
    TranslationManager.cs
  Security/
    PermissionModel.cs         (Role, TablePermission, PermissionFlags)
```
**Зависимости:** нет

---

### Scraps.Database
```
Scraps.Database/
  Scraps.Database.csproj
  DatabaseProviderFactory.cs   (фабрика провайдеров)
  Database.cs                  (статический фасад)
  DatabaseBase.cs              (абстрактный базовый класс)
  Interfaces/
    IDatabase.cs
    IDatabaseConnection.cs
    IDatabaseSchema.cs
    IDatabaseData.cs
    IDatabaseUsers.cs
    IDatabaseRoles.cs
    IDatabaseRolePermissions.cs
    IRowEditor.cs
    IVirtualTableRegistry.cs
    IForeignKeyProvider.cs
  Models/
    ForeignKeyJoin.cs
    ForeignKeyInfo.cs
    LookupItem.cs
    TableEditColumnMetadata.cs
    TableEditMetadata.cs
    SqlFilterOperator.cs
    SqlFilterCondition.cs
    SqlSortCondition.cs
    ForeignKeyQueryOptions.cs
    ExpandForeignKeysOptions.cs
    RoleInfo.cs
    RolePermissionInfo.cs
    ColumnInfo.cs
    AddEditResult.cs
    ChildInsert.cs
    DatabaseGenerationMode.cs
    DatabaseGenerationOptions.cs
  Local/
    LocalDatabase.cs           (заглушка — throw NotImplementedException)
```
**Зависимости:** Scraps.Core

---

### Scraps.Database.MSSQL
```
Scraps.Database.MSSQL/
  Scraps.Database.MSSQL.csproj
  MSSQLDatabase.cs             (реализация DatabaseBase)
  Databases/
    MSSQL/
      Core.cs                  (legacy partial class MSSQL — без изменений)
      Data.cs                  (legacy partial class MSSQL — без изменений)
      ForeignKeyApi.cs         (legacy partial class MSSQL — без изменений)
      Generation.cs            (legacy partial class MSSQL — без изменений)
      RolePermissions.cs       (legacy partial class MSSQL — без изменений)
      Roles.cs                 (legacy partial class MSSQL — без изменений)
      Schema.cs                (legacy partial class MSSQL — без изменений)
      Users.cs                 (legacy partial class MSSQL — без изменений)
    VirtualTableRegistry.cs    (legacy static class — без изменений)
    Utilities/
      TableCatalog.cs          (legacy static class — без изменений)
```
**Зависимости:** Scraps.Core, Scraps.Database, System.Data.SqlClient

---

### Scraps.Security
```
Scraps.Security/
  Scraps.Security.csproj
  Security/
    RoleManager.cs             (перенесён из Scraps.Database.MSSQL/Security/)
    UserSession.cs             (обновлён — через DatabaseProviderFactory)
```
**Зависимости:** Scraps.Core, Scraps.Database

---

### Scraps.Import
```
Scraps.Import/
  Scraps.Import.csproj
  Import/
    DataImportService.cs       (обновлён — через Database.* вместо MSSQL.*)
```
**Зависимости:** Scraps.Core, Scraps.Database

---

### Scraps.Export
```
Scraps.Export/
  Scraps.Export.csproj
  Export/
    ReportDataBuilder.cs       (обновлён — через Database.* вместо MSSQL.*)
```
**Зависимости:** Scraps.Core, Scraps.Database

---

### Scraps.UI.WinForms
```
Scraps.UI.WinForms/
  Scraps.UI.WinForms.csproj
  UI/
    WinForms/
      FKEditors.cs             (обновлён — через Database.* вместо MSSQL.*)
```
**Зависимости:** Scraps.Core, Scraps.Database

---

### Scraps.Tests
```
Scraps.Tests/
  Scraps.Tests.csproj
  RoleManagerTests.cs          (без изменений — работает через RoleManager)
```
**Зависимости:** Scraps.Core, Scraps.Database, Scraps.Database.MSSQL, Scraps.Security

---

### Scraps (meta-package)
```
Scraps/
  Scraps.csproj
```
**Зависимости:** Scraps.Core, Scraps.Database, Scraps.Database.MSSQL, Scraps.Security, Scraps.Import, Scraps.Export

---

## Граф зависимостей

```
                    ┌─► Scraps.Database.MSSQL ◄────┐
                    │         (реализация)          │
                    │                               │
Scraps.Core ◄───────┼─► Scraps.Database ◄───────────┤
                    │      (интерфейсы)             │
                    │                               │
                    ├─► Scraps.Security ◄───────────┘
                    │      (RoleManager, UserSession)
                    │
                    ├─► Scraps.Import
                    │
                    ├─► Scraps.Export
                    │
                    └─► Scraps.UI.WinForms
```

---

## Пошаговый план выполнения

### Шаг 1. Создать проект `Scraps.Database`
- Создать `Scraps.Database/Scraps.Database.csproj` (`netstandard2.0`)
- Добавить `ProjectReference` на `Scraps.Core`
- Добавить проект в `Scraps.sln`

### Шаг 2. Вынести DTO-модели в `Scraps.Database`
Перенести из `Scraps.Database.MSSQL/Databases/MSSQL/*.cs`:
- `ForeignKeyJoin` → `Models/ForeignKeyJoin.cs`
- `ForeignKeyInfo`, `LookupItem`, `TableEditColumnMetadata`, `TableEditMetadata`, `SqlFilterOperator`, `SqlFilterCondition`, `SqlSortCondition`, `ForeignKeyQueryOptions`, `ExpandForeignKeysOptions` → `Models/`
- `RoleInfo` → `Models/RoleInfo.cs`
- `RolePermissionInfo` → `Models/RolePermissionInfo.cs`
- `DatabaseGenerationMode`, `DatabaseGenerationOptions` → `Models/`
- Новый: `ColumnInfo`, `AddEditResult`, `ChildInsert` → `Models/`

Namespace: `Scraps.Database`

### Шаг 3. Добавить `DatabaseProvider` в `ScrapsConfig`
В `Scraps.Core/Configs/ScrapsConfig.cs`:
```csharp
public enum DatabaseProvider 
{ 
    None, 
    LocalFiles, 
    MSSQL, 
    MySQL, 
    PostgreSQL, 
    SQLite 
}

public static DatabaseProvider DatabaseProvider = DatabaseProvider.None;
public static string LocalDataPath = "Data";
```

### Шаг 4. Создать интерфейсы
Файлы в `Scraps.Database/Interfaces/`:
- `IDatabase.cs` — агрегирует все остальные + TestConnection + Initialize
- `IDatabaseConnection.cs` — ConnectionString, ExecuteNonQuery, ExecuteScalar, GetDataTable, TestConnection
- `IDatabaseSchema.cs` — GetTables, GetTableColumns, GetTableSchema, IsIdentityColumn, IsNullableColumn
- `IDatabaseData.cs` — GetTableData, GetTableDataExpanded, FindByColumn, ApplyTableChanges, BulkInsert, GetNx2Dictionary, GetNx1List
- `IDatabaseUsers.cs` — GetByLogin, GetUserStatus, Create, Delete, ChangePassword, ChangeRole
- `IDatabaseRoles.cs` — GetRoleNameById, GetRoleIdByName, Create, Delete, Rename, GetAll
- `IDatabaseRolePermissions.cs` — GetAll, GetByRoleName, GetByRoleId, Set, Delete, DeleteAllForRole
- `IRowEditor.cs` — AddRow, UpdateRow
- `IVirtualTableRegistry.cs` — Register, Remove, Clear, GetNames, TryGetQuery, GetData
- `IForeignKeyProvider.cs` — GetForeignKeys, GetForeignKeyLookup, GetForeignKeyLookupItems, ResolveDisplayColumn, GetTableEditMetadata

### Шаг 5. Создать `DatabaseBase`
Файл: `Scraps.Database/DatabaseBase.cs`
- Абстрактный класс, реализует `IDatabase`
- Все свойства virtual
- `TestConnection()` → через `Connection`
- `Initialize()` → пустой или виртуальный

### Шаг 6. Создать `Database` фасад
Файл: `Scraps.Database/Database.cs`
- Статический класс
- `private static IDatabase Current => DatabaseProviderFactory.Current`
- Проксирует все методы: `Database.GetTableData(...)`, `Database.Users.GetByLogin(...)`, и т.д.

### Шаг 7. Создать `DatabaseProviderFactory`
Файл: `Scraps.Database/DatabaseProviderFactory.cs`
- `Register(DatabaseProvider, Func<IDatabase>)`
- `Create(DatabaseProvider)`
- `Current` property — lazy init из `ScrapsConfig.DatabaseProvider`
- `Reset()`
- Static constructor — регистрирует `LocalDatabase` для `LocalFiles`

### Шаг 8. Обновить `Scraps.Database.MSSQL`
- Добавить `ProjectReference` на `Scraps.Database`
- Создать `MSSQLDatabase.cs` — `class MSSQLDatabase : DatabaseBase`
- Внутри `MSSQLDatabase` — адаптеры для каждого `IDatabase*` интерфейса
- Legacy `partial class MSSQL` в 8 файлах — **без изменений**
- Зарегистрировать `MSSQLDatabase` в фабрике (static constructor или инициализация)

### Шаг 9. Перенести `RoleManager` в `Scraps.Security`
- Переместить файл: `Scraps.Database.MSSQL/Security/RoleManager.cs` → `Scraps.Security/Security/RoleManager.cs`
- Заменить все `MSSQL.Roles.*` / `MSSQL.RolePermissions.*` → `DatabaseProviderFactory.Current.Roles.*` / `DatabaseProviderFactory.Current.RolePermissions.*`
- Убрать `using Scraps.Databases;`
- Обновить `Scraps.Security.csproj` — заменить `Scraps.Database.MSSQL` на `Scraps.Database`

### Шаг 10. Обновить `UserSession`
- Заменить `MSSQL.Users.*` → `DatabaseProviderFactory.Current.Users.*`
- Убрать `using Scraps.Databases;`

### Шаг 11. Мигрировать `Scraps.Import`, `Scraps.Export`, `Scraps.UI.WinForms`

**Scraps.Import/Import/DataImportService.cs:**
- `MSSQL.GetTableSchema(tableName)` → `Database.Schema.GetTableSchema(tableName)` (возвращает DataTable — адаптировать)
- `MSSQL.BulkInsert(...)` → `Database.BulkInsert(...)`
- Убрать `using Scraps.Databases;`

**Scraps.UI.WinForms/UI/WinForms/FKEditors.cs:**
- `MSSQL.GetTableData(...)` → `Database.GetTableData(...)`
- `MSSQL.ResolveDisplayColumn(...)` → `Database.ResolveDisplayColumn(...)`
- Убрать `using Scraps.Databases;`

**Scraps.Export/Export/ReportDataBuilder.cs:**
- `MSSQL.GetDataTableFromSQL(sql)` → `Database.GetDataTable(sql)`
- Убрать `using Scraps.Databases;`

**Общее:**
- Добавить `ProjectReference` на `Scraps.Database`
- Убрать `ProjectReference` на `Scraps.Database.MSSQL` (если был)

### Шаг 12. Обновить Solution
- Добавить `Scraps.Database.csproj` в `Scraps.sln`
- Обновить `.csproj` зависимости

### Шаг 13. Подготовить `LocalDatabase` заглушку
- `Scraps.Database/Local/LocalDatabase.cs` — `class LocalDatabase : DatabaseBase`
- Все методы — `throw new NotImplementedException("LocalDatabase ещё не реализован")`
- Зарегистрирован в `DatabaseProviderFactory`

### Шаг 14. Чистая сборка
- Удалить все `bin/` и `obj/`
- Собрать `Scraps.sln` в Release
- Проверить отсутствие ошибок

---

## Критические архитектурные решения

| Вопрос | Решение |
|--------|---------|
| `RoleManager`/`UserSession` проверяют `UseRoleIdMapping`? | **Нет.** Всегда через `DatabaseProviderFactory.Current` |
| Локальный режим (`LocalFiles`) | Заглушка `throw NotImplementedException`, JSON на потом |
| Базовый класс | `DatabaseBase : IDatabase` в `Scraps.Database` |
| `GetTableSchema` | Возвращает `DataTable` |
| Legacy `MSSQL.*` API | **Не трогаем**, оставляем в `namespace Scraps.Databases` |
| DTO-модели | Вынесены в `Scraps.Database`, namespace `Scraps.Database` |
| Фабрика провайдеров | `DatabaseProviderFactory` — static, с `Register` и `Current` |

---

## Примеры использования после рефакторинга

### Новый универсальный API:
```csharp
ScrapsConfig.DatabaseProvider = DatabaseProvider.MSSQL;
ScrapsConfig.ConnectionString = "...";

var data = Database.GetTableData("Users");        // работает с любой БД
Database.Users.Create("ivan", "pass", "admin");   // универсально
```

### Legacy API (MSSQL-only, сохраняется):
```csharp
var data = MSSQL.GetTableData("Users");           // всё ещё работает
MSSQL.Users.Create("ivan", "pass", "admin");      // для старых проектов
```

### Регистрация нового провайдера:
```csharp
DatabaseProviderFactory.Register(DatabaseProvider.MySQL, () => new MySQLDatabase());
```
