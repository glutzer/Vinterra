using Vintagestory.API.Common;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

public class BlockPatchConfigExtended : BlockPatchConfig
{
    public BlockPatchExtended[] extendedNonTreePatches;

    public static bool CanGeneratePatch(BlockPatchExtended patch, Block onBlock, int mapSizeY, int climate, int y, float forestNormal, float shrubNormal, double riverDistance, bool beach)
    {
        if ((patch.Placement == EnumBlockPatchPlacement.NearWater || patch.Placement == EnumBlockPatchPlacement.UnderWater) && onBlock.LiquidCode != "water") return false;
        if ((patch.Placement == EnumBlockPatchPlacement.NearSeaWater || patch.Placement == EnumBlockPatchPlacement.UnderSeaWater) && onBlock.LiquidCode != "saltwater") return false;

        //Suitable at these forest levels? 0-1
        if (forestNormal < patch.MinForest || forestNormal > patch.MaxForest || shrubNormal < patch.MinShrub || forestNormal > patch.MaxShrub) return false;

        //Suitable at these rain levels? 0-1
        int rain = TerraGenConfig.GetRainFall((climate >> 8) & 0xff, y);
        float rainNormal = rain / 255f;
        if (rainNormal < patch.MinRain || rainNormal > patch.MaxRain) return false;

        //Suitable at this temperature? Uses actual temperature
        int temp = TerraGenConfig.GetScaledAdjustedTemperature((climate >> 16) & 0xff, y - TerraGenConfig.seaLevel);
        if (temp < patch.MinTemp || temp > patch.MaxTemp) return false;

        //Suitable at this height? 0 = sea level 1 = map height. Negative values generate below sea level?
        float heightNormal = ((float)y - TerraGenConfig.seaLevel) / ((float)mapSizeY - TerraGenConfig.seaLevel);
        if (heightNormal < patch.MinY || heightNormal > patch.MaxY) return false;

        //Fertility suitable? 0-1
        float fertilityNormal = TerraGenConfig.GetFertility(rain, temp, heightNormal) / 255f;
        if (fertilityNormal <= patch.MinFertility || fertilityNormal >= patch.MaxFertility) return false;

        //Beach only patches
        if (patch.onBeaches && beach == false) return false;

        //River only patches
        if (patch.nearRivers && riverDistance > patch.riverDistance) return false;

        return true;
    }
}