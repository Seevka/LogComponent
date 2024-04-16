using Moq;
using LogComponent.Core.Interfaces;
using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using NUnit.Framework.Internal;
using System.ComponentModel.Design;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LogComponent.Tests
{
    [TestFixture]
    public class AsyncLoggerTests
    {
        private AsyncLogger _logger;
        private Mock<ISysClock> _mockSysClock;
        private Mock<IDirectoryInfo> _mockDirectoryInfo;
        private string _tempDirectory;

        [SetUp]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDirectory);

            _mockSysClock = new Mock<ISysClock>();
            _mockSysClock.Setup(m => m.Now).Returns(DateTime.UtcNow);

            _mockDirectoryInfo = new Mock<IDirectoryInfo>();
            _mockDirectoryInfo.Setup(d => d.Folder).Returns(_tempDirectory);
            _mockDirectoryInfo.Setup(d => d.EnsureFolderCreated()).Verifiable();

            _logger = new AsyncLogger(_mockDirectoryInfo.Object, _mockSysClock.Object);
        }

        [Test]
        public async Task LogAsync_CreatesNewFileAfterMidnight()
        {
            // Arrange
            DateTime justBeforeMidnight = new DateTime(2023, 4, 10, 23, 59, 50);
            DateTime justAfterMidnight = justBeforeMidnight.AddDays(1).Date;
            _mockSysClock.SetupSequence(m => m.Now)
                         .Returns(justBeforeMidnight)
                         .Returns(justAfterMidnight);

            // Act
            await _logger.LogAsync("Final message of the day");
            await Task.Delay(1000);
            _mockSysClock.Setup(m => m.Now).Returns(justAfterMidnight);
            await _logger.LogAsync("First message of the new day");
            await _logger.CloseWithFlushAsync();

            // Assert
            string expectedNewDayFileName = $"{justAfterMidnight:yyyy-MM-dd}.log";
            string expectedNewDayFilePath = Path.Combine(_tempDirectory, expectedNewDayFileName);
            Assert.IsTrue(File.Exists(expectedNewDayFilePath), "Log file for new day was not created.");
        }

        [Test]
        public async Task LogAsync_WritesMessage()
        {
            // Arrange
            string expectedMessage = "Test message";
            string expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
            string expectedFilePath = Path.Combine(_tempDirectory, expectedFileName);

            // Act
            await _logger.LogAsync(expectedMessage);
            await Task.Delay(1000);
            await _logger.CloseWithFlushAsync();

            // Assert
            Assert.IsTrue(File.Exists(expectedFilePath), "Log file was not created.");
            string logContents = File.ReadAllText(expectedFilePath);
            Assert.IsTrue(logContents.Contains(expectedMessage), "The message was not logged as expected.");
        }

        [Test]
        public async Task CloseWithFlushAsync_ShouldFinishWritingOutstandingLogs()
        {
            // Act
            await _logger.LogAsync("Message 1");
            await _logger.LogAsync("Message 2");
            await _logger.CloseWithFlushAsync();

            // Assert
            string expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
            string expectedFilePath = Path.Combine(_tempDirectory, expectedFileName);
            Assert.IsTrue(File.Exists(expectedFilePath), "Log file was not created.");

            var logContents = File.ReadAllText(expectedFilePath);
            Assert.IsTrue(logContents.Contains("Message 1"), "Message 1 was not logged as expected.");
            Assert.IsTrue(logContents.Contains("Message 2"), "Message 2 was not logged as expected.");
        }

        [Test]
        public async Task CloseAsync_ShouldStopWithoutWritingOutstandingLogs()
        {
            // Act
            await _logger.LogAsync("Message 1");
            await _logger.LogAsync("Should not be logged");
            await _logger.CloseAsync();
            await Task.Delay(1000);

            // Assert
            string expectedFileName = $"{DateTime.UtcNow:yyyy-MM-dd}.log";
            string expectedFilePath = Path.Combine(_tempDirectory, expectedFileName);
            Assert.IsTrue(File.Exists(expectedFilePath), "Log file was not created.");

            var logContents = File.ReadAllText(expectedFilePath);
            Assert.IsFalse(logContents.Contains("Should not be logged"), "Message was logged after CloseAsync, which is not expected.");
        }
    }
}
