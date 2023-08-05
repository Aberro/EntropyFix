using System;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Effects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;
using static Assets.Scripts.Atmospherics.AtmosphereHelper;

namespace EntropyFix
{
    public class AtmosphericHelper
    {
        private static float EqualizableMatter(Atmosphere inputAtmos, Atmosphere outputAtmos)
        {
            float pressureChange = inputAtmos.PressureGassesAndLiquids - outputAtmos.PressureGassesAndLiquids;
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
        /// <param name="desiredPressureOrVolumeChange">Limit of change in input or output pressure or, when movedContent is liquid, volume.</param>
        /// <param name="regulationType">When upstream, desiredPressureOrVolumeChange is applied for output atmosphere, when downstream for input atmosphere.</param>
        /// <param name="powerRating">The maximum power for pumping action.</param>
        /// <param name="movedContent">The matter state to move.</param>
        /// <returns>Power consumption.</returns>
        public static float PumpVolume(
            Atmosphere inputAtmosphere,
            Atmosphere outputAtmosphere,
            float pumpVolume,
            float desiredPressureOrVolumeChange,
            RegulatorType regulationType,
            float powerRating,
            AtmosphereHelper.MatterState movedContent,
            bool useReductor = true)
        {
            var predictedInputAtmosphere = new Atmosphere();
            predictedInputAtmosphere.Volume = inputAtmosphere.Volume;
            predictedInputAtmosphere.GasMixture = new GasMixture(inputAtmosphere.GasMixture);
            var predictedOutputAtmosphere = new Atmosphere();
            predictedOutputAtmosphere.Volume = outputAtmosphere.Volume;
            predictedOutputAtmosphere.GasMixture = new GasMixture(outputAtmosphere.GasMixture);
            var equalizableMatter = EqualizableMatter(inputAtmosphere, outputAtmosphere);
            var maxMovedMatter = inputAtmosphere.GasMixture.TotalMoles(movedContent) * Math.Min(1, pumpVolume / inputAtmosphere.Volume);
            var movedGasMixture = predictedInputAtmosphere.Remove(maxMovedMatter, movedContent);
            predictedOutputAtmosphere.Add(movedGasMixture);
            float pumpingRatio = 1; // How much of pump volume is utilized to move matter.
            switch (regulationType)
            {
                case RegulatorType.Upstream:
	                var predictedOutputPressureOrVolume = movedContent switch
	                {
		                MatterState.Liquid => predictedOutputAtmosphere.TotalVolumeLiquids,
		                MatterState.Gas => predictedOutputAtmosphere.PressureGasses,
		                _ => predictedOutputAtmosphere.PressureGassesAndLiquids,
	                };
	                var outputPressureOrVolume = movedContent switch
	                {
		                MatterState.Liquid => outputAtmosphere.TotalVolumeLiquids,
		                MatterState.Gas => outputAtmosphere.PressureGasses,
		                _ => outputAtmosphere.PressureGassesAndLiquids,
	                };
                    if (predictedOutputPressureOrVolume > desiredPressureOrVolumeChange)
                        pumpingRatio *= Math.Min(1, desiredPressureOrVolumeChange / (predictedOutputPressureOrVolume - outputPressureOrVolume));
                    break;
                case RegulatorType.Downstream:
	                var predictedInputPressureOrVolume = movedContent switch
	                {
		                MatterState.Liquid => predictedInputAtmosphere.TotalVolumeLiquids,
		                MatterState.Gas => predictedInputAtmosphere.PressureGasses,
		                _ => predictedInputAtmosphere.PressureGassesAndLiquids,
	                };
	                var inputPressureOrVolume = movedContent switch
	                {
		                MatterState.Liquid => inputAtmosphere.TotalVolumeLiquids,
		                MatterState.Gas => inputAtmosphere.PressureGasses,
		                _ => inputAtmosphere.PressureGassesAndLiquids,
	                };
                    if (predictedInputPressureOrVolume > desiredPressureOrVolumeChange)
                        pumpingRatio *= Math.Min(1, desiredPressureOrVolumeChange / (inputPressureOrVolume - predictedInputPressureOrVolume));
                    break;
            }
            // How much matter should be moved.
            var movedMatter = maxMovedMatter * pumpingRatio;
            // How much matter should be moved using pump.
            var powerMovedMatter = Mathf.Max(0, movedMatter - equalizableMatter);
            var createdPressureOrVolumeDifference = movedContent switch
            {
                MatterState.Liquid => predictedOutputAtmosphere.TotalVolumeLiquids - predictedInputAtmosphere.TotalVolumeLiquids,
                MatterState.Gas => predictedOutputAtmosphere.PressureGasses - predictedInputAtmosphere.PressureGasses,
                _ => predictedOutputAtmosphere.PressureGassesAndLiquids - predictedInputAtmosphere.PressureGassesAndLiquids
            };

            var maxPressureFromPower = inputAtmosphere.PressureGassesAndLiquids *
                                       Math.Exp(-powerRating / (maxMovedMatter * 8.3144598 * inputAtmosphere.Temperature));
            var usedPowerRatio = pumpingRatio * (powerMovedMatter / movedMatter) * (createdPressureOrVolumeDifference / maxPressureFromPower);
            if (movedMatter <= float.Epsilon)
                usedPowerRatio = 0;
            if (usedPowerRatio > 1)
            {
                if (true/*useReductor*/)
                {
                    movedMatter /= (float)usedPowerRatio;
                    usedPowerRatio = 1;
                }
                else
                {
                    movedMatter = 0;
                    usedPowerRatio = 0;
                }
            }
            if (Debug)
            {
                Plugin.Log($"AtmosphericHelper.PumpVolume:\n" +
                               $"{{\n" +
                               $"\tinputAtmosphere.pressure: {inputAtmosphere.PressureGassesAndLiquids},\n" +
                               $"\toutputAtmosphere.pressure: {outputAtmosphere.PressureGassesAndLiquids},\n" +
                               $"\tinputAtmosphere.quantity: {inputAtmosphere.GasMixture.TotalMolesGassesAndLiquids},\n" +
                               $"\toutputAtmosphere.quantity: {outputAtmosphere.GasMixture.TotalMolesGassesAndLiquids},\n" +
                               $"\tregulationType: {regulationType},\n" +
                               $"\tpumpVolume: {pumpVolume},\n" +
                               $"\tdesiredPressureOrVolumeChange: {desiredPressureOrVolumeChange},\n" +
                               $"\tpowerRating: {powerRating},\n" +
                               $"\tequalizableMatter: {equalizableMatter},\n" +
                               $"\tmaxMovedMatter: {maxMovedMatter},\n" +
                               $"\tpredictedInput.pressure: {predictedInputAtmosphere.PressureGassesAndLiquids},\n" +
                               $"\tpredictedOutput.pressure: {predictedOutputAtmosphere.PressureGassesAndLiquids},\n" +
                               $"\tpredictedInput.quantity: {predictedInputAtmosphere.GasMixture.TotalMolesGassesAndLiquids},\n" +
                               $"\tpredictedOutput.quantity: {predictedOutputAtmosphere.GasMixture.TotalMolesGassesAndLiquids},\n" +
                               $"\tpumpingRatio: {pumpingRatio},\n" +
                               $"\tmovedMatter: {movedMatter},\n" +
                               $"\tpowerMovedMatter: {powerMovedMatter},\n" +
                               $"\tcreatedPressureOrVolumeDifference: {createdPressureOrVolumeDifference},\n" +
                               $"\tmaxPressureFromPower: {maxPressureFromPower},\n" +
                               $"\tusedPowerRatio: {usedPowerRatio},\n" +
                               $"\tresult: {powerRating*usedPowerRatio}\n" +
                               $"}}");
            }
            if(movedMatter > 0)
                outputAtmosphere.Add(inputAtmosphere.Remove(movedMatter, movedContent));
            return powerRating * (float)usedPowerRatio;
        }
    }
}
