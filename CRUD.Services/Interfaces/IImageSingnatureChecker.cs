namespace CRUD.Services.Interfaces;

/// <summary>
/// Сервис для проверки сигнатуры файла на соответствие изображению.
/// </summary>
public interface IImageSingnatureChecker
{
    /// <summary>
    /// Проверяет файл на соответствие сигнатуры.
    /// </summary>
    /// <remarks>
    /// <para>Допустимые сигнатуры: <c>png</c>, <c>jpeg</c>, <c>jpeg2000</c>, <c>jpg</c>.</para>
    /// 
    /// Возможные исключения:
    /// <list type="bullet">
    /// <item>
    /// <term>Если <paramref name="stream"/> <see langword="null"/></term>
    /// <description>исключение <see cref="ArgumentNullException"/>.</description>
    /// </item>
    /// </list>
    /// 
    /// </remarks>
    /// <param name="stream">Поток файла.</param>
    /// <exception cref="ArgumentNullException">Если <paramref name="stream"/> <see langword="null"/>.</exception>
    /// <returns>Подходит ли сигнатура файла, расширение этого файла без точки и MimeType. Примеры возврата: <c>(<see langword="false"/>, <see langword="null"/>, <see langword="null"/>)</c>, <c>(<see langword="true"/>, "png", "image/png")</c>.</returns>
    (bool IsValid, string FileExtension, string ContentType) IsFileValid(Stream stream);
}