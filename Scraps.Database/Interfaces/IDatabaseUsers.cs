using System.Data;

namespace Scraps.Database
{
    /// <summary>
    /// Интерфейс для работы с пользователями.
    /// </summary>
    public interface IDatabaseUsers
    {
        /// <summary>Получить пользователя по логину.</summary>
        DataRow GetByLogin(string login);

        /// <summary>Получить статус/роль пользователя.</summary>
        string GetUserStatus(string login);

        /// <summary>Создать пользователя.</summary>
        void Create(string login, string password, string role);

        /// <summary>Удалить пользователя.</summary>
        void Delete(string login);

        /// <summary>Изменить пароль.</summary>
        void ChangePassword(string login, string newPassword);

        /// <summary>Изменить роль.</summary>
        void ChangeRole(string login, string newRole);
    }
}
