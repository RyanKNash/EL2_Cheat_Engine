using System;
using System.Collections.Generic;
using System.Reflection;

namespace EL2_cheat_engine
{
	public static class ReflectionHelpers
	{
		public static object TryGetMemberValue(object obj, string name)
		{
			if (obj == null || string.IsNullOrEmpty(name))
			{
				return null;
			}

			foreach (MemberInfo member in GetAllInstanceMembers(obj.GetType()))
			{
				if (member.Name == name)
				{
					return ReadInstanceMember(obj, member);
				}
			}

			return null;
		}

		public static bool TrySetMemberValue(object obj, string name, object value)
		{
			if (obj == null || string.IsNullOrEmpty(name))
			{
				return false;
			}

			foreach (MemberInfo member in GetAllInstanceMembers(obj.GetType()))
			{
				if (member.Name == name && TrySetMemberValue(obj, member, value))
				{
					return true;
				}
			}

			return false;
		}

		public static bool TryGetMember(object obj, string name, out MemberInfo member)
		{
			member = null;
			if (obj == null || string.IsNullOrEmpty(name))
			{
				return false;
			}

			foreach (MemberInfo candidate in GetAllInstanceMembers(obj.GetType()))
			{
				if (candidate.Name == name)
				{
					member = candidate;
					return true;
				}
			}

			return false;
		}

		public static Type GetMemberValueType(MemberInfo member)
		{
			if (member is FieldInfo field)
			{
				return field.FieldType;
			}

			if (member is PropertyInfo property)
			{
				return property.PropertyType;
			}

			return null;
		}

		public static bool IsWritableMember(MemberInfo member)
		{
			if (member is FieldInfo field)
			{
				return !field.IsInitOnly;
			}

			if (member is PropertyInfo property)
			{
				return property.CanWrite;
			}

			return false;
		}

		public static bool TryReadIntMember(object obj, string name, out int value)
		{
			value = 0;
			object memberValue = TryGetMemberValue(obj, name);
			if (!IsIntegerValue(memberValue))
			{
				return false;
			}

			try
			{
				value = Convert.ToInt32(memberValue);
				return true;
			}
			catch (Exception ex)
			{
				LogReflectionDebug("Could not read int member " + name + ": " + ex.Message);
				return false;
			}
		}

		public static IEnumerable<MemberInfo> GetAllInstanceMembers(Type type)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy;
			return GetAllMembers(type, flags);
		}

		public static IEnumerable<MemberInfo> GetAllStaticMembers(Type type)
		{
			const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;
			return GetAllMembers(type, flags);
		}

		public static object ReadStaticMember(MemberInfo member)
		{
			try
			{
				if (member is FieldInfo field)
				{
					return field.GetValue(null);
				}

				if (member is PropertyInfo property && property.GetIndexParameters().Length == 0)
				{
					return property.GetValue(null, null);
				}
			}
			catch (Exception ex)
			{
				LogReflectionDebug("Could not read static member " + member.Name + ": " + ex.Message);
			}

			return null;
		}

		public static object ReadInstanceMember(object obj, MemberInfo member)
		{
			try
			{
				if (member is FieldInfo field)
				{
					return field.GetValue(obj);
				}

				if (member is PropertyInfo property && property.GetIndexParameters().Length == 0)
				{
					return property.GetValue(obj, null);
				}
			}
			catch (Exception ex)
			{
				LogReflectionDebug("Could not read member " + member.Name + ": " + ex.Message);
			}

			return null;
		}

		public static bool IsIntegerValue(object value)
		{
			return value is int || value is short || value is long || value is byte;
		}

		private static IEnumerable<MemberInfo> GetAllMembers(Type type, BindingFlags flags)
		{
			HashSet<string> seen = new HashSet<string>();
			for (Type current = type; current != null; current = current.BaseType)
			{
				foreach (FieldInfo field in current.GetFields(flags))
				{
					if (seen.Add(field.DeclaringType.FullName + "." + field.Name))
					{
						yield return field;
					}
				}

				foreach (PropertyInfo property in current.GetProperties(flags))
				{
					if (seen.Add(property.DeclaringType.FullName + "." + property.Name))
					{
						yield return property;
					}
				}
			}
		}

		private static bool TrySetMemberValue(object obj, MemberInfo member, object value)
		{
			try
			{
				if (member is FieldInfo field && !field.IsInitOnly)
				{
					field.SetValue(obj, ConvertValue(value, field.FieldType));
					return true;
				}

				if (member is PropertyInfo property && property.CanWrite)
				{
					property.SetValue(obj, ConvertValue(value, property.PropertyType), null);
					return true;
				}
			}
			catch (Exception ex)
			{
				LogReflectionDebug("Could not write member " + member.Name + ": " + ex.Message);
			}

			return false;
		}

		private static object ConvertValue(object value, Type targetType)
		{
			Type type = Nullable.GetUnderlyingType(targetType) ?? targetType;
			if (value == null || type.IsInstanceOfType(value))
			{
				return value;
			}

			return Convert.ChangeType(value, type);
		}

		private static void LogReflectionDebug(string message)
		{
			if (Config.DEBUG_REFLECTION)
			{
				ModLog.Debug("[EL2 Sandbox] Reflection: " + message);
			}
		}
	}
}
