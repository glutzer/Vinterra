using System;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

[Feature("lava")]
public class FeatureLava : PartialFeature
{
    public FeatureLava(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override bool CanGenerate(int localX, int localZ, GenerationData data)
    {
        return data.specialArea[localX, localZ];
    }

    //Make a 5x5
    public override void Generate(BlockPos blockPos, IServerChunk[] chunkData, LCGRandom rand, Vec2d chunkStart, Vec2d chunkEnd, IWorldGenBlockAccessor blockAccessor)
    {
        int bRadius = (int)(hSize * 20);

        int ASL = blockPos.Y - seaLevel;

        int maxHeight = (int)((sapi.WorldManager.MapSizeY - seaLevel) * 0.55f);

        //Lava column goes down to mantle

        FeatureBoundingBox box = new(blockPos.ToVec3d().Add(-bRadius * 2, -30, -bRadius * 2), blockPos.ToVec3d().Add(bRadius * 2, maxHeight - ASL, bRadius * 2));
        if (!box.SetBounds(chunkStart, chunkEnd)) return;

        int lavaId = Decorator.GetId(blocks[0]);

        Vec2d centerPos = new Vec2d(blockPos.X, blockPos.Z);
        Vec2d currentPos = new Vec2d(0, 0);

        box.ForEachPosition((x, y, z, cPos) =>
        {
            currentPos.X = x;
            currentPos.Y = z;
            if (currentPos.DistanceTo(centerPos) < bRadius)
            {
                if (blockAccessor.GetBlock(cPos.AsBlockPos).Id == 0)
                {
                    blockAccessor.SetBlock(lavaId, cPos.AsBlockPos);
                }
            }
        });
    }
}