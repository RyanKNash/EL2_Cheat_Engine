using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace EL2_cheat_engine
{
	public static class ArmyHelpers
	{
		private const int MaxTraversalItems = 50000;

		public static IEnumerable<object> GetAllArmies()
		{
			List<object> armies = new List<object>();
			HashSet<object> seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
			int visitedCount = 0;

			foreach (Type type in GetSandboxTypes())
			{
				AddNamedStaticContainers(type, armies, seen, ref visitedCount);
				AddNamedLikeStaticContainers(type, armies, seen, ref visitedCount);
			}

			return armies;
		}

		public static int? GetArmyId(object army)
		{
			return ReadFirstIntMember(army, new string[] { "Id", "ID", "ArmyId", "ArmyID", "EntityId", "EntityID", "Index" });
		}

		public static int? GetArmyClientId(object army)
		{
			return ReadFirstIntMember(army, new string[] { "ClientId", "ClientID", "ClientEntityId", "ClientEntityID" });
		}

		public static int? GetEmpireIndex(object army)
		{
			return EmpireHelpers.TryGetEmpireIndex(army);
		}

		public static object GetWorldPosition(object army)
		{
			return ReflectionHelpers.TryGetMemberValue(army, "WorldPosition") ??
				ReflectionHelpers.TryGetMemberValue(army, "Position");
		}

		public static object GetMovementProperty(object army)
		{
			return ReflectionHelpers.TryGetMemberValue(GetUnitCollection(army), "MinMovementPoints");
		}

		public static object GetRemainingMovementProperty(object army)
		{
			return ReflectionHelpers.TryGetMemberValue(GetUnitCollection(army), "MinRemainingMovementPoints");
		}

		public static object GetHealthProperty(object army)
		{
			object unitCollection = GetUnitCollection(army);
			return ReflectionHelpers.TryGetMemberValue(unitCollection, "Health") ??
				ReflectionHelpers.TryGetMemberValue(unitCollection, "HitPoints") ??
				ReflectionHelpers.TryGetMemberValue(unitCollection, "MinHealth") ??
				ReflectionHelpers.TryGetMemberValue(unitCollection, "MinRemainingHealth");
		}

		public static object GetUnitCollection(object army)
		{
			return ReflectionHelpers.TryGetMemberValue(army, "UnitCollection") ?? army;
		}

		public static string GetArmyIdText(object army)
		{
			int? id = GetArmyId(army);
			return id.HasValue ? id.Value.ToString() : ReadFirstTextMember(army, IdMemberNames);
		}

		public static string GetArmyClientIdText(object army)
		{
			int? id = GetArmyClientId(army);
			return id.HasValue ? id.Value.ToString() : ReadFirstTextMember(army, ClientIdMemberNames);
		}

		private static IEnumerable<Type> GetSandboxTypes()
		{
			string[] names =
			{
				"Amplitude.Mercury.Sandbox.Sandbox",
				"Amplitude.Mercury.Simulation.Sandbox",
				"Amplitude.Mercury.Simulation.World"
			};

			foreach (string name in names)
			{
				Type type = AccessTools.TypeByName(name);
				if (type != null)
				{
					yield return type;
				}
			}
		}

		private static void AddNamedStaticContainers(Type type, List<object> armies, HashSet<object> seen, ref int visitedCount)
		{
			string[] names = { "Armies", "ArmyByGUID", "ArmiesByGUID", "UnitCollections", "EntityRepository", "Entities", "MajorEmpires" };
			foreach (string name in names)
			{
				AddArmiesFromContainer(ReadStaticMember(type, name), armies, seen, 4, ref visitedCount);
			}
		}

		private static void AddNamedLikeStaticContainers(Type type, List<object> armies, HashSet<object> seen, ref int visitedCount)
		{
			foreach (MemberInfo member in ReflectionHelpers.GetAllStaticMembers(type))
			{
				if (IsArmyContainerName(member.Name))
				{
					AddArmiesFromContainer(ReflectionHelpers.ReadStaticMember(member), armies, seen, 4, ref visitedCount);
				}
			}
		}

		private static void AddArmiesFromContainer(object container, List<object> armies, HashSet<object> seen, int depth, ref int visitedCount)
		{
			if (container == null || visitedCount > MaxTraversalItems)
			{
				return;
			}

			visitedCount++;
			if (!TryTrackObject(container, seen) || TryAddArmy(container, armies) || depth <= 0 || container is string)
			{
				return;
			}

			if (container is IDictionary dictionary)
			{
				AddDictionaryValues(dictionary, armies, seen, depth, ref visitedCount);
				return;
			}

			if (container is IEnumerable enumerable)
			{
				AddEnumerableItems(enumerable, armies, seen, depth, ref visitedCount);
				return;
			}

			AddNestedArmyContainers(container, armies, seen, depth, ref visitedCount);
		}

		private static void AddDictionaryValues(IDictionary dictionary, List<object> armies, HashSet<object> seen, int depth, ref int visitedCount)
		{
			foreach (object value in dictionary.Values)
			{
				AddArmiesFromContainer(value, armies, seen, depth - 1, ref visitedCount);
			}
		}

		private static void AddEnumerableItems(IEnumerable enumerable, List<object> armies, HashSet<object> seen, int depth, ref int visitedCount)
		{
			foreach (object item in enumerable)
			{
				AddArmiesFromContainer(item, armies, seen, depth - 1, ref visitedCount);
			}
		}

		private static void AddNestedArmyContainers(object container, List<object> armies, HashSet<object> seen, int depth, ref int visitedCount)
		{
			foreach (MemberInfo member in ReflectionHelpers.GetAllInstanceMembers(container.GetType()))
			{
				if (IsArmyContainerName(member.Name))
				{
					AddArmiesFromContainer(ReflectionHelpers.ReadInstanceMember(container, member), armies, seen, depth - 1, ref visitedCount);
				}
			}
		}

		private static bool TryTrackObject(object obj, HashSet<object> seen)
		{
			Type type = obj.GetType();
			return type.IsValueType || seen.Add(obj);
		}

		private static bool TryAddArmy(object obj, List<object> armies)
		{
			if (!IsArmyObject(obj))
			{
				return false;
			}

			armies.Add(obj);
			return true;
		}

		private static bool IsArmyObject(object obj)
		{
			Type type = obj.GetType();
			return string.Equals(type.Name, "Army", StringComparison.Ordinal) &&
				(type.FullName ?? string.Empty).IndexOf("Simulation", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static bool IsArmyContainerName(string name)
		{
			return Contains(name, "Army") ||
				Contains(name, "Armies") ||
				Contains(name, "UnitCollection") ||
				Contains(name, "EntityRepository") ||
				Contains(name, "Entities") ||
				Contains(name, "MajorEmpires");
		}

		private static object ReadStaticMember(Type type, string name)
		{
			foreach (MemberInfo member in ReflectionHelpers.GetAllStaticMembers(type))
			{
				if (member.Name == name)
				{
					return ReflectionHelpers.ReadStaticMember(member);
				}
			}

			return null;
		}

		private static int? ReadFirstIntMember(object obj, string[] names)
		{
			foreach (string name in names)
			{
				if (ReflectionHelpers.TryReadIntMember(obj, name, out int value))
				{
					return value;
				}
			}

			return null;
		}

		private static string ReadFirstTextMember(object obj, string[] names)
		{
			foreach (string name in names)
			{
				object value = ReflectionHelpers.TryGetMemberValue(obj, name);
				if (value != null)
				{
					return SafeToString(value);
				}
			}

			return "unknown";
		}

		private static string SafeToString(object value)
		{
			try
			{
				return value != null ? value.ToString() : "unknown";
			}
			catch
			{
				return "unprintable";
			}
		}

		private static bool Contains(string value, string pattern)
		{
			return !string.IsNullOrEmpty(value) &&
				value.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static readonly string[] IdMemberNames =
		{
			"GUID", "Guid", "Id", "ID", "EntityGUID", "EntityGuid", "ArmyGUID", "ArmyGuid"
		};

		private static readonly string[] ClientIdMemberNames =
		{
			"ClientId", "ClientID", "ClientGuid", "ClientGUID", "ClientEntityGuid", "ClientEntityGUID"
		};

		private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

			public new bool Equals(object x, object y)
			{
				return ReferenceEquals(x, y);
			}

			public int GetHashCode(object obj)
			{
				return obj != null ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj) : 0;
			}
		}
	}
}
