using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MiUtils.Plugin.Services;

namespace MiUtils.Plugin;

public class PluginContainer
{
    public static async Task Main(string[] args)
    {
        var build = Host.CreateDefaultBuilder(args);
        build.ConfigureLogging((context, logger) =>
        {
            logger.ClearProviders();
            logger.AddProvider(new MiLoggerProvider("./logs"));
        });
        build.ConfigureServices((context, services) =>
        {
            LoadPluginsAndRegisterServices(services);
            services.AddHostedService<CommandsService>();
        });
    }
    
    private static void LoadPluginsAndRegisterServices(IServiceCollection services)
    {
        const string libsPath = "./libs";
        const string pluginsPath = "./plugins";
        Directory.CreateDirectory(libsPath);
        Directory.CreateDirectory(pluginsPath);
        LoadDependencyAssemblies(libsPath);
        var pluginAssemblies = Directory.GetFiles(pluginsPath, "*.dll")
            .Select(Assembly.LoadFrom)
            .ToArray();
        foreach (var assembly in pluginAssemblies)
        {
            var pluginTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(BasePlugin)));
            foreach (var pluginType in pluginTypes)
            {
                var method = typeof(ServiceCollectionHostedServiceExtensions)
                    .GetMethods()
                    .First(m => m is { Name: "AddHostedService", IsGenericMethod: true });
                var genericMethod = method.MakeGenericMethod(pluginType);
                genericMethod.Invoke(null, new object[] { services, null });
            }
        }
    }
    private static void LoadDependencyAssemblies(string libsPath)
    {
        var dependencyAssemblies = Directory.GetFiles(libsPath, "*.dll");
        foreach (var assemblyPath in dependencyAssemblies)
        {
            Assembly.LoadFrom(assemblyPath);
        }
    }
}