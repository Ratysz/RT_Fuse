﻿using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace RT_Fuse
{
	[HarmonyPatch(typeof(ShortCircuitUtility))]
	[HarmonyPatch("DoShortCircuit")]
	internal static class Patch_DoShortCircuit
	{
		private static MethodInfo tryStartFireNearMethodInfo = AccessTools.Method(typeof(ShortCircuitUtility), "TryStartFireNear");

		private static bool TryStartFireNear(Building culprit)
		{
			return (bool)tryStartFireNearMethodInfo.Invoke(null, new object[] { culprit });
		}

		private static bool Prefix(Building culprit)
		{
			PowerNet powerNet = culprit.PowerComp.PowerNet;
			Map map = culprit.Map;
			float totalEnergy = 0f;
			float totalEnergyHistoric = 0f;
			float explosionRadius = 0f;
			bool shouldStartFire = false;
			bool startedFire = false;
			string culpritString;

			if (powerNet.batteryComps.Any((CompPowerBattery x) => x.StoredEnergy > 20f))
			{
				foreach (CompPowerBattery batteryComp in powerNet.batteryComps)
				{
					totalEnergy += batteryComp.StoredEnergy;
					batteryComp.DrawPower(batteryComp.StoredEnergy);
				}
				totalEnergyHistoric = totalEnergy;
				foreach (CompPower transmitter in powerNet.transmitters)
				{
					CompRTFuse fuseComp = transmitter.parent.GetComp<CompRTFuse>();
					if (fuseComp != null)
					{
						totalEnergy -= fuseComp.MitigateSurge();
						if (totalEnergy <= 0) break;
					}
				}
			}
			else
			{
				shouldStartFire = true;
				bool mitigated = false;
				foreach (CompPower transmitter in powerNet.transmitters)
				{
					CompRTFuse fuseComp = transmitter.parent.GetComp<CompRTFuse>();
					if (fuseComp != null)
					{
						fuseComp.MitigateSurge();
						mitigated = true;
						break;
					}
				}
				startedFire = !mitigated && TryStartFireNear(culprit);
			}

			if (culprit.def == ThingDefOf.PowerConduit)
			{
				culpritString = "AnElectricalConduit".Translate();
			}
			else
			{
				culpritString = Find.ActiveLanguageWorker.WithIndefiniteArticle(culprit.Label);
			}

			StringBuilder stringBuilder = new StringBuilder();
			if (shouldStartFire)
			{
				if (startedFire)
				{
					stringBuilder.Append("ShortCircuitStartedFire".Translate(culpritString));
				}
				else
				{
					stringBuilder.Append("ShortCircuit".Translate(culpritString));
				}
			}
			else
			{
				stringBuilder.Append("ShortCircuit".Translate(culpritString));
				if (totalEnergy > 0f)
				{
					explosionRadius = Mathf.Sqrt(totalEnergy) * 0.05f;
					explosionRadius = Mathf.Clamp(explosionRadius, 1.5f, 14.9f);
					GenExplosion.DoExplosion(culprit.Position, powerNet.Map, explosionRadius, DamageDefOf.Flame, null);
					if (explosionRadius > 3.5f)
					{
						GenExplosion.DoExplosion(culprit.Position, powerNet.Map, explosionRadius * 0.3f, DamageDefOf.Bomb, null);
					}

					if (totalEnergy == totalEnergyHistoric)
					{
						stringBuilder.AppendLine();
						stringBuilder.AppendLine();
						stringBuilder.Append("ShortCircuitDischargedEnergy".Translate(totalEnergyHistoric.ToString("F0")));
					}
					else
					{
						stringBuilder.AppendLine();
						stringBuilder.AppendLine();
						stringBuilder.Append("IncidentWorker_RTShortCircuit_PartialMitigation".Translate(
							totalEnergyHistoric.ToString("F0"),
							(totalEnergyHistoric - totalEnergy).ToString("F0")));
					}
				}
				else
				{
					culprit.TakeDamage(new DamageInfo(
						DamageDefOf.Bomb, Rand.Range(0, (int)Math.Floor(0.1f * culprit.MaxHitPoints)),
						0f, -1f, null, null));

					stringBuilder.AppendLine();
					stringBuilder.AppendLine();
					stringBuilder.Append("IncidentWorker_RTShortCircuit_FullMitigation".Translate(totalEnergyHistoric.ToString("F0")));
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
			Find.LetterStack.ReceiveLetter(
				"LetterLabelShortCircuit".Translate(), stringBuilder.ToString(),
				LetterDefOf.NegativeEvent, new TargetInfo(culprit.Position, map, false), null);
			return false;
		}
	}
}