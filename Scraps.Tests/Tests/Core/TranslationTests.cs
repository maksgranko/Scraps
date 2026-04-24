using Scraps.Localization;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Xunit;

namespace Scraps.Tests.Core
{
    public class TranslationTests
    {
        [Fact]
        public void Translate_Works()
        {
            TranslationManager.Translations.Clear();

            TranslationManager.Translations["Users"] = "Пользователи";
            Assert.Equal("Пользователи", TranslationManager.Translate("Users"));
            Assert.Equal("Users", TranslationManager.Untranslate("Пользователи"));
        }

        [Fact]
        public void TranslateDataTable_Works()
        {
            TranslationManager.Translations.Clear();
            TranslationManager.Translations[TranslationManager.ColumnKey("Users", "Login")] = "Логин";

            var dt = new DataTable();
            dt.Columns.Add("Login");
            dt.Rows.Add("admin");

            TranslationManager.Translate(dt, "Users");
            Assert.True(dt.Columns.Contains("Логин"));

            TranslationManager.Untranslate(dt, "Users");
            Assert.True(dt.Columns.Contains("Login"));
        }

        [Fact]
        public void Translate_Array_Works()
        {
            TranslationManager.Translations.Clear();

            TranslationManager.Translations["ImportTest"] = "Импорт";
            var list = TranslationManager.Translate(new[] { "ImportTest" });
            Assert.Equal("Импорт", list[0]);
        }

        [Fact]
        public void Translate_And_Untranslate_ForPlainStrings_Works()
        {
            TranslationManager.Translations.Clear();
            TranslationManager.Translations["Hello"] = "Привет";

            Assert.Equal("Привет", TranslationManager.Translate("Hello"));
            Assert.Equal("Hello", TranslationManager.Untranslate("Привет"));
            Assert.Equal("Unknown", TranslationManager.Translate("Unknown"));
            Assert.Equal("Неизвестно", TranslationManager.Untranslate("Неизвестно"));
        }

        [Fact]
        public void Load_Merges_And_CanClear()
        {
            TranslationManager.Replace(
                new Dictionary<string, string>
                {
                    ["Users"] = "Пользователи",
                    [TranslationManager.ColumnKey("Users", "Login")] = "Логин"
                });

            TranslationManager.Load(
                new Dictionary<string, string>
                {
                    ["Roles"] = "Роли",
                    [TranslationManager.ColumnKey("Users", "Password")] = "Пароль"
                },
                clearBeforeLoad: false);

            Assert.Equal("Пользователи", TranslationManager.Translate("Users"));
            Assert.Equal("Роли", TranslationManager.Translate("Roles"));
            Assert.Equal("Логин", TranslationManager.TranslateColumnName("Users", "Login"));
            Assert.Equal("Пароль", TranslationManager.TranslateColumnName("Users", "Password"));

            TranslationManager.Load(
                new Dictionary<string, string> { ["Only"] = "Только" },
                clearBeforeLoad: true);

            Assert.Equal("Only", TranslationManager.Untranslate("Только"));
            Assert.Equal("Users", TranslationManager.Translate("Users"));
        }

        [Fact]
        public void Replace_Replaces_All_In_OneShot()
        {
            TranslationManager.Replace(
                new Dictionary<string, string>
                {
                    ["Old"] = "Старое",
                    [TranslationManager.ColumnKey("Old", "Col")] = "Колонка"
                });

            TranslationManager.Replace(
                new Dictionary<string, string>
                {
                    ["New"] = "Новое",
                    [TranslationManager.ColumnKey("New", "Name")] = "Имя"
                });

            Assert.Equal("Old", TranslationManager.Translate("Old"));
            Assert.Equal("Новое", TranslationManager.Translate("New"));
            Assert.Equal("Name", TranslationManager.UntranslateColumnName("New", "Имя"));
        }

        [Fact]
        public void Load_FromCsvFile_Works()
        {
            TranslationManager.Translations.Clear();
            var path = Path.Combine(Path.GetTempPath(), "scraps_translations_" + System.Guid.NewGuid().ToString("N") + ".csv");

            try
            {
                File.WriteAllText(path, "Key;Value\nUsers;Пользователи\nHello;Привет");
                TranslationManager.Load(path, delimiter: ';', hasHeader: true, clearBeforeLoad: true);

                Assert.Equal("Пользователи", TranslationManager.Translate("Users"));
                Assert.Equal("Привет", TranslationManager.Translate("Hello"));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        [Fact]
        public void Load_FromCsvFile_WithCustomRowSeparator_Works()
        {
            TranslationManager.Translations.Clear();
            var path = Path.Combine(Path.GetTempPath(), "scraps_translations_rowsep_" + System.Guid.NewGuid().ToString("N") + ".csv");

            try
            {
                File.WriteAllText(path, "k|v||A|1||B|2");
                TranslationManager.Load(
                    filePath: path,
                    delimiter: '|',
                    rowSeparator: "||",
                    hasHeader: true,
                    clearBeforeLoad: true);

                Assert.Equal("1", TranslationManager.Translate("A"));
                Assert.Equal("2", TranslationManager.Translate("B"));
            }
            finally
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
        }
    }
}
