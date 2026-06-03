namespace Microservice.EmailSender.Tests.UnitTests;

public sealed class QueueEmailUnitTest
{
    private readonly IQueueEmail _queueEmail;

    public QueueEmailUnitTest()
    {
        _queueEmail = new QueueEmail();
    }
}