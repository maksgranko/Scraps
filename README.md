# Scraps

Scraps — небольшая библиотека‑помощник. Здесь собраны повторно используемые методы для работы с БД, правами, переводами, авторизацией/сессией, импортом и экспортом.

## Важно про безопасность

Это учебная библиотека. Для реальных проектов **не рекомендуется**, потому что многие решения упрощены (например, авто‑поиск SQL Server, хранение паролей, прямой SQL и т.п.).

## Структура

- `Scraps/Configs`
  - `ScrapsConfig.cs` — глобальные настройки (имя БД, строка подключения, таблица пользователей и т.д.)
- `Scraps/Databases`
  - `MSSQL/` — работа с SQL Server, генерация схемы, users/roles/permissions
  - `Utilities/` — общие утилиты (TableCatalog, DatabaseGenerationOptions)
  - `VirtualTableRegistry.cs` — виртуальные таблицы (имя → SQL)
- `Scraps/Security`
  - `RoleManager.cs` — права, роли, проверка доступа
  - `UserSession.cs` — авторизация/сессия + утилиты паролей
- `Scraps/Localization`
  - `TranslationManager.cs` — переводы названий таблиц/колонок
- `Scraps/Import`
  - `DataImportService.cs` — импорт Excel/CSV + проверки
- `Scraps/Export`
  - `ReportDataBuilder.cs`, `ReportExporter.cs` — экспорт Excel/PDF
- `Scraps/Data`
  - `DataTable/` — работа с DataTable (поиск, парс)

## Быстрый старт

```csharp
// 1) Настройки
ScrapsConfig.DatabaseName = "MyDb";
ScrapsConfig.ConnectionString = MSSQL.ConnectionStringBuilder();

// 2) Создать базовую схему (Users/Roles/RolePermissions), если нет
MSSQL.Initialize(databaseName: "MyDb", mode: DatabaseGenerationMode.Full);
// режим None: только создать БД, без таблиц
// MSSQL.Initialize(databaseName: "MyDb", mode: DatabaseGenerationMode.None);

// 3) Подгрузить роли/права из БД
RoleManager.InitializeFromDb();

// 4) Вход и сессия
UserSession.Login("admin", "Password123!");
var role = UserSession.UserRole;
```

## Поддерживаемые платформы

- Библиотека: `netstandard2.0`
- Тесты: `net48` (есть WinForms/STA‑тесты)

## Права (PermissionFlags)

Если нужно несколько прав сразу — используем `|` (побитовое ИЛИ):

```csharp
var flags = PermissionFlags.Read | PermissionFlags.Export;
```

Все права сразу:

```csharp
var all = PermissionFlags.Read | PermissionFlags.Write | PermissionFlags.Delete | PermissionFlags.Export | PermissionFlags.Import;
```

Так работает везде, где можно указывать права.

## Виртуальные таблицы (только SELECT)

Виртуальная таблица — это просто SQL‑запрос. Она **только для чтения**.

```csharp
VirtualTableRegistry.Register(
    name: "Virtual_Sales",
    sql: "SELECT ...",
    rolePermissions: new Dictionary<string, PermissionFlags>
    {
        ["Администратор"] = PermissionFlags.Read,
        ["Менеджер"] = PermissionFlags.Read,
        ["*"] = PermissionFlags.None // всем остальным запрещено
    }
);

var data = VirtualTableRegistry.GetData("Virtual_Sales", roleName: "Администратор");
```

## Список таблиц (TableCatalog)

```csharp
var tables = TableCatalog.InitializeTablesWithRegistry(
    autodetect: true,
    manualTables: null,
    removeOnAutodetect: new[] { "sysdiagrams" }
);
```

## Импорт

```csharp
var dt = DataImportService.LoadExcelToDataTable("input.xlsx");

// Безопасный импорт с проверками и правами
DataImportService.ImportToTableSafe(
    tableName: "Users",
    importData: dt,
    roleName: UserSession.UserRole
);
```

## Парс DataTable из строки

```csharp
var input = "Name,Age\nIvan,20";
var dt = DataTableParser.ParseDelimited(input, delimiter: ',', hasHeader: true);
```

## Экспорт

```csharp
var dt = ReportDataBuilder.GetTableTranslated("Users");
ReportExporter.ExportToExcel(dt, "users.xlsx");
ReportExporter.ExportToPdf(dt, "users.pdf");
```

## Конфигурация (важные поля)

- `ScrapsConfig.DatabaseName` — имя БД.
- `ScrapsConfig.ConnectionString` — строка подключения (если не задана, можно попробовать `MSSQL.ConnectionStringBuilder()`).
- `ScrapsConfig.UseRoleIdMapping` — роль как `RoleID` (Standard/Full) или строка (Simple/None).
- `ScrapsConfig.AuthHashPasswords` и `AuthHashAlgorithm` — хэширование паролей.

## Тесты

Тесты делятся на:

- DB‑тесты (нужен доступный SQL Server, права на создание БД).
- UI‑тесты (WinForms, STA).

Запуск:

```bash
dotnet test Scraps.Tests/Scraps.Tests.csproj
```

Если SQL Server недоступен, DB‑тесты будут автоматически пропущены.

## Полезные заметки

- `UserSession.Utilities` — проверка пароля и хэши.
- `TranslationManager` меняет названия колонок прямо в `DataTable`.
- В `VirtualTableRegistry` можно задавать права по ролям, есть правило `*` (для всех).
