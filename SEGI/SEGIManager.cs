using System;
using Assets.Scripts.UI;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Util;
using Object = UnityEngine.Object;
using Assets.Scripts.Voxel;
using static SEGI;

namespace EntropyFix
{
	public class SEGIManager : MonoBehaviour
	{
		public enum SettingPreset
		{
			[DisplayName("Custom")]
			Custom,
			[DisplayName("Low")]
			Low,
			[DisplayName("Medium")]
			Medium,
			[DisplayName("High")]
			High,
			[DisplayName("Ultra")]
			Ultra,
			[DisplayName("Insane")]
			Insane,
			[DisplayName("Sponza Low")]
			SponzaLow,
			[DisplayName("Sponza Medium")]
			SponzaMedium,
			[DisplayName("Sponza High")]
			SponzaHigh,
			[DisplayName("Sponza Ultra")]
			SponzaUltra,
			[DisplayName("Bright")]
			Bright,
			[DisplayName("Ultra Clean")]
			UltraClean,
		}

		public enum SettingCascadedPreset
		{
			[DisplayName("Custom")]
			Custom,
			[DisplayName("Lite")]
			Lite,
			[DisplayName("Standard")]
			Standard,
			[DisplayName("Accurate")]
			Accurate,
			[DisplayName("Accurate 2")]
			Accurate2,
		}

		private static AssetBundle _bundle;

		private SEGI _currentSEGI;
		private SEGICascaded _currentCascadedSEGI;
		private SEGIPreset _currentPreset;
		private SEGIPreset _currentCascadedPreset;
		private Dictionary<SettingPreset, SEGIPreset> _presetsDictionary = new Dictionary<SettingPreset, SEGIPreset>();
		private Dictionary<SettingCascadedPreset, SEGIPreset> _cascadedPresetsDictionary = new Dictionary<SettingCascadedPreset, SEGIPreset>();

		public static void Enable()
		{
			var manager = Object.FindObjectOfType<SEGIManager>();
			if (manager == null)
			{
				var gameObject = new GameObject("@@SEGI");
				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				gameObject.AddComponent<SEGIManager>();
				Plugin.Log("SEGI enabled");
			}
		}

		public static void Disable()
		{
			var manager = Object.FindObjectOfType<SEGIManager>();
			if (manager != null)
			{
				var go = manager.gameObject;
				manager.DestroyComponent();
				go.DestroyGameObject();
			}
			Plugin.Log("SEGI disabled");
		}

		private void Awake()
		{
			if (_bundle == null)
			{
				var codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
				var uri = new UriBuilder(codeBase);
				var path = Uri.UnescapeDataString(uri.Path);
				var directory = Path.GetDirectoryName(path);
				var pathToBundle = Path.Combine(directory, "Content", "SEGI.asset");
				_bundle = AssetBundle.LoadFromFile(pathToBundle);
				if (_bundle == null)
					throw new ApplicationException("Unable to find \"SEGI.asset\" file!");
				LoadPresets();
			}
			SEGI.Bundle = _bundle;
			SEGICascaded.Bundle = _bundle;
			EnsureCurrentSEGI();
		}

		private void OnDestroy()
		{
			if (Camera.main)
			{
				var segi = Camera.main.gameObject.GetComponent<SEGI>();
				var segiCascaded = Camera.main.gameObject.GetComponent<SEGICascaded>();
				segi?.DestroyComponent();
				segiCascaded?.DestroyComponent();
			}
			_bundle.Unload(true);
			_bundle = null;
			RenderSettings.ambientIntensity = 1;
		}

		private void EnsureCurrentSEGI()
		{
			if(!Camera.main) return;
			if (Plugin.Config.CascadedSEGI)
			{
				if(!_currentCascadedSEGI)
					_currentCascadedSEGI = Camera.main.gameObject.GetComponent<SEGICascaded>();
				else if (_currentCascadedSEGI.gameObject != Camera.main.gameObject)
				{
					_currentCascadedSEGI.DestroyComponent();
					_currentCascadedSEGI = Camera.main.gameObject.GetComponent<SEGICascaded>();
					_currentCascadedSEGI.ApplyPreset(_currentCascadedPreset);
				}
				if (!_currentCascadedSEGI)
				{
					_currentCascadedSEGI = Camera.main.gameObject.AddComponent<SEGICascaded>();
					_currentCascadedSEGI.ApplyPreset(_currentCascadedPreset);
				}
				if (_currentSEGI)
				{
					_currentSEGI.enabled = false;
					_currentSEGI.DestroyComponent();
				}
				_currentCascadedSEGI.enabled = true;
			}
			else
			{
				if(!_currentSEGI)
					_currentSEGI = Camera.main.gameObject.GetComponent<SEGI>();
				else if (_currentSEGI.gameObject != Camera.main.gameObject)
				{
					_currentSEGI.DestroyComponent();
					_currentSEGI = Camera.main.gameObject.GetComponent<SEGI>();
					_currentSEGI.ApplyPreset(_currentPreset);
				}
				if (!_currentSEGI)
				{
					_currentSEGI = Camera.main.gameObject.AddComponent<SEGI>();
					_currentSEGI.ApplyPreset(_currentPreset);
				}
				if (_currentCascadedSEGI)
				{
					_currentCascadedSEGI.enabled = false;
					_currentCascadedSEGI.DestroyComponent();
				}
				_currentSEGI.enabled = true;
			}
			RenderSettings.ambientIntensity = 0;
			if(!_currentCascadedSEGI && !_currentSEGI)
				throw new ApplicationException("Unable to initialize SEGI!");
		}

		private void LoadPresets()
		{
			if (_bundle == null)
			{
				throw new ApplicationException("Unable to find \"SEGI.asset\" file!");
			}
			foreach (var preset in Enum.GetValues(typeof(SettingPreset)).Cast<SettingPreset>())
				if (preset != SettingPreset.Custom)
				{
					var asset = _bundle.LoadAsset<SEGIPreset>(preset.GetDisplayName());
					if (asset == null)
						Debug.Log($"Unable to find preset {preset.GetDisplayName()} in \"SEGI.asset\" file");
					_presetsDictionary.Remove(preset);
					_presetsDictionary.Add(preset, asset);
				}
			_presetsDictionary.Add(SettingPreset.Custom, _currentPreset = ScriptableObject.CreateInstance<SEGIPreset>());

			foreach (var preset in Enum.GetValues(typeof(SettingCascadedPreset)).Cast<SettingCascadedPreset>())
				if (preset != SettingCascadedPreset.Custom)
				{
					var asset = _bundle.LoadAsset<SEGIPreset>(preset.GetDisplayName());
					if (asset == null)
						Debug.Log($"Unable to find preset {preset.GetDisplayName()} in \"SEGI.asset\" file");
					_cascadedPresetsDictionary.Remove(preset);
					_cascadedPresetsDictionary.Add(preset, asset);
				}
			_cascadedPresetsDictionary.Add(SettingCascadedPreset.Custom, _currentCascadedPreset = ScriptableObject.CreateInstance<SEGIPreset>());
		}

		private void UpdateSettings()
		{
			SEGIPreset preset;
			bool customPreset = Plugin.Config.SEGICascadedPreset == SettingCascadedPreset.Custom;
			if (Plugin.Config.CascadedSEGI)
			{
				preset = _cascadedPresetsDictionary[Plugin.Config.SEGICascadedPreset];
				if (preset == null)
					LoadPresets();
				preset = _cascadedPresetsDictionary[Plugin.Config.SEGICascadedPreset];
				if (preset == null)
					throw new ApplicationException("Unable to find preset!");
				if (preset != _currentCascadedPreset)
				{
					_currentCascadedPreset = preset;
					UpdateConfigFromPreset(preset);
					Plugin.Log($"{Plugin.Config.SEGICascadedPreset} preset applied");
				}
				else if (customPreset)
				{
					UpdatePresetFromConfig(_currentCascadedPreset);
				}
				_currentCascadedSEGI.ApplyPreset(_currentCascadedPreset);
			}
			else
			{
				preset = _presetsDictionary[Plugin.Config.SEGIPreset];
				if (preset == null)
					LoadPresets();
				preset = _presetsDictionary[Plugin.Config.SEGIPreset];
				if (preset == null)
					throw new ApplicationException("Unable to find preset!");
				if (preset != _currentPreset)
				{
					_currentPreset = preset;
					UpdateConfigFromPreset(preset);
					Plugin.Log($"{Plugin.Config.SEGIPreset} preset applied");
				} 
				else if (customPreset)
				{
					UpdatePresetFromConfig(_currentPreset);
				}
				_currentSEGI.ApplyPreset(_currentPreset);
			}
		}

		private void UpdateConfigFromPreset(SEGIPreset preset)
		{
			Plugin.Config.VoxelResolution = preset.voxelResolution;
			Plugin.Config.VoxelAA = preset.voxelAA;
			Plugin.Config.InnerOcclusionLayers = preset.innerOcclusionLayers;
			Plugin.Config.InfiniteBounces = preset.infiniteBounces;
			Plugin.Config.TemporalBlendWeight = preset.temporalBlendWeight;
			Plugin.Config.UseBilateralFiltering = preset.useBilateralFiltering;
			Plugin.Config.HalfResolution = preset.halfResolution;
			Plugin.Config.StochasticSampling = preset.stochasticSampling;
			Plugin.Config.DoReflections = preset.doReflections;
			Plugin.Config.Cones = preset.cones;
			Plugin.Config.ConeTraceSteps = preset.coneTraceSteps;
			Plugin.Config.ConeLength = preset.coneLength;
			Plugin.Config.ConeWidth = preset.coneWidth;
			Plugin.Config.ConeTraceBias = preset.coneTraceBias;
			Plugin.Config.OcclusionStrength = preset.occlusionStrength;
			Plugin.Config.NearOcclusionStrength = preset.nearOcclusionStrength;
			Plugin.Config.OcclusionPower = preset.occlusionPower;
			Plugin.Config.NearLightGain = preset.nearLightGain;
			Plugin.Config.GIGain = preset.giGain;
			Plugin.Config.SecondaryBounceGain = preset.secondaryBounceGain;
			Plugin.Config.ReflectionSteps = preset.reflectionSteps;
			Plugin.Config.ReflectionOcclusionPower = preset.reflectionOcclusionPower;
			Plugin.Config.SkyReflectionIntensity = preset.skyReflectionIntensity;
			Plugin.Config.GaussianMipFilter = preset.gaussianMipFilter;
			Plugin.Config.FarOcclusionStrength = preset.farOcclusionStrength;
			Plugin.Config.FarthestOcclusionStrength = preset.farthestOcclusionStrength;
			Plugin.Config.SecondaryCones = preset.secondaryCones;
			Plugin.Config.SecondaryOcclusionStrength = preset.secondaryOcclusionStrength;
		}

		void UpdatePresetFromConfig(SEGIPreset preset)
		{
			preset.voxelResolution = Plugin.Config.VoxelResolution;
			preset.voxelAA = Plugin.Config.VoxelAA;
			preset.innerOcclusionLayers = Plugin.Config.InnerOcclusionLayers;
			preset.infiniteBounces = Plugin.Config.InfiniteBounces;
			preset.temporalBlendWeight = Plugin.Config.TemporalBlendWeight;
			preset.useBilateralFiltering = Plugin.Config.UseBilateralFiltering;
			preset.halfResolution = Plugin.Config.HalfResolution;
			preset.stochasticSampling = Plugin.Config.StochasticSampling;
			preset.doReflections = Plugin.Config.DoReflections;
			preset.cones = Plugin.Config.Cones;
			preset.coneTraceSteps = Plugin.Config.ConeTraceSteps;
			preset.coneLength = Plugin.Config.ConeLength;
			preset.coneWidth = Plugin.Config.ConeWidth;
			preset.coneTraceBias = Plugin.Config.ConeTraceBias;
			preset.occlusionStrength = Plugin.Config.OcclusionStrength;
			preset.nearOcclusionStrength = Plugin.Config.NearOcclusionStrength;
			preset.occlusionPower = Plugin.Config.OcclusionPower;
			preset.nearLightGain = Plugin.Config.NearLightGain;
			preset.giGain = Plugin.Config.GIGain;
			preset.secondaryBounceGain = Plugin.Config.SecondaryBounceGain;
			preset.reflectionSteps = Plugin.Config.ReflectionSteps;
			preset.reflectionOcclusionPower = Plugin.Config.ReflectionOcclusionPower;
			preset.skyReflectionIntensity = Plugin.Config.SkyReflectionIntensity;
			preset.gaussianMipFilter = Plugin.Config.GaussianMipFilter;
			preset.farOcclusionStrength = Plugin.Config.FarOcclusionStrength;
			preset.farthestOcclusionStrength = Plugin.Config.FarthestOcclusionStrength;
			preset.secondaryCones = Plugin.Config.SecondaryCones;
			preset.secondaryOcclusionStrength = Plugin.Config.SecondaryOcclusionStrength;
		}

		private void Update()
		{

			if (Plugin.Config.CascadedSEGI)
			{
				if (!_currentCascadedSEGI)
					EnsureCurrentSEGI();
				UpdateSettings();
				_currentCascadedSEGI.sun = WorldManager.Instance.WorldSun?.TargetLight;
			}
			else
			{
				if (!_currentSEGI)
					EnsureCurrentSEGI();
				UpdateSettings();
				_currentSEGI.sun = WorldManager.Instance.WorldSun?.TargetLight;
			}
		}
	}
}