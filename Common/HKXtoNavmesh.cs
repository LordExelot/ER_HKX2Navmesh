using SoulsFormats;
using System.Collections.Concurrent;
using System.Diagnostics;
using static SoulsFormats.DCX;

namespace ER_HKX2Navmesh.Common;

public class HKXtoNavmesh
{
    private const int NVA_NV_UNK00_MAGIC = 1726789910;
    private static readonly string _cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cache");
    private static readonly string _resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
    private static readonly string _generatorPath = Path.Combine(_resourcesPath, "ERNavmeshGenerator.exe");

    public static async Task GenerateNavmeshAsync(List<string> collisionPaths, string outputPath, int map, string mapFileId)
    {
        // Create settings files
        string nNvmSettingsPath = Path.Combine(_cachePath, "n_nav_settings.json");
        string oNvmSettingsPath = Path.Combine(_cachePath, "o_nav_settings.json");
        HkxUtility.SaveNavmeshGenerationSettings(HkxUtility.GetDefaultNavmeshGenerationSnapshot(), nNvmSettingsPath);
        HkxUtility.SaveNavmeshGenerationSettings(HkxUtility.GetLodNavmeshGenerationSnapshot(), oNvmSettingsPath);

        // Create NVA and NVBND instances
        NVA nva = CreateNVA();
        BND4 nvmhktbnd = CreateNVBND();

        for (int i = 0; i < collisionPaths.Count; i++)
        {
            string hkxPath = collisionPaths[i];
            int navmeshModelId = (i * 100) + 1000;
            string navFilePathBase = Path.Combine(_cachePath, Path.GetFileName(hkxPath));
            string nnavPath = Path.ChangeExtension(navFilePathBase, ".n.nav");
            string onavPath = Path.ChangeExtension(navFilePathBase, ".o.nav");

            try
            {
                nva.Navmeshes.Add(CreateNavmeshEntry(navmeshModelId));
                nvmhktbnd.Files.Add(await CreateNavmeshFileAsync(hkxPath, 'n', 10000 + i, nnavPath, nNvmSettingsPath, navmeshModelId, mapFileId));
                nvmhktbnd.Files.Add(await CreateNavmeshFileAsync(hkxPath, 'o', 20000 + i, onavPath, oNvmSettingsPath, navmeshModelId, mapFileId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {hkxPath}: {ex.Message}");
                return;
            }
            finally
            {
                if (File.Exists(nnavPath)) File.Delete(nnavPath);
                if (File.Exists(onavPath)) File.Delete(onavPath);
            }
        }

        nva.Write(Path.Combine(outputPath, "map", $"m{map:D2}", $"m{mapFileId}", $"m{mapFileId}.nva.dcx"));
        nvmhktbnd.Write(Path.Combine(outputPath, "map", $"m{map:D2}", $"m{mapFileId}", $"m{mapFileId}.nvmhktbnd.dcx"));
    }

    private static async Task<BinderFile> CreateNavmeshFileAsync(string hkxPath, char internalNamePrefix, int id, string navPath, string nvmSettingsPath, int navmeshModelId, string mapFileId)
    {
        await HKXtoNAV(hkxPath, navPath, nvmSettingsPath);

        return new()
        {
            Bytes = await File.ReadAllBytesAsync(navPath),
            ID = id,
            Name = $"N:\\GR\\data\\INTERROOT_win64\\map\\m{mapFileId}\\navimesh\\bind6\\{internalNamePrefix}{mapFileId}_{navmeshModelId:D6}.hkx"
        };
    }

    private static async Task HKXtoNAV(string hkxPath, string navPath, string settingsPath)
    {
        var gamePath = SteamUtil.GetEldenRingExePath();
        if (gamePath == null)
        {
            Console.WriteLine("Elden Ring installation not found. Please ensure Elden Ring is installed and try again.");
            return;
        }

        ProcessStartInfo startInfo = new(_generatorPath, $"-g \"{gamePath}\" -i \"{hkxPath}\" -o \"{navPath}\" -s \"{settingsPath}\"")
        {
            WorkingDirectory = _resourcesPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            Console.WriteLine($"Failed to start process: {_generatorPath}");
            return;
        }

        await process.WaitForExitAsync();
        if (process.ExitCode == 0)
            return;

        string error = await process.StandardError.ReadToEndAsync();
        string output = await process.StandardOutput.ReadToEndAsync();
        Console.WriteLine($"Error: ERNavmeshGenerator exited with code {process.ExitCode}.");
        if (!string.IsNullOrWhiteSpace(error))
            Console.WriteLine($"Standard Error:\n{error}");
        if (!string.IsNullOrWhiteSpace(output))
            Console.WriteLine($"Standard Output:\n{output}");
    }

    #region Creation Helpers
    private static NVA CreateNVA()
    {
        NVA nva = new() { Compression = new DcxKrakCompressionInfo(KrakCompressionPreset.EldenRing) };
        nva.Navmeshes.Version = 4;
        nva.Entries11.Add(new()
        {
            Unk00 = NVA_NV_UNK00_MAGIC,
            Unk04 = 0,
            Unk08 = 0,
            Unk0C = 0
        });
        return nva;
    }

    private static BND4 CreateNVBND()
    {
        return new()
        {
            Compression = new DcxKrakCompressionInfo(KrakCompressionPreset.EldenRing),
            Version = "07D7R6"
        };
    }

    private static NVA.Navmesh CreateNavmeshEntry(int navmeshModelId)
    {
        return new()
        {
            NameID = int.Parse($"1{navmeshModelId:D6}"),
            ModelID = navmeshModelId,
            IsConnectedNavmeshesInline = true,
            Position = new(0f, 0f, 0f, 1f),
            Rotation = new(0f),
            Scale = new(1f, 1f, 1f, 0f),
            FaceCount = 1,
            Unk3C = 0,
            Unk4C = 0,
            Unk44 = 1075419545
        };
    }

    #endregion
}