using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

public class BiomeType
{
    public ICoreServerAPI sapi;
    public BiomeSystem biomeSystem;
    public LCGRandom random;

    public string name = "Undefined";

    //If a river can generate through the center of this zone
    public bool riversAllowed = true;
    
    //Blocks near water and height at which a block will be considered a beach
    public int[] beachLayerIds; //Must be resolved
    public double beachHeight = 0.01;

    //Blocks on edges of rivers
    public int[] riverLayerIds; //Must be resolved
    public double riverDistance = 0.5;

    //Ambience
    public float[] fogColor = new float[] { 1, 1, 1 };
    public float fogDensity = 0;
    public float fogWeight = 0;
    public float[] ambientColor = new float[] { 1, 1, 1 };
    public float ambientWeight = 0;

    //Min and max climate
    public bool coastalOnly = false;
    public bool inlandOnly = false;
    public int minTemperature = 0;
    public int maxTemperature = 255;
    public int minRain = 0;
    public int maxRain = 255;

    //Weight
    public int weight = 100;

    //Terrain type for generation
    public List<TerrainType> terrainList = new();

    //Trees
    public float treeDensity = 1;
    public BiomeTree[] trees = Array.Empty<BiomeTree>();
    public BiomeTree[] shrubs = Array.Empty<BiomeTree>();

    //Features (testing) stuff like boulders
    public PartialFeature[] biomeFeatures = Array.Empty<PartialFeature>();

    //For getting the layers of the biome surface
    public Decorator[] surfaceDecorators = Array.Empty<Decorator>();
    public Decorator[] gradientSurfaceDecorators = Array.Empty<Decorator>();
    public double gradientThreshold = 3;

    //Lakes
    public float lakeChance = 1;
    public int lakeTries = 0;
    public int lakeMinSize = 50;
    public int lakeMaxSize = 100;
    public int lakeOffset = 1000;

    //Ponds
    public bool hasPonds = true;
    public float pondFrequency = 1;
    public int waterId;
    public int bedId;

    //Misc
    public bool erosion = false;
    public bool spawnPatches = true;
    public bool spawnGrass = true;
    public bool surfaceCaves = true;

    public string[] patchTags = Array.Empty<string>();

    public BiomeType()
    {
    }

    /// <summary>
    /// Checks if conditions are valid for this biome.
    /// </summary>
    public bool IsInRange(double temperature, double rain, bool coastal)
    {
        if (temperature < minTemperature || temperature > maxTemperature) return false;
        if (rain < minRain || rain > maxRain) return false;
        if (coastal && inlandOnly) return false;
        if (!coastal && coastalOnly) return false;
        return true;
    }

    /// <summary>
    /// Returns layers of the river bank for this biome.
    /// Has default generation.
    /// </summary>
    public virtual int[] GetRiverLayers(int yPos, double heightGradient, int worldX, int worldZ)
    {
        int[] returnLayers = new int[riverLayerIds.Length];

        for (int i = 0; i < riverLayerIds.Length; i++)
        {
            returnLayers[i] = Decorator.GetId(riverLayerIds[i]); //This should be called after strata types are set
        }
        return returnLayers;
    }

    /// <summary>
    /// Returns layers for the beach of this biome.
    /// Has default generation.
    /// </summary>
    public virtual int[] GetBeachLayers(int yPos, double heightGradient, int worldX, int worldZ)
    {
        int[] returnLayers = new int[beachLayerIds.Length];

        if (heightGradient < 3)
        {
            for (int i = 0; i < beachLayerIds.Length; i++)
            {
                returnLayers[i] = Decorator.GetId(beachLayerIds[i]); //This should be called after strata types are set
            }
            return returnLayers;
        }

        return Array.Empty<int>();
    }

    /// <summary>
    /// Load tree generators.
    /// </summary>
    public void LoadTreeVariants()
    {
        TreeGenPropertiesExtended properties = sapi.Assets.Get("game:worldgen/treengenproperties.json").ToObject<TreeGenPropertiesExtended>();

        properties.descVineMinTempRel = TerraGenConfig.DescaleTemperature(properties.vinesMinTemp) / 255f; //Scaled temperature is real temperature. This descales is back to 0-255 then normalizes it

        foreach (TreeVariant variant in properties.TreeGens)
        {
            foreach (BiomeTree biomeTree in trees)
            {
                if (biomeTree.code == variant.Generator.GetName())
                {
                    biomeTree.variant = variant;
                }
            }

            foreach (BiomeTree biomeTree in shrubs)
            {
                if (biomeTree.code == variant.Generator.GetName())
                {
                    biomeTree.variant = variant;
                }
            }
        }

        //Remove invalid trees
        trees = trees.Where(t => t.variant != null).ToArray();
        shrubs = shrubs.Where(t => t.variant != null).ToArray();
    }
}