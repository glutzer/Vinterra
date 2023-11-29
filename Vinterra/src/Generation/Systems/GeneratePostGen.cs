using System.Diagnostics;
using Vintagestory.API.Server;

/// <summary>
/// After the chunk has finished generating.
/// </summary>
public class GeneratePostGen : WorldGenBase
{
    public ICoreServerAPI sapi;

    public override double ExecuteOrder() => 50;

    public Stopwatch stopwatch;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        sapi.Event.ChunkColumnGeneration(ChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");

        stopwatch = new Stopwatch();
        stopwatch.Start();
    }

    public void ChunkColumnGeneration(IChunkColumnGenerateRequest request)
    {
        if (stopwatch.ElapsedMilliseconds < 20000) return;
        GenerateData.generationDataCache.Clear();
        GenerateData.erodedGenerationDataCache.Clear();
        stopwatch.Restart();
    }
}