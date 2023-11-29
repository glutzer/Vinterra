public class VConfig
{
    public static VConfig Loaded { get; set; } = new VConfig();

    //Multiplier of each "zone" (biome / terrain) in a plate
    public int zoneSize = 2048;
    public int zonesInPlate = 16;

    public int riverGrowth = 7; //Amount river will grow traversing 1 zone
    public int riverSpawnChance = 100; //Chance for river to spawn at the edge of the coast
    public int riverSplitChance = 100; //Chance for river to split at the center of a region
    public int lakeChance = 100; //Chance for ends of rivers to spawn a lake
    public int segmentsInRiver = 5; //How many segments each river is composed of
    public double segmentOffset = 200; //How much to offset each segment along the river line

    public double riverDepth = 0.006; //Based on the square root of the river size
    public double baseDepth = 0.02;

    public int temperatureDivisor = 2;
    public int rainDivisor = 10;

    //Y level of valid sand in oceanic areas
    public float beachFrequency = 0.01f;

    //Larger offset of zones/rivers
    public int broadEdgeOctaves = 2;
    public float broadEdgeFrequency = 0.0001f;
    public int broadEdgeWeight = 500;
    public float broadEdgeGain = 0.3f;
    public float broadEdgeLacunarity = 2.6f;

    //Frequent offsets of zones/rivers
    public int edgeOctaves = 3;
    public float edgeFrequency = 0.001f;
    public int edgeWeight = 80;
    public float edgeGain = 0.4f;
    public float edgeLacunarity = 2.6f;

    //Erosion settings
    public float erosionHeightFalloff = 0.4f;
    public float erosionMinHeight = 0.05f;
    public int erosionRadius = 7;
    public float erosionInertia = 0.005f;
    public float sedimentCapacityFactor = 7;
    public float minSedimentCapacity = 0.005f;
    public float erodeSpeed = 1f;
    public float depositSpeed = 0.5f;
    public float evaporateSpeed = 0.05f;
    public float gravity = 3f;
    public int maxDropletLifetime = 12;
    public float initialWaterVolume = 1;
    public float initialSpeed = 0.7f;
    public int erosionDroplets = 1000;

    //Amount of blocks to boost inland
    public int continentalnessBoost = 3;

    //Max 32
    public int biomeScatterRadius = 12;

    public bool biomeGui = true;

    public double seaWaterContinentalness = 1;

    public int borderDistanceThreshold = 400;

    public int zonePadding = 300;
}