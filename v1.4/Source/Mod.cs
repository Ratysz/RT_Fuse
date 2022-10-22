using HarmonyLib;
using System.Reflection;
using Verse;

namespace RT_Fuse
{
	internal class Mod : Verse.Mod
	{
		public Mod(ModContentPack content) : base(content)
		{
			var harmony = new Harmony("io.github.ratysz.madskills");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
		}
	}
}