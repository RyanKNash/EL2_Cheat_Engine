using BepInEx;
using UnityEngine;

namespace EL2_cheat_engine
{
	[BepInPlugin("com.yourname.el2_cheat_engine", "EL2 Cheat Engine", "1.3.0")]
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
			ModState.ShowMenu = true;
			guiRenderer.Initialize(new Rect(50f, 80f, 450f, 520f));
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Insert) ||
				Input.GetKeyDown(KeyCode.F10) ||
				Input.GetKeyDown(KeyCode.Home) ||
				Input.GetKeyDown(KeyCode.F8))
			{
				ModState.ShowMenu = !ModState.ShowMenu;
				ModLog.Info($"Menu toggle key pressed. Menu toggled: {ModState.ShowMenu}");
			}
		}

		private void OnGUI()
		{
			guiRenderer.Draw();
		}
	}
}
