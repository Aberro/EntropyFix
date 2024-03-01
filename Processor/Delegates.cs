using Assets.Scripts.Objects.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntropyFix.Processor
{
    public partial class ChipProcessor
    {
        public delegate int ProcessorCode(int line, ChipProcessor processor, double[] registers, Alias[] aliases);

        public delegate (Variable, Variable, Variable, Variable, Variable, Variable) ArgumentsTranslator(ArgumentTraverse traverse, int lineNumber);

        public delegate void LineGenerator(LineOfCode line, CodeGeneratorData data, int lineNumber);
    }
}
