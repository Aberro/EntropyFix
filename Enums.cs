using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntropyFix
{
	public enum PatchCategory
	{
		None,
		[DisplayName("Advanced Tablet writeable Mode")]
		[Description("Enables writing Mode logic value into Advanced Tablet to switch between cartridges.")]
		AdvancedTabletWriteableMode,
		[DisplayName("Advanced Tablet IC Fix")]
		[Description("Stops IC execution when Advanced Tablet is off.")]
		AdvancedTabletDisableIC,
		[DisplayName("Better Atmos Analyzer")]
		[Description("Allows Atmos Analyzer to show more devices with internal atmospheres.")]
		AtmosAnalyzerInternalAtmospheres,
		[DisplayName("Entropy Fix")]
		[Description("Disables thermal radiations for devices placed in a room.")]
		EntropyFix,
		[DisplayName("Heat exchange patch")]
		[Description("Makes Heat Exchanger more efficient.")]
		HeatExchangerPatch,
		[DisplayName("Smaller particles")]
		[Description("Makes gas particles smaller. The gas particles that appears in the world when there's wind or pressure/temperature difference.")]
		SmallerParticles,
		[DisplayName("No Trails")]
		[Description("Disables gas particle trails.")]
		NoTrails,
		[DisplayName("Atmospheric patches")]
		[Description("Changes gas/liquid devices more physical.")]
		AtmosphericPatches,
		[DisplayName("SEGI")]
		[Description("Enables SonicEther Global Illumination.")]
		SEGI,
	}

	public enum ConfigSection
	{
		[DisplayName("Features")]
		Features,
		[DisplayName("SEGI")]
		SEGI,
		[DisplayName("SEGI Settings")]
		SEGISettings,
	}
}
