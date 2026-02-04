using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using DubsBadHygiene;
using XmlExtensions;

namespace OneWithBadHygiene
{
    /// <summary>
    /// Settings for One With Bad Hygiene mod
    /// </summary>
    public static class OWBHSettings
    {
        private const string ModId = "azrazalea.owd.dbh.patch";

        public static bool EnableProgressionEffects { get; private set; }

        public static void LoadSettings()
        {
            string setting = SettingsManager.GetSetting(ModId, "enableProgressionEffects");
            EnableProgressionEffects = !string.IsNullOrEmpty(setting) && bool.TryParse(setting, out bool result) && result;
        }
    }

    [StaticConstructorOnStartup]
    public static class OneWithBadHygienePatcher
    {
        // All undead types - exempt from bladder, thirst, and hygiene
        private static readonly List<string> UndeadHediffs = new List<string>
        {
            "OWD_Undead",
            "UndeadChampion",
            "UndeadOverseer",
            "OWD_UndeadCreature",
            "OWD_Hediff_LesserUndead"
        };

        // Necromancer transformation hediffs - always exempt from bladder and thirst
        // FamiliarWithDeath exemption is conditional: added at runtime only when progression is OFF
        private static readonly List<string> NecromancerHediffs = new List<string>
        {
            "OneWithDeath"
        };

        // Hygiene exempt hediffs (undead + OneWithDeath only, not FamiliarWithDeath)
        private static readonly List<string> HygieneExemptHediffs = new List<string>
        {
            "OWD_Undead",
            "UndeadChampion",
            "UndeadOverseer",
            "OWD_UndeadCreature",
            "OWD_Hediff_LesserUndead",
            "OneWithDeath"
            // FamiliarWithDeath still has hygiene (just decays faster via XML patch)
        };

        static OneWithBadHygienePatcher()
        {
            OWBHSettings.LoadSettings();

            var harmony = new Harmony("azrazalea.owd.dbh.patch");
            harmony.PatchAll();

            // Inject exemptions into DBH settings
            InjectExemptions();

            Log.Message($"[OneWithBadHygiene] Patched DBH exemptions for One With Death. Progression effects: {OWBHSettings.EnableProgressionEffects}");
        }

        private static void InjectExemptions()
        {
            var settings = DubsBadHygieneMod.Settings;
            if (settings == null)
            {
                Log.Warning("[OneWithBadHygiene] Could not access DBH settings.");
                return;
            }

            // Initialize lists if null
            if (settings.BladderHediff == null)
                settings.BladderHediff = new List<string>();
            if (settings.HygieneHediff == null)
                settings.HygieneHediff = new List<string>();

            // Add all undead to bladder/thirst exemptions (DBH uses BladderHediff for both)
            foreach (var hediff in UndeadHediffs)
            {
                if (!settings.BladderHediff.Contains(hediff))
                {
                    settings.BladderHediff.Add(hediff);
                }
            }

            // Add necromancer hediffs to bladder/thirst exemptions
            foreach (var hediff in NecromancerHediffs)
            {
                if (!settings.BladderHediff.Contains(hediff))
                {
                    settings.BladderHediff.Add(hediff);
                }
            }

            // When progression effects are OFF, FamiliarWithDeath gets full bladder/thirst exemption
            // When ON, it gets gradual reduction via XML patches instead
            if (!OWBHSettings.EnableProgressionEffects)
            {
                if (!settings.BladderHediff.Contains("FamiliarWithDeath"))
                {
                    settings.BladderHediff.Add("FamiliarWithDeath");
                }
            }

            // Add hygiene exemptions
            foreach (var hediff in HygieneExemptHediffs)
            {
                if (!settings.HygieneHediff.Contains(hediff))
                {
                    settings.HygieneHediff.Add(hediff);
                }
            }
        }
    }

    /// <summary>
    /// Patch to increase aging rate for necromancers as they progress toward undeath.
    /// OneWithDeath disables aging via MutantDef, but the earlier stages accelerate decay.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_AgeTracker), "BiologicalTicksPerTick", MethodType.Getter)]
    public static class Pawn_AgeTracker_BiologicalTicksPerTick_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn ___pawn, ref float __result)
        {
            // Only apply if progression effects are enabled
            if (!OWBHSettings.EnableProgressionEffects)
                return;

            if (___pawn?.health?.hediffSet == null)
                return;

            var hediffSet = ___pawn.health.hediffSet;

            // Check in order of severity (most severe first)
            // OneWithDeath is handled by MutantDef disableAging, so we skip it

            if (hediffSet.hediffs.Any(h => h.def.defName == "FamiliarWithDeath"))
            {
                __result *= 1.4f; // 40% faster aging
            }
            else if (hediffSet.hediffs.Any(h => h.def.defName == "UnfamiliarWithDeath"))
            {
                __result *= 1.2f; // 20% faster aging
            }
            else if (hediffSet.hediffs.Any(h => h.def.defName == "NecromancerImplant"))
            {
                __result *= 1.1f; // 10% faster aging
            }
        }
    }
}
