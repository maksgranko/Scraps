# Scraps

Scraps — небольшая библиотека помощник. Тут собраны повторно используемые методы для работы с БД, правами, переводами, авторизацией/сессией, импортом и экспортом.
 

## НЕ РЕКОМЕНДУЕТСЯ В ОБЫЧНЫХ ПРОЕКТАХ, ПОСКОЛЬКУ ЭТО НЕ ГОДИТСЯ К ИСПОЛЬЗОВАНИЮ ИЗ-ЗА ПРОБЛЕМ С БЕЗОПАСНОСТЬЮ.
Однако, в то же время, это может помочь студентам, которым нужно часто создавать формочки, работать с ними, и создавать базы данных с различными требованиями на C#, в основном .NET Framework 4, однако может быть совместимо и с другими версиями.


## Структура

- `Scraps/Configs`
  - `ScrapsConfig.cs` — глобальные настройки (имя БД, строка подключения, таблица пользователей и т.д.)
- `Scraps/Databases`
  - `MSSQL/` — работа с SQL Server, генерация схемы, users/roles/permissions
  - `TableCatalog.cs` — список таблиц и автодетект
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

## Быстрый старт (самое главное)

```csharp
// 1) Настройки
ScrapsConfig.DatabaseName = "MyDb";
ScrapsConfig.ConnectionString = MSSQL.ConnectionStringBuilder();

// 2) Создать базовую схему (Users/Roles/RolePermissions), если нет
MSSQL.PreCheck();

// 3) Подгрузить роли/права из БД
RoleManager.InitializeFromDb();

// 4) Вход и сессия
var ok = UserSession.Login("admin", "Password123!");
if (ok)
{
    var role = UserSession.UserRole;
}
```

## Права (PermissionFlags)

Если нужно несколько прав сразу — используем `|` (побитовое ИЛИ):

```csharp
var flags = PermissionFlags.Read | PermissionFlags.Export;
```

Все права сразу:

```csharp
var all = PermissionFlags.Read | PermissionFlags.Write | PermissionFlags.Delete | PermissionFlags.Export | PermissionFlags.Import;
```
Так работает ВЕЗДЕ, где можно указывать права.

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
// Автодетект + виртуальные таблицы из реестра
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

## Экспорт

```csharp
var dt = ReportDataBuilder.GetTableTranslated("Users");
ReportExporter.ExportToExcel(dt, "users.xlsx");
ReportExporter.ExportToPdf(dt, "users.pdf");
```

## Полезные заметки

- `UserSession.Utilities` — проверка пароля и хэши.
- `TranslationManager` меняет названия колонок прямо в `DataTable`.
- В `VirtualTableRegistry` можно задавать права по ролям, есть правило `*` (для всех).

