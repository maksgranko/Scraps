using Scraps.Configs;
using System;
using System.Collections.Generic;
using System.Data;

namespace Scraps.Database.Local
{
    /// <summary>
    /// Локальная база данных (LocalFiles режим).
    /// Заглушка — будет реализована позже (JSON-хранилище).
    /// </summary>
    public class LocalDatabase : DatabaseBase
    {
        /// <summary>Провайдер базы данных.</summary>
        public override DatabaseProvider Provider => DatabaseProvider.LocalFiles;

        static LocalDatabase()
        {
            DatabaseProviderFactory.Register(DatabaseProvider.LocalFiles, () => new LocalDatabase());
        }

        /// <summary>Создать экземпляр LocalDatabase.</summary>
        public LocalDatabase()
        {
        }

        /// <inheritdoc/>
        public override bool TestConnection()
        {
            throw new NotImplementedException("LocalDatabase ещё не реализован. Используйте DatabaseProvider.MSSQL.");
        }

        /// <inheritdoc/>
        public override void Initialize(DatabaseGenerationOptions options)
        {
            throw new NotImplementedException("LocalDatabase ещё не реализован. Используйте DatabaseProvider.MSSQL.");
        }
    }
}
