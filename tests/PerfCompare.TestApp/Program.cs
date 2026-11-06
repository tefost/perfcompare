using System.Globalization;

if (args.Length < 2)
{
    await Console.Error.WriteLineAsync("usage: exit <code> | sleep <seconds>");
    return 64;
}

switch (args[0])
{
    case "exit":
        return int.Parse(args[1], CultureInfo.InvariantCulture);
    case "sleep":
        await Task.Delay(TimeSpan.FromSeconds(double.Parse(args[1], CultureInfo.InvariantCulture)));
        return 0;
    default:
        await Console.Error.WriteLineAsync("unknown mode");
        return 64;
}
