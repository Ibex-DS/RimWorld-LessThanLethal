/*
	Name: newCheckState.cs - LessThanLethal
	Author: Jamie Wood <Jamie.Wood@TheIbex.net>
*/
using RimWorld;
using System;
using System.Reflection;
using System.Linq;
using Verse.AI;
using HugsLib;
using RimWorld.Planet;
using Harmony;

namespace LTL
{
	public class LTL : ModBase {
		public override string ModIdentifier {
			get {
				return "LessThanLethal";
			}
		}
		public static int likelihood_lethalHuman;
		public static int likelihood_lethalAnimal;
		public static int likelihood_nonlethal;
		public override void SettingsChanged() {
			this.DefsLoaded();
		}
		public override void DefsLoaded() {
			likelihood_lethalHuman = Settings.GetHandle<int>("lethalHuman", "Normal Human Lethality", "How lethal should regular weapons be against humans? Overrides the default 67% chance of death upon becoming incapacitated for non-collonists. (0-100)", 67);
			likelihood_lethalAnimal = Settings.GetHandle<int>("lethalAnimal", "Normal Animal Lethality", "How lethal should regular weapons be against animals? Overrides the default 47% chance of death upon becoming incapacitated for non-collonial animals.(0-100)", 47);
			likelihood_nonlethal = Settings.GetHandle<int>("nonLethal", "Less Than Lethal Lethality", "How lethal should less than lethal weapons be? Determines how often people will be killed upon being incapacitated by less than lethal means.", 0);
		}
	}
		
	[HarmonyPatch(typeof(Verse.Pawn_HealthTracker), "CheckForStateChange")]
	public static class stateChange {
		internal static BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
		[HarmonyPrefix]
		internal static bool _StateCheck (this Verse.Pawn_HealthTracker __instance, Verse.DamageInfo? dinfo, Verse.Hediff hediff) {
			MethodInfo _shouldBeDead = typeof(Verse.Pawn_HealthTracker).GetMethod("ShouldBeDead", BF);
			FieldInfo _pawn = typeof(Verse.Pawn_HealthTracker).GetField("pawn", BF);
			MethodInfo _shouldBeDowned = typeof(Verse.Pawn_HealthTracker).GetMethod("ShouldBeDowned", BF);
			MethodInfo _makeDowned = typeof(Verse.Pawn_HealthTracker).GetMethod("MakeDowned", BF);
			MethodInfo _makeUndowned = typeof(Verse.Pawn_HealthTracker).GetMethod("MakeUndowned", BF);
			
			bool shouldBeDead = (bool)_shouldBeDead.Invoke(__instance, new object[]{});
			bool shouldBeDowned = (bool)_shouldBeDowned.Invoke(__instance, new object[]{});
			bool isNotLethalDam = ((dinfo != null) ? (dinfo.ToString().Contains('=') ? dinfo.ToString().Split('=')[1].Split(',')[0].StartsWith("LTL_") : false) : false);
			bool isNotLethalDis = ((hediff != null) ? hediff.ToString().StartsWith("(LTL_") : false);
			
			Verse.Pawn pawn = (Verse.Pawn)_pawn.GetValue(__instance);
			if (!__instance.Dead) {
				if (shouldBeDead) {
					if (!pawn.Destroyed) {
						bool flag = PawnUtility.ShouldSendNotificationAbout(pawn);
						Caravan caravan = pawn.GetCaravan();
						pawn.Kill(dinfo);
						if (flag) {
							__instance.NotifyPlayerOfKilled(dinfo, hediff, caravan);
						}
					}
					return false;
				}
				if (!__instance.Downed) {
					if (shouldBeDowned) {
						float num = (isNotLethalDam || isNotLethalDis) ? ((float)LTL.likelihood_nonlethal/(float)100) : ((!pawn.RaceProps.Animal) ? ((float)LTL.likelihood_lethalHuman/(float)100) : ((float)LTL.likelihood_lethalAnimal/(float)10));
						if (!__instance.forceIncap && (pawn.Faction == null || !pawn.Faction.IsPlayer) && !pawn.IsPrisonerOfColony && pawn.RaceProps.IsFlesh && Verse.Rand.Value < num) {
							pawn.Kill(dinfo);
							return false;
						}
						__instance.forceIncap = false;
						_makeDowned.Invoke(__instance, new object[] {dinfo, hediff});
						return false;
					} else if (!__instance.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) {
						if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null && pawn.jobs != null && pawn.CurJob != null) {
							pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
						}
						if (pawn.equipment != null && pawn.equipment.Primary != null) {
							if (pawn.InContainerEnclosed) {
								pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.holdingOwner);
							} else if (pawn.SpawnedOrAnyParentSpawned) {
								Verse.ThingWithComps thingWithComps;
								pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out thingWithComps, pawn.Position, true);
							} else {
								pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
							}
						}
					}
				} else if (!shouldBeDowned) {
					_makeUndowned.Invoke(__instance, new object[] {});
					return false;
				}
			}
			return false;
		}
	}
}