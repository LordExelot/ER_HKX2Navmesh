using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly: InternalsVisibleTo("ConvergenceLauncher.Tests")]

namespace ER_HKX2Navmesh.Common
{
    public class SteamUtil
    {
        private const string ELDEN_RING_STEAM_APP_ID = "1245620";
        private const string STEAM_PROCESS_NAME = "steam";

        private static string? _steamInstallationPath;
        private static string? _eldenRingInstallationPath;
        private static string? _eldenRingLanguageSetting;
        private static string? _eldenRingInstallationRoot;

        /// <summary>
        /// Clears all cached paths and manifest data. Call this to force re-detection of installations.
        /// </summary>
        public static void ClearCache()
        {
            _steamInstallationPath = null;
            _eldenRingInstallationPath = null;
            _eldenRingLanguageSetting = null;
            _eldenRingInstallationRoot = null;
        }

        public static string? GetSteamInstallationFolder()
        {
            if (!string.IsNullOrEmpty(_steamInstallationPath))
                return _steamInstallationPath;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return _steamInstallationPath = GetSteamPathWindows();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return _steamInstallationPath = GetSteamPathLinux();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return _steamInstallationPath = GetSteamPathMacOS();

            return null;
        }

        private static string? GetSteamPathWindows()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;

            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam")
                    ?? Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam");

                if (key?.GetValue("InstallPath") is string installPath)
                {
                    var steamExe = Path.Combine(installPath, "steam.exe");
                    if (File.Exists(steamExe))
                        return installPath;
                }
            }
            catch
            {
                Console.WriteLine("Steam Detection: Failed to detect Steam installation.");
            }

            return null;
        }

        private static string? GetSteamPathLinux()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return null;

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // Check typical Linux Steam paths - looking for the Steam directory with steamapps
            var possiblePaths = new[]
            {
                Path.Combine(homeDir, ".steam", "steam"),          // Most common symlink
                Path.Combine(homeDir, ".local", "share", "Steam"), // Flatpak and standard
                Path.Combine(homeDir, "Steam"),                    // Direct home directory
            };

            foreach (var path in possiblePaths)
            {
                var steamappsPath = Path.Combine(path, "steamapps");
                if (Directory.Exists(steamappsPath))
                    return path;  // Return the Steam directory, not the executable
            }

            // If not found in standard locations, try to use 'which steam' to find installation
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "steam",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        // 'which steam' returns /usr/bin/steam, but we need the actual Steam directory
                        // Try to find it by checking common locations again or look in /opt
                        var optPath = "/opt/Steam";
                        if (Directory.Exists(Path.Combine(optPath, "steamapps")))
                            return optPath;
                    }
                }
            }
            catch
            {
                // Silent fail - not critical
            }

            return null;
        }

        private static string? GetSteamPathMacOS()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return null;

            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var steamPath = Path.Combine(homeDir, "Library", "Application Support", "Steam");

            // Check if Steam directory exists with steamapps subdirectory
            var steamappsPath = Path.Combine(steamPath, "steamapps");
            return Directory.Exists(steamappsPath) ? steamPath : null;
        }

        public static bool IsSteamInstalled()
            => !string.IsNullOrEmpty(GetSteamInstallationFolder());

        private static string? GetEldenRingInstallationRoot()
        {
            if (!string.IsNullOrEmpty(_eldenRingInstallationRoot))
                return _eldenRingInstallationRoot;

            var steamPath = GetSteamInstallationFolder();
            if (string.IsNullOrEmpty(steamPath))
                return null;

            var defaultLibrary = Path.Combine(steamPath, "steamapps");
            var defaultManifest = Path.Combine(defaultLibrary, $"appmanifest_{ELDEN_RING_STEAM_APP_ID}.acf");
            if (File.Exists(defaultManifest))
                return _eldenRingInstallationRoot = defaultLibrary;

            var libraryFile = Path.Combine(defaultLibrary, "libraryfolders.vdf");
            if (!File.Exists(libraryFile))
                return null;

            try
            {
                var lines = File.ReadAllLines(libraryFile);
                var libraryFolders = GetVdfDictionary(lines, "libraryfolders");
                if (libraryFolders == null)
                    return null;

                foreach (var libraryEntry in libraryFolders)
                {
                    if (libraryEntry.Value is not Dictionary<string, object> libraryValue)
                        continue;

                    if (!libraryValue.TryGetValue("path", out var pathObj) || pathObj is not string libraryPath)
                        continue;

                    if (!libraryValue.TryGetValue("apps", out var appsObj) || appsObj is not Dictionary<string, object> appsSection)
                        continue;

                    if (appsSection.TryGetValue(ELDEN_RING_STEAM_APP_ID, out var sizeObj) && sizeObj is string sizeStr && long.TryParse(sizeStr, out var size) && size > 0)
                    {
                        return _eldenRingInstallationRoot = Path.Combine(libraryPath, "steamapps");
                    }
                }

                return _eldenRingInstallationRoot = null;
            }
            catch
            {
                return null;
            }
        }

        public static string? GetEldenRingInstallationPath()
        {
            if (!string.IsNullOrEmpty(_eldenRingInstallationPath))
                return _eldenRingInstallationPath;

            var installRoot = GetEldenRingInstallationRoot();
            if (string.IsNullOrEmpty(installRoot))
                return null;

            var installPath = GetEldenRingManifestProperty("installdir");
            if (string.IsNullOrEmpty(installPath))
                return null;

            var path = Path.Combine(installRoot, "common", installPath, "Game");
            var exePath = Path.Combine(path, "eldenring.exe");
            if (!File.Exists(exePath))
                return null;

            return _eldenRingInstallationPath = path;
        }

        public static string? GetEldenRingExePath()
        {
            var installpath = GetEldenRingInstallationPath();
            if (string.IsNullOrEmpty(installpath))
                return null;

            var exePath = Path.Combine(installpath, "eldenring.exe");
            if (!File.Exists(exePath))
                return null;

            return exePath;
        }

        public static bool IsEldenRingInstalled()
            => !string.IsNullOrEmpty(GetEldenRingExePath());

        /// <summary>
        /// Parses VDF (Valve Data Format) lines into a nested dictionary structure.
        /// Handles quoted strings and nested sections with braces.
        /// </summary>
        internal static Dictionary<string, object> ParseVdfLines(string[] fileLines)
        {
            var root = new Dictionary<string, object>();
            var stack = new Stack<Dictionary<string, object>>();
            stack.Push(root);

            try
            {
                string? lastKey = null;

                foreach (var line in fileLines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    if (trimmed == "{")
                    {
                        if (lastKey == null || stack.Count == 0)
                            continue;

                        var newDict = new Dictionary<string, object>();
                        stack.Peek()[lastKey] = newDict;
                        stack.Push(newDict);
                        lastKey = null;
                        continue;
                    }

                    if (trimmed == "}")
                    {
                        if (stack.Count > 1)
                            stack.Pop();
                        lastKey = null;
                        continue;
                    }

                    // Parse quoted strings, preserving empty values
                    var quotedParts = new List<string>();
                    var inQuotes = false;
                    var currentPart = new StringBuilder();

                    foreach (var c in trimmed)
                    {
                        if (c == '"')
                        {
                            if (inQuotes)
                            {
                                quotedParts.Add(currentPart.ToString());
                                currentPart.Clear();
                                inQuotes = false;
                            }
                            else
                            {
                                inQuotes = true;
                            }
                        }
                        else if (inQuotes)
                        {
                            currentPart.Append(c);
                        }
                    }

                    // Filter out empty whitespace-only parts between quotes
                    var parts = quotedParts.Where(p => p.Length > 0 || quotedParts.IndexOf(p) > 0).ToArray();
                    if (parts.Length == 0 || stack.Count == 0)
                        continue;

                    var key = parts[0];
                    if (parts.Length > 1)
                    {
                        stack.Peek()[key] = parts[1];
                        lastKey = null;
                    }
                    else
                    {
                        lastKey = key;
                    }
                }
            }
            catch
            {
                // Return whatever we parsed so far
            }

            return root;
        }

        /// <summary>
        /// Parses a VDF (Valve Data Format) file into a nested dictionary structure.
        /// Handles quoted strings and nested sections with braces.
        /// </summary>
        internal static Dictionary<string, object> ParseVdfFile(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                return ParseVdfLines(lines);
            }
            catch
            {
                return [];
            }
        }

        private static readonly string[] first = ["AppState"];
        private static string? GetEldenRingManifestProperty(params string[] propertyPath)
        {
            if (propertyPath.Length == 0)
                return null;

            var installRoot = GetEldenRingInstallationRoot();
            if (string.IsNullOrEmpty(installRoot))
                return null;

            var manifestPath = Path.Combine(installRoot, $"appmanifest_{ELDEN_RING_STEAM_APP_ID}.acf");
            if (!File.Exists(manifestPath))
                return null;

            try
            {
                var lines = File.ReadAllLines(manifestPath);
                var result = GetVdfProperty(lines, first.Concat(propertyPath).ToArray());
                return result;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a property from VDF content by navigating through the specified property path.
        /// Works with any VDF structure and root key.
        /// Supports both simple path navigation and deep nested searches.
        /// </summary>
        private static string? GetVdfProperty(string[] fileLines, params string[] propertyPath)
        {
            if (propertyPath.Length == 0)
                return null;

            var vdf = ParseVdfLines(fileLines);

            // Navigate through the property path starting from root
            object? current = (object)vdf;

            foreach (var key in propertyPath)
            {
                if (current is not Dictionary<string, object> dict)
                    return null;

                if (!dict.TryGetValue(key, out current))
                    return null;
            }

            return current is string str ? str : null;
        }

        /// <summary>
        /// Gets a dictionary from VDF content by navigating through the specified property path.
        /// Useful for drilling down to nested structures before further processing.
        /// </summary>
        private static Dictionary<string, object>? GetVdfDictionary(string[] fileLines, params string[] propertyPath)
        {
            if (propertyPath.Length == 0)
                return ParseVdfLines(fileLines);

            var vdf = ParseVdfLines(fileLines);

            object? current = (object)vdf;

            foreach (var key in propertyPath)
            {
                if (current is not Dictionary<string, object> dict)
                    return null;

                if (!dict.TryGetValue(key, out current))
                    return null;
            }

            return current as Dictionary<string, object>;
        }
    }
}