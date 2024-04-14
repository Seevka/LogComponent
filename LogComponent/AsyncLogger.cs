using LogComponent.Core;
using LogComponent.Core.Interfaces;
using System.Collections.Concurrent;

namespace LogComponent
{
    // The original AsyncLog class used a simple but less efficient approach for asynchronous logging, 
    // heavily reliant on a continuous loop running on a separate thread, 
    // and manipulating shared resources without synchronization primitives.
    // This design was prone to concurrency issues and did not efficiently handle resource management, especially in environments requiring high-throughput logging. 
    // To improve upon these issues and avoid the "golden hammer" anti-pattern—where a familiar technology or method 
    // is overused regardless of suitability—the logging mechanism was thoroughly refactored in the new AsyncLogger class.
    public sealed class AsyncLogger : ILogger, IDisposable
    {
        private readonly ISysClock _sysClock;
        private DateTime? _currentDate;

        private readonly string _logDirectory;
        private string _logFileName;

        private readonly BlockingCollection<LogMessage> _logQueue;
        private readonly Task _loggingTask;
        private readonly SemaphoreSlim _semaphore;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private StreamWriter _logWriter;

        private bool _disposed;

        public AsyncLogger(IDirectoryInfo directoryInfo, ISysClock sysClock)
        {
            _sysClock = sysClock;
            directoryInfo.EnsureFolderCreated();
            _logDirectory = directoryInfo.Folder;

            _semaphore = new SemaphoreSlim(1, 1);
            _cancellationTokenSource = new CancellationTokenSource();
            _logQueue = new BlockingCollection<LogMessage>();
            _loggingTask = Task.Run(WriteLogsAsync, _cancellationTokenSource.Token);
        }

        ~AsyncLogger()
        {
            Dispose(false);
        }

        public async Task LogAsync(string message)
        {
            await _semaphore.WaitAsync();
            try
            {
                _logQueue.Add(new LogMessage(message, _sysClock.Now));
            }
            catch
            {
                // Unexpected Exception happened while adding to queue
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task CloseAsync()
        {
            _cancellationTokenSource.Cancel();
            await _loggingTask;
            Dispose();
        }

        public async Task CloseWithFlushAsync()
        {
            _cancellationTokenSource.Cancel();
            await _loggingTask;
            await FlushQueueAsync();
            Dispose();
        }

        private async Task WriteLogsAsync()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var logMessage = _logQueue.Take(_cancellationTokenSource.Token);
                    await EnsureLogFileIsCurrentAsync();
                    await _logWriter.WriteLineAsync(logMessage.ToString());
                    await _logWriter.FlushAsync();
                }
                catch
                {
                    // Exception happened while writing to file or operation was cancelled
                }
            }
        }

        private async Task EnsureLogFileIsCurrentAsync()
        {
            if (_currentDate == _sysClock.Now.Date)
                return;

            _currentDate = _sysClock.Now.Date;
            _logFileName = GetFileName();
            if (_logWriter != null)
                await _logWriter.DisposeAsync();
            OpenLogFile();
        }

        private void OpenLogFile()
        {
            string filePath = Path.Combine(_logDirectory, _logFileName);
            _logWriter = new StreamWriter(filePath, append: true);
        }

        private string GetFileName() => $"{_currentDate:yyyy-MM-dd}.log";

        private async Task FlushQueueAsync()
        {
            foreach (var logMessage in _logQueue)
            {
                await EnsureLogFileIsCurrentAsync();
                await _logWriter.WriteLineAsync(logMessage.ToString());
            }

            await _logWriter?.FlushAsync();
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _logQueue.CompleteAdding();
                _logWriter?.Dispose();
                _semaphore?.Dispose();
                _cancellationTokenSource?.Dispose();
            }

            _disposed = true;
        }
    }
}