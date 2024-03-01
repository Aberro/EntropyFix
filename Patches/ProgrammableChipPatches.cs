using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Scripts;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using EntropyFix.Processor;
using HarmonyLib;

namespace EntropyFix
{
	[HarmonyPatch(typeof(InputSourceCode), nameof(InputSourceCode.Initialize))]
    [HarmonyPatchCategory(PatchCategory.ProgrammableChipPatches)]
    public class InputSourceCode_Initialize_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Plugin.Log("Applying transpilation to Initialize method...");
            foreach (var instruction in instructions)
            {
                if (instruction.operand is 128)
                {
                    instruction.operand = 1024;
                }
                yield return instruction;
            }
            Plugin.Log("Initialize method is updated.");
        }
    }

    [HarmonyPatch(typeof(InputSourceCode), "RemoveLine")]
    [HarmonyPatchCategory(PatchCategory.ProgrammableChipPatches)]
    public class InputSourceCode_RemoveLine_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Plugin.Log("Applying transpilation to RemoveLine method...");
            foreach (var instruction in instructions)
            {
                if (instruction.operand is 128)
                {
                    instruction.operand = 1024;
                }
                yield return instruction;
            }
            Plugin.Log("RemoveLine method is updated.");
        }
    }

    [HarmonyPatch(typeof(InputSourceCode), "HandleInput")]
    [HarmonyPatchCategory(PatchCategory.ProgrammableChipPatches)]
    public class InputSourceCode_HandleInput_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Plugin.Log("Applying transpilation to HandleInput method...");
            // Here we need to look forward, so...
            CodeInstruction prevInstruction = null;
            bool replace = true;
            foreach (var instruction in instructions)
            {
                // We need to be sure that the next instruction after ldc.i4.s is not a call, the static call (to GetKeyDown).
                // If it is, we ignore updating prevInstruction.
                replace = instruction.opcode != OpCodes.Call;
                if (replace && prevInstruction != null && prevInstruction.opcode == OpCodes.Ldc_I4_S && prevInstruction.operand is (sbyte) 127)
                {
                    prevInstruction.opcode = OpCodes.Ldc_I4;
                    prevInstruction.operand = 1023;
                }
                if(prevInstruction != null)
                    yield return prevInstruction;
                prevInstruction = instruction;
            }
            yield return prevInstruction;
            Plugin.Log("HandleInput method is updated.");
        }
    }

    [HarmonyPatch(typeof(InputSourceCode), "UpdateFileSize")]
    [HarmonyPatchCategory(PatchCategory.ProgrammableChipPatches)]
    public class InputSourceCode_UpdateFileSize_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is 4096)
                {
                    instruction.operand = 65536;
                }
                yield return instruction;
            }
        }
    }

    [HarmonyPatch(typeof(ProgrammableChip), nameof(ProgrammableChip.SetSourceCode), typeof(string))]
    [HarmonyPatchCategory(PatchCategory.ProgrammableChipReplacement)]
    public class ProgrammableChip_SetSourceCode_Patch
    {
        public static void Postfix(ProgrammableChip __instance, string sourceCode)
        {
            var processor = ChipProcessor.Translate(__instance, 127);
            __instance.SetExtension(processor);
        }
    }

    [HarmonyPatch(typeof(ProgrammableChip), nameof(ProgrammableChip.Execute))]
    [HarmonyPatchCategory(PatchCategory.ProgrammableChipReplacement)]
    public class ProgrammableChip_Execute_Patch
    {
        public static bool Prefix(ProgrammableChip __instance, int runCount)
        {
            var processor = __instance.GetExtension<ProgrammableChip, ChipProcessor>();
            if (processor == null)
            {
                processor = ChipProcessor.Translate(__instance, 127);
                __instance.SetExtension(processor);
            }
            processor.Execute(runCount);
            return false;
        }
    }

    [HarmonyPatch(typeof(ProgrammableChip), nameof(ProgrammableChip.SerializeSave))]
    [HarmonyPatchCategory(PatchCategory.ProgrammableChipReplacement)]
    public class ProgrammableChip_SerializeSave_Patch
    {
        public static bool Prefix(ProgrammableChip __instance, ref ThingSaveData __result)
        {
            var processor = __instance.GetExtension<ProgrammableChip, ChipProcessor>();
            if (processor == null)
                return true;
            ChipProcessorSaveData chipProcessorSaveData;
            ThingSaveData savedData = (ThingSaveData)(chipProcessorSaveData = new ChipProcessorSaveData());
            var method = __instance.GetType().GetMethod("InitialiseSaveData", BindingFlags.NonPublic | BindingFlags.Instance);
            object[] parameters = new object[] { savedData };
            method.Invoke(__instance, parameters);
            savedData = (ThingSaveData)parameters[0]; // Update savedData with the potentially modified value
            chipProcessorSaveData.SleepDuration = __instance.GetExtension<ProgrammableChip, ChipProcessor>().SleepDuration;
            chipProcessorSaveData.Slept = GameManager.GameTime - __instance.GetExtension<ProgrammableChip, ChipProcessor>().SleptAt;
            __result = savedData;
            return false;
        }
    }

    [HarmonyPatch(typeof(ProgrammableChip), nameof(ProgrammableChip.DeserializeSave))]
    [HarmonyPatchCategory(PatchCategory.ProgrammableChipReplacement)]
    public class ProgrammableChip_DeserializeSave_Patch
    {
        public static void Postfix(ProgrammableChip __instance, ThingSaveData savedData)
        {
            if(savedData is not ChipProcessorSaveData chipProcessorSaveData)
                return;
            var processor = __instance.GetExtension<ProgrammableChip, ChipProcessor>();
            if (processor == null)
            {
                processor = ChipProcessor.Translate(__instance, 127);
                __instance.SetExtension(processor);
            }
            var traverse = Traverse.Create(processor);
            traverse.Field<double>("_sleepDuration").Value = ((ChipProcessorSaveData)savedData).SleepDuration;
            // We need to signal to the processor that the SleptAt value was deserialized, so we use negative value.
            traverse.Field<double>("_sleptAt").Value = -((ChipProcessorSaveData)savedData).Slept;
        }
    }

    [HarmonyPatch(typeof(XmlSaveLoad), nameof(XmlSaveLoad.AddExtraTypes))]
    public class XmlSaveLoad_AddExtraTypes_Patch
    {
        public static void Postfix(List<Type> extraTypes)
        {
            extraTypes.Add(typeof(ChipProcessorSaveData));
        }
    }

    //[HarmonyPatch]
    public static class ProgrammableChip_S_OperationExecutePatch
	{
		public static MethodBase TargetMethod()
		{
			Type privateClassType = typeof(ProgrammableChip).GetNestedType("_S_Operation", BindingFlags.NonPublic);
			if (privateClassType == null)
				throw new ApplicationException("Cannot find _S_Operation class!");
			var result = privateClassType.GetMethod("Execute", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			if (result == null)
				throw new ApplicationException("Cannot find _S_Operation.Execute method!");
			return result;
		}

		public static void Postfix(object __instance, int index, ProgrammableChip ____Chip, object ____DeviceIndex, object ____Argument1, object ____LogicType, ref int __result)
		{
			Type aliasTargetType = typeof(ProgrammableChip).GetNestedType("_AliasTarget", BindingFlags.NonPublic);
			int aliasTargetDevice = (int)aliasTargetType.GetField("Device", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
			int aliasTargetRegister = (int)aliasTargetType.GetField("Register", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

			// Then, call the methods on the traversed objects
			int variableIndex = Traverse.Create(____DeviceIndex).Method("GetVariableIndex", aliasTargetDevice, true).GetValue<int>();
			double variableValue1 = Traverse.Create(____Argument1).Method("GetVariableValue", aliasTargetRegister).GetValue<double>();
			LogicType variableValue2 = Traverse.Create(____LogicType).Method("GetVariableValue", aliasTargetRegister).GetValue<LogicType>();

			int networkIndex = Traverse.Create(____DeviceIndex).Method("GetNetworkIndex").GetValue<int>();
			ILogicable logicable = Traverse.Create(____Chip).Property("CircuitHousing").Method("GetLogicable", variableIndex, networkIndex).GetValue<ILogicable>();

#pragma warning disable CS0252 // Possible unintended reference comparison; left hand side needs cast
            if (logicable == ____Chip && variableValue2 == LogicType.On && variableValue1 == 0)
			{
				__result = -index - 1;
			}
#pragma warning restore CS0252 // Possible unintended reference comparison; left hand side needs cast
            //int variableIndex = ____DeviceIndex.GetVariableIndex(ProgrammableChip._AliasTarget.Device, true);
            //double variableValue1 = ____Argument1.GetVariableValue(ProgrammableChip._AliasTarget.Register);
            //LogicType variableValue2 = ____LogicType.GetVariableValue(ProgrammableChip._AliasTarget.Register);
            //int networkIndex = ____DeviceIndex.GetNetworkIndex();
            //ILogicable logicable = ____Chip.CircuitHousing.GetLogicable(variableIndex, networkIndex);
        }
	}
}
