using PerfCompare.Server.Api;

namespace PerfCompare.Server.Tests;

public class CsvWriterTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("baseline", "baseline")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("line1\nline2", "\"line1\nline2\"")]
    public void Field_QuotesOnlyWhenNeeded(string? value, string expected)
    {
        Assert.Equal(expected, CsvWriter.Field(value));
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("+cmd", "'+cmd")]
    [InlineData("-2", "'-2")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    public void Field_NeutralizesFormulaPrefixes(string value, string expected)
    {
        Assert.Equal(expected, CsvWriter.Field(value));
    }

    [Fact]
    public void Number_UsesInvariantCultureAndEmptyForNull()
    {
        Assert.Equal("812.4", CsvWriter.Number(812.4));
        Assert.Equal(string.Empty, CsvWriter.Number(null));
        Assert.Equal("3", CsvWriter.Integer(3));
        Assert.Equal(string.Empty, CsvWriter.Integer(null));
    }
}
