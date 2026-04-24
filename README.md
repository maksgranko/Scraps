# Scraps

Scraps — библиотека утилит для SQL Server, прав доступа, локализации, импорта/экспорта и операций с `DataTable`.

## Важно

Библиотека учебная. Для production-проектов используйте её с аудитом безопасности и архитектуры.

## Структура (модульная)

Scraps разделён на подбиблиотеки (каждая — отдельный NuGet-пакет):

| Пакет | Содержимое |
|-------|-----------|
| **Scraps.Core** | `Configs`, `Data.DataTables`, `Diagnostics`, `Localization` |
| **Scraps.Database.LocalFiles** | `LocalFiles` JSON-провайдер + локальный SQL-эмулятор |
| **Scraps.Database.MSSQL** | `Databases.MSSQL`, `Databases.Utilities`, `VirtualTableRegistry` |
| **Scraps.Import** | `DataImportService` (Excel/CSV) |
| **Scraps.Export** | `ReportExporter`, `ReportDataBuilder` (Excel/PDF) |
| **Scraps.UI.WinForms** | `DataGridViewHelpers`, `FKEditors` |
| **Scraps** (мета) | Зависит от всех пакетов выше для обратной совместимости |

`Scraps.Security` используется как namespace (например, `UserSession`, `PermissionFlags`), но отдельного пакета `Scraps.Security` больше нет.

В будущем планируются: `Scraps.Database.PostgreSQL` и др.

### Legacy-структура (до разделения)

- `Scraps/Configs/ScrapsConfig.cs` — глобальная конфигурация.
- `Scraps/Databases/MSSQL/*` — подключение, генерация схемы, CRUD.
- `Scraps/Databases/Utilities/*` — `TableCatalog`, `DatabaseGenerationOptions`, `TableRows`.
- `Scraps/Databases/VirtualTableRegistry.cs` — виртуальные таблицы.
- `Scraps.Database/Security/*` — `UserSession`.
- `Scraps/Localization/TranslationManager.cs` — переводы.
- `Scraps/Import/DataImportService.cs` — импорт.
- `Scraps/Export/*` — экспорт.
- `Scraps/Data/DataTables/*` — `Parser`, `Search`.

## Быстрый старт

```csharp
using Scraps.Configs;
using Scraps.Database.MSSQL;
using Scraps.Database.MSSQL.Utilities;
using Scraps.Security;

ScrapsConfig.DatabaseName = "MyDb";
ScrapsConfig.ConnectionString = MSSQL.ConnectionStringBuilder("MyDb");

// None / Simple / Standard / Full
MSSQL.Initialize("MyDb", DatabaseGenerationMode.Full);

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
using Scraps.Database.MSSQL.Utilities.TableRows;
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

### Обратное преобразование: Nx1/Nx2 → DataTable

```csharp
// Nx1: List → DataTable
var list = new List<string> { "Антон", "Андрей", "Василий" };
DataTable dt1 = Parser.FromNx1(list, columnName: "Name");

// Nx2: Dictionary → DataTable
var dict = new Dictionary<int, string>
{
    [1] = "Отлично",
    [2] = "Хорошо",
    [3] = "Плохо"
};
DataTable dt2 = Parser.FromNx2(dict, keyColumnName: "GradeID", valueColumnName: "GradeName");
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

## UI.WinForms (DataGridView)

```csharp
using Scraps.UI.WinForms;
```

### Выделение строк/ячеек

```csharp
// Получить выделенные строки (объекты)
var rows = dataGridView1.GetSelectedRows();

// Получить индексы выделенных строк
var rowIndices = dataGridView1.GetSelectedRowIndices();

// Выделить строку (добавляет к выделению по умолчанию)
dataGridView1.SelectRow(5);

// Выделить строку со сбросом предыдущего выделения
dataGridView1.SelectRow(5, clearOthers: true);

// Получить выделенные ячейки (объекты)
var cells = dataGridView1.GetSelectedCells();

// Получить индексы выделенных ячеек (Row, Column)
var cellIndices = dataGridView1.GetSelectedCellIndices();

// Выделить ячейку (добавляет к выделению по умолчанию)
dataGridView1.SelectCell(row: 3, column: 1);

// Выделить ячейку со сбросом предыдущего выделения
dataGridView1.SelectCell(row: 3, column: 1, clearOthers: true);
```

### Добавление/удаление из выделения

```csharp
// Добавить ячейку к выделению (не сбрасывая остальные)
dataGridView1.AddCellToSelection(2, 3);

// Снять выделение с конкретной ячейки
dataGridView1.DeselectCell(0, 0);

// Снять выделение со строки/столбца
dataGridView1.DeselectRow(1);
dataGridView1.DeselectColumn(2);

// Очистить всё выделение
dataGridView1.DeselectAll();
```

### Сохранение и восстановление выделения

```csharp
// Сохранить текущее выделение
var saved = dataGridView1.SaveSelection();

// ... выполняем операции, которые сбрасывают выделение (фильтрация, обновление и т.д.) ...

// Восстановить выделение
dataGridView1.RestoreSelection(saved);
```

### Работа со строками

```csharp
// Удалить строку
dataGridView1.DeleteRow(0);

// Удалить выделенные строки
dataGridView1.DeleteSelectedRows();

// Переместить строку вверх/вниз
dataGridView1.MoveRowUp(2);
dataGridView1.MoveRowDown(1);

// Дублировать строку
dataGridView1.DuplicateRow(0);
```

### Работа с ячейками

```csharp
// Установить значение ячейки
dataGridView1.SetCellValue(0, 1, "Новое значение");

// Получить значение ячейки
var value = dataGridView1.GetCellValue(0, 1);
```

### Фильтрация

```csharp
// Поиск по всем текстовым колонкам (LIKE '%Александр%' для каждой колонки через OR)
dataGridView1.ApplyFilter("Александр");

// Фильтр по конкретной колонке
dataGridView1.ApplyFilter("Name", "Иван");

// Полный DataView RowFilter (сложные условия)
dataGridView1.ApplyFilter("[Age] > 18 AND [City] = 'Москва'");

// Сбросить фильтр
dataGridView1.ClearFilter();

// Получить отфильтрованные строки
var filtered = dataGridView1.GetFilteredRows();
```

### Поиск и навигация

```csharp
// Создать навигатор по совпадениям
var navigator = dataGridView1.CreateMatchNavigator("Иван");

// Кнопка "Найти далее"
dataGridView1.FindNext(navigator);

// Кнопка "Найти назад"
dataGridView1.FindPrevious(navigator);

// Найти и подсветить все совпадения
var matches = dataGridView1.HighlightSearchResults("Иван", highlightColor: Color.Yellow);

// Снять подсветку поиска
dataGridView1.ClearSearchHighlight();
```

### FK ComboBox-колонка

```csharp
// Колонка с выпадающим списком из справочника
var fkCol = new DataGridViewFKComboBoxColumn
{
    HeaderText = "Роль",
    DataPropertyName = "RoleID",
    ReferenceTable = "Roles",
    ReferenceIdColumn = "RoleID"
};
fkCol.LoadLookupData(); // авто-загрузка справочника

dataGridView1.Columns.Add(fkCol);
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
