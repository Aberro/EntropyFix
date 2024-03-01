using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Util;
using HarmonyLib;
using UnityEngine;

namespace EntropyFix.Patches
{
	//[HarmonyPatch(typeof(Atmosphere), nameof(Atmosphere.StateChange))]
	//[HarmonyPatchCategory(PatchCategory.NoTrails)]
	//public static class AtmosphereStateChangePatch
	//{
	//	public static bool Prefix(Atmosphere __instance)
	//	{
	//		if (__instance.Thing && __instance.Thing.PreventStateChange)
	//			return;
	//		__instance.GasMixture.Liqui
	//	}
	//}

}

/*
public enum GasType : ushort
{
	Undefined = 0,
	Oxygen = 1,
	Nitrogen = 2,
	CarbonDioxide = 4,
	Volatiles = 8,
	Pollutant = 16, // 0x0010
	Water = 32, // 0x0020
	NitrousOxide = 64, // 0x0040
	LiquidNitrogen = 128, // 0x0080
	LiquidOxygen = 256, // 0x0100
	LiquidVolatiles = 512, // 0x0200
	Steam = 1024, // 0x0400
	LiquidCarbonDioxide = 2048, // 0x0800
	LiquidPollutant = 4096, // 0x1000
	LiquidNitrousOxide = 8192, // 0x2000
}
public struct Mole
{
	public float Energy;
	public float Quantity;
	public float MolarMass { get; }
	public float LatentHeatOfVaporization { get; }
	public float SpecificHeat { get; }
	public float HeatCapacity => SpecificHeat * Quantity;
	public Chemistry.GasType Type { get; }
	public float MolarVolume { get; }
	public float Temperature
	{
		get
		{
			if (!HeatCapacity.IsDenormalOrZero())
				return Energy / HeatCapacity;
			MoleHelper.LogMessage("mole HeatCapacity denormal/negative").Forget();
			return 0.0f;
		}
	}
	// This is the volume of this matter when in a liquid state
	public float Volume => MolarVolume * Quantity;

	public Mole(Chemistry.GasType gasType, float quantity = 0.0f, float energy = 0.0f)
	{
		Energy = energy;
		Quantity = quantity;
		if (float.IsNaN(quantity))
			Quantity = 0.0f;
		if (float.IsNaN(energy))
			Energy = 0.0f;
		Type = gasType;
		LatentHeatOfVaporization = PhysicsConstants.GetLatentHeat(gasType);
		SpecificHeat = PhysicsConstants.GetSpecificHeat(gasType);
		MolarMass = PhysicsConstants.GetMolarMass(gasType);
		MolarVolume = PhysicsConstants.GetMolarVolume(gasType);
	}

	public void Add(float quantity, float energy)
	{
		// adds given amount of matter of given kind and given amount of energy into the mole
		throw new NotImplementedException();
	}

	public void Add(float quantity)
	{
		float energy = quantity * SpecificHeat * Chemistry.Temperature.TwentyDegrees;
		Add(quantity, energy);
	}

	public void Add(Mole mole)
	{
		Add(mole.Quantity, mole.Energy);
	}

	public Mole Remove(float removedMoles)
	{
		// Removes given amount of matter from the mole and proportional amount of energy and returns it as a new Mole
		throw new NotImplementedException();
	}
	public void Set(Mole newMole)
	{
		Set(newMole.Quantity, newMole.Energy);
	}
	public void Set(float quantity, float energy)
	{
		if (quantity.IsDenormal() || quantity < 0.0 || energy.IsDenormal() || energy < 0.0)
		{
			Quantity = 0.0f;
			Energy = 0.0f;
		}
		else
		{
			Quantity = quantity;
			Energy = energy;
		}
	}
	public void Set(float quantity) => Set(quantity, 0.0f);
	public void Scale(float scaleFactor)
	{
		if (scaleFactor.IsDenormal())
			scaleFactor = 0.0f;
		if (float.IsNaN(scaleFactor))
			return;
		Quantity *= scaleFactor;
		Energy *= scaleFactor;
	}
	public void Split(float divideBy)
	{
		if (divideBy.IsDenormalOrZero() || float.IsNaN(divideBy))
			return;
		Quantity /= divideBy;
		Energy /= divideBy;
	}

	public float EvaporationTemperatureClamped(float pressure)
	{
		// returns temperature at which this mole will evaporate at given pressure
		throw new NotImplementedException();
	}

	public float EvaporationPressureClamped(float temperature)
	{
		// returns pressure at which this mole will evaporate at given temperature
		throw new NotImplementedException();
	}
}
public struct GasMixture
{
	public Mole Oxygen;
	public Mole Nitrogen;
	public Mole CarbonDioxide;
	public Mole Volatiles;
	public Mole Pollutant;
	public Mole Water;
	public Mole NitrousOxide;
	public Mole LiquidNitrogen;
	public Mole LiquidOxygen;
	public Mole LiquidVolatiles;
	public Mole Steam;
	public Mole LiquidCarbonDioxide;
	public Mole LiquidPollutant;
	public Mole LiquidNitrousOxide;

	public float TotalEnergy
	{
		set
		{
			if (HeatCapacity.IsDenormalToNegative() || float.IsNaN(HeatCapacity))
				return;
			if (value.IsDenormalOrNegative())
				value = 0.0f;
			Oxygen.Energy = value * (Oxygen.HeatCapacity / HeatCapacity);
			CarbonDioxide.Energy = value * (CarbonDioxide.HeatCapacity / HeatCapacity);
			Nitrogen.Energy = value * (Nitrogen.HeatCapacity / HeatCapacity);
			Volatiles.Energy = value * (Volatiles.HeatCapacity / HeatCapacity);
			Pollutant.Energy = value * (Pollutant.HeatCapacity / HeatCapacity);
			Water.Energy = value * (Water.HeatCapacity / HeatCapacity);
			NitrousOxide.Energy = value * (NitrousOxide.HeatCapacity / HeatCapacity);
			LiquidNitrogen.Energy = value * (LiquidNitrogen.HeatCapacity / HeatCapacity);
			LiquidOxygen.Energy = value * (LiquidOxygen.HeatCapacity / HeatCapacity);
			LiquidVolatiles.Energy = value * (LiquidVolatiles.HeatCapacity / HeatCapacity);
			Steam.Energy = value * (Steam.HeatCapacity / HeatCapacity);
			LiquidCarbonDioxide.Energy = value * (LiquidCarbonDioxide.HeatCapacity / HeatCapacity);
			LiquidPollutant.Energy = value * (LiquidPollutant.HeatCapacity / HeatCapacity);
			LiquidNitrousOxide.Energy = value * (LiquidNitrousOxide.HeatCapacity / HeatCapacity);
		}
		get => Oxygen.Energy + CarbonDioxide.Energy + Nitrogen.Energy + Volatiles.Energy + Pollutant.Energy + Water.Energy +
		       NitrousOxide.Energy + LiquidNitrogen.Energy + LiquidOxygen.Energy + LiquidVolatiles.Energy + Steam.Energy +
		       LiquidCarbonDioxide.Energy + LiquidPollutant.Energy + LiquidNitrousOxide.Energy;
	}
	public float HeatCapacity => Oxygen.HeatCapacity + Nitrogen.HeatCapacity + CarbonDioxide.HeatCapacity + Volatiles.HeatCapacity +
	                             Pollutant.HeatCapacity + Water.HeatCapacity + NitrousOxide.HeatCapacity + LiquidNitrogen.HeatCapacity +
	                             LiquidOxygen.HeatCapacity + LiquidVolatiles.HeatCapacity + Steam.HeatCapacity +
	                             LiquidCarbonDioxide.HeatCapacity + LiquidPollutant.HeatCapacity + LiquidNitrousOxide.HeatCapacity;
	public float TotalMolesGassesAndLiquids => TotalMoles(AtmosphereHelper.MatterState.All);
	public float TotalMolesLiquids => TotalMoles(AtmosphereHelper.MatterState.Liquid);
	public float TotalMolesGasses => TotalMoles(AtmosphereHelper.MatterState.Gas);
	public float Temperature => HeatCapacity.IsDenormalToNegative() ? 0.0f : TotalEnergy / HeatCapacity;
	public float VolumeLiquids => Water.Volume + LiquidNitrogen.Volume + LiquidOxygen.Volume + LiquidVolatiles.Volume + LiquidCarbonDioxide.Volume + LiquidPollutant.Volume + LiquidNitrousOxide.Volume;

	public float TotalMoles(AtmosphereHelper.MatterState matterState)
	{
		float num = 0.0f;
		switch (matterState)
		{
			case AtmosphereHelper.MatterState.Liquid:
				num = num + Water.Quantity + LiquidNitrogen.Quantity + LiquidOxygen.Quantity + LiquidVolatiles.Quantity +
				      LiquidCarbonDioxide.Quantity + LiquidPollutant.Quantity + LiquidNitrousOxide.Quantity;
				break;
			case AtmosphereHelper.MatterState.Gas:
				num = num + Oxygen.Quantity + Nitrogen.Quantity + CarbonDioxide.Quantity + Volatiles.Quantity + Pollutant.Quantity +
				      NitrousOxide.Quantity + Steam.Quantity;
				break;
			case AtmosphereHelper.MatterState.All:
				num = num + TotalMoles(AtmosphereHelper.MatterState.Liquid) + TotalMoles(AtmosphereHelper.MatterState.Gas);
				break;
		}
		return num;
	}
	public void Add(GasMixture newGasMix, AtmosphereHelper.MatterState matterState = AtmosphereHelper.MatterState.All)
	{
		// adds matter that is currently in specified matter state from given gas mixture.
		throw new NotImplementedException();
	}
	public void Add(Mole mole)
	{
		// adds given matter into the gas mixture
		throw new NotImplementedException();
	}
	public void AddEnergy(float energy)
	{
		if (TotalMolesGassesAndLiquids <= 1.0 / 1000.0 || energy.IsDenormalOrZero() || float.IsNaN(energy))
			return;
		TotalEnergy += energy;
	}
	public GasMixture Remove(float totalMolesRemoved, AtmosphereHelper.MatterState stateToRemove)
	{
		// removes given amount of matter that is currently in specified state from the gas mixture and returns it as a new gas mixture
		throw new NotImplementedException();
	}
	public void Remove(GasMixture gasMixture)
	{
		Oxygen.Remove(gasMixture.Oxygen.Quantity);
		Nitrogen.Remove(gasMixture.Nitrogen.Quantity);
		CarbonDioxide.Remove(gasMixture.CarbonDioxide.Quantity);
		Volatiles.Remove(gasMixture.Volatiles.Quantity);
		Pollutant.Remove(gasMixture.Pollutant.Quantity);
		Water.Remove(gasMixture.Water.Quantity);
		NitrousOxide.Remove(gasMixture.NitrousOxide.Quantity);
		LiquidNitrogen.Remove(gasMixture.LiquidNitrogen.Quantity);
		LiquidOxygen.Remove(gasMixture.LiquidOxygen.Quantity);
		LiquidVolatiles.Remove(gasMixture.LiquidVolatiles.Quantity);
		Steam.Remove(gasMixture.Steam.Quantity);
		LiquidCarbonDioxide.Remove(gasMixture.LiquidCarbonDioxide.Quantity);
		LiquidPollutant.Remove(gasMixture.LiquidPollutant.Quantity);
		LiquidNitrousOxide.Remove(gasMixture.LiquidNitrousOxide.Quantity);
	}
	public Mole Remove(Chemistry.GasType gasType, float quantity)
	{
		// removes specified matter of given kind in given quantity from the gas mixture and returns it as a new Mole
		throw new NotImplementedException();
	}
	public Mole RemoveAll(Chemistry.GasType gasType)
	{
		// removes all matter of given kind from the gas mixture and returns it as a new Mole
		throw new NotImplementedException();
	}
	public GasMixture RemoveAndReturn(
		GasMixture gasMixture,
		AtmosphereHelper.MatterState matterState)
	{
		// removes the amounts, specified by gasMixture argument, of matter given of each kind that is currently in specified state from the current gas mixture and returns it as a new gas mixture.
		throw new NotImplementedException();
	}
	public float RemoveEnergy(float energy)
	{
		if (TotalMolesGassesAndLiquids <= 1.0 / 1000.0 || energy <= 0.0 || float.IsNaN(energy))
			return 0.0f;
		float num = Mathf.Min(TotalEnergy, energy);
		TotalEnergy -= num;
		return num;
	}
	public void Scale(float ratio, AtmosphereHelper.MatterState matterState = AtmosphereHelper.MatterState.All)
	{
		// Reduces the amount of energy and matter in a given state by a given ratio.
		throw new NotImplementedException();
	}

	public Mole GetMoleValue(Chemistry.GasType gasType)
	{
		// returns the quantity of given matter kind.
		throw new NotImplementedException(); // should be a switch here to access one of the Mole properties.
	}
}

public class Atmosphere
{
	public float Volume { get; set; }
	public GasMixture GasMixture { get; }
	public float LiquidPressureOffset
	{
		get
		{
			if (GasMixture.TotalMolesLiquids < 9.99999974737875E-06)
				return 0.0f;
			float num = Mathf.Clamp01(GasMixture.VolumeLiquids / Volume);
			return num < 1.0 ? (float)(10.0 / (1.0 - num) - 10.0) : float.PositiveInfinity;
		}
	}
	public float PressureGassesAndLiquids
	{
		get
		{
			float gassesAndLiquids = GasMixture.TotalMolesGasses * 8.3144f * GasMixture.Temperature / GetGasVolume();
			if (LiquidPressureOffset > (double)gassesAndLiquids)
			{
				return LiquidPressureOffset;
			}
			return gassesAndLiquids;
		}
	}
	public float PressureGassesAndLiquidsInPa => PressureGassesAndLiquids * RocketMath.Thousand;
	public float PressureGasses => GasMixture.TotalMolesGasses * 8.3144f * GasMixture.Temperature / GetGasVolume();
	public void Add(GasMixture gasMixture) => GasMixture.Add(gasMixture);
	public void Add(Mole mole) => GasMixture.Add(mole);

	public void Add(GasMixture gasMixture, float scale)
	{
		GasMixture newGasMix = new GasMixture(gasMixture);
		newGasMix.Scale(scale);
		GasMixture.Add(newGasMix);
	}
	public GasMixture Remove(float transferMoles, AtmosphereHelper.MatterState matterStateToRemove)
	{
		return GasMixture.Remove(transferMoles, matterStateToRemove);
	}

	public GasMixture Remove(GasMixture gasMixture, AtmosphereHelper.MatterState matterState)
	{
		return GasMixture.RemoveAndReturn(gasMixture, matterState);
	}
	public float Pressure(AtmosphereHelper.MatterState matterState)
	{
		switch (matterState)
		{
			case AtmosphereHelper.MatterState.Liquid:
				return LiquidPressureOffset;
			case AtmosphereHelper.MatterState.Gas:
				return PressureGasses;
			case AtmosphereHelper.MatterState.All:
				return PressureGassesAndLiquids;
			default:
				return PressureGassesAndLiquids;
		}
	}
	public float GetGasVolume()
	{
		return Mathf.Max(Volume - GasMixture.VolumeLiquids, float.Epsilon);
	}

	public void StateChange()
	{
		// Iterate through each type of gas and liquid to check if state change occurs
		foreach (Chemistry.GasType gasType in Enum.GetValues(typeof(Chemistry.GasType)))
		{
			// Skip undefined gas type
			if (gasType == Chemistry.GasType.Undefined)
				continue;

			// Check if the gas type is a liquid
			bool isLiquid = gasType.HasFlag(Chemistry.GasType.LiquidNitrogen) ||
							gasType.HasFlag(Chemistry.GasType.LiquidOxygen) ||
							gasType.HasFlag(Chemistry.GasType.LiquidVolatiles) ||
							gasType.HasFlag(Chemistry.GasType.LiquidCarbonDioxide) ||
							gasType.HasFlag(Chemistry.GasType.LiquidPollutant) ||
							gasType.HasFlag(Chemistry.GasType.LiquidNitrousOxide);

			// Get the corresponding mole
			Mole mole = GasMixture.GetMoleValue(gasType);

			// Check for evaporation (liquid to gas)
			if (isLiquid && mole.Temperature > mole.EvaporationTemperatureClamped(PressureGassesAndLiquids))
			{
				// Calculate the amount of moles to evaporate
				float evaporatedMoles = mole.Quantity * (mole.Temperature - mole.EvaporationTemperatureClamped(PressureGassesAndLiquids)) / mole.Temperature;
				float energyTransferred = evaporatedMoles * mole.LatentHeatOfVaporization;

				// Create a new mole of the corresponding gas type
				Chemistry.GasType correspondingGasType = (Chemistry.GasType)((int)gasType >> 7); // Shift right to get the corresponding gas type
				Mole newMole = new Mole(correspondingGasType, evaporatedMoles, energyTransferred);

				// Add the new mole to the gas mixture and remove the evaporated moles from the liquid
				GasMixture.Add(newMole);
				GasMixture.Remove(gasType, evaporatedMoles);
			}
			// Check for condensation (gas to liquid)
			else if (!isLiquid && mole.Temperature < mole.EvaporationTemperatureClamped(PressureGassesAndLiquids))
			{
				// Calculate the amount of moles to condense
				float condensedMoles = mole.Quantity * (mole.EvaporationTemperatureClamped(PressureGassesAndLiquids) - mole.Temperature) / mole.Temperature;
				float energyTransferred = condensedMoles * mole.LatentHeatOfVaporization;

				// Create a new mole of the corresponding liquid type
				Chemistry.GasType correspondingLiquidType = (Chemistry.GasType)((int)gasType << 7); // Shift left to get the corresponding liquid type
				Mole newMole = new Mole(correspondingLiquidType, condensedMoles, energyTransferred);

				// Add the new mole to the gas mixture and remove the condensed moles from the gas
				GasMixture.Add(newMole);
				GasMixture.Remove(gasType, condensedMoles);
			}
		}
	}
}
*/