using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

/// <summary>
/// Takes 3x3 cached noise maps, stitches them together, generates droplets removing them if they exit the array, then returns the middle part.
/// Also calculates gradients and scatters biomes.
/// Disabled by default (partially).
/// </summary>
public class ErosionGenerator
{
    public ICoreServerAPI sapi;

    public float heightFalloff;
    public float minHeight;

    public int chunkMapWidth;
    public int chunkSize;

    public int erosionRadius; //Area to erode. Impacts performance
    public float inertia;
    public float sedimentCapacityFactor;
    public float minSedimentCapacity;

    public float erodeSpeed;
    public float depositSpeed;
    public float evaporateSpeed;
    public float gravity;

    public int maxDropletLifetime;

    public float initialWaterVolume;
    public float initialSpeed;

    public int[][] erosionBrushIndices;
    public float[][] erosionBrushWeights;

    public int currentErosionRadius;
    public int currentMapSize;

    public int numIterations; //How many times to apply a drop to each chunk

    public LCGRandom rand = new(0);

    public int seaLevel;
    public int mapHeight;

    public int biomeScatterRadius;
    public int biomeScatterDiameter;

    public BiomeSystem biomeSystem;

    public void Initialize()
    {
        heightFalloff = VConfig.Loaded.erosionHeightFalloff;
        minHeight = VConfig.Loaded.erosionMinHeight;
        erosionRadius = VConfig.Loaded.erosionRadius;
        inertia = VConfig.Loaded.erosionInertia;
        sedimentCapacityFactor = VConfig.Loaded.sedimentCapacityFactor;
        minSedimentCapacity = VConfig.Loaded.minSedimentCapacity;
        erodeSpeed = VConfig.Loaded.erodeSpeed;
        depositSpeed = VConfig.Loaded.depositSpeed;
        evaporateSpeed = VConfig.Loaded.evaporateSpeed;
        gravity = VConfig.Loaded.gravity;
        maxDropletLifetime = VConfig.Loaded.maxDropletLifetime;
        initialWaterVolume = VConfig.Loaded.initialWaterVolume;
        initialSpeed = VConfig.Loaded.initialSpeed;
        numIterations = VConfig.Loaded.erosionDroplets;
        biomeScatterRadius = VConfig.Loaded.biomeScatterRadius;
        biomeScatterDiameter = biomeScatterRadius * 2;

        currentMapSize = chunkSize * 3;

        if (erosionBrushIndices == null || currentErosionRadius != erosionRadius)
        {
            InitializeBrushIndices(currentMapSize, erosionRadius);
            currentErosionRadius = erosionRadius;
        }
    }

    public ErosionGenerator(int chunkMapWidth, int chunkSize, int seaLevel, int mapHeight, ICoreServerAPI sapi) 
    {
        this.sapi = sapi;
        this.chunkMapWidth = chunkMapWidth;
        this.chunkSize = chunkSize;
        this.seaLevel = seaLevel;
        this.mapHeight = mapHeight - seaLevel - 4;

        biomeSystem = BiomeSystem.Get(sapi);

        Initialize();
    }

    public void Erode(int chunkX, int chunkZ)
    {
        rand.InitPositionSeed(chunkX, chunkZ);

        List<float[,]> arrays = new();

        List<ushort[,]> biomeArrays = new(); //Biome scattering

        for (int z = -1; z < 2; z++)
        {
            for (int x = -1; x < 2; x++)
            {
                GenerationData tempData = GenerateData.GetOrCreateData(chunkX + x, chunkZ + z, chunkMapWidth, sapi);
                arrays.Add(tempData.erosionMap);
                biomeArrays.Add(tempData.biomeIds); //Biome scattering
            }
        }

        float[] map = new float[chunkSize * chunkSize * 9];
        for (int x = 0; x < chunkSize * 3; x++)
        {
            for (int z = 0; z < chunkSize * 3; z++)
            {
                map[z * currentMapSize + x] = arrays[z / chunkSize * 3 + x / chunkSize][x % chunkSize, z % chunkSize];
            }
        }

        //Biome scattering
        ushort[] biomeMap = new ushort[chunkSize * chunkSize * 9];
        for (int x = 0; x < chunkSize * 3; x++)
        {
            for (int z = 0; z < chunkSize * 3; z++)
            {
                biomeMap[z * currentMapSize + x] = biomeArrays[z / chunkSize * 3 + x / chunkSize][x % chunkSize, z % chunkSize];
            }
        }

        //Apply drops

        for (int iteration = 0; iteration < numIterations; iteration++)
        {
            //Create water droplet at point
            float posX = rand.NextInt(currentMapSize - 1);
            float posY = rand.NextInt(currentMapSize - 1);

            int nodeX = (int)posX;
            int nodeY = (int)posY;

            if (!biomeSystem.biomes[biomeMap[nodeY * currentMapSize + nodeX]].erosion) continue;

            //Initialize direction to 0
            float dirX = 0;
            float dirY = 0;
            float sediment = 0;

            //Initialize variables
            float speed = initialSpeed;
            float water = initialWaterVolume;
            
            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
            {
                nodeX = (int)posX;
                nodeY = (int)posY;

                int dropletIndex = nodeY * currentMapSize + nodeX;

                //Calculate droplet's offset inside the cell (0, 0) = at NW node, (1, 1) = at SE node
                float cellOffsetX = posX - nodeX;
                float cellOffsetY = posY - nodeY;

                //Calculate droplet's height and direction of flow with bilinear interpolation of surrounding heights
                HeightAndGradient heightAndGradient = CalculateHeightAndGradient(map, currentMapSize, posX, posY);

                //Update the droplet's direction and position (move position 1 unit regardless of speed)
                dirX = dirX * inertia - heightAndGradient.gradientX * (1 - inertia);
                dirY = dirY * inertia - heightAndGradient.gradientY * (1 - inertia);

                //Normalize direction
                float len = MathF.Sqrt(dirX * dirX + dirY * dirY);

                if (len != 0)
                {
                    dirX /= len;
                    dirY /= len;
                }

                posX += dirX;
                posY += dirY;

                if ((dirX == 0 && dirY == 0) || posX < 0 || posX >= currentMapSize - 1 || posY < 0 || posY >= currentMapSize - 1)
                {
                    break;
                }

                //Find the droplet's new height and calculate the deltaHeight
                float falloff = GetFalloff(map[dropletIndex]);
                float newHeight = CalculateHeightAndGradient(map, currentMapSize, posX, posY).height;

                float deltaHeight = (newHeight - heightAndGradient.height) * falloff;

                //Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                float sedimentCapacity = Math.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);

                //If carrying more sediment than capacity, or if flowing uphill:
                if (sediment > sedimentCapacity || deltaHeight > 0)
                {
                    //If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (deltaHeight > 0) ? Math.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * depositSpeed;
                    sediment -= amountToDeposit;

                    //Add the sediment to the four nodes of the current cell using bilinear interpolation
                    //Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    if (map[dropletIndex] > minHeight) map[dropletIndex] += amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetY);
                    if (map[dropletIndex + 1] > minHeight) map[dropletIndex + 1] += amountToDeposit * cellOffsetX * (1 - cellOffsetY);
                    if (map[dropletIndex + currentMapSize] > minHeight) map[dropletIndex + currentMapSize] += amountToDeposit * (1 - cellOffsetX) * cellOffsetY;
                    if (map[dropletIndex + currentMapSize + 1] > minHeight) map[dropletIndex + currentMapSize + 1] += amountToDeposit * cellOffsetX * cellOffsetY;
                }
                else
                {
                    //Erode a fraction of the droplet's current carry capacity.
                    //Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                    float amountToErode = Math.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight);

                    //Use erosion brush to erode from all nodes inside the droplet's erosion radius
                    for (int brushPointIndex = 0; brushPointIndex < erosionBrushIndices[dropletIndex].Length; brushPointIndex++)
                    {
                        int nodeIndex = erosionBrushIndices[dropletIndex][brushPointIndex];
                        float weighedErodeAmount = amountToErode * erosionBrushWeights[dropletIndex][brushPointIndex];
                        float deltaSediment = (map[nodeIndex] < weighedErodeAmount) ? map[nodeIndex] : weighedErodeAmount;
                        if (map[nodeIndex] > minHeight)
                        {
                            map[nodeIndex] -= deltaSediment;
                            sediment += deltaSediment;
                        }
                    }
                }

                //Update droplet's speed and water content
                float speedCalc = speed * speed + deltaHeight * gravity;

                if (speedCalc <= 0)
                {
                    break;
                }

                speed = MathF.Sqrt(speedCalc);

                water *= 1 - evaporateSpeed;
            }
        }

        GenerationData data = GenerateData.GetOrCreateData(chunkX, chunkZ, chunkMapWidth, sapi);

        double[,] paddedYLevels = new double[chunkSize + 2, chunkSize + 2];

        //Scatter biomes
        for (int x = chunkSize; x < chunkSize * 2; x++)
        {
            for (int z = chunkSize; z < chunkSize * 2; z++)
            {
                int offsetZ = z + (rand.NextInt(biomeScatterDiameter) - biomeScatterRadius);
                int offsetX = x + (rand.NextInt(biomeScatterDiameter) - biomeScatterRadius);
                data.biomeIds[x - chunkSize, z - chunkSize] = biomeMap[offsetZ * currentMapSize + offsetX];
            }
        }

        //After complete, set terrain height to middle of map
        for (int x = chunkSize - 1; x < chunkSize * 2 + 1; x++)
        {
            for (int z = chunkSize - 1; z < chunkSize * 2 + 1; z++)
            {
                paddedYLevels[x - (chunkSize - 1), z - (chunkSize - 1)] = map[z * currentMapSize + x];
            }
        }

        for (int x = 1; x < paddedYLevels.GetLength(0) - 1; x++)
        {
            for (int z = 1; z < paddedYLevels.GetLength(1) - 1; z++)
            {
                data.heightLevels[x - 1, z - 1] = (ushort)(seaLevel + paddedYLevels[x, z] * mapHeight);

                double n = paddedYLevels[x, z - 1];
                double s = paddedYLevels[x, z + 1];
                double e = paddedYLevels[x + 1, z];
                double w = paddedYLevels[x - 1, z];

                double dx = e - w;
                double dz = s - n;
                double grad = Math.Sqrt(dx * dx + dz * dz);

                data.heightGradient[x - 1, z - 1] = Math.Clamp(grad * mapHeight, 0, 20);
            }
        }
    }

    public void InitializeBrushIndices(int mapSize, int radius)
    {
        erosionBrushIndices = new int[mapSize * mapSize][];
        erosionBrushWeights = new float[mapSize * mapSize][];

        int[] xOffsets = new int[radius * radius * 4];
        int[] yOffsets = new int[radius * radius * 4];
        float[] weights = new float[radius * radius * 4];
        float weightSum = 0;
        int addIndex = 0;

        for (int i = 0; i < erosionBrushIndices.GetLength(0); i++)
        {
            int centreX = i % mapSize;
            int centreY = i / mapSize;

            if (centreY <= radius || centreY >= mapSize - radius || centreX <= radius + 1 || centreX >= mapSize - radius)
            {
                weightSum = 0;
                addIndex = 0;
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        float sqrDst = x * x + y * y;
                        if (sqrDst < radius * radius)
                        {
                            int coordX = centreX + x;
                            int coordY = centreY + y;

                            if (coordX >= 0 && coordX < mapSize && coordY >= 0 && coordY < mapSize)
                            {
                                float weight = 1 - MathF.Sqrt(sqrDst) / radius;
                                weightSum += weight;
                                weights[addIndex] = weight;
                                xOffsets[addIndex] = x;
                                yOffsets[addIndex] = y;
                                addIndex++;
                            }
                        }
                    }
                }
            }

            int numEntries = addIndex;
            erosionBrushIndices[i] = new int[numEntries];
            erosionBrushWeights[i] = new float[numEntries];

            for (int j = 0; j < numEntries; j++)
            {
                erosionBrushIndices[i][j] = (yOffsets[j] + centreY) * mapSize + xOffsets[j] + centreX;
                erosionBrushWeights[i][j] = weights[j] / weightSum;
            }
        }
    }

    public float GetFalloff(float height)
    {
        if (height > heightFalloff) return 1;

        return Math.Max(height / heightFalloff - minHeight, 0);
    }

    public struct HeightAndGradient
    {
        public float height;
        public float gradientX;
        public float gradientY;
    }

    public static HeightAndGradient CalculateHeightAndGradient(float[] nodes, int mapSize, float posX, float posY)
    {
        int coordX = (int)posX;
        int coordY = (int)posY;

        //Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
        float x = posX - coordX;
        float y = posY - coordY;

        //Calculate heights of the four nodes of the droplet's cell
        int nodeIndexNW = coordY * mapSize + coordX;
        float heightNW = nodes[nodeIndexNW];
        float heightNE = nodes[nodeIndexNW + 1];
        float heightSW = nodes[nodeIndexNW + mapSize];
        float heightSE = nodes[nodeIndexNW + mapSize + 1];

        //Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
        float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

        //Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;

        return new HeightAndGradient() { height = height, gradientX = gradientX, gradientY = gradientY };
    }
}