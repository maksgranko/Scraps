using System;
using System.Collections.Generic;
using System.Text;

namespace Scraps.Database.LocalFiles.Sql
{
    /// <summary>Тип лексемы SQL-токенизатора.</summary>
    public enum TokenType
    {
        /// <summary>Ключевое слово SQL.</summary>
        Keyword,
        /// <summary>Идентификатор (имя таблицы, колонки и т.п.).</summary>
        Identifier,
        /// <summary>Строковый литерал.</summary>
        String,
        /// <summary>Числовой литерал.</summary>
        Number,
        /// <summary>Символ (знаки пунктуации и операторы).</summary>
        Symbol,
        /// <summary>Маркер конца входной строки.</summary>
        EOF
    }

    /// <summary>Одна лексема SQL-текста.</summary>
    public class Token
    {
        /// <summary>Тип лексемы.</summary>
        public TokenType Type { get; set; }
        /// <summary>Текстовое значение лексемы.</summary>
        public string Value { get; set; }
        /// <summary>Позиция лексемы в исходном SQL.</summary>
        public int Position { get; set; }
    }

    /// <summary>Токенизатор SQL-строк для локального SQL-эмулятора.</summary>
    public class SqlTokenizer
    {
        private readonly string _sql;
        private int _pos;

        /// <summary>Создать токенизатор для SQL-строки.</summary>
        public SqlTokenizer(string sql)
        {
            _sql = sql ?? "";
            _pos = 0;
        }

        /// <summary>Разбить SQL-строку на список токенов.</summary>
        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            while (true)
            {
                var token = NextToken();
                tokens.Add(token);
                if (token.Type == TokenType.EOF) break;
            }
            return tokens;
        }

        private Token NextToken()
        {
            SkipWhitespace();
            if (_pos >= _sql.Length)
                return new Token { Type = TokenType.EOF, Value = "", Position = _pos };

            char c = _sql[_pos];
            int start = _pos;

            // Unicode string literal: N'...'
            if (c == 'N' && _pos + 1 < _sql.Length && (_sql[_pos + 1] == '\'' || _sql[_pos + 1] == '\"'))
            {
                _pos++; // skip N
                return ReadString(_sql[_pos], start, isUnicode: true);
            }

            if (c == '\'' || c == '\"')
                return ReadString(c, start, isUnicode: false);

            if (c == '[')
                return ReadBracketIdentifier(start);

            if (char.IsLetter(c) || c == '_')
                return ReadWord(start);

            if (char.IsDigit(c))
                return ReadNumber(start);

            // Multi-char operators
            if (c == '!' && _pos + 1 < _sql.Length && _sql[_pos + 1] == '=')
            {
                _pos += 2;
                return new Token { Type = TokenType.Symbol, Value = "!=", Position = start };
            }
            if (c == '<' && _pos + 1 < _sql.Length && _sql[_pos + 1] == '=')
            {
                _pos += 2;
                return new Token { Type = TokenType.Symbol, Value = "<=", Position = start };
            }
            if (c == '>' && _pos + 1 < _sql.Length && _sql[_pos + 1] == '=')
            {
                _pos += 2;
                return new Token { Type = TokenType.Symbol, Value = ">=", Position = start };
            }
            if (c == '<' && _pos + 1 < _sql.Length && _sql[_pos + 1] == '>')
            {
                _pos += 2;
                return new Token { Type = TokenType.Symbol, Value = "<>", Position = start };
            }

            _pos++;
            return new Token { Type = TokenType.Symbol, Value = c.ToString(), Position = start };
        }

        private void SkipWhitespace()
        {
            while (_pos < _sql.Length && char.IsWhiteSpace(_sql[_pos]))
                _pos++;
        }

        private Token ReadString(char quote, int start, bool isUnicode = false)
        {
            _pos++; // skip opening quote
            var sb = new StringBuilder();
            while (_pos < _sql.Length)
            {
                char c = _sql[_pos];
                if (c == quote)
                {
                    _pos++;
                    if (_pos < _sql.Length && _sql[_pos] == quote)
                    {
                        // escaped quote
                        sb.Append(c);
                        _pos++;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    sb.Append(c);
                    _pos++;
                }
            }
            return new Token { Type = TokenType.String, Value = sb.ToString(), Position = start };
        }

        private Token ReadBracketIdentifier(int start)
        {
            _pos++; // skip [
            var sb = new StringBuilder();
            while (_pos < _sql.Length && _sql[_pos] != ']')
            {
                sb.Append(_sql[_pos]);
                _pos++;
            }
            if (_pos < _sql.Length && _sql[_pos] == ']')
                _pos++;
            return new Token { Type = TokenType.Identifier, Value = sb.ToString(), Position = start };
        }

        private Token ReadWord(int start)
        {
            var sb = new StringBuilder();
            while (_pos < _sql.Length && (char.IsLetterOrDigit(_sql[_pos]) || _sql[_pos] == '_'))
            {
                sb.Append(_sql[_pos]);
                _pos++;
            }
            var word = sb.ToString();
            var type = IsKeyword(word) ? TokenType.Keyword : TokenType.Identifier;
            return new Token { Type = type, Value = word, Position = start };
        }

        private Token ReadNumber(int start)
        {
            var sb = new StringBuilder();
            while (_pos < _sql.Length && (char.IsDigit(_sql[_pos]) || _sql[_pos] == '.' || _sql[_pos] == '-'))
            {
                sb.Append(_sql[_pos]);
                _pos++;
            }
            return new Token { Type = TokenType.Number, Value = sb.ToString(), Position = start };
        }

        private static readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "INSERT", "INTO", "VALUES", "UPDATE", "SET", "DELETE",
            "CREATE", "TABLE", "DROP", "IF", "OBJECT_ID", "IS", "NULL", "NOT", "AND", "OR",
            "LIKE", "IDENTITY", "PRIMARY", "KEY", "NVARCHAR", "VARCHAR", "INT", "BIT",
            "DATETIME", "DECIMAL", "FLOAT", "UNIQUEIDENTIFIER", "ON", "EXISTS", "COUNT"
        };

        private static bool IsKeyword(string word)
        {
            return Keywords.Contains(word);
        }

        /// <summary>Проверить, является ли слово SQL-ключевым словом.</summary>
        public static bool IsKeywordStatic(string word)
        {
            return Keywords.Contains(word);
        }
    }
}
