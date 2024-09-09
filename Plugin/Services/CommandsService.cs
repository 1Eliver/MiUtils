using Microsoft.Extensions.Hosting;

namespace MiUtils.Plugin.Services;

public class CommandsService(IHostApplicationLifetime hostApplicationLifetime) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (command, args) = ParseCommand(Console.ReadLine());
                if (command is "exit" or "quit")
                {
                    hostApplicationLifetime.StopApplication();
                    break;
                }
            }
        }, cancellationToken);
    }
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Implement any cleanup logic if necessary
        return Task.CompletedTask;
    }
    private static (string Command, string[] Args) ParseCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return (string.Empty, []);
        }

        var parts = input.Split(' ');
        var command = parts[0];
        var args = parts.Length > 1 ? parts[1..] : [];

        return (command, args);
    }
}