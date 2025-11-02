using System.Diagnostics;

Console.WriteLine("=== Loyalty Software Application Host ===");
Console.WriteLine("Starting API and Frontend services...");

var processes = new List<Process>();
var cancellationTokenSource = new CancellationTokenSource();

// Handle Ctrl+C gracefully
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
};

try
{
    // Get the solution root directory
    var currentDir = Directory.GetCurrentDirectory();
    
    // If we're in the AppHost directory, go up two levels to reach the solution root
    var solutionRoot = currentDir;
    if (currentDir.EndsWith("App.AppHost"))
    {
        solutionRoot = Path.GetFullPath(Path.Combine(currentDir, "../.."));
    }
    
    Console.WriteLine($"Current directory: {currentDir}");
    Console.WriteLine($"Solution root: {solutionRoot}");
    
    // Start API first
    Console.WriteLine("Starting API service on https://localhost:7001...");
    var apiPath = Path.Combine(solutionRoot, "src", "App.Api");
    Console.WriteLine($"API path: {apiPath}");
    var apiProcess = StartProject(apiPath, "https://localhost:7001", "API");
    processes.Add(apiProcess);

    // Wait a bit for API to start
    await Task.Delay(3000);

    // Start Frontend
    Console.WriteLine("Starting Frontend service on https://localhost:7000...");
    var frontendPath = Path.Combine(solutionRoot, "src", "App.Frontend");
    Console.WriteLine($"Frontend path: {frontendPath}");
    var frontendProcess = StartProject(frontendPath, "https://localhost:7000", "Frontend");
    processes.Add(frontendProcess);

    Console.WriteLine();
    Console.WriteLine("✅ Both applications started successfully!");
    Console.WriteLine("🌐 API: https://localhost:7001");
    Console.WriteLine("🖥️  Frontend: https://localhost:7000");
    Console.WriteLine();
    Console.WriteLine("Press Ctrl+C to stop all services...");

    // Keep running until cancellation
    while (!cancellationTokenSource.Token.IsCancellationRequested)
    {
        await Task.Delay(5000);
        
        // Check if any process has exited
        foreach (var process in processes.ToList())
        {
            if (process.HasExited)
            {
                Console.WriteLine($"⚠️  {process.ProcessName} has exited with code {process.ExitCode}");
                cancellationTokenSource.Cancel();
                break;
            }
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}
finally
{
    Console.WriteLine("Stopping all services...");
    foreach (var process in processes)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill();
                await process.WaitForExitAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping process: {ex.Message}");
        }
    }
    Console.WriteLine("All services stopped.");
}

static Process StartProject(string projectPath, string url, string name)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --urls \"{url}\"",
        WorkingDirectory = projectPath,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = false
    };

    var process = Process.Start(startInfo);
    if (process == null)
    {
        throw new InvalidOperationException($"Failed to start {name}");
    }

    // Log output asynchronously
    _ = Task.Run(async () =>
    {
        try
        {
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"[{name}] {line}");
                }
            }
        }
        catch { /* Ignore errors when process is killed */ }
    });

    _ = Task.Run(async () =>
    {
        try
        {
            while (!process.StandardError.EndOfStream)
            {
                var line = await process.StandardError.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    Console.WriteLine($"[{name} ERROR] {line}");
                }
            }
        }
        catch { /* Ignore errors when process is killed */ }
    });

    return process;
}
