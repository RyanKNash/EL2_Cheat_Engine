namespace EL2_cheat_engine
{
	public static class ModState
	{
		public static bool ShowMenu = false;

		public static float IndustryMult = 100f;

		public static float MoneyMult = 1000f;

		public static float InfluenceMult = 1000f;

		public static float ScienceMult = 1000f;

		public static float FameMult = 10f;

		public static bool EnableIndustry = false;

		public static bool EnableMoney = false;

		public static bool EnableInfluence = false;

		public static bool EnableScience = false;

		public static bool EnableFame = false;

		public static int ResourceAmount = 1000;

		public static bool FillStrategic = false;

		public static bool FillLuxury = false;

		public static bool FillSpecials = false;

		public static int HumanEmpireIndexOverride = -1;

		public static bool AllowResourceInjection = false;

		public static bool[] TargetPlayers = new bool[7];
	}
}
