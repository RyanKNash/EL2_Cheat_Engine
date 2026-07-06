namespace EL2_cheat_engine
{
	public static class EmpireHelpers
	{
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

		public static bool IsTargetEmpire(int empireIndex)
		{
			return empireIndex >= 0 &&
				empireIndex < ModState.TargetPlayers.Length &&
				ModState.TargetPlayers[empireIndex];
		}

		public static bool IsTargetEmpire(object army)
		{
			int? empireIndex = TryGetEmpireIndex(army);
			return empireIndex.HasValue && IsTargetEmpire(empireIndex.Value);
		}

		public static int? TryGetEmpireIndex(object army)
		{
			if (army == null)
			{
				return null;
			}

			if (ReflectionHelpers.TryReadIntMember(army, "EmpireIndex", out int armyIndex))
			{
				return armyIndex;
			}

			object unitCollection = ArmyHelpers.GetUnitCollection(army);
			if (!ReferenceEquals(unitCollection, army) &&
				ReflectionHelpers.TryReadIntMember(unitCollection, "EmpireIndex", out int unitCollectionIndex))
			{
				return unitCollectionIndex;
			}

			if (Config.DEBUG_EMPIRE)
			{
				ModLog.Debug("[EL2 Sandbox] Empire index unavailable for " + OwnershipResolver.SafeTypeName(army));
			}

			return null;
		}

		public static string GetSelectedTargetIndexesText()
		{
			string selected = string.Empty;
			for (int i = 0; i < ModState.TargetPlayers.Length; i++)
			{
				if (!ModState.TargetPlayers[i])
				{
					continue;
				}

				selected += selected.Length > 0 ? ", " + i : i.ToString();
			}

			return selected.Length > 0 ? selected : "none";
		}
	}
}
