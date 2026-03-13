using System.Reflection;
using Xunit;

namespace CRUD.Test.Shared;

public class AssertExtensions : Assert
{
    // Все поля совпадают, кроме RowVersion, но RowVersion также должен пройти проверку на null
    //AssertExtensions.EqualIgnoring(userFromDbAfterUpdate, mustUserFromDbAfterUpdate, (result) =>
    //{
    //    if (result.RowVersion == null)
    //        Assert.Fail(nameof(result.RowVersion) + " is null.");
    //}, nameof(userFromDbAfterUpdate.RowVersion));

    /// <summary>
    /// Сравнивает два объекта с типом <see langword="T"/> через метод <see cref="Assert.Equal{T}(T, T)"/>, игнорируя указанные поля.
    /// </summary>
    /// <remarks>
    /// <para>Игнорируемые поля указываются относительно <see langword="T"/>.</para>
    /// <para>Например, <c>ServiceResponse.AccessToken</c>, для типа, в котором определёно свойство <c>ServiceResponse</c>.</para>
    /// <para>Можно указать кастомную проверку <paramref name="customCheckValidIgnoreProperty"/> для игнорируемых полей.</para>
    /// <para>Смысл проверки, можно трактовать так: "Одинаковый результат, кроме токена (поля), но токен также должен пройти проверку". Например, хоть мы и игнорируем поле, но это поле должно быть не пустым.</para>
    /// <para>Вызывающий метод должен сам обрабатывать проверку, например через <see cref="Assert.Fail"/>. <see cref="EqualIgnoring{T}(T, T, Action{T}?, string[])"/> лишь вызывает делегаты с проверкой для сравниваемых объектов.</para>
    /// <para>Если игнорируемое поле указанно неверно, поле не сможет быть найдено и выбросится исключение <see cref="KeyNotFoundException"/>.</para>
    /// 
    /// <example>
    /// Пример использования:
    /// <code>
    ///  // Все поля совпадают, кроме RowVersion, но RowVersion также должен пройти проверку на null
    /// AssertExtensions.EqualIgnoring(userFromDbAfterUpdate, mustUserFromDbAfterUpdate, (user) =>
    /// {
    ///     Assert.NotNull(user.RowVersion);
    /// }, nameof(userFromDbAfterUpdate.RowVersion));
    /// </code>
    /// </example>
    /// 
    /// </remarks>
    /// <exception cref="KeyNotFoundException">Если игнорируемое поле указанно неверно.</exception>
    /// <typeparam name="T">Тип сравниваемых объектов.</typeparam>
    /// <param name="object1">Первый объект сравнения.</param>
    /// <param name="object2">Второй объект сравнения.</param>
    /// <param name="customCheckValidIgnoreProperty">Кастомная проверка игнорируемых полей, внутри которой можно вызывать <see cref="Assert.Fail"/>.</param>
    /// <param name="ignoreProperties">Игнорируемые поля, относительно <see langword="T"/>.</param>
    public static void EqualIgnoring<T>(T object1, T object2, Action<T>? customCheckValidIgnoreProperty = null, params string[] ignoreProperties)
    {
        // Кастомная проверка игнорируемых полей
        customCheckValidIgnoreProperty?.Invoke(object1);
        customCheckValidIgnoreProperty?.Invoke(object2);

        // Без повторов
        var ignoreSet = new HashSet<string>(ignoreProperties);

        CompareObjects(object1!, object2!, ignoreSet, typeof(T), "");

        // Какие-то поля не найдены (в методе CompareObjects, если поле успешно проигнорировалось, оно удаляется из ignoreProperties)
        if (ignoreSet.Count > 0)
            throw new KeyNotFoundException("Fields not found: " + string.Join(", ", ignoreSet));
    }

    /// <summary>
    /// Рекурсивно сравнивает каждое поле, кроме игнорируемых через метод <see cref="Assert.Equal{T}(T, T)"/>.
    /// </summary>
    /// <remarks>
    /// Если поле успешно проигнорировалось, оно удаляется из <paramref name="ignoreProperties"/>.
    /// </remarks>
    /// <param name="object1">Первый объект сравнения.</param>
    /// <param name="object2">Второй объект сравнения.</param>
    /// <param name="ignoreProperties">Игнорируемые свойства.</param>
    /// <param name="type">Тип сравниваемых объектов.</param>
    /// <param name="prefix">Префикс для рекурсивного поиска полей.</param>
    private static void CompareObjects(object object1, object object2, HashSet<string> ignoreProperties, Type type, string prefix)
    {
        // Если объекты пустые, выходим
        if (object1 == null && object2 == null)
            return;

        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Для рекурсивного поиска вложенных полей, добавляется точка
            var propertyName = prefix + property.Name;

            // Пропускаем, игнорируем, не сравниваем, указанное поле
#pragma warning disable CA1868 // Ненужный вызов "Contains(item)"
            if (ignoreProperties.Contains(propertyName)) // Всё равно нужно проверить, это часть логики
            {
                ignoreProperties.Remove(propertyName);
                continue;
            }
#pragma warning restore CA1868 // Ненужный вызов "Contains(item)"

            var expectedValue = property.GetValue(object1);
            var actualValue = property.GetValue(object2);

            // Если свойство - это массив, то представляем свойство, как массив и успешно сравниваем (решаем проблему с RowVersion)
            if (property.PropertyType.IsArray)
                Equal(expectedValue as Array, actualValue as Array);
            else if (property.PropertyType.IsClass && property.PropertyType != typeof(string)) // Если свойство - это класс и не строка, рекурсивно вызываем CompareObjects
                CompareObjects(expectedValue!, actualValue!, ignoreProperties, property.PropertyType, propertyName + ".");
            else
                Equal(expectedValue, actualValue);
        }
    }

    /// <summary>
    /// Проверяет является ли значение <paramref name="value"/> пустым через метод <see cref="string.IsNullOrWhiteSpace(string?)"/>.
    /// </summary>
    /// <remarks>
    /// Если значение является пустым, то вызывается метод <see cref="Assert.Fail(string?)"/>.
    /// </remarks>
    /// <param name="value">Значение.</param>
    /// <param name="valueNameOf">Имя значения.</param>
    public static void IsNotNullOrNotWhiteSpace(string value, string valueNameOf = "value")
    {
        if (string.IsNullOrWhiteSpace(value))
            Fail($"{valueNameOf} is null or white space");
    }

    /// <summary>
    /// Проверяет является ли значение <paramref name="value"/> не пустым через метод !<see cref="string.IsNullOrWhiteSpace(string?)"/>.
    /// </summary>
    /// <remarks>
    /// Если значение не является пустым, то вызывается метод <see cref="Assert.Fail(string?)"/>.
    /// </remarks>
    /// <param name="value">Значение.</param>
    /// <param name="valueNameOf">Имя значения.</param>
    public static void IsNullOrWhiteSpace(string value, string valueNameOf = "value")
    {
        if (!string.IsNullOrWhiteSpace(value))
            Fail($"{valueNameOf} is not null or not white space");
    }
}