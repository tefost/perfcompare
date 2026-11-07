using PerfCompare.Server.Analytics;

namespace PerfCompare.Server.Tests;

public class StatisticsTests
{
    private static readonly double[] OneToFiftyStep10 = { 10, 20, 30, 40, 50 };

    [Fact]
    public void Mean_ReturnsAverage()
    {
        Assert.Equal(30.0, Statistics.Mean(OneToFiftyStep10), 6);
    }

    [Fact]
    public void Median_OddCount_ReturnsMiddle()
    {
        Assert.Equal(30.0, Statistics.Median(new double[] { 50, 10, 30, 20, 40 }), 6);
    }

    [Fact]
    public void Median_EvenCount_ReturnsAverageOfTwoMiddle()
    {
        Assert.Equal(25.0, Statistics.Median(new double[] { 40, 10, 30, 20 }), 6);
    }

    [Fact]
    public void Percentile95_UsesNearestRank()
    {
        var values = Enumerable.Range(1, 20).Select(value => (double)value).ToArray();

        Assert.Equal(19.0, Statistics.Percentile(values, 95), 6);
    }

    [Fact]
    public void Percentile95_FiveValues_ReturnsMaximum()
    {
        Assert.Equal(50.0, Statistics.Percentile(OneToFiftyStep10, 95), 6);
    }

    [Fact]
    public void Percentile_SingleValue_ReturnsThatValue()
    {
        Assert.Equal(7.0, Statistics.Percentile(new[] { 7.0 }, 95), 6);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void Percentile_OutOfRange_Throws(double percent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Statistics.Percentile(OneToFiftyStep10, percent));
    }

    [Fact]
    public void StandardDeviation_UsesSampleFormula()
    {
        Assert.Equal(15.811388, Statistics.StandardDeviation(OneToFiftyStep10), 5);
    }

    [Fact]
    public void StandardDeviation_SingleValue_IsZero()
    {
        Assert.Equal(0.0, Statistics.StandardDeviation(new[] { 42.0 }), 6);
    }

    [Fact]
    public void StandardDeviation_EqualValues_IsZero()
    {
        Assert.Equal(0.0, Statistics.StandardDeviation(new[] { 100.0, 100.0, 100.0 }), 6);
    }

    [Fact]
    public void CoefficientOfVariation_IsDeviationOverMeanInPercent()
    {
        Assert.Equal(52.704628, Statistics.CoefficientOfVariationPercent(OneToFiftyStep10), 5);
    }

    [Fact]
    public void CoefficientOfVariation_ZeroMean_IsZero()
    {
        Assert.Equal(0.0, Statistics.CoefficientOfVariationPercent(new[] { 0.0, 0.0 }), 6);
    }

    [Fact]
    public void AllFunctions_EmptyInput_Throw()
    {
        var empty = Array.Empty<double>();

        Assert.Throws<ArgumentException>(() => Statistics.Mean(empty));
        Assert.Throws<ArgumentException>(() => Statistics.Median(empty));
        Assert.Throws<ArgumentException>(() => Statistics.Percentile(empty, 95));
        Assert.Throws<ArgumentException>(() => Statistics.StandardDeviation(empty));
    }

    [Fact]
    public void Functions_DoNotChangeTheInputOrder()
    {
        var values = new double[] { 30, 10, 20 };

        Statistics.Median(values);
        Statistics.Percentile(values, 95);

        Assert.Equal(new double[] { 30, 10, 20 }, values);
    }
}
