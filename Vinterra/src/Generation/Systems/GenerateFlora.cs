using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

/// <summary>
/// Trees, vines, reeds, etc.
/// </summary>
public class GenerateFlora : WorldGenBase
{
    public ICoreServerAPI sapi;

    public LCGRandom rand;
    public IWorldGenBlockAccessor blockAccessor;
    public BiomeTreeSupplier treeSupplier;
    public BiomeSystem biomeSystem;

    public int chunkMapSizeY;

    public Dictionary<string, int> rockBlockIdsByType;
    public BlockPatchConfigExtended blockPatchConfig;
    public Dictionary<string, MapLayerBase> blockPatchMaps = new();

    public int noiseSizeDensityMap;

    public float forestModifier;
    public float shrubModifier;

    public override double ExecuteOrder()
    {
        return 0.5;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        if (TerraGenConfig.DoDecorationPass)
        {
            api.Event.InitWorldGenerator(InitWorldGenerator, "standard");
            api.Event.ChunkColumnGeneration(StartChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
        }
    }

    public void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
    {
        int noiseSize = sapi.WorldManager.RegionSize / TerraGenConfig.blockPatchesMapScale;
        foreach (KeyValuePair<string, MapLayerBase> maps in blockPatchMaps)
        {
            IntDataMap2D map = IntDataMap2D.CreateEmpty();
            map.Size = noiseSize + 1;
            map.BottomRightPadding = 1;
            map.Data = maps.Value.GenLayer(regionX * noiseSize, regionZ * noiseSize, noiseSize + 1, noiseSize + 1);
            mapRegion.BlockPatchMaps[maps.Key] = map;
        }
    }

    public void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
    {
        treeSupplier = new BiomeTreeSupplier(sapi);
        blockAccessor = chunkProvider.GetBlockAccessor(true);
    }

    public void InitWorldGenerator()
    {
        LoadGlobalConfig(sapi);

        noiseSizeDensityMap = regionSize / TerraGenConfig.blockPatchesMapScale;
        
        rand = new LCGRandom(sapi.WorldManager.Seed - 87698);

        treeSupplier.CallMethod("LoadTrees");

        //Rock strata for patches
        RockStrataConfig rockStrata = sapi.Assets.Get("worldgen/rockstrata.json").ToObject<RockStrataConfig>();
        rockBlockIdsByType = new Dictionary<string, int>();
        for (int i = 0; i < rockStrata.Variants.Length; i++)
        {
            Block block = sapi.World.GetBlock(rockStrata.Variants[i].BlockCode);
            rockBlockIdsByType[block.LastCodePart()] = block.BlockId;
        }

        //Loads every mods worldgen/blockpatches from jsons
        blockPatchConfig = sapi.Assets.Get("worldgen/blockpatches.json").ToObject<BlockPatchConfigExtended>();
        Dictionary<AssetLocation, BlockPatchExtended[]> blockPatchesFiles = sapi.Assets.GetMany<BlockPatchExtended[]>(sapi.World.Logger, "worldgen/blockpatches/");
        foreach (BlockPatchExtended[] patches in blockPatchesFiles.Values)
        {
            blockPatchConfig.Patches = blockPatchConfig.Patches.Append(patches);
        }
        blockPatchConfig.CallMethod("ResolveBlockIds", sapi, rockStrata, rand);
        blockPatchConfig.extendedNonTreePatches = new BlockPatchExtended[blockPatchConfig.PatchesNonTree.Length];
        for (int i = 0; i < blockPatchConfig.PatchesNonTree.Length; i++)
        {
            blockPatchConfig.extendedNonTreePatches[i] = (BlockPatchExtended)blockPatchConfig.PatchesNonTree[i];
        }

        //Initialize tree generator for internal class
        object treeGeneratorsInstance = typeof(BiomeTreeSupplier).GetField("treeGenerators", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(treeSupplier);
        ForestFloorSystem forestFloorSystem = (ForestFloorSystem)treeGeneratorsInstance.GetType().GetField("forestFloorSystem").GetValue(treeGeneratorsInstance);
        forestFloorSystem.SetBlockPatches(blockPatchConfig);

        //Get world config
        ITreeAttribute worldConfig = sapi.WorldManager.SaveGame.WorldConfiguration;
        forestModifier = worldConfig.GetString("globalForestation").ToFloat(0);
        shrubModifier = worldConfig.GetString("globalForestation").ToFloat(0);

        blockPatchMaps.Clear();
        foreach (BlockPatch patch in blockPatchConfig.Patches)
        {
            if (patch.MapCode == null || blockPatchMaps.ContainsKey(patch.MapCode)) continue;

            int hash = patch.MapCode.GetHashCode();
            int seed = sapi.World.Seed + 112890 + hash;

            blockPatchMaps[patch.MapCode] = new MapLayerWobbled(seed, 2, 0.9f, TerraGenConfig.forestMapScale, 4000, -3000);
        }

        biomeSystem = BiomeSystem.Get(sapi);
    }

    //Rain height map
    public ushort[] heightMap;

    //Erm
    public int forestUpLeft;
    public int forestUpRight;
    public int forestBotLeft;
    public int forestBotRight;

    public int shrubUpLeft;
    public int shrubUpRight;
    public int shrubBotLeft;
    public int shrubBotRight;

    public int climateUpLeft;
    public int climateUpRight;
    public int climateBotLeft;
    public int climateBotRight;

    public BlockPos tempPos = new();
    public BlockPos chunkBase = new();
    public BlockPos chunkEnd = new();
    public List<Cuboidi> structuresIntersectingChunk = new();

    public BiomeType biome;

    public void StartChunkColumnGen(IChunkColumnGenerateRequest request)
    {
        //Sets cached chunk index to -1. Call it when starting a new chunk to be safe. Mystery use
        blockAccessor.BeginColumn();

        //Get chunks
        IServerChunk[] chunks = request.Chunks;
        IMapChunk mapChunk = chunks[0].MapChunk;

        int chunkX = request.ChunkX;
        int chunkZ = request.ChunkZ;

        int localChunkX = chunkX % regionChunkSize;
        int localChunkZ = chunkZ % regionChunkSize;

        //Init seed for this generation
        rand.InitPositionSeed(chunkX, chunkZ);

        //Maps that will be replaced will noise?
        IntDataMap2D forestMap = mapChunk.MapRegion.ForestMap;
        IntDataMap2D shrubMap = mapChunk.MapRegion.ShrubMap;
        IntDataMap2D climateMap = mapChunk.MapRegion.ClimateMap;

        GenerationData data = GenerateData.GetOrCreateErodedData(chunkX, chunkZ, chunkMapWidth, sapi);

        //Forestation level
        float forestFactor = (float)forestMap.InnerSize / regionChunkSize;
        forestUpLeft = forestMap.GetUnpaddedInt((int)(localChunkX * forestFactor), (int)(localChunkZ * forestFactor));
        forestUpRight = forestMap.GetUnpaddedInt((int)(localChunkX * forestFactor + forestFactor), (int)(localChunkZ * forestFactor));
        forestBotLeft = forestMap.GetUnpaddedInt((int)(localChunkX * forestFactor), (int)(localChunkZ * forestFactor + forestFactor));
        forestBotRight = forestMap.GetUnpaddedInt((int)(localChunkX * forestFactor + forestFactor), (int)(localChunkZ * forestFactor + forestFactor));

        //Shrub level
        float shrubFactor = (float)shrubMap.InnerSize / regionChunkSize;
        shrubUpLeft = shrubMap.GetUnpaddedInt((int)(localChunkX * shrubFactor), (int)(localChunkZ * shrubFactor));
        shrubUpRight = shrubMap.GetUnpaddedInt((int)(localChunkX * shrubFactor + shrubFactor), (int)(localChunkZ * shrubFactor));
        shrubBotLeft = shrubMap.GetUnpaddedInt((int)(localChunkX * shrubFactor), (int)(localChunkZ * shrubFactor + shrubFactor));
        shrubBotRight = shrubMap.GetUnpaddedInt((int)(localChunkX * shrubFactor + shrubFactor), (int)(localChunkZ * shrubFactor + shrubFactor));

        //Climate
        float climateFactor = (float)climateMap.InnerSize / regionChunkSize;
        climateUpLeft = climateMap.GetUnpaddedInt((int)(localChunkX * climateFactor), (int)(localChunkZ * climateFactor));
        climateUpRight = climateMap.GetUnpaddedInt((int)(localChunkX * climateFactor + climateFactor), (int)(localChunkZ * climateFactor));
        climateBotLeft = climateMap.GetUnpaddedInt((int)(localChunkX * climateFactor), (int)(localChunkZ * climateFactor + climateFactor));
        climateBotRight = climateMap.GetUnpaddedInt((int)(localChunkX * climateFactor + climateFactor), (int)(localChunkZ * climateFactor + climateFactor));

        heightMap = chunks[0].MapChunk.RainHeightMap;

        structuresIntersectingChunk.Clear();

        sapi.World.BlockAccessor.WalkStructures(chunkBase.Set(chunkX * chunkSize, 0, chunkZ * chunkSize), chunkEnd.Set(chunkX * chunkSize + chunkSize, chunkMapSizeY * chunkSize, chunkZ * chunkSize + chunkSize), (struc) =>
        {
            if (struc.SuppressTreesAndShrubs)
            {
                structuresIntersectingChunk.Add(struc.Location.Clone().GrowBy(1, 1, 1));
            }
        });

        if (TerraGenConfig.GenerateVegetation)
        {
            GeneratePatches(chunkX, chunkZ, false, data.biomeIds, data.riverDistance, data.isBeach);

            GenerateTreesAndShrubs(chunkX, chunkZ, data, false); //Shrubs
            GenerateTreesAndShrubs(chunkX, chunkZ,  data, true); //Trees

            //Post pass
            GeneratePatches(chunkX, chunkZ, true, data.biomeIds, data.riverDistance, data.isBeach); 
        }
    }

    /// <summary>
    /// Generate flowers, bushes, cacti.
    /// </summary>
    public void GeneratePatches(int chunkX, int chunkZ, bool postPass, ushort[,] biomeMap, double[,] riverDistances, bool[,] beachMap)
    {
        int localX, localZ, worldX, worldZ;

        Block liquidBlock;
        
        IMapRegion mapRegion = sapi.WorldManager.GetMapRegion(chunkX * chunkSize / regionSize, chunkZ * chunkSize / regionSize);

        biome = biomeSystem.biomes[biomeMap[16, 16]];

        List<BlockPatchExtended> validPatches = new();
        foreach (BlockPatchExtended patch in blockPatchConfig.extendedNonTreePatches)
        {
            if (patch.tags.Contains("global"))
            {
                validPatches.Add(patch);
                continue;
            }

            foreach (string tag in biome.patchTags)
            {
                if (patch.tags.Contains(tag))
                {
                    validPatches.Add(patch);
                }
            }
        }

        foreach (BlockPatchExtended blockPatch in validPatches)
        {
            if (blockPatch.PostPass != postPass) continue;

            float chance = blockPatch.Chance * blockPatchConfig.ChanceMultiplier.nextFloat();

            while (chance-- > rand.NextFloat())
            {
                localX = rand.NextInt(chunkSize);
                localZ = rand.NextInt(chunkSize);

                //Check if biome allows patches
                biome = biomeSystem.biomes[biomeMap[localX, localZ]];
                if (!biome.spawnPatches) continue;

                worldX = localX + chunkX * chunkSize;
                worldZ = localZ + chunkZ * chunkSize;

                //Don't generate patches at extreme heights
                int posY = heightMap[localZ * chunkSize + localX];
                if (posY <= 0 || posY >= mapHeight - 15) continue;

                tempPos.Set(worldX, posY, worldZ);

                liquidBlock = blockAccessor.GetBlock(tempPos, BlockLayersAccess.Fluid);
                float forestNormal = GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, (float)localX / chunkSize, (float)localZ / chunkSize) / 255f;
                forestNormal = GameMath.Clamp(forestNormal + forestModifier, 0, 1);
                float shrubNormal = GameMath.BiLerp(shrubUpLeft, shrubUpRight, shrubBotLeft, shrubBotRight, (float)localX / chunkSize, (float)localZ / chunkSize) / 255f;
                shrubNormal = GameMath.Clamp(shrubNormal + shrubModifier, 0, 1);
                int climate = GameMath.BiLerpRgbColor((float)localX / chunkSize, (float)localZ / chunkSize, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);

                if (BlockPatchConfigExtended.CanGeneratePatch(blockPatch, liquidBlock, mapHeight, climate, posY, forestNormal, shrubNormal, riverDistances[localX, localZ], beachMap[localX, localZ]))
                {
                    if (blockPatch.MapCode != null && rand.NextInt(255) > GetPatchDensity(blockPatch.MapCode, worldX, worldZ, mapRegion))
                    {
                        continue;
                    }

                    int firstBlockId = 0;
                    bool found = true;

                    if (blockPatch.BlocksByRockType != null)
                    {
                        found = false;
                        int dY = 1;
                        while (dY < 5 && posY - dY > 0)
                        {
                            string lastCodePart = blockAccessor.GetBlock(worldX, posY - dY, worldZ).LastCodePart();
                            if (rockBlockIdsByType.TryGetValue(lastCodePart, out firstBlockId)) { found = true; break; }
                            dY++;
                        }
                    }

                    if (found)
                    {
                        blockPatch.Generate(blockAccessor, rand, worldX, posY, worldZ, firstBlockId);
                    }
                }
            }
        }
    }

    public void GenerateTreesAndShrubs(int chunkX, int chunkZ, GenerationData data, bool tree)
    {
        int tries = tree ? (int)treeSupplier.treeGenProperties.treesPerChunk.nextFloat() : (int)treeSupplier.treeGenProperties.shrubsPerChunk.nextFloat();

        int localX, localZ, worldX, worldZ;
        Block block;
        int treesGenerated = 0;

        EnumHemisphere hemisphere = sapi.World.Calendar.GetHemisphere(new BlockPos(chunkX * chunkSize + chunkSize / 2, 0, chunkZ * chunkSize + chunkSize / 2));

        while (tries > 0)
        {
            tries--;

            localX = rand.NextInt(chunkSize);
            localZ = rand.NextInt(chunkSize);

            biome = biomeSystem.biomes[data.biomeIds[localX, localZ]];

            worldX = localX + chunkX * chunkSize;
            worldZ = localZ + chunkZ * chunkSize;

            int posY = heightMap[localZ * chunkSize + localX];
            if (posY <= 0 || posY >= mapHeight - 15) continue;

            tempPos.Set(worldX, posY, worldZ);

            //Check if underwater
            bool underwater = false;
            block = blockAccessor.GetBlock(tempPos, BlockLayersAccess.Fluid);
            if (block.IsLiquid())
            {
                underwater = true;
                tempPos.Y--;
                block = blockAccessor.GetBlock(tempPos, BlockLayersAccess.Fluid);
                if (block.IsLiquid()) tempPos.Y--;
            }

            //Get block
            block = blockAccessor.GetBlock(tempPos);
            if (block.Fertility == 0) continue;

            //Place according to forest value
            float chance = tree ? GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, (float)localX / chunkSize, (float)localZ / chunkSize) 
                                : GameMath.BiLerp(shrubUpLeft, shrubUpRight, shrubBotLeft, shrubBotRight, (float)localX / chunkSize, (float)localZ / chunkSize);
            chance = tree ? Math.Clamp(chance + 255 * forestModifier, 0, 255) : Math.Clamp(chance + 255 * shrubModifier, 0, 255);
            float chanceNormal = chance / 255;

            if (rand.NextFloat() > Math.Max(0.0025, chanceNormal * chanceNormal * biome.treeDensity)) continue;

            double riverDistance = data.riverDistance[localX, localZ];
            double continentalness = data.continentalness[localX, localZ];
            int climate = GameMath.BiLerpRgbColor((float)localX / chunkSize, (float)localZ / chunkSize, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);

            TreeGenInstance treeGenParams = treeSupplier.GetRandomGenForBiome(tree ? biome.trees : biome.shrubs, posY, underwater, worldX, worldZ, continentalness, block, climate, riverDistance);

            if (treeGenParams != null)
            {
                bool canGen = true;
                for (int i = 0; i < structuresIntersectingChunk.Count; i++)
                {
                    if (structuresIntersectingChunk[i].Contains(tempPos)) { canGen = false; break; }
                }
                if (!canGen) continue;

                if (blockAccessor.GetBlock(tempPos.X, tempPos.Y, tempPos.Z).Replaceable >= 6000)
                {
                    tempPos.Y--;
                }

                treeGenParams.skipForestFloor = tree ? false : true;

                treeGenParams.hemisphere = hemisphere;

                if (tree) treeGenParams.treesInChunkGenerated = treesGenerated;

                treeGenParams.GrowTree(blockAccessor, tempPos);

                if (tree) treesGenerated++;
            }
        }
    }

    /// <summary>
    /// Returns 0-255.
    /// Density of patches like flowers.
    /// </summary>
    public int GetPatchDensity(string code, int posX, int posZ, IMapRegion mapRegion)
    {
        if (mapRegion == null) return 0;

        int localX = posX % regionSize;
        int localZ = posZ % regionSize;

        mapRegion.BlockPatchMaps.TryGetValue(code, out IntDataMap2D map);

        if (map != null)
        {
            float posXInRegionOre = GameMath.Clamp((float)localX / regionSize * noiseSizeDensityMap, 0, noiseSizeDensityMap - 1);
            float posZInRegionOre = GameMath.Clamp((float)localZ / regionSize * noiseSizeDensityMap, 0, noiseSizeDensityMap - 1);

            int density = map.GetUnpaddedColorLerped(posXInRegionOre, posZInRegionOre);

            return density;
        }

        return 0;
    }
}