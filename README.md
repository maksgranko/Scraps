# Scraps

Scraps - модульная .NET-библиотека для работы с данными: провайдеры БД (MSSQL и LocalFiles), роли/права, импорт/экспорт, локализация, утилиты `DataTable` и WinForms-хелперы.

## Актуальная структура

Каждый модуль - отдельный пакет/проект:

| Пакет | Назначение |
|---|---|
| `Scraps.Core` | конфигурация, локализация, `DataTable`-утилиты, модели безопасности (`Scraps.Security`) |
| `Scraps.Database` | общие интерфейсы БД + фасад `Scraps.Database.Current` |
| `Scraps.Database.MSSQL` | SQL Server провайдер + MSSQL-утилиты |
| `Scraps.Database.LocalFiles` | локальный JSON/file-based провайдер |
| `Scraps.Import` | импорт из Excel/CSV |
| `Scraps.Export` | экспорт в Excel/PDF |
| `Scraps.UI.WinForms` | расширения для `DataGridView` (net472) |
| `Scraps` | мета-пакет, подтягивает все ключевые модули |

Важно:
- отдельного пакета `Scraps.Security` больше нет;
- namespace `Scraps.Security` используется и доступен через `Scraps.Core`/`Scraps.Database`.

## Быстрый старт (единый фасад `Current`)

```csharp
using Scraps.Configs;
using Scraps.Database;

// 1) Выбор провайдера
ScrapsConfig.DatabaseProvider = DatabaseProvider.MSSQL;

// 2) Настройки подключения
ScrapsConfig.DatabaseName = "MyDb";
ScrapsConfig.ConnectionString = Current.ConnectionStringBuilder("MyDb");
// или вручную: "Server=.\\SQLEXPRESS;Database=MyDb;Trusted_Connection=True;"

// 3) Работа через единый фасад
bool ok = Current.TestConnection();
var tables = Current.GetTables();
var users = Current.GetTableData("Users");
```

`DatabaseProviderFactory` автоматически подхватывает нужный провайдер по `ScrapsConfig.DatabaseProvider`.

Примечание: в `Current` есть `ConnectionStringBuilder(...)`.
- для MSSQL параметр - имя БД или готовая connection string;
- для LocalFiles параметр - путь к папке данных (или `ScrapsConfig.LocalDataPath`, если параметр не передан).

## Основные API

### `Current` (Scraps.Database)

- `Current.Connection`, `Current.Schema`, `Current.Data`, `Current.Users`, `Current.Roles`, `Current.RolePermissions`
- `Current.GetTables(includeSystem: false)`
- `Current.GetTableData(...)`, `Current.GetTableDataExpanded(...)`
- `Current.FindByColumn(table, column, value, SqlFilterOperator)`
- `Current.ApplyTableChanges(...)`, `Current.BulkInsert(...)`
- `Current.AddRow(...)`, `Current.UpdateRow(...)`
- `Current.VirtualTables.*`, `Current.GetForeignKeys(...)`, `Current.GetForeignKeyLookup(...)`

### Фильтрация (`SqlFilterOperator`)

Вместо `exactMatch` используется оператор:

```csharp
using Scraps.Database;

var exact = Current.FindByColumn("Users", "Login", "admin", SqlFilterOperator.Eq);
var like = Current.FindByColumn("Users", "Login", "adm", SqlFilterOperator.Like);
var nulls = Current.FindByColumn("Users", "MiddleName", null, SqlFilterOperator.IsNull);
```

### Роли и права

`RoleManager` удален. Используйте API провайдера:

```csharp
using Scraps.Database;
using Scraps.Security;

bool canRead = Current.Roles.CheckAccess("Admin", "Users", PermissionFlags.Read);
var effective = Current.Roles.GetEffectivePermissions("Admin", "Users");
```

Сессия пользователя:

```csharp
using Scraps.Security;

UserSession.Login("admin", "Password123!");
```

## MSSQL-утилиты

Модуль `Scraps.Database.MSSQL` также содержит привычный статический API `MSSQL` (инициализация, генерация схемы, FK helpers, и т.д.) для сценариев, где нужен провайдер-специфичный функционал.

## Import/Export

### Import (`Scraps.Import`)

- `DataImportService.LoadExcelToDataTable(...)`
- `DataImportService.LoadCsvToDataTable(...)`
- `DataImportService.ValidateColumns/ValidateTypes/...`
- `DataImportService.ImportToTable(...)`

### Export (`Scraps.Export`)

- `ReportExporter` (Excel/PDF)
- `ReportDataBuilder`

## UI.WinForms (`Scraps.UI.WinForms`)

Модуль для `net472`:
- расширения для выделения/фильтрации/поиска в `DataGridView`;
- FK ComboBox-колонки и связанные хелперы.

## Сборка, упаковка и публикация

Из корня репозитория:

```bash
./pack.sh
./publish-nuget.sh
```

Windows:

```bat
pack.bat
publish-nuget.bat
```

Что делает `pack`:
- `dotnet clean` -> `dotnet build` -> `dotnet pack`
- складывает `.nupkg` в папку `NuGet/`

`publish-nuget`:
- публикует все пакеты из `NuGet/` в NuGet.org;
- использует `--skip-duplicate` и выводит счетчик успешных/неуспешных публикаций.

## Тесты

```bash
dotnet test Scraps.Tests/Scraps.Tests.csproj
```

Замечания:
- `Scraps.Tests` таргетит `net472`;
- часть тестов зависит от Windows Forms и/или доступного SQL Server;
- в Linux/CI без нужного окружения такие тесты могут быть недоступны.

## Совместимость

- `Scraps.Core`, `Scraps.Database`, `Scraps.Database.LocalFiles`: `net45`, `net472`, `netstandard2.0`
- `Scraps.Database.MSSQL`: `net451`, `net472`, `netstandard2.0`
- импорт/экспорт и мета-пакет (`Import`, `Export`, `Scraps`): `net472`, `netstandard2.0`
- WinForms и тесты: `net472`

## Примечание

Проект учебный. Перед production-использованием рекомендуется аудит безопасности, SQL-политик и архитектурных ограничений.
