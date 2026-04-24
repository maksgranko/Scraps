using System;
using System.Collections.Generic;
using System.Linq;

namespace Scraps.Database.LocalFiles.Sql
{
    /// <summary>Парсер SQL-токенов в AST-структуры локального SQL-эмулятора.</summary>
    public class SqlParser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        /// <summary>Создать парсер для списка токенов.</summary>
        public SqlParser(List<Token> tokens)
        {
            _tokens = tokens;
            _pos = 0;
        }

        /// <summary>Распарсить SQL-строку в AST-оператор.</summary>
        public static SqlStatement Parse(string sql)
        {
            var tokenizer = new SqlTokenizer(sql);
            var tokens = tokenizer.Tokenize();
            var parser = new SqlParser(tokens);
            return parser.ParseStatement();
        }

        private SqlStatement ParseStatement()
        {
            var token = Current();
            if (token.Type == TokenType.EOF)
                return null;

            if (MatchKeyword("SELECT"))
                return ParseSelect();
            if (MatchKeyword("INSERT"))
                return ParseInsert();
            if (MatchKeyword("UPDATE"))
                return ParseUpdate();
            if (MatchKeyword("DELETE"))
                return ParseDelete();
            if (MatchKeyword("CREATE"))
                return ParseCreateTable();
            if (MatchKeyword("DROP"))
                return ParseDropTable();
            if (MatchKeyword("IF"))
                return ParseIf();

            throw new InvalidOperationException($"Unexpected token: {token.Value}");
        }

        private SelectStatement ParseSelect()
        {
            var stmt = new SelectStatement();

            if (MatchKeyword("COUNT"))
            {
                ExpectSymbol("(");
                MatchSymbol("*"); // optional *
                ExpectSymbol(")");
                stmt.IsCountAll = true;
            }
            else if (MatchSymbol("*"))
            {
                stmt.Columns.Add("*");
            }
            else
            {
                do
                {
                    stmt.Columns.Add(ReadIdentifierOrValue());
                } while (MatchSymbol(","));
            }

            ExpectKeyword("FROM");
            stmt.TableName = ReadIdentifierOrValue();
            if (Current().Type == TokenType.Identifier && !IsKeyword(Current().Value))
            {
                stmt.TableAlias = Current().Value;
                Advance();
            }

            if (MatchKeyword("WHERE"))
                stmt.Where = new WhereClause { Predicate = ParsePredicate() };

            return stmt;
        }

        private InsertStatement ParseInsert()
        {
            var stmt = new InsertStatement();
            ExpectKeyword("INTO");
            stmt.TableName = ReadIdentifierOrValue();
            ExpectSymbol("(");
            do
            {
                stmt.Columns.Add(ReadIdentifierOrValue());
            } while (MatchSymbol(","));
            ExpectSymbol(")");

            ExpectKeyword("VALUES");
            ExpectSymbol("(");
            do
            {
                stmt.Values.Add(ReadValue());
            } while (MatchSymbol(","));
            ExpectSymbol(")");

            return stmt;
        }

        private UpdateStatement ParseUpdate()
        {
            var stmt = new UpdateStatement { TableName = ReadIdentifierOrValue() };
            ExpectKeyword("SET");
            do
            {
                var col = ReadIdentifierOrValue();
                ExpectSymbol("=");
                var val = ReadValue();
                stmt.Assignments.Add(new SetAssignment { Column = col, Value = val });
            } while (MatchSymbol(","));

            if (MatchKeyword("WHERE"))
                stmt.Where = new WhereClause { Predicate = ParsePredicate() };

            return stmt;
        }

        private DeleteStatement ParseDelete()
        {
            var stmt = new DeleteStatement();
            ExpectKeyword("FROM");
            stmt.TableName = ReadIdentifierOrValue();
            if (MatchKeyword("WHERE"))
                stmt.Where = new WhereClause { Predicate = ParsePredicate() };
            return stmt;
        }

        private CreateTableStatement ParseCreateTable()
        {
            ExpectKeyword("TABLE");
            var stmt = new CreateTableStatement { TableName = ReadIdentifierOrValue() };
            ExpectSymbol("(");
            do
            {
                var col = new ColumnDef { Name = ReadIdentifierOrValue() };
                var typeTok = Current();
                if (typeTok.Type == TokenType.Keyword || typeTok.Type == TokenType.Identifier)
                {
                    col.Type = typeTok.Value;
                    Advance();
                    // handle nvarchar(50)
                    if (MatchSymbol("("))
                    {
                        var size = ReadIdentifierOrValue(); // 50 or MAX
                        col.Type += $"({size})";
                        ExpectSymbol(")");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Expected column type, got {typeTok.Value}");
                }

                while (true)
                {
                    if (MatchKeyword("IDENTITY"))
                    {
                        col.IsIdentity = true;
                        if (MatchSymbol("("))
                        {
                            col.IdentitySeed = int.Parse(ReadIdentifierOrValue());
                            ExpectSymbol(",");
                            col.IdentityIncrement = int.Parse(ReadIdentifierOrValue());
                            ExpectSymbol(")");
                        }
                    }
                    else if (MatchKeyword("PRIMARY"))
                    {
                        ExpectKeyword("KEY");
                    }
                    else if (MatchKeyword("NOT"))
                    {
                        ExpectKeyword("NULL");
                        col.IsNullable = false;
                    }
                    else if (MatchKeyword("NULL"))
                    {
                        col.IsNullable = true;
                    }
                    else
                    {
                        break;
                    }
                }

                stmt.Columns.Add(col);
            } while (MatchSymbol(","));
            ExpectSymbol(")");
            return stmt;
        }

        private DropTableStatement ParseDropTable()
        {
            ExpectKeyword("TABLE");
            return new DropTableStatement { TableName = ReadIdentifierOrValue() };
        }

        private IfStatement ParseIf()
        {
            var stmt = new IfStatement();
            if (MatchKeyword("OBJECT_ID"))
            {
                ExpectSymbol("(");
                // Handle N'...' where N may be tokenized separately or as part of the string
                if (Current().Type == TokenType.Identifier && string.Equals(Current().Value, "N", StringComparison.OrdinalIgnoreCase))
                    Advance();
                var tableName = ReadIdentifierOrValue()?.Trim('\'', '[', ']');
                ExpectSymbol(",");
                var typeValue = ReadIdentifierOrValue()?.Trim('\''); // 'U' as String token
                ExpectSymbol(")");
                bool isNotNull = false;
                if (MatchKeyword("IS"))
                {
                    if (MatchKeyword("NOT"))
                    {
                        ExpectKeyword("NULL");
                        isNotNull = true;
                    }
                    else
                    {
                        ExpectKeyword("NULL");
                        isNotNull = false;
                    }
                }
                stmt.Condition = new ObjectIdCondition { TableName = tableName, IsNotNull = isNotNull };
            }
            else if (MatchKeyword("EXISTS"))
            {
                ExpectSymbol("(");
                var sub = ParseSelect();
                ExpectSymbol(")");
                stmt.Condition = new ObjectIdCondition { TableName = sub.TableName, IsNotNull = true };
            }
            else
            {
                throw new InvalidOperationException("Unsupported IF condition");
            }

            stmt.ThenStatement = ParseStatement();
            return stmt;
        }

        private Predicate ParsePredicate()
        {
            return ParseOr();
        }

        private Predicate ParseOr()
        {
            var left = ParseAnd();
            while (MatchKeyword("OR"))
            {
                left = new OrPredicate { Left = left, Right = ParseAnd() };
            }
            return left;
        }

        private Predicate ParseAnd()
        {
            var left = ParseNot();
            while (MatchKeyword("AND"))
            {
                left = new AndPredicate { Left = left, Right = ParseNot() };
            }
            return left;
        }

        private Predicate ParseNot()
        {
            if (MatchKeyword("NOT"))
                return new NotPredicate { Inner = ParseNot() };
            return ParseComparison();
        }

        private Predicate ParseComparison()
        {
            var col = ReadIdentifierOrValue();

            if (MatchKeyword("IS"))
            {
                bool isNot = MatchKeyword("NOT");
                ExpectKeyword("NULL");
                return new IsNullPredicate { Column = col, IsNot = isNot };
            }

            if (MatchKeyword("LIKE"))
            {
                var pattern = ReadValue()?.ToString();
                return new LikePredicate { Column = col, Pattern = pattern };
            }

            var op = ReadOperator();
            var val = ReadValue();
            return new ComparisonPredicate { Column = col, Operator = op, Value = val };
        }

        private string ReadOperator()
        {
            var t = Current();
            if (t.Type == TokenType.Symbol && new[] { "=", "<", ">", "<=", ">=", "!=", "<>" }.Contains(t.Value))
            {
                Advance();
                return t.Value;
            }
            throw new InvalidOperationException($"Expected operator, got {t.Value}");
        }

        private string ReadIdentifierOrValue()
        {
            var t = Current();
            if (t.Type == TokenType.Identifier || t.Type == TokenType.String || t.Type == TokenType.Number)
            {
                Advance();
                return t.Value;
            }
            if (t.Type == TokenType.Keyword)
            {
                Advance();
                return t.Value;
            }
            throw new InvalidOperationException($"Expected identifier, got {t.Value} ({t.Type})");
        }

        private object ReadValue()
        {
            var t = Current();
            if (t.Type == TokenType.String)
            {
                Advance();
                return t.Value;
            }
            if (t.Type == TokenType.Number)
            {
                Advance();
                if (t.Value.Contains("."))
                    return decimal.Parse(t.Value, System.Globalization.CultureInfo.InvariantCulture);
                return int.Parse(t.Value);
            }
            if (MatchKeyword("NULL"))
                return null;
            if (t.Type == TokenType.Identifier)
            {
                Advance();
                return t.Value;
            }
            throw new InvalidOperationException($"Expected value, got {t.Value}");
        }

        private Token Current()
        {
            if (_pos < _tokens.Count) return _tokens[_pos];
            return new Token { Type = TokenType.EOF, Value = "" };
        }

        private void Advance()
        {
            if (_pos < _tokens.Count) _pos++;
        }

        private bool MatchKeyword(string keyword)
        {
            if (Current().Type == TokenType.Keyword && string.Equals(Current().Value, keyword, StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                return true;
            }
            return false;
        }

        private bool MatchSymbol(string symbol)
        {
            if (Current().Type == TokenType.Symbol && Current().Value == symbol)
            {
                Advance();
                return true;
            }
            return false;
        }

        private void ExpectKeyword(string keyword)
        {
            if (!MatchKeyword(keyword))
                throw new InvalidOperationException($"Expected keyword '{keyword}', got {Current().Value}");
        }

        private void ExpectSymbol(string symbol)
        {
            if (!MatchSymbol(symbol))
                throw new InvalidOperationException($"Expected symbol '{symbol}', got {Current().Value}");
        }

        private static bool IsKeyword(string word)
        {
            return SqlTokenizer.IsKeywordStatic(word);
        }
    }
}
