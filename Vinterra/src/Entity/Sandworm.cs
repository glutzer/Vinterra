using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

public abstract class Sandworm : EntityAgent
{
    public Dictionary<int, Sandworm> segments;
    public int segmentOrder = 0;
    public double speed = 0.1;

    //Worm created here
    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);
    }

    public virtual void GenerateSegments(int segmentOrder, Dictionary<int, Sandworm> segments) 
    {

    }

    //Apply gravity if it's dead
    public override bool ApplyGravity
    {
        get { return Alive; }
    }
}