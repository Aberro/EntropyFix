using System.Collections;
using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Util;
using HarmonyLib;
using UnityEngine;

namespace EntropyFix
{
	public class VendingMachineSetting
	{
		public double? Setting { get; set; }
	}
	[HarmonyPatch(typeof(VendingMachine), nameof(VendingMachine.CanLogicRead), typeof(LogicType))]
	public static class VendingMachineCanLogicReadPatch
	{
		public static void Postfix(VendingMachine __instance, LogicType logicType, ref bool __result)
		{
			if (logicType == LogicType.Setting)
				__result = true;
		}
	}
	[HarmonyPatch(typeof(VendingMachine), nameof(VendingMachine.CanLogicWrite), typeof(LogicType))]
	public static class VendingMachineCanLogicWritePatch
	{
		public static void Postfix(VendingMachine __instance, LogicType logicType, ref bool __result)
		{
			if (logicType == LogicType.Setting)
				__result = true;
		}
	}
	[HarmonyPatch(typeof(VendingMachine), nameof(VendingMachine.GetLogicValue))]
	public static class VendingMachineGetLogicValuePatch
	{
		public static void Postfix(VendingMachine __instance, LogicType logicType, ref double __result)
		{
			if (logicType == LogicType.Setting)
			{
				__result = __instance.GetExtension<VendingMachine, VendingMachineSetting>()?.Setting ?? 0;
			}
		}
	}

	[HarmonyPatch(typeof(VendingMachine), nameof(VendingMachine.SetLogicValue))]
	public static class VendingMachineSetLogicValuePatch
	{
		private static readonly WaitForSeconds _waitForDelay = new WaitForSeconds(0.5f);
		private static readonly WaitForEndOfFrame _waitForFrame = new WaitForEndOfFrame();
		public static void Postfix(VendingMachine __instance, LogicType logicType, double value)
		{
			if (logicType == LogicType.Setting)
			{
				__instance.GetOrCreateExtension(_ => new VendingMachineSetting()).Setting = value;
				if (value >= 2 && value < __instance.Slots.Count)
				{
					if (GameManager.IsThread)
						UnityMainThreadDispatcher.Instance().Enqueue(WaitSetRequestFromSlot(__instance, (int)value));
					else
						__instance.StartCoroutine(WaitSetRequestFromSlot(__instance, (int)value));
				}
			}
		}

		private static IEnumerator WaitSetRequestFromSlot(VendingMachine __instance, int slot)
		{
			if (slot >= 2 && slot < __instance.Slots.Count && __instance.Slots[slot].Get())
			{
				while (!__instance.Powered || __instance.IsLocked)
					yield return _waitForFrame;
				OnServer.Interact(__instance.InteractLock, 1);
				__instance.CurrentIndex = slot;
				yield return _waitForDelay;
				OnServer.MoveToSlot(__instance.CurrentSlot.Get(), __instance.ExportSlot);
				OnServer.Interact(__instance.InteractExport, 1);
				__instance.RequestedHash = 0;
				__instance.ClearExtension<VendingMachine, VendingMachineSetting>();
				OnServer.Interact(__instance.InteractLock, 0);
			}
		}
	}

	[HarmonyPatch(typeof(VendingMachine), nameof(VendingMachine.CurrentIndex), MethodType.Setter)]
	public static class VendingMachineSetCurrentIndexPatch
	{
		public static void Postfix(VendingMachine __instance, int value)
		{
			__instance.ClearExtension<VendingMachine, VendingMachineSetting>();
		}
	}
}
