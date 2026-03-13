namespace CRUD.Test.Shared;

/// <summary>
/// Вспомогательный класс для тестов.
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Возвращает путь до папки проекта.
    /// </summary>
    public static string GetProjectDirectoryPath()
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var projectDir = Directory.GetParent(assemblyLocation)?.Parent?.Parent?.Parent?.FullName ?? string.Empty;
        return projectDir;
    }
}