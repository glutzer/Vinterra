using Vintagestory.API.Server;

[Terrain("dunesbadlands")]
public class DuneBadlands : TerrainType
{
    public DuneBadlands(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Initialize(int seed, ICoreServerAPI sapi, float frequencyMultiplier, float strengthMultiplier)
    {
        MultiChooser chooser = new(new Noise(seed + 5, 0.0002f, 3, 0.5f, 2.6f));
        chooser.AddTerrains(new Dunes(sapi), new Dunes(sapi), new Badlands(sapi), new Badlands(sapi));
        chooser.AddThresholds(0, 0.6f, 0.8f, 1);
        module = chooser;
    }
}