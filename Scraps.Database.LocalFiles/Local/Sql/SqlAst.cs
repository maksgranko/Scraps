using System.Collections.Generic;

namespace Scraps.Database.LocalFiles.Sql
{
    /// <summary>Базовый тип AST-узла SQL-оператора.</summary>
    public abstract class SqlStatement { }

    /// <summary>Оператор SELECT.</summary>
    public class SelectStatement : SqlStatement
    {
        /// <summary>Список выбираемых колонок.</summary>
        public List<string> Columns { get; set; } = new List<string>();
        /// <summary>Имя таблицы источника.</summary>
        public string TableName { get; set; }
        /// <summary>Алиас таблицы (если указан).</summary>
        public string TableAlias { get; set; }
        /// <summary>Условие WHERE.</summary>
        public WhereClause Where { get; set; }
        /// <summary>Признак агрегата COUNT(*).</summary>
        public bool IsCountAll { get; set; }
    }

    /// <summary>Оператор INSERT.</summary>
    public class InsertStatement : SqlStatement
    {
        /// <summary>Имя таблицы назначения.</summary>
        public string TableName { get; set; }
        /// <summary>Список колонок для вставки.</summary>
        public List<string> Columns { get; set; } = new List<string>();
        /// <summary>Список значений для вставки.</summary>
        public List<object> Values { get; set; } = new List<object>();
    }

    /// <summary>Оператор UPDATE.</summary>
    public class UpdateStatement : SqlStatement
    {
        /// <summary>Имя обновляемой таблицы.</summary>
        public string TableName { get; set; }
        /// <summary>Список присваиваний SET.</summary>
        public List<SetAssignment> Assignments { get; set; } = new List<SetAssignment>();
        /// <summary>Условие WHERE.</summary>
        public WhereClause Where { get; set; }
    }

    /// <summary>Одна операция присваивания в секции SET.</summary>
    public class SetAssignment
    {
        /// <summary>Имя колонки.</summary>
        public string Column { get; set; }
        /// <summary>Назначаемое значение.</summary>
        public object Value { get; set; }
    }

    /// <summary>Оператор DELETE.</summary>
    public class DeleteStatement : SqlStatement
    {
        /// <summary>Имя таблицы.</summary>
        public string TableName { get; set; }
        /// <summary>Условие WHERE.</summary>
        public WhereClause Where { get; set; }
    }

    /// <summary>Оператор CREATE TABLE.</summary>
    public class CreateTableStatement : SqlStatement
    {
        /// <summary>Имя создаваемой таблицы.</summary>
        public string TableName { get; set; }
        /// <summary>Описание создаваемых колонок.</summary>
        public List<ColumnDef> Columns { get; set; } = new List<ColumnDef>();
    }

    /// <summary>Описание колонки в CREATE TABLE.</summary>
    public class ColumnDef
    {
        /// <summary>Имя колонки.</summary>
        public string Name { get; set; }
        /// <summary>SQL-тип колонки.</summary>
        public string Type { get; set; }
        /// <summary>Признак IDENTITY.</summary>
        public bool IsIdentity { get; set; }
        /// <summary>Признак nullable-колонки.</summary>
        public bool IsNullable { get; set; } = true;
        /// <summary>Начальное значение IDENTITY.</summary>
        public int IdentitySeed { get; set; } = 1;
        /// <summary>Шаг инкремента IDENTITY.</summary>
        public int IdentityIncrement { get; set; } = 1;
    }

    /// <summary>Оператор DROP TABLE.</summary>
    public class DropTableStatement : SqlStatement
    {
        /// <summary>Имя удаляемой таблицы.</summary>
        public string TableName { get; set; }
    }

    /// <summary>Оператор IF с одним THEN-оператором.</summary>
    public class IfStatement : SqlStatement
    {
        /// <summary>Условие IF.</summary>
        public IfCondition Condition { get; set; }
        /// <summary>Оператор, выполняемый при истинном условии.</summary>
        public SqlStatement ThenStatement { get; set; }
    }

    /// <summary>Базовый тип условия IF.</summary>
    public abstract class IfCondition { }
    /// <summary>Условие OBJECT_ID для проверки существования таблицы.</summary>
    public class ObjectIdCondition : IfCondition
    {
        /// <summary>Имя таблицы для проверки.</summary>
        public string TableName { get; set; }
        /// <summary>Если true — проверка на NOT NULL (таблица существует), иначе на NULL.</summary>
        public bool IsNotNull { get; set; }
    }

    /// <summary>Секция WHERE.</summary>
    public class WhereClause
    {
        /// <summary>Корневой предикат условия.</summary>
        public Predicate Predicate { get; set; }
    }

    /// <summary>Базовый тип предиката WHERE.</summary>
    public abstract class Predicate { }
    /// <summary>Предикат сравнения (Column Operator Value).</summary>
    public class ComparisonPredicate : Predicate
    {
        /// <summary>Имя колонки.</summary>
        public string Column { get; set; }
        /// <summary>Оператор сравнения (=, !=, &gt;, &lt;, &gt;=, &lt;=).</summary>
        public string Operator { get; set; }
        /// <summary>Сравниваемое значение.</summary>
        public object Value { get; set; }
    }
    /// <summary>Предикат LIKE.</summary>
    public class LikePredicate : Predicate
    {
        /// <summary>Имя колонки.</summary>
        public string Column { get; set; }
        /// <summary>Шаблон LIKE.</summary>
        public string Pattern { get; set; }
    }
    /// <summary>Предикат IS NULL / IS NOT NULL.</summary>
    public class IsNullPredicate : Predicate
    {
        /// <summary>Имя колонки.</summary>
        public string Column { get; set; }
        /// <summary>Если true — IS NOT NULL, иначе IS NULL.</summary>
        public bool IsNot { get; set; }
    }
    /// <summary>Логический предикат AND.</summary>
    public class AndPredicate : Predicate
    {
        /// <summary>Левый операнд.</summary>
        public Predicate Left { get; set; }
        /// <summary>Правый операнд.</summary>
        public Predicate Right { get; set; }
    }
    /// <summary>Логический предикат OR.</summary>
    public class OrPredicate : Predicate
    {
        /// <summary>Левый операнд.</summary>
        public Predicate Left { get; set; }
        /// <summary>Правый операнд.</summary>
        public Predicate Right { get; set; }
    }
    /// <summary>Логический предикат NOT.</summary>
    public class NotPredicate : Predicate
    {
        /// <summary>Внутренний предикат.</summary>
        public Predicate Inner { get; set; }
    }
}
