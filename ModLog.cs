using System.Diagnostics;
using BepInEx.Logging;
using UnityEngine;

namespace EL2_cheat_engine
{
	public static class ModLog
	{
		private static ManualLogSource logger;

		public static void Initialize(ManualLogSource source)
		{
			logger = source;
		}

		public static void Info(string message)
		{
			if (logger != null)
			{
				logger.LogInfo(message);
			}
			else
			{
				UnityEngine.Debug.Log("[EL2CE] " + message);
			}
		}

		public static void Warn(string message)
		{
			if (logger != null)
			{
				logger.LogWarning(message);
			}
			else
			{
				UnityEngine.Debug.LogWarning("[EL2CE] " + message);
			}
		}

		public static void Error(string message)
		{
			if (logger != null)
			{
				logger.LogError(message);
			}
			else
			{
				UnityEngine.Debug.LogError("[EL2CE] " + message);
			}
		}

		[Conditional("EL2_CHEAT_ENGINE_DEBUG")]
		public static void DebugLog(string message)
		{
			Info(message);
		}

		[Conditional("EL2_CHEAT_ENGINE_DEBUG")]
		public static void Debug(string message)
		{
			Info(message);
		}
	}
}
