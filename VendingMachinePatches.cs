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
		public static double Get(VendingMachine instance)
		{
			return ClassExtension<VendingMachine, VendingMachineSetting>.GetExtension(instance)?.Setting ?? 0;
		}

		public static void Set(VendingMachine instance, double value)
		{
			var extension = ClassExtension<VendingMachine, VendingMachineSetting>.GetOrCreateExtension(instance, (_) => new VendingMachineSetting());
			extension.Setting = value;
		}

		public static bool Has(VendingMachine instance)
		{
			var extension = ClassExtension<VendingMachine, VendingMachineSetting>.GetExtension(instance);
			return extension?.Setting != null;
		}

		public static void Clear(VendingMachine instance)
		{
			var extension = ClassExtension<VendingMachine, VendingMachineSetting>.GetExtension(instance);
			if (extension?.Setting != null)
			{
				extension.Setting = null;
			}
		}
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
				__result = VendingMachineSetting.Has(__instance) ? VendingMachineSetting.Get(__instance) : 0;
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
				VendingMachineSetting.Set(__instance, value);
				//value -= 2;
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
			if (slot >= 2 && slot < __instance.Slots.Count && __instance.Slots[slot].Occupant)
			{
				while (!__instance.Powered || __instance.IsLocked)
					yield return _waitForFrame;
				OnServer.Interact(__instance.InteractLock, 1);
				__instance.CurrentIndex = slot;
				yield return _waitForDelay;
				OnServer.MoveToSlot(__instance.CurrentSlot.Occupant, __instance.ExportSlot);
				OnServer.Interact(__instance.InteractExport, 1);
				__instance.RequestedHash = 0;
				VendingMachineSetting.Clear(__instance);
				OnServer.Interact(__instance.InteractLock, 0);
			}
		}
	}

	[HarmonyPatch(typeof(VendingMachine), nameof(VendingMachine.CurrentIndex), MethodType.Setter)]
	public static class VendingMachineSetCurrentIndexPatch
	{
		public static void Postfix(VendingMachine __instance, int value)
		{
			VendingMachineSetting.Clear(__instance);
		}
	}
}
