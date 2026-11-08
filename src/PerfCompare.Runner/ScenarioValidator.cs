namespace PerfCompare.Runner;

public static class ScenarioValidator
{
    public const int MaxRepeats = 100;
    public const int MaxTimeoutSeconds = 3600;
    public const int MaxNameLength = 100;

    public static void Validate(Scenario scenario)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(scenario.Name))
        {
            errors.Add("name: не може бути порожнім");
        }
        else if (scenario.Name.Length > MaxNameLength)
        {
            errors.Add($"name: не більше {MaxNameLength} символів");
        }

        if (string.IsNullOrWhiteSpace(scenario.Command))
        {
            errors.Add("command: не може бути порожнім");
        }

        if (scenario.Repeats is < 1 or > MaxRepeats)
        {
            errors.Add($"repeats: від 1 до {MaxRepeats}, зараз {scenario.Repeats}");
        }

        if (scenario.TimeoutSeconds is < 1 or > MaxTimeoutSeconds)
        {
            errors.Add($"timeoutSeconds: від 1 до {MaxTimeoutSeconds}, зараз {scenario.TimeoutSeconds}");
        }

        if (scenario.Args.Exists(static argument => argument is null))
        {
            errors.Add("args: елементи не можуть бути порожніми");
        }

        if (scenario.Warmup < 0)
        {
            errors.Add($"warmup: не менше 0, зараз {scenario.Warmup}");
        }

        if (errors.Count > 0)
        {
            throw new ScenarioException(string.Join("; ", errors));
        }
    }
}
