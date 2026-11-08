using System.Globalization;
using System.Security.Cryptography;

var failPercent = 30;
if (args.Length > 0 && int.TryParse(args[0], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed <= 100)
{
    failPercent = parsed;
}

await Task.Delay(100);

if (RandomNumberGenerator.GetInt32(100) < failPercent)
{
    await Console.Error.WriteLineAsync("flaky: simulated failure");
    return 1;
}

Console.WriteLine("flaky: ok");
return 0;
