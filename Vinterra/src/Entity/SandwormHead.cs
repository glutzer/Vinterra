using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

public class SandwormHead : Sandworm
{
    public ICoreAPI api;
    public ICoreServerAPI sapi;

    //On death, move to surface one time
    public bool moved = false;

    //Health
    public EntityBehaviorHealth bhHealth;

    //Worm created here
    public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
    {
        base.Initialize(properties, api, InChunkIndex3d);

        this.api = api;

        if (api.World.Side == EnumAppSide.Server)
        {
            sapi = api as ICoreServerAPI;

            segmentOrder = 1;
            segments = new Dictionary<int, Sandworm>
            {
                { segmentOrder, this }
            };

            GenerateSegments(segmentOrder, segments);

            bhHealth = GetBehavior<EntityBehaviorHealth>();
        }
    }

    public override void GenerateSegments(int segmentOrder, Dictionary<int, Sandworm> segments)
    {
        //Make entity type
        EntityProperties type = api.World.GetEntityType(new AssetLocation("game:sandwormsegment"));
        SandwormSegment segment = (SandwormSegment)api.ClassRegistry.CreateEntity(type);

        //Next segment order is this one + 1
        segment.segmentOrder = segmentOrder + 1;

        //Add the list of segments
        segment.segments = segments;

        //Add the new segment to the list
        segments.Add(segment.segmentOrder, segment);

        //Set pos
        segment.ServerPos.SetPos(ServerPos.BehindCopy(6).XYZ.Add(0, LocalEyePos.Y, 0));
        segment.ServerPos.SetFrom(segment.ServerPos);
        segment.World = World;

        //Spawn segment
        World.SpawnEntity(segment);

        //Generate the next one
        segment.GenerateSegments(segment.segmentOrder, segments);
    }

    //Start looking up
    public Vec3d v = new(0, 1, 0);
    public int stopTicks = 0;
    public double gravity = 0;
    public bool underground = true;
    public int deltaTicks = 0;
    public IPlayer[] playersToDamage;

    public override void OnGameTick(float dt)
    {
        base.OnGameTick(dt);

        if (World.Side == EnumAppSide.Server && Alive)
        {
            IPlayer playerClient = TargetPlayer();

            //If no alive player found within 150 blocks die
            if (playerClient != null)
            {
                EntityPlayer player = playerClient.Entity;

                //Difficulty factor
                float s = 1f;

                //Targets below the player
                Vec3d dPos = player.ServerPos.XYZ.AddCopy(0, -gravity, 0) - ServerPos.XYZ;

                //VECTOR TO TARGET
                Vec3d t = dPos.AddCopy(0, 0, 0).Normalize();

                //Better tracking as he gets closer to death
                v = Slerp(v, t, 0.03 * s);

                //Random events
                if (World.Rand.Next(300) == 1) gravity += 8; //Gravity variance
                if (World.Rand.Next(300) == 2) gravity -= 8;

                //if (World.Rand.Next(500) == 3) stopTicks = 60; //Stop and aim at player
                //if (World.Rand.Next(300) == 4) { v.Set(v.Z, v.Y, v.X); } //Flip axis
                //if (World.Rand.Next(300) == 5) { v.Set(t); } //Go towards player

                double yaw = Math.Atan2(-v.X, -v.Z);
                double roll = Math.Asin(v.Y);
                ServerPos.Yaw = (float)yaw;
                ServerPos.Roll = (float)roll;
                if (stopTicks == 0 || underground)
                {
                    ServerPos.Add(v.X * s, v.Y * s, v.Z * s);
                } 
                else
                {
                    stopTicks--;
                }
            } 
            else
            {
                Die(EnumDespawnReason.Expire);
            }

            //Despawn if too low
            if (ServerPos.Y < 0)
            {
                Die(EnumDespawnReason.Expire);
            }

            //Determine if underground or above ground
            if (World.BlockAccessor.GetBlock(ServerPos.AsBlockPos).Id != 0 || World.BlockAccessor.GetBlock(ServerPos.AsBlockPos).LiquidLevel == 7)
            {
                deltaTicks++;
                if (deltaTicks > 20)
                {
                    api.World.PlaySoundAt(new AssetLocation("game:sounds/rock"), ServerPos.X, ServerPos.Y, ServerPos.Z, null, false, 64, 2); //Reel sound
                    deltaTicks = 0;
                }
                gravity -= 0.2;
                underground = true;
            }
            else
            {
                gravity += 0.2;
                underground = false;
            }

            //Damage players around
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

        //Move corpse to ground
        if (World.Side == EnumAppSide.Server && !moved && !Alive)
        {
            ServerPos.Y = (double)World.SeaLevel + 100d;
            BlockPos pos = ServerPos.AsBlockPos;
            while (!World.BlockAccessor.GetBlock(pos).AllSidesOpaque && pos.Y > 0 && World.BlockAccessor.GetBlock(pos).LiquidLevel != 7)
            {
                pos.Y -= 1;
            }
            ServerPos.SetPos(pos.Add(0, 2, 0));
            moved = true;
        }
    }

    //Gets nearest alive player entity within 150 blocks
    public IPlayer TargetPlayer()
    {
        IPlayer targeted = null;
        foreach (IServerPlayer plr in sapi.World.AllOnlinePlayers)
        {
            if (plr.Entity.ServerPos.DistanceTo(ServerPos.XYZ) > 500) continue;

            if (targeted != null && plr.Entity?.Alive == true)
            {
                if (ServerPos.DistanceTo(plr.Entity.ServerPos) < ServerPos.DistanceTo(targeted.Entity.ServerPos)) targeted = plr;
            }
            else
            {
                if (plr.Entity?.Alive == true) targeted = plr;
            }
        }
        return targeted;
    }

    public static Vec3d Slerp(Vec3d start, Vec3d end, double percent)
    {
        double dot = start.Dot(end);
        GameMath.Clamp(dot, -1.0f, 1.0f);
        double theta = GameMath.Acos(dot) * percent;
        Vec3d RelativeVec = end - start * dot;
        RelativeVec.Normalize();
        return start * GameMath.Cos(theta) + RelativeVec * GameMath.Sin(theta);
    }

    //Immune to weather, refund stability
    public override bool ReceiveDamage(DamageSource damageSource, float damage)
    {
        if (damageSource.Source == EnumDamageSource.Weather) return true;

        //On killing blow
        if (World.Side == EnumAppSide.Server && damage > bhHealth.Health)
        {
            WatchedAttributes.SetFloat("animalWeight", 1);
        }

        return base.ReceiveDamage(damageSource, damage);
    }
}