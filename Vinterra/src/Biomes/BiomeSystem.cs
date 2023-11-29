using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

public class BiomeSystem : ModSystem
{
    public ICoreServerAPI sapi;

    public LCGRandom selectorRandom;

    //All registered biomes
    public List<BiomeType> biomes = new();
    public Dictionary<string, BiomeType> biomeRegistry = new();
    public Dictionary<string, TerrainType> terrainRegistry = new(); //Convert this to a type later so they can be initialized differently

    public Dictionary<string, Type> decoratorRegistry = new();
    public Dictionary<string, Type> decoratorConditionRegistry = new();

    public Dictionary<string, Type> featureRegistry = new();

    public Dictionary<string, TreeGroup> treeGroupRegistry = new();

    //Global features added by a seperate json
    public PartialFeature[] globalFeatures = Array.Empty<PartialFeature>();

    public int seaLevel = 100;

    public static BiomeSystem Get(ICoreAPI api)
    {
        return api.ModLoader.GetModSystem<BiomeSystem>();
    }

    //Load before generation
    public override double ExecuteOrder() => 0.00002;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        selectorRandom = new LCGRandom(sapi.World.Seed);

        //Register all terrain classes
        List<Type> annotatedTerrains = Assembly.GetExecutingAssembly().GetTypes().Where(t => Attribute.IsDefined(t, typeof(TerrainAttribute))).ToList();
        foreach (Type annotatedTerrain in annotatedTerrains)
        {
            TerrainAttribute customAttribute = (TerrainAttribute)Attribute.GetCustomAttribute(annotatedTerrain, typeof(TerrainAttribute));

            terrainRegistry.Add(customAttribute.TerrainName, Activator.CreateInstance(annotatedTerrain, sapi) as TerrainType);
        }

        //Register all decorators
        List<Type> annotatedDecorators = Assembly.GetExecutingAssembly().GetTypes().Where(t => Attribute.IsDefined(t, typeof(DecoratorAttribute))).ToList();
        foreach (Type annotatedDecorator in annotatedDecorators)
        {
            DecoratorAttribute customAttribute = (DecoratorAttribute)Attribute.GetCustomAttribute(annotatedDecorator, typeof(DecoratorAttribute));

            decoratorRegistry.Add(customAttribute.DecoratorName, annotatedDecorator); //Initialized in biome
        }

        //Register all conditions
        List<Type> annotatedConditions = Assembly.GetExecutingAssembly().GetTypes().Where(t => Attribute.IsDefined(t, typeof(DecoratorConditionAttribute))).ToList();
        foreach (Type annotatedCondition in annotatedConditions)
        {
            DecoratorConditionAttribute customAttribute = (DecoratorConditionAttribute)Attribute.GetCustomAttribute(annotatedCondition, typeof(DecoratorConditionAttribute));

            decoratorConditionRegistry.Add(customAttribute.DecoratorConditionName, annotatedCondition); //Initialized in biome
        }

        //Register all features
        List<Type> annotatedFeatures = Assembly.GetExecutingAssembly().GetTypes().Where(t => Attribute.IsDefined(t, typeof(FeatureAttribute))).ToList();
        foreach (Type annotatedFeature in annotatedFeatures)
        {
            FeatureAttribute customAttribute = (FeatureAttribute)Attribute.GetCustomAttribute(annotatedFeature, typeof(FeatureAttribute));

            featureRegistry.Add(customAttribute.FeatureName, annotatedFeature); //Initialized in biome
        }

        //Load all types of tree groups
        Dictionary<AssetLocation, TreeGroup> treeGroupValues = sapi.Assets.GetMany<TreeGroup>(sapi.World.Logger, "worldgen/treegroups/");
        foreach (TreeGroup group in treeGroupValues.Values)
        {
            treeGroupRegistry.Add(group.code, group);
        }

        LoadBiomesFromJson();

        //Sort biomes alphabetically in lists
        biomes = biomes.OrderBy(a => a.name).ToList();
        foreach (BiomeType biome in biomes)
        {
            biome.LoadTreeVariants();
        }
    }

    public BiomeType RegisterBiome(string name, Type type)
    {
        BiomeType biome = (BiomeType)Activator.CreateInstance(type, sapi);
        biome.name = name;
        biomes.Add(biome);
        biomeRegistry.Add(name, biome);
        return biome;
    }

    /// <summary>
    /// Select a biome at a specific point.
    /// Performance is not suited for every block, only zones.
    /// </summary>
    public int SelectBiome(int temperature, int rain, int x, int y, bool coastal)
    {
        List<BiomeType> valid = new();

        int weight = 0;

        foreach (BiomeType biome in biomes)
        {
            if (biome.weight > 0 && biome.IsInRange(temperature, rain, coastal))
            {
                valid.Add(biome);
                weight += biome.weight;
            }
        }

        selectorRandom.InitPositionSeed(x, y);
        int roll = selectorRandom.NextInt(weight);

        foreach (BiomeType biome in valid)
        {
            weight -= biome.weight;
            if (roll >= weight)
            {
                return biomes.IndexOf(biome);
            }
        }

        return 0; //Return first biome if none are valid
    }

    public ushort GetIdOf(BiomeType biomeType)
    {
        return (ushort)biomes.IndexOf(biomeType);
    }

    public void LoadBiomesFromJson()
    {
        string[] paths = Directory.GetFiles(GamePaths.DataPath + "/vinterra/biomes");

        foreach (string path in paths)
        {
            using TextReader textReader = new StreamReader(path);
            string biomeJson = textReader.ReadToEnd();
            textReader.Close();

            //Data that can't be directly deserialized
            BiomeJsonObject jsonObject = JsonConvert.DeserializeObject<BiomeJsonObject>(biomeJson);
            BiomeType newBiome = JsonConvert.DeserializeObject<BiomeType>(biomeJson);

            newBiome.sapi = sapi;
            newBiome.biomeSystem = this;
            newBiome.random = new LCGRandom(sapi.World.Seed);

            //Add terrains
            string[] terrainTypes = jsonObject.terrainTypes;
            foreach (string type in terrainTypes)
            {
                newBiome.terrainList.Add(terrainRegistry[type]);
            }

            //Decorators
            List<Decorator> decorators = new();

            foreach (DecoratorJsonObject decoratorObject in jsonObject.decorators)
            {
                Decorator decorator = Activator.CreateInstance(decoratorRegistry[decoratorObject.type]) as Decorator;
                decorator.type = decoratorObject.type;

                if (decorator.type == "soil") (decorator as SoilDecorator).Resolve(sapi);

                List<DecoratorCondition> conditions = new();

                foreach (DecoratorConditionJsonObject conditionObject in decoratorObject.conditions)
                {
                    DecoratorCondition condition = Activator.CreateInstance(decoratorConditionRegistry[conditionObject.type]) as DecoratorCondition;

                    condition.value = conditionObject.value;
                    condition.chance = conditionObject.chance;

                    conditions.Add(condition);
                }

                decorator.conditions = conditions.ToArray();

                decorator.seaLevel = seaLevel;
                Decorator.ResolveLayers(decoratorObject.layers, ref decorator.layers, sapi);
                decorator.layerLength = decorator.layers.Length;

                //Noise
                decorator.noiseThresholds = decoratorObject.noiseThresholds;
                decorator.noise = new Noise(sapi.World.Seed, decoratorObject.frequency, decoratorObject.octaves);
                decorator.noiseStrength = decoratorObject.noiseStrength;

                //Repeating layers
                decorator.repeatTimes = decoratorObject.repeatTimes;
                decorator.repeatLayerThickness = decoratorObject.repeatLayerThickness;

                decorators.Add(decorator);
            }

            newBiome.surfaceDecorators = decorators.ToArray();

            //Gradient
            List<Decorator> gradientDecorators = new();

            foreach (DecoratorJsonObject decoratorObject in jsonObject.gradientDecorators)
            {
                Decorator decorator = Activator.CreateInstance(decoratorRegistry[decoratorObject.type]) as Decorator;

                List<DecoratorCondition> conditions = new();

                foreach (DecoratorConditionJsonObject conditionObject in decoratorObject.conditions)
                {
                    DecoratorCondition condition = Activator.CreateInstance(decoratorConditionRegistry[conditionObject.type]) as DecoratorCondition;

                    condition.value = conditionObject.value;
                    condition.chance = conditionObject.chance;

                    conditions.Add(condition);
                }

                decorator.conditions = conditions.ToArray();

                decorator.seaLevel = seaLevel;
                Decorator.ResolveLayers(decoratorObject.layers, ref decorator.layers, sapi);
                decorator.layerLength = decorator.layers.Length;

                //Noise
                decorator.noiseThresholds = decoratorObject.noiseThresholds;
                decorator.noise = new Noise(sapi.World.Seed, decoratorObject.frequency, decoratorObject.octaves);
                decorator.noiseStrength = decoratorObject.noiseStrength;

                //Repeating layers
                decorator.repeatTimes = decoratorObject.repeatTimes;
                decorator.repeatLayerThickness = decoratorObject.repeatLayerThickness;

                gradientDecorators.Add(decorator);
            }

            newBiome.gradientSurfaceDecorators = gradientDecorators.ToArray();

            //Features
            List<PartialFeature> features = new();

            foreach (FeatureJsonObject featureObject in jsonObject.features)
            {
                PartialFeature feature = Activator.CreateInstance(featureRegistry[featureObject.type], sapi) as PartialFeature;

                List<DecoratorCondition> conditions = new();

                foreach (DecoratorConditionJsonObject conditionObject in featureObject.conditions)
                {
                    DecoratorCondition condition = Activator.CreateInstance(decoratorConditionRegistry[conditionObject.type]) as DecoratorCondition;

                    condition.value = conditionObject.value;
                    condition.chance = conditionObject.chance;

                    conditions.Add(condition);
                }

                feature.conditions = conditions.ToArray();

                feature.hSize = featureObject.hSize;
                feature.hSizeVariance = featureObject.hSize;
                feature.vSize = featureObject.vSize;
                feature.vSizeVariance = featureObject.vSize;
                feature.chance = featureObject.chance;
                feature.tries = featureObject.tries;

                feature.noise = new Noise(sapi.World.Seed, featureObject.frequency, featureObject.octaves);

                Decorator.ResolveLayers(featureObject.blocks, ref feature.blocks, sapi); //Resolve

                features.Add(feature);
            }

            newBiome.biomeFeatures = features.ToArray();

            //Resolve biome layers
            Decorator.ResolveBlock(jsonObject.waterCode, ref newBiome.waterId, sapi);
            Decorator.ResolveBlock(jsonObject.bedCode, ref newBiome.bedId, sapi);
            Decorator.ResolveLayers(jsonObject.beachLayers, ref newBiome.beachLayerIds, sapi);
            Decorator.ResolveLayers(jsonObject.riverLayers, ref newBiome.riverLayerIds, sapi);

            foreach (string code in jsonObject.treeGroups)
            {
                foreach (BiomeTree tree in treeGroupRegistry[code].trees)
                {
                    tree.sizeMultiplier *= treeGroupRegistry[code].sizeMultiplier;
                    tree.weight = (int)(tree.weight * treeGroupRegistry[code].weight);
                    newBiome.trees = newBiome.trees.Append(tree).ToArray();
                }

                foreach (BiomeTree shrub in treeGroupRegistry[code].shrubs)
                {
                    shrub.sizeMultiplier *= treeGroupRegistry[code].sizeMultiplier;
                    shrub.weight = (int)(shrub.weight * treeGroupRegistry[code].weight);
                    newBiome.trees = newBiome.trees.Append(shrub).ToArray();
                }
            }

            biomes.Add(newBiome);
            biomeRegistry.Add(jsonObject.code, newBiome);
        }
    }
}