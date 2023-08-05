using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;

namespace EntropyFix.Patches
{

	[HarmonyPatch(typeof(HeatExchangerDeprecated), MethodType.Constructor)]
	[HarmonyPatchCategory(PatchCategory.HeatExchangerPatch)]
	public static class HeatExchangerCtorPatch
	{
		public static void Postfix(HeatExchangerDeprecated __instance)
		{
			var area = Traverse.Create(__instance).Field<float>(nameof(HeatExchangerDeprecated.HeatExchangeArea));
			area.Value = 250;
			var volume = Traverse.Create(__instance).Field<float>(nameof(HeatExchangerDeprecated.Volume));
			volume.Value = 100;
		}
	}

	/// <summary>
	/// A patch for heat exchanger to increase it's efficiency.
	/// </summary>
	[HarmonyPatch(typeof(HeatExchangerDeprecated), nameof(HeatExchangerDeprecated.OnAtmosphericTick))]
	[HarmonyPatchCategory(PatchCategory.HeatExchangerPatch)]
	public static class HeatExchangerOnAtmosphericTickPatch
	{
		public static bool Prefix(HeatExchangerDeprecated __instance)
		{
			__instance.InternalAtmosphere2.Volume = __instance.Volume;
			__instance.InternalAtmosphere3.Volume = __instance.Volume;
			if (__instance.InputNetwork == null || __instance.InputNetwork2 == null || __instance.OutputNetwork == null || __instance.OutputNetwork2 == null ||
				__instance.InputNetwork.Atmosphere == null || __instance.InputNetwork2.Atmosphere == null)
			{
				return false;
			}
			bool lowInputPressure1 = __instance.InputNetwork.Atmosphere.PressureGassesAndLiquids < __instance.InternalAtmosphere2.PressureGassesAndLiquids + 5;
			if (lowInputPressure1 && __instance.InputNetwork.NetworkContentType == Pipe.ContentType.Liquid)
				lowInputPressure1 = __instance.InputNetwork.Atmosphere.LiquidVolumeRatio < __instance.InternalAtmosphere2.LiquidVolumeRatio + 1.0 / 2000.0;
			bool lowInputPressure2 = __instance.InputNetwork2.Atmosphere.PressureGassesAndLiquids < __instance.InternalAtmosphere3.PressureGassesAndLiquids + 5;
			if (lowInputPressure2 && __instance.InputNetwork2.NetworkContentType == Pipe.ContentType.Liquid)
				lowInputPressure2 = __instance.InputNetwork2.Atmosphere.LiquidVolumeRatio < __instance.InternalAtmosphere3.LiquidVolumeRatio + 1.0 / 2000.0;

			AtmosphereHelper.Mix(__instance.InternalAtmosphere2, __instance.InputNetwork.Atmosphere, AtmosphereHelper.MatterState.All);
			if (!lowInputPressure1)
				AtmosphereHelper.MoveToEqualize(__instance.InputNetwork.Atmosphere, __instance.InternalAtmosphere2, float.MaxValue, AtmosphereHelper.MatterState.All);

			AtmosphereHelper.Mix(__instance.InternalAtmosphere3, __instance.InputNetwork2.Atmosphere, AtmosphereHelper.MatterState.All);
			if (!lowInputPressure2)
				AtmosphereHelper.MoveToEqualize(__instance.InputNetwork2.Atmosphere, __instance.InternalAtmosphere3, float.MaxValue, AtmosphereHelper.MatterState.All);

			var heatExchangeRatio = Traverse.Create(__instance).Method("HeatExchangeRatio").GetValue<float>();
			var convectionHeat = (float)((double)AtmosphereHelper.GetConvectionHeat(
				__instance.InternalAtmosphere2,
				__instance.InternalAtmosphere3,
				__instance.HeatExchangeArea * heatExchangeRatio) * AtmosphericsManager.Instance.TickSpeedMs * 1.0);
			__instance.InternalAtmosphere2.GasMixture.TransferEnergyTo(ref __instance.InternalAtmosphere3.GasMixture, convectionHeat);

			//convectionHeat = (float) ((double) AtmosphereHelper.GetConvectionHeat(
			//	__instance.InternalAtmosphere3,
			//	__instance.InternalAtmosphere2,
			//	__instance.HeatExchangeArea * heatExchangeRatio) * AtmosphericsManager.Instance.TickSpeedMs * 1.0);
			//__instance.InternalAtmosphere3.GasMixture.TransferEnergyTo(ref __instance.InternalAtmosphere2.GasMixture, convectionHeat);
			//EntropyFix.Log("ConvectionHeat heat: " + convectionHeat);
			bool lowOutputPressure1 = __instance.InternalAtmosphere2.PressureGassesAndLiquids < __instance.OutputNetwork.Atmosphere.PressureGassesAndLiquids + 5;
			if (lowOutputPressure1 && __instance.OutputNetwork.NetworkContentType == Pipe.ContentType.Liquid)
				lowOutputPressure1 = __instance.InternalAtmosphere2.LiquidVolumeRatio < __instance.OutputNetwork.Atmosphere.LiquidVolumeRatio + 1.0 / 2000.0;
			bool lowOutputPressure2 = __instance.InternalAtmosphere3.PressureGassesAndLiquids < __instance.OutputNetwork2.Atmosphere.PressureGassesAndLiquids + 5;
			if (lowOutputPressure2 && __instance.OutputNetwork2.NetworkContentType == Pipe.ContentType.Liquid)
				lowOutputPressure2 = __instance.InternalAtmosphere3.LiquidVolumeRatio < __instance.OutputNetwork2.Atmosphere.LiquidVolumeRatio + 1.0 / 2000.0;

			AtmosphereHelper.Mix(__instance.InternalAtmosphere2, __instance.OutputNetwork.Atmosphere, AtmosphereHelper.MatterState.All);
			if (!lowOutputPressure1)
				AtmosphereHelper.MoveToEqualize(__instance.InternalAtmosphere2, __instance.OutputNetwork.Atmosphere, float.MaxValue, AtmosphereHelper.MatterState.All);

			AtmosphereHelper.Mix(__instance.InternalAtmosphere3, __instance.OutputNetwork2.Atmosphere, AtmosphereHelper.MatterState.All);
			if (!lowOutputPressure2)
				AtmosphereHelper.MoveToEqualize(__instance.InternalAtmosphere3, __instance.OutputNetwork2.Atmosphere, float.MaxValue, AtmosphereHelper.MatterState.All);
			return false;
		}
	}
}
