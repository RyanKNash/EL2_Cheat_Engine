using UnityEngine;

namespace EL2_cheat_engine
{
	public sealed class GuiRenderer
	{
		private Rect windowRect;
		private bool loggedOnGuiFirstRun;
		private bool expanded = true;

		private GUIStyle titleStyle;
		private GUIStyle headerStyle;
		private GUIStyle buttonStyle;
		private GUIStyle targetButtonStyle;
		private GUIStyle toggleStyle;
		private GUIStyle sliderLabelStyle;
		private GUIStyle debugLabelStyle;

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
			GUI.Label(new Rect(10f, 10f, 560f, 24f), "EL2 Cheat Engine loaded - Press Home/F8/Insert/F10", debugLabelStyle);

			if (!ModState.ShowMenu && GUI.Button(new Rect(10f, 36f, 110f, 26f), "Open EL2CE"))
			{
				ModState.ShowMenu = true;
				ModLog.Info($"Open EL2CE button pressed. Menu toggled: {ModState.ShowMenu}");
			}

			if (!ModState.ShowMenu)
			{
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
			if (!expanded)
			{
				GUI.DragWindow();
				return;
			}

			GUILayout.Space(15f);
			GUILayout.Label("Target Empires", headerStyle);
			GUILayout.BeginVertical("box");
			DrawTargetButtons();
			GUILayout.EndVertical();

			GUILayout.Space(15f);
			GUILayout.Label("Yield Multipliers", headerStyle);
			GUILayout.BeginVertical("box");
			DrawStyledSlider("Dust", icoDust, ref ModState.EnableMoney, ref ModState.MoneyMult, 5000f);
			DrawStyledSlider("Industry", icoInd, ref ModState.EnableIndustry, ref ModState.IndustryMult, 1000f);
			DrawStyledSlider("Science", icoSci, ref ModState.EnableScience, ref ModState.ScienceMult, 5000f);
			DrawStyledSlider("Influence", icoInf, ref ModState.EnableInfluence, ref ModState.InfluenceMult, 5000f);
			DrawStyledSlider("Fame", icoFame, ref ModState.EnableFame, ref ModState.FameMult, 100f);
			GUILayout.EndVertical();

			GUILayout.Space(20f);
			GUILayout.Label("Resource Injection", headerStyle);
			GUILayout.BeginVertical("box");
			GUILayout.BeginHorizontal();
			GUILayout.Label($"Amount: {ModState.ResourceAmount}", GUILayout.Width(100f));
			ModState.ResourceAmount = (int)GUILayout.HorizontalSlider(ModState.ResourceAmount, 100f, 10000f);
			GUILayout.EndHorizontal();

			GUILayout.Space(8f);
			ModState.AllowResourceInjection = GUILayout.Toggle(ModState.AllowResourceInjection, "Allow Resource Injection", toggleStyle);

			GUILayout.Space(10f);
			GUILayout.BeginHorizontal();
			DrawStyledToggle("Strategic", icoStrat, ref ModState.FillStrategic);
			DrawStyledToggle("Luxury", icoLux, ref ModState.FillLuxury);
			DrawStyledToggle("Specials", icoSpec, ref ModState.FillSpecials);
			GUILayout.EndHorizontal();

			GUILayout.Space(15f);
			if (GUILayout.Button("ADD RESOURCES NOW", buttonStyle, GUILayout.Height(40f)))
			{
				if (ModState.AllowResourceInjection && OwnershipResolver.AnyTargetSelected())
				{
					ResourceInjector.TryAddResources();
				}
				else
				{
					ModLog.Warn($"ADD RESOURCES NOW blocked. AllowResourceInjection={ModState.AllowResourceInjection}, AnyTargetSelected={OwnershipResolver.AnyTargetSelected()}");
				}
			}

			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			GUILayout.Label("Press Home / F8 / INSERT / F10 to hide", sliderLabelStyle);
			GUI.DragWindow();
		}

		private void DrawHeader()
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("EL2 Cheat Engine", titleStyle);
			string arrow = expanded ? "\u25bc" : "\u25b2";
			if (GUILayout.Button(arrow, buttonStyle, GUILayout.Width(36f), GUILayout.Height(28f)))
			{
				expanded = !expanded;
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
				if (GUILayout.Button(OwnershipResolver.GetPlayerLabel(i), style, GUILayout.Width(58f), GUILayout.Height(28f)))
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
				GUI.DrawTexture(GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(16f)), icon);
			}

			GUILayout.Space(5f);
			toggle = GUILayout.Toggle(toggle, label, toggleStyle, GUILayout.Width(80f));
			GUI.enabled = toggle;
			value = GUILayout.HorizontalSlider(value, 1f, max);
			GUILayout.Label($"x{(int)value}", sliderLabelStyle, GUILayout.Width(45f));
			GUI.enabled = true;
			GUILayout.EndHorizontal();
			GUILayout.Space(4f);
		}

		private void DrawStyledToggle(string label, Texture2D icon, ref bool toggle)
		{
			GUILayout.BeginHorizontal(GUILayout.Width(110f));
			if ((bool)icon)
			{
				GUI.DrawTexture(GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f)), icon);
			}

			toggle = GUILayout.Toggle(toggle, label, toggleStyle);
			GUILayout.EndHorizontal();
		}

		private void EnsureInitialized()
		{
			if (bgTexture == null) bgTexture = TextureFactory.MakeTex(2, 2, new Color(0.08f, 0.1f, 0.15f, 0.95f));
			if (btnTexture == null) btnTexture = TextureFactory.MakeTex(2, 2, new Color(0f, 0.45f, 0.65f, 1f));
			if (btnHoverTexture == null) btnHoverTexture = TextureFactory.MakeTex(2, 2, new Color(0f, 0.55f, 0.75f, 1f));
			if (icoDust == null) icoDust = TextureFactory.MakeTex(12, 12, new Color(1f, 0.84f, 0f));
			if (icoInd == null) icoInd = TextureFactory.MakeTex(12, 12, new Color(1f, 0.5f, 0f));
			if (icoSci == null) icoSci = TextureFactory.MakeTex(12, 12, new Color(0f, 0.8f, 1f));
			if (icoInf == null) icoInf = TextureFactory.MakeTex(12, 12, new Color(0.7f, 0f, 1f));
			if (icoFame == null) icoFame = TextureFactory.MakeTex(12, 12, Color.white);
			if (icoStrat == null) icoStrat = TextureFactory.MakeTex(12, 12, Color.gray);
			if (icoLux == null) icoLux = TextureFactory.MakeTex(12, 12, Color.green);
			if (icoSpec == null) icoSpec = TextureFactory.MakeTex(12, 12, new Color(0.8f, 0.2f, 0.2f));

			GUIStyle labelBase = GUI.skin != null && GUI.skin.label != null ? GUI.skin.label : GUIStyle.none;
			GUIStyle buttonBase = GUI.skin != null && GUI.skin.button != null ? GUI.skin.button : GUIStyle.none;
			GUIStyle toggleBase = GUI.skin != null && GUI.skin.toggle != null ? GUI.skin.toggle : GUIStyle.none;

			if (titleStyle == null)
			{
				titleStyle = new GUIStyle(labelBase)
				{
					fontSize = 18,
					fontStyle = FontStyle.Bold,
					alignment = TextAnchor.MiddleCenter,
					normal = { textColor = Color.white }
				};
			}

			if (headerStyle == null)
			{
				headerStyle = new GUIStyle(labelBase)
				{
					fontSize = 14,
					fontStyle = FontStyle.Bold,
					normal = { textColor = new Color(0.6f, 0.8f, 1f) }
				};
			}

			if (buttonStyle == null)
			{
				buttonStyle = new GUIStyle(buttonBase)
				{
					fontSize = 14,
					fontStyle = FontStyle.Bold,
					normal = { textColor = Color.white, background = btnTexture }
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
						textColor = Color.white,
						background = btnHoverTexture
					}
				};
			}

			if (toggleStyle == null)
			{
				toggleStyle = new GUIStyle(toggleBase)
				{
					fontSize = 13,
					normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
				};
			}

			if (sliderLabelStyle == null)
			{
				sliderLabelStyle = new GUIStyle(labelBase)
				{
					fontSize = 12,
					alignment = TextAnchor.MiddleRight,
					normal = { textColor = Color.yellow }
				};
			}

			if (debugLabelStyle == null)
			{
				debugLabelStyle = new GUIStyle(labelBase)
				{
					fontSize = 14,
					fontStyle = FontStyle.Bold,
					normal = { textColor = Color.yellow }
				};
			}
		}
	}
}
