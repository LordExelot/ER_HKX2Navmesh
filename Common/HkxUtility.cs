using ERNavmeshGenCS;
using System.Text.Json;

namespace ER_HKX2Navmesh.Common;

public static class HkxUtility
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true, // For better readability
        IncludeFields = true, // Include fields in serialization
    };

    public static hkaiNavMeshGenerationSnapshot GetDefaultNavmeshGenerationSnapshot()
    {
        // Initialize the elden ring dll so we can construct the settings
        var _ = new ERNavmeshGen();

        // This takes care of all constructors for the snapshot and generation settings
        hkaiNavMeshGenerationSnapshot snapshot = new();

        // set the up for Elden Ring
        snapshot.settings.up = new hkVector4 { x = 0f, y = 1f, z = 0f, w = 0f };
        // IDK what this does but it was set in the original code 12th gave me...
        snapshot.settings.precalculateClearanceSeedingData = true;

        // This snapshot can be loaded up in havok content tools, and I think modified and then ran through
        // another function I will have to add to generate from snapshot.
        // snapshot.settings.saveInputSnapshot = true;
        // snapshot.settings.snapshotFilename = "C:\\Temp\\debug_input.hkx";

        // This snapshot didn't seem to do anything???? Does this mean the simplification isn't happening or maybe it
        // needs to be turned on with a flag in the settings here?
        // snapshot.settings.simplificationSettings.saveInputSnapshot = true;
        // snapshot.settings.simplificationSettings.snapshotFilename = "C:\\Temp\\debug_simplified.hkx";

        return snapshot;
    }
    public static hkaiNavMeshGenerationSnapshot GetLodNavmeshGenerationSnapshot()
    {
        // Initialize the elden ring dll so we can construct the settings
        var _ = new ERNavmeshGen();

        // This takes care of all constructors for the snapshot and generation settings
        hkaiNavMeshGenerationSnapshot snapshot = new();

        // set the up for Elden Ring
        snapshot.settings.up = new hkVector4 { x = 0f, y = 1f, z = 0f, w = 0f };
        // IDK what this does but it was set in the original code 12th gave me...
        snapshot.settings.precalculateClearanceSeedingData = true;

        // This snapshot can be loaded up in havok content tools, and I think modified and then ran through
        // another function I will have to add to generate from snapshot.
        // snapshot.settings.saveInputSnapshot = true;
        // snapshot.settings.snapshotFilename = "C:\\Temp\\debug_input.hkx";

        // This snapshot didn't seem to do anything???? Does this mean the simplification isn't happening or maybe it
        // needs to be turned on with a flag in the settings here?
        // snapshot.settings.simplificationSettings.saveInputSnapshot = true;
        // snapshot.settings.simplificationSettings.snapshotFilename = "C:\\Temp\\debug_simplified.hkx";

        return snapshot;
    }
    public static void SaveNavmeshGenerationSettings(hkaiNavMeshGenerationSnapshot snapshot, string outputPath)
    {
        if (File.Exists(outputPath))
            return; // Don't overwrite existing settings, as they might have been modified by the user

        // Serialize to a JSON string and write to disk  
        string jsonOutput = JsonSerializer.Serialize(snapshot, jsonOptions);
        File.WriteAllText(outputPath, jsonOutput);
    }
}