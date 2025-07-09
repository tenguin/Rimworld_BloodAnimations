using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace BloodAnimations
{

    [HarmonyPatch(typeof(Verb))]
    public static class Verb_TryCastNextBurstShot
    {
        [HarmonyPostfix]
        [HarmonyPatch("TryCastNextBurstShot")]
        public static void TryCastNextBurstShot(Verb __instance, ref int ___burstShotsLeft)
        {
            if (!Settings.bulletCasingToggle || __instance?.verbProps?.verbClass == null || !__instance.Caster.SpawnedOrAnyParentSpawned || __instance.Caster?.Map == null || __instance.EquipmentSource == null || !(__instance.verbProps.muzzleFlashScale > 0.01f) || __instance.verbProps.verbClass == typeof(Verb_ShootOneUse))
                return;

            if (__instance.verbProps.verbClass == typeof(Verb_Shoot))
            {
                if (__instance.CasterIsPawn && __instance.CasterPawn.equipment?.Primary?.def?.defName != null && __instance.GetProjectile()?.projectile?.damageDef != null && __instance.CasterPawn.equipment.Primary.def.techLevel >= TechLevel.Industrial)
                {
                    //Log.Message($"defName:{__instance.CasterPawn.equipment.Primary.def.defName}, shotsLeft:{___burstShotsLeft + 1}, shotsPerBurst:{__instance.verbProps.burstShotCount}");
                    //Log.Message($"verbClass:{__instance.verbProps.verbClass}, defName:{__instance.CasterPawn.equipment.Primary.def.defName}, techLvl:{__instance.CasterPawn.equipment.Primary.def.techLevel}, damageDef:{__instance.GetProjectile()?.projectile.damageDef}, dmg:{__instance.GetProjectile().projectile.GetDamageAmount(1f)}");
                    ThingDef filthDef = null;
                    float exactRotation = (__instance.CasterPawn.TargetCurrentlyAimingAt.CenterVector3 - __instance.CasterPawn.DrawPos).AngleFlat();
                    if (__instance.CasterPawn.equipment.Primary.def.defName.Contains("Charge", StringComparison.OrdinalIgnoreCase) || __instance.CasterPawn.equipment.Primary.def.defName.Contains("Laser", StringComparison.OrdinalIgnoreCase) || __instance.CasterPawn.equipment.Primary.def.defName.Contains("Plasma", StringComparison.OrdinalIgnoreCase))
                    {
                        if (___burstShotsLeft == 0)
                        {
                            ThrowCasing(__instance.CasterPawn, __instance.Caster.Map, __instance.GetProjectile().projectile.GetDamageAmount(1f, null), ParticleDefOf.Fuu_BulletCasingCharge, exactRotation);
                            filthDef = ParticleDefOf.Filth_BulletCasingsCharge;
                        }
                        else
                            return;
                    }
                    else if (__instance.GetProjectile().projectile.damageDef == DamageDefOf.Bullet && __instance.CasterPawn.equipment.Primary.def.defName.Contains("Shotgun", StringComparison.OrdinalIgnoreCase))
                    {
                        ThrowCasing(__instance.CasterPawn, __instance.Caster.Map, __instance.GetProjectile().projectile.GetDamageAmount(1f, null), ParticleDefOf.Fuu_BulletCasingShotgun, exactRotation);
                        filthDef = ParticleDefOf.Filth_BulletCasingsShotgun;
                    }
                    else if (__instance.GetProjectile().projectile.damageDef == DamageDefOf.Bullet)
                    {
                        ThrowCasing(__instance.CasterPawn, __instance.Caster.Map, __instance.GetProjectile().projectile.GetDamageAmount(1f, null), ParticleDefOf.Fuu_BulletCasingRifle, exactRotation);
                        filthDef = ParticleDefOf.Filth_BulletCasingsRifle;
                    }

                    if (Settings.bulletCasingFilthToggle && filthDef != null && Rand.Chance(0.15f))
                    {
                        IntVec3 pos = new IntVec3(__instance.Caster.Position.x + Facing(exactRotation), 0, __instance.Caster.Position.z);
                        if (pos.InBounds(__instance.Caster.Map))
                            FilthMaker.TryMakeFilth(pos, __instance.Caster.Map, filthDef);
                    }
                }
                else if (__instance.Caster?.def?.building?.turretGunDef?.defName != null && __instance.Caster.def.building.IsTurret && __instance.GetProjectile()?.projectile?.damageDef != null)
                {
                    //Log.Message($"defName:{__instance.Caster.def.building.turretGunDef.defName}, shotsLeft:{___burstShotsLeft + 1}, shotsPerBurst:{__instance.verbProps.burstShotCount}");
                    //Log.Message($"defName:{__instance.Caster.def.building.turretGunDef.defName}, projectileDmg:{__instance.GetProjectile().projectile.GetDamageAmount(1f)}, IsTurret:{__instance.Caster.def.building.IsTurret}");
                    ThingDef filthDef = null;
                    if (__instance.Caster.def.building.turretGunDef.defName.Contains("Charge", StringComparison.OrdinalIgnoreCase) || __instance.Caster.def.building.turretGunDef.defName.Contains("Laser", StringComparison.OrdinalIgnoreCase) || __instance.Caster.def.building.turretGunDef.defName.Contains("Plasma", StringComparison.OrdinalIgnoreCase))
                    {
                        if (___burstShotsLeft == 0)
                        {
                            ThrowCasingTurret(__instance.Caster, __instance.Caster.Map, __instance.GetProjectile().projectile.GetDamageAmount(1f, null), ParticleDefOf.Fuu_BulletCasingCharge);
                            filthDef = ParticleDefOf.Filth_BulletCasingsCharge;
                        }
                        else
                            return;
                    }
                    else if (__instance.GetProjectile().projectile.damageDef == DamageDefOf.Bullet && __instance.Caster.def.building.turretGunDef.defName.Contains("Shotgun", StringComparison.OrdinalIgnoreCase))
                    {
                        ThrowCasingTurret(__instance.Caster, __instance.Caster.Map, __instance.GetProjectile().projectile.GetDamageAmount(1f, null), ParticleDefOf.Fuu_BulletCasingShotgun);
                        filthDef = ParticleDefOf.Filth_BulletCasingsShotgun;
                    }
                    else if (__instance.GetProjectile().projectile.damageDef == DamageDefOf.Bullet)
                    {
                        ThrowCasingTurret(__instance.Caster, __instance.Caster.Map, __instance.GetProjectile().projectile.GetDamageAmount(1f, null), ParticleDefOf.Fuu_BulletCasingRifle);
                        filthDef = ParticleDefOf.Filth_BulletCasingsRifle;
                    }

                    if (Settings.bulletCasingFilthToggle && filthDef != null && Rand.Chance(0.15f))
                    {
                        IntVec3 pos = new IntVec3(__instance.Caster.Position.x + 1, 0, __instance.Caster.Position.z);
                        if(pos.InBounds(__instance.Caster.Map))
                            FilthMaker.TryMakeFilth(pos, __instance.Caster.Map, filthDef);
                    }
                }
            }
        }

        public static Mote ThrowCasing(Pawn caster, Map map, int weaponDamage, ThingDef moteDef, float exactRotation)
        {
            if (!caster.Position.ShouldSpawnMotesAt(map) || map.moteCounter.Saturated)
                return null;

            Vector3 pos = caster.Position.ToVector3Shifted();
            pos.x += Facing(exactRotation) * 0.4f;
            MoteThrowCasing moteThrowCasing = (MoteThrowCasing)ThingMaker.MakeThing(moteDef);
            moteThrowCasing.Scale = GenMath.LerpDoubleClamped(5f, 30f, 0.2f, 0.4f, weaponDamage);
            moteThrowCasing.exactPosition = pos;
            moteThrowCasing.rotationRate = Rand.Range(-180f, 180f);
            moteThrowCasing.speedMultiplier = Rand.Range(1f, 2f);
            moteThrowCasing.facing = Facing(exactRotation);
            moteThrowCasing.exactRotation = (caster.TargetCurrentlyAimingAt.CenterVector3 - caster.DrawPos).AngleFlat();
            GenSpawn.Spawn(moteThrowCasing, caster.Position, map);
            return moteThrowCasing;
        }

        public static Mote ThrowCasingTurret(Thing caster, Map map, int weaponDamage, ThingDef moteDef)
        {
            if (!caster.Position.ShouldSpawnMotesAt(map) || map.moteCounter.Saturated)
                return null;

            MoteThrowCasing moteThrowCasing = (MoteThrowCasing)ThingMaker.MakeThing(moteDef);
            moteThrowCasing.Scale = GenMath.LerpDoubleClamped(5f, 30f, 0.2f, 0.4f, weaponDamage);
            moteThrowCasing.exactPosition = caster.TrueCenter();
            moteThrowCasing.rotationRate = Rand.Range(-180f, 180f);
            moteThrowCasing.speedMultiplier = Rand.Range(2f, 3f);
            GenSpawn.Spawn(moteThrowCasing, caster.Position, map);
            return moteThrowCasing;
        }

        public static int Facing(float exactRotation)
        {
            if (exactRotation < 180f)
                return 1;
            else
                return -1;
        }

        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }
}