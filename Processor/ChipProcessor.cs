using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Assets.Scripts.Objects.Motherboards;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using Reagents;
using Assets.Scripts;

namespace EntropyFix.Processor
{
    public partial class ChipProcessor
    {
#if DEBUG
        private static bool SaveToAssembly = false;
#endif

        public static readonly Dictionary<string, ArgumentsTranslator> InstructionTranslators = new();
        public static readonly Dictionary<string, LineGenerator> InstructionCompilers = new();

        public readonly Alias[] Aliases;
        /// <summary>
        /// Registers. 16'th is SP, 17'th is PC
        /// </summary>
        public readonly double[] Registers;
        /// <summary>
        /// Program Counter
        /// </summary>
        public int PC;
        /// <summary>
        /// Stack memory
        /// </summary>
        public readonly double[] Stack;

        private ProcessorCode _code;
        private double _sleepDuration;
        private double _sleptAt;
        private CircuitHousing _circuitHousing;
        private Traverse<CircuitHousing> _circuitHousingField;
        private Traverse<ushort> _errorLineNumber;
        private Traverse<int> _nextAddr;
        private Traverse<ProgrammableChipException.ICExceptionType> _errorType;

        public double SleepDuration
        {
            get => this._sleepDuration;
            private set => this._sleepDuration = value;
        }
        public double SleptAt
        {
            get => this._sleptAt;
            private set => this._sleptAt = value;
        }
        public ProgrammableChip Chip { get; private set; }

        public ChipProcessor(ProgrammableChip chip, ProcessorCode code, Alias[] aliases)
        {
            Chip = chip;
            var traverse = Traverse.Create(chip);
            Stack = traverse.Field<double[]>("_Stack").Value;
            Registers = traverse.Field<double[]>("_Registers").Value;
            Aliases = aliases;
            _circuitHousingField = traverse.Property<CircuitHousing>("CircuitHousing");
            _errorLineNumber = traverse.Property<ushort>("_ErrorLineNumber");
            _errorType = traverse.Property<ProgrammableChipException.ICExceptionType>("_ErrorType");
            _nextAddr = traverse.Field<int>("_NextAddr");
            _code = code;
        }

        public static void DefineInstructionHandler(string instruction, ArgumentsTranslator translator, LineGenerator generator)
        {
            if (InstructionTranslators.ContainsKey(instruction) || InstructionCompilers.ContainsKey(instruction))
                throw new ArgumentException($"Instruction `{instruction}' already has a handler defined.");
            InstructionTranslators.Add(instruction, translator);
            InstructionCompilers.Add(instruction, generator);
        }

        public virtual void Execute(int runCount)
        {
            if (SleepDuration > 0)
            {
                // This is a hack for serialization, because during deserialization the GameTime is incorrect and we can't rely on it.
                if (SleptAt < 0)
                {
                    // SleptAt after deserialization would be set as the negative time spent asleep instead, so restore the correct SleptAt value.
                    // Trying to calculate SleptAt based on current GameTime might result in negative value, so we adjust SleepDuration instead.
                    // Also, we need to account for the last tick time.
                    SleepDuration += SleptAt - GameManager.LastTickTimeSeconds;
                    SleptAt = GameManager.GameTime;
                }
                if(GameManager.GameTime - SleptAt < SleepDuration)
                    return;
                SleepDuration = 0;
                SleptAt = 0;
            }
            if (_code != null)
            {
                _circuitHousing = _circuitHousingField.Value;
                try
                {
                    PC = _code(PC, this, Registers, this.Aliases);
                    _nextAddr.Value = PC;
                }
                catch (ProgrammableChipException e)
                {
                    if (_circuitHousing != null)
                    {
                        _circuitHousing.RaiseError(1);
                        _errorLineNumber.Value = e.LineNumber;
                        _errorType.Value = e.ExceptionType;
                    }
                    PC = e.LineNumber;
                    _nextAddr.Value = e.LineNumber;
                    return;
                }
                catch
                {
                    if (_circuitHousing != null)
                    {
                        _circuitHousing.RaiseError(1);
                        _errorLineNumber.Value = ushort.MaxValue;
                        _errorType.Value = ProgrammableChipException.ICExceptionType.Unknown;
                    }
                    PC = ushort.MaxValue;
                    _nextAddr.Value = PC;
                    return;
                }
                if (_circuitHousing != null)
                {
                    _circuitHousing.RaiseError(0);
                    _errorLineNumber.Value = 0;
                    _errorType.Value = ProgrammableChipException.ICExceptionType.None;
                }
                _nextAddr.Value = PC;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ILogicable GetDevice(int index, int networkIndex, bool throwError, int lineNum)
        {
            if (index == short.MaxValue)
                index = int.MaxValue;
            var logicable = _circuitHousing.GetLogicableFromIndex(index, networkIndex);
            if (logicable == null && throwError)
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, lineNum);
            return logicable;
        }

        public ILogicable GetDeviceById(int id, bool throwError, int lineNum)
        {
            var logicable = _circuitHousing.GetLogicableFromId(id);
            if (logicable == null && throwError)
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceNotFound, lineNum);
            return logicable;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDevice(ILogicable logicable, LogicType logicType, int lineNum)
        {
            return logicable.CanLogicRead(logicType)
                ? logicable.GetLogicValue(logicType)
                : throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
        }

        public double ReadDeviceBatch(int deviceHash, LogicType logicType, LogicBatchMethod method, int lineNum)
        {
            var devices = _circuitHousing.GetBatchOutput();
            if (devices == null)
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, lineNum);
            if(devices
               .Where(x => x != null && x.GetPrefabHash() == deviceHash)
               .Any(x => !x.CanLogicRead(logicType)))
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
            return Device.BatchRead(method, logicType, deviceHash, devices);
        }

        public double BatchReadSlot(int deviceHash, int slotIndex, LogicSlotType logicSlotType, LogicBatchMethod batchMode, int lineNum)
        {
            var devices = _circuitHousing.GetBatchOutput();
            if (devices == null)
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, lineNum);
            if (devices
                .Where(x => x != null && x.GetPrefabHash() == deviceHash)
                .Any(x => !x.CanLogicRead(logicSlotType, slotIndex)))
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
            return Device.BatchRead(batchMode, logicSlotType, slotIndex, deviceHash, devices);
        }

        public double BatchReadName(int deviceHash, int nameHash, LogicType logicType, LogicBatchMethod batchMode, int lineNum)
        {
            var devices = _circuitHousing.GetBatchOutput();
            if (devices == null)
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, lineNum);
            if (devices
                .Where(x => x != null && x.GetPrefabHash() == deviceHash && x.GetNameHash() == nameHash)
                .Any(x => x.CanLogicRead(logicType)))
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
            return Device.BatchRead(batchMode, logicType, nameHash, deviceHash, devices);
        }

        public double BatchReadNameSlot(int deviceHash, int nameHash, int slotIndex, LogicSlotType logicSlotType, LogicBatchMethod batchMode, int lineNum)
        {
            var devices = _circuitHousing.GetBatchOutput();
            if (devices == null)
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, lineNum);
            if (devices
                .Where(x => x != null && x.GetPrefabHash() == deviceHash && x.GetNameHash() == nameHash)
                .Any(x => !x.CanLogicRead(logicSlotType, slotIndex)))
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
            return Device.BatchRead(batchMode, logicSlotType, slotIndex, nameHash, deviceHash, devices);
        }

        public void BatchWriteName(int deviceHash, int nameHash, LogicType logicType, double value, int lineNum)
        {
            var devices = _circuitHousing.GetBatchOutput();
            if (devices == null)
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, lineNum);
            if (devices
                .Where(x => x != null && x.GetPrefabHash() == deviceHash && x.GetNameHash() == nameHash)
                .Any(x => !x.CanLogicWrite(logicType)))
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
            devices.ForEach(x => x.SetLogicValue(logicType, value));
        }

        public void BatchWriteSlot(int deviceHash, int slotIndex, LogicSlotType logicSlotType, double value, int lineNum)
        {
            var devices = _circuitHousing.GetBatchOutput();
            if (devices == null)
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, lineNum);
            if (devices
                .Where(x => x != null && x.GetPrefabHash() == deviceHash)
                .Any(x => x is not ISlotWriteable slotWriteable || !slotWriteable.CanLogicWrite(logicSlotType, slotIndex)))
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
            devices.ForEach(x => ((ISlotWriteable)x).SetLogicValue(logicSlotType, slotIndex, value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ReadDeviceSlot(ILogicable logicable, LogicSlotType slotType, int slotId, int lineNum)
        {
            return logicable.CanLogicRead(slotType, slotId)
                ? logicable.GetLogicValue(slotType, slotId)
                : throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDeviceSlot(ILogicable logicable, LogicSlotType slotType, int slotId, double value, int lineNum)
        {
            if(logicable is ISlotWriteable slotWriteable && slotWriteable.CanLogicWrite(slotType, slotId))
                slotWriteable.SetLogicValue(slotType, slotId, value);
            else
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
        }

        public double ReadDeviceReagent(ILogicable logicable, LogicReagentMode mode, int reagent, int lineNum)
        {
            switch (mode)
            {
                case LogicReagentMode.Contents:
                    if (logicable is Device device)
                        return device.ReagentMixture.Get(Reagent.Find(reagent));
                    break;
                case LogicReagentMode.Required:
                    if (logicable is IRequireReagent requireReagent)
                        return requireReagent.RequiredReagents.Get(Reagent.Find(reagent));
                    break;
                case LogicReagentMode.Recipe:
                    if (logicable is IRequireReagent recipeReagent)
                        return recipeReagent.CurrentRecipe.Get(Reagent.Find(reagent));
                    break;
                case LogicReagentMode.TotalContents:
                    if(logicable is Device totalContentsDevice)
                        return totalContentsDevice.ReagentMixture.TotalReagents;
                    break;
            }
            throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.UnhandledReagentMode, lineNum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDevice(ILogicable logicable, LogicType logicType, double value, int lineNum)
        {
            if (logicable.CanLogicWrite(logicType))
                logicable.SetLogicValue(logicType, value);
            else
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
        }

        public void WriteDeviceBatch(int deviceHash, LogicType logicType, double value, int lineNum)
        {
            var devices = _circuitHousing.GetBatchOutput();
            if (devices == null)
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.DeviceListNull, lineNum);
            foreach (var device in devices)
            {
                if (device.CanLogicWrite(logicType))
                    device.SetLogicValue(logicType, value);
                else
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectLogicType, lineNum);
            }
        }

        public void Sleep(double durationSeconds)
        {
            SleepDuration = durationSeconds;
            SleptAt = GameManager.GameTime;
        }

        public static ChipProcessor Translate(ProgrammableChip chip, int operationsPerTick)
        {
            if (chip.CompilationError)
                return new InvalidProcessor(chip);
            var chipTraverse = Traverse.Create(chip);
            var linesOfCodeField = chipTraverse.Field("_LinesOfCode");
            var defineValuesChip = chipTraverse.Field<Dictionary<string, double>>("_Defines").Value;
            var jumpTags = chipTraverse.Field<Dictionary<string, int>>("_JumpTags").Value;
            if(defineValuesChip.Keys.Any(x => jumpTags.ContainsKey(x)))
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.ExtraDefine, 0);
            List<string> defines = defineValuesChip.Keys.Concat(jumpTags.Keys).ToList();
            var count = linesOfCodeField.Property<int>("Count").Value;
            Span<LineOfCode> lines = new LineOfCode[count];
            if (count > 0)
            {
                // First, translate lines to LineOfCode structures, which are easier to use
                var enumerator = ((IEnumerable)linesOfCodeField.GetValue()).GetEnumerator();
                List<string> aliases = new();
                for (int i = 0; i < count; i++)
                {
                    enumerator.MoveNext();
                    var lineObj = enumerator.Current;
                    var lineOfCode = TranslateLine(lineObj, i, aliases, defines);
                    lines[i] = lineOfCode;
                }
                var defineValues = defineValuesChip.ToDictionary(x => defines.IndexOf(x.Key), x => x.Value); // copy the dictionary to avoid modifying the original
                foreach (var tag in jumpTags)
                    defineValues.Add(defines.IndexOf(tag.Key), tag.Value);
                // This is to avoid name collision in generated code:
                aliases.ForEach(x => x = "alias_" + x);
                var aliasesArray = new Alias[aliases.Count];
                // Then generate the code.
                var code = Compile(chip, lines, defineValues, aliases, operationsPerTick);
                return new ChipProcessor(chip, code, aliasesArray);
            }
            return new InvalidProcessor(chip);
        }

        private static ProcessorCode Compile(
            ProgrammableChip chip, 
            Span<LineOfCode> lines, 
            Dictionary<int, double> defines, 
            List<string> aliases,
            int operationsPerTick)
        {
            var lineParamName = "line";
            var processorParamName = "processor";
            var registersParamName = "registers";
            var yieldCounterName = "yield";
            var aliasesParamName = "aliases";
            
            // Declare parameters:
            var lineParam = Expression.Parameter(typeof(int), lineParamName);
            var processorParam = Expression.Parameter(typeof(ChipProcessor), processorParamName);
            var registersParam = Expression.Parameter(typeof(double[]), registersParamName);
            var aliasesParam = Expression.Parameter(typeof(Alias[]), aliasesParamName);
            var parameters = new[] { lineParam, processorParam, registersParam, aliasesParam };
            
            // Declare variables:
            var variables = new Dictionary<string, ParameterExpression>();
            var aliasesIndexed = new Dictionary<int, Expression>(aliases.Count);
            for(var i = 0; i < aliases.Count; i++)
                aliasesIndexed.Add(i, Expression.ArrayAccess(aliasesParam, Expression.Constant(i)));
            variables.Add(lineParamName, lineParam);
            variables.Add(processorParamName, processorParam);
            variables.Add(registersParamName, registersParam);
            variables.Add(aliasesParamName, aliasesParam);
            var yield = Expression.Variable(typeof(int), yieldCounterName);
            // This variable would be used as a counter to force execution yield.
            variables.Add(yieldCounterName, yield);

            // Declare line labels for each line
            var labels = new LabelTarget[lines.Length];
            for (var i = 0; i < lines.Length; i++)
                labels[i] = Expression.Label("Line_" + i);
            var endLabel = Expression.Label("End");
            var bodyExpressions = new List<Expression>();
            // Initialize yield counter
            bodyExpressions.Add(Expression.Assign(variables[yieldCounterName], Expression.Constant(operationsPerTick)));
            // Initialize alias values
            // The resulting code should look like this:
            // alias_x = aliases[idx];
            //aliases.ForEach(x => 
            //    bodyExpressions.Add(
            //        Expression.Assign(variables[x], Expression.ArrayIndex(aliasesParam, Expression.Constant(aliases.IndexOf(x))))));
            

            // Declare control block switch (used to transition between lines of code)
            var controlSwitchLabel = Expression.Label("ControlSwitch");
            bodyExpressions.Add(Expression.Label(controlSwitchLabel)); // Place the label before the control switch
            bodyExpressions.Add(Expression.Switch( // Define the control switch
                    lineParam, // switch based on value of 'line' variable
                    Expression.Goto(endLabel), // default case: jump to the end of code
                    labels.Select((x, i) => Expression.SwitchCase(Expression.Goto(x), Expression.Constant(i))).ToArray()) // jumps to lines of code
            );

            // Write the lines of code, preceded by label.
            // The resulting base variant of the code should look like this:
            // Line_x:
            // (the effective code of the line)
            // if(yield--) { goto Return; }
            // (optional) goto ControlSwitch;
            var index = 0;
            var data = new CodeGeneratorData(bodyExpressions, defines, variables, aliasesIndexed, controlSwitchLabel, labels, endLabel);
            foreach (var line in lines)
            {
                // Place the line label
                bodyExpressions.Add(Expression.Label(labels[index]));
                // Instead of incrementing the line value to track currently executing line and return it on yield, we just assign it for each line.
                bodyExpressions.Add(Expression.Assign(lineParam, Expression.Constant(index)));
                // Place the yield check
                bodyExpressions.Add(
                    Expression.IfThen(
                        Expression.LessThanOrEqual(
                            Expression.PreDecrementAssign(yield), Expression.Constant(0)),
                        Expression.Block(
                            Expression.PostIncrementAssign(lineParam), // increment the line to return to the next line on next execution
                            Expression.Goto(endLabel)))); // and go to the end to yield the execution.
                if(line != LineOfCode.Empty)
                    GenerateLineCode(line, data, index);
                index++;
            }

            bodyExpressions.Add(Expression.Label(endLabel));
            var returnLabel = Expression.Label(typeof(int));
            bodyExpressions.Add(Expression.Label(returnLabel, lineParam));
            //bodyExpressions.Add(Expression.Label(returnLabel, Expression.Constant(lines.Length-1)));
            var body = Expression.Block(new[] { yield }, bodyExpressions);
            var lambda = Expression.Lambda<ProcessorCode>(body, "IC_"+chip.ReferenceId, parameters);
            var result = lambda.Compile();

            if (SaveToAssembly)
            {
                SaveExpressionTreeToAssembly(lambda, "IC_" + chip.ReferenceId, "ChipProcessor", "ChipProcessor.dll");
                SaveToAssembly = false;
            }

            return result;
        }
#if DEBUG
        private static void SaveExpressionTreeToAssembly(LambdaExpression lambda, string methodName, string assemblyName, string assemblyPath)
        {
            var dynamicAssembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Save);
            var dynamicModule = dynamicAssembly.DefineDynamicModule(assemblyName + "_module", assemblyName + ".dll");
            var dynamicType = dynamicModule.DefineType(assemblyName + "_type");

            lambda.CompileToMethod(dynamicType.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static));
            dynamicType.CreateType();
            dynamicAssembly.Save(assemblyPath);
        }
#endif

        private static void GenerateLineCode(LineOfCode line, CodeGeneratorData data, int lineNumber)
        {
            if (!InstructionCompilers.TryGetValue(line.Command, out var generator))
            {
                throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.UnrecognisedInstruction, lineNumber);
            }
#if DEBUG
            data.Debug(line, lineNumber);
#endif
            generator(line, data, lineNumber);
        }

        private static LineOfCode TranslateLine(object lineObj, int lineNumber, List<string> aliases, List<string> defines)
        {
            var traverse = Traverse.Create(lineObj).Field("Operation");
#if DEBUG
            var lineOfCode = Traverse.Create(lineObj).Field<string>("LineOfCode").Value;
#endif
            var instruction = GetInstruction(lineObj, traverse);
            if (instruction != null)
            {
                var (arg1, arg2, arg3, arg4, arg5, arg6) = TranslateArguments(lineObj, lineNumber, instruction, traverse, aliases, defines);
                return new LineOfCode(instruction, arg1, arg2, arg3, arg4, arg5, arg6)
#if DEBUG
                {
                    SourceLine = lineOfCode
                }
#endif
                ;
            }
            return LineOfCode.Empty;
        }

        private delegate int DynamicMethodDelegate(int startFromLine, ChipProcessor processor, ref double alias1, ref double alias2);

        private static string GetInstruction(object lineOfCode, Traverse traverse)
        {
            var operationObj = traverse.GetValue();
            var typeName = operationObj.GetType().Name;
            switch (typeName)
            {
                case "_ALIAS_Operation":
                    return "alias";
                case "_LABEL_Operation":
                    return "label";
                case "_DEFINE_Operation":
                    return "define";
                case "_HCF_Operation":
                    return "hcf";
                case "_GET_Operation":
                    return "get";
                case "_LBNS_Operation":
                    return "lbns";
                case "_LBN_Operation":
                    return "lbn";
                case "_LBS_Operation":
                    return "lbs";
                case "_LB_Operation":
                    return "lb";
                case "_LR_Operation":
                    return "lr";
                case "_LS_Operation":
                    return "ls";
                case "_L_Operation":
                    return "l";
                case "_ABS_Operation":
                    return "abs";
                case "_ACOS_Operation":
                    return "acos";
                case "_ASIN_Operation":
                    return "asin";
                case "_ATAN_Operation":
                    return "atan";
                case "_CEIL_Operation":
                    return "ceil";
                case "_COS_Operation":
                    return "cos";
                case "_EXP_Operation":
                    return "exp";
                case "_FLOOR_Operation":
                    return "floor";
                case "_LOG_Operation":
                    return "log";
                case "_MOVE_Operation":
                    return "move";
                case "_NOT_Operation":
                    return "not";
                case "_ADD_Operation":
                    return "add";
                case "_AND_Operation":
                    return "and";
                case "_ATAN2_Operation":
                    return "atan2";
                case "_DIV_Operation":
                    return "div";
                case "_MAX_Operation":
                    return "max";
                case "_MIN_Operation":
                    return "min";
                case "_MOD_Operation":
                    return "mod";
                case "_MUL_Operation":
                    return "mul";
                case "_NOR_Operation":
                    return "nor";
                case "_SAP_Operation":
                    return "sap";
                case "_SAPZ_Operation":
                    return "sapz";
                case "_SELECT_Operation":
                    return "select";
                case "_SNA_Operation":
                    return "sna";
                case "_SNAZ_Operation":
                    return "snaz";
                case "_OR_Operation":
                    return "or";
                case "_SEQ_Operation":
                    return "seq";
                case "_SEQZ_Operation":
                    return "seqz";
                case "_SGE_Operation":
                    return "sge";
                case "_SGEZ_Operation":
                    return "sgez";
                case "_SGT_Operation":
                    return "sgt";
                case "_SGTZ_Operation":
                    return "sgtz";
                case "_SLA_SLL_Operation":
                    return "sll";
                case "_SLE_Operation":
                    return "sle";
                case "_SLEZ_Operation":
                    return "slez";
                case "_SLT_Operation":
                    return "slt";
                case "_SLTZ_Operation":
                    return "sltz";
                case "_SNE_Operation":
                    return "sne";
                case "_SNEZ_Operation":
                    return "snez";
                case "_SRA_Operation":
                    return "sra";
                case "_SRL_Operation":
                    return "srl";
                case "_SUB_Operation":
                    return "sub";
                case "_XOR_Operation":
                    return "xor";
                case "_ROUND_Operation":
                    return "round";
                case "_SIN_Operation":
                    return "sin";
                case "_SNANZ_Operation":
                    return "snanz";
                case "_SNAN_Operation":
                    return "snan";
                case "_SQRT_Operation":
                    return "sqrt";
                case "_TAN_Operation":
                    return "tan";
                case "_TRUNC_Operation":
                    return "trunc";
                case "_GETD_Operation":
                    return "getd";
                case "_LD_Operation":
                    return "ld";
                case "_PEEK_Operation":
                    return "peek";
                case "_POP_Operation":
                    return "pop";
                case "_RAND_Operation":
                    return "rand";
                case "_SDNS_Operation":
                    return "sdns";
                case "_SDSE_Operation":
                    return "sdse";
                case "_BRDNS_Operation":
                    return "brdns";
                case "_BDNS_Operation":
                    return "bdns";
                case "_BDNSAL_Operation":
                    return "bdnsal";
                case "_BRDSE_Operation":
                    return "brdse";
                case "_BDSE_Operation":
                    return "bdse";
                case "_BDSEAL_Operation":
                    return "bdseal";
                case "_JR_Operation":
                    return "jr";
                case "_J_Operation":
                    return "j";
                case "_JAL_Operation":
                    return "jal";
                case "_BRNAN_Operation":
                    return "brnan";
                case "_BNAN_Operation":
                    return "bnan";
                case "_BREQ_Operation":
                    return "breq";
                case "_BEQ_Operation":
                    return "beq";
                case "_BEQAL_Operation":
                    return "beqal";
                case "_BEQZAL_Operation":
                    return "beqzal";
                case "_BEQZ_Operation":
                    return "beqz";
                case "_BREQZ_Operation":
                    return "breqz";
                case "_BRGE_Operation":
                    return "brge";
                case "_BGE_Operation":
                    return "bge";
                case "_BGEAL_Operation":
                    return "bgeal";
                case "_BGEZAL_Operation":
                    return "bgezal";
                case "_BGEZ_Operation":
                    return "bgez";
                case "_BRGEZ_Operation":
                    return "brgez";
                case "_BRGT_Operation":
                    return "brgt";
                case "_BGT_Operation":
                    return "bgt";
                case "_BGTAL_Operation":
                    return "bgtal";
                case "_BGTZAL_Operation":
                    return "bgtzal";
                case "_BGTZ_Operation":
                    return "bgtz";
                case "_BRGTZ_Operation":
                    return "brgtz";
                case "_BRLE_Operation":
                    return "brle";
                case "_BLE_Operation":
                    return "ble";
                case "_BLEAL_Operation":
                    return "bleal";
                case "_BLEZAL_Operation":
                    return "blezal";
                case "_BLEZ_Operation":
                    return "blez";
                case "_BRLEZ_Operation":
                    return "brlez";
                case "_BRLT_Operation":
                    return "brlt";
                case "_BLT_Operation":
                    return "blt";
                case "_BLTAL_Operation":
                    return "bltal";
                case "_BLTZAL_Operation":
                    return "bltzal";
                case "_BLTZ_Operation":
                    return "bltz";
                case "_BRLTZ_Operation":
                    return "brltz";
                case "_BRNE_Operation":
                    return "brne";
                case "_BNE_Operation":
                    return "bne";
                case "_BNEAL_Operation":
                    return "bneal";
                case "_BNEZAL_Operation":
                    return "bnezal";
                case "_BNEZ_Operation":
                    return "bnez";
                case "_BRNEZ_Operation":
                    return "brnez";
                case "_BRAP_Operation":
                    return "brap";
                case "_BAP_Operation":
                    return "bap";
                case "_BAPAL_Operation":
                    return "bapal";
                case "_BAPZAL_Operation":
                    return "bapzal";
                case "_BAPZ_Operation":
                    return "bapz";
                case "_BRAPZ_Operation":
                    return "brapz";
                case "_BRNA_Operation":
                    return "brna";
                case "_BNA_Operation":
                    return "bna";
                case "_BNAAL_Operation":
                    return "bnaal";
                case "_BNAZAL_Operation":
                    return "bnazal";
                case "_BNAZ_Operation":
                    return "bnaz";
                case "_BRNAZ_Operation":
                    return "brnaz";
                case "_POKE_Operation":
                    return "poke";
                case "_PUSH_Operation":
                    return "push";
                case "_PUTD_Operation":
                    return "putd";
                case "_PUT_Operation":
                    return "put";
                case "_SBN_Operation":
                    return "sbn";
                case "_SBS_Operation":
                    return "sbs";
                case "_SB_Operation":
                    return "sb";
                case "_SD_Operation":
                    return "sd";
                case "_SLEEP_Operation":
                    return "sleep";
                case "_SS_Operation":
                    return "ss";
                case "_S_Operation":
                    return "s";
                case "_YIELD_Operation":
                    return "yield";
                case "_NOOP_Operation":
                default:
                    return null;
            }
        }

#if DEBUG
        public void Debug(string sourceLine, int lineNum)
        {
        }
#endif

        private static (Variable, Variable, Variable, Variable, Variable, Variable) TranslateArguments(object lineOfCode, int lineNumber, string instruction, Traverse traverse, List<string> aliases, List<string> defines)
        {
            ArgumentTraverse args = new ArgumentTraverse(lineOfCode, instruction, traverse, aliases, defines);
            if (instruction == null || !InstructionTranslators.TryGetValue(instruction, out var translator))
            {
                return (Variable.None, Variable.None, Variable.None, Variable.None, Variable.None, Variable.None);
            }
            return translator(args, lineNumber);
        }
    }

    public class InvalidProcessor : ChipProcessor
    {
        public InvalidProcessor(ProgrammableChip chip) : base(chip, null, null) { }

        public override void Execute(int runCount)
        {
        }
    }
}
