using System;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

[Feature("boulder")]
public class FeatureBoulder : PartialFeature
{
    public FeatureBoulder(ICoreServerAPI sapi) : base(sapi)
    {
    }

    public override void Generate(BlockPos blockPos, IServerChunk[] chunkData, LCGRandom rand, Vec2d chunkStart, Vec2d chunkEnd, IWorldGenBlockAccessor blockAccessor)
    {
        int bRadius = Math.Max((int)((hSize - hSizeVariance + rand.NextFloat() * hSizeVariance) * 5), 1);

        blockPos.Sub(0, bRadius, 0);

        FeatureBoundingBox box = new(blockPos.ToVec3d().Add(-bRadius * 2, -bRadius * 2, -bRadius * 2), blockPos.ToVec3d().Add(bRadius * 2, bRadius * 2, bRadius * 2));

        if (!box.SetBounds(chunkStart, chunkEnd)) return;

        Vec3d centerPos = blockPos.ToVec3d();

        int rockId = Decorator.GetId(blocks[0]);

        box.ForEachPosition((x, y, z, cPos) =>
        {
            //bRadius + noise.GetNoise(x, y, z) * (bRadius / 2)
            //Random spheres for now until a better way to distort
            if (cPos.DistanceTo(centerPos) < bRadius)
            {
                blockAccessor.SetBlock(rockId, new BlockPos(x, y, z));
            }
        });
    }

    public override bool CanGenerate(int localX, int localZ, GenerationData data)
    {
        return data.heightGradient[localX, localZ] < 2;
    }
}