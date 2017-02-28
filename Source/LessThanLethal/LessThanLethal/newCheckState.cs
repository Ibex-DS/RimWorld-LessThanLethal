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
using HugsLib.Source.Detour;

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
	
		
	
	public static class stateChange {
		internal static BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
		
		[DetourMethod(typeof(Verse.Pawn_HealthTracker), "CheckForStateChange")]
		internal static void _StateCheck (this Verse.Pawn_HealthTracker self, Verse.DamageInfo? dinfo, Verse.Hediff hediff) {
			
			MethodInfo _shouldBeDead = typeof(Verse.Pawn_HealthTracker).GetMethod("ShouldBeDead", BF);
			FieldInfo _pawn = typeof(Verse.Pawn_HealthTracker).GetField("pawn", BF);
			MethodInfo _shouldBeDowned = typeof(Verse.Pawn_HealthTracker).GetMethod("ShouldBeDowned", BF);
			MethodInfo _makeDowned = typeof(Verse.Pawn_HealthTracker).GetMethod("MakeDowned", BF);
			MethodInfo _makeUndowned = typeof(Verse.Pawn_HealthTracker).GetMethod("MakeUndowned", BF);
			
			bool shouldBeDead = (bool)_shouldBeDead.Invoke(self, new object[]{});
			bool shouldBeDowned = (bool)_shouldBeDowned.Invoke(self, new object[]{});
			bool isNotLethalDam = ((dinfo != null) ? (dinfo.ToString().Contains('=') ? dinfo.ToString().Split('=')[1].Split(',')[0].StartsWith("LTL_") : false) : false);
			bool isNotLethalDis = ((hediff != null) ? hediff.ToString().StartsWith("(LTL_") : false);
			
			Verse.Pawn pawn = (Verse.Pawn)_pawn.GetValue(self);
			if (!self.Dead) {
				if (shouldBeDead) {
					if (!pawn.Destroyed) {
						self.Kill(dinfo, hediff);
					}
					return;
				}
				if (!self.Downed) {
					if (shouldBeDowned) {
						float num = (isNotLethalDam || isNotLethalDis) ? ((float)LTL.likelihood_nonlethal/(float)100) : ((!pawn.RaceProps.Animal) ? ((float)LTL.likelihood_lethalHuman/(float)100) : ((float)LTL.likelihood_lethalAnimal/(float)10));
						if (!self.forceIncap && (pawn.Faction == null || !pawn.Faction.IsPlayer) && !pawn.IsPrisonerOfColony && pawn.RaceProps.IsFlesh && Verse.Rand.Value < num) {
							self.Kill(dinfo, null);
							return;
						}
						self.forceIncap = false;
						_makeDowned.Invoke(self, new object[] {dinfo, hediff});
						return;
					} else if (!self.capacities.CapableOf(PawnCapacityDefOf.Manipulation)) {
						if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null && pawn.jobs != null && pawn.CurJob != null) {
							pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
						}
						if (pawn.equipment != null && pawn.equipment.Primary != null) {
							if (pawn.InContainerEnclosed) {
								Verse.ThingWithComps thingWithComps;
								pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.holdingContainer, out thingWithComps);
							} else if (pawn.Spawned) {
								Verse.ThingWithComps thingWithComps;
								pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out thingWithComps, pawn.Position, true);
							} else {
								pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
							}
						}
					}
				} else if (!shouldBeDowned) {
					_makeUndowned.Invoke(self, new object[] {});
					return;
				}
			}
		}
	}
}