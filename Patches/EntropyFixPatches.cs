using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Objects;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using static Assets.Scripts.Atmospherics.Chemistry;

namespace EntropyFix.Patches
{
	[HarmonyPatch(typeof(AtmosphereHelper), nameof(AtmosphereHelper.CalculateThingEntropy))]
	[HarmonyPatchCategory(PatchCategory.EntropyFix)]
	public class AtmosphereCalculateThingEntropyPatch
	{

		[UsedImplicitly]
		public static bool Prefix(Thing thing, Atmosphere worldAtmosphere, Atmosphere internalAtmosphere, float scale, ref float __result)
		{
			Atmosphere atmosphere = worldAtmosphere?.AtmosphericsController.SampleGlobalAtmosphere(thing.WorldGrid);
            Atmosphere globalAtmosphere = AtmosphericsController.GlobalAtmosphere(thing.GridPosition);
			if (globalAtmosphere.Room != atmosphere?.Room)
			{
				__result = 0f; // Prevent entropy exchange with world when in a room.
				return false;
			}

			if (internalAtmosphere.Temperature <= globalAtmosphere.Temperature)
			{
				__result = 0f;
				return false;
			}
			var thingTemperature = globalAtmosphere.Temperature < 1.0 ? 50f : globalAtmosphere.Temperature;
			var num1 = thingTemperature;
			if (worldAtmosphere != null)
			{
				var temperature = worldAtmosphere.Temperature;
				if (thingTemperature < temperature)
				{
					var num2 = Limits.PressureMinimumSafe *
						worldAtmosphere.Volume / (8.31439971923828 * Temperature.ZeroDegrees);
					var t = Mathf.Clamp01(worldAtmosphere.TotalMoles / (float)num2);
					num1 = Mathf.Lerp(thingTemperature, temperature, t);
				}
			}
			float num3;
			lock (AtmosphericsManager.EntropyCurve)
				num3 = AtmosphericsManager.EntropyCurve.Evaluate(internalAtmosphere.Temperature - num1);
			__result = Mathf.Clamp(thing.RadiationFactor * thing.SurfaceArea * internalAtmosphere.RatioOneAtmosphereClamped() * scale * num3,
				0.0f,
				internalAtmosphere.GasMixture.TotalEnergy / 2f);
			return false;
		}
	}
}
