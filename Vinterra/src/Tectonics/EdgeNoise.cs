using Vintagestory.ServerMods;
/// <summary>
/// Gets distortion for biome edges.
/// </summary>
public class EdgeNoise
{
    public Noise broadNoiseX;
    public Noise broadNoiseZ;
    public int broadWeight;

    public Noise edgeNoiseX;
    public Noise edgeNoiseZ;
    public int edgeWeight;

    public EdgeNoise(int seed)
    {
        broadNoiseX = new Noise(seed + 48, VConfig.Loaded.broadEdgeFrequency, VConfig.Loaded.broadEdgeOctaves, VConfig.Loaded.broadEdgeGain, VConfig.Loaded.broadEdgeLacunarity);
        broadNoiseZ = new Noise(seed + 64, VConfig.Loaded.broadEdgeFrequency, VConfig.Loaded.broadEdgeOctaves, VConfig.Loaded.broadEdgeGain, VConfig.Loaded.broadEdgeLacunarity);
        broadWeight = VConfig.Loaded.broadEdgeWeight;

        edgeNoiseX = new Noise(seed + 16, VConfig.Loaded.edgeFrequency, VConfig.Loaded.edgeOctaves, VConfig.Loaded.edgeGain, VConfig.Loaded.edgeLacunarity);
        edgeNoiseZ = new Noise(seed + 32, VConfig.Loaded.edgeFrequency, VConfig.Loaded.edgeOctaves, VConfig.Loaded.edgeGain, VConfig.Loaded.edgeLacunarity);
        edgeWeight = VConfig.Loaded.edgeWeight;
    }

    public double GetX(int x, int y)
    {
        return broadNoiseX.GetNoise(x, y) * broadWeight + edgeNoiseX.GetNoise(x, y) * edgeWeight;
    }

    public double GetZ(int x, int y)
    {
        return broadNoiseZ.GetNoise(x, y) * broadWeight + edgeNoiseZ.GetNoise(x, y) * edgeWeight;
    }
}