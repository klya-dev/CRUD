namespace CRUD.Tests.Collections;

/// <summary>
/// Класс объявления коллекции.
/// </summary>
/// <remarks>
/// <para>Нужно для того, чтобы можно было переиспользовать Docker-контейнер на всю тестовую сессию, а не только на один класс.</para>
/// <para>У всех тестовых классов, у которых стоит этот аттрибут будут использовать один Docker-контейнер на всех (без перезапуска).</para>
/// </remarks>
[CollectionDefinition(nameof(GlobalDbContainerCollection), DisableParallelization = true)]
public sealed class GlobalDbContainerCollection : ICollectionFixture<DbContainerFixture>
{

}