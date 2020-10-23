using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace DHU
{
    class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        private static void ConfigureLogging(WebHostBuilderContext arg1, ILoggingBuilder loggerFactory)
        {
#if DEBUG
            loggerFactory.AddProvider(new SimpleLoggerProvider());
            loggerFactory.SetMinimumLevel(LogLevel.Trace);
#endif
        }

        public static IWebHost BuildWebHost(string[] args) =>
            //WebHost.CreateDefaultBuilder(args)
            new WebHostBuilder()
				//.UseKestrel(this.ConfigureKestrel)
                .UseKestrel(options =>
                {
                    //options.Limits.MaxConcurrentConnections = 1;
                    //options.Limits.MaxConcurrentUpgradedConnections = 1;                
                })
                .ConfigureLogging(ConfigureLogging)
                .UseStartup<Startup>()
                .UseUrls("http://0.0.0.0:5001")
                .Build();
    }

    public class SimpleLoggerProvider : ILoggerProvider
    {
        public SimpleLoggerProvider()
        {
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new SimpleLogger();
        }

        public void Dispose()
        {
        }
    }

    public class NullDisposable : IDisposable
    {

        public void Dispose()
        {

        }

    }

    public class SimpleLogger : ILogger
    {

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {

        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new NullDisposable();
        }

    }
}
