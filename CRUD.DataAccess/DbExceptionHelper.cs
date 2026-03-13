using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace CRUD.DataAccess;

public static class DbExceptionHelper
{
    // Коды ошибок https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html

    /// <summary>
    /// Определяет является ли переданное исключение конфликтом параллельности, исходя из регламента проекта.
    /// </summary>
    /// <remarks>
    /// Например, если код исключения базы данных совпадает с кодом дублирования сущности, то это относится к конфликту параллельности.
    /// </remarks>
    /// <param name="ex">Исключение.</param>
    /// <returns><see langword="true"/>, если исключение является конфликтом параллельности, иначе <see langword="false"/>.</returns>
    public static bool IsConcurrencyConflict(DbUpdateException ex)
    {
        // Конфликт параллельности || Исключение из базы, попытка дубликата уникального значения, т.к это связанно с конфликтом параллельности, соответствующая ошибка
        // Исключение о дублировании может произойти, если между бизнес проверкой и записью в базу, другой процесс успеет вставить данные
        if (ex is DbUpdateConcurrencyException || ex.InnerException is MySqlException mySqlException && mySqlException.Number == (int)MySqlErrorCode.DuplicateKeyEntry)
        {
            // Кто первый обновил - тот и остаётся в базе. Второму сообщение о конфликте и предложение попробовать позже
            return true;
        }

        return false;
    }
}