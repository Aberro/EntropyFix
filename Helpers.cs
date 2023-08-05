using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EntropyFix
{
	public static class EnumHelper
	{
		public static string GetDescription(this Enum value)
		{
			var fieldInfo = value.GetType().GetField(value.ToString());
			var attribute = fieldInfo.GetCustomAttribute<DescriptionAttribute>();
			return attribute == null ? value.ToString() : attribute.Description;
		}
		public static string GetDisplayName(this Enum value)
		{
			var fieldInfo = value.GetType().GetField(value.ToString());
			var attribute = fieldInfo.GetCustomAttribute<DisplayNameAttribute>();
			return attribute == null ? value.ToString() : attribute.DisplayName;
		}

		public static PatchCategory GetCategory(this Type type)
		{
			var attribute = type.GetCustomAttribute<HarmonyPatchCategory>();
			return attribute?.Category ?? PatchCategory.None;
		}
	}
}
