using Assets.Scripts.Atmospherics;
using HarmonyLib;
using UnityEngine;

namespace EntropyFix
{

	/// <summary>
	/// Fix for clamping.
	/// </summary>
	[HarmonyPatch(typeof(AtmosphereHelper), nameof(AtmosphereHelper.MoveVolume))]
	public class AtmosphereHelperMoveVolumePatch
	{
		public static bool Prefix(Atmosphere inputAtmos, Atmosphere outputAtmos, float volume, AtmosphereHelper.MatterState matterStateToMove)
		{
			float num = Mathf.Clamp01(volume / inputAtmos.GetVolume(matterStateToMove));
			if (num <= 0.0)
				return false;
			GasMixture gasMixture = inputAtmos.Remove(num * inputAtmos.GasMixture.TotalMoles(matterStateToMove), matterStateToMove);
			outputAtmos.Add(gasMixture);
			return false;
		}
	}
}
