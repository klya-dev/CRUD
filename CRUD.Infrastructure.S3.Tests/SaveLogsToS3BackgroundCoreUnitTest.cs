#nullable disable
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace CRUD.Infrastructure.S3.Tests;

public class SaveLogsToS3BackgroundCoreUnitTest : IDisposable
{
    private readonly SaveLogsToS3BackgroundCore _saveLogsToS3BackgroundCore;
    private readonly Mock<IS3Manager> _s3ManagerMock;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<IOptions<S3Options>> _s3OptionsMock;
    private readonly Mock<ILogger<SaveLogsToS3BackgroundCore>> _loggerMock;
    private readonly string _tempRoot;
    private readonly string _logsDir;

    public SaveLogsToS3BackgroundCoreUnitTest()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);

        // WebRootPath будет указывать на временную папку
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.SetupGet(e => e.ContentRootPath).Returns(_tempRoot);

        // Создаём папку "/Logs" во временной папке
        var s3Options = TestSettingsHelper.GetConfigurationValue<S3Options, TestMarker>(S3Options.SectionName);
        _logsDir = Path.Combine(_tempRoot, s3Options.LogsDirectory);
        Directory.CreateDirectory(_logsDir);

        _s3ManagerMock = new();
        _s3OptionsMock = new();
        _loggerMock = new();

        _s3OptionsMock.Setup(x => x.Value).Returns(new S3Options() { ServiceURL = "https://localhost", BucketName = "", AccessKey = "", SecretKey = "", LogsDirectory = s3Options.LogsDirectory, LogsInS3Directory = s3Options.LogsInS3Directory });

        _saveLogsToS3BackgroundCore = new(_envMock.Object, _s3ManagerMock.Object, _s3OptionsMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
        catch { }

        GC.SuppressFinalize(this);
    }

    private string CreateLogFile(DateTime date)
    {
        // Имя формата log-yyyyMMdd.txt -> совпадает с логикой класса
        var fileName = $"log-{date:yyyyMMdd}.txt";
        var fullPath = Path.Combine(_logsDir, fileName);
        File.WriteAllText(fullPath, "test");
        return fullPath;
    }

    [Fact] // Файл успешно загружен в облако и удалён локально
    public async Task DoWorkAsync_WhenUploadSucceeds_FileIsDeleted()
    {
        // Arrange
        var oldDate = DateTime.Now.Date.AddDays(-1);
        var path = CreateLogFile(oldDate);
        _s3ManagerMock.Setup(s => s.CreateObjectAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(ServiceResult.Success());

        // Act
        await _saveLogsToS3BackgroundCore.DoWorkAsync(CancellationToken.None);

        // Assert
        Assert.False(File.Exists(path));

        // CreateObjectAsync был вызван
        _s3ManagerMock.Verify(s => s.CreateObjectAsync(It.IsAny<Stream>(), It.Is<string>(k => k.EndsWith("log-" + oldDate.ToString("yyyyMMdd") + ".txt")), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // Не удалось загрузить файл в облако, файл остаётся и логгер логирует
    public async Task DoWorkAsync_WhenUploadFails_FileRemains_AndErrorLogged()
    {
        // Arrange
        var oldDate = DateTime.Now.Date.AddDays(-1);
        var path = CreateLogFile(oldDate);
        _s3ManagerMock.Setup(s => s.CreateObjectAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(ServiceResult.Fail("some error"));

        // Act
        await _saveLogsToS3BackgroundCore.DoWorkAsync(CancellationToken.None);

        // Assert: файл остался
        Assert.True(File.Exists(path));
        _s3ManagerMock.Verify(s => s.CreateObjectAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once); // Метод вызвался

        // Проверим, что логгер получил вызов LogError (в Moq логирование сложнее проверить напрямую, простая проверка вызова)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Не удалось загрузить файл-лог")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact] // Сегодняшний лог
    public async Task DoWorkAsync_WhenFileIsToday_ReturnsWithoutProcessing()
    {
        // Arrange
        var today = DateTime.Now.Date;
        var path = CreateLogFile(today);

        // Act
        await _saveLogsToS3BackgroundCore.DoWorkAsync(CancellationToken.None);

        // Assert
        // файл остается на месте
        Assert.True(File.Exists(path));

        _s3ManagerMock.Verify(s => s.CreateObjectAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never); // Метод не вызвался
    }
}