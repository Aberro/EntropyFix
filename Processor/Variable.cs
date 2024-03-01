namespace EntropyFix.Processor
{
    public partial class ChipProcessor
    {
        public struct Variable
        {
            public VariableKind Kind = VariableKind.None;
            public short RegisterIndex = -1;
            public short RegisterRecurse = -1;
            public short AliasIndex = -1;
            public short DefineIndex = -1;
            public int NetworkIndex = -1;
            public double Constant = double.NaN;
            public Variable() { }
            public static Variable None => new();

            public static bool operator ==(Variable a, Variable b)
            {
                return a.Kind == b.Kind
                       && a.RegisterIndex == b.RegisterIndex
                       && a.RegisterRecurse == b.RegisterRecurse
                       && a.AliasIndex == b.AliasIndex
                       && a.NetworkIndex == b.NetworkIndex
                       && a.Constant == b.Constant;
            }

            public static bool operator !=(Variable a, Variable b)
            {
                return a.Kind != b.Kind
                       || a.RegisterIndex != b.RegisterIndex
                       || a.RegisterRecurse != b.RegisterRecurse
                       || a.AliasIndex != b.AliasIndex
                       || a.NetworkIndex != b.NetworkIndex
                       || a.Constant != b.Constant;
            }
        }
    }
}
