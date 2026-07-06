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
				if (!EmpireHelpers.AnyTargetSelected())
				{
					ModLog.Warn("Resource injection blocked: no target empires selected.");
					return;
				}

				ModLog.Info("Resource injection selected target indexes: " + EmpireHelpers.GetSelectedTargetIndexesText());

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

					if (!EmpireHelpers.IsTargetEmpire(empireIndex))
					{
						ModLog.Debug($"Resource injection skipped untargeted empire index {empireIndex}.");
						continue;
					}

					appliedToAnyEmpire = true;
					ModLog.Info($"Resource injection selected target empire index {empireIndex}");
					ProcessEmpireResources(enumType, empire, empireIndex);
					AddCurrentStock(empire, empireIndex, ModState.AddGold, "money", new string[1] { "MoneyStock" });
					AddCurrentStock(empire, empireIndex, ModState.AddInfluence, "influence", new string[1] { "InfluenceStock" });
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

		private static void ProcessEmpireResources(Type enumType, object empire, int empireIndex)
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
				for (int i = 0; i <= 32; i++)
				{
					if (ShouldFill(i))
					{
						TryGiveResource(enumType, giveMethod, departmentOfResources, i, empireIndex);
					}
				}

				ModLog.Info($"Resource injection completed for empire index {empireIndex}.");
				return;
			}

			for (int i = 0; i <= 32; i++)
			{
				if (ShouldFill(i))
				{
					TrySetResourceStock(traverse, i, empireIndex);
				}
			}

			ModLog.Info($"Resource injection completed for empire index {empireIndex} using stock fields.");
		}

		private static void TryGiveResource(Type enumType, MethodInfo giveMethod, object departmentOfResources, int resourceIndex, int empireIndex)
		{
			try
			{
				object resourceType = Enum.ToObject(enumType, resourceIndex);
				giveMethod.Invoke(departmentOfResources, new object[2] { resourceType, ModState.ResourceAmount });
			}
			catch (Exception ex)
			{
				ModLog.Warn($"Resource injection failed for empire index {empireIndex}, resource index {resourceIndex}: {ex.Message}");
			}
		}

		private static void TrySetResourceStock(Traverse traverse, int resourceIndex, int empireIndex)
		{
			string stockName = $"Resource{resourceIndex + 1:00}Stock";
			object stock = traverse.Field(stockName).GetValue();
			if (stock == null)
			{
				ModLog.Warn($"Resource injection could not find {stockName} for empire index {empireIndex}.");
				return;
			}

			if (!TryIncreaseValueMember(stock, "Value", ModState.ResourceAmount))
			{
				ModLog.Warn($"Resource injection could not update {stockName}.Value for empire index {empireIndex}.");
			}
		}

		private static void AddCurrentStock(object empire, int empireIndex, bool enabled, string label, string[] memberNames)
		{
			if (!enabled)
			{
				return;
			}

			bool foundStock = false;
			foreach (string memberName in memberNames)
			{
				object stock = ReflectionHelpers.TryGetMemberValue(empire, memberName);
				if (stock == null)
				{
					continue;
				}

				foundStock = true;
				if (TryAddCurrentStockRaw(stock, empireIndex, label, memberName))
				{
					return;
				}
			}

			if (!foundStock)
			{
				ModLog.Error($"Resource injection could not find {label} stock for target empire index {empireIndex}.");
			}
		}

		private static bool TryAddCurrentStockRaw(object stock, int empireIndex, string label, string memberName)
		{
			if (!SimulationPropertyHelpers.ReadRaw(stock, out int beforeRaw))
			{
				ModLog.Error($"Resource injection could not read {memberName} raw value for target empire index {empireIndex}.");
				return false;
			}

			int attemptedRaw = beforeRaw + (ModState.ResourceAmount * 1000);
			SimulationPropertyHelpers.WriteRaw(stock, attemptedRaw);
			int afterRaw = SimulationPropertyHelpers.ReadRaw(stock, out int rereadRaw) ? rereadRaw : beforeRaw;

			if (afterRaw == beforeRaw)
			{
				ModLog.Warn($"Resource injection {memberName} write did nothing for target empire index {empireIndex}: before={FormatRaw(beforeRaw)} attempted={FormatRaw(attemptedRaw)} after={FormatRaw(afterRaw)}.");
				return false;
			}

			ModLog.Info($"Resource injection added {ModState.ResourceAmount} {label} to target empire index {empireIndex} via {memberName}: before={FormatRaw(beforeRaw)} after={FormatRaw(afterRaw)}.");
			return true;
		}

		private static string FormatRaw(int raw)
		{
			if (raw % 1000 == 0)
			{
				return (raw / 1000).ToString();
			}

			return (raw / 1000m).ToString("0.###");
		}

		private static bool TryIncreaseStockMember(object owner, string memberName, int amount)
		{
			if (owner == null)
			{
				return false;
			}

			Type type = owner.GetType();
			FieldInfo field = AccessTools.Field(type, memberName);
			if (field != null)
			{
				object value = field.GetValue(owner);
				if (TryIncreaseEditableValue(value, amount))
				{
					return true;
				}

				if (!field.IsInitOnly && TryAddAmount(value, field.FieldType, amount, out object updatedValue))
				{
					field.SetValue(owner, updatedValue);
					return true;
				}
			}

			PropertyInfo property = AccessTools.Property(type, memberName);
			if (property != null)
			{
				object value = property.GetValue(owner, null);
				if (TryIncreaseEditableValue(value, amount))
				{
					return true;
				}

				if (property.CanWrite && TryAddAmount(value, property.PropertyType, amount, out object updatedValue))
				{
					property.SetValue(owner, updatedValue, null);
					return true;
				}
			}

			return false;
		}

		private static bool TryIncreaseEditableValue(object stock, int amount)
		{
			return stock != null && TryIncreaseValueMember(stock, "Value", amount);
		}

		private static bool TryIncreaseValueMember(object owner, string memberName, int amount)
		{
			if (owner == null)
			{
				return false;
			}

			Type type = owner.GetType();
			PropertyInfo property = AccessTools.Property(type, memberName);
			if (property != null && property.CanWrite)
			{
				object currentValue = property.GetValue(owner, null);
				if (TryAddAmount(currentValue, property.PropertyType, amount, out object updatedValue))
				{
					property.SetValue(owner, updatedValue, null);
					return true;
				}
			}

			FieldInfo field = AccessTools.Field(type, memberName);
			if (field != null && !field.IsInitOnly)
			{
				object currentValue = field.GetValue(owner);
				if (TryAddAmount(currentValue, field.FieldType, amount, out object updatedValue))
				{
					field.SetValue(owner, updatedValue);
					return true;
				}
			}

			return false;
		}

		private static bool TryAddAmount(object currentValue, Type valueType, int amount, out object updatedValue)
		{
			updatedValue = null;
			Type targetType = Nullable.GetUnderlyingType(valueType) ?? valueType;

			try
			{
				if (targetType == typeof(FixedPoint))
				{
					FixedPoint current = currentValue is FixedPoint fixedPoint ? fixedPoint : (FixedPoint)0;
					updatedValue = current + (FixedPoint)amount;
					return true;
				}

				if (targetType == typeof(int))
				{
					updatedValue = (currentValue is int current ? current : 0) + amount;
					return true;
				}

				if (targetType == typeof(long))
				{
					updatedValue = (currentValue is long current ? current : 0L) + amount;
					return true;
				}

				if (targetType == typeof(float))
				{
					updatedValue = (currentValue is float current ? current : 0f) + amount;
					return true;
				}

				if (targetType == typeof(double))
				{
					updatedValue = (currentValue is double current ? current : 0d) + amount;
					return true;
				}

				if (targetType == typeof(decimal))
				{
					updatedValue = (currentValue is decimal current ? current : 0m) + amount;
					return true;
				}
			}
			catch (Exception ex)
			{
				ModLog.Warn($"Could not add amount to {valueType.FullName}: {ex.Message}");
			}

			return false;
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

		private static bool ShouldFill(int index)
		{
			if (index <= 10)
			{
				return ModState.FillStrategic;
			}

			if (index >= 26)
			{
				return ShouldFillSpecial(index);
			}

			return ModState.FillLuxury;
		}

		private static bool ShouldFillSpecial(int index)
		{
			switch (index)
			{
				case 26: // Corpses
					return ModState.FillSpecial26;
				// case 27: // Fallen Spirits, not currently used in gameplay.
				// 	return ModState.FillSpecial27;
				default:
					return false;
			}
		}
	}
}

