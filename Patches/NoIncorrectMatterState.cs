
using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects;
using HarmonyLib;
using Networks;
using UnityEngine;
using EntropyFix;
using System.Linq;
using Assets.Scripts.Util;

namespace EntropyFix.Patches
{
	public class IceFormation
	{
		public GasMixture FrozenMatter = new GasMixture(0);
	}
	[HarmonyPatch(typeof(AtmosphericsNetwork), "EvaluateIncorrectMatterState")]
	[HarmonyPatchCategory(PatchCategory.NoIncorrectMatterState)]
	public static class AtmosphericsNetworkEvaluateIncorrectMatterState
	{
		public static bool Prefix(AtmosphericsNetwork __instance)
		{
			// This only selects moles that are below the freezing point, not the amount.
			GasMixture gasMixture = __instance.Atmosphere.GasMixture.CheckForFreezing(__instance.Atmosphere.PressureGasses);
			if (gasMixture.TotalMolesGassesAndLiquids <= 0)
				return false;
			// Therefore, we need to calculate the amount based on energy deficit.
			// Start with calculating the minimal energy - at freezing point.
			double minimalEnergy = gasMixture.Moles().Sum(x => x.HeatCapacity * x.FreezingTemperature);
			double maxEnthalpyOfFusion = gasMixture.Moles().Sum(x => x.Quantity * (x.LatentHeatOfVaporization / 8));
			// Ensure that we don't freeze more than we can.
			double energyDeficit = minimalEnergy - gasMixture.TotalEnergy;
			var totalMoles = gasMixture.TotalMolesGassesAndLiquids;
			if (energyDeficit > 0)
			{
				var iceFormation = __instance.GetOrCreateExtension(_ => new IceFormation());
				var frozen = new GasMixture(0);
				var totalEnthalpy = 0d;
				foreach (var mole in gasMixture.Moles())
				{
					if (mole.Quantity.IsDenormalToNegative())
						continue;
					// We'll use very simplified enthalpy calculation, assuming that enthalpy of fusion is always 8 times lower the enthalpy of vaporization.
					var enthalpyOfFusion = mole.LatentHeatOfVaporization / 8;

					var fraction = mole.Quantity / totalMoles;
					var energyToFreeze = fraction * energyDeficit;

					var molesToFreeze = Math.Min(mole.Quantity, energyToFreeze / enthalpyOfFusion);
					var freezingRatio = 0.1;
					if (molesToFreeze < 0.1)
						freezingRatio = 0.5;
					if (molesToFreeze > 0.00001)
					{
						var molesFrozen = (float) (molesToFreeze * freezingRatio);
						var frozenMole = gasMixture.Remove(mole.Type, molesFrozen);
						if (frozenMole.Quantity > 0)
						{
							frozen.Add(frozenMole);
							totalEnthalpy += molesFrozen * enthalpyOfFusion;
						}
					}
				}
				if (frozen.TotalMolesGassesAndLiquids > 0)
				{
					frozen = __instance.Atmosphere.Remove(frozen, AtmosphereHelper.MatterState.All);
					__instance.Atmosphere.GasMixture.TotalEnergy += (float) totalEnthalpy;
					iceFormation.FrozenMatter.Add(frozen);

					// Measure volume of frozen matter, for simplicity we assume that it's equal to liquid volume
					var volume = frozen.Moles().Sum(x => x.Type.MolarVolumeLiquid() * x.Quantity);
					__instance.Atmosphere.Volume = Math.Max(0, __instance.Atmosphere.Volume - volume);
				}
			}
			else
			{
				// We have energy surplus, try to thaw ice formation
				var iceFormation = __instance.GetOrCreateExtension(_ => new IceFormation());
				var frozen = iceFormation.FrozenMatter;
				if(frozen.TotalMolesGassesAndLiquids <= 0)
					return false;
				var totalEnthalphy = 0d;
				var frozenTotalMoles = frozen.TotalMolesGassesAndLiquids;
				var thawed = new GasMixture(0);
				foreach (var mole in frozen.Moles())
				{
					var enthalpyOfFusion = mole.LatentHeatOfVaporization / 8;

					var fraction = mole.Quantity / frozenTotalMoles;
					var energyToThaw = fraction * -energyDeficit;
					var molesToThaw = Math.Min(mole.Quantity, energyToThaw / enthalpyOfFusion);
					var thawingRatio = 0.1;
					if(molesToThaw < 0.1)
						thawingRatio = 0.5;
					if (molesToThaw > 0.00001)
					{
						var molesThawed = (float)(molesToThaw * thawingRatio);
						var thawedMole = frozen.Remove(mole.Type, molesThawed);
						if (thawedMole.Quantity > 0)
						{
							thawed.Add(thawedMole);
							totalEnthalphy += molesThawed * enthalpyOfFusion;
						}
					}
				}
				if (thawed.TotalMolesGassesAndLiquids > 0)
				{
					__instance.Atmosphere.Add(thawed);
					__instance.Atmosphere.GasMixture.TotalEnergy -= (float) totalEnthalphy;
					iceFormation.FrozenMatter = frozen;

					var volume = thawed.Moles().Sum(x => x.Type.MolarVolumeLiquid() * x.Quantity);
					var traverse = Traverse.Create(__instance);
					var maxVolume = traverse.Method("GetNetworkVolume").GetValue<float>();
					__instance.Atmosphere.Volume = Math.Min(maxVolume, __instance.Atmosphere.Volume + volume);
					if (frozen.TotalMolesGassesAndLiquids <= 0)
						__instance.Atmosphere.Volume = maxVolume;
				}
			}
			return false;
		}
	}
}
