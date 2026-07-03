using System;
using System.Reflection;
using Amplitude;
using HarmonyLib;

namespace EL2_cheat_engine
{
	public static class HarmonyPatches
	{
		private static bool loggedIndustryStack;
		private static bool loggedMoneyStack;
		private static bool loggedInfluenceStack;
		private static bool loggedScienceStack;
		private static bool loggedFameStack;

		public static void Register()
		{
			Harmony harmony = new Harmony("com.yourname.el2_cheat_engine");
			PatchPostfix(harmony, "Amplitude.Mercury.Simulation.DepartmentOfIndustry", "ComputeProductionIncome", "Patch_Industry");
			PatchPrefix(harmony, "Amplitude.Mercury.Simulation.DepartmentOfTheTreasury", "GainMoney", "Patch_Money");
			PatchPrefix(harmony, "Amplitude.Mercury.Simulation.DepartmentOfCulture", "GainInfluence", "Patch_Influence");
			PatchPrefix(harmony, "Amplitude.Mercury.Simulation.DepartmentOfScience", "GainResearch", "Patch_Science");
			PatchPrefix(harmony, "Amplitude.Mercury.Simulation.DepartmentOfDevelopment", "GainFame", "Patch_Fame");
		}

		private static void PatchPostfix(Harmony harmony, string className, string methodName, string patchMethodName)
		{
			try
			{
				ModLog.Info($"Searching target method for postfix patch: {className}.{methodName}");
				MethodInfo target = AccessTools.Method(className + ":" + methodName);
				if (target == null)
				{
					ModLog.Error($"AccessTools.Method returned null for target method: {className}.{methodName}");
					return;
				}

				MethodInfo patch = typeof(HarmonyPatches).GetMethod(patchMethodName, BindingFlags.Static | BindingFlags.NonPublic);
				if (patch == null)
				{
					ModLog.Error($"Could not find postfix patch method: {patchMethodName}");
					return;
				}

				harmony.Patch(target, postfix: new HarmonyMethod(patch));
				ModLog.Info($"Patched postfix successfully: {className}.{methodName} -> {patchMethodName}");
			}
			catch (Exception ex)
			{
				ModLog.Error($"Failed to patch postfix {className}.{methodName} with {patchMethodName}: {ex}");
			}
		}

		private static void PatchPrefix(Harmony harmony, string className, string methodName, string patchMethodName)
		{
			try
			{
				ModLog.Info($"Searching target method for prefix patch: {className}.{methodName}");
				MethodInfo target = AccessTools.Method(className + ":" + methodName);
				if (target == null)
				{
					ModLog.Error($"AccessTools.Method returned null for target method: {className}.{methodName}");
					return;
				}

				MethodInfo patch = typeof(HarmonyPatches).GetMethod(patchMethodName, BindingFlags.Static | BindingFlags.NonPublic);
				if (patch == null)
				{
					ModLog.Error($"Could not find prefix patch method: {patchMethodName}");
					return;
				}

				harmony.Patch(target, prefix: new HarmonyMethod(patch));
				ModLog.Info($"Patched prefix successfully: {className}.{methodName} -> {patchMethodName}");
			}
			catch (Exception ex)
			{
				ModLog.Error($"Failed to patch prefix {className}.{methodName} with {patchMethodName}: {ex}");
			}
		}

		private static void Patch_Industry(object __0, ref FixedPoint __result)
		{
			LogPatchStackOnce(ref loggedIndustryStack, "Patch_Industry");
			OwnershipResolver.DumpObjectDeepOnce("Industry settlement deep dump", __0);
			OwnershipResolver.DumpObjectDeepOnce("Industry settlement Empire wrapper deep dump", OwnershipResolver.TryGetMemberValue(__0, "Empire"));
			OwnershipResolver.DumpMajorEmpireOnce("Industry Settlement MajorEmpire FINAL", __0);

			FixedPoint incoming = __result;
			int empireIndex = OwnershipResolver.ResolveEmpireIndex(__0);
			bool isTargeted = OwnershipResolver.IsTargetedEmpire(__0);
			ModLog.Debug($"Patch_Industry fired on settlement {OwnershipResolver.SafeTypeName(__0)}. incoming __result={incoming}, enabled={ModState.EnableIndustry}, multiplier={ModState.IndustryMult}, empireIndex={empireIndex}, isTargeted={isTargeted}");
			if (!isTargeted)
			{
				ModLog.Debug($"Patch_Industry skipped untargeted settlement empire index {empireIndex}. final value unchanged at {__result}");
				return;
			}

			if (ModState.EnableIndustry && __result > 0)
			{
				__result *= (FixedPoint)ModState.IndustryMult;
				ModLog.Debug($"Patch_Industry changed __result from {incoming} to {__result}");
			}
			else
			{
				ModLog.Debug($"Patch_Industry final value unchanged at {__result}");
			}
		}

		private static void Patch_Money(object __instance, ref FixedPoint gain)
		{
			LogPatchStackOnce(ref loggedMoneyStack, "Patch_Money");
			OwnershipResolver.DumpObjectDeepOnce("Treasury instance deep dump", __instance);
			OwnershipResolver.DumpObjectDeepOnce("Treasury majorEmpire deep dump", OwnershipResolver.TryGetMemberValue(__instance, "majorEmpire"));
			OwnershipResolver.DumpObjectDeepOnce("Treasury Empire deep dump", OwnershipResolver.TryGetMemberValue(__instance, "Empire"));
			OwnershipResolver.DumpMajorEmpireOnce("Money MajorEmpire FINAL", __instance);
			ApplyPrefixMultiplier("Patch_Money", __instance, ref gain, ModState.EnableMoney, ModState.MoneyMult);
		}

		private static void Patch_Influence(object __instance, ref FixedPoint gain)
		{
			LogPatchStackOnce(ref loggedInfluenceStack, "Patch_Influence");
			OwnershipResolver.DumpMajorEmpireOnce("Influence MajorEmpire FINAL", __instance);
			ApplyPrefixMultiplier("Patch_Influence", __instance, ref gain, ModState.EnableInfluence, ModState.InfluenceMult);
		}

		private static void Patch_Science(object __instance, ref FixedPoint gain)
		{
			LogPatchStackOnce(ref loggedScienceStack, "Patch_Science");
			OwnershipResolver.DumpObjectDeepOnce("Science instance deep dump", __instance);
			OwnershipResolver.DumpObjectDeepOnce("Science majorEmpire deep dump", OwnershipResolver.TryGetMemberValue(__instance, "majorEmpire"));
			OwnershipResolver.DumpObjectDeepOnce("Science Empire deep dump", OwnershipResolver.TryGetMemberValue(__instance, "Empire"));
			OwnershipResolver.DumpMajorEmpireOnce("Science MajorEmpire FINAL", __instance);
			ApplyPrefixMultiplier("Patch_Science", __instance, ref gain, ModState.EnableScience, ModState.ScienceMult);
		}

		private static void Patch_Fame(object __instance, ref FixedPoint gain)
		{
			LogPatchStackOnce(ref loggedFameStack, "Patch_Fame");
			OwnershipResolver.DumpMajorEmpireOnce("Fame MajorEmpire FINAL", __instance);
			ApplyPrefixMultiplier("Patch_Fame", __instance, ref gain, ModState.EnableFame, ModState.FameMult);
		}

		private static void ApplyPrefixMultiplier(string label, object instance, ref FixedPoint gain, bool enabled, float multiplier)
		{
			FixedPoint incoming = gain;
			int empireIndex = OwnershipResolver.ResolveEmpireIndex(instance);
			bool isTargeted = OwnershipResolver.IsTargetedEmpire(instance);
			ModLog.Debug($"{label} fired on {OwnershipResolver.SafeTypeName(instance)}. incoming gain={incoming}, enabled={enabled}, multiplier={multiplier}, empireIndex={empireIndex}, isTargeted={isTargeted}");
			if (!isTargeted)
			{
				ModLog.Debug($"{label} skipped untargeted empire index {empireIndex}. final value unchanged at {gain}");
				return;
			}

			if (enabled && gain > 0)
			{
				gain *= (FixedPoint)multiplier;
				ModLog.Debug($"{label} changed gain from {incoming} to {gain}");
			}
			else
			{
				ModLog.Debug($"{label} final value unchanged at {gain}");
			}
		}

		private static void LogPatchStackOnce(ref bool logged, string patchName)
		{
			if (logged)
			{
				return;
			}

			logged = true;
			ModLog.Debug($"{patchName} first-fire stack trace:\n{new System.Diagnostics.StackTrace(true)}");
		}
	}
}
