namespace EL2_cheat_engine
{
	public static class ModState
	{
		public static bool MenuExpanded = true;

		public static bool SliderSectionExpanded = true;

		public static bool ResourceInjectionExpanded = false;

		public static float IndustryMult = Config.IndustryMultiplierDefault;

		public static float MoneyMult = Config.MoneyMultiplierDefault;

		public static float InfluenceMult = Config.InfluenceMultiplierDefault;

		public static float ScienceMult = Config.ScienceMultiplierDefault;

		// Fame is reserved for future implementation and intentionally hidden from the GUI.
		// public static float FameMult = Config.FameMultiplierDefault;

		public static bool EnableIndustry = false;

		public static bool EnableMoney = false;

		public static bool EnableInfluence = false;

		public static bool EnableScience = false;

		// Fame is not currently in use.
		// public static bool EnableFame = false;

		public static int ResourceAmount = Config.ResourceAmountDefault;

		public static bool FillStrategic = false;

		public static bool FillLuxury = false;

		public static bool FillSpecial26 = false; // Corpses

		// Special 27 maps to Fallen Spirits. It is not currently used in gameplay,
		// so keep this disabled until the game exposes a real use for it.
		// public static bool FillSpecial27 = false;

		public static bool AddGold = false;

		public static bool AddInfluence = false;

		public static int HumanEmpireIndexOverride = -1;

		public static bool[] TargetPlayers = new bool[7];
	}
}

