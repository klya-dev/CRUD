using System.Security.Claims;

namespace CRUD.Utility;

/// <summary>
/// Расширения для класса <see cref="Claim"/>.
/// </summary>
public static class ClaimExtensions
{
    /// <summary>
    /// Получает <see cref="ClaimTypes.NameIdentifier"/> из коллекции <see cref="Claim"/>'ов.
    /// </summary>
    /// <param name="nameIdentifier">Имя сущности (идентификатор).</param>
    /// <returns><see langword="true"/>, если удалось получить <see cref="ClaimTypes.NameIdentifier"/>, иначе <see langword="false"/> и <paramref name="nameIdentifier"/> <see cref="Guid.Empty"/>.</returns>
    public static bool GetNameIdentifierGuid(this IEnumerable<Claim> claims, out Guid nameIdentifier)
    {
        // Ищем userId в claim'ах и пытаемся пропарсить Id, т.к может прийти "" или вообще любая строчка
        var claimUserId = claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
        if (claimUserId == null || !Guid.TryParse(claimUserId.Value, out Guid userId))
        {
            nameIdentifier = Guid.Empty;
            return false; // Не удалось получить
        }

        nameIdentifier = userId;
        return true;
    }
}