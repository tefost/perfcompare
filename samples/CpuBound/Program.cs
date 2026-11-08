using System.Globalization;

var limit = 2_000_000;
if (args.Length > 0 && int.TryParse(args[0], NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 1)
{
    limit = parsed;
}

var primes = 0;
for (var candidate = 2; candidate <= limit; candidate++)
{
    if (IsPrime(candidate))
    {
        primes++;
    }
}

Console.WriteLine($"primes up to {limit}: {primes}");
return 0;

static bool IsPrime(int value)
{
    for (var divisor = 2; (long)divisor * divisor <= value; divisor++)
    {
        if (value % divisor == 0)
        {
            return false;
        }
    }

    return true;
}
