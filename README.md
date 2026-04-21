# Scraps

Scraps — библиотека утилит для SQL Server, прав доступа, локализации, импорта/экспорта и операций с `DataTable`.

## Важно

Библиотека учебная. Для production-проектов используйте её с аудитом безопасности и архитектуры.

## Структура

- `Scraps/Configs/ScrapsConfig.cs`
  - Глобальная конфигурация: БД, строка подключения, параметры auth/roles.
- `Scraps/Databases/MSSQL/*`
  - Подключение, генерация схемы, CRUD-утилиты, схема таблиц, роли/права.
- `Scraps/Databases/Utilities/*`
  - `TableCatalog`, `DatabaseGenerationOptions`.
- `Scraps/Databases/VirtualTableRegistry.cs`
  - Реестр виртуальных таблиц и проверка доступа.
- `Scraps/Security/*`
  - `RoleManager`, `UserSession`.
- `Scraps/Localization/TranslationManager.cs`
  - Переводы таблиц и колонок.
- `Scraps/Import/DataImportService.cs`
  - Импорт Excel/CSV, валидации и запись в БД.
- `Scraps/Export/*`
  - Построение данных отчёта и экспорт в Excel/PDF.
- `Scraps/Data/DataTables/*`
  - `Parser` и `Search` для работы с `DataTable`.

## Быстрый старт

```csharp
using Scraps.Configs;
using Scraps.Databases;
using Scraps.Databases.Utilities;
using Scraps.Security;

ScrapsConfig.DatabaseName = "MyDb";
ScrapsConfig.ConnectionString = MSSQL.ConnectionStringBuilder("MyDb");

// None / Simple / Standard / Full
MSSQL.Initialize("MyDb", DatabaseGenerationMode.Full);

RoleManager.InitializeFromDb();
UserSession.Login("admin", "Password123!");
```

## Генерация БД

`DatabaseGenerationMode`:

- `None` — создаётся только БД, без таблиц.
- `Simple` — таблица пользователей, роль хранится строкой.
- `Standard` — `Users` + `Roles`, роль хранится как `RoleID`.
- `Full` — `Users` + `Roles` + `RolePermissions`.

Пример:

```csharp
MSSQL.GenerateIfNotExists(new DatabaseGenerationOptions
{
    DatabaseName = "MyDb",
    Mode = DatabaseGenerationMode.Standard,
    DefaultRoleName = "default",
    SeedRoles = new[] { "Teacher", "Manager" }
});
```

## Основные API

`MSSQL`:

- `ConnectionStringBuilder(...)` — сборка/подбор строки подключения.
- `Initialize(...)` / `GenerateIfNotExists(...)` — подготовка БД.
- `GetTables(...)`, `GetTableColumns(...)`, `GetTableSchema(...)` — чтение схемы.
- `GetTableData(...)`, `FindByColumn(...)`, `ApplyTableChanges(...)`, `BulkInsert(...)`.
- `GetTableData(..., IEnumerable<MSSQL.ForeignKeyJoin>, params string[] baseColumns)` — выборка с `LEFT JOIN` по FK + выбор колонок.
- `GetNx2Dictionary(...)` — прочитать таблицу формата `Nx2` (2 колонки) из БД сразу в `Dictionary`.
- `GetNx1List(...)` — прочитать таблицу формата `Nx1` (1 колонка) из БД в `List<string>`.
- `Roles.*` и `Users.*` — операции с пользователями и ролями.

`VirtualTableRegistry`:

- `Register(...)`, `RegisterSelect(...)`, `GetData(...)`, `CheckAccess(...)`, `Clear()`.
- Пустая роль запрещена: передавайте валидное `roleName`.

`DataImportService`:

- `LoadExcelToDataTable(...)`, `LoadCsvToDataTable(...)`.
- `ValidateColumns(...)`, `ValidateColumnCount(...)`, `ValidateTypes(...)`.
- `ImportToTable(...)`, `ImportToTableSafe(...)`.

`Scraps.Data.DataTables`:

- `Parser.ParseDelimited(...)` — парсинг строки в `DataTable`.
- `Parser.ParseNx2ToDictionary(...)` — парсинг таблицы формата `Nx2` в `Dictionary`.
- `Parser.ParseNx1ToList(...)` — парсинг моно-таблицы `Nx1` в `List<string>`.
- `Search.FindMatches(...)`, `Search.FilterRows(...)`, `Search.CreateNavigator(...)`.
- `Search.GetMatchNavigatorHelp()` — встроенная справка по использованию навигатора в UI (в т.ч. `DataGridView`).

Пример FK-выборки:

```csharp
var data = MSSQL.GetTableData(
    tableName: "Users",
    foreignKeys: new[]
    {
        new MSSQL.ForeignKeyJoin("Role", "Roles", "RoleID", "RoleName")
        {
            AliasPrefix = "Role"
        }
    },
    baseColumns: new[] { "UserID", "Login", "Role" });
```

Пример FK-выборки с автоматическим раскрытием всех FK:

```csharp
var expanded = MSSQL.GetTableData("Users", expandForeignKeys: true);
// Добавляются alias-колонки вида: t0_Role_RoleName

var withOptions = MSSQL.GetTableData("Users", expandForeignKeys: true,
    new MSSQL.ExpandForeignKeysOptions { AutoResolveDisplayColumn = false },
    "Login", "Role");
```

## Add/Edit с автоматическим разрешением FK (TableRows)

```csharp
using Scraps.Databases.Utilities.TableRows;
```

### Упрощённое создание значений

```csharp
// Вариант 1 — через Values.Create (короткий)
var values = Values.Create("Name", "Иван", "Client_ID", "Пётр");

// Вариант 2 — стандартный Dictionary (если нужна сложная логика)
var values = new Dictionary<string, object>
{
    ["Name"] = "Иван",
    ["Client_ID"] = "Пётр"
};
```

### Добавление записи (AddRow)

**Строгий режим** (`strictFk = true`) — справочник должен существовать:

```csharp
var result = RowEditor.AddRow("Users",
    Values.Create("Login", "ivan", "Password", "pass", "Role", "Admin"),
    strictFk: true);

// result.Success = true  — роль "Admin" найдена
// result.Success = false — роль не найдена, result.Error содержит описание
```

**Мягкий режим** (`strictFk = false`) — авто-создание в справочнике:

```csharp
// Роль "Moderator" не существует → будет создана автоматически
var result = RowEditor.AddRow("Users",
    Values.Create("Login", "petr", "Password", "pass", "Role", "Moderator"),
    strictFk: false);

// result.Success = true
// result.RowId = ID нового пользователя
// В таблице Roles появилась запись "Moderator"
```

### Редактирование записи (UpdateRow)

```csharp
var result = RowEditor.UpdateRow("Users", "Login", "ivan",
    Values.Create("Role", "SuperAdmin"),
    strictFk: false);
```

### Связанные таблицы (ChildInsert)

Добавление в родительскую таблицу + автоматическая вставка в дочерние:

```csharp
var result = RowEditor.AddRow("Groups",
    Values.Create("Name", "Администраторы", "Client_ID", "Пётр"),
    strictFk: false,
    children: new[]
    {
        // GroupID подставится автоматически из только что созданной Groups
        new ChildInsert("GroupClients", Values.Create("Note", "VIP-клиент"))
    });

// Результат:
// Clients:    создан "Пётр" (если не существовал)
// Groups:     создана группа "Администраторы"
// GroupClients: создана связь с Note = "VIP-клиент"
```

### Поведение Foreign Key

| Входное значение | `strictFk = true` | `strictFk = false` |
|------------------|-------------------|--------------------|
| `null` | `NULL` | `NULL` |
| `int` (существует) | Использовать | Использовать |
| `int` (не существует) | **Ошибка** | **Ошибка** |
| `string` (найден) | Использовать ID | Использовать ID |
| `string` (не найден) | **Ошибка** | **Авто-INSERT в справочник** |
| Тип не совпадает с DisplayColumn | **Ошибка** | **Ошибка** |
| `""` (пустая строка) | `NULL` | `NULL` |

**Авто-создание справочника** работает только для таблиц Nx1 (1 колонка) или Nx2 (`ID + поле`). Если таблица имеет больше колонок — будет ошибка.

Пример `Nx2` -> `Dictionary`:

```csharp
string text = "1 Отлично\n2 Хорошо\n3 Плохо\n4 Неизвестно";
Dictionary<int, string> grades = Parser.ParseNx2ToDictionary(text);
```

Пример `Nx2` с кастомными разделителями:

```csharp
string text = "1=>Отлично|2=>Хорошо|3=>Плохо";
Dictionary<int, string> grades = Parser.ParseNx2ToDictionary(
    text,
    columnSeparator: "=>",
    rowSeparator: "|");
```

Пример `Nx2` из таблицы БД:

```csharp
Dictionary<int, string> grades = MSSQL.GetNx2Dictionary(
    tableName: "GradeCatalog",
    keyColumn: "GradeID",
    valueColumn: "GradeName");
```

Пример `Nx1` (текст и таблица БД):

```csharp
List<string> names = Parser.ParseNx1ToList("Антон\nАндрей\nВасилий");

List<string> dbNames = MSSQL.GetNx1List(
    tableName: "NameCatalog",
    valueColumn: "Name");
```

Справка по `MatchNavigator`:

```csharp
string help = Search.GetMatchNavigatorHelp();
```

## Права доступа

```csharp
var flags = PermissionFlags.Read | PermissionFlags.Export;
```

Проверка для виртуальной таблицы:

```csharp
VirtualTableRegistry.Register(
    "Virtual_Sales",
    "SELECT * FROM [Sales]",
    new Dictionary<string, PermissionFlags>
    {
        ["Admin"] = PermissionFlags.Read | PermissionFlags.Export,
        ["*"] = PermissionFlags.None
    });

var data = VirtualTableRegistry.GetData("Virtual_Sales", roleName: "Admin");
```

## Локализация

```csharp
TranslationManager.TableTranslations["Users"] = "Пользователи";
TranslationManager.ColumnTranslations["Users"] = new Dictionary<string, string>
{
    ["Login"] = "Логин",
    ["Password"] = "Пароль"
};
```

## Тесты

- `Scraps.Tests` (`net48`) содержит DB/UI/unit тесты.
- DB-тесты требуют доступного SQL Server и прав на создание/удаление тестовых БД.
- При неготовом окружении DB-тесты сейчас **падают ошибкой**, а не пропускаются.

Запуск:

```bash
dotnet test Scraps.Tests/Scraps.Tests.csproj
```

## Совместимость

- Библиотека: `netstandard2.0`
- Тесты: `net48`

## Примечания

- Документация по параметрам и исключениям также доступна через XML-doc комментарии в публичном API.
