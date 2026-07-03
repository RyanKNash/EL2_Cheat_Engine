using System;
using System.Collections;
using System.Reflection;
using Amplitude;
using HarmonyLib;

namespace EL2_cheat_engine
{
	public static class ResourceInjector
	{
		public static void TryAddResources()
		{
			try
			{
				if (!ModState.AllowResourceInjection)
				{
					ModLog.Warn("Resource injection blocked: AllowResourceInjection is false.");
					return;
				}

				if (!OwnershipResolver.AnyTargetSelected())
				{
					ModLog.Warn("Resource injection blocked: no target empires selected.");
					return;
				}

				ModLog.Info("Resource injection selected target indexes: " + GetSelectedTargetIndexesText());

				Type enumType = AccessTools.TypeByName("Amplitude.Mercury.Data.Simulation.ResourceType");
				Type sandboxType = AccessTools.TypeByName("Amplitude.Mercury.Sandbox.Sandbox");
				if (enumType == null)
				{
					ModLog.Error("Resource injection failed: ResourceType enum was not found.");
					return;
				}

				if (sandboxType == null)
				{
					ModLog.Error("Resource injection failed: Sandbox type was not found.");
					return;
				}

				object majorEmpires = ReadStaticMember(sandboxType, "MajorEmpires");
				if (majorEmpires == null || !(majorEmpires is IEnumerable enumerable))
				{
					ModLog.Error("Resource injection failed: Sandbox.MajorEmpires was not enumerable.");
					return;
				}

				bool appliedToAnyEmpire = false;
				foreach (object empire in enumerable)
				{
					if (empire == null)
					{
						continue;
					}

					int empireIndex = OwnershipResolver.ResolveEmpireIndex(empire);
					if (empireIndex < 0 || empireIndex >= ModState.TargetPlayers.Length)
					{
						ModLog.Debug($"Resource injection skipped empire with unresolved/out-of-range index {empireIndex}: {OwnershipResolver.SafeTypeName(empire)}");
						continue;
					}

					if (!ModState.TargetPlayers[empireIndex])
					{
						ModLog.Debug($"Resource injection skipped untargeted empire index {empireIndex}.");
						continue;
					}

					appliedToAnyEmpire = true;
					ModLog.Info($"Resource injection selected target empire index {empireIndex}");
					ProcessEmpire(enumType, empire, empireIndex);
				}

				if (!appliedToAnyEmpire)
				{
					ModLog.Warn("Resource injection found no matching selected empires.");
				}
			}
			catch (Exception ex)
			{
				ModLog.Error("Error adding resources: " + ex);
			}
		}

		private static void ProcessEmpire(Type enumType, object empire, int empireIndex)
		{
			Traverse traverse = Traverse.Create(empire);
			object departmentOfResources = traverse.Field("DepartmentOfResources").GetValue();
			if (departmentOfResources == null)
			{
				ModLog.Error($"Resource injection failed for empire index {empireIndex}: DepartmentOfResources was null.");
				return;
			}

			MethodInfo giveMethod = AccessTools.Method(departmentOfResources.GetType(), "GiveGodAccessToResource");
			if (giveMethod != null)
			{
				for (int i = 0; i < 32; i++)
				{
					if (ShouldFill(i))
					{
						object resourceType = Enum.ToObject(enumType, i);
						giveMethod.Invoke(departmentOfResources, new object[2] { resourceType, ModState.ResourceAmount });
					}
				}

				ModLog.Info($"Resource injection completed for empire index {empireIndex}.");
				return;
			}

			for (int j = 1; j <= 32; j++)
			{
				if (ShouldFill(j - 1))
				{
					object stock = traverse.Field($"Resource{j:00}Stock").GetValue();
					if (stock != null)
					{
						Traverse.Create(stock).Property("Value").SetValue((FixedPoint)ModState.ResourceAmount);
					}
				}
			}

			ModLog.Info($"Resource injection completed for empire index {empireIndex} using stock fields.");
		}

		private static object ReadStaticMember(Type type, string name)
		{
			FieldInfo field = AccessTools.Field(type, name);
			if (field != null)
			{
				return field.GetValue(null);
			}

			PropertyInfo property = AccessTools.Property(type, name);
			return property != null ? property.GetValue(null, null) : null;
		}

		private static string GetSelectedTargetIndexesText()
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

			return selected.Length > 0 ? selected : "none";
		}

		private static bool ShouldFill(int index)
		{
			if (index <= 10)
			{
				return ModState.FillStrategic;
			}

			if (index >= 26)
			{
				return ModState.FillSpecials;
			}

			return ModState.FillLuxury;
		}
	}
}
