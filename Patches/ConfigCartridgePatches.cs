using Assets.Scripts.Objects.Items;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntropyFix.Patches
{
    /// <summary>
    /// A patch to see all internal atmospheres in devices that have more than one.
    /// </summary>
    [HarmonyPatch(typeof(ConfigCartridge), nameof(ConfigCartridge.ReadLogicText))]
    [HarmonyPatchCategory(PatchCategory.ConfigCartridgeDebug)]
    public class ConfigCartridge_ReadLogicText_Patch
    {

    }
}
