using ER_HKX2Navmesh.Common;
using ERNavmeshGenCS;
using SoulsFormats;
using System.Diagnostics;
using static SoulsFormats.DCX;

namespace ER_HKX2Navmesh;

class Program
{
    public static readonly int NVA_NV_UNK00_MAGIC = 1726789910;
    public static string CACHE_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");

    static async Task Main(string[] args)
    {
        var collisionPath = args.FirstOrDefault();
        await NAVtoNVA(collisionPath);
        Console.WriteLine("Press any key to close...");
        Console.ReadKey();
        Environment.Exit(0);
    }

    private static async Task NAVtoNVA(string? collisionPath)
    {
        if (collisionPath == null)
        {
            if (!Debugger.IsAttached)
            {
                Console.WriteLine("No collision file path provided. Please provide a path to a collision HKX file as an argument.");
                return;
            }
            collisionPath = @"C:\Users\Jesse\Downloads\n31_83_00_00_000100.hkx";
        }

        int map, area, region, block;
        Console.WriteLine("Enter Map, Area, Region and Block." +
            "\nAccepted patterns:" +
            "\nm11_10_00_00" +
            "\n11 10 00 0" +
            "\n11,10,0,00");
        Console.WriteLine();
        while (true)
        {
            var input = Console.ReadLine();
            var (success, _map, _area, _region, _block) = ParseMapAreaRegionBlock(input);

            if (!success)
            {
                Console.WriteLine("Invalid input format. Please enter in the format 'm31_83_00_00' or similar.");
                continue;
            }

            map = _map;
            area = _area;
            region = _region;
            block = _block;
            break;
        }

        Console.WriteLine();
        Console.WriteLine($"Generating navmesh for m{map:D2}_{area:D2}_{region:D2}_{block:D2}");
        Console.WriteLine("Processing...");
        Console.WriteLine();

        /* Some vars */
        int nextNavId = int.Parse($"1{area:D2}{region:D2}00000");
        string mid = $"{map:D2}_{area:D2}_{region:D2}_{block:D2}";
        string navFilePathBase = Path.Combine(CACHE_PATH, 'n' + Path.GetFileName(collisionPath)[1..]);
        string nnavPath = Path.ChangeExtension(navFilePathBase, ".n.nav");
        string onavPath = Path.ChangeExtension(navFilePathBase, ".o.nav");


        Directory.CreateDirectory(CACHE_PATH);

        await GenerateNAVs(new List<string> { collisionPath });

        /* Create NVA */
        NVA nva = new() { Compression = new DcxKrakCompressionInfo(KrakCompressionPreset.EldenRing) };
        nva.Navmeshes.Version = 4;
        nva.Entries11.Add(new()
        {
            Unk00 = NVA_NV_UNK00_MAGIC,
            Unk04 = 0,
            Unk08 = 0,
            Unk0C = 0
        });

        /* Create NVBND */
        BND4 nvbnd = new()
        {
            Compression = new DcxKrakCompressionInfo(KrakCompressionPreset.EldenRing),
            Version = "07D7R6"
        };

        /* Add navmesh entry to NVA */
        int nextN = int.Parse($"{area:D2}{region:D2}{0:D2}");
        nva.Navmeshes.Add(new()
        {
            NameID = nextNavId,
            ModelID = nextN,
            IsConnectedNavmeshesInline = true,
            Position = new(0f, 0f, 0f, 1f),
            Rotation = new(0f),
            Scale = new(1f, 1f, 1f, 0f),
            FaceCount = 1, // might be unnesscary? doesn't really seem to do anything?
            Unk3C = 0,
            Unk4C = 0,
            Unk44 = 1075419545
        });

        /* Add navmesh file to NVBND */
        var nnavBytesTask = File.ReadAllBytesAsync(nnavPath);
        var onavBytesTask = File.ReadAllBytesAsync(onavPath);
        await Task.WhenAll(nnavBytesTask, onavBytesTask);
        nvbnd.Files.AddRange(
        [
            new()
            {
                Bytes = nnavBytesTask.Result,
                ID = 10000,
                Name = $"N:\\GR\\data\\INTERROOT_win64\\map\\m{mid}\\navimesh\\bind6\\n{mid}_{nextN:D6}.hkx"
            },
            new()
            {
                Bytes = onavBytesTask.Result,
                ID = 20000,
                Name = $"N:\\GR\\data\\INTERROOT_win64\\map\\m{mid}\\navimesh\\bind6\\o{mid}_{nextN:D6}.hkx"
            }
        ]);

        /* Write Files */
        var outputPath = Path.Combine(Path.GetDirectoryName(collisionPath)!, "navgenOutput");
        nva.Write(Path.Combine(outputPath, "map", $"m{map:D2}", $"m{mid}", $"m{mid}.nva.dcx"));
        nvbnd.Write(Path.Combine(outputPath, "map", $"m{map:D2}", $"m{mid}", $"m{mid}.nvmhktbnd.dcx"));

        Console.WriteLine("Files created succesfully");
        Console.WriteLine("Output Path: " + outputPath);
    }


    private static async Task GenerateNAVs(List<string> hkxFilePaths)
    {
        /* Write navmesh settings */
        hkaiNavMeshGenerationSnapshot nNavmeshSettings = HkxUtility.GetDefaultNavmeshGenerationSnapshot();
        hkaiNavMeshGenerationSnapshot oNavmeshSettings = HkxUtility.GetLodNavmeshGenerationSnapshot();
        string nNvmSettingsPath = Path.Combine(CACHE_PATH, "n_nav_settings.json");
        string oNvmSettingsPath = Path.Combine(CACHE_PATH, "o_nav_settings.json");
        HkxUtility.SaveNavmeshGenerationSettings(nNavmeshSettings, nNvmSettingsPath);
        HkxUtility.SaveNavmeshGenerationSettings(oNavmeshSettings, oNvmSettingsPath);

        /* HKX -> NAV conversion of navmeshes */
        foreach (string hkxPath in hkxFilePaths)
        {
            string hkxCachePath = Path.Combine(CACHE_PATH, Path.GetFileName(hkxPath));
            string nnavPath = Path.ChangeExtension(hkxCachePath, ".n.nav");
            string onavPath = Path.ChangeExtension(hkxCachePath, ".o.nav");
            if (File.Exists(nnavPath) && File.Exists(onavPath))
                continue; // if debug_reuse is on, skip if file already created

            await HKXtoNAV(hkxPath, nnavPath, nNvmSettingsPath);
            await HKXtoNAV(hkxPath, onavPath, oNvmSettingsPath);
        }
    }
    private static async Task HKXtoNAV(string hkxPath, string navPath, string settingsPath)
    {
        var gamePath = SteamUtil.GetEldenRingExePath();
        if (gamePath == null)
        {
            Console.WriteLine("Elden Ring installation not found. Please ensure Elden Ring is installed and try again.");
            return;
        }

        var resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        var generatorPath = Path.Combine(resourcesPath, "ERNavmeshGenerator.exe");

        ProcessStartInfo startInfo = new(generatorPath, $"-g \"{gamePath}\" -i \"{hkxPath}\" -o \"{navPath}\" -s \"{settingsPath}\"")
        {
            WorkingDirectory = resourcesPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            Console.WriteLine($"Failed to start process: {generatorPath}");
            return;
        }

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync();
            string output = await process.StandardOutput.ReadToEndAsync();
            Console.WriteLine($"Error: ERNavmeshGenerator exited with code {process.ExitCode}.");
            if (!string.IsNullOrWhiteSpace(error))
                Console.WriteLine($"Standard Error:\n{error}");
            if (!string.IsNullOrWhiteSpace(output))
                Console.WriteLine($"Standard Output:\n{output}");
        }
    }

    private static (bool success, int map, int area, int region, int block) ParseMapAreaRegionBlock(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return (false, 0, 0, 0, 0);
        }

        // Remove unwanted characters and split by common delimiters
        var cleanedInput = input.Replace("m", "").Replace("_", " ").Replace(",", " ");
        var parts = cleanedInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 ||
            !int.TryParse(parts[0], out int map) ||
            !int.TryParse(parts[1], out int area) ||
            !int.TryParse(parts[2], out int region) ||
            !int.TryParse(parts[3], out int block))
        {
            return (false, 0, 0, 0, 0);
        }
        return (true, map, area, region, block);
    }
}