using Vintagestory.API.Common;
using Vintagestory.API.Server;

public class FeatureMeteor : PartialFeature
{
    public int meteoriteId;

    public FeatureMeteor(ICoreServerAPI sapi) : base(sapi)
    {
        meteoriteId = sapi.World.GetBlock(new AssetLocation("game:meteorite-iron")).Id;
    }

    /*
    public override void Generate(BlockPos blockPos, int rockId, IBlockAccessor blockAccessor, LCGRandom rand)
    {
        int radius = 10;

        BlockPos tempPos = new BlockPos();

        for (int x = blockPos.X - radius; x < blockPos.X + radius; x++)
        {
            for (int z = blockPos.Z - radius; z < blockPos.Z + radius; z++)
            {
                for (int y = blockPos.Y - radius; y < blockPos.Y + radius; y++)
                {
                    tempPos.Set(x, y, z);

                    if (tempPos.DistanceTo(blockPos) < radius)
                    {
                        blockAccessor.SetBlock(0, tempPos);
                    }
                }
            }
        }

        BlockPos meteorPos = new BlockPos(blockPos.X, blockPos.Y - radius, blockPos.Z);

        for (int x = meteorPos.X - radius; x < meteorPos.X + radius; x++)
        {
            for (int z = meteorPos.Z - radius; z < meteorPos.Z + radius; z++)
            {
                for (int y = meteorPos.Y - radius; y < meteorPos.Y + radius; y++)
                {
                    tempPos.Set(x, y, z);

                    if (tempPos.DistanceTo(meteorPos) < radius / 3)
                    {
                        blockAccessor.SetBlock(meteoriteId, tempPos);
                    }
                }
            }
        }

        for (int x = blockPos.X - radius; x < blockPos.X + radius; x++)
        {
            for (int z = blockPos.Z - radius; z < blockPos.Z + radius; z++)
            {
                for (int y = blockPos.Y - radius; y < blockPos.Y + radius; y++)
                {
                    tempPos.Set(x, y, z);

                    if (rand.NextFloat() < 0.1f)
                    {
                        if (blockAccessor.GetBlock(tempPos).Id != 0 && blockAccessor.GetBlock(tempPos.AddCopy(0, -1, 0)).Id != 0)
                        {
                            blockAccessor.SetBlock(meteoriteId, tempPos);
                        }
                    }
                }
            }
        }
    }
    */
}