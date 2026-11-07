using PerfCompare.Contracts;

namespace PerfCompare.Server.Api;

public static class RunValidator
{
    private const int MaxNameLength = 100;
    private const int MaxRepeats = 100;
    private const int MaxTimeoutSeconds = 3600;

    public static IReadOnlyList<string> Validate(RunResult? run)
    {
        var errors = new List<string>();
        if (run is null)
        {
            errors.Add("тіло запиту: порожнє");
            return errors;
        }

        ValidateScenario(run.Scenario, errors);
        ValidateEnvironment(run.Environment, errors);

        if (run.StartedAt == default)
        {
            errors.Add("startedAt: обов'язкове поле");
        }

        ValidateMeasurements(run.Measurements, run.Scenario?.Repeats ?? 0, errors);
        return errors;
    }

    private static void ValidateScenario(ScenarioInfo? scenario, List<string> errors)
    {
        if (scenario is null)
        {
            errors.Add("scenario: обов'язкове поле");
            return;
        }

        if (string.IsNullOrWhiteSpace(scenario.Name))
        {
            errors.Add("scenario.name: не може бути порожнім");
        }
        else if (scenario.Name.Length > MaxNameLength)
        {
            errors.Add($"scenario.name: не більше {MaxNameLength} символів");
        }

        RequireText(scenario.Command, "scenario.command", errors);
        RequireRange(scenario.Repeats, 1, MaxRepeats, "scenario.repeats", errors);
        RequireRange(scenario.TimeoutSeconds, 1, MaxTimeoutSeconds, "scenario.timeoutSeconds", errors);
    }

    private static void ValidateEnvironment(EnvironmentInfo? environment, List<string> errors)
    {
        if (environment is null)
        {
            errors.Add("environment: обов'язкове поле");
            return;
        }

        RequireText(environment.Os, "environment.os", errors);
        RequireText(environment.OsVersion, "environment.osVersion", errors);
        RequireText(environment.Runtime, "environment.runtime", errors);

        if (environment.Cores < 1)
        {
            errors.Add($"environment.cores: не менше 1, зараз {environment.Cores}");
        }

        if (environment.RamMb < 1)
        {
            errors.Add($"environment.ramMb: не менше 1, зараз {environment.RamMb}");
        }
    }

    private static void ValidateMeasurements(IReadOnlyList<Measurement>? measurements, int repeats, List<string> errors)
    {
        if (measurements is null || measurements.Count == 0)
        {
            errors.Add("measurements: не може бути порожнім");
            return;
        }

        if (repeats > 0 && measurements.Count > repeats)
        {
            errors.Add($"measurements: {measurements.Count} вимірювань, а repeats = {repeats}");
        }

        var seen = new HashSet<int>();
        for (var index = 0; index < measurements.Count; index++)
        {
            ValidateMeasurement(measurements[index], $"measurements[{index}]", seen, errors);
        }
    }

    private static void ValidateMeasurement(Measurement? measurement, string path, HashSet<int> seen, List<string> errors)
    {
        if (measurement is null)
        {
            errors.Add($"{path}: порожній елемент");
            return;
        }

        if (measurement.Iteration < 1)
        {
            errors.Add($"{path}.iteration: не менше 1, зараз {measurement.Iteration}");
        }
        else if (!seen.Add(measurement.Iteration))
        {
            errors.Add($"{path}.iteration: значення {measurement.Iteration} повторюється");
        }

        if (!IsNotNegative(measurement.DurationMs))
        {
            errors.Add($"{path}.durationMs: має бути числом не менше 0");
        }

        if (measurement.PeakMemMb is double peak && !IsNotNegative(peak))
        {
            errors.Add($"{path}.peakMemMb: має бути null або числом не менше 0");
        }

        if (measurement.CpuTimeMs is double cpu && !IsNotNegative(cpu))
        {
            errors.Add($"{path}.cpuTimeMs: має бути null або числом не менше 0");
        }

        if (!Enum.IsDefined(measurement.Status))
        {
            errors.Add($"{path}.status: невідоме значення");
        }
    }

    private static bool IsNotNegative(double value)
    {
        return value >= 0 && !double.IsInfinity(value);
    }

    private static void RequireText(string? value, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field}: не може бути порожнім");
        }
    }

    private static void RequireRange(int value, int min, int max, string field, List<string> errors)
    {
        if (value < min || value > max)
        {
            errors.Add($"{field}: від {min} до {max}, зараз {value}");
        }
    }
}
