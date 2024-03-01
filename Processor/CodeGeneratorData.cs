using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Motherboards;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using static EntropyFix.Processor.ChipProcessor;

namespace EntropyFix.Processor
{
    public partial class ChipProcessor
    {
        private static readonly MethodInfo GetDeviceMethodInfo = typeof(ChipProcessor).GetMethod(nameof(GetDevice));
        private static readonly MethodInfo GetDeviceByIdMethodInfo = typeof(ChipProcessor).GetMethod(nameof(GetDeviceById));
        private static readonly MethodInfo ReadDeviceMethodInfo = typeof(ChipProcessor).GetMethod(nameof(ReadDevice));
        private static readonly MethodInfo ReadDeviceSlotMethodInfo = typeof(ChipProcessor).GetMethod(nameof(ReadDeviceSlot));
        private static readonly MethodInfo WriteDeviceSlotMethodInfo = typeof(ChipProcessor).GetMethod(nameof(WriteDeviceSlot));
        private static readonly MethodInfo ReadDeviceReagentMethodInfo = typeof(ChipProcessor).GetMethod(nameof(ReadDeviceReagent));
        private static readonly MethodInfo WriteDeviceMethodInfo = typeof(ChipProcessor).GetMethod(nameof(WriteDevice));
        private static readonly MethodInfo WriteDeviceBatchMethodInfo = typeof(ChipProcessor).GetMethod(nameof(WriteDeviceBatch));
        private static readonly MethodInfo ReadDeviceBatchMethodInfo = typeof(ChipProcessor).GetMethod(nameof(ReadDeviceBatch));
        private static readonly MethodInfo BatchReadSlotMethodInfo = typeof(ChipProcessor).GetMethod(nameof(BatchReadSlot));
        private static readonly MethodInfo BatchReadNameMethodInfo = typeof(ChipProcessor).GetMethod(nameof(BatchReadName));
        private static readonly MethodInfo BatchReadNameSlotMethodInfo = typeof(ChipProcessor).GetMethod(nameof(BatchReadNameSlot));
        private static readonly MethodInfo BatchWriteNameMethodInfo = typeof(ChipProcessor).GetMethod(nameof(BatchWriteName));
        private static readonly MethodInfo BatchWriteSlotMethodInfo = typeof(ChipProcessor).GetMethod(nameof(BatchWriteSlot));
        private static readonly MethodInfo SleepMethodInfo = typeof(ChipProcessor).GetMethod(nameof(Sleep));
        private static readonly ConstructorInfo ProgrammableChipExceptionConstructorInfo =
            typeof(ProgrammableChipException).GetConstructor(new[] {typeof(ProgrammableChipException.ICExceptionType), typeof(int)});
#if DEBUG
        private static readonly MethodInfo DebugMethodInfo = typeof(ChipProcessor).GetMethod(nameof(Debug));
#endif

        public struct CodeGeneratorData
        {
            private List<Expression> _body;
            private Dictionary<int, double> _defines;
            private Dictionary<string, ParameterExpression> _variables;
            private Dictionary<int, Expression> _aliases;
            private LabelTarget _controlSwitchLabel;
            private LabelTarget[] _lineLabels;
            private LabelTarget _endLabel;
            public CodeGeneratorData(
                List<Expression> body, 
                Dictionary<int, double> defines,
                Dictionary<string, ParameterExpression> variables,
                Dictionary<int, Expression> aliases,
                LabelTarget controlSwitchLabel,
                LabelTarget[] lineLabels, 
                LabelTarget endLabel)
            {
                _body = body;
                _defines = defines;
                _variables = variables;
                _aliases = aliases;
                _controlSwitchLabel = controlSwitchLabel;
                _lineLabels = lineLabels;
                _endLabel = endLabel;
            }

            public void Add(Expression expression)
            {
                _body.Add(expression);
            }

            public LabelTarget ControlSwitchLabel() => _controlSwitchLabel;
            public LabelTarget EndLabel() => _endLabel;
            public LabelTarget LineLabel(int index) => _lineLabels[index];
            public Expression StackPointer() => Expression.ArrayAccess(_variables["registers"], Expression.Constant(16));
            public ParameterExpression LineVariable() => _variables["line"];
            public Expression Stack(Expression pointer) => Expression.ArrayAccess(Expression.Field(_variables["processor"], nameof(ChipProcessor.Stack)), pointer);

            public Expression StackSize() => Expression.Property(Expression.Field(_variables["processor"], nameof(ChipProcessor.Stack)), nameof(Array.Length));
            public Expression Alias(short index) => _aliases[index];

            /// <summary>
            /// Returns an expression that traverses register references and results in final index. If registerRecurse is 0, the result is registerIndex.
            /// </summary>
            public Expression TraverseReferences(short registerIndex, short registerRecurse)
            {
                var registers = _variables["registers"];
                var index = (Expression)Expression.Constant((int)registerIndex);
                while(registerRecurse-- > 0)
                {
                    index = Expression.Convert(Expression.ArrayAccess(registers, index), typeof(int));
                }
                return index;
            }

            /// <summary>
            /// Returns an expression that accesses a target register for reading.
            /// </summary>
            public Expression RegisterGet(Variable variable, int lineNum) => variable.Kind switch
            {
                // registers[registers[index]]
                VariableKind.Register => Expression.ArrayAccess(this._variables["registers"], TraverseReferences(variable.RegisterIndex, variable.RegisterRecurse)),

                // alias_x.GetValue()
                VariableKind.Alias => Expression.Call(Alias(variable.AliasIndex), AliasGetValueMethodInfo, Expression.Constant(lineNum)),
                _ => throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum)
            };

            /// <summary>
            /// Returns an expression that accesses a target register for writing (either directly or via an alias).
            /// </summary>
            public Expression RegisterSet(Variable variable, int lineNum) => variable.Kind switch
            {
                // registers[registers[index]]
                VariableKind.Register => Expression.ArrayAccess(this._variables["registers"], TraverseReferences(variable.RegisterIndex, variable.RegisterRecurse)),

                // alias_x.SetValue(value)
                VariableKind.Alias => Expression.Call(Alias(variable.AliasIndex), AliasSetValueMethodInfo, Expression.Constant(lineNum)),

                _ => throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum)
            };

            /// <summary>
            /// Returns an expression that returns an enumeration value (either directly or via a register or an alias).
            /// </summary>
            public Expression Enum<T>(Variable variable, int lineNum) =>
                variable.Kind switch
                {
                    // LogicType.value
                    VariableKind.Constant => Expression.Convert(Expression.Constant((int)variable.Constant), typeof(T)),
                    // (LogicType)((int)registers[registers[index]])
                    VariableKind.Register => 
                        Expression.Convert(
                            Expression.Convert(
                                RegisterGet(variable, lineNum),
                                typeof(int)),
                            typeof(T)),
                    // (LogicType)(int)alias_x.GetValue()
                    VariableKind.Alias => 
                        Expression.Convert(
                            Expression.Convert(
                                Expression.Call(Alias(variable.AliasIndex), AliasGetValueMethodInfo, Expression.Constant(lineNum)),
                                typeof(int)),
                            typeof(T)),
                    _ => throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum)
                };

            public Expression DoubleValue(Variable variable, int lineNum) => variable.Kind switch
            {
                VariableKind.Define => Expression.Constant(_defines[variable.DefineIndex]),
                VariableKind.Constant => Expression.Constant(variable.Constant),
                VariableKind.Register => RegisterGet(variable, lineNum),
                VariableKind.Alias => RegisterGet(variable, lineNum),
                _ => throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum),
            };

            public Expression LongValue(Variable variable, int lineNum) => LongValue(variable, true, lineNum);
            public Expression LongValue(Variable variable, bool signed, int lineNum)
            {
                switch(variable.Kind)
                {
                    case VariableKind.Define:
                    case VariableKind.Constant:
                        // In case of defined value, we convert it to long value during the compilation.
                        var value = variable.Kind switch // there's no missing cases
                        {
                            VariableKind.Define => _defines[variable.DefineIndex],
                            VariableKind.Constant => variable.Constant,
                            _ => throw new ApplicationException("Unexpected variable kind.")
                        };
                        return value < long.MinValue
                            ? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftUnderflow, lineNum)
                            : value > long.MaxValue
                                ? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftOverflow, lineNum)
                                : Expression.Constant(DoubleToLong(value));
                    case VariableKind.Register:
                        // And in other cases, we need to do the same in runtime.
                        var valueExpr = RegisterGet(variable, lineNum);

                        // value < long.MinValue
                        return Expression.Condition(Expression.LessThan(valueExpr, Expression.Constant(long.MinValue)),
                            // ? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftUnderflow, lineNum)
                            Expression.Throw(Expression.New(ProgrammableChipExceptionConstructorInfo,
                                Expression.Constant(ProgrammableChipException.ICExceptionType.ShiftUnderflow), Expression.Constant(lineNum))),
                            // : value > long.MaxValue
                            Expression.Condition(Expression.GreaterThan(valueExpr, Expression.Constant(long.MaxValue)),
                                // ? throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ShiftOverflow, lineNum)
                                Expression.Throw(Expression.New(ProgrammableChipExceptionConstructorInfo,
                                    Expression.Constant(ProgrammableChipException.ICExceptionType.ShiftOverflow), Expression.Constant(lineNum))),
                                // : (long)(d % 9.00719925474099E+15)
                                DoubleToLongExpression(valueExpr)));
                    default: throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum);
                };
            }

            public double DefineValue(int index) => _defines[index];

            public Expression IntValue(Variable variable, int lineNum) => variable.Kind switch
            {
                VariableKind.Alias => Expression.Convert(Expression.Call(Alias(variable.AliasIndex), AliasGetValueMethodInfo, Expression.Constant(lineNum)), typeof(int)),
                VariableKind.Define => Expression.Constant((int)_defines[variable.DefineIndex]),
                VariableKind.Register => Expression.Convert(RegisterGet(variable, lineNum), typeof(int)),
                VariableKind.Constant => Expression.Constant((int)variable.Constant),
                _ => throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum),
            };

            public Expression LongToDoubleExpression(Expression l, int lineNum) =>
                Expression.Convert(
                    Expression.Condition(
                        Expression.NotEqual(
                            Expression.And(l, Expression.Constant(0x20000000000000)),
                            Expression.Constant(0)),
                        Expression.Or(Expression.And(l, Expression.Constant(0x1FFFFFFFFFFFFF)), Expression.Constant(-0x20000000000000)),
                        Expression.And(l, Expression.Constant(0x1FFFFFFFFFFFFF))),
                    typeof(double));

            public static double LongToDouble(long l) =>
                (double)((l & 0x20000000000000) != 0 ? l & 0x1FFFFFFFFFFFFF | -0x20000000000000 /* FFE0 0000 0000 0000 */ : l & 0x1FFFFFFFFFFFFF);

            public Expression DoubleToLongExpression(Expression d, bool signed = true) =>
                signed 
                    ? Expression.Convert(Expression.Modulo(d, Expression.Constant(9.00719925474099E+15)), typeof(long))
                    : Expression.And(
                        Expression.Convert(Expression.Modulo(d, Expression.Constant(9.00719925474099E+15)), typeof(long)),
                        Expression.Constant(0x3FFFFFFFFFFFFF));
            public long DoubleToLong(double d, bool signed = true) =>
                (long) (d % 9.00719925474099E+15) & (signed ? -1 : 0x3FFFFFFFFFFFFF);

            /// <summary>
            /// Returns an expression that assigns given value expression to a target register (either directly or via an alias).
            /// </summary>
            public Expression Assign(Variable variable, Expression value, int lineNum)
            {
                return variable.Kind switch
                {
                    VariableKind.Register => Expression.Assign(RegisterSet(variable, lineNum), value),
                    VariableKind.Alias => Expression.Call(Alias(variable.AliasIndex), AliasSetValueMethodInfo, value, Expression.Constant(lineNum)),
                    _ => throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum)
                };
            }

            /// <summary>
            /// Returns an expressions that returns ILogicable of a device.
            /// </summary>
            public Expression Device(Variable variable, bool throwError, int lineNum) => variable.Kind switch
            {
                // devices[registers[index]]
                VariableKind.DeviceRegister => Expression.Call(
                    _variables["processor"],
                    GetDeviceMethodInfo,
                    TraverseReferences(variable.RegisterIndex, variable.RegisterRecurse),
                    Expression.Constant(variable.NetworkIndex),
                    Expression.Constant(throwError),
                    Expression.Constant(lineNum)),
                // alias_x.GetDevice()
                VariableKind.Alias => Expression.Call(Alias(variable.AliasIndex), AliasGetDeviceMethodInfo, Expression.Constant(lineNum)),
                _ => throw new InvalidOperationException("Unexpected type of variable in l instruction.")
            };

            public Expression GetDeviceById(Variable variable, bool throwError, int lineNum) =>
                Expression.Call(
                    _variables["processor"],
                    GetDeviceByIdMethodInfo,
                    IntValue(variable, lineNum),
                    Expression.Constant(throwError),
                    Expression.Constant(lineNum));

            public Expression ReadDevice(Variable device, Variable logicType, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    ReadDeviceMethodInfo,
                    Device(device, true, lineNum),
                    Enum<LogicType>(logicType, lineNum),
                    Expression.Constant(lineNum));
            }
            public Expression ReadDevice(Expression device, Variable logicType, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    ReadDeviceMethodInfo,
                    device,
                    Enum<LogicType>(logicType, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression ReadDeviceBatch(Variable deviceHash, Variable logicType, Variable batchMethod, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    ReadDeviceBatchMethodInfo,
                    IntValue(deviceHash, lineNum),
                    Enum<LogicType>(logicType, lineNum),
                    Enum<LogicBatchMethod>(batchMethod, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression ReadDeviceSlot(Variable device, Variable slotIndex, Variable logicType, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    ReadDeviceSlotMethodInfo,
                    Device(device, true, lineNum),
                    Enum<LogicSlotType>(logicType, lineNum),
                    IntValue(slotIndex, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression WriteDeviceSlot(Variable device, Variable slotIndex, Variable logicType, Variable value, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    WriteDeviceSlotMethodInfo,
                    Device(device, true, lineNum),
                    Enum<LogicSlotType>(logicType, lineNum),
                    IntValue(slotIndex, lineNum),
                    DoubleValue(value, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression ReadDeviceReagent(Variable device, Variable mode, Variable reagent, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    ReadDeviceReagentMethodInfo,
                    Device(device, true, lineNum),
                    Enum<LogicReagentMode>(mode, lineNum),
                    IntValue(reagent, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression BatchReadSlot(Variable deviceHash, Variable slotIndex, Variable logicType, Variable batchMethod, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    BatchReadSlotMethodInfo,
                    IntValue(deviceHash, lineNum),
                    IntValue(slotIndex, lineNum),
                    Enum<LogicSlotType>(logicType, lineNum),
                    Enum<LogicBatchMethod>(batchMethod, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression BatchReadName(Variable deviceHash, Variable nameHash, Variable logicType, Variable batchMethod, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    BatchReadNameMethodInfo,
                    IntValue(deviceHash, lineNum),
                    IntValue(nameHash, lineNum),
                    Enum<LogicType>(logicType, lineNum),
                    Enum<LogicBatchMethod>(batchMethod, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression BatchReadNameSlot(Variable deviceHash, Variable nameHash, Variable slotIndex, Variable logicSlotType, Variable batchMethod, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    BatchReadNameSlotMethodInfo,
                    IntValue(deviceHash, lineNum),
                    IntValue(nameHash, lineNum),
                    IntValue(slotIndex, lineNum),
                    Enum<LogicSlotType>(logicSlotType, lineNum),
                    Enum<LogicBatchMethod>(batchMethod, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression BatchWriteName(Variable deviceHash, Variable nameHash, Variable logicType, Variable value, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    BatchWriteNameMethodInfo,
                    IntValue(deviceHash, lineNum),
                    IntValue(nameHash, lineNum),
                    Enum<LogicType>(logicType, lineNum),
                    DoubleValue(value, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression BatchWriteSlot(Variable deviceHash, Variable slotIndex, Variable logicSlotType, Variable value, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    BatchWriteSlotMethodInfo,
                    IntValue(deviceHash, lineNum),
                    IntValue(slotIndex, lineNum),
                    Enum<LogicSlotType>(logicSlotType, lineNum),
                    DoubleValue(value, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression WriteDevice(Expression device, Variable logicType, Variable value, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    WriteDeviceMethodInfo,
                    device,
                    Enum<LogicType>(logicType, lineNum),
                    DoubleValue(value, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression WriteDevice(Variable device, Variable logicType, Variable value, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    WriteDeviceMethodInfo,
                    Device(device, true, lineNum),
                    Enum<LogicType>(logicType, lineNum),
                    DoubleValue(value, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression WriteDeviceBatch(Variable deviceHash, Variable logicType, Variable value, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    WriteDeviceBatchMethodInfo,
                    IntValue(deviceHash, lineNum),
                    Enum<LogicType>(logicType, lineNum),
                    DoubleValue(value, lineNum),
                    Expression.Constant(lineNum));
            }

            public Expression Sleep(Variable duration, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    SleepMethodInfo,
                    DoubleValue(duration, lineNum));
            }

#if DEBUG
            public Expression Debug(LineOfCode line, int lineNum)
            {
                return Expression.Call(
                    _variables["processor"],
                    DebugMethodInfo,
                    Expression.Constant(line.SourceLine),
                    Expression.Constant(lineNum));
            }
#endif
        }
    }
}
