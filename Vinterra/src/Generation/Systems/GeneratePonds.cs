using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

public class GeneratePonds : WorldGenBase
{
    public BiomeSystem biomeSystem;

    public ICoreServerAPI sapi;
    public LCGRandom rand;
    public IWorldGenBlockAccessor blockAccessor;

    public readonly QueueOfInt searchPositionsDeltas = new();
    public readonly QueueOfInt pondPositions = new();

    public int searchSize;
    public int minBoundary;
    public int maxBoundary;

    public int climateUpLeft;
    public int climateUpRight;
    public int climateBotLeft;
    public int climateBotRight;

    public int[] didCheckPosition;
    public int iteration;

    public Dictionary<int, int> gravelMappings = new();
    public Dictionary<int, int> sandMappings = new();
    public Dictionary<int, int> hardenedSandMappings = new();

    public override double ExecuteOrder() => 0.4;

    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        sapi.Event.InitWorldGenerator(InitWorldGenerator, "standard");
        sapi.Event.ChunkColumnGeneration(ChunkColumnGeneration, EnumWorldGenPass.TerrainFeatures, "standard");
        sapi.Event.GetWorldgenBlockAccessor(GetWorldgenBlockAccessor);

        biomeSystem = BiomeSystem.Get(sapi);
    }

    public void GetWorldgenBlockAccessor(IChunkProviderThread chunkProvider)
    {
        blockAccessor = chunkProvider.GetBlockAccessor(true);
    }

    public void InitWorldGenerator()
    {
        LoadGlobalConfig(sapi);

        rand = new LCGRandom(sapi.WorldManager.Seed - 12);
        minBoundary = -chunkSize + 1;
        maxBoundary = 2 * chunkSize - 1;
        searchSize = 3 * chunkSize;
        didCheckPosition = new int[searchSize * searchSize];

        hardenedSandMappings = sapi.ModLoader.GetModSystem<GenerateBlockLayers>().hardenedSandMappings;
        sandMappings = sapi.ModLoader.GetModSystem<GenerateBlockLayers>().sandMappings;
        gravelMappings = sapi.ModLoader.GetModSystem<GenerateBlockLayers>().gravelMappings;
    }

    public void ChunkColumnGeneration(IChunkColumnGenerateRequest request)
    {
        blockAccessor.BeginColumn();

        IServerChunk[] chunks = request.Chunks;
        int chunkX = request.ChunkX;
        int chunkZ = request.ChunkZ;

        rand.InitPositionSeed(chunkX, chunkZ);
        int maxHeight = mapHeight - 1;
        int pondYPos;

        ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;

        GenerationData data = GenerateData.GetOrCreateErodedData(chunkX, chunkZ, chunkMapWidth, sapi);

        BiomeType biome = biomeSystem.biomes[data.biomeIds[16, 16]];

        if (!biome.hasPonds) return;

        //Could use the biomes climate or something
        IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
        int regionChunkSize = sapi.WorldManager.RegionSize / chunkSize;
        float factor = (float)climateMap.InnerSize / regionChunkSize;
        int rlX = chunkX % regionChunkSize;
        int rlZ = chunkZ % regionChunkSize;
        climateUpLeft = climateMap.GetUnpaddedInt((int)(rlX * factor), (int)(rlZ * factor));
        climateUpRight = climateMap.GetUnpaddedInt((int)(rlX * factor + factor), (int)(rlZ * factor));
        climateBotLeft = climateMap.GetUnpaddedInt((int)(rlX * factor), (int)(rlZ * factor + factor));
        climateBotRight = climateMap.GetUnpaddedInt((int)(rlX * factor + factor), (int)(rlZ * factor + factor));
        int climateMid = GameMath.BiLerpRgbColor(0.5f, 0.5f, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
        int temperature = (climateMid >> 16) & 0xff;
        int rain = (climateMid >> 8) & 0xff;

        //Pond density (chance?) based on rain
        float pondDensity = Math.Max(0, 4 * (rain - 10) / 255f);

        float seaLevelTemp = TerraGenConfig.GetScaledAdjustedTemperatureFloat(temperature, 0);

        //Lower temperature below -5 degrees
        pondDensity -= Math.Max(0, 5 - seaLevelTemp);

        float maxTries = pondDensity * 10 * biome.pondFrequency;
        int dx, dz;

        //Above ground ponds
        while (maxTries-- > 0f)
        {
            if (maxTries < 1f && rand.NextFloat() > maxTries) break;

            dx = rand.NextInt(chunkSize);
            dz = rand.NextInt(chunkSize);

            pondYPos = heightMap[dz * chunkSize + dx] + 1;
            if (pondYPos <= 0 || pondYPos >= maxHeight) return;

            TryPlacePondAt(dx, pondYPos, dz, chunkX, chunkZ, biome);
        }

        //Underground ponds
        int iMaxTries = 600;
        while (iMaxTries-- > 0)
        {
            dx = rand.NextInt(chunkSize);
            dz = rand.NextInt(chunkSize);

            pondYPos = (int)(rand.NextFloat() * heightMap[dz * chunkSize + dx]);
            if (pondYPos <= 0 || pondYPos >= maxHeight) return; //Randomly exits, e.g. 1/96 of the time pondYPos will be 0

            int chunkY = pondYPos / chunkSize;
            int dy = pondYPos % chunkSize;
            int blockId = chunks[chunkY].Data.GetBlockIdUnsafe((dy * chunkSize + dz) * chunkSize + dx);

            while (blockId == 0 && pondYPos > 20)
            {
                pondYPos--;

                chunkY = pondYPos / chunkSize;
                dy = pondYPos % chunkSize;
                blockId = chunks[chunkY].Data.GetBlockIdUnsafe((dy * chunkSize + dz) * chunkSize + dx);

                if (blockId != 0)
                {
                    TryPlacePondAt(dx, pondYPos, dz, chunkX, chunkZ, biome);
                }
            }
        }
    }

    public void TryPlacePondAt(int dx, int pondYPos, int dz, int chunkX, int chunkZ, BiomeType biome, int depth = 0)
    {
        if (depth == 0 && pondYPos < seaLevel)
        {
            pondPositions.Clear();
            searchPositionsDeltas.Clear();
            return;
        }

        int searchSize = this.searchSize;
        int minBoundary = this.minBoundary;
        int maxBoundary = this.maxBoundary;

        int waterId = biome.waterId;
        int ndx, ndz;
        searchPositionsDeltas.Clear();
        pondPositions.Clear();

        int basePosX = chunkX * chunkSize;
        int basePosZ = chunkZ * chunkSize;
        Vec2i temp = new();

        // The starting block is an air block
        int arrayIndex = (dz + chunkSize) * searchSize + dx + chunkSize;
        searchPositionsDeltas.Enqueue(arrayIndex);
        pondPositions.Enqueue(arrayIndex);
        int iteration = ++this.iteration;
        didCheckPosition[arrayIndex] = iteration;

        BlockPos tempPos = new();

        while (searchPositionsDeltas.Count > 0)
        {
            int p = searchPositionsDeltas.Dequeue();
            int px = p % searchSize - chunkSize;
            int pz = p / searchSize - chunkSize;

            foreach (BlockFacing facing in BlockFacing.HORIZONTALS)
            {
                Vec3i facingNormal = facing.Normali;
                ndx = px + facingNormal.X;
                ndz = pz + facingNormal.Z;

                arrayIndex = (ndz + chunkSize) * searchSize + ndx + chunkSize;

                //If not already checked, see if we can spread water into this position (queue it) - or do nothing if it's a pond border
                if (didCheckPosition[arrayIndex] != iteration)
                {
                    didCheckPosition[arrayIndex] = iteration;

                    temp.Set(basePosX + ndx, basePosZ + ndz);

                    tempPos.Set(temp.X, pondYPos - 1, temp.Y);
                    Block belowBlock = blockAccessor.GetBlock(tempPos);

                    bool inBoundary = ndx > minBoundary && ndz > minBoundary && ndx < maxBoundary && ndz < maxBoundary;

                    //Only continue when every position is within our 3x3 chunk search area and has a more or less solid block below (or water)
                    //Checks blocks under edges as well, not needed
                    if (inBoundary && (belowBlock.GetLiquidBarrierHeightOnSide(BlockFacing.UP, tempPos) >= 1.0 || belowBlock.BlockId == waterId))
                    {
                        tempPos.Set(temp.X, pondYPos, temp.Y);
                        if (blockAccessor.GetBlock(tempPos).GetLiquidBarrierHeightOnSide(facing.Opposite, tempPos) < 0.9)
                        {
                            searchPositionsDeltas.Enqueue(arrayIndex);
                            pondPositions.Enqueue(arrayIndex);
                        }
                    }

                    //Exit if those conditions were failed - it means the pond is leaking from the bottom, or the sides (extends beyond min/max boundary)
                    else
                    {
                        pondPositions.Clear();
                        searchPositionsDeltas.Clear();
                        return;
                    }
                }
            }
        }

        // Now place water into the pondPositions

        int curChunkX, curChunkZ;
        int prevChunkX = -1, prevChunkZ = -1;
        int regionChunkSize = sapi.WorldManager.RegionSize / chunkSize;
        IMapChunk mapChunk = null;
        IServerChunk chunk = null;
        IServerChunk chunkOneBlockBelow = null;

        int localY = GameMath.Mod(pondYPos, chunkSize);

        bool extraPondDepth = rand.NextFloat() > 0.5f;

        while (pondPositions.Count > 0)
        {
            int p = pondPositions.Dequeue();
            int px = p % searchSize - chunkSize + basePosX;
            int pz = p / searchSize - chunkSize + basePosZ;
            curChunkX = px / chunkSize;
            curChunkZ = pz / chunkSize;

            int localX = GameMath.Mod(px, chunkSize);
            int localZ = GameMath.Mod(pz, chunkSize);

            //Get correct chunk and correct climate data if we don't have it already
            if (curChunkX != prevChunkX || curChunkZ != prevChunkZ)
            {
                chunk = (IServerChunk)blockAccessor.GetChunk(curChunkX, pondYPos / chunkSize, curChunkZ);
                if (chunk == null) chunk = sapi.WorldManager.GetChunk(curChunkX, pondYPos / chunkSize, curChunkZ);
                chunk.Unpack();

                if (localY == 0)
                {
                    chunkOneBlockBelow = (IServerChunk)blockAccessor.GetChunk(curChunkX, (pondYPos - 1) / chunkSize, curChunkZ);
                    if (chunkOneBlockBelow == null) return;
                    chunkOneBlockBelow.Unpack();
                }
                else
                {
                    chunkOneBlockBelow = chunk;
                }

                mapChunk = chunk.MapChunk;

                //Didn't this already get defined earlier?
                IntDataMap2D climateMap = mapChunk.MapRegion.ClimateMap;
                float fac = (float)climateMap.InnerSize / regionChunkSize;
                int rlX = curChunkX % regionChunkSize;
                int rlZ = curChunkZ % regionChunkSize;
                climateUpLeft = climateMap.GetUnpaddedInt((int)(rlX * fac), (int)(rlZ * fac));
                climateUpRight = climateMap.GetUnpaddedInt((int)(rlX * fac + fac), (int)(rlZ * fac));
                climateBotLeft = climateMap.GetUnpaddedInt((int)(rlX * fac), (int)(rlZ * fac + fac));
                climateBotRight = climateMap.GetUnpaddedInt((int)(rlX * fac + fac), (int)(rlZ * fac + fac));

                prevChunkX = curChunkX;
                prevChunkZ = curChunkZ;

                chunkOneBlockBelow.MarkModified();
                chunk.MarkModified();
            }

            //Raise heightmap by 1 (only relevant for above-ground ponds)
            if (mapChunk.RainHeightMap[localZ * chunkSize + localX] < pondYPos) mapChunk.RainHeightMap[localZ * chunkSize + localX] = (ushort)pondYPos;

            //Identify correct climate at this position - could be optimised if we place water into ponds in columns instead of layers
            int climate = GameMath.BiLerpRgbColor((float)localX / chunkSize, (float)localZ / chunkSize, climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
            float temperature = TerraGenConfig.GetScaledAdjustedTemperatureFloat((climate >> 16) & 0xff, pondYPos - TerraGenConfig.seaLevel);

            //1. Place water or ice block
            int index3d = (localY * chunkSize + localZ) * chunkSize + localX;
            Block existing = sapi.World.GetBlock(chunk.Data.GetBlockId(index3d, BlockLayersAccess.Solid));
            if (existing.BlockMaterial == EnumBlockMaterial.Plant)
            {
                chunk.Data.SetBlockAir(index3d);
                if (existing.EntityClass != null)
                {
                    tempPos.Set(curChunkX * chunkSize + localX, pondYPos, curChunkZ * chunkSize + localZ);
                    chunk.RemoveBlockEntity(tempPos);
                }
            }
            chunk.Data.SetFluid(index3d, temperature < -5 ? globalConfig.lakeIceBlockId : waterId);

            //2. Let's make a nice muddy gravel sea bed
            //Need to check the block below first
            int index = localY == 0 ? ((31 * chunkSize + localZ) * chunkSize + localX) : (((localY - 1) * chunkSize + localZ) * chunkSize + localX);

            Block belowBlock = sapi.World.Blocks[chunkOneBlockBelow.Data.GetFluid(index)];

            // Water below? Seabed already placed
            if (belowBlock.IsLiquid()) continue;

            int rockBlockId = mapChunk.TopRockIdMap[localZ * chunkSize + localX];
            if (rockBlockId == 0) continue;

            //Lake bed code will always be the one defined in the biome
            int bedId = biome.bedId;
            if (bedId == 0)
            {
                chunkOneBlockBelow.Data[index] = gravelMappings.Get(chunkOneBlockBelow.Data[index], globalConfig.defaultRockId);
            }
            else if (bedId == -1)
            {
                chunkOneBlockBelow.Data[index] = sandMappings.Get(chunkOneBlockBelow.Data[index], globalConfig.defaultRockId);
            }
            else
            {
                chunkOneBlockBelow.Data[index] = biome.bedId;
            }
        }

        if (extraPondDepth)
        {
            TryPlacePondAt(dx, pondYPos + 1, dz, chunkX, chunkZ, biome, depth + 1);
        }
    }
}