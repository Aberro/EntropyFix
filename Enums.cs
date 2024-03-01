using System.ComponentModel;

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
        [DisplayName("Better Config Cartridge")]
        [Description("Config cartridge would display more IC related data.")]
        ConfigCartridgeDebug,
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
		[DisplayName("Atmospherics regulation")]
		[Description("Allows to set Power and Volume logic value to atmospheric devices with pumps to limit pumping and allow passive pressure regulator.")]
		AtmosphericRegulatorPatches,
		[DisplayName("SEGI")]
		[Description("Enables SonicEther Global Illumination.")]
		SEGI,
		[DisplayName("No incorrect matter state")]
		[Description("Disables incorrect matter state damage to pipes. Frozen matter will reduce pipe volume instead.")]
		NoIncorrectMatterState,
		[DisplayName("Programmable chip patches")]
		[Description("Expands programm lines number to 4096, and program size limit to 65536")]
		ProgrammableChipPatches,
		[DisplayName("Programmable chip replacement")]
		[Description("Replaces programmable chip command processor with a modded, more optimized version.")]
		ProgrammableChipReplacement,
		//[DisplayName("Realistic state change")]
		//[Description("Makes state change physics more realistic. Evaporation, condensation and solidification depends only on energy balance, no more superheated or supercooled liquids/gases.")]
		//StateChangePatches,
	}

	public enum ConfigSection
	{
		[DisplayName("Features")]
		Features,
		[DisplayName("Atmospheric settings")]
		AtmosphericPatchesSettings,
		[DisplayName("SEGI")]
		SEGI,
		[DisplayName("SEGI Settings")]
		SEGISettings,
	}
}
