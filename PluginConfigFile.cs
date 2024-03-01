using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace EntropyFix
{
	public class PluginConfigFile : ConfigFile
	{
		public enum ConfigEntry
		{
			[DisplayName("Cascaded SEGI")]
			[Description("Experimental version of SEGI that should works better at long distances, but has fewer presets and has some missing features.")]
			CascadedSEGI,
			[DisplayName("SEGI preset")]
			[Description("Preconfigured settings for SEGI.")]
			SEGIPreset,
			[DisplayName("Cascaded SEGI preset")]
			[Description("Preconfigured settings for Cascaded SEGI.")]
			CascadedSEGIPreset,
			[DisplayName("Voxel resolution")]
			[Description("Higher resolutions are more accurate, but require more memory expensive.")]
			VoxelResolution,
			[DisplayName("Voxel antialiasing")]
			[Description("Smooths out the edges of global illumination voxels.")]
			VoxelAA,
			[DisplayName("Inner occlusion layers")]
			InnerOcclusionLayers,
			[DisplayName("Infinite bounces")]
			[Description("Allows light to bounce infinitely until light source is hit, but is more performance expensive.")]
			InfiniteBounces,
			[DisplayName("Temporal blend weight")]
			[Description("Blends between the current frame and the previous frame to reduce temporal flickering.")]
			TemporalBlendWeight,
			[DisplayName("Use bilateral filtering")]
			UseBilateralFiltering,
			[DisplayName("Half resolution")]
			HalfResolution,
			[DisplayName("Stochastic sampling")]
			[Description("Randomizes the sampling pattern to smooth out light transport.")]
			StochasticSampling,
			[DisplayName("Reflections")]
			[Description("Enables reflections.")]
			DoReflections,
			[DisplayName("Cones")]
			[Description("Number of cones to use for cone tracing.")]
			Cones,
			[DisplayName("Cone trace steps")]
			[Description("Number of steps to use for cone tracing.")]
			ConeTraceSteps,
			[DisplayName("Cone length")]
			[Description("Length of the cone to use for cone tracing.")]
			ConeLength,
			[DisplayName("Cone width")]
			[Description("Width of the cone to use for cone tracing.")]
			ConeWidth,
			[Description("Cone tracing bias")]
			ConeTraceBias,
			[DisplayName("Occlusion strength")]
			OcclusionStrength,
			[DisplayName("Near occlusion strength")]
			NearOcclusionStrength,
			[DisplayName("Occlusion power")]
			OcclusionPower,
			[DisplayName("Near light gain")]
			NearLightGain,
			[DisplayName("GI gain")]
			[Description("Global illumination gain.")]
			GIGain,
			[DisplayName("Secondary bounce gain")]
			SecondaryBounceGain,
			[DisplayName("Reflection steps")]
			[Description("Number of steps to use for reflection tracing.")]
			ReflectionSteps,
			[DisplayName("Reflection occlusion power")]
			ReflectionOcclusionPower,
			[DisplayName("Sky reflection intensity")]
			SkyReflectionIntensity,
			[DisplayName("Gaussian mip filter")]
			GaussianMipFilter,
			[DisplayName("Far occlusion strength")]
			FarOcclusionStrength,
			[DisplayName("Farthest occlusion strength")]
			FarthestOcclusionStrength,
			[DisplayName("Secondary cones")]
			SecondaryCones,
			[DisplayName("Secondary occlusion strength")]
			SecondaryOcclusionStrength,

			[DisplayName("Small pump power")]
			[Description("Power of small pumps (volume pump, regulator, active vent, etc).")]
			SmallPumpPower,
			[DisplayName("Large pump power")]
			[Description("Power of large pumps (Powered vent, turbo pump).")]
			LargePumpPower,
			[DisplayName("Air conditioner power")]
			[Description("Power consumption of air conditioner.")]
			AirConditionerPower,
			[DisplayName("Air conditioner heat transfer efficiency")]
			[Description("Amount of heat power per electricity power consumption under ideal conditions.")]
			AirConditionerEfficiency,
		}
		public Dictionary<PatchCategory, ConfigEntry<bool>> Features { get; } = new Dictionary<PatchCategory, ConfigEntry<bool>>();

		public ConfigEntry<bool> CascadedSEGIEntry;
		public ConfigEntry<SEGIManager.SettingPreset> SEGIPresetEntry;
		public ConfigEntry<SEGIManager.SettingCascadedPreset> SEGICascadedPresetEntry;
		public ConfigEntry<SEGI.VoxelResolution> VoxelResolutionEntry;
		public ConfigEntry<bool> VoxelAAEntry;
		public ConfigEntry<int> InnerOcclusionLayersEntry;
		public ConfigEntry<bool> InfiniteBouncesEntry;
		public ConfigEntry<float> TemporalBlendWeightEntry;
		public ConfigEntry<bool> UseBilateralFilteringEntry;
		public ConfigEntry<bool> HalfResolutionEntry;
		public ConfigEntry<bool> StochasticSamplingEntry;
		public ConfigEntry<bool> DoReflectionsEntry;
		public ConfigEntry<int> ConesEntry;
		public ConfigEntry<int> ConeTraceStepsEntry;
		public ConfigEntry<float> ConeLengthEntry;
		public ConfigEntry<float> ConeWidthEntry;
		public ConfigEntry<float> ConeTraceBiasEntry;
		public ConfigEntry<float> OcclusionStrengthEntry;
		public ConfigEntry<float> NearOcclusionStrengthEntry;
		public ConfigEntry<float> OcclusionPowerEntry;
		public ConfigEntry<float> NearLightGainEntry;
		public ConfigEntry<float> GIGainEntry;
		public ConfigEntry<float> SecondaryBounceGainEntry;
		public ConfigEntry<int> ReflectionStepsEntry;
		public ConfigEntry<float> ReflectionOcclusionPowerEntry;
		public ConfigEntry<float> SkyReflectionIntensityEntry;
		public ConfigEntry<bool> GaussianMipFilterEntry;
		public ConfigEntry<float> FarOcclusionStrengthEntry;
		public ConfigEntry<float> FarthestOcclusionStrengthEntry;
		public ConfigEntry<int> SecondaryConesEntry;
		public ConfigEntry<float> SecondaryOcclusionStrengthEntry;

		#region SEGI settings
		public bool CascadedSEGI
		{
			get => this.CascadedSEGIEntry.Value;
			set => this.CascadedSEGIEntry.Value = value;
		}
		public SEGIManager.SettingPreset SEGIPreset
		{
			get => this.SEGIPresetEntry.Value;
			set => this.SEGIPresetEntry.Value = value;
		}
		public SEGIManager.SettingCascadedPreset SEGICascadedPreset
		{
			get => this.SEGICascadedPresetEntry.Value;
			set => this.SEGICascadedPresetEntry.Value = value;
		}
		public SEGI.VoxelResolution VoxelResolution
		{
			get => this.VoxelResolutionEntry.Value;
			set => this.VoxelResolutionEntry.Value = value;
		}
		public bool VoxelAA
		{
			get => this.VoxelAAEntry.Value;
			set => this.VoxelAAEntry.Value = value;
		}
		public int InnerOcclusionLayers
		{
			get => this.InnerOcclusionLayersEntry.Value;
			set => this.InnerOcclusionLayersEntry.Value = value;
		}
		public bool InfiniteBounces
		{
			get => this.InfiniteBouncesEntry.Value;
			set => this.InfiniteBouncesEntry.Value = value;
		}
		public float TemporalBlendWeight
		{
			get => this.TemporalBlendWeightEntry.Value;
			set => this.TemporalBlendWeightEntry.Value = value;
		}
		public bool UseBilateralFiltering
		{
			get => this.UseBilateralFilteringEntry.Value;
			set => this.UseBilateralFilteringEntry.Value = value;
		}
		public bool HalfResolution
		{
			get => this.HalfResolutionEntry.Value;
			set => this.HalfResolutionEntry.Value = value;
		}
		public bool StochasticSampling
		{
			get => this.StochasticSamplingEntry.Value;
			set => this.StochasticSamplingEntry.Value = value;
		}
		public bool DoReflections
		{
			get => this.DoReflectionsEntry.Value;
			set => this.DoReflectionsEntry.Value = value;
		}
		public int Cones
		{
			get => this.ConesEntry.Value;
			set => this.ConesEntry.Value = value;
		}
		public int ConeTraceSteps
		{
			get => this.ConeTraceStepsEntry.Value;
			set => this.ConeTraceStepsEntry.Value = value;
		}
		public float ConeLength
		{
			get => this.ConeLengthEntry.Value;
			set => this.ConeLengthEntry.Value = value;
		}
		public float ConeWidth
		{
			get => this.ConeWidthEntry.Value;
			set => this.ConeWidthEntry.Value = value;
		}
		public float ConeTraceBias
		{
			get => this.ConeTraceBiasEntry.Value;
			set => this.ConeTraceBiasEntry.Value = value;
		}
		public float OcclusionStrength
		{
			get => this.OcclusionStrengthEntry.Value;
			set => this.OcclusionStrengthEntry.Value = value;
		}
		public float NearOcclusionStrength
		{
			get => this.NearOcclusionStrengthEntry.Value;
			set => this.NearOcclusionStrengthEntry.Value = value;
		}
		public float OcclusionPower
		{
			get => this.OcclusionPowerEntry.Value;
			set => this.OcclusionPowerEntry.Value = value;
		}
		public float NearLightGain
		{
			get => this.NearLightGainEntry.Value;
			set => this.NearLightGainEntry.Value = value;
		}
		public float GIGain
		{
			get => this.GIGainEntry.Value;
			set => this.GIGainEntry.Value = value;
		}
		public float SecondaryBounceGain
		{
			get => this.SecondaryBounceGainEntry.Value;
			set => this.SecondaryBounceGainEntry.Value = value;
		}
		public int ReflectionSteps
		{
			get => this.ReflectionStepsEntry.Value;
			set => this.ReflectionStepsEntry.Value = value;
		}
		public float ReflectionOcclusionPower
		{
			get => this.ReflectionOcclusionPowerEntry.Value;
			set => this.ReflectionOcclusionPowerEntry.Value = value;
		}
		public float SkyReflectionIntensity
		{
			get => this.SkyReflectionIntensityEntry.Value;
			set => this.SkyReflectionIntensityEntry.Value = value;
		}
		public bool GaussianMipFilter
		{
			get => this.GaussianMipFilterEntry.Value;
			set => this.GaussianMipFilterEntry.Value = value;
		}
		public float FarOcclusionStrength
		{
			get => this.FarOcclusionStrengthEntry.Value;
			set => this.FarOcclusionStrengthEntry.Value = value;
		}
		public float FarthestOcclusionStrength
		{
			get => this.FarthestOcclusionStrengthEntry.Value;
			set => this.FarthestOcclusionStrengthEntry.Value = value;
		}
		public int SecondaryCones
		{
			get => this.SecondaryConesEntry.Value;
			set => this.SecondaryConesEntry.Value = value;
		}
		public float SecondaryOcclusionStrength
		{
			get => this.SecondaryOcclusionStrengthEntry.Value;
			set => this.SecondaryOcclusionStrengthEntry.Value = value;
		}
		#endregion

		#region Atmospheric patches settings
		public ConfigEntry<float> SmallPumpPower;
		public ConfigEntry<float> LargePumpPower;
		public ConfigEntry<float> AirConditionerPower;
		public ConfigEntry<float> AirConditionerEfficiency;
		#endregion

		public PluginConfigFile(string configPath, bool saveOnInit) : base(configPath, saveOnInit)
		{
			Init();
		}

		public PluginConfigFile(string configPath, bool saveOnInit, BepInPlugin ownerMetadata) : base(configPath, saveOnInit, ownerMetadata)
		{
			Init();
		}

		private void Init()
		{
			ConfigEntry<bool> feature = null;
			foreach (PatchCategory category in Enum.GetValues(typeof(PatchCategory)))
			{
				if (category == PatchCategory.None)
					continue;
				if (!Features.TryGetValue(category, out feature))
				{
					feature = Bind(ConfigSection.Features.GetDisplayName(), category.GetDisplayName(), true, category.GetDescription());
					Features.Add(category, feature);
				}
			}
			CascadedSEGIEntry = Bind(ConfigSection.SEGI.GetDisplayName(), ConfigEntry.CascadedSEGI.GetDisplayName(), false);
			SEGIPresetEntry = Bind(ConfigSection.SEGI.GetDisplayName(), ConfigEntry.SEGIPreset.GetDisplayName(), SEGIManager.SettingPreset.High);
			SEGICascadedPresetEntry = Bind(ConfigSection.SEGI.GetDisplayName(), ConfigEntry.CascadedSEGIPreset.GetDisplayName(), SEGIManager.SettingCascadedPreset.Standard);

			ConesEntry = BindSEGI(ConfigEntry.Cones, 13, 1, 128);
			SecondaryConesEntry = BindSEGI(ConfigEntry.SecondaryCones, 6, 3, 16);
			ConeTraceStepsEntry = BindSEGI(ConfigEntry.ConeTraceSteps, 8, 1, 32);
			ConeLengthEntry = BindSEGI(ConfigEntry.ConeLength, 1f, 0.1f, 2.0f);
			ConeWidthEntry = BindSEGI(ConfigEntry.ConeWidth, 6f, 0.5f, 6.0f);
			ConeTraceBiasEntry = BindSEGI(ConfigEntry.ConeTraceBias, 0.63f, 0.0f, 4.0f);

			DoReflectionsEntry = BindSEGI(ConfigEntry.DoReflections, true);
			ReflectionStepsEntry = BindSEGI(ConfigEntry.ReflectionSteps, 64, 12, 128);
			ReflectionOcclusionPowerEntry = BindSEGI(ConfigEntry.ReflectionOcclusionPower, 1f, 0.001f, 4.0f);
			SkyReflectionIntensityEntry = BindSEGI(ConfigEntry.SkyReflectionIntensity, 1f, 0.0f, 1.0f);

			VoxelResolutionEntry = BindSEGI(ConfigEntry.VoxelResolution, SEGI.VoxelResolution.high);
			HalfResolutionEntry = BindSEGI(ConfigEntry.HalfResolution, true);
			VoxelAAEntry = BindSEGI(ConfigEntry.VoxelAA, false);
			StochasticSamplingEntry = BindSEGI(ConfigEntry.StochasticSampling, true);
			UseBilateralFilteringEntry = BindSEGI(ConfigEntry.UseBilateralFiltering, true);
			TemporalBlendWeightEntry = BindSEGI(ConfigEntry.TemporalBlendWeight, 0.15f, 0.01f, 1.0f);
			GaussianMipFilterEntry = BindSEGI(ConfigEntry.GaussianMipFilter, false);

			NearLightGainEntry = BindSEGI(ConfigEntry.NearLightGain, 1f, 0.0f, 4.0f);
			GIGainEntry = BindSEGI(ConfigEntry.GIGain, 1f, 0.0f, 4.0f);
			NearOcclusionStrengthEntry = BindSEGI(ConfigEntry.NearOcclusionStrength, 0f, 0.0f, 4.0f);
			OcclusionPowerEntry = BindSEGI(ConfigEntry.OcclusionPower, 1f, 0.001f, 4.0f);
			OcclusionStrengthEntry = BindSEGI(ConfigEntry.OcclusionStrength, 1f, 0.0f, 4.0f);
			FarOcclusionStrengthEntry = BindSEGI(ConfigEntry.FarOcclusionStrength, 1f, 0.1f, 4.0f);
			FarthestOcclusionStrengthEntry = BindSEGI(ConfigEntry.FarthestOcclusionStrength, 1f, 0.1f, 4.0f);

			InnerOcclusionLayersEntry = BindSEGI(ConfigEntry.InnerOcclusionLayers, 1, 0, 2);
			InfiniteBouncesEntry = BindSEGI(ConfigEntry.InfiniteBounces, true);
			SecondaryBounceGainEntry = BindSEGI(ConfigEntry.SecondaryBounceGain, 1f, 0.0f, 2.0f);
			SecondaryOcclusionStrengthEntry = BindSEGI(ConfigEntry.SecondaryOcclusionStrength, 1f, 0.1f, 4.0f);

			SmallPumpPower = Bind(ConfigSection.AtmosphericPatchesSettings.GetDisplayName(), ConfigEntry.SmallPumpPower.GetDisplayName(), 500f, 
				new ConfigDescription(ConfigEntry.SmallPumpPower.GetDescription(), new AcceptableValueRange<float>(10f, 2000f)));
			LargePumpPower = Bind(ConfigSection.AtmosphericPatchesSettings.GetDisplayName(), ConfigEntry.LargePumpPower.GetDisplayName(), 1500f,
				new ConfigDescription(ConfigEntry.SmallPumpPower.GetDescription(), new AcceptableValueRange<float>(100f, 10000f)));
			AirConditionerPower = Bind(ConfigSection.AtmosphericPatchesSettings.GetDisplayName(), ConfigEntry.AirConditionerPower.GetDisplayName(), 1000f,
				new ConfigDescription(ConfigEntry.AirConditionerPower.GetDescription(), new AcceptableValueRange<float>(100f, 10000f)));
			AirConditionerEfficiency = Bind(ConfigSection.AtmosphericPatchesSettings.GetDisplayName(), ConfigEntry.AirConditionerEfficiency.GetDisplayName(), 4f,
				new ConfigDescription(ConfigEntry.AirConditionerPower.GetDescription(), new AcceptableValueRange<float>(0.5f, 10f)));
		}

		private ConfigEntry<T> BindSEGI<T>(ConfigEntry entry, T defaultValue)
		{
			var description = new ConfigDescription(entry.GetDescription());
			return Bind(ConfigSection.SEGISettings.GetDisplayName(), entry.GetDisplayName(), defaultValue, description);
		}

		private ConfigEntry<T> BindSEGI<T>(ConfigEntry entry, T defaultValue, T from, T to) where T : IComparable
		{
			AcceptableValueRange<T> acceptableValues = new AcceptableValueRange<T>(from, to);
			var description = new ConfigDescription(entry.GetDescription(), acceptableValues);
			return Bind(ConfigSection.SEGISettings.GetDisplayName(), entry.GetDisplayName(), defaultValue, description);
		}
	}
}
