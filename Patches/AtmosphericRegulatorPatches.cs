using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using JetBrains.Annotations;
using Objects.Pipes;

namespace EntropyFix.Patches
{
	public class DeviceAtmosphericsRegulator
	{
		public float PowerLimit { get; set; }
		public float VolumeLimit { get; set; }
	}
    [HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.CanLogicWrite))]
    [HarmonyPatchCategory(PatchCategory.AtmosphericRegulatorPatches)]
	public static class DeviceAtmosphericsPowerRegulatorPatch
	{
		[UsedImplicitly]
		public static bool Prefix(DeviceAtmospherics __instance, LogicType logicType, ref bool __result)
		{
			if (logicType is not LogicType.Power and not LogicType.Volume 
			    || __instance is not VolumePump and not PressureRegulator and not ActiveVent and not PoweredVent and not AdvancedFurnace and not AirConditioner)
				return true;
			__result = true;
			return false;
		}

	}

	[HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.SetLogicValue))]
	[HarmonyPatchCategory(PatchCategory.AtmosphericRegulatorPatches)]
	public static class DeviceAtmosphericsSetLogicValuePatch
	{
		[UsedImplicitly]
		public static bool Prefix(DeviceAtmospherics __instance, LogicType logicType, double value)
		{
			if (logicType is not LogicType.Power and not LogicType.Volume
			    || __instance is not VolumePump and not PressureRegulator and not ActiveVent and not PoweredVent and not AdvancedFurnace and not AirConditioner)
				return true;
			if(logicType == LogicType.Power)
				__instance.GetOrCreateExtension(_ => new DeviceAtmosphericsRegulator()).PowerLimit = (float)value;
			else if (logicType == LogicType.Volume)
				__instance.GetOrCreateExtension(_ => new DeviceAtmosphericsRegulator()).VolumeLimit = (float)value;
			return false;
		}
	}
}
