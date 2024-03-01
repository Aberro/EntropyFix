using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntropyFix.Patches
{
	/// <summary>
	/// A patch to allow writing to the mode logic.
	/// </summary>
	[HarmonyPatch(typeof(AdvancedTablet), nameof(AdvancedTablet.CanLogicWrite))]
	[HarmonyPatchCategory(PatchCategory.AdvancedTabletWriteableMode)]
	public class AdvancedTabletCanLogicWritePatch
	{
		[UsedImplicitly]
		public static bool Prefix(AdvancedTablet __instance, LogicType logicType, ref bool __result)
		{
			if (logicType == LogicType.Mode)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// A patch to read the mode values and switch Tablet cartridges.
	/// </summary>
	[HarmonyPatch(typeof(AdvancedTablet), nameof(AdvancedTablet.SetLogicValue))]
	[HarmonyPatchCategory(PatchCategory.AdvancedTabletWriteableMode)]
	public class AdvancedTabletSetLogicValuePatch
	{
		[UsedImplicitly]
		public static bool Prefix(AdvancedTablet __instance, LogicType logicType, double value, ref int ___currentCartSlot)
		{
			if (logicType == LogicType.Mode)
			{
				OnServer.Interact(__instance.InteractMode, ___currentCartSlot);
				if (value >= 0 && (Math.Abs(value % 1) < 0.00001 || Math.Abs(value % 1) > 0.9999) && value < __instance.CartridgeSlots.Count)
				{
					___currentCartSlot = (int)value;
					SwitchMode(__instance);
				}
				return false;
			}
			return true;
		}

		private static async void SwitchMode(AdvancedTablet __instance)
		{
			if (!GameManager.IsMainThread)
				await UniTask.SwitchToMainThread();
			try
			{
				var val = Traverse.Create(__instance).Method("GetCartridge").GetValue();
			}
			catch (Exception e)
			{
				Plugin.LogWarning(e.ToString());
			}
		}
	}

	/// <summary>
	/// A patch to stop IC execution when the Tablet is turned off.
	/// </summary>
	[HarmonyPatch(typeof(AdvancedTablet), nameof(AdvancedTablet.Execute))]
	[HarmonyPatchCategory(PatchCategory.AdvancedTabletDisableIC)]
	public class AdvancedTabletExecutePatch
	{
		public static bool Prefix(AdvancedTablet __instance)
		{
			// Disable IC execution when off
			if (!__instance.OnOff)
				return false;
			return true;
		}
	}

	/// <summary>
	/// A patch to reset IC when the Tablet is turned off and force immediate execution when turned on.
	/// </summary>
	[HarmonyPatch(typeof(Tablet), nameof(AdvancedTablet.OnInteractableUpdated))]
	[HarmonyPatchCategory(PatchCategory.AdvancedTabletWriteableMode)]
	public class TabledOnInteractableUpdatedPatch
	{
		public static void Postfix(AdvancedTablet __instance, Interactable interactable)
		{
			if (__instance is AdvancedTablet instance && interactable.Action == InteractableType.OnOff && instance.Slots.Count > 3)
			{
				// Reset IC when turned off and force execution when turned on
				if (instance.OnOff)
					instance.Execute();
				else
					(instance.ChipSlot.Get() as ProgrammableChip)?.Reset();
			}
		}
	}
}
