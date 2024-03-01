
using System;
using System.ComponentModel;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Assets.Scripts.Util;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Assets.Scripts;
using Cysharp.Threading.Tasks;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Networks;
using Assets.Scripts.Objects.Pipes;

namespace EntropyFix
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	[BepInProcess("rocketstation.exe")]
	public class Plugin : BaseUnityPlugin
	{
		public const string PluginGuid = "net.aberro.stationeers.entropyfix";
		public const string PluginName = "Entropy Fix";
		public const string PluginVersion = "0.2";

		private static readonly Dictionary<PatchCategory, HarmonyPatchInfo[]> Patches;

		public new static PluginConfigFile Config { get; private set; }

		static Plugin()
		{
			var patches = AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()).Select(HarmonyPatchInfo.Create);
			Patches = patches.Where(p => p != null)
				.GroupBy(patch => patch.Category)
				.ToDictionary(group => group.Key, group => group.ToArray());
		}
		public static void Log(string line)
		{
			Debug.Log("[" + PluginName + "]: " + line);
		}
		public static void LogWarning(string line)
		{
			Debug.LogWarning("[" + PluginName + "]: " + line);
		}
		public static void LogError(string line)
		{
			Debug.LogError("[" + PluginName + "]: " + line);
		}
		void Awake()
		{
			Log("Initializing...");
			Config = new PluginConfigFile(base.Config.ConfigFilePath, true);
			// Force updating backing field
			var backingField = typeof(BaseUnityPlugin).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).First(x => x.Name.Contains(nameof(BaseUnityPlugin.Config)));
			if (backingField == null)
				throw new Exception("Could not find backing field for Config property");
			backingField.SetValue(this, Config);
			try
			{
				Configure();
				Config.SettingChanged += (_1,_2) => Configure();
				Log("Patching done.");
			}
			catch (Exception e)
			{
				Log("Patching failed:");
				Log(e.ToString());
			}
		}

		private static void Configure()
		{
			foreach(PatchCategory category in Enum.GetValues(typeof(PatchCategory)))
			{
				Harmony harmony = null;

				var enabled = category == PatchCategory.None || Config.Features[category].Value;
				if (category == PatchCategory.None || enabled)
				{
					if (Patches.TryGetValue(category, out var patches))
					{
						harmony ??= new Harmony($"{PluginGuid}.{category}");
						bool patched = false;
						foreach (var patch in patches)
						{
							patched |= patch.Patch(harmony);
						}
						if (category != PatchCategory.None && patched)
							Log($"`{category.GetDisplayName()}' feature enabled.");
					}
				}
				else
				{
					if (Patches.TryGetValue(category, out var patches))
					{
						harmony ??= new Harmony($"{PluginGuid}.{category}");
						bool unpatched = false;
						foreach (var patch in patches)
						{
							unpatched |= patch.Unpatch(harmony);
						}
						harmony.UnpatchSelf(); // Just to ensure
						if (category != PatchCategory.None && unpatched)
							Log($"`{category.GetDisplayName()}' feature disabled.");
					}
				}
			}
			if (Config.Features[PatchCategory.SEGI].Value)
				SEGIManager.Enable();
			else
				SEGIManager.Disable();
		}
	}

	[HarmonyPatch(typeof(ManagerBase), "ManagerAwake")]
	public class EntryPointPatch
	{
		public static bool Initialized { get; private set; }
		public static void Postfix()
		{
			if (Initialized)
				return;
			Plugin.Log("EntryPoint initialization...");
			if (Plugin.Config.Features[PatchCategory.SEGI].Value)
			{
				SEGIManager.Enable();
			}
			var man = UnityEngine.Object.FindObjectOfType<ConfigurationManager.ConfigurationManager>();
			if (man == null)
			{
				var go = new GameObject("@@ConfigurationManager");
				UnityEngine.Object.DontDestroyOnLoad(go);
				go.AddComponent<ConfigurationManager.ConfigurationManager>();
				Plugin.Log("Configuration Manager not found, creating a new one");
			}
			Initialized = true;
		}
	}

	[HarmonyPatch(typeof(MoleHelper), nameof(MoleHelper.LogMessage))]
	public static class MoleHelperLogMessagePatch
	{
		public static bool Prefix(string errorMessage)
		{
			Plugin.LogError($"error atmos thread: {errorMessage}");
			return false;
		}
	}
}
