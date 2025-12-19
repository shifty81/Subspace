using System;
using System.IO;

try
{
    using var game = new Subspace.Game1();
    game.Run();
}
catch (Microsoft.Xna.Framework.Graphics.NoSuitableGraphicsDeviceException ex)
{
    // Log graphics device errors (e.g., no display server, missing OpenGL)
    string errorMessage = $"""
        Failed to initialize graphics device.

        This usually happens when:
        1. Running on a system without a display (headless server)
        2. Display server (X11/Wayland) is not running
        3. OpenGL drivers are not installed or accessible
        4. Running in a container without graphics support

        Technical details: {ex.Message}
        Inner exception: {ex.InnerException?.Message}
        """;
    
    Console.Error.WriteLine("===== SUBSPACE CRASH REPORT =====");
    Console.Error.WriteLine(errorMessage);
    Console.Error.WriteLine("\nFull stack trace:");
    Console.Error.WriteLine(ex.ToString());
    Console.Error.WriteLine("=================================");
    
    // Also write to a crash log file
    WriteCrashLog(errorMessage, ex);
    
    Environment.Exit(1);
}
catch (Exception ex)
{
    // Catch all other exceptions
    string errorMessage = $"An unexpected error occurred: {ex.Message}";
    
    Console.Error.WriteLine("===== SUBSPACE CRASH REPORT =====");
    Console.Error.WriteLine(errorMessage);
    Console.Error.WriteLine("\nFull stack trace:");
    Console.Error.WriteLine(ex.ToString());
    Console.Error.WriteLine("=================================");
    
    // Also write to a crash log file
    WriteCrashLog(errorMessage, ex);
    
    Environment.Exit(1);
}

static void WriteCrashLog(string errorMessage, Exception ex)
{
    try
    {
        string logFile = Path.Combine(Directory.GetCurrentDirectory(), "crash.log");
        using var writer = new StreamWriter(logFile, false);
        writer.WriteLine("===== SUBSPACE CRASH REPORT =====");
        writer.WriteLine($"Date/Time: {DateTime.Now}");
        writer.WriteLine($"OS: {Environment.OSVersion}");
        writer.WriteLine($".NET Version: {Environment.Version}");
        writer.WriteLine();
        writer.WriteLine(errorMessage);
        writer.WriteLine();
        writer.WriteLine("Full stack trace:");
        writer.WriteLine(ex.ToString());
        writer.WriteLine("=================================");
        
        Console.Error.WriteLine($"\nCrash log written to: {logFile}");
    }
    catch
    {
        // If we can't write the log file, just continue
        Console.Error.WriteLine("\nWarning: Could not write crash log file.");
    }
}
