using System.Collections.Generic;
using HarmonyLib;
using Verse;
using DubsBadHygiene;

namespace OneWithBadHygiene
{
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

        // Necromancer transformation hediffs - exempt from bladder and thirst
        private static readonly List<string> NecromancerHediffs = new List<string>
        {
            "OneWithDeath",
            "FamiliarWithDeath"
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
            var harmony = new Harmony("azrazalea.owd.dbh.patch");
            harmony.PatchAll();

            // Inject exemptions into DBH settings
            InjectExemptions();

            Log.Message("[OneWithBadHygiene] Patched DBH exemptions for One With Death undead and necromancers.");
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
}
