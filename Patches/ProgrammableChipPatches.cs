using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;

namespace EntropyFix
{
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

			if (logicable == ____Chip && variableValue2 == LogicType.On && variableValue1 == 0)
			{
				__result = -index - 1;
			}
			//int variableIndex = ____DeviceIndex.GetVariableIndex(ProgrammableChip._AliasTarget.Device, true);
			//double variableValue1 = ____Argument1.GetVariableValue(ProgrammableChip._AliasTarget.Register);
			//LogicType variableValue2 = ____LogicType.GetVariableValue(ProgrammableChip._AliasTarget.Register);
			//int networkIndex = ____DeviceIndex.GetNetworkIndex();
			//ILogicable logicable = ____Chip.CircuitHousing.GetLogicable(variableIndex, networkIndex);
		}
	}
}
