using System.Globalization;

var count = 1_000_000;
if (args.Length > 0 && int.TryParse(args[0], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
{
    count = parsed;
}

var path = Path.Combine(Path.GetTempPath(), $"sortfile-{Guid.NewGuid():N}.txt");
try
{
    var numbers = new int[count];
    long state = 12345;
    for (var index = 0; index < numbers.Length; index++)
    {
        state = (state * 1103515245 + 12345) & 0x7fffffff;
        numbers[index] = (int)state;
    }

    await File.WriteAllLinesAsync(path, numbers.Select(number => number.ToString(CultureInfo.InvariantCulture)));

    var lines = await File.ReadAllLinesAsync(path);
    var loaded = lines.Select(line => int.Parse(line, CultureInfo.InvariantCulture)).ToArray();
    Array.Sort(loaded);

    Console.WriteLine($"sorted {loaded.Length} numbers, min {loaded[0]}, max {loaded[^1]}");
    return 0;
}
finally
{
    File.Delete(path);
}
