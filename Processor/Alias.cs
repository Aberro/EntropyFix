using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Pipes;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EntropyFix.Processor
{
    public partial class ChipProcessor
    {
        public struct Alias
        {
            private readonly Func<double> _read;
            private readonly Action<double> _write;
            private readonly Func<ILogicable> _readDevice;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public double GetValue(int lineNum) => Target == AliasTarget.Register
                ? _read()
                : throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetValue(double value, int lineNum)
            {
                if (Target == AliasTarget.Register)
                {
                    _write(value);
                }
                else
                    throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ILogicable GetDevice(int lineNum) => Target == AliasTarget.Device
                ? _readDevice()
                : throw new ProgrammableChipException(ProgrammableChipException.ICExceptionType.IncorrectVariableType, lineNum);

            public AliasTarget Target
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => this._readDevice != null ? AliasTarget.Device : this._read != null && this._write != null ? AliasTarget.Register : AliasTarget.None;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Alias(Func<double> read, Action<double> write)
            {
                _read = read;
                _write = write;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Alias(Func<ILogicable> read)
            {
                _readDevice = read;
            }
        }

        private static readonly ConstructorInfo AliasRegisterConstructorInfo = typeof(Alias).GetConstructor(new[] { typeof(Func<double>), typeof(Action<double>) });
        private static readonly ConstructorInfo AliasDeviceConstructorInfo = typeof(Alias).GetConstructor(new[] { typeof(Func<ILogicable>) });
        private static readonly MethodInfo AliasGetValueMethodInfo = typeof(Alias).GetMethod(nameof(Alias.GetValue));
        private static readonly MethodInfo AliasSetValueMethodInfo = typeof(Alias).GetMethod(nameof(Alias.SetValue));
        private static readonly MethodInfo AliasGetDeviceMethodInfo = typeof(Alias).GetMethod(nameof(Alias.GetDevice));
    }
}
