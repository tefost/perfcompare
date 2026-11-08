using PerfCompare.Runner;

using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    if (cancellation.IsCancellationRequested)
    {
        return;
    }

    eventArgs.Cancel = true;
    cancellation.Cancel();
};

return await CliApp.RunAsync(args, Console.Out, Console.Error, cancellation: cancellation.Token);
