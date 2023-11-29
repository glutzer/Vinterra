using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

public class GenerateBlockLayers : WorldGenBase
{
    public ICoreServerAPI sapi;
    public BlockLayerConfig blockLayerConfig;
    public BiomeSystem biomeSystem;

    public ClampedSimplexNoise grassDensity;
    public ClampedSimplexNoise grassHeight;

    public LCGRandom rand;

    public Dictionary<int, int> gravelMappings = new();
    public Dictionary<int, int> sandMappings = new();
    public Dictionary<int, int> hardenedSandMappings = new();

    public Noise depthNoise;

    public float transitionStrength = 30;

    public override double ExecuteOrder() => 0.4;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        if (TerraGenConfig.DoDecorationPass)
        {
            sapi.Event.InitWorldGenerator(InitWorldGenerator, "standard");
            sapi.Event.InitWorldGenerator(InitWorldGenerator, "superflat");
            sapi.Event.ChunkColumnGeneration(ChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");
        }
    }

    public void ResolveGravelAndSand(RockStrataConfig rockStrata)
    {
        foreach (RockStratum stratum in rockStrata.Variants)
        {
            int stratumId = sapi.World.GetBlock(stratum.BlockCode).BlockId;

            if (gravelMappings.ContainsKey(stratumId)) continue;

            //No gravel for kimberlite/phyllite
            int gravelId = sapi.World.GetBlock(new AssetLocation("gravel-" + stratum.BlockCode.ToString().Split('-')[1]))?.BlockId ?? stratumId;
            int sandId;
            int hardenedSandId;

            if (stratum.RockGroup == EnumRockGroup.Sedimentary)
            {
                sandId = sapi.World.GetBlock(new AssetLocation("sand-" + stratum.BlockCode.ToString().Split('-')[1]))?.BlockId ?? stratumId;
                hardenedSandId = sapi.World.GetBlock(new AssetLocation("hardenedsand-" + stratum.BlockCode.ToString().Split('-')[1]))?.BlockId ?? stratumId;
            }
            else
            {
                sandId = sapi.World.GetBlock(new AssetLocation("sand-" + "sandstone"))?.BlockId ?? stratumId;
                hardenedSandId = sapi.World.GetBlock(new AssetLocation("hardenedsand-" + "sandstone"))?.BlockId ?? stratumId;
            }

            gravelMappings.Add(stratumId, gravelId);
            sandMappings.Add(stratumId, sandId);
            hardenedSandMappings.Add(stratumId, hardenedSandId);
        }
    }

    public void InitWorldGenerator()
    {
        //Load config into the system base
        LoadGlobalConfig(sapi);
        biomeSystem = BiomeSystem.Get(sapi);

        //Get rock strata json
        RockStrataConfig rockStrata = sapi.Assets.Get("worldgen/rockstrata.json").ToObject<RockStrataConfig>();
        ResolveGravelAndSand(rockStrata);

        //Get block layers json
        blockLayerConfig = sapi.Assets.Get("worldgen/blocklayers.json").ToObject<BlockLayerConfig>();
        blockLayerConfig.ResolveBlockIds(sapi, rockStrata);

        //Simplex noise clamped to 0-1
        grassDensity = new ClampedSimplexNoise(new double[] { 4 }, new double[] { 0.5 }, sapi.WorldManager.Seed);
        grassHeight = new ClampedSimplexNoise(new double[] { 1.5 }, new double[] { 0.5 }, sapi.WorldManager.Seed);

        rand = new LCGRandom(sapi.WorldManager.Seed);

        depthNoise = new Noise(sapi.World.Seed, 0.005f, 2); //Set configs for this, higher frequency makes things go up and down more eratically
    }

    public void ChunkColumnGeneration(IChunkColumnGenerateRequest request)
    {
        IServerChunk[] chunks = request.Chunks;
        rand.InitPositionSeed(request.ChunkX, request.ChunkZ);

        ushort[] terrainHeightMap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
        ushort[] rainHeightMap = chunks[0].MapChunk.RainHeightMap;

        GenerationData generationData = GenerateData.GetOrCreateErodedData(request.ChunkX, request.ChunkZ, chunkMapWidth, sapi);
        ushort[,] biomeData = generationData.biomeIds;
        double[,] heightGradient = generationData.heightGradient;

        //Retardation
        //--
        //--
        int chunkX = request.ChunkX;
        int chunkZ = request.ChunkZ;

        int rdX = chunkX % regionChunkSize;
        int rdZ = chunkZ % regionChunkSize;

        IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
        float climateStep = (float)climateMap.InnerSize / regionChunkSize;

        IntDataMap2D forestMap = chunks[0].MapChunk.MapRegion.ForestMap;
        float forestStep = (float)forestMap.InnerSize / regionChunkSize;
        int forestUpLeft = forestMap.GetUnpaddedInt((int)(rdX * forestStep), (int)(rdZ * forestStep));
        int forestUpRight = forestMap.GetUnpaddedInt((int)(rdX * forestStep + forestStep), (int)(rdZ * forestStep));
        int forestBotLeft = forestMap.GetUnpaddedInt((int)(rdX * forestStep), (int)(rdZ * forestStep + forestStep));
        int forestBotRight = forestMap.GetUnpaddedInt((int)(rdX * forestStep + forestStep), (int)(rdZ * forestStep + forestStep));
        //--
        //--
        //Retardation

        //Multi-thread then place results after? Decorator would need to be reworked and strata ids passed in directly
        for (int localX = 0; localX < chunkSize; localX++)
        {
            for (int localZ = 0; localZ < chunkSize; localZ++)
            {
                int worldX = request.ChunkX * chunkSize + localX;
                int worldZ = request.ChunkZ * chunkSize + localZ;

                float depthValue = depthNoise.GetPosNoise(worldX, worldZ);

                int mapIndex = LocalChunkIndex2D(localX, localZ);
                int yPos = terrainHeightMap[mapIndex];
                int yForCalculation = Math.Max((int)(yPos - depthValue * transitionStrength), seaLevel); //Modulated y value for calculating temperature, rain, and fertility

                bool underwater = false;
                if (yPos != rainHeightMap[mapIndex]) underwater = true;

                int index3d = (chunkSize * (yPos % chunkSize) + localZ) * chunkSize + localX;

                int rockBlockId = chunks[yPos / chunkSize].Data.GetBlockIdUnsafe(index3d); //Should always be a rock since strata are generated second. All strata have sand/gravel. Do not generate ice in generate terrain
                chunks[0].MapChunk.TopRockIdMap[mapIndex] = rockBlockId;

                //Get the chunk at that y position
                IChunkBlocks chunkData = chunks[yPos / chunkSize].Data;

                BiomeType biome = biomeSystem.biomes[biomeData[localX, localZ]];

                int gravelId = gravelMappings.Get(rockBlockId, rockBlockId);
                int sandId = sandMappings.Get(rockBlockId, rockBlockId);
                int hardenedSandId = hardenedSandMappings.Get(rockBlockId, rockBlockId);

                int climate = climateMap.GetUnpaddedColorLerped(rdX * climateStep + climateStep * localX / chunkSize, rdZ * climateStep + climateStep * localZ / chunkSize);
                int tempInt = (climate >> 16) & 0xff; //0-255
                int rainInt = (climate >> 8) & 0xff; //0-255
                float realTempAtY = TerraGenConfig.GetScaledAdjustedTemperatureFloat(tempInt, yForCalculation - seaLevel); //Float displayed on screen
                float tempNormal = TerraGenConfig.GetAdjustedTemperature(tempInt, yForCalculation - seaLevel) / 255f; //0-1 temperature value
                float rainNormal = TerraGenConfig.GetRainFall(rainInt, yPos) / 255f; //0-1 rain value, not modulated
                float heightNormal = ((float)yForCalculation - seaLevel) / ((float)mapHeight - seaLevel); //0-1 value for above sea level
                float fertilityNormal = TerraGenConfig.GetFertilityFromUnscaledTemp(rainInt, tempInt, heightNormal) / 255f;

                if (underwater)
                {
                    //50/50 sand/gravel
                    if (depthValue > 0.5)
                    {
                        chunkData[index3d] = gravelId;
                        index3d -= 1024;
                        yPos--;
                        if (index3d < 0)
                        {
                            index3d += 32768;
                            chunkData = chunks[yPos / chunkSize].Data;
                        }
                        chunkData[index3d] = gravelId;
                    }
                    else
                    {
                        chunkData[index3d] = sandId;
                        index3d -= 1024;
                        yPos--;
                        if (index3d < 0)
                        {
                            index3d += 32768;
                            chunkData = chunks[yPos / chunkSize].Data;
                        }
                        chunkData[index3d] = sandId;
                    }
                }
                else
                {
                    //Set the rock types to be used by the surface decorator
                    Decorator.SetStrata(rockBlockId, gravelId, sandId, hardenedSandId);

                    double gradient = heightGradient[localX, localZ];

                    //The vanilla game calculates soil depth from rain, fertility and height. This is only using a random dirt depth noise and the height due to rain and fertility being infrequent
                    //New depth is under depth * inverse lerp of y threshold to sea level
                    //If depth is < 0 and temperature is below threshold, place y layers. Otherwise place gradient layers

                    //Get regular layers and set them

                    Decorator[] decorators = gradient > biome.gradientThreshold ? biome.gradientSurfaceDecorators : biome.surfaceDecorators;
                    foreach (Decorator decorator in decorators)
                    {
                        bool invalid = false;
                        foreach (DecoratorCondition condition in decorator.conditions)
                        {
                            if (condition.IsInvalid(heightNormal, realTempAtY, rainNormal, fertilityNormal, rand))
                            {
                                invalid = true;
                                break;
                            }
                        }
                        if (invalid) continue;

                        yPos = terrainHeightMap[mapIndex];
                        index3d = (chunkSize * (yPos % chunkSize) + localZ) * chunkSize + localX;
                        chunkData = chunks[yPos / chunkSize].Data;

                        int[] layers = decorator.GetBlockLayers(yPos, gradient, worldX, worldZ, depthValue, rainNormal, fertilityNormal);
                        for (int i = 0; i < layers.Length; i++)
                        {
                            if (chunkData[index3d] != 0 || i < 2) chunkData[index3d] = layers[i]; //Always put down 2 layers if applicable, but don't cover air
                            index3d -= 1024;
                            yPos--;

                            if (index3d < 0)
                            {
                                index3d += 32768;
                                chunkData = chunks[yPos / chunkSize].Data;
                            }
                        }
                    }

                    yPos = terrainHeightMap[mapIndex];
                    index3d = (chunkSize * (yPos % chunkSize) + localZ) * chunkSize + localX;
                    chunkData = chunks[yPos / chunkSize].Data;

                    //Layers at beach
                    double continentalness = generationData.continentalness[localX, localZ];
                    if (continentalness < 1 && generationData.isBeach[localX, localZ])
                    {
                        int[] beachLayers = biome.GetBeachLayers(yPos, gradient, worldX, worldZ);

                        for (int i = 0; i < beachLayers.Length; i++)
                        {
                            if (chunkData[index3d] != 0 || i < 2) chunkData[index3d] = beachLayers[i];
                            index3d -= 1024;
                            yPos--;

                            if (index3d < 0)
                            {
                                index3d += 32768;
                                chunkData = chunks[yPos / chunkSize].Data;
                            }
                        }
                    }

                    yPos = terrainHeightMap[mapIndex];
                    index3d = (chunkSize * (yPos % chunkSize) + localZ) * chunkSize + localX;
                    chunkData = chunks[yPos / chunkSize].Data;
                    
                    //River layers around bank
                    double riverDistance = generationData.riverDistance[localX, localZ];
                    if (riverDistance < biome.riverDistance * (depthValue * depthValue)) //Random square dropoff of river bank
                    {
                        int[] riverLayers = biome.GetRiverLayers(yPos, gradient, worldX, worldZ);

                        for (int i = 0; i < riverLayers.Length; i++)
                        {
                            if (chunkData[index3d] != 0 || i < 2) chunkData[index3d] = riverLayers[i];
                            index3d -= 1024;
                            yPos--;

                            if (index3d < 0)
                            {
                                index3d += 32768;
                                chunkData = chunks[yPos / chunkSize].Data;
                            }
                        }
                    }
                }

                if (!biome.spawnGrass) continue; //Don't spawn grass in deserts

                //Retardation / grass
                //--
                //--
                yPos = terrainHeightMap[mapIndex];
                if (yPos < rainHeightMap[mapIndex]) continue; //Grass generating underwater, originally checked below sea level

                //Climate
                float forestNormal = GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, (float)localX / chunkSize, (float)localZ / chunkSize) / 255f;
                //Climate

                PlaceTallGrass(localX, yPos, localZ, chunks, rainNormal, tempNormal, realTempAtY, forestNormal);
                //--
                //--
                //Retardation / grass
            }
        }
    }

    //Retardation
    public void PlaceTallGrass(int x, int posY, int z, IServerChunk[] chunks, float rainNormal, float tempNormal, float realTemp, float forestNormal)
    {
        double randValue = blockLayerConfig.Tallgrass.RndWeight * rand.NextDouble() + blockLayerConfig.Tallgrass.PerlinWeight * grassDensity.Noise(x, z, -0.5f);

        double extraGrass = Math.Max(0, rainNormal * tempNormal - 0.25);

        if (randValue <= GameMath.Clamp(forestNormal - extraGrass, 0.05, 0.99) || posY >= mapHeight - 1 || posY < 1) return;

        int blockId = chunks[posY / chunkSize].Data[(chunkSize * (posY % chunkSize) + z) * chunkSize + x];

        if (sapi.World.Blocks[blockId].Fertility <= rand.NextInt(100)) return; //If the block is not fertile (like dirt) skip

        double grassSample = Math.Max(0, grassHeight.Noise(x, z) * blockLayerConfig.Tallgrass.BlockCodeByMin.Length - 1);
        int start = (int)grassSample + (rand.NextDouble() < grassSample ? 1 : 0);

        for (int i = start; i < blockLayerConfig.Tallgrass.BlockCodeByMin.Length; i++)
        {
            TallGrassBlockCodeByMin codeByMin = blockLayerConfig.Tallgrass.BlockCodeByMin[i];

            if (forestNormal <= codeByMin.MaxForest && rainNormal >= codeByMin.MinRain && realTemp >= codeByMin.MinTemp)
            {
                chunks[(posY + 1) / chunkSize].Data[(chunkSize * ((posY + 1) % chunkSize) + z) * chunkSize + x] = codeByMin.BlockId;
                return;
            }
        }
    }
}

public class ChunkBiLerp
{
    readonly float valueNw;
    readonly float valueNe;
    readonly float valueSw;
    readonly float valueSe;

    public ChunkBiLerp(int sx, int sz, Noise noise)
    {
        double startX = (float)sx - 0.5f;
        double startZ = (float)sz - 0.5f;

        valueNw = noise.GetPosNoise(startX, startZ);
        valueNe = noise.GetPosNoise(startX + 32, startZ);
        valueSw = noise.GetPosNoise(startX, startZ + 32);
        valueSe = noise.GetPosNoise(startX + 32, startZ + 32);
    }

    public float BiLerp(int localX, int localZ)
    {
        return GameMath.BiLerp(valueNw, valueNe, valueSw, valueSe, (float)localX / 32, (float)localZ / 32);
    }
}