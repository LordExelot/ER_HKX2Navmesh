using ER_HKX2Navmesh.Common;
using System.Diagnostics;

namespace ER_HKX2Navmesh;

class Program
{
    private static readonly ToolSettings _settings = ToolSettings.Instance;

    static async Task Main(string[] args)
    {
        await MainWorker([ .. args ]);
        Console.WriteLine("This window will automatically close in 5 seconds or when a key is pressed.");

        var delayTask = Task.Delay(5000);
        var keyTask = Task.Run(() => Console.ReadKey(true));
        await Task.WhenAny(delayTask, keyTask);

        Environment.Exit(0);
    }

    private static async Task MainWorker(List<string>? collisionPaths)
    { 
        if (collisionPaths == null || collisionPaths.Count == 0)
        {
            if (!Debugger.IsAttached)
            {
                Console.WriteLine("No collision file path provided. Please provide a path to a collision HKX file as an argument.");
                return;
            }
            collisionPaths = [@"C:\Users\Jesse\Downloads\n12_01_00_00_000100.hkx"];
        }

        var outputPath = await InputProcessor.GetOutputPath(collisionPaths);

        var (map, area, region, block) = InputProcessor.GetMapId();
        string mapFileId = $"{map:D2}_{area:D2}_{region:D2}_{block:D2}";
        _settings.LastMapId = mapFileId;
        await _settings.SaveAsync();

        var outputDir = Path.Combine(outputPath, "map", $"m{map:D2}", $"m{mapFileId}");
        var shouldMerge = InputProcessor.GetShouldMerge(outputDir, mapFileId);
        if (shouldMerge)
            Console.WriteLine("Merging enabled: Generated navmesh will be merged with the existing NVA");

        Console.WriteLine($"Generating navmesh for m{mapFileId}");
        Console.WriteLine("Processing...");
        Console.WriteLine();

        await HKXtoNavmesh.GenerateNavmeshAsync(collisionPaths, outputDir, mapFileId, shouldMerge);

        Console.WriteLine("Files created succesfully");
        Console.WriteLine("Output Path: " + outputDir + "\\");
        Console.WriteLine();
    }
}