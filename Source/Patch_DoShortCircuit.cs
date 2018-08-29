using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Harmony;
using UnityEngine;
using Verse;
using RimWorld;

namespace RT_Fuse
{
	[HarmonyPatch(typeof(ShortCircuitUtility))]
	[HarmonyPatch("DoShortCircuit")]
	static class Patch_DoShortCircuit
	{
		private static MethodInfo tryStartFireNearMethodInfo = AccessTools.Method(typeof(ShortCircuitUtility), "TryStartFireNear");

		private static bool TryStartFireNear(Building culprit)
		{
			return (bool)tryStartFireNearMethodInfo.Invoke(null, new object[] { culprit });
		}

		static bool Prefix(Building culprit)
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
						totalEnergy -= fuseComp.MitigateSurge(totalEnergy);
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
						fuseComp.MitigateSurge(fuseComp.surgeMitigation);
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
					stringBuilder.Append("ShortCircuitStartedFire".Translate(new object[]
					{
						culpritString
					}));
				}
				else
				{
					stringBuilder.Append("ShortCircuit".Translate(new object[]
					{
						culpritString
					}));
				}
			}
			else
			{
				stringBuilder.Append("ShortCircuit".Translate(new object[]
				{
					culpritString
				}));
				if (totalEnergy > 0f)
				{
					explosionRadius = Mathf.Sqrt(totalEnergy) * 0.05f;
					explosionRadius = Mathf.Clamp(explosionRadius, 1.5f, 14.9f);
					GenExplosion.DoExplosion(culprit.Position, powerNet.Map, explosionRadius, DamageDefOf.Flame, null, -1, -1f, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
					if (explosionRadius > 3.5f)
					{
						GenExplosion.DoExplosion(culprit.Position, powerNet.Map, explosionRadius * 0.3f, DamageDefOf.Bomb, null, -1, -1f, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 0f, false);
					}

					if (totalEnergy == totalEnergyHistoric)
					{
						stringBuilder.AppendLine();
						stringBuilder.AppendLine();
						stringBuilder.Append("ShortCircuitDischargedEnergy".Translate(new object[]
						{
						totalEnergyHistoric.ToString("F0")
						}));
					}
					else
					{
						stringBuilder.AppendLine();
						stringBuilder.AppendLine();
						stringBuilder.Append("IncidentWorker_RTShortCircuit_PartialMitigation".Translate(new object[]
						{
						totalEnergyHistoric.ToString("F0"),
						(totalEnergyHistoric - totalEnergy).ToString("F0")
						}));
					}
				}
				else
				{
					culprit.TakeDamage(new DamageInfo(
						DamageDefOf.Bomb, Rand.Range(0, (int)Math.Floor(0.1f * culprit.MaxHitPoints)),
						0f, -1f, null, null));

					stringBuilder.AppendLine();
					stringBuilder.AppendLine();
					stringBuilder.Append("IncidentWorker_RTShortCircuit_FullMitigation".Translate(new object[]
					{
					totalEnergyHistoric.ToString("F0")
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
			Find.LetterStack.ReceiveLetter("LetterLabelShortCircuit".Translate(), stringBuilder.ToString(), LetterDefOf.NegativeEvent, new TargetInfo(culprit.Position, map, false), null);
			return false;
		}
	}
}
