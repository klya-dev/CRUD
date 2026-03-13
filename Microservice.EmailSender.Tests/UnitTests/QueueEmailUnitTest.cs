namespace Microservice.EmailSender.Tests.UnitTests;

public class QueueEmailUnitTest
{
    private readonly IQueueEmail _queueEmail;

    public QueueEmailUnitTest()
    {
        _queueEmail = new QueueEmail();
    }
}