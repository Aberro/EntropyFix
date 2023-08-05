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
		public enum ConfigSEGIEntry
		{
			[DisplayName("Cascaded SEGI")]
			[Description("Works better at long distances, but has fewer presets and has some missing features.")]
			CascadedSEGI,
			[DisplayName("SEGI preset")]
			[Description("Preconfigured settings for SEGI.")]
			SEGIPreset,
			[DisplayName("Cascaded SEGI preset")]
			[Description("Preconfigured settings for Cascaded SEGI.")]
			CascadedSEGIPreset,
			VoxelResolution,
			VoxelAA,
			InnerOcclusionLayers,
			InfiniteBounces,
			TemporalBlendWeight,
			UseBilateralFiltering,
			HalfResolution,
			StochasticSampling,
			DoReflections,
			Cones,
			ConeTraceSteps,
			ConeLength,
			ConeWidth,
			ConeTraceBias,
			OcclusionStrength,
			NearOcclusionStrength,
			OcclusionPower,
			NearLightGain,
			GIGain,
			SecondaryBounceGain,
			ReflectionSteps,
			ReflectionOcclusionPower,
			SkyReflectionIntensity,
			GaussianMipFilter,
			FarOcclusionStrength,
			FarthestOcclusionStrength,
			SecondaryCones,
			SecondaryOcclusionStrength,
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
				Harmony harmony = null;
				if (!Features.TryGetValue(category, out feature))
				{
					feature = Bind(ConfigSection.Features.GetDisplayName(), category.GetDisplayName(), true, category.GetDescription());
					Features.Add(category, feature);
				}
			}
			CascadedSEGIEntry = Bind(ConfigSection.SEGI.GetDisplayName(), ConfigSEGIEntry.CascadedSEGI.GetDisplayName(), false);
			SEGIPresetEntry = Bind(ConfigSection.SEGI.GetDisplayName(), ConfigSEGIEntry.SEGIPreset.GetDisplayName(), SEGIManager.SettingPreset.High);
			SEGICascadedPresetEntry = Bind(ConfigSection.SEGI.GetDisplayName(), ConfigSEGIEntry.CascadedSEGIPreset.GetDisplayName(), SEGIManager.SettingCascadedPreset.Standard);

			VoxelResolutionEntry = Bind(ConfigSEGIEntry.VoxelResolution, SEGI.VoxelResolution.high);
			VoxelAAEntry = Bind(ConfigSEGIEntry.VoxelAA, false);
			InnerOcclusionLayersEntry = Bind(ConfigSEGIEntry.InnerOcclusionLayers, 1, 0, 2);
			InfiniteBouncesEntry = Bind(ConfigSEGIEntry.InfiniteBounces, true);
			TemporalBlendWeightEntry = Bind(ConfigSEGIEntry.TemporalBlendWeight, 0.15f, 0.01f, 1.0f);
			UseBilateralFilteringEntry = Bind(ConfigSEGIEntry.UseBilateralFiltering, true);
			HalfResolutionEntry = Bind(ConfigSEGIEntry.HalfResolution, true);
			StochasticSamplingEntry = Bind(ConfigSEGIEntry.StochasticSampling, true);
			DoReflectionsEntry = Bind(ConfigSEGIEntry.DoReflections, true);
			ConesEntry = Bind(ConfigSEGIEntry.Cones, 13, 1, 128);
			ConeTraceStepsEntry = Bind(ConfigSEGIEntry.ConeTraceSteps, 8, 1, 32);
			ConeLengthEntry = Bind(ConfigSEGIEntry.ConeLength, 1f, 0.1f, 2.0f);
			ConeWidthEntry = Bind(ConfigSEGIEntry.ConeWidth, 6f, 0.5f, 6.0f);
			ConeTraceBiasEntry = Bind(ConfigSEGIEntry.ConeTraceBias, 0.63f, 0.0f, 4.0f);
			OcclusionStrengthEntry = Bind(ConfigSEGIEntry.OcclusionStrength, 1f, 0.0f, 4.0f);
			NearOcclusionStrengthEntry = Bind(ConfigSEGIEntry.NearOcclusionStrength, 0f, 0.0f, 4.0f);
			OcclusionPowerEntry = Bind(ConfigSEGIEntry.OcclusionPower, 1f, 0.001f, 4.0f);
			NearLightGainEntry = Bind(ConfigSEGIEntry.NearLightGain, 1f, 0.0f, 4.0f);
			GIGainEntry = Bind(ConfigSEGIEntry.GIGain, 1f, 0.0f, 4.0f);
			SecondaryBounceGainEntry = Bind(ConfigSEGIEntry.SecondaryBounceGain, 1f, 0.0f, 2.0f);
			ReflectionStepsEntry = Bind(ConfigSEGIEntry.ReflectionSteps, 64, 12, 128);
			ReflectionOcclusionPowerEntry = Bind(ConfigSEGIEntry.ReflectionOcclusionPower, 1f, 0.001f, 4.0f);
			SkyReflectionIntensityEntry = Bind(ConfigSEGIEntry.SkyReflectionIntensity, 1f, 0.0f, 1.0f);
			GaussianMipFilterEntry = Bind(ConfigSEGIEntry.GaussianMipFilter, false);
			FarOcclusionStrengthEntry = Bind(ConfigSEGIEntry.FarOcclusionStrength, 1f, 0.1f, 4.0f);
			FarthestOcclusionStrengthEntry = Bind(ConfigSEGIEntry.FarthestOcclusionStrength, 1f, 0.1f, 4.0f);
			SecondaryConesEntry = Bind(ConfigSEGIEntry.SecondaryCones, 6, 3, 16);
			SecondaryOcclusionStrengthEntry = Bind(ConfigSEGIEntry.SecondaryOcclusionStrength, 1f, 0.1f, 4.0f);
		}

		private ConfigEntry<T> Bind<T>(ConfigSEGIEntry entry, T defaultValue)
		{
			var description = new ConfigDescription(entry.GetDescription());
			return Bind(ConfigSection.SEGISettings.GetDisplayName(), entry.GetDisplayName(), defaultValue, description);
		}

		private ConfigEntry<T> Bind<T>(ConfigSEGIEntry entry, T defaultValue, T from, T to) where T : IComparable
		{
			AcceptableValueRange<T> acceptableValues = new AcceptableValueRange<T>(from, to);
			var description = new ConfigDescription(entry.GetDescription(), acceptableValues);
			return Bind(ConfigSection.SEGISettings.GetDisplayName(), entry.GetDisplayName(), defaultValue, description);
		}
	}
}
