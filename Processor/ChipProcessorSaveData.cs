using Assets.Scripts.Objects.Electrical;
using System.Xml.Serialization;

namespace EntropyFix.Processor
{
    [XmlInclude(typeof(ProgrammableChipSaveData))]
    public class ChipProcessorSaveData : ProgrammableChipSaveData
    {
        [XmlElement]
        public double SleepDuration = 0.0;
        [XmlElement]
        public double Slept = 0.0;
    }
}
