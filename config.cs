namespace EL2_cheat_engine
{
	public static class Config
	{
		public const float SliderMin = 1f;

		public const float IndustryMultiplierDefault = 5f;
		public const float IndustryMultiplierMax = 100f;

		public const float MoneyMultiplierDefault = 5f;
		public const float MoneyMultiplierMax = 100f;

		public const float InfluenceMultiplierDefault = 5f;
		public const float InfluenceMultiplierMax = 100f;

		public const float ScienceMultiplierDefault = 5f;
		public const float ScienceMultiplierMax = 100f;

		// Fame is reserved for future implementation and intentionally hidden from the GUI.
		// public const float FameMultiplierDefault = 5f;
		// public const float FameMultiplierMax = 100f;

		public const int ResourceAmountDefault = 100;
		public const float ResourceAmountMin = 1f;
		public const float ResourceAmountMax = 1000f;

		public static readonly bool DEBUG_REFLECTION = false;
		public static readonly bool DEBUG_MOVEMENT = false;
		public static readonly bool DEBUG_EMPIRE = false;
		public static readonly bool DEBUG_GODSPEED_MOVEMENT = false;
	}
}
