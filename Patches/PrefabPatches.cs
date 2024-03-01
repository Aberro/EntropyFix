using System.Collections.Generic;
using Assets.Scripts.Objects;
using Assets.Scripts.Util;
using HarmonyLib;
using System.Diagnostics;
using System.Linq;
using Assets.Scripts.Objects.Items;
using Util;
using EntropyFix.Assets.Scripts.Objects.Items;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace EntropyFix.Patches;

[HarmonyPatch(typeof(Prefab), nameof(Prefab.LoadAll))]
public class PrefabPatches
{
    public static void Prefix()
    {
        DebugCartridge.PrefabGeneration = true;
        var prototype = WorldManager.Instance.SourcePrefabs.FirstOrDefault(p => p.name == "CartridgeConfiguration");
        var prefab = UnityEngine.Object.Instantiate(prototype.gameObject);
        prefab.hideFlags = HideFlags.HideAndDontSave;
        Object.DontDestroyOnLoad(prefab);
        prefab.name = "DebugCartridge";
        var prototypeCartridge = prefab.GetComponent<ConfigCartridge>();
        var cartridge = prefab.AddComponent<DebugCartridge>();
        cartridge.Slots = [];
        var traverse = Traverse.Create(cartridge);
        var prototypeTraverse = Traverse.Create(prototypeCartridge);
        traverse.Field<TextMeshProUGUI>("_displayTextMesh").Value = prototypeTraverse.Field<TextMeshProUGUI>("_displayTextMesh").Value;
        prototypeCartridge.DestroyComponent();
        WorldManager.Instance.SourcePrefabs.Add(cartridge);
        DebugCartridge.PrefabGeneration = false;
    }
}