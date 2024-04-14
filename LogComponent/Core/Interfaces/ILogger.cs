namespace LogComponent.Core.Interfaces
{
    public interface ILogger
    {
        /// <summary>
        /// Stop the logging. If any outstanding logs theses will not be written to Log
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// Stop the logging. The call will not return until all logs have been written to Log.
        /// </summary>
        Task CloseWithFlushAsync();

        /// <summary>
        /// Write a message to the Log.
        /// </summary>
        /// <param name="message">The text to written to the log</param>
        Task LogAsync(string message);
    }
}
