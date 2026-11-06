using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PerfCompare.Runner;

public static class ScenarioLoader
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static Scenario Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new ScenarioException($"Файл сценарію не знайдено: {path}");
        }

        return Parse(File.ReadAllText(path));
    }

    public static Scenario Parse(string yaml)
    {
        Scenario? scenario;
        try
        {
            scenario = Deserializer.Deserialize<Scenario>(yaml);
        }
        catch (YamlException ex)
        {
            throw new ScenarioException($"Некоректний YAML: {ex.Message}", ex);
        }

        if (scenario is null)
        {
            throw new ScenarioException("Сценарій порожній");
        }

        ScenarioValidator.Validate(scenario);
        return scenario;
    }
}
