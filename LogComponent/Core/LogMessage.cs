namespace LogComponent.Core
{
    /// <summary>
    /// This is the object that the diff. loggers (file logger, console logger etc.) will operate on. The LineText() method will be called to get the text (formatted) to log
    /// </summary>
    public class LogMessage
    {
        public LogMessage(string message, DateTime timestamp)
        {
            Message = message;
            Timestamp = timestamp;
        }

        /// <summary>
        ///     The text to be display in log line
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     The Timestamp is initialized when the log is added.
        /// </summary>
        public DateTime Timestamp { get; }

        public override string ToString()
        {
            return $"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss:fff}\tMessage: {Message}";
        }
    }
}