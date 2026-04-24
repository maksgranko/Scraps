using Scraps.Security;

namespace Scraps.Database
{
    /// <summary>Модель прав роли по таблице.</summary>
    public class RolePermissionInfo
    {
        /// <summary>Идентификатор роли.</summary>
        public int RoleId { get; set; }
        /// <summary>Имя таблицы.</summary>
        public string TableName { get; set; }
        /// <summary>Права (флаги).</summary>
        public TablePermission Permission { get; set; }
        /// <summary>Права (флаги).</summary>
        public PermissionFlags Flags => Permission?.Flags ?? PermissionFlags.None;
    }
}
