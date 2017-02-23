/*
 * Created by SharpDevelop.
 * User: Jamie Wood
 * Date: 21/02/2017
 * Time: 21:54
 * 
 * To change self template use Tools | Options | Coding | Edit Standard Headers.
 */
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;
using Verse;

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
	}
	
	public static class stateChange {
		[DetourMethod(typeof(Verse.Pawn_HealthTracker), "CheckForStateChange")]
		internal static void _StateCheck (this Verse.Pawn_HealthTracker self, Verse.DamageInfo? dinfo, Verse.Hediff hediff) {
			MethodInfo _shouldBeDead = typeof(Verse.Pawn_HealthTracker).GetMethod("ShouldBeDead", BindingFlags.NonPublic | BindingFlags.Instance);
			FieldInfo _pawn = typeof(Verse.Pawn_HealthTracker).GetField("pawn", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo _shouldBeDowned = typeof(Verse.Pawn_HealthTracker).GetMethod("ShouldBeDowned", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo _makeDowned = typeof(Verse.Pawn_HealthTracker).GetMethod("MakeDowned", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo _makeUndowned = typeof(Verse.Pawn_HealthTracker).GetMethod("MakeUndowned", BindingFlags.NonPublic | BindingFlags.Instance);
			bool sbd = (bool)_shouldBeDead.Invoke(self, new object[]{});
			bool sbd2 = (bool)_shouldBeDowned.Invoke(self, new object[]{});
			bool isNotLethalDam = (dinfo.ToString().Contains('=') ? dinfo.ToString().Split('=')[1].Split(',')[0].StartsWith("LTL_") : false);
			bool isNotLethalDis = hediff.ToString().StartsWith("(LTL_");
			Verse.Pawn pawn = (Verse.Pawn)_pawn.GetValue(self);
			if (!self.Dead)
			{
				if (sbd)
				{
					if (!pawn.Destroyed)
					{
						self.Kill(dinfo, hediff);
					}
					return;
				}
				if (!self.Downed)
				{
					if (sbd2)
					{
						float num = (isNotLethalDam || isNotLethalDis) ? 0.00f : ((!pawn.RaceProps.Animal) ? 0.67f : 0.47f);
						if (!self.forceIncap && (pawn.Faction == null || !pawn.Faction.IsPlayer) && !pawn.IsPrisonerOfColony && pawn.RaceProps.IsFlesh && Verse.Rand.Value < num)
						{
							self.Kill(dinfo, null);
							return;
						}
						self.forceIncap = false;
						_makeDowned.Invoke(self, new object[] {dinfo, hediff});
						return;
					}
					else if (!self.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
					{
						if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null && pawn.jobs != null && pawn.CurJob != null)
						{
							pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
						}
						if (pawn.equipment != null && pawn.equipment.Primary != null)
						{
							if (pawn.InContainerEnclosed)
							{
								Verse.ThingWithComps thingWithComps;
								pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.holdingContainer, out thingWithComps);
							}
							else if (pawn.Spawned)
							{
								Verse.ThingWithComps thingWithComps;
								pawn.equipment.TryDropEquipment(pawn.equipment.Primary, out thingWithComps, pawn.Position, true);
							}
							else
							{
								pawn.equipment.DestroyEquipment(pawn.equipment.Primary);
							}
						}
					}
				}
				else if (!sbd2)
				{
					_makeUndowned.Invoke(self, new object[] {});
					return;
				}
			}
		}
	}
}