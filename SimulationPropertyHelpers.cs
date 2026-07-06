using System;
using System.Reflection;
using Amplitude;
using HarmonyLib;

namespace EL2_cheat_engine
{
	public static class SimulationPropertyHelpers
	{
		public static bool ReadRaw(object propertyObj, out int raw)
		{
			raw = 0;
			if (propertyObj == null)
			{
				return false;
			}

			return ReflectionHelpers.TryReadIntMember(propertyObj, "FixedPointRawValue", out raw) ||
				ReflectionHelpers.TryReadIntMember(propertyObj, "RawValue", out raw) ||
				ReflectionHelpers.TryReadIntMember(propertyObj, "Raw", out raw) ||
				TryReadDisplayRaw(propertyObj, out raw);
		}

		public static bool WriteRaw(object propertyObj, int raw)
		{
			if (propertyObj == null)
			{
				return false;
			}

			return ReflectionHelpers.TrySetMemberValue(propertyObj, "FixedPointRawValue", raw) ||
				ReflectionHelpers.TrySetMemberValue(propertyObj, "RawValue", raw) ||
				TryWrite(propertyObj, "Value", raw) ||
				TryWrite(propertyObj, "CurrentValue", raw);
		}

		public static bool HasFixedPointRawValueField(object propertyObj, out bool writable)
		{
			writable = false;
			if (propertyObj == null)
			{
				return false;
			}

			if (!ReflectionHelpers.TryGetMember(propertyObj, "FixedPointRawValue", out MemberInfo member))
			{
				return false;
			}

			writable = ReflectionHelpers.IsWritableMember(member);
			return member is FieldInfo;
		}

		public static bool IsEditablePropertyLike(object propertyObj)
		{
			if (propertyObj == null)
			{
				return false;
			}

			Type type = propertyObj.GetType();
			string fullName = type.FullName ?? string.Empty;
			if (fullName.IndexOf("EditableProperty", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}

			return HasFixedPointRawValueField(propertyObj, out bool writable) && writable;
		}

		public static bool IsWritableFixedPointLikeMember(MemberInfo member)
		{
			Type type = ReflectionHelpers.GetMemberValueType(member);
			if (type == null || !ReflectionHelpers.IsWritableMember(member))
			{
				return false;
			}

			return type == typeof(FixedPoint) ||
				(type.FullName ?? string.Empty).IndexOf("FixedPoint", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		public static bool ReadDisplayValue(object propertyObj, out string display)
		{
			display = null;
			if (propertyObj == null)
			{
				return false;
			}

			try
			{
				display = propertyObj.ToString();
				return !string.IsNullOrEmpty(display);
			}
			catch
			{
				return false;
			}
		}

		public static bool ReadFixedPoint(object propertyObj, out FixedPoint fixedPoint)
		{
			fixedPoint = default(FixedPoint);
			if (!ReadRaw(propertyObj, out int raw))
			{
				return false;
			}

			fixedPoint = FixedPoint.FixedPointFromRawValue(raw);
			return true;
		}

		public static bool TryRead(object propertyObj, out int raw)
		{
			return ReadRaw(propertyObj, out raw);
		}

		public static bool TryWrite(object propertyObj, int raw)
		{
			return WriteRaw(propertyObj, raw);
		}

		public static bool TryWrite(object owner, string memberName, int raw)
		{
			if (owner == null)
			{
				return false;
			}

			object propertyObj = ReflectionHelpers.TryGetMemberValue(owner, memberName);
			if (WriteRaw(propertyObj, raw))
			{
				return true;
			}

			return TryWriteMemberFromRaw(owner, memberName, raw);
		}

		private static bool TryReadDisplayRaw(object propertyObj, out int raw)
		{
			raw = 0;
			if (TryReadDecimalMember(propertyObj, "Value", out decimal value) ||
				TryReadDecimalMember(propertyObj, "CurrentValue", out value) ||
				TryConvertToDecimal(propertyObj, out value))
			{
				raw = (int)(value * 1000m);
				return true;
			}

			return false;
		}

		private static bool TryWriteMemberFromRaw(object owner, string name, int raw)
		{
			foreach (MemberInfo member in ReflectionHelpers.GetAllInstanceMembers(owner.GetType()))
			{
				if (member.Name == name && TrySetRawValue(owner, member, raw))
				{
					return true;
				}
			}

			return false;
		}

		private static bool TrySetRawValue(object owner, MemberInfo member, int raw)
		{
			try
			{
				if (member is FieldInfo field && !field.IsInitOnly)
				{
					return TrySetConvertedRaw(owner, field, field.FieldType, raw);
				}

				if (member is PropertyInfo property && property.CanWrite)
				{
					return TrySetConvertedRaw(owner, property, property.PropertyType, raw);
				}
			}
			catch (Exception ex)
			{
				LogDebug("Write raw member failed: " + ex.Message);
			}

			return false;
		}

		private static bool TrySetConvertedRaw(object owner, MemberInfo member, Type type, int raw)
		{
			if (!TryCreateValueFromRaw(type, raw, out object value))
			{
				return false;
			}

			if (member is FieldInfo field)
			{
				field.SetValue(owner, value);
				return true;
			}

			((PropertyInfo)member).SetValue(owner, value, null);
			return true;
		}

		private static bool TryCreateValueFromRaw(Type type, int raw, out object value)
		{
			value = null;
			Type targetType = Nullable.GetUnderlyingType(type) ?? type;

			if (targetType == typeof(FixedPoint))
			{
				value = FixedPoint.FixedPointFromRawValue(raw);
				return true;
			}

			return TryCreateNumericFromRaw(targetType, raw, out value);
		}

		private static bool TryCreateNumericFromRaw(Type type, int raw, out object value)
		{
			value = null;
			decimal displayValue = raw / 1000m;

			if (type == typeof(int))
			{
				value = raw;
			}
			else if (type == typeof(float))
			{
				value = (float)displayValue;
			}
			else if (type == typeof(double))
			{
				value = (double)displayValue;
			}
			else if (type == typeof(decimal))
			{
				value = displayValue;
			}

			return value != null;
		}

		private static bool TryReadDecimalMember(object owner, string name, out decimal value)
		{
			return TryConvertToDecimal(ReflectionHelpers.TryGetMemberValue(owner, name), out value);
		}

		private static bool TryConvertToDecimal(object value, out decimal result)
		{
			result = 0m;
			try
			{
				if (value is IConvertible)
				{
					result = Convert.ToDecimal(value);
					return true;
				}
			}
			catch (Exception ex)
			{
				LogDebug("Decimal conversion failed: " + ex.Message);
			}

			return false;
		}

		private static void LogDebug(string message)
		{
			if (Config.DEBUG_REFLECTION)
			{
				ModLog.Debug("[EL2 Sandbox] SimulationProperty: " + message);
			}
		}
	}
}
