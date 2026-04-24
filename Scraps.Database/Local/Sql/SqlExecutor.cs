using System;
using System.Data;
using System.IO;
using System.Linq;
using Scraps.Configs;

namespace Scraps.Database.LocalFiles.Sql
{
    /// <summary>Исполнитель SQL AST для локального JSON-хранилища.</summary>
    public static class SqlExecutor
    {
        /// <summary>Выполнить SQL-запрос и вернуть таблицу результата.</summary>
        public static DataTable ExecuteQuery(string sql, params object[] parameters)
        {
            var stmt = SqlParser.Parse(sql);
            var result = Execute(stmt, parameters);
            if (result is DataTable dt) return dt;
            throw new InvalidOperationException("Query did not return a DataTable.");
        }

        /// <summary>Выполнить SQL-запрос и вернуть скалярное значение.</summary>
        public static object ExecuteScalar(string sql, params object[] parameters)
        {
            var dt = ExecuteQuery(sql, parameters);
            if (dt.Rows.Count > 0 && dt.Columns.Count > 0)
                return dt.Rows[0][0];
            return null;
        }

        /// <summary>Выполнить SQL-оператор без результирующего набора данных.</summary>
        public static int ExecuteNonQuery(string sql, params object[] parameters)
        {
            var stmt = SqlParser.Parse(sql);
            return ExecuteNonQuery(stmt, parameters);
        }

        /// <summary>Выполнить уже распарсенный SQL-оператор без результирующего набора данных.</summary>
        public static int ExecuteNonQuery(SqlStatement stmt, params object[] parameters)
        {
            if (stmt is IfStatement ifStmt)
            {
                if (EvaluateIfCondition(ifStmt.Condition))
                {
                    return ExecuteNonQuery(ifStmt.ThenStatement, parameters);
                }
                return 0;
            }

            switch (stmt)
            {
                case InsertStatement insert:
                    return InsertExecutor.Execute(insert);
                case UpdateStatement update:
                    return UpdateExecutor.Execute(update);
                case DeleteStatement delete:
                    return DeleteExecutor.Execute(delete);
                case CreateTableStatement create:
                    return CreateTableExecutor.Execute(create);
                case DropTableStatement drop:
                    return DropTableExecutor.Execute(drop);
                default:
                    throw new NotSupportedException($"Statement type {stmt.GetType().Name} is not supported for non-query execution.");
            }
        }

        private static object Execute(SqlStatement stmt, params object[] parameters)
        {
            if (stmt is SelectStatement select)
                return SelectExecutor.Execute(select);
            if (stmt is InsertStatement insert)
                return InsertExecutor.Execute(insert);
            if (stmt is UpdateStatement update)
                return UpdateExecutor.Execute(update);
            if (stmt is DeleteStatement delete)
                return DeleteExecutor.Execute(delete);
            if (stmt is CreateTableStatement create)
                return CreateTableExecutor.Execute(create);
            if (stmt is DropTableStatement drop)
                return DropTableExecutor.Execute(drop);
            if (stmt is IfStatement ifStmt)
            {
                if (EvaluateIfCondition(ifStmt.Condition))
                    return Execute(ifStmt.ThenStatement, parameters);
                return 0;
            }
            throw new NotSupportedException($"Statement type {stmt.GetType().Name} is not supported.");
        }

        private static bool EvaluateIfCondition(IfCondition condition)
        {
            if (condition is ObjectIdCondition objId)
            {
                var schema = new LocalDatabaseSchema();
                bool exists = schema.TableExists(objId.TableName);
                return objId.IsNotNull ? exists : !exists;
            }
            return false;
        }
    }
}
