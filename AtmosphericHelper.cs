using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Effects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Util;
using UnityEngine;
using static Assets.Scripts.Atmospherics.AtmosphereHelper;
using static Assets.Scripts.CGARenderData;

namespace EntropyFix
{
    public static class AtmosphericHelper
    {
        private static float EqualizableMatter(Atmosphere inputAtmos, Atmosphere outputAtmos)
        {
            float pressureChange = inputAtmos.PressureGassesAndLiquidsInPa - outputAtmos.PressureGassesAndLiquidsInPa;
            if (pressureChange <= 0.0)
                return 0;
            float num2 = 8.3144f * inputAtmos.Temperature / inputAtmos.GetVolume(AtmosphereHelper.MatterState.Gas) + 8.3144f * inputAtmos.Temperature / outputAtmos.GetVolume(AtmosphereHelper.MatterState.Gas);
            return pressureChange / num2;
		}

        private static float PressureGassesAndLiquids(Atmosphere atmosphere)
        {
            float gassesAndLiquids = atmosphere.GasMixture.TotalMolesGasses * 8.3144f * atmosphere.GasMixture.Temperature / atmosphere.GetGasVolume();
            if (atmosphere.LiquidPressureOffset > gassesAndLiquids)
            {
                switch (atmosphere.Mode)
                {
                    case AtmosphereHelper.AtmosphereMode.Network:
                    case AtmosphereHelper.AtmosphereMode.Thing:
                    case AtmosphereHelper.AtmosphereMode.None:
                        return atmosphere.LiquidPressureOffset;
                }
            }
            return gassesAndLiquids;
        }

        public static bool Debug = false;
        /// <summary>
		/// Passively or actively pumps volume from input to output, with pressure increase limited by the desiredPressureChange.
		/// </summary>
		/// <param name="inputAtmosphere"></param>
		/// <param name="outputAtmosphere"></param>
		/// <param name="pumpVolume"></param>
		/// <param name="inputPressureLimit">The minimum pressure of input atmosphere</param>
		/// <param name="outputPressureLimit">The maximum pressure of output atmosphere</param>
		/// <param name="powerRating">The maximum power for pumping action.</param>
		/// <param name="movedContent">The matter state to move.</param>
		/// <returns>Power consumption.</returns>
		public static float PumpVolume(
	        Atmosphere inputAtmosphere,
	        Atmosphere outputAtmosphere,
	        float pumpVolume,
	        float inputPressureLimit,
	        float outputPressureLimit,
	        float powerRating,
	        MatterState movedContent)
		{
			if (Debug)
			{
				Plugin.Log($"PumpVolume\n" +
				           $"input.Pressure: {inputAtmosphere.PressureGassesAndLiquidsInPa}\n" +
				           $"input.Temperature: {inputAtmosphere.Temperature}\n" +
				           $"input.Volume: {inputAtmosphere.Volume}\n" +
				           $"input.Amount: {inputAtmosphere.TotalMoles}\n" +
						   $"output.Pressure: {outputAtmosphere.PressureGassesAndLiquidsInPa}\n" +
				           $"output.Temperature: {outputAtmosphere.Temperature}\n" +
				           $"output.Volume: {outputAtmosphere.Volume}\n" +
				           $"output.Amount: {outputAtmosphere.TotalMoles}\n" +
				           $"pumpVolume: {pumpVolume}\n" +
				           $"inputPressureLimit: {inputPressureLimit}\n" +
				           $"outputPressureLimit: {outputPressureLimit}\n" +
				           $"powerRating: {powerRating}\n" +
				           $"movedContent: {movedContent}");
			}
			//var inputPressureLimit = regulationType == RegulatorType.Upstream ? 0 : pressureLimit;
			//var outputPressureLimit = regulationType == RegulatorType.Downstream ? float.MaxValue : pressureLimit;
			var epsilon = 0.0000001f;
			if(inputAtmosphere.PressureGassesAndLiquidsInPa - epsilon <= inputPressureLimit)
				return 0;
			if(outputAtmosphere.PressureGassesAndLiquidsInPa + epsilon >= outputPressureLimit)
				return 0;
			//if (movedContent == MatterState.Liquid)
			//{
			//	throw new NotSupportedException("PumpVolume does not support liquid pump! Contact EntropyFix mod author.");
			//}
			var pumpingVolume = CalculatePumpVolume(inputAtmosphere, outputAtmosphere, pumpVolume, powerRating, inputPressureLimit, outputPressureLimit, movedContent);
			if (pumpingVolume <= 0.0001)
				return 0;

			// Stage 1, intake
			var initialVolume = movedContent switch
			{
				MatterState.Gas => inputAtmosphere.Volume - inputAtmosphere.TotalVolumeLiquids,
				MatterState.Liquid => inputAtmosphere.TotalVolumeLiquids,
				_ => inputAtmosphere.Volume,
			};
			var phase1FinalVolume = initialVolume + pumpingVolume;
			var inputAtmosphereMoles = inputAtmosphere.Moles(movedContent).ToArray();
			var stage1 = ChangeVolume(initialVolume, phase1FinalVolume, movedContent, inputAtmosphereMoles);
			var pumpAtmospherePressure = stage1.Pressure;
			var scoopedFraction = pumpingVolume / phase1FinalVolume;
			var pumpAtmosphereMolesList = new List<Mole>();
			var inputGasMixture = inputAtmosphere.GasMixture;
			foreach (var mole in stage1.Result)
			{
				if (movedContent != MatterState.All && mole.MatterState != movedContent)
					continue;
				// First, update energies from phase 1
				inputGasMixture.SetEnergy(mole.Type, mole.Energy);
				var quantity = (float)(mole.Quantity * scoopedFraction);
				// Then, remove scooped quantity from input
				if (!quantity.IsDenormalToNegative())
				{
					var scoopedMole = inputGasMixture.Remove(mole.Type, quantity);
					if (!scoopedMole.Quantity.IsDenormalToNegative() && !scoopedMole.Energy.IsDenormalToNegative())
						pumpAtmosphereMolesList.Add(scoopedMole);
				}
			}

			// Stage 2, compression
			var pumpAtmosphereMoles = pumpAtmosphereMolesList.ToArray();
			inputAtmosphere.GasMixture = inputGasMixture;
			var stage2Volume = pumpingVolume;
			(double WorkDone, double Temperature, double Pressure, Mole[] Result) stage2 = (0, stage1.Temperature, stage1.Pressure, pumpAtmosphereMoles);
			double equalizedVolume = pumpingVolume;
			if (pumpAtmospherePressure < outputAtmosphere.PressureGassesAndLiquidsInPa && movedContent != MatterState.Liquid)
			{
				equalizedVolume = EqualizationVolume(stage1.Pressure, pumpAtmosphereMoles.AverageHeatCapacityRatio(), phase1FinalVolume,
					outputAtmosphere.PressureGassesAndLiquidsInPa);
				if (equalizedVolume < pumpingVolume && equalizedVolume > 0.00000001)
				{
					stage2 = ChangeVolume(pumpingVolume, equalizedVolume, movedContent, pumpAtmosphereMoles);
					stage2Volume = equalizedVolume;
				}
			}

			// Stage 3, exhaust

			// First, we mix in gases from the pump.
			var outputGasMixture = outputAtmosphere.GasMixture;
			foreach (var mole in stage2.Result)
			{
				if(!mole.Quantity.IsDenormalToNegative() && !mole.Energy.IsDenormalToNegative())
					outputGasMixture.Add(mole);
			}
			outputAtmosphere.GasMixture = outputGasMixture;

			// Then we "compress" the output atmosphere, simulating pump pushing out it's atmosphere.
			var finalAtmosphereMoles = outputAtmosphere.Moles(movedContent).ToArray();
			// from V_o + V_p to V_o
			var stage3Volume = outputAtmosphere.Volume + stage2Volume;
			var stage3 = ChangeVolume(stage3Volume, outputAtmosphere.Volume, movedContent, finalAtmosphereMoles);
			var totalEnergy = (stage1.WorkDone + stage2.WorkDone + stage3.WorkDone);
			var energyBack = totalEnergy < 0 ? -totalEnergy : 0;
			totalEnergy = totalEnergy < 0 ? 0 : totalEnergy;
			var energyRatio = 1 / finalAtmosphereMoles.Sum(x => x.Quantity) * energyBack;
			// Finally, we update the output atmosphere's energy values.
			// And add excess energy back into the pumped gas.
			foreach (var mole in stage3.Result)
				outputGasMixture.SetEnergy(mole.Type, (float)(mole.Energy + energyRatio * mole.Quantity));
			outputAtmosphere.GasMixture = outputGasMixture;

			if (Debug)
			{
				Plugin.Log($"pumpingVolume: {pumpingVolume}\n" +
				           $"pumpAtmospherePressure: {pumpAtmospherePressure}\n" +
				           $"stage1.WorkDone: {stage1.WorkDone}\n" +
				           $"stage1.Temperature: {stage1.Temperature}\n" +
				           $"stage1.Pressure: {stage1.Pressure}\n" +
				           $"stage1.Amount: {stage1.Result.Sum(x => x.Quantity)}\n" +
				           $"stage2.WorkDone: {stage2.WorkDone}\n" +
				           $"stage2.Temperature: {stage2.Temperature}\n" +
				           $"stage2.Pressure: {stage2.Pressure}\n" +
				           $"stage2.Amount: {stage2.Result.Sum(x => x.Quantity)}\n" +
						   $"stage3.WorkDone: {stage3.WorkDone}\n" +
				           $"stage3.Temperature: {stage3.Temperature}\n" +
				           $"stage3.Pressure: {stage3.Pressure}\n" +
				           $"stage3.Amount: {stage3.Result.Sum(x => x.Quantity)}\n" +
						   $"movedAmount: {stage2.Result.Sum(x => x.Quantity)}\n" +
						   $"equalizedVolume: {equalizedVolume}\n" +
				           $"TotalWork: {(float)(stage1.WorkDone + stage2.WorkDone + stage3.WorkDone)}");
			}
			return (float)totalEnergy;
		}

		private static double CalculatePumpVolume(Atmosphere inputAtmosphere, Atmosphere outputAtmosphere, double pumpVolume, double powerLimit,
			double inputPressureLimit, double outputPressureLimit, AtmosphereHelper.MatterState movedContent)
		{
			int iterations = 0;
			double powerUsed = 0;
			var inputAtmosphereMoles = inputAtmosphere.Moles(MatterState.All).ToArray();
			var outputAtmosphereMoles = outputAtmosphere.Moles(MatterState.All).ToArray();

			// This function calculates the cost, based on defined limits for power, input and output pressure.
			// As long as the power is below power limit, the input pressure is above input ressure limit, and
			// the output pressure is below output pressure limit, the function returns zero. Otherwise, it returns
			// the sum of over-the-limit values.
			double TargetPumpFunction(double volume)
			{
				iterations++;
				if (volume == 0)
					return 0;

				// intake
				// from V_i to V_i + V_p
				var initialVolume = movedContent switch
				{
					MatterState.Gas => inputAtmosphere.Volume - inputAtmosphere.TotalVolumeLiquids,
					MatterState.Liquid => inputAtmosphere.TotalVolumeLiquids,
					_ => inputAtmosphere.Volume,
				};
				var phase1FinalVolume = initialVolume
				+ volume;
				var inputAtmosphereCopy = inputAtmosphereMoles.Copy();
				var stage1 = ChangeVolume(initialVolume, phase1FinalVolume, movedContent, inputAtmosphereCopy);
				var scoopedFraction = volume / phase1FinalVolume;
				var pumpAtmosphereMoles = inputAtmosphereCopy
					.Select(m =>
					{
						if (movedContent != MatterState.All && m.MatterState != movedContent)
							return new Mole(m.Type);
						var quantity = (float) (m.Quantity * scoopedFraction);
						if (quantity.IsDenormalToNegative())
							quantity = 0;
						var energy = (float) (m.Energy * scoopedFraction);
						if (energy.IsDenormalToNegative())
							energy = 0;
						return new Mole(m.Type, quantity, energy);
					}).ToArray();
				var pumpAtmospherePressure = stage1.Pressure;

				// compression
				// close the intake. No work is done at this point, as pressures equalized between input and the pump
				var stage2Volume = volume;
				(double WorkDone, double Temperature, double Pressure, Mole[] Result) stage2 = (0, stage1.Temperature, stage1.Pressure, pumpAtmosphereMoles);
				if (pumpAtmospherePressure < outputAtmosphere.PressureGassesAndLiquidsInPa && movedContent != MatterState.Liquid)
				{
					var equalizedVolume = EqualizationVolume(stage1.Pressure, pumpAtmosphereMoles.AverageHeatCapacityRatio(), phase1FinalVolume,
						outputAtmosphere.PressureGassesAndLiquidsInPa);
					if (equalizedVolume < volume && equalizedVolume > 0.00000001)
					{
						stage2 = ChangeVolume(volume, equalizedVolume, movedContent, pumpAtmosphereMoles);
						stage2Volume = equalizedVolume;
					}
				}

				// exhaust
				// from V_o + V_p to V_o
				var stage3Volume = movedContent switch
				{
					MatterState.Gas => outputAtmosphere.Volume - outputAtmosphere.TotalVolumeLiquids,
					MatterState.Liquid => outputAtmosphere.TotalVolumeLiquids,
					_ => outputAtmosphere.Volume,
				} + stage2Volume;

				// We know that both stage2.Result and outputAtmosphereMoles have same elements on the same places.
				var finalAtmosphere = stage2.Result.Zip(outputAtmosphereMoles,
					(m1, m2) =>
					{
						var quantity = m1.Quantity + m2.Quantity;
						if(quantity.IsDenormalToNegative())
							quantity = 0;
						var energy = m1.Energy + m2.Energy;
						if (energy.IsDenormalToNegative())
							energy = 0;
						return new Mole(m1.Type, quantity, energy);
					}).ToArray();
				var stage3 = ChangeVolume(stage3Volume, outputAtmosphere.Volume, movedContent, finalAtmosphere);

				// As long as input pressure at stage 1 is above input pressure limit, this value is zero.
				var inputPressureDifference = Math.Max(0, inputPressureLimit - stage1.Pressure);

				// As long as output pressure at stage 3 is below output pressure limit, this value is zero.
				var outputPressureDifference = Math.Max(0, stage3.Pressure - outputPressureLimit);
				powerUsed = stage1.WorkDone + stage2.WorkDone + stage3.WorkDone;
				var powerUsedDifference = Math.Max(0, powerUsed - powerLimit);

				return powerUsedDifference + inputPressureDifference + outputPressureDifference;
			}

			// Try the maximum volume first
			var cost = TargetPumpFunction(pumpVolume);
			if (cost <= 0)
				return pumpVolume;
			var initialGuess = pumpVolume * (powerLimit / powerUsed);
			if(initialGuess <= 0)
				initialGuess = pumpVolume / 2;
			if (double.IsNaN(powerUsed) || double.IsInfinity(initialGuess) || double.IsNaN(initialGuess))
				throw new ApplicationException("Initial guess: " + initialGuess);

			iterations = 0;
			var result = GoldenSectionSearch(TargetPumpFunction, 0, pumpVolume, 0, cost, initialGuess);

			return result;
		}
		private static (double WorkDone, double Temperature, double Pressure, Mole[] Result) ChangeVolume(double initialVolume, double finalVolume, MatterState movedContent, params Mole[] gases)
		{
			if (finalVolume <= 0)
				throw new ArgumentException("Final volume can't be zero or negative!");
			double workDone = .0, temperature = 0, pressure = 0;
			for (int i = 0; i < gases.Length; i++)
			{
				var mole = gases[i];
				// Do not process too small amount and other states
				if ((movedContent != MatterState.All && mole.MatterState != movedContent) || mole.Quantity.IsDenormalToNegative())
					continue;
				temperature = mole.Temperature * Math.Pow(initialVolume / finalVolume, mole.HeatCapacityRatio() - 1);
				temperature = Math.Max(1, temperature);
				pressure += mole.Quantity * Chemistry.IdealGasEquation * temperature;
				var temperatureDifference = temperature - mole.Temperature;
				//var moleWorkDone = -mole.Quantity * mole.SpecificHeat * temperatureDifference;
				var moleWorkDone = (mole.Quantity * mole.SpecificHeat * temperatureDifference) / (mole.HeatCapacityRatio() - 1);
				workDone += moleWorkDone;
				var energy = (float) (mole.Energy + moleWorkDone);
				mole.Set(mole.Quantity, energy);
				gases[i] = mole;
			}
			pressure /= finalVolume;
			return (workDone, temperature, pressure, gases);
		}
		private static double EqualizationVolume(double pressure, double heatCapacityRatio, double volume, double targetPressure)
		{
			return Math.Pow(pressure / targetPressure, 1 / heatCapacityRatio) * volume;
		}
		public static double GoldenSectionSearch(Func<double, double> f, double minimum, double maximum, double f_minimum, double f_maximum, double initialGuess, double tolerance = 1e-6, int maxIterations = 100)
		{
			var scale = 1 / tolerance;
			double eval(double x)
			{
				var value = f(x);
				return value > 0 ? value * scale : maximum - x;
			}
			const double phi = 0.61803398875;

			double a = minimum;
			double b = maximum;
			double c = minimum + (1 - phi) * (maximum - minimum);
			double d = minimum + phi * (maximum - minimum);

			double f_a = f_minimum > 0 ? f_minimum * scale : maximum - minimum;
			double f_b = f_maximum > 0 ? f_maximum * scale : maximum - minimum;
			double f_c = eval(c);
			double f_d = eval(d);

			int iteration = 0;

			while (Math.Abs(b - a) > tolerance && iteration < maxIterations)
			{
				if (f_c < f_d)
				{
					b = d;
					f_b = f_d;
					d = c;
					f_d = f_c;
					c = a + (1 - phi) * (b - a);
					f_c = eval(c);
				}
				else
				{
					a = c;
					f_a = f_c;
					c = d;
					f_c = f_d;
					d = a + phi * (b - a);
					f_d = eval(d);
				}

				iteration++;
			}

			var result = (a + b) / 2.0;
			var f_result = eval(result);
			if (f_a < f_result)
				return a;
			if (f_b < f_result)
				return b;
			return result;
		}
		public static IEnumerable<Mole> Moles(this Atmosphere atmosphere, MatterState matterState = MatterState.All)
		{
			if (matterState is MatterState.Gas or MatterState.All)
			{
				yield return atmosphere.GasMixture.Oxygen;
				yield return atmosphere.GasMixture.Nitrogen;
				yield return atmosphere.GasMixture.CarbonDioxide;
				yield return atmosphere.GasMixture.Volatiles;
				yield return atmosphere.GasMixture.Pollutant;
				yield return atmosphere.GasMixture.Steam;
				yield return atmosphere.GasMixture.NitrousOxide;
			}
			if (matterState is MatterState.Liquid or MatterState.All)
			{
				yield return atmosphere.GasMixture.Water;
				yield return atmosphere.GasMixture.LiquidNitrogen;
				yield return atmosphere.GasMixture.LiquidOxygen;
				yield return atmosphere.GasMixture.LiquidVolatiles;
				yield return atmosphere.GasMixture.LiquidCarbonDioxide;
				yield return atmosphere.GasMixture.LiquidPollutant;
				yield return atmosphere.GasMixture.LiquidNitrousOxide;
			}
		}
		public static IEnumerable<Mole> Moles(this GasMixture gasMixture, MatterState matterState = MatterState.All)
		{
			if (matterState is MatterState.Gas or MatterState.All)
			{
				yield return gasMixture.Oxygen;
				yield return gasMixture.Nitrogen;
				yield return gasMixture.CarbonDioxide;
				yield return gasMixture.Volatiles;
				yield return gasMixture.Pollutant;
				yield return gasMixture.Steam;
				yield return gasMixture.NitrousOxide;
			}
			if (matterState is MatterState.Liquid or MatterState.All)
			{
				yield return gasMixture.Water;
				yield return gasMixture.LiquidNitrogen;
				yield return gasMixture.LiquidOxygen;
				yield return gasMixture.LiquidVolatiles;
				yield return gasMixture.LiquidCarbonDioxide;
				yield return gasMixture.LiquidPollutant;
				yield return gasMixture.LiquidNitrousOxide;
			}
		}
		private static double AverageHeatCapacityRatio(this Mole[] atmosphere)
		{
			var sumQuantity = .0;
			var sumHeatCapacityRatio = .0;
			foreach (var mole in atmosphere)
			{
				if (mole.Quantity <= 0)
					continue;
				sumQuantity += mole.Quantity;
				sumHeatCapacityRatio = mole.Quantity * mole.HeatCapacityRatio();
			}
			return sumHeatCapacityRatio / sumQuantity;
		}
		private static Mole[] Copy(this Mole[] array)
		{
			var result = new Mole[array.Length];
			array.CopyTo(result, 0);
			return result;
		}
		private static void SetEnergy(this ref GasMixture gasMixture, Chemistry.GasType gas, float energy)
		{
			switch (gas)
			{
				case Chemistry.GasType.Undefined:
					break;
				case Chemistry.GasType.Oxygen:
					gasMixture.Oxygen.Set(gasMixture.Oxygen.Quantity, energy);
					break;
				case Chemistry.GasType.Nitrogen:
					gasMixture.Nitrogen.Set(gasMixture.Nitrogen.Quantity, energy);
					break;
				case Chemistry.GasType.CarbonDioxide:
					gasMixture.CarbonDioxide.Set(gasMixture.CarbonDioxide.Quantity, energy);
					break;
				case Chemistry.GasType.Volatiles:
					gasMixture.Volatiles.Set(gasMixture.Volatiles.Quantity, energy);
					break;
				case Chemistry.GasType.Pollutant:
					gasMixture.Pollutant.Set(gasMixture.Pollutant.Quantity, energy);
					break;
				case Chemistry.GasType.Water:
					gasMixture.Water.Set(gasMixture.Water.Quantity, energy);
					break;
				case Chemistry.GasType.NitrousOxide:
					gasMixture.NitrousOxide.Set(gasMixture.NitrousOxide.Quantity, energy);
					break;
				case Chemistry.GasType.LiquidNitrogen:
					gasMixture.LiquidNitrogen.Set(gasMixture.LiquidNitrogen.Quantity, energy);
					break;
				case Chemistry.GasType.LiquidOxygen:
					gasMixture.LiquidOxygen.Set(gasMixture.LiquidOxygen.Quantity, energy);
					break;
				case Chemistry.GasType.LiquidVolatiles:
					gasMixture.LiquidVolatiles.Set(gasMixture.LiquidVolatiles.Quantity, energy);
					break;
				case Chemistry.GasType.Steam:
					gasMixture.Steam.Set(gasMixture.Steam.Quantity, energy);
					break;
				case Chemistry.GasType.LiquidCarbonDioxide:
					gasMixture.LiquidCarbonDioxide.Set(gasMixture.LiquidCarbonDioxide.Quantity, energy);
					break;
				case Chemistry.GasType.LiquidPollutant:
					gasMixture.LiquidPollutant.Set(gasMixture.LiquidPollutant.Quantity, energy);
					break;
				case Chemistry.GasType.LiquidNitrousOxide:
					gasMixture.LiquidNitrousOxide.Set(gasMixture.LiquidNitrousOxide.Quantity, energy);
					break;
			}
		}

		public static float MolarVolumeLiquid(this Chemistry.GasType type)
		{
			switch (type)
			{
				case Chemistry.GasType.Oxygen:
				case Chemistry.GasType.LiquidOxygen:
					return 0.03f;
				case Chemistry.GasType.Nitrogen:
				case Chemistry.GasType.LiquidNitrogen:
					return 0.0348f;
				case Chemistry.GasType.CarbonDioxide:
				case Chemistry.GasType.LiquidCarbonDioxide:
					return 0.04f;
				case Chemistry.GasType.Volatiles:
				case Chemistry.GasType.LiquidVolatiles:
					return 0.04f;
				case Chemistry.GasType.Pollutant:
				case Chemistry.GasType.LiquidPollutant:
					return 0.04f;
				case Chemistry.GasType.NitrousOxide:
				case Chemistry.GasType.LiquidNitrousOxide:
					return 0.026f;
				case Chemistry.GasType.Steam:
				case Chemistry.GasType.Water:
					return 0.018f;
				default:
					return 0;
			}
		}

		public static void AddFix(this ref GasMixture gasMixture, Mole mole)
		{
			switch (mole.Type)
			{
				default:
					break;
				case Chemistry.GasType.Oxygen:
					var oxygen = gasMixture.Oxygen;
					oxygen.Add(mole);
					gasMixture.Oxygen = oxygen;
					break;
				case Chemistry.GasType.Nitrogen:
					var nitrogen = gasMixture.Nitrogen;
					nitrogen.Add(mole);
					gasMixture.Nitrogen = nitrogen;
					break;
				case Chemistry.GasType.CarbonDioxide:
					var carbonDioxide = gasMixture.CarbonDioxide;
					carbonDioxide.Add(mole);
					gasMixture.CarbonDioxide = carbonDioxide;
					break;
				case Chemistry.GasType.Volatiles:
					var volatiles = gasMixture.Volatiles;
					volatiles.Add(mole);
					gasMixture.Volatiles = volatiles;
					break;
				case Chemistry.GasType.Pollutant:
					var pollutant = gasMixture.Pollutant;
					pollutant.Add(mole);
					gasMixture.Pollutant = pollutant;
					break;
				case Chemistry.GasType.Water:
					var water = gasMixture.Water;
					water.Add(mole);
					gasMixture.Water = water;
					break;
				case Chemistry.GasType.NitrousOxide:
					var nitrousOxide = gasMixture.NitrousOxide;
					nitrousOxide.Add(mole);
					gasMixture.NitrousOxide = nitrousOxide;
					break;
				case Chemistry.GasType.LiquidNitrogen:
					var liquidNitrogen = gasMixture.LiquidNitrogen;
					liquidNitrogen.Add(mole);
					gasMixture.LiquidNitrogen = liquidNitrogen;
					break;
				case Chemistry.GasType.LiquidOxygen:
					var liquidOxygen = gasMixture.LiquidOxygen;
					liquidOxygen.Add(mole);
					gasMixture.LiquidOxygen = liquidOxygen;
					break;
				case Chemistry.GasType.LiquidVolatiles:
					var liquidVolatiles = gasMixture.LiquidVolatiles;
					liquidVolatiles.Add(mole);
					gasMixture.LiquidVolatiles = liquidVolatiles;
					break;
				case Chemistry.GasType.Steam:
					var steam = gasMixture.Steam;
					steam.Add(mole);
					gasMixture.Steam = steam;
					break;
				case Chemistry.GasType.LiquidCarbonDioxide:
					var liquidCarbonDioxide = gasMixture.LiquidCarbonDioxide;
					liquidCarbonDioxide.Add(mole);
					gasMixture.LiquidCarbonDioxide = liquidCarbonDioxide;
					break;
				case Chemistry.GasType.LiquidPollutant:
					var liquidPollutant = gasMixture.LiquidPollutant;
					liquidPollutant.Add(mole);
					gasMixture.LiquidPollutant = liquidPollutant;
					break;
				case Chemistry.GasType.LiquidNitrousOxide:
					var liquidNitrousOxide = gasMixture.LiquidNitrousOxide;
					liquidNitrousOxide.Add(mole);
					gasMixture.LiquidNitrousOxide = liquidNitrousOxide;
					break;
			}
		}

		public static void AddFix(this ref GasMixture gasMixture, GasMixture otherMixture)
		{
			foreach (var mole in otherMixture.Moles())
				gasMixture.AddFix(mole);
		}
	}
}
