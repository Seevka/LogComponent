using Moq;
using LogComponent.Core.Interfaces;

// Assuming a file system interface like this exists
public interface IFileSystem
{
    void CreateLogFile();
}

namespace LogComponent.Tests
{
    [TestFixture]
    public class LoggerTests
    {
        private Mock<ILogger> _mockLogger;
        private Mock<ISysClock> _mockSysClock;
        private Mock<IFileSystem> _mockFileSystem;
        private CancellationTokenSource _cts;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mockSysClock = new Mock<ISysClock>();
            _mockFileSystem = new Mock<IFileSystem>();
            _cts = new CancellationTokenSource();

            // Default setup to allow logging unless canceled
            _mockLogger.Setup(m => m.LogAsync(It.IsAny<string>()))
                       .Returns((string message) =>
                       {
                           if (_cts.IsCancellationRequested)
                           {
                               throw new OperationCanceledException();
                           }
                           return Task.CompletedTask;
                       });
        }

        // Unit Test
        // Reason: This test verifies the functionality of a single method (LogAsync) in isolation.
        // It checks if the method behaves as expected when a message is passed to it, ensuring that it logs precisely that message.
        // This is a textbook example of a unit test because it focuses on one function and uses mocks to simulate dependencies.
        [Test]
        public async Task LogAsync_WritesMessage()
        {
            // Arrange
            string expectedMessage = "Test message";

            // Act
            await _mockLogger.Object.LogAsync(expectedMessage);

            // Assert
            _mockLogger.Verify(m => m.LogAsync(It.Is<string>(msg => msg == expectedMessage)), Times.Once(), "The message was not logged as expected.");
        }

        // Integration Test
        // Reason: This test evaluates the behavior of the LogAsync method under the condition of crossing midnight,
        // involving interaction with both the system clock (ISysClock) and the file system (IFileSystem).
        // It tests how different components (time checking and file logging) work together to handle a specific scenario.
        // The reliance on multiple system components and their interaction to simulate real-world use cases classifies it as an integration test.
        [Test]
        public async Task LogAsync_CreatesNewFileAfterMidnight()
        {
            // Arrange
            DateTime justBeforeMidnight = new DateTime(2023, 4, 10, 23, 59, 50);
            DateTime justAfterMidnight = justBeforeMidnight.AddDays(1).Date;
            _mockSysClock.SetupSequence(m => m.UtcNow)
                         .Returns(justBeforeMidnight)
                         .Returns(justAfterMidnight);

            _mockLogger.Setup(m => m.LogAsync(It.IsAny<string>()))
                       .Callback(() => {
                           if (_mockSysClock.Object.UtcNow >= justAfterMidnight)
                           {
                               _mockFileSystem.Object.CreateLogFile();
                           }
                       })
                       .Returns(Task.CompletedTask);

            // Act
            await _mockLogger.Object.LogAsync("Final message of the day");
            await _mockLogger.Object.LogAsync("First message of the new day");

            // Assert
            _mockFileSystem.Verify(fs => fs.CreateLogFile(), Times.Once());
        }

        // Integration Test
        // Reason: This test checks that the CloseWithFlushAsync method waits for all outstanding logs to be written before it completes.
        // This involves the logger's interaction with its internal queue or state management to handle logs correctly,
        // and likely depends on the integration of the logging logic with some form of state tracking or persistence mechanisms.
        // Testing how the logger manages state and handles synchronization issues makes this an integration test.
        [Test]
        public async Task CloseWithFlushAsync_ShouldFinishWritingOutstandingLogs()
        {
            // Arrange
            int messagesLogged = 0;
            _mockLogger.Setup(m => m.LogAsync(It.IsAny<string>()))
                       .Returns(() => {
                           messagesLogged++;
                           return Task.CompletedTask;
                       });

            // Start some logs
            await _mockLogger.Object.LogAsync("Message 1");
            await _mockLogger.Object.LogAsync("Message 2");

            // Act
            await _mockLogger.Object.CloseWithFlushAsync();  // This should block until all logs are processed

            // Assert
            Assert.AreEqual(2, messagesLogged, "Not all messages were logged before CloseWithFlushAsync returned.");
        }


        // Integration Test
        // Reason: Similar to the above, this test involves the logger's control logic and cancellation mechanisms.
        // It checks the functionality of CloseAsync to ensure it immediately halts further operations using a cancellation token.
        // Although it uses a single method, the test evaluates the integration of the logger with a
        // cancellation system and its response to external control signals, making it more aligned with integration testing due to the broader interaction scenario.
        [Test]
        public async Task CloseAsync_ShouldStopWithoutWritingOutstandingLogs()
        {
            // Arrange
            _mockLogger.Setup(m => m.CloseAsync())
                       .Returns(Task.CompletedTask)
                       .Callback(() => _cts.Cancel());  // Simulate immediate stop

            // Act
            await _mockLogger.Object.CloseAsync();  // Close logging
            var ex = Assert.CatchAsync<OperationCanceledException>(() =>
                       _mockLogger.Object.LogAsync("Should not be logged"));

            // Assert
            Assert.That(ex, Is.Not.Null, "Expected a cancellation exception but none was thrown.");
        }
    }
}