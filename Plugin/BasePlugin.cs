using System.Xml.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MiUtils.Plugin;

public abstract class BasePlugin(IServiceProvider serviceProvider, IServiceCollection serviceCollection, ILogger logger)
    : IHostedService
{
    private IServiceProvider ServiceProvider { get; set; } = serviceProvider;
    private IServiceCollection ServiceCollection { get; set; } = serviceCollection;
    private ILogger Logger { get; set; } = logger;

    public async Task Init()
    {
        // 用于重写添加功能的Init
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Init();
        await Load(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await UnLoad(cancellationToken);
    }

    protected abstract Task Load(CancellationToken cancellationToken);
    protected abstract Task UnLoad(CancellationToken cancellationToken);
}

public abstract class BasePlugin<TConfig> : IHostedService where TConfig : class, new()
{
    private IServiceProvider ServiceProvider { get; set; }
    private IServiceCollection ServiceCollection { get; set; }
    private ILogger Logger { get; set; }
    protected TConfig Config { get; private set; }
    private string ConfigFilePath { get; set; }

    protected BasePlugin(IServiceProvider serviceProvider, IServiceCollection serviceCollection, ILogger logger)
    {
        ServiceProvider = serviceProvider;
        ServiceCollection = serviceCollection;
        Logger = logger;

        ConfigFilePath = GetDefaultConfigFilePath();

        Task.Run(async () =>
        {
            await Init();
        }).Wait();
    }

    private string GetDefaultConfigFilePath()
    {
        var pluginType = GetType();
        var pluginName = pluginType.Name;
        var configFileType = typeof(TConfig).Name.ToLower();

        return configFileType switch
        {
            var ext when ext.Contains("json") => $"./configs/{pluginName}.json",
            var ext when ext.Contains("xml") => $"./configs/{pluginName}.xml",
            var ext when ext.Contains("yaml") || ext.Contains("yml") => $"./configs/{pluginName}.yaml",
            _ => throw new NotSupportedException($"Unsupported config file type: {configFileType}")
        };
    }

    private async Task Init()
    {
        // 读取配置文件
        Config = await LoadConfigAsync();

        // 用于重写添加功能的Init
        await OnInit();
    }

    protected abstract Task OnInit();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Load(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await UnLoad(cancellationToken);

        // 保存配置文件
        await SaveConfigAsync();
    }

    protected abstract Task Load(CancellationToken cancellationToken);
    protected abstract Task UnLoad(CancellationToken cancellationToken);

    private async Task<TConfig> LoadConfigAsync()
    {
        if (!File.Exists(ConfigFilePath))
        {
            var config = new TConfig();
            await SaveConfigAsync(config);
            return config;
        }

        var fileExtension = Path.GetExtension(ConfigFilePath).ToLower();
        var fileContent = await File.ReadAllTextAsync(ConfigFilePath);

        return fileExtension switch
        {
            ".json" => JsonConvert.DeserializeObject<TConfig>(fileContent),
            ".xml" => DeserializeXml(fileContent),
            ".yaml" or ".yml" => new DeserializerBuilder()
                                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                .Build()
                                .Deserialize<TConfig>(fileContent),
            _ => throw new NotSupportedException($"Unsupported file extension: {fileExtension}")
        };
    }

    public async Task ReloadConfigAsync()
    {
        Config = await LoadConfigAsync();
    }

    private async Task SaveConfigAsync()
    {
        await SaveConfigAsync(Config);
    }

    private async Task SaveConfigAsync(TConfig config)
    {
        var fileExtension = Path.GetExtension(ConfigFilePath).ToLower();
        var fileContent = fileExtension switch
        {
            ".json" => JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented),
            ".xml" => SerializeXml(config),
            ".yaml" or ".yml" => new SerializerBuilder()
                                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                .Build()
                                .Serialize(config),
            _ => throw new NotSupportedException($"Unsupported file extension: {fileExtension}")
        };

        Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath));
        await File.WriteAllTextAsync(ConfigFilePath, fileContent);
    }

    private static TConfig DeserializeXml(string xml)
    {
        var serializer = new XmlSerializer(typeof(TConfig));
        using var reader = new StringReader(xml);
        return (TConfig)serializer.Deserialize(reader);
    }

    private static string SerializeXml(TConfig config)
    {
        var serializer = new XmlSerializer(typeof(TConfig));
        using var writer = new StringWriter();
        serializer.Serialize(writer, config);
        return writer.ToString();
    }
}
