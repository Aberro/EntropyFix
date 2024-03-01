using System;

namespace EntropyFix.Processor
{
    public partial class ChipProcessor
    {
        public enum VariableKind : byte
        {
            None,
            Alias,
            Define,
            Register,
            DeviceRegister,
            DeviceNetwork,
            Constant,
            EnumLogicType,
            EnumBatchMode,
            EnumLogicSlotType,
            EnumLogicReagentMode,
        }
        [Flags]
        public enum AliasTarget
        {
            None = 0,
            Register = 1,
            Device = 2,
        }
    }
}
