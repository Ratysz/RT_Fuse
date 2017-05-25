using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_Fuse
{
	public class IncidentWorker_RTShortCircuit : IncidentWorker
	{
		private IEnumerable<Building> UsableBatteries(Map map)
		{
			return
				from Building battery in map.listerBuildings.allBuildingsColonist
				where (battery.TryGetComp<CompPowerBattery>() != null
						&& battery.TryGetComp<CompPowerBattery>().StoredEnergy >= 50f)
				select battery as Building;
		}
		
		protected override bool CanFireNowSub(IIncidentTarget target)
		{
			return UsableBatteries((Map)target).Any();
		}

		public override bool TryExecute(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			List<Building> batteries = UsableBatteries(map).ToList();
			if (batteries.Count() == 0) return false;

			PowerNet powerNet = batteries.RandomElement().PowerComp.PowerNet;
			List<CompPower> victims = (
				from transmitter in powerNet.transmitters
				where transmitter.parent.def == ThingDefOf.PowerConduit
				select transmitter
				).ToList();
			if (victims.Count == 0) return false;

			List<Building> fuses = (
				from transmitter in powerNet.transmitters
				where transmitter.parent.GetComp<CompRTFuse>() != null
				select transmitter.parent as Building
				).ToList();

			float energyTotal = 0.0f;
			foreach (CompPowerBattery battery in powerNet.batteryComps)
			{
				energyTotal += battery.StoredEnergy;
				battery.DrawPower(battery.StoredEnergy);
			}

			float energyTotalHistoric = energyTotal;
			foreach (Building fuse in fuses)
			{
				energyTotal -= fuse.GetComp<CompRTFuse>().MitigateSurge(energyTotal);
				if (energyTotal <= 0) break;
			}

			StringBuilder stringBuilder = new StringBuilder();
			Thing victim = victims.RandomElement().parent;

			if (energyTotal > 0)
			{
				float explosionRadius = Mathf.Sqrt(energyTotal * 0.05f);
				if (explosionRadius > 14.9f) explosionRadius = 14.9f;
				
				GenExplosion.DoExplosion(
					victim.Position, map, explosionRadius, DamageDefOf.Flame,
					null, null, null, null, null, 0f, 1, false, null, 0f, 1);

				if (explosionRadius > 3.5f)
					GenExplosion.DoExplosion(
						victim.Position, map, explosionRadius * 0.3f, DamageDefOf.Bomb,
						null, null, null, null, null, 0f, 1, false, null, 0f, 1);

				if (!victim.Destroyed)
					victim.TakeDamage(new DamageInfo(
						DamageDefOf.Bomb, 200, -1f, null, null));

				if (energyTotal == energyTotalHistoric)
				{
					stringBuilder.Append("ShortCircuit".Translate(new object[]
					{
						"AnElectricalConduit".Translate(),
						energyTotalHistoric.ToString("F0")
					}));
				}
				else
				{
					stringBuilder.Append("IncidentWorker_RTShortCircuit_PartialMitigation".Translate(new object[]
					{
						"AnElectricalConduit".Translate(),
						energyTotalHistoric.ToString("F0"),
						(energyTotalHistoric - energyTotal).ToString("F0")
					}));
				}

				if (explosionRadius > 5f)
				{
					stringBuilder.AppendLine();
					stringBuilder.AppendLine();
					stringBuilder.Append("ShortCircuitWasLarge".Translate());
				}

				if (explosionRadius > 8f)
				{
					stringBuilder.AppendLine();
					stringBuilder.AppendLine();
					stringBuilder.Append("ShortCircuitWasHuge".Translate());
				}
			}
			else
			{
				victim.TakeDamage(new DamageInfo(
					DamageDefOf.Bomb, Rand.Range(0, (int)Math.Floor(0.1f * victim.MaxHitPoints)),
					-1f, null, null));
				
				stringBuilder.Append("IncidentWorker_RTShortCircuit_FullMitigation".Translate(new object[] 
				{
					"AnElectricalConduit".Translate(),
					energyTotalHistoric.ToString("F0")
				}));
			}

			Find.LetterStack.ReceiveLetter(
				"LetterLabelShortCircuit".Translate(), stringBuilder.ToString(),
				LetterDefOf.BadNonUrgent, new TargetInfo(victim.Position, map, false), null);

			return true;
		}
	}
}
