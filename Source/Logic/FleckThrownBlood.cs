using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BloodAnimations
{
    //Modified version of RimWorld.FleckThrown to be used by our blood flecks, unfortunately a lot of code needs to be duplicated because struct's can't be inherited
    //We are using FleckAttachedLink to pass the thing information needed to create a filth after the fleck reaches its final position.
    //
    //Changes from original are marked by comments with ***ADDED/REMOVED***
    //New functionality:
    //1. Reduced skidding, because it looks weird for blood to skid
    //2. Leave filth after fleck reaches final destination
    //3. Remove fleck if it stops over water or other invalid terrain
    public struct FleckThrownBlood : IFleck
    {
        public FleckStatic baseData;

        public float airTimeLeft;

        public Vector3 velocity;

        public float rotationRate;

        public FleckAttachLink link;

        private Vector3 attacheeLastPosition;

        public float orbitSpeed;

        public float archHeight;

        public float? archDuration;

        public float orbitSnapStrength;

        public const float MinSpeed = 0.02f;

        public const float MinOrbitSpeed = 0.2f;

        public float orbitDistance;

        private float orbitAccum;

        public bool Flying => airTimeLeft > 0f;

        public float skidSpeedMultiplierPerTick; //***ADDED***

        public bool Skidding
        {
            get
            {
                if (!Flying)
                {
                    return Speed > 0.01f;
                }
                return false;
            }
        }

        public bool Orbiting => orbitSpeed != 0f;

        public bool Arching => archHeight != 0f;

        public Vector3 Velocity
        {
            get
            {
                return velocity;
            }
            set
            {
                velocity = value;
            }
        }

        public float MoveAngle
        {
            get
            {
                return velocity.AngleFlat();
            }
            set
            {
                SetVelocity(value, Speed);
            }
        }

        public float Speed
        {
            get
            {
                return velocity.MagnitudeHorizontal();
            }
            set
            {
                if (value == 0f)
                {
                    velocity = Vector3.zero;
                }
                else if (velocity == Vector3.zero)
                {
                    velocity = new Vector3(value, 0f, 0f);
                }
                else
                {
                    velocity = velocity.normalized * value;
                }
            }
        }

        private void ApplyOrbit(float deltaTime, ref Vector3 nextPosition)
        {
            orbitAccum += deltaTime * orbitSpeed;
            Vector3 vector = new Vector3(Mathf.Cos(orbitAccum), 0f, Mathf.Sin(orbitAccum)) * orbitDistance;
            Vector3 vector2 = link.Target.CenterVector3 + vector;
            MoveAngle = (vector2 - baseData.position.worldPosition).AngleFlat();
            nextPosition = Vector3.Lerp(nextPosition, vector2, orbitSnapStrength);
        }

        public void Setup(FleckCreationData creationData)
        {
            baseData = default(FleckStatic);
            baseData.Setup(creationData);
            airTimeLeft = creationData.airTimeLeft ?? 999999f;
            attacheeLastPosition = new Vector3(-1000f, -1000f, -1000f);
            link = creationData.link;
            if (link.Linked)
            {
                attacheeLastPosition = link.LastDrawPos;
            }
            rotationRate = creationData.rotationRate;
            SetVelocity(creationData.velocityAngle, creationData.velocitySpeed);
            if (creationData.velocity.HasValue)
            {
                velocity += creationData.velocity.Value;
            }
            orbitSpeed = creationData.orbitSpeed;
            orbitSnapStrength = creationData.orbitSnapStrength;
            if (Orbiting)
            {
                orbitDistance = (link.Target.CenterVector3 - baseData.position.worldPosition).MagnitudeHorizontal();
                orbitAccum = Rand.Range(0f, (float)Math.PI * 2f);
                if (Mathf.Abs(orbitSpeed) < 0.2f)
                {
                    orbitSpeed = 0.2f * GenMath.Sign(orbitSpeed);
                }
            }
            archHeight = creationData.def.archHeight.RandomInRange;
            archDuration = creationData.def.archDuration.RandomInRange;

            //***ADDED*** Overrides vanilla: Rand.Range(0.3f, 0.95f);
            skidSpeedMultiplierPerTick = 0.8f;
            if (link.Target.IsValid && link.Target.HasThing)
            {
                Pawn target = null;
                if (link.Target.Thing is Pawn pawn)
                    target = pawn;
                else if (link.Target.Thing is Corpse corpse)
                    target = corpse.InnerPawn;

                if (target?.RaceProps != null && target.RaceProps.IsFlesh)
                    skidSpeedMultiplierPerTick = 0.6f;
            }//***END ADDED***
        }


        public bool TimeInterval(float deltaTime, Map map)
        {
            //Log.Message($"EndOfLife:{baseData.EndOfLife}, Lifespan:{baseData.def.Lifespan}, Agesec:{baseData.ageSecs}, defName:{baseData.def.defName}, def.solidTime:{baseData.def.solidTime}");
            if (baseData.TimeInterval(deltaTime, map))
            {
                return true;
            }
            if (!Flying && !Skidding)
            {
                return false;
            }
            if (baseData.def.rotateTowardsMoveDirection && velocity != default(Vector3))
            {
                baseData.exactRotation = velocity.AngleFlat() + baseData.def.rotateTowardsMoveDirectionExtraAngle;
            }
            else
            {
                baseData.exactRotation += rotationRate * deltaTime;
            }
            velocity += baseData.def.acceleration * deltaTime;
            if (baseData.def.speedPerTime != FloatRange.Zero)
            {
                Speed = Mathf.Max(Speed + baseData.def.speedPerTime.RandomInRange * deltaTime, 0f);
            }
            if (airTimeLeft > 0f)
            {
                airTimeLeft -= deltaTime;
                if (airTimeLeft < 0f)
                {
                    airTimeLeft = 0f;
                }
                if (airTimeLeft <= 0f && !baseData.def.landSound.NullOrUndefined())
                {
                    baseData.def.landSound.PlayOneShot(new TargetInfo(new IntVec3(baseData.position.worldPosition), map));
                }

                //***ADDED*** Spawns filth at the end
                if (airTimeLeft <= 0f && map != null)
                {
                    IntVec3 worldPos = new IntVec3(baseData.position.worldPosition);
                    if (worldPos.InBounds(map))
                    {
                        if (FilthMaker.TerrainAcceptsFilth(worldPos.GetTerrain(map), ThingDefOf.Filth_Blood, FilthSourceFlags.None))
                        {
                            if (link.Target.IsValid && link.Target.HasThing)
                            {
                                Pawn target = null;
                                if (link.Target.Thing is Pawn pawn)
                                    target = pawn;
                                else if (link.Target.Thing is Corpse corpse)
                                    target = corpse.InnerPawn;

                                if (Rand.Chance(Settings.filthMultiplier))
                                {
                                    if (target != null)
                                    {
                                        ThingDef bloodDef = BloodDefUtility.GetBloodDef(target);
                                        if (bloodDef != null && target.MapHeld != null)
                                            FilthMaker.TryMakeFilth(worldPos, target.MapHeld, bloodDef, 1, FilthSourceFlags.None, false);
                                    }
                                    else if (link.Target.Thing.def?.filthLeaving != null && link.Target.Thing.MapHeld != null)
                                        FilthMaker.TryMakeFilth(worldPos, link.Target.Thing.MapHeld, link.Target.Thing.def.filthLeaving, 1, FilthSourceFlags.None);
                                }
                            }
                        }
                        else
                        {
                            baseData.ageSecs = baseData.SolidTime + (baseData.def.fadeOutTime * 0.7f); //Fade fleck immediately if it lands on a tile that doesn't accept blood filth, like water
                        }
                    }
                }//***END ADDED***
            }
            if (Skidding)
            {
                Speed *= skidSpeedMultiplierPerTick; //***CHANGED TO USE LOCAL SKID MULTIPLIER INSTEAD OF PARENT'S***
                rotationRate *= skidSpeedMultiplierPerTick; //***CHANGED TO USE LOCAL SKID MULTIPLIER INSTEAD OF PARENT'S***
                if (Speed < 0.02f)
                {
                    Speed = 0f;
                }
            }
            FleckDrawPosition position = NextPosition(deltaTime);
            if (Orbiting)
            {
                ApplyOrbit(deltaTime, ref position.worldPosition);
            }
            IntVec3 intVec = new IntVec3(position.worldPosition);
            if (intVec != new IntVec3(baseData.position.worldPosition))
            {
                if (!intVec.InBounds(map))
                {
                    return true;
                }
                if (baseData.def.collide && intVec.Filled(map))
                {
                    WallHit();
                    return false;
                }
            }
            baseData.position = position;
            return false;
        }

        private FleckDrawPosition NextPosition(float deltaTime)
        {
            Vector3 worldPos = baseData.position.worldPosition + velocity * deltaTime;
            float height = 0f;
            Vector3 attachedOffset = Vector3.zero;
            if (Arching)
            {
                float x = Mathf.Clamp01(baseData.ageSecs / archDuration.Value);
                height = ((baseData.def.archCurve != null) ? (archHeight * baseData.def.archCurve.Evaluate(x)) : (archHeight * GenMath.InverseParabola(x)));
            }

            /* ***REMOVED*** We're only using the link to pass the target info needed to create the filth, don't want flecks to attach to a pawn's changing position.  
            if (link.Target.HasThing)
            {
                bool flag = link.detachAfterTicks == -1 || baseData.ageTicks < link.detachAfterTicks;
                if (!link.Target.ThingDestroyed && flag)
                {
                    link.UpdateDrawPos();
                }
                Vector3 attachedDrawOffset = baseData.def.attachedDrawOffset;
                if (baseData.def.attachedToHead && link.Target.Thing is Pawn pawn && pawn.story != null)
                {
                    attachedOffset = pawn.Drawer.renderer.BaseHeadOffsetAt((pawn.GetPosture() == PawnPosture.Standing) ? Rot4.North : pawn.Drawer.renderer.LayingFacing()).RotatedBy(pawn.Drawer.renderer.BodyAngle(PawnRenderFlags.None));
                }
                Vector3 vector = link.LastDrawPos - attacheeLastPosition;
                worldPos += vector;
                worldPos += attachedDrawOffset;
                worldPos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                attacheeLastPosition = link.LastDrawPos;
            } */

            Vector3 anchorOffset = (new Vector3(0.5f, 0f, 0.5f) - baseData.def.scalingAnchor).ScaledBy(baseData.AddedScale);
            return new FleckDrawPosition(worldPos, height, anchorOffset, baseData.def.unattachedDrawOffset, attachedOffset);
        }

        public void SetVelocity(float angle, float speed)
        {
            velocity = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * speed;
        }

        public void Draw(DrawBatch batch)
        {
            baseData.Draw(batch);
        }

        private void WallHit()
        {
            airTimeLeft = 0f;
            Speed = 0f;
            rotationRate = 0f;
        }

        public Vector3 GetPosition()
        {
            return baseData.position.worldPosition;
        }
    }
}
