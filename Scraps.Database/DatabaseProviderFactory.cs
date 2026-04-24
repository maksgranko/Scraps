using Scraps.Configs;
using System;
using System.Collections.Generic;

namespace Scraps.Database
{
    /// <summary>
    /// Фабрика для создания провайдеров баз данных.
    /// </summary>
    public static class DatabaseProviderFactory
    {
        private static readonly Dictionary<DatabaseProvider, Func<IDatabase>> _providers = new Dictionary<DatabaseProvider, Func<IDatabase>>();
        private static readonly object _lock = new object();
        private static IDatabase _current;

        /// <summary>
        /// Зарегистрировать провайдер базы данных.
        /// </summary>
        public static void Register(DatabaseProvider provider, Func<IDatabase> factory)
        {
            _providers[provider] = factory;
        }

        /// <summary>
        /// Создать провайдер базы данных.
        /// </summary>
        public static IDatabase Create(DatabaseProvider provider)
        {
            if (_providers.TryGetValue(provider, out var factory))
                return factory();

            throw new InvalidOperationException($"Провайдер {provider} не зарегистрирован. Установите соответствующий NuGet пакет (например, Scraps.Database.MSSQL).");
        }

        /// <summary>
        /// Текущий провайдер базы данных.
        /// </summary>
        public static IDatabase Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_lock)
                    {
                        if (_current == null)
                        {
                            var provider = ScrapsConfig.DatabaseProvider;
                            if (provider == DatabaseProvider.None)
                                throw new InvalidOperationException("DatabaseProvider не установлен. Укажите ScrapsConfig.DatabaseProvider.");

                            _current = Create(provider);
                        }
                    }
                }
                return _current;
            }
        }

        /// <summary>
        /// Сбросить текущий провайдер (для смены БД).
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _current = null;
            }
        }
    }
}
