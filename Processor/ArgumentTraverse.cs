using Assets.Scripts;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using static Assets.Scripts.Util.Defines;

namespace EntropyFix.Processor
{
    public partial class ChipProcessor
    {
        public struct ArgumentTraverse
        {
            private object lineOfCode;
            private string instruction;
            private Traverse traverse;
            private List<string> aliases;
            private List<string> defines;
            private List<double> constants;
            public ArgumentTraverse(object lineOfCode, string instruction, Traverse traverse, List<string> aliases, List<string> defines)
            {
                this.lineOfCode = lineOfCode;
                this.instruction = instruction ?? throw new ArgumentNullException(nameof(instruction));
                this.traverse = traverse;
                this.aliases = aliases;
                this.defines = defines;
            }

            /// <summary>
            /// A special kind of variable that is used for alias instruction alone, it is used to store the alias index and the device or register index.
            /// The variable kind is set to alias target type.
            /// There's a special case for alias of an alias, in this case the index of target alias is stored in the constant index.
            /// </summary>
            public Variable Alias(string aliasFieldName, string targetTypeFieldName, string targetFieldName, int lineNumber)
            {
                var value = traverse.Field<string>(aliasFieldName).Value;
                var targetTypeVal = traverse.Field(targetTypeFieldName).GetValue();
                var targetType = (AliasTarget)Convert.ToInt32(targetTypeVal);

                var target = traverse.Field(targetFieldName);
                var registerIndex = target.Field<int>("_RegisterIndex").Value;
                var registerRecurse = target.Field<int>("_RegisterRecurse").Value;
                var alias = target.Field<string>("_Alias")?.Value;
                var deviceIndex = target.Field<int>("_DeviceIndex").Value;
                var deviceRecurse = target.Field<int>("_DeviceRecurse").Value;
                var deviceNetwork = target.Field<int>("_DeviceNetwork").Value;
                var index = aliases.IndexOf(value);
                if (index < 0)
                {
                    index = aliases.Count;
                    aliases.Add(value);
                }
                return targetType switch
                {
                    AliasTarget.Device =>
                        new Variable()
                        {
                            Kind = VariableKind.DeviceRegister,
                            AliasIndex = (short)index,
                            RegisterIndex = (short)deviceIndex,
                            RegisterRecurse = (short)(deviceRecurse-1),
                            NetworkIndex = (short)deviceNetwork,
                            Constant = alias != null ? aliases.IndexOf(alias) : throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, lineNumber),
                        },
                    AliasTarget.Register =>
                        new Variable()
                        {
                            Kind = VariableKind.Register,
                            AliasIndex = (short)index,
                            RegisterIndex = (short)registerIndex,
                            RegisterRecurse = (short)(registerRecurse-1),
                            Constant = alias != null ? aliases.IndexOf(alias) : throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, lineNumber)
                        }
                };
            }

            private string[] ParseLine(string line)
            {
                string masterString = line.IndexOf('#') < 0 ? line : line.Substring(0, line.IndexOf('#'));
                Localization.RegexResult hashPreprocessing = Localization.GetMatchesForHashPreprocessing(ref masterString);
                for (int index = 0; index < hashPreprocessing.Count(); ++index)
                    masterString = masterString.Replace(hashPreprocessing.GetFull(index), UnityEngine.Animator.StringToHash(hashPreprocessing.GetName(index)).ToString());
                Localization.RegexResult binaryPreprocessing = Localization.GetMatchesForBinaryPreprocessing(ref masterString);
                for (int index = 0; index < binaryPreprocessing.Count(); ++index)
                {
                    string str = binaryPreprocessing.GetName(index).Replace("_", "");
                    masterString = masterString.Replace(binaryPreprocessing.GetFull(index), Convert.ToInt64(str, 2).ToString());
                }
                Localization.RegexResult hexPreprocessing = Localization.GetMatchesForHexPreprocessing(ref masterString);
                for (int index = 0; index < hexPreprocessing.Count(); ++index)
                {
                    string str = hexPreprocessing.GetName(index).Replace("_", "");
                    masterString = masterString.Replace(hexPreprocessing.GetFull(index), Convert.ToInt64(str, 16).ToString());
                }
                string[] source = masterString.Split();
                for (int index = source.Length - 1; index >= 0; --index)
                {
                    if (string.IsNullOrEmpty(source[index]))
                        source = source.RemoveAt(index);
                }
                return source;
            }

            /// <summary>
            /// Variable should be a device index or an alias.
            /// </summary>
            public Variable Device(string fieldName, int lineNumber)
            {
                var value = traverse.Field(fieldName);
                var deviceIndex = value.Field<int>("_DeviceIndex").Value;
                var recurse = value.Field<int>("_DeviceRecurse").Value;
                var deviceNetwork = value.Field<int>("_DeviceNetwork").Value;
                var instructionInclude = value.Field<InstructionInclude>("_PropertiesToUse").Value;
                var alias = value.Field<string>("_Alias")?.Value;
                return deviceIndex >= 0
                    ? ReturnDeviceRegisterVariable(deviceIndex, deviceNetwork, recurse, lineNumber)
                    : ReturnAliasVariable(alias, lineNumber, deviceIndex, recurse);
            }

            /// <summary>
            /// Variable should be a register or an alias.
            /// </summary>
            public Variable Store(string fieldName, int lineNumber)
            {
                var value = traverse.Field(fieldName);
                var registerIndex = value.Field<int>("_RegisterIndex").Value;
                var recurse = value.Field<int>("_RegisterRecurse").Value;
                recurse = recurse > 0 ? recurse-1 : recurse;
                var instructionInclude = value.Field<InstructionInclude>("_PropertiesToUse").Value;
                var alias = value.Field<string>("_Alias")?.Value;
                return registerIndex >= 0
                    ? ReturnRegisterVariable(registerIndex, recurse, lineNumber)
                    : ReturnAliasVariable(alias, lineNumber, registerIndex, recurse);
            }

            /// <summary>
            /// Variable should be a register or an alias or a value.
            /// </summary>
            public Variable DoubleValue(string fieldName, int lineNumber)
            {
                var value = traverse.Field(fieldName);
                var constant = value.Field<double>("_Value").Value;
                var isNan = value.Field<bool>("_qNaN").Value;
                var registerIndex = value.Field<int>("_RegisterIndex").Value;
                var recurse = value.Field<int>("_RegisterRecurse").Value;
                var instructionInclude = value.Field<InstructionInclude>("_PropertiesToUse").Value;
                var alias = value.Field<string>("_Alias")?.Value;
                if (alias != null)
                {
                    var defineIndex = defines.IndexOf(alias);
                    if (defineIndex >= 0)
                    {
                        return new Variable
                        {
                            Kind = VariableKind.Define,
                            DefineIndex = (short)defineIndex
                        };
                    }
                    var aliasIndex = aliases.IndexOf(alias);
                    if (aliasIndex >= 0)
                    {
                        return new Variable
                        {
                            Kind = VariableKind.Alias,
                            AliasIndex = (short) aliasIndex
                        };
                    }
                }
                if (registerIndex >= 0)
                {
                    return ReturnRegisterVariable(registerIndex, recurse, lineNumber);
                }
                if (!double.IsNaN(constant) || isNan)
                {
                    return new Variable
                    {
                        Kind = VariableKind.Constant,
                        Constant = constant,
                    };
                }
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, lineNumber);
            }

            public Variable IntValue(string fieldName, int lineNumber)
            {
                var value = traverse.Field(fieldName);
                var constant = value.Field<int>("_Value").Value;
                var isNan = value.Field<bool>("_qNaN").Value;
                var registerIndex = value.Field<int>("_RegisterIndex").Value;
                var recurse = value.Field<int>("_RegisterRecurse").Value;
                var instructionInclude = value.Field<InstructionInclude>("_PropertiesToUse").Value;
                var alias = value.Field<string>("_Alias")?.Value;
                if (alias != null)
                {
                    var defineIndex = defines.IndexOf(alias);
                    if (defineIndex >= 0)
                    {
                        return new Variable
                        {
                            Kind = VariableKind.Define,
                            DefineIndex = (short)defineIndex
                        };
                    }
                    var aliasIndex = aliases.IndexOf(alias);
                    if (aliasIndex >= 0)
                    {
                        return new Variable
                        {
                            Kind = VariableKind.Alias,
                            AliasIndex = (short)aliasIndex
                        };
                    }
                }
                if (registerIndex >= 0)
                {
                    return ReturnRegisterVariable(registerIndex, recurse, lineNumber);
                }
                if (!double.IsNaN(constant) || isNan)
                {
                    return new Variable
                    {
                        Kind = VariableKind.Constant,
                        Constant = constant,
                    };
                }
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, lineNumber);
            }

            /// <summary>
            /// Variable should be an enum value (either name, or integer, or register, or register alias).
            /// If it's a name or an integer, in both cases the resulting integer value of enum is stored in the ConstantIndex.
            /// </summary>
            public Variable Enum<T>(string logicTypeFieldName, int lineNumber)
            {
                var value = traverse.Field(logicTypeFieldName);
                var enumValue = value.Field<int>("_Value").Value;
                var registerIndex = value.Field<int>("_RegisterIndex").Value;
                var recurse = value.Field<int>("_RegisterRecurse").Value;
                var instructionInclude = value.Field<InstructionInclude>("_PropertiesToUse").Value;
                var alias = value.Field<string>("_Alias")?.Value;
                if (enumValue >= 0)
                {
                    return new Variable
                    {
                        Kind = VariableKind.Constant,
                        Constant = enumValue,
                    };
                }
                if (registerIndex >= 0)
                {
                    return ReturnRegisterVariable(registerIndex, recurse, lineNumber);
                }
                if (alias != null)
                {
                    return ReturnAliasVariable(alias, lineNumber, registerIndex, recurse);
                }
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, lineNumber);
            }

            private Variable ReturnAliasVariable(string alias, int lineNumber, int registerIndex, int registerRecurse)
            {
                var index = aliases.IndexOf(alias);
                if (index < 0)
                {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, lineNumber);
                }
                return new Variable
                {
                    Kind = VariableKind.Alias,
                    AliasIndex = (short)index,
                };
            }

            private Variable ReturnRegisterVariable(int registerIndex, int registerRecurse, int lineNumber)
            {
                if (registerIndex < 0 || registerRecurse < 0)
                {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, lineNumber);
                }
                return new Variable
                {
                    Kind = VariableKind.Register,
                    RegisterIndex = (short)registerIndex,
                    RegisterRecurse = (short)(registerRecurse-1),
                };
            }

            private Variable ReturnDeviceRegisterVariable(int deviceIndex, int deviceNetwork, int registerRecurse, int lineNumber)
            {
                if (deviceIndex < 0)
                {
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariable, lineNumber);
                }
                if (deviceIndex == int.MaxValue)
                    deviceIndex = short.MaxValue;
                return new Variable
                {
                    Kind = VariableKind.DeviceRegister,
                    RegisterIndex = (short)deviceIndex,
                    RegisterRecurse = (short)(registerRecurse-1),
                    NetworkIndex = deviceNetwork,
                };
            }
        }
    }
}
