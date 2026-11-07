namespace PerfCompare.Server.Analytics;

public static class Statistics
{
    public static double Mean(IReadOnlyList<double> values)
    {
        EnsureNotEmpty(values);
        return values.Sum() / values.Count;
    }

    public static double Median(IReadOnlyList<double> values)
    {
        EnsureNotEmpty(values);
        var sorted = values.OrderBy(value => value).ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2.0;
    }

    public static double Percentile(IReadOnlyList<double> values, double percent)
    {
        EnsureNotEmpty(values);
        if (percent <= 0 || percent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), "відсоток має бути в межах (0; 100]");
        }

        var sorted = values.OrderBy(value => value).ToList();
        var rank = (int)Math.Ceiling(percent * sorted.Count / 100.0);
        return sorted[Math.Clamp(rank, 1, sorted.Count) - 1];
    }

    public static double StandardDeviation(IReadOnlyList<double> values)
    {
        EnsureNotEmpty(values);
        if (values.Count < 2)
        {
            return 0;
        }

        var mean = Mean(values);
        var sumOfSquares = values.Sum(value => (value - mean) * (value - mean));
        return Math.Sqrt(sumOfSquares / (values.Count - 1));
    }

    public static double CoefficientOfVariationPercent(IReadOnlyList<double> values)
    {
        var mean = Mean(values);
        return mean == 0 ? 0 : StandardDeviation(values) / mean * 100.0;
    }

    private static void EnsureNotEmpty(IReadOnlyList<double> values)
    {
        if (values.Count == 0)
        {
            throw new ArgumentException("набір значень порожній", nameof(values));
        }
    }
}
