using BepInEx;
using UnityEngine;

namespace EL2_cheat_engine
{
	[BepInPlugin("com.rknash.el2_cheat_engine", "EL2 Cheat Engine", "1.0.0")]
	public class Plugin : BaseUnityPlugin
	{
		private readonly GuiRenderer guiRenderer = new GuiRenderer();

		private void Awake()
		{
			ModLog.Initialize(Logger);
			ModLog.Info("EL2 Cheat Engine (Hybrid V7) Loaded");
			HarmonyPatches.Register();
		}

		private void Start()
		{
			ModLog.Info("Start() ran");
			ModState.MenuExpanded = true;
			guiRenderer.Initialize(GuiConfig.InitialWindowRect);
		}

		private void OnGUI()
		{
			guiRenderer.Draw();
		}
	}
}


