using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_Fuse
{
	public class CompRTFuse : ThingComp
	{
		private CompProperties_RTFuse properties
		{
			get
			{
				return (CompProperties_RTFuse)this.props;
			}
		}
		public float surgeMitigation
		{
			get
			{
				return properties.surgeMitigation;
			}
		}
		public bool breakdownOnTrip
		{
			get
			{
				return properties.breakdownOnTrip;
			}
		}

		private CompBreakdownable compBreakdownable;
		private CompFlickable compFlickable;

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			compBreakdownable = parent.TryGetComp<CompBreakdownable>();
			compFlickable = parent.TryGetComp<CompFlickable>();
		}

		public override string CompInspectStringExtra()
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (compBreakdownable == null || !compBreakdownable.BrokenDown)
			{
				stringBuilder.Append("CompRTFuse_SurgeMitigation".Translate(new object[]
					{
						surgeMitigation.ToString("F0")
					}));
				if (breakdownOnTrip && compBreakdownable != null)
				{
					stringBuilder.AppendLine();
					stringBuilder.Append("CompRTFuse_WillBreakdown".Translate());
				}
				else if (compFlickable != null)
				{
					stringBuilder.AppendLine();
					stringBuilder.Append("CompRTFuse_WillFlick".Translate());
				}
			}
			return stringBuilder.ToString();
		}

		public float MitigateSurge(float amount)
		{
			if (compBreakdownable == null || !compBreakdownable.BrokenDown)
			{
				if (compBreakdownable != null && breakdownOnTrip)
				{
					compBreakdownable.DoBreakdown();
					return surgeMitigation;
				}
				else if (compFlickable != null)
				{
					compFlickable.ResetToOn();
					compFlickable.DoFlick();
					FlickUtility.UpdateFlickDesignation(parent);
					return surgeMitigation;
				}
			}
			return 0.0f;
		}
	}
}
