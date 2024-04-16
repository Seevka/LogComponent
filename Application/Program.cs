using LogComponent;
using LogComponent.Core.Implementation;
using LogComponent.Core.Interfaces;

namespace LogUsers
{
    public sealed class Program
    {
        internal static async Task Main(string[] args)
        {
            await NumberFlushAsync();
            await NumberNoFlushAsync();
        }

        private static async Task NumberFlushAsync()
        {
            ILogger asyncLogger = new AsyncLogger(
                new DefaultDirectoryInfo(@"D:\VSFromGitProjects\LogComponent\"),
                new SysClock());

            for (var i = 0; i < 15; i++)
            {
                asyncLogger.LogAsync("Number with Flush: " + i);
                await Task.Delay(50);
            }

            await asyncLogger.CloseWithFlushAsync();
        }

        private static async Task NumberNoFlushAsync()
        {
            ILogger asyncLogger = new AsyncLogger(
                new DefaultDirectoryInfo(@"D:\VSFromGitProjects\LogComponent\"),
                new SysClock());

            for (var i = 50; i > 0; i--)
            {
                asyncLogger.LogAsync("Number with No flush: " + i);
                await Task.Delay(20);
            }

            await asyncLogger.CloseAsync();
        }
    }
}
