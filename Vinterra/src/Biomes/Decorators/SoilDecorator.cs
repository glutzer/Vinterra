using Vintagestory.API.Common;
using Vintagestory.API.Server;

[Decorator("soil")]
public class SoilDecorator : Decorator
{
    public string[,] grassCodes = { { "soil-verylow-verysparse", "soil-verylow-sparse", "soil-verylow-normal" },
                                    { "soil-low-verysparse", "soil-low-sparse", "soil-low-normal" },
                                    { "soil-medium-verysparse", "soil-medium-sparse", "soil-medium-normal" } };

    public string[] dirtCodes = { "soil-verylow-none", "soil-low-none", "soil-medium-none" };

    public void Resolve(ICoreServerAPI sapi)
    {
        for (int fert = 0; fert < 3; fert++)
        {
            for (int rain = 0; rain < 3; rain++)
            {
                grassIds[fert, rain] = sapi.World.GetBlock(new AssetLocation(grassCodes[fert, rain])).Id;
            }

            dirtIds[fert] = sapi.World.GetBlock(new AssetLocation(dirtCodes[fert])).Id;
        }
    }

    public int[,] grassIds = new int[3, 3];
    public int[] dirtIds = new int[3];

    public override int[] GetBlockLayers(int yPos, double heightGradient, int worldX, int worldZ, float depthNoise, float rainNormal, float fertNormal)
    {
        int fertilityIndex = 0;
        if (fertNormal >= 0.3f)
        {
            fertilityIndex = 2;
        }
        else if (fertNormal >= 0.15f)
        {
            fertilityIndex = 1;
        }

        int coverageIndex = 0;
        if (rainNormal >= 0.33f)
        {
            coverageIndex = 2;
        }
        else if (rainNormal >= 0.12f)
        {
            coverageIndex = 1;
        }

        int[] returnLayers = new int[4];
        for (int i = 0; i < returnLayers.Length; i++)
        {
            if (i == 0)
            {
                returnLayers[i] = grassIds[fertilityIndex, coverageIndex];
                continue;
            }

            returnLayers[i] = dirtIds[fertilityIndex];
        }
        return returnLayers;
    }
}