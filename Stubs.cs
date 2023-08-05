using Assets.Scripts.Objects.Pipes;
using HarmonyLib;
using System;
using System.Runtime.CompilerServices;

namespace EntropyFix
{

	[HarmonyPatch(typeof(DeviceAtmospherics), nameof(DeviceAtmospherics.OnAtmosphericTick))]
	public class DeviceAtmosphericsOnAtmosphericTickStub
	{
		[HarmonyReversePatch]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void OnAtmosphericTick(DeviceAtmospherics instance)
		{
			throw new NotImplementedException("This is a stub method that should never be executed");
		}
	}
}
