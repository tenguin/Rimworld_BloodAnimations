using Verse;

namespace BloodAnimations
{
    [StaticConstructorOnStartup]
    public static class ModSupport
    {
        public static bool AlphaGenesActive = false;
        public static bool VanillaExpandedFrameworkActive = false;
        public static bool TheProfanedActive = false;

        public static ThingDef AG_FilthMucus;
        public static ThingDef AG_TarBlood;
        public static ThingDef AG_Filth_Fuel;
        public static ThingDef AG_Filth_RubbleRock;

        public static ThingDef BotchJob_Filth_Ectoplasm;
        public static ThingDef BotchJob_Filth_Undead;

        static ModSupport()
        {
            AlphaGenesActive = ModLister.HasActiveModWithName("Alpha Genes");
            VanillaExpandedFrameworkActive = ModLister.HasActiveModWithName("Vanilla Expanded Framework");
            TheProfanedActive = ModLister.HasActiveModWithName("The Profaned");

            if (AlphaGenesActive)
            {
                AG_FilthMucus = ThingDef.Named("AG_FilthMucus");
                AG_TarBlood = ThingDef.Named("AG_TarBlood");
                AG_Filth_Fuel = ThingDef.Named("AG_Filth_Fuel");
                AG_Filth_RubbleRock = ThingDef.Named("AG_Filth_RubbleRock");
            }

            if (TheProfanedActive)
            {
                BotchJob_Filth_Ectoplasm = ThingDef.Named("BotchJob_Filth_Ectoplasm");
                BotchJob_Filth_Undead = ThingDef.Named("BotchJob_Filth_Undead");
            }
        }

        public static ThingDef GetVanillaGenesExpandedBloodDef(Pawn pawn)
        {
            foreach (Gene gene in pawn.genes.GenesListForReading)
            {
                if (gene.Active)
                {
                    VEF.Genes.GeneExtension modExtension = gene.def.GetModExtension<VEF.Genes.GeneExtension>();
                    if (modExtension?.customBloodThingDef != null)
                        return modExtension.customBloodThingDef;
                }
            }
            return null;
        }
    }
}
