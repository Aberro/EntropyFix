namespace EntropyFix.Processor
{
    public partial class ChipProcessor
    {
        public struct LineOfCode
        {
#if DEBUG
            public string SourceLine;
#endif
            public readonly string Command;
            public readonly Variable Argument1;
            public readonly Variable Argument2;
            public readonly Variable Argument3;
            public readonly Variable Argument4;
            public readonly Variable Argument5;
            public readonly Variable Argument6;

            public static LineOfCode Empty = new();
            public LineOfCode(string command) : this(command, Variable.None, Variable.None, Variable.None, Variable.None, Variable.None, Variable.None) { }
            public LineOfCode(string command, Variable var1) : this(command, var1, Variable.None, Variable.None, Variable.None, Variable.None, Variable.None) { }
            public LineOfCode(string command, Variable var1, Variable var2) : this(command, var1, var2, Variable.None, Variable.None, Variable.None, Variable.None) { }
            public LineOfCode(string command, Variable var1, Variable var2, Variable var3) : this(command, var1, var2, var3, Variable.None, Variable.None, Variable.None) { }
            public LineOfCode(string command, Variable var1, Variable var2, Variable var3, Variable var4) : this(command, var1, var2, var3, var4, Variable.None, Variable.None) { }
            public LineOfCode(string command, Variable var1, Variable var2, Variable var3, Variable var4, Variable var5) : this(command, var1, var2, var3, var4, var5, Variable.None) { }
            public LineOfCode(string command, Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6)
            {
                Command = command;
                Argument1 = var1;
                Argument2 = var2;
                Argument3 = var3;
                Argument4 = var4;
                Argument5 = var5;
                Argument6 = var6;
            }

            public static bool operator==(LineOfCode a, LineOfCode b)
            {
                return a.Command == b.Command && a.Argument1 == b.Argument1 && a.Argument2 == b.Argument2 && a.Argument3 == b.Argument3 && a.Argument4 == b.Argument4 && a.Argument5 == b.Argument5 && a.Argument6 == b.Argument6;
            }

            public static bool operator !=(LineOfCode a, LineOfCode b)
            {
                return a.Command != b.Command || a.Argument1 != b.Argument1 || a.Argument2 != b.Argument2 || a.Argument3 != b.Argument3 || a.Argument4 != b.Argument4 || a.Argument5 != b.Argument5 || a.Argument6 != b.Argument6;
            }
        }
    }
}
