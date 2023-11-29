using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

public class SandwormSegment : Sandworm
{
    public ICoreAPI api;
    public IPlayer[] playersToDamage;

    //Worm created here
    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);

        this.api = api;
    }

    public override void GenerateSegments(int segmentOrder, Dictionary<int, Sandworm> segments)
    {
        //Max 40 segments
        if (segmentOrder == 40) return;

        //Create the type
        EntityProperties type = api.World.GetEntityType(new AssetLocation("game:sandwormsegment"));
        SandwormSegment segment = (SandwormSegment)api.ClassRegistry.CreateEntity(type);

        //Set the next segment to +1 order
        segment.segmentOrder = segmentOrder + 1;

        //Set the list
        segment.segments = segments;

        //Add the new segment
        segments.Add(segment.segmentOrder, segment);

        //Initialize position
        segment.ServerPos.SetPos(SidedPos.BehindCopy(5).XYZ.Add(0, LocalEyePos.Y, 0));
        segment.ServerPos.SetFrom(segment.ServerPos);
        segment.World = World;

        //Spawn entity
        World.SpawnEntity(segment);

        //Recursive
        segment.GenerateSegments(segment.segmentOrder, segments);
    }

    public override void OnGameTick(float dt)
    {
        if (World.Side == EnumAppSide.Server)
        {
            //If the segment exists without a head remove it
            if (segmentOrder == 0 && Alive) Die();
            if (segmentOrder == 0) return;

            //Get the segment in front of it
            Sandworm segment = segments.Get(segmentOrder - 1);

            //If the head is dead remove segment
            if (segments.Get(1)?.Alive != true) Die();

            //Point and move towards next segment
            if (segment != null)
            {
                if (segment.ServerPos.XYZ.DistanceTo(ServerPos.XYZ) > 10f)
                {
                    Vec3d dPos = segment.ServerPos.XYZ - ServerPos.XYZ;
                    double desiredLength = dPos.Length() - 10;
                    dPos.Normalize();

                    ServerPos.Yaw = (float)Math.Atan2(-dPos.X, -dPos.Z);
                    ServerPos.Roll = (float)Math.Asin(dPos.Y);

                    dPos.Mul(desiredLength);
                    ServerPos.Add(dPos.X, dPos.Y, dPos.Z);
                }
            }

            //Damage players
            playersToDamage = World.GetPlayersAround(ServerPos.XYZ.AddCopy(0, 4.5, 0), 6.5f, 6.5f);
            if (playersToDamage != null)
            {
                foreach (IPlayer entity in playersToDamage)
                {
                    entity.Entity.ReceiveDamage(new DamageSource()
                    {
                        Source = EnumDamageSource.Entity,
                        DamageTier = 3,
                        KnockbackStrength = 5,
                        CauseEntity = this,
                        SourceEntity = this,
                        HitPosition = entity.Entity.ServerPos.XYZ,
                        Type = EnumDamageType.BluntAttack,
                        SourcePos = ServerPos.XYZ
                    }, 12);
                }
            }
        }
    }

    public override bool ReceiveDamage(DamageSource damageSource, float damage)
    {
        float multiplier = 0.025f * (40 - segmentOrder);
        if (segments != null)
        {
            if (damageSource.SourceEntity?.Code?.FirstCodePart() == "arrow")
            {
                segments.Get(1).ReceiveDamage(damageSource, damage * 0.5f * multiplier);
                damageSource.SourceEntity.Die();
            } 
            else
            {
                segments.Get(1).ReceiveDamage(damageSource, damage * multiplier);
            }
        }
        return true;
    }
}