try
{
    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
    System.IO.File.WriteAllText(logPath, $"{DateTime.Now} - Starting game...\n");
    using var game = new Dokryun.Game1();
    System.IO.File.AppendAllText(logPath, $"{DateTime.Now} - Game created, calling Run()...\n");
    game.Run();
    System.IO.File.AppendAllText(logPath, $"{DateTime.Now} - Game.Run() returned normally.\n");
}
catch (Exception ex)
{
    var logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
    System.IO.File.WriteAllText(logPath, $"{DateTime.Now}\n{ex}\n");
    Console.Error.WriteLine($"CRASH: {ex}");
    throw;
}
