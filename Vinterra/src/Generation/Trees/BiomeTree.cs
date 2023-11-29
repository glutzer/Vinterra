using Vintagestory.ServerMods.NoObf;

public class BiomeTree
{
    public TreeVariant variant;

    //JSON
    public int weight = 10;
    public string code = "mountainpine";

    public double minContinentalness = 0;
    public double maxContinentalness = 3;

    public int minFertility = 0;
    public int maxFertility = 1000;

    public float minY = 0;
    public float maxY = 1;

    public float minForest = 0;
    public float maxForest = 0;

    public float sizeMultiplier = 1;
    public float chance = 1;

    public bool nearRivers = false;
    public double riverDistance = 50;
}