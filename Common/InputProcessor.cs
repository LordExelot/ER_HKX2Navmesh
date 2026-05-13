namespace ER_HKX2Navmesh.Common;

public class InputProcessor
{
    private static readonly ToolSettings _settings = ToolSettings.Instance;
    public static (bool success, int map, int area, int region, int block) ParseMapAreaRegionBlock(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return (false, 0, 0, 0, 0);

        // Remove unwanted characters and split by common delimiters
        var cleanedInput = input.Replace("m", "").Replace("_", " ").Replace(",", " ");
        var parts = cleanedInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
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

    public static async Task<string> GetOutputPath(List<string> collisionPaths)
    {
        var outputPath = Path.Combine(Path.GetDirectoryName(collisionPaths.First())!, "navgenOutput");
        if (!string.IsNullOrEmpty(_settings.OutputPath))
            outputPath = _settings.OutputPath;
        else
        {
            Console.WriteLine("No output path set. Please provide a path to save the generated navmesh." +
                $"\nSuggestion: Your \'\\mod\' folder" +
                $"\nLeaving it empty will make it save in {outputPath}");
            Console.WriteLine();
            while (true)
            {
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    Console.WriteLine($"No output path provided. Using default output path: {outputPath}");
                    break;
                }
                else if (!Directory.Exists(input))
                {
                    Console.WriteLine("Provided path does not exist. Please enter a valid output path.");
                    continue;
                }

                outputPath = input;
                _settings.OutputPath = outputPath;
                await _settings.SaveAsync();
                break;
            }
            Console.WriteLine();
        }
        return outputPath;
    }

    public static (int map, int area, int region, int block) GetMapId()
    {
        Console.WriteLine("Enter Map, Area, Region and Block." +
            "\nAccepted patterns:" +
            "\nm11_10_00_00" +
            "\n11 10 00 0" +
            "\n11,10,0,00");
        if (_settings.LastMapId != null)
        {
            Console.WriteLine($"Last used value: m{_settings.LastMapId}" +
                $"\nPress enter without filling anything to reuse the last value.");
        }
        Console.WriteLine();

        while (true)
        {
            var input = Console.ReadLine();
            if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(_settings.LastMapId))
                input = _settings.LastMapId;

            var (success, _map, _area, _region, _block) = ParseMapAreaRegionBlock(input);

            if (!success)
            {
                Console.WriteLine("Invalid input format. Please enter in the format 'm11_10_00_00' or similar.");
                continue;
            }

            Console.WriteLine();
            return (_map, _area, _region, _block);
        }
    }
}