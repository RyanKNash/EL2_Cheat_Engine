using UnityEngine;

namespace EL2_cheat_engine
{
	public static class GuiConfig
	{
		public static readonly Rect InitialWindowRect = new Rect(50f, 80f, 540f, 600f);
		public static readonly Rect ReopenButtonRect = new Rect(10f, 10f, 34f, 30f);

		public const int TextureSize = 2;
		public const int IconTextureSize = 12;

		public const float SectionSpacing = 15f;
		public const float ResourceSectionSpacing = 20f;
		public const float ResourceToggleSpacing = 10f;
		public const float SpecialToggleSpacing = 6f;
		public const float SliderIconSpacing = 5f;
		public const float SliderRowSpacing = 4f;

		public const float ResourceAmountLabelWidth = 100f;
		public const float HeaderButtonWidth = 36f;
		public const float HeaderButtonHeight = 28f;
		public const float TargetButtonWidth = 58f;
		public const float TargetButtonHeight = 28f;
		public const float SliderIconSize = 16f;
		public const float SliderToggleWidth = 80f;
		public const float SliderValueWidth = 45f;
		public const float ToggleRowWidth = 125f;
		public const float ToggleIconSize = 14f;
		public const float AddResourcesButtonHeight = 40f;

		public const int TitleFontSize = 18;
		public const int HeaderFontSize = 14;
		public const int ButtonFontSize = 14;
		public const int ToggleFontSize = 13;
		public const int SliderLabelFontSize = 12;

		public static readonly Color WindowBackgroundColor = new Color(0.08f, 0.1f, 0.15f, 0.95f);
		public static readonly Color ButtonColor = new Color(0f, 0.45f, 0.65f, 1f);
		public static readonly Color ButtonHoverColor = new Color(0f, 0.55f, 0.75f, 1f);
		public static readonly Color TitleTextColor = Color.white;
		public static readonly Color HeaderTextColor = new Color(0.6f, 0.8f, 1f);
		public static readonly Color ButtonTextColor = Color.white;
		public static readonly Color ToggleTextColor = new Color(0.9f, 0.9f, 0.9f);
		public static readonly Color SliderLabelTextColor = Color.yellow;

		public static readonly Color DustIconColor = new Color(1f, 0.84f, 0f);
		public static readonly Color IndustryIconColor = new Color(1f, 0.5f, 0f);
		public static readonly Color ScienceIconColor = new Color(0f, 0.8f, 1f);
		public static readonly Color InfluenceIconColor = new Color(0.7f, 0f, 1f);
		// Fame is reserved for future implementation and intentionally hidden from the GUI.
		// public static readonly Color FameIconColor = Color.white;
		public static readonly Color StrategicIconColor = Color.gray;
		public static readonly Color LuxuryIconColor = Color.green;
		public static readonly Color SpecialIconColor = new Color(0.8f, 0.2f, 0.2f);
	}
}
