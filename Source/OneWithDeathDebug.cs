using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;
using OneWithDeath;

namespace OneWithBadHygiene
{
    /// <summary>
    /// Debug actions for testing One With Death necromancy states.
    /// Uses "OneWithDeath" header to merge with the mod's existing debug actions.
    /// </summary>
    public static class OneWithDeathDebug
    {
        /// <summary>
        /// Removes all necromancy progression hediffs from a pawn (not the implant)
        /// </summary>
        private static void ClearNecromancyHediffs(Pawn pawn)
        {
            var hediffsToRemove = new List<HediffDef>
            {
                MyModDefs.UnfamiliarWithDeath,
                MyModDefs.FamiliarWithDeath,
                MyModDefs.OneWithDeath
            };

            foreach (var hediffDef in hediffsToRemove)
            {
                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);
                if (hediff != null)
                {
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        /// <summary>
        /// Ensures the pawn has the necromancer implant
        /// </summary>
        private static void EnsureImplant(Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(MyModDefs.NecromancerImplant))
                return;

            var brain = pawn.health.hediffSet.GetBrain();
            if (brain != null)
            {
                pawn.health.AddHediff(MyModDefs.NecromancerImplant, brain);
            }
        }

        #region Debug Actions

        [DebugAction("OneWithDeath", "Set Unfamiliar With Death", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetUnfamiliarWithDeath(Pawn pawn)
        {
            ClearNecromancyHediffs(pawn);
            EnsureImplant(pawn);
            pawn.health.AddHediff(MyModDefs.UnfamiliarWithDeath);
            Messages.Message($"{pawn.LabelShort} is now Unfamiliar With Death.", MessageTypeDefOf.PositiveEvent);
        }

        [DebugAction("OneWithDeath", "Set Familiar With Death", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SetFamiliarWithDeath(Pawn pawn)
        {
            ClearNecromancyHediffs(pawn);
            EnsureImplant(pawn);
            pawn.health.AddHediff(MyModDefs.FamiliarWithDeath);
            Messages.Message($"{pawn.LabelShort} is now Familiar With Death.", MessageTypeDefOf.PositiveEvent);
        }

        [DebugAction("OneWithDeath", "Make Lich (One With Death)", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void MakeLich(Pawn pawn)
        {
            ClearNecromancyHediffs(pawn);
            EnsureImplant(pawn);

            // Use OWD's actual utility function which handles hediff, mutant, genes, graphics, etc.
            OneWithDeathUtility.ApplyOneWithDeathEffects(pawn);

            // Re-add implant after transformation in case it was removed
            EnsureImplant(pawn);

            Messages.Message($"{pawn.LabelShort} is now One With Death (Lich).", MessageTypeDefOf.PositiveEvent);
        }

        [DebugAction("OneWithDeath", "Promote Necromancy Rank", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void PromoteNecromancyRank(Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(MyModDefs.OneWithDeath))
            {
                Messages.Message($"{pawn.LabelShort} is already at max rank (One With Death).", MessageTypeDefOf.RejectInput);
                return;
            }

            if (pawn.health.hediffSet.HasHediff(MyModDefs.FamiliarWithDeath))
            {
                MakeLich(pawn);
                return;
            }

            if (pawn.health.hediffSet.HasHediff(MyModDefs.UnfamiliarWithDeath))
            {
                SetFamiliarWithDeath(pawn);
                return;
            }

            // No necromancy state - start at Unfamiliar
            SetUnfamiliarWithDeath(pawn);
        }

        [DebugAction("OneWithDeath", "Demote Necromancy Rank", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void DemoteNecromancyRank(Pawn pawn)
        {
            if (pawn.health.hediffSet.HasHediff(MyModDefs.OneWithDeath))
            {
                // Remove Lich mutant status
                if (pawn.mutant != null && pawn.mutant.Def == MyModDefs.OWD_Lich)
                {
                    pawn.mutant = null;
                }
                SetFamiliarWithDeath(pawn);
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
                return;
            }

            if (pawn.health.hediffSet.HasHediff(MyModDefs.FamiliarWithDeath))
            {
                SetUnfamiliarWithDeath(pawn);
                return;
            }

            if (pawn.health.hediffSet.HasHediff(MyModDefs.UnfamiliarWithDeath))
            {
                ClearNecromancyHediffs(pawn);
                Messages.Message($"{pawn.LabelShort} is no longer on the necromancy path.", MessageTypeDefOf.NeutralEvent);
                return;
            }

            Messages.Message($"{pawn.LabelShort} has no necromancy rank to demote.", MessageTypeDefOf.RejectInput);
        }

        [DebugAction("OneWithDeath", "Clear Necromancy State", false, false, false, false, false, 0, false, actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ClearNecromancyState(Pawn pawn)
        {
            ClearNecromancyHediffs(pawn);

            // Remove Lich mutant status if present
            if (pawn.mutant != null && pawn.mutant.Def == MyModDefs.OWD_Lich)
            {
                pawn.mutant = null;
                pawn.Drawer?.renderer?.SetAllGraphicsDirty();
            }

            Messages.Message($"Cleared necromancy state from {pawn.LabelShort}.", MessageTypeDefOf.NeutralEvent);
        }

        #endregion
    }
}
