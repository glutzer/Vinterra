public class BiomeJsonObject
{
    public string code;

    public string surfaceDecoratorType = "placeonce";
    public string[] beachLayers = new string[] { "sand", "sand" };
    public string[] riverLayers = new string[] { "gravel", "gravel" };
    public string waterCode = "water-still-7";
    public string bedCode = "muddygravel";
    public string[] terrainTypes; //Terrain types to pick from

    public DecoratorJsonObject[] decorators = System.Array.Empty<DecoratorJsonObject>();
    public DecoratorJsonObject[] gradientDecorators = System.Array.Empty<DecoratorJsonObject>();

    public FeatureJsonObject[] features = System.Array.Empty<FeatureJsonObject>();

    public string[] treeGroups = System.Array.Empty<string>();
}

public class DecoratorJsonObject
{
    public string type;

    public DecoratorConditionJsonObject[] conditions = System.Array.Empty<DecoratorConditionJsonObject>();

    public string[] layers = System.Array.Empty<string>();

    //Noise
    public float[] noiseThresholds = System.Array.Empty<float>();
    public int octaves = 1;
    public float frequency = 0.01f;
    public float noiseStrength = 1;

    //Repeating
    public int repeatTimes = 100;
    public int repeatLayerThickness = 1;
}

public class DecoratorConditionJsonObject
{
    public string type;

    public float value = 0;
    public float chance = 1;
}

public class FeatureJsonObject
{
    public string type;

    //Blocks to use for this feature
    public string[] blocks = new string[] { "rock" };
    public float hSize = 1;
    public float hSizeVariance = 0;
    public float vSize = 1;
    public float vSizeVariance = 0;
    public float chance = 1;
    public int tries = 1;

    public float frequency = 0.05f;
    public int octaves = 2;

    //Also use conditions for features
    public DecoratorConditionJsonObject[] conditions = System.Array.Empty<DecoratorConditionJsonObject>();
}