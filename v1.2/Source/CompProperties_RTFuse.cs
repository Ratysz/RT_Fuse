using Verse;

namespace RT_Fuse
{
	public class CompProperties_RTFuse : CompProperties
	{
		public float surgeMitigation = 600.0f;
		public bool breakdownOnTrip = true;

		public CompProperties_RTFuse()
		{
			compClass = typeof(CompRTFuse);
		}
	}
}