using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace EL2_cheat_engine
{
	public static class OwnershipResolver
	{
		private static bool loggedOwnershipReflectionError;
		private static readonly HashSet<string> dumpedLabels = new HashSet<string>();

		public static bool IsHumanOwned(object obj)
		{
			return IsTargetedEmpire(obj);
		}

		public static bool IsTargetedEmpire(object obj)
		{
			int index = ResolveEmpireIndex(obj);
			return index >= 0 && index < ModState.TargetPlayers.Length && ModState.TargetPlayers[index];
		}

		public static bool AnyTargetSelected()
		{
			for (int i = 0; i < ModState.TargetPlayers.Length; i++)
			{
				if (ModState.TargetPlayers[i])
				{
					return true;
				}
			}

			return false;
		}

		public static string GetPlayerLabel(int index)
		{
			if (index == 0)
			{
				return "Player";
			}

			return "AI " + index;
		}

		public static object ResolveMajorEmpire(object obj)
		{
			if (obj == null)
			{
				return null;
			}

			try
			{
				if (IsMajorEmpireObject(obj))
				{
					return obj;
				}

				object majorEmpire = TryGetMemberValue(obj, "majorEmpire");
				if (majorEmpire != null)
				{
					return majorEmpire;
				}

				object resolvedEmpire = ResolveEmpireMember(TryGetMemberValue(obj, "Empire"));
				if (resolvedEmpire != null)
				{
					return resolvedEmpire;
				}

				foreach (PropertyInfo property in GetAllProperties(obj.GetType()))
				{
					if (!property.Name.EndsWith(".Empire", StringComparison.Ordinal) ||
						property.GetIndexParameters().Length > 0)
					{
						continue;
					}

					object explicitEmpire = SafeGetPropertyValue(property, obj);
					resolvedEmpire = ResolveEmpireMember(explicitEmpire);
					if (resolvedEmpire != null)
					{
						return resolvedEmpire;
					}
				}
			}
			catch (Exception ex)
			{
				LogReflectionErrorOnce(ex);
			}

			return null;
		}

		public static bool IsHumanMajorEmpire(object empire)
		{
			if (empire == null)
			{
				return false;
			}

			Type type = empire.GetType();
			LogMajorEmpireBoolAndIndexMembersOnce(empire);

			string[] humanNames =
			{
				"IsControlledByHuman",
				"ControlledByHuman",
				"IsHuman",
				"IsHumanPlayer",
				"IsLocalPlayer",
				"IsLocal",
				"Local",
				"IsHumanControlled",
				"IsControlledByLocalPlayer"
			};

			foreach (string name in humanNames)
			{
				if (IsTruthyBool(TryGetMemberValue(empire, name)))
				{
					ModLog.Debug($"IsHumanMajorEmpire matched {name}=true on {type.FullName}");
					return true;
				}
			}

			string[] aiNames = { "IsAI", "IsControlledByAI", "IsMajorEmpireControlledByAI" };
			foreach (string name in aiNames)
			{
				if (IsTruthyBool(TryGetMemberValue(empire, name)))
				{
					ModLog.Debug($"IsHumanMajorEmpire matched {name}=true on {type.FullName}; treating as non-human");
					return false;
				}
			}

			return false;
		}

		public static object TryGetMemberValue(object obj, string name)
		{
			if (obj == null || string.IsNullOrEmpty(name))
			{
				return null;
			}

			try
			{
				const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
				for (Type current = obj.GetType(); current != null; current = current.BaseType)
				{
					FieldInfo field = current.GetField(name, flags);
					if (field != null)
					{
						return field.GetValue(obj);
					}

					PropertyInfo property = current.GetProperty(name, flags);
					if (property != null && property.GetIndexParameters().Length == 0)
					{
						return property.GetValue(obj, null);
					}
				}
			}
			catch (Exception ex)
			{
				LogReflectionErrorOnce(ex);
			}

			return null;
		}

		public static int ResolveEmpireIndex(object obj)
		{
			object empire = ResolveMajorEmpire(obj) ?? obj;
			if (empire == null)
			{
				return -1;
			}

			string[] names = { "Index", "EmpireIndex", "MajorEmpireIndex" };
			foreach (string name in names)
			{
				try
				{
					object value = TryGetMemberValue(empire, name);
					if (IsIntegerValue(value))
					{
						return Convert.ToInt32(value);
					}
				}
				catch (Exception ex)
				{
					LogReflectionErrorOnce(ex);
				}
			}

			return -1;
		}

		public static string SafeTypeName(object instance)
		{
			try
			{
				return instance != null ? instance.GetType().FullName : "<null instance>";
			}
			catch (Exception ex)
			{
				return "<instance type unavailable: " + ex.GetType().FullName + ": " + ex.Message + ">";
			}
		}

		public static void DumpMajorEmpireOnce(string label, object source)
		{
			try
			{
				object majorEmpire = ResolveMajorEmpire(source);
				if (majorEmpire == null)
				{
					DumpObjectDeepOnce(label, null);
					ModLog.Debug($"{label}: no MajorEmpire was resolved from {SafeTypeName(source)}");
					return;
				}

				DumpObjectDeepOnce(label, majorEmpire);
			}
			catch (Exception ex)
			{
				LogReflectionErrorOnce(ex);
			}
		}

		public static void DumpObjectDeepOnce(string label, object obj)
		{
			try
			{
				if (string.IsNullOrEmpty(label))
				{
					label = "<null label>";
				}

				if (dumpedLabels.Contains(label))
				{
					return;
				}
				dumpedLabels.Add(label);

				StringBuilder builder = new StringBuilder();
				builder.AppendLine(label);
				if (obj == null)
				{
					builder.AppendLine("Object: <null>");
					ModLog.Debug(builder.ToString());
					return;
				}

				Type type = obj.GetType();
				builder.AppendLine("Object type: " + type.FullName);
				builder.AppendLine("Fields:");
				foreach (FieldInfo field in GetAllFields(type))
				{
					object value = SafeGetFieldValue(field, obj);
					builder.AppendLine("  " + field.Name + " : " + field.FieldType.FullName + " | valueType=" + SafeTypeName(value) + " | value=" + SafeToString(value));
				}

				builder.AppendLine("Properties:");
				foreach (PropertyInfo property in GetAllProperties(type))
				{
					object value = SafeGetPropertyValue(property, obj);
					builder.AppendLine("  " + property.Name + " : " + property.PropertyType.FullName + " | valueType=" + SafeTypeName(value) + " | value=" + SafeToString(value));
				}

				ModLog.Debug(builder.ToString());
			}
			catch (Exception ex)
			{
				LogReflectionErrorOnce(ex);
			}
		}

		private static object ResolveEmpireMember(object empire)
		{
			if (empire == null)
			{
				return null;
			}

			if (IsMajorEmpireObject(empire))
			{
				return empire;
			}

			if (IsReferenceLike(empire.GetType()))
			{
				object entity = TryGetMemberValue(empire, "Entity");
				if (IsMajorEmpireObject(entity))
				{
					return entity;
				}
			}

			return null;
		}

		private static bool IsMajorEmpireObject(object obj)
		{
			if (obj == null)
			{
				return false;
			}

			Type type = obj.GetType();
			string name = type.Name ?? string.Empty;
			string fullName = type.FullName ?? string.Empty;
			return name.IndexOf("MajorEmpire", StringComparison.OrdinalIgnoreCase) >= 0 ||
				fullName.IndexOf("MajorEmpire", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static bool IsReferenceLike(Type type)
		{
			if (type == null)
			{
				return false;
			}

			string name = type.Name ?? string.Empty;
			string fullName = type.FullName ?? string.Empty;
			return name.StartsWith("Reference`", StringComparison.Ordinal) ||
				name.IndexOf("Reference", StringComparison.OrdinalIgnoreCase) >= 0 ||
				fullName.IndexOf("Reference", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static void LogMajorEmpireBoolAndIndexMembersOnce(object empire)
		{
			try
			{
				if (empire == null)
				{
					return;
				}

				const string label = "MajorEmpire bool/int member log";
				if (dumpedLabels.Contains(label))
				{
					return;
				}
				dumpedLabels.Add(label);

				Type type = empire.GetType();
				StringBuilder builder = new StringBuilder();
				builder.AppendLine(label);
				builder.AppendLine("Object type: " + type.FullName);
				builder.AppendLine("Bool fields/properties:");

				foreach (FieldInfo field in GetAllFields(type))
				{
					object value = SafeGetFieldValue(field, empire);
					if (value is bool)
					{
						builder.AppendLine("  " + field.Name + " : " + field.FieldType.FullName + " = " + value);
					}
				}

				foreach (PropertyInfo property in GetAllProperties(type))
				{
					object value = SafeGetPropertyValue(property, empire);
					if (value is bool)
					{
						builder.AppendLine("  " + property.Name + " : " + property.PropertyType.FullName + " = " + value);
					}
				}

				builder.AppendLine("Index-like int fields/properties:");
				foreach (FieldInfo field in GetAllFields(type))
				{
					object value = SafeGetFieldValue(field, empire);
					if (IsIntegerValue(value) && IsIndexLikeName(field.Name))
					{
						builder.AppendLine("  " + field.Name + " : " + field.FieldType.FullName + " = " + value);
					}
				}

				foreach (PropertyInfo property in GetAllProperties(type))
				{
					object value = SafeGetPropertyValue(property, empire);
					if (IsIntegerValue(value) && IsIndexLikeName(property.Name))
					{
						builder.AppendLine("  " + property.Name + " : " + property.PropertyType.FullName + " = " + value);
					}
				}

				ModLog.Debug(builder.ToString());
			}
			catch (Exception ex)
			{
				LogReflectionErrorOnce(ex);
			}
		}

		private static IEnumerable<FieldInfo> GetAllFields(Type type)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			HashSet<string> seen = new HashSet<string>();
			for (Type current = type; current != null; current = current.BaseType)
			{
				foreach (FieldInfo field in current.GetFields(flags))
				{
					string key = field.DeclaringType.FullName + "." + field.Name;
					if (seen.Add(key))
					{
						yield return field;
					}
				}
			}
		}

		private static IEnumerable<PropertyInfo> GetAllProperties(Type type)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			HashSet<string> seen = new HashSet<string>();
			for (Type current = type; current != null; current = current.BaseType)
			{
				foreach (PropertyInfo property in current.GetProperties(flags))
				{
					string key = property.DeclaringType.FullName + "." + property.Name;
					if (seen.Add(key))
					{
						yield return property;
					}
				}
			}
		}

		private static object SafeGetFieldValue(FieldInfo field, object obj)
		{
			try
			{
				return field.GetValue(obj);
			}
			catch
			{
				return null;
			}
		}

		private static object SafeGetPropertyValue(PropertyInfo property, object obj)
		{
			try
			{
				if (property.GetIndexParameters().Length > 0)
				{
					return null;
				}

				return property.GetValue(obj, null);
			}
			catch
			{
				return null;
			}
		}

		private static bool IsTruthyBool(object value)
		{
			try
			{
				if (value is bool boolValue)
				{
					return boolValue;
				}

				if (value != null && value.GetType().FullName == "System.Boolean")
				{
					return Convert.ToBoolean(value);
				}
			}
			catch (Exception ex)
			{
				LogReflectionErrorOnce(ex);
			}

			return false;
		}

		public static bool IsIntegerValue(object value)
		{
			return value is int || value is short || value is long || value is byte;
		}

		private static bool IsIndexLikeName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}

			return name.IndexOf("Index", StringComparison.OrdinalIgnoreCase) >= 0 ||
				name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 ||
				name.IndexOf("Empire", StringComparison.OrdinalIgnoreCase) >= 0 ||
				name.IndexOf("Slot", StringComparison.OrdinalIgnoreCase) >= 0 ||
				name.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0 ||
				name.IndexOf("Client", StringComparison.OrdinalIgnoreCase) >= 0 ||
				name.IndexOf("Network", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static string SafeToString(object value)
		{
			try
			{
				return value != null ? value.ToString() : "<null>";
			}
			catch (Exception ex)
			{
				return "<ToString error: " + ex.GetType().Name + ": " + ex.Message + ">";
			}
		}

		private static void LogReflectionErrorOnce(Exception ex)
		{
			if (loggedOwnershipReflectionError)
			{
				return;
			}

			loggedOwnershipReflectionError = true;
			ModLog.Debug("Ownership reflection error: " + ex);
		}

		private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

			public new bool Equals(object x, object y)
			{
				return ReferenceEquals(x, y);
			}

			public int GetHashCode(object obj)
			{
				return obj != null ? RuntimeHelpers.GetHashCode(obj) : 0;
			}
		}
	}
}
