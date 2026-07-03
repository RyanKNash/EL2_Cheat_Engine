using UnityEngine;

namespace EL2_cheat_engine
{
	public sealed class GuiRenderer
	{
		private Rect windowRect;
		private bool loggedOnGuiFirstRun;

		private GUIStyle titleStyle;
		private GUIStyle headerStyle;
		private GUIStyle buttonStyle;
		private GUIStyle targetButtonStyle;
		private GUIStyle toggleStyle;
		private GUIStyle sliderLabelStyle;

		private Texture2D bgTexture;
		private Texture2D btnTexture;
		private Texture2D btnHoverTexture;
		private Texture2D icoDust;
		private Texture2D icoInd;
		private Texture2D icoSci;
		private Texture2D icoInf;
		private Texture2D icoFame;
		private Texture2D icoStrat;
		private Texture2D icoLux;
		private Texture2D icoSpec;

		public void Initialize(Rect initialWindowRect)
		{
			windowRect = initialWindowRect;
			EnsureInitialized();
		}

		public void Draw()
		{
			if (!loggedOnGuiFirstRun)
			{
				loggedOnGuiFirstRun = true;
				ModLog.Info("OnGUI first ran");
			}

			EnsureInitialized();

			if (!ModState.MenuExpanded)
			{
				if (GUI.Button(GuiConfig.ReopenButtonRect, "\u25bc", buttonStyle))
				{
					ModState.MenuExpanded = true;
					ModLog.Info("Menu expanded from reopen button.");
				}

				return;
			}

			GUIStyle windowStyle = GUIStyle.none;
			if (GUI.skin != null && GUI.skin.window != null)
			{
				windowStyle = GUI.skin.window;
				if (bgTexture != null)
				{
					windowStyle.normal.background = bgTexture;
					windowStyle.onNormal.background = bgTexture;
				}
			}

			windowRect = GUILayout.Window(0, windowRect, WindowFunction, "", windowStyle);
		}

		private void WindowFunction(int windowID)
		{
			EnsureInitialized();
			DrawHeader();

			GUILayout.Space(GuiConfig.SectionSpacing);
			GUILayout.Label("Target Empires", headerStyle);
			GUILayout.BeginVertical("box");
			DrawTargetButtons();
			GUILayout.EndVertical();

			GUILayout.Space(GuiConfig.SectionSpacing);
			GUILayout.Label("Yield Multipliers", headerStyle);
			GUILayout.BeginVertical("box");
			DrawStyledSlider("Dust", icoDust, ref ModState.EnableMoney, ref ModState.MoneyMult, Config.MoneyMultiplierMax);
			DrawStyledSlider("Industry", icoInd, ref ModState.EnableIndustry, ref ModState.IndustryMult, Config.IndustryMultiplierMax);
			DrawStyledSlider("Science", icoSci, ref ModState.EnableScience, ref ModState.ScienceMult, Config.ScienceMultiplierMax);
			DrawStyledSlider("Influence", icoInf, ref ModState.EnableInfluence, ref ModState.InfluenceMult, Config.InfluenceMultiplierMax);
			DrawStyledSlider("Fame", icoFame, ref ModState.EnableFame, ref ModState.FameMult, Config.FameMultiplierMax);
			GUILayout.EndVertical();

			GUILayout.Space(GuiConfig.ResourceSectionSpacing);
			GUILayout.Label("Resource Injection", headerStyle);
			GUILayout.BeginVertical("box");
			GUILayout.BeginHorizontal();
			GUILayout.Label($"Amount: {ModState.ResourceAmount}", GUILayout.Width(GuiConfig.ResourceAmountLabelWidth));
			ModState.ResourceAmount = (int)GUILayout.HorizontalSlider(ModState.ResourceAmount, Config.ResourceAmountMin, Config.ResourceAmountMax);
			GUILayout.EndHorizontal();

			GUILayout.Space(GuiConfig.ResourceToggleSpacing);
			GUILayout.BeginHorizontal();
			DrawStyledToggle("Strategic", icoStrat, ref ModState.FillStrategic);
			DrawStyledToggle("Luxury", icoLux, ref ModState.FillLuxury);
			DrawStyledToggle("Add Gold", icoDust, ref ModState.AddGold);
			DrawStyledToggle("Add Influence", icoInf, ref ModState.AddInfluence);
			GUILayout.EndHorizontal();

			GUILayout.Space(GuiConfig.SpecialToggleSpacing);
			GUILayout.BeginHorizontal();
			DrawStyledToggle("Corpses", icoSpec, ref ModState.FillSpecial26);
			GUILayout.EndHorizontal();

			GUILayout.Space(GuiConfig.SectionSpacing);
			if (GUILayout.Button("ADD RESOURCES NOW", buttonStyle, GUILayout.Height(GuiConfig.AddResourcesButtonHeight)))
			{
				if (OwnershipResolver.AnyTargetSelected())
				{
					ResourceInjector.TryAddResources();
				}
				else
				{
					ModLog.Warn("ADD RESOURCES NOW blocked: no target empires selected.");
				}
			}

			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUI.DragWindow();
		}

		private void DrawHeader()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("EL2 Cheat Engine", titleStyle);
			if (GUILayout.Button("\u25b2", buttonStyle, GUILayout.Width(GuiConfig.HeaderButtonWidth), GUILayout.Height(GuiConfig.HeaderButtonHeight)))
			{
				ModState.MenuExpanded = false;
				ModLog.Info("Menu collapsed from header button.");
			}
			GUILayout.EndHorizontal();
		}

		private void DrawTargetButtons()
		{
			GUILayout.BeginHorizontal();
			for (int i = 0; i < ModState.TargetPlayers.Length; i++)
			{
				bool selected = ModState.TargetPlayers[i];
				GUIStyle style = selected ? targetButtonStyle : GUI.skin.button;
				if (GUILayout.Button(OwnershipResolver.GetPlayerLabel(i), style, GUILayout.Width(GuiConfig.TargetButtonWidth), GUILayout.Height(GuiConfig.TargetButtonHeight)))
				{
					ModState.TargetPlayers[i] = !selected;
					LogSelectedTargets();
				}
			}
			GUILayout.EndHorizontal();
		}

		private void LogSelectedTargets()
		{
			string selected = string.Empty;
			for (int i = 0; i < ModState.TargetPlayers.Length; i++)
			{
				if (!ModState.TargetPlayers[i])
				{
					continue;
				}

				if (selected.Length > 0)
				{
					selected += ", ";
				}

				selected += i.ToString();
			}

			if (selected.Length == 0)
			{
				selected = "none";
			}

			ModLog.Info("Selected target indexes: " + selected);
		}

		private void DrawStyledSlider(string label, Texture2D icon, ref bool toggle, ref float value, float max)
		{
			GUILayout.BeginHorizontal();
			if ((bool)icon)
			{
				GUI.DrawTexture(GUILayoutUtility.GetRect(GuiConfig.SliderIconSize, GuiConfig.SliderIconSize, GUILayout.Width(GuiConfig.SliderIconSize)), icon);
			}

			GUILayout.Space(GuiConfig.SliderIconSpacing);
			toggle = GUILayout.Toggle(toggle, label, toggleStyle, GUILayout.Width(GuiConfig.SliderToggleWidth));
			GUI.enabled = toggle;
			value = GUILayout.HorizontalSlider(value, Config.SliderMin, max);
			GUILayout.Label($"x{(int)value}", sliderLabelStyle, GUILayout.Width(GuiConfig.SliderValueWidth));
			GUI.enabled = true;
			GUILayout.EndHorizontal();
			GUILayout.Space(GuiConfig.SliderRowSpacing);
		}

		private void DrawStyledToggle(string label, Texture2D icon, ref bool toggle)
		{
			GUILayout.BeginHorizontal(GUILayout.Width(GuiConfig.ToggleRowWidth));
			if ((bool)icon)
			{
				GUI.DrawTexture(GUILayoutUtility.GetRect(GuiConfig.ToggleIconSize, GuiConfig.ToggleIconSize, GUILayout.Width(GuiConfig.ToggleIconSize)), icon);
			}

			toggle = GUILayout.Toggle(toggle, label, toggleStyle);
			GUILayout.EndHorizontal();
		}

		private void EnsureInitialized()
		{
			if (bgTexture == null) bgTexture = TextureFactory.MakeTex(GuiConfig.TextureSize, GuiConfig.TextureSize, GuiConfig.WindowBackgroundColor);
			if (btnTexture == null) btnTexture = TextureFactory.MakeTex(GuiConfig.TextureSize, GuiConfig.TextureSize, GuiConfig.ButtonColor);
			if (btnHoverTexture == null) btnHoverTexture = TextureFactory.MakeTex(GuiConfig.TextureSize, GuiConfig.TextureSize, GuiConfig.ButtonHoverColor);
			if (icoDust == null) icoDust = TextureFactory.MakeTex(GuiConfig.IconTextureSize, GuiConfig.IconTextureSize, GuiConfig.DustIconColor);
			if (icoInd == null) icoInd = TextureFactory.MakeTex(GuiConfig.IconTextureSize, GuiConfig.IconTextureSize, GuiConfig.IndustryIconColor);
			if (icoSci == null) icoSci = TextureFactory.MakeTex(GuiConfig.IconTextureSize, GuiConfig.IconTextureSize, GuiConfig.ScienceIconColor);
			if (icoInf == null) icoInf = TextureFactory.MakeTex(GuiConfig.IconTextureSize, GuiConfig.IconTextureSize, GuiConfig.InfluenceIconColor);
			if (icoFame == null) icoFame = TextureFactory.MakeTex(GuiConfig.IconTextureSize, GuiConfig.IconTextureSize, GuiConfig.FameIconColor);
			if (icoStrat == null) icoStrat = TextureFactory.MakeTex(GuiConfig.IconTextureSize, GuiConfig.IconTextureSize, GuiConfig.StrategicIconColor);
			if (icoLux == null) icoLux = TextureFactory.MakeTex(GuiConfig.IconTextureSize, GuiConfig.IconTextureSize, GuiConfig.LuxuryIconColor);
			if (icoSpec == null) icoSpec = TextureFactory.MakeTex(GuiConfig.IconTextureSize, GuiConfig.IconTextureSize, GuiConfig.SpecialIconColor);

			GUIStyle labelBase = GUI.skin != null && GUI.skin.label != null ? GUI.skin.label : GUIStyle.none;
			GUIStyle buttonBase = GUI.skin != null && GUI.skin.button != null ? GUI.skin.button : GUIStyle.none;
			GUIStyle toggleBase = GUI.skin != null && GUI.skin.toggle != null ? GUI.skin.toggle : GUIStyle.none;

			if (titleStyle == null)
			{
				titleStyle = new GUIStyle(labelBase)
				{
					fontSize = GuiConfig.TitleFontSize,
					fontStyle = FontStyle.Bold,
					alignment = TextAnchor.MiddleCenter,
					normal = { textColor = GuiConfig.TitleTextColor }
				};
			}

			if (headerStyle == null)
			{
				headerStyle = new GUIStyle(labelBase)
				{
					fontSize = GuiConfig.HeaderFontSize,
					fontStyle = FontStyle.Bold,
					normal = { textColor = GuiConfig.HeaderTextColor }
				};
			}

			if (buttonStyle == null)
			{
				buttonStyle = new GUIStyle(buttonBase)
				{
					fontSize = GuiConfig.ButtonFontSize,
					fontStyle = FontStyle.Bold,
					normal = { textColor = GuiConfig.ButtonTextColor, background = btnTexture }
				};
				buttonStyle.hover.background = btnHoverTexture;
				buttonStyle.active.background = btnHoverTexture;
			}

			if (targetButtonStyle == null)
			{
				targetButtonStyle = new GUIStyle(buttonStyle)
				{
					normal =
					{
						textColor = GuiConfig.ButtonTextColor,
						background = btnHoverTexture
					}
				};
			}

			if (toggleStyle == null)
			{
				toggleStyle = new GUIStyle(toggleBase)
				{
					fontSize = GuiConfig.ToggleFontSize,
					normal = { textColor = GuiConfig.ToggleTextColor }
				};
			}

			if (sliderLabelStyle == null)
			{
				sliderLabelStyle = new GUIStyle(labelBase)
				{
					fontSize = GuiConfig.SliderLabelFontSize,
					alignment = TextAnchor.MiddleRight,
					normal = { textColor = GuiConfig.SliderLabelTextColor }
				};
			}
		}
	}
}
