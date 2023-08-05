using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace EntropyFix
{
	/// <summary>Annotation to define a category for use with PatchCategory</summary>
	///
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class HarmonyPatchCategory : HarmonyAttribute
	{
		public PatchCategory Category { get; }
		/// <summary>Annotation specifying the category</summary>
		/// <param name="category">Name of patch category</param>
		///
		public HarmonyPatchCategory(PatchCategory category)
		{
			Category = category;
		}
	}
	public class DisplayNameAttribute : Attribute
	{
		public string DisplayName { get; }

		public DisplayNameAttribute(string displayName)
		{
			DisplayName = displayName;
		}
	}
}
