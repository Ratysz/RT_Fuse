using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Harmony;
using Verse;
using UnityEngine;

namespace RT_Fuse
{
	class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			var harmony = HarmonyInstance.Create("io.github.ratysz.madskills");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}
}
