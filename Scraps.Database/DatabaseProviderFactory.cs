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
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (_lock)
            {
                _providers[provider] = factory;
            }
        }

        /// <summary>
        /// Создать провайдер базы данных.
        /// </summary>
        public static IDatabase Create(DatabaseProvider provider)
        {
            lock (_lock)
            {
                if (_providers.TryGetValue(provider, out var factory))
                    return factory();
            }

            if (TryAutoRegister(provider))
            {
                lock (_lock)
                {
                    if (_providers.TryGetValue(provider, out var factory))
                        return factory();
                }
            }

            throw new InvalidOperationException(GetMissingProviderMessage(provider));
        }

        private static bool TryAutoRegister(DatabaseProvider provider)
        {
            var providerTypeName = GetProviderTypeName(provider);
            if (string.IsNullOrWhiteSpace(providerTypeName))
                return false;

            var providerType = Type.GetType(providerTypeName, throwOnError: false);
            if (providerType == null)
                return false;

            if (!typeof(IDatabase).IsAssignableFrom(providerType))
                throw new InvalidOperationException($"Тип '{providerType.FullName}' не реализует IDatabase.");

            Register(provider, () => (IDatabase)Activator.CreateInstance(providerType));
            return true;
        }

        private static string GetProviderTypeName(DatabaseProvider provider)
        {
            switch (provider)
            {
                case DatabaseProvider.MSSQL:
                    return "Scraps.Database.MSSQL.MSSQLDatabase, Scraps.Database.MSSQL";
                case DatabaseProvider.LocalFiles:
                    return "Scraps.Database.LocalFiles.LocalDatabase, Scraps.Database.LocalFiles";
                default:
                    return null;
            }
        }

        private static string GetMissingProviderMessage(DatabaseProvider provider)
        {
            switch (provider)
            {
                case DatabaseProvider.MSSQL:
                    return "Провайдер MSSQL не найден. Подключите пакет Scraps.Database.MSSQL.";
                case DatabaseProvider.LocalFiles:
                    return "Провайдер LocalFiles не найден. Подключите пакет Scraps.Database.LocalFiles.";
                default:
                    return $"Провайдер {provider} не поддерживается.";
            }
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
