namespace Microservice.EmailSender.Tests.Collections;

/// <summary>
/// Класс объявления коллекции.
/// </summary>
/// <remarks>
/// <para>Нужно для того, чтобы интеграционные тесты выполнялись последовательно, а не параллельно.</para>
/// <para>У всех тестовых классов, у которых стоит этот аттрибут будут выполняться последовательно.</para>
/// </remarks>
[CollectionDefinition(nameof(IntegrationsTestCollection), DisableParallelization = true)]
public sealed class IntegrationsTestCollection
{

}