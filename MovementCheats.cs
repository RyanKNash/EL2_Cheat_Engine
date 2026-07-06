using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EL2_cheat_engine
{
	public static class MovementCheats
	{
		private static readonly string[] MovementDiscoveryTerms =
		{
			"Movement", "Move", "Remaining", "ActionPoint", "Action", "GodSpeed"
		};

		public static void RefillMovement()
		{
			ModLog.Info("[EL2 Sandbox] Refill Movement Points clicked");

			if (!EmpireHelpers.AnyTargetSelected())
			{
				ModLog.Warn("[EL2 Sandbox] Refill Movement Points blocked: no target empire/player selected.");
				return;
			}

			List<object> armies = ArmyHelpers.GetAllArmies().ToList();
			if (armies.Count == 0)
			{
				ModLog.Warn("[EL2 Sandbox] Refill Movement Points found no armies.");
				return;
			}

			RefillTargetArmies(armies);
		}

		public static void RefillMovementPoints()
		{
			RefillMovement();
		}

		private static void RefillTargetArmies(IEnumerable<object> armies)
		{
			bool touchedTargetArmy = false;
			foreach (object army in armies)
			{
				if (!EmpireHelpers.IsTargetEmpire(army))
				{
					LogSkippedArmy(army);
					continue;
				}

				touchedTargetArmy = true;
				TryRefillArmy(army);
			}

			if (!touchedTargetArmy)
			{
				ModLog.Warn("[EL2 Sandbox] Refill Movement Points found no armies matching selectedTargets=" + EmpireHelpers.GetSelectedTargetIndexesText() + ".");
			}
		}

		private static bool TryRefillArmy(object army)
		{
			object maxMovement = ArmyHelpers.GetMovementProperty(army);
			object remainingMovement = ArmyHelpers.GetRemainingMovementProperty(army);
			if (!TryReadMovement(army, maxMovement, remainingMovement, out int maxRaw, out int beforeRaw))
			{
				LogMovementFallbackCandidates(army);
				return false;
			}

			LogRemainingMovementDiagnostics(remainingMovement, beforeRaw, maxRaw, beforeRaw);
			bool writeAttempted = SimulationPropertyHelpers.WriteRaw(remainingMovement, maxRaw) ||
				SimulationPropertyHelpers.TryWrite(ArmyHelpers.GetUnitCollection(army), "MinRemainingMovementPoints", maxRaw);

			object updatedMovement = ArmyHelpers.GetRemainingMovementProperty(army) ?? remainingMovement;
			int afterRaw = ReadRawOrDefault(updatedMovement, beforeRaw);
			LogRemainingMovementDiagnostics(updatedMovement, beforeRaw, maxRaw, afterRaw);

			if (!writeAttempted || afterRaw == beforeRaw)
			{
				LogUnchangedMovement(army, beforeRaw, maxRaw, afterRaw);
				LogMovementFallbackCandidates(army);
				return false;
			}

			if (afterRaw == maxRaw || afterRaw > beforeRaw)
			{
				LogRefilledArmy(army, beforeRaw, maxRaw, afterRaw);
				return true;
			}

			LogUnexpectedMovementResult(army, beforeRaw, maxRaw, afterRaw);
			return false;
		}

		private static bool TryReadMovement(object army, object maxMovement, object remainingMovement, out int maxRaw, out int beforeRaw)
		{
			maxRaw = 0;
			beforeRaw = 0;
			if (maxMovement != null &&
				remainingMovement != null &&
				SimulationPropertyHelpers.ReadRaw(maxMovement, out maxRaw) &&
				SimulationPropertyHelpers.ReadRaw(remainingMovement, out beforeRaw))
			{
				return true;
			}

			ModLog.Error($"[EL2 Sandbox] Skipped armyId={ArmyHelpers.GetArmyIdText(army)} armyClientId={ArmyHelpers.GetArmyClientIdText(army)} empireIndex={FormatEmpireIndex(army)}: movement properties were not readable.");
			return false;
		}

		private static void LogRemainingMovementDiagnostics(object remainingMovement, int beforeRaw, int attemptedRaw, int afterRaw)
		{
			bool hasRawField = SimulationPropertyHelpers.HasFixedPointRawValueField(remainingMovement, out bool writable);
			ModLog.Info("[EL2 Sandbox] MinRemainingMovementPoints diagnostics " +
				"objectType=" + OwnershipResolver.SafeTypeName(remainingMovement) +
				" fixedPointRawValueFieldExists=" + hasRawField +
				" fixedPointRawValueWritable=" + writable +
				" rawBefore=" + beforeRaw +
				" rawAttempted=" + attemptedRaw +
				" rawAfter=" + afterRaw);
		}

		private static void LogRefilledArmy(object army, int beforeRaw, int maxRaw, int afterRaw)
		{
			ModLog.Info($"[EL2 Sandbox] Refilled movement armyId={ArmyHelpers.GetArmyIdText(army)} armyClientId={ArmyHelpers.GetArmyClientIdText(army)} empireIndex={FormatEmpireIndex(army)} remainingBefore={FormatRaw(beforeRaw)} max={FormatRaw(maxRaw)} remainingAfter={FormatRaw(afterRaw)}");
		}

		private static void LogUnchangedMovement(object army, int beforeRaw, int maxRaw, int afterRaw)
		{
			ModLog.Warn($"[EL2 Sandbox] Movement refill did not change armyId={ArmyHelpers.GetArmyIdText(army)} armyClientId={ArmyHelpers.GetArmyClientIdText(army)} empireIndex={FormatEmpireIndex(army)} remainingBefore={FormatRaw(beforeRaw)} max={FormatRaw(maxRaw)} remainingAfter={FormatRaw(afterRaw)}");
			ModLog.Warn("[EL2 Sandbox] MinRemainingMovementPoints appears read-only/computed.");
		}

		private static void LogUnexpectedMovementResult(object army, int beforeRaw, int maxRaw, int afterRaw)
		{
			ModLog.Warn($"[EL2 Sandbox] Movement refill produced unexpected value armyId={ArmyHelpers.GetArmyIdText(army)} armyClientId={ArmyHelpers.GetArmyClientIdText(army)} empireIndex={FormatEmpireIndex(army)} remainingBefore={FormatRaw(beforeRaw)} max={FormatRaw(maxRaw)} remainingAfter={FormatRaw(afterRaw)}");
		}

		private static void LogMovementFallbackCandidates(object army)
		{
			LogMovementFallbackCandidates("Army", army);
			object unitCollection = ArmyHelpers.GetUnitCollection(army);
			if (!ReferenceEquals(unitCollection, army))
			{
				LogMovementFallbackCandidates("UnitCollection", unitCollection);
			}
		}

		private static void LogMovementFallbackCandidates(string ownerLabel, object owner)
		{
			if (owner == null)
			{
				return;
			}

			foreach (MemberInfo member in ReflectionHelpers.GetAllInstanceMembers(owner.GetType()))
			{
				if (IsMovementCandidateName(member.Name))
				{
					LogWritableMovementCandidate(ownerLabel, owner, member);
				}
			}
		}

		private static void LogWritableMovementCandidate(string ownerLabel, object owner, MemberInfo member)
		{
			object value = ReflectionHelpers.ReadInstanceMember(owner, member);
			bool editable = SimulationPropertyHelpers.IsEditablePropertyLike(value);
			bool writableFixedPoint = SimulationPropertyHelpers.IsWritableFixedPointLikeMember(member);
			if (!editable && !writableFixedPoint)
			{
				return;
			}

			SimulationPropertyHelpers.ReadRaw(value, out int raw);
			ModLog.Warn("[EL2 Sandbox] Movement fallback candidate " +
				"owner=" + ownerLabel +
				" ownerType=" + OwnershipResolver.SafeTypeName(owner) +
				" member=" + member.Name +
				" memberType=" + FormatMemberType(member) +
				" valueType=" + OwnershipResolver.SafeTypeName(value) +
				" editableProperty=" + editable +
				" writableFixedPointLike=" + writableFixedPoint +
				" raw=" + raw);
		}

		private static bool IsMovementCandidateName(string name)
		{
			foreach (string term in MovementDiscoveryTerms)
			{
				if (name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}

			return false;
		}

		private static void LogSkippedArmy(object army)
		{
			if (Config.DEBUG_MOVEMENT)
			{
				ModLog.Debug($"[EL2 Sandbox] Skipped armyId={ArmyHelpers.GetArmyIdText(army)} empireIndex={FormatEmpireIndex(army)} selectedTargets={EmpireHelpers.GetSelectedTargetIndexesText()}");
			}
		}

		private static int ReadRawOrDefault(object propertyObj, int defaultRaw)
		{
			return SimulationPropertyHelpers.ReadRaw(propertyObj, out int raw) ? raw : defaultRaw;
		}

		private static string FormatEmpireIndex(object army)
		{
			int? empireIndex = EmpireHelpers.TryGetEmpireIndex(army);
			return empireIndex.HasValue ? empireIndex.Value.ToString() : "unknown";
		}

		private static string FormatMemberType(MemberInfo member)
		{
			Type type = ReflectionHelpers.GetMemberValueType(member);
			return type != null ? type.FullName : "unknown";
		}

		private static string FormatRaw(int raw)
		{
			if (raw % 1000 == 0)
			{
				return (raw / 1000).ToString();
			}

			return (raw / 1000m).ToString("0.###");
		}
	}
}
