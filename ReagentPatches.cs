using System.Collections.Generic;
using Assets.Scripts.Objects;
using HarmonyLib;

namespace EntropyFix
{
	[HarmonyPatch(typeof(Reagents.Reagent), nameof(Reagents.Reagent.GenerateReagentTypeLookup))]
	public static class ReagentGenerateReagentTypeLookupPatches
	{
		public static void Postfix()
		{
			var traverse = Traverse.Create<Reagents.Reagent>();
			var reagentLookup = traverse.Field("_reagentLookup").GetValue<Reagents.Reagent[]>();
			var reagentHashLookup = traverse.Field("_reagentHashLookup").GetValue<Dictionary<int, Reagents.Reagent>>();
			Dictionary<Item, Reagents.Reagent> reagentItems = new Dictionary<Item, Reagents.Reagent>
			{
				{ (Item)Prefab.Find("ItemFlour"), new Reagents.Flour() },
				{ (Item)Prefab.Find("ItemMilk"), new Reagents.Milk() },
				{ (Item)Prefab.Find("ItemEgg"), new Reagents.Egg() },
				{ (Item)Prefab.Find("ItemPotato"), new Reagents.Potato() },
				{ (Item)Prefab.Find("ItemTomato"), new Reagents.Tomato() },
				{ (Item)Prefab.Find("ItemPumpkin"), new Reagents.Pumpkin() },
				{ (Item)Prefab.Find("ItemRice"), new Reagents.Rice() },
				{ (Item)Prefab.Find("ItemCorn"), new Reagents.Corn() },
				{ (Item)Prefab.Find("ItemWheat"), new Reagents.Wheat() },
				{ (Item)Prefab.Find("ItemMushroom"), new Reagents.Mushroom() },
				{ (Item)Prefab.Find("ItemSoybean"), new Reagents.Soy() },
				{ (Item)Prefab.Find("ItemSoyOil"), new Reagents.Oil() },
				{ (Item)Prefab.Find("ItemFern"), new Reagents.Fenoxitone() },
				{ (Item)Prefab.Find("ItemIronIngot"), new Reagents.Iron() },
				{ (Item)Prefab.Find("ItemIronOre"), new Reagents.Iron() },
				{ (Item)Prefab.Find("ItemGoldIngot"), new Reagents.Gold() },
				{ (Item)Prefab.Find("ItemGoldOre"), new Reagents.Gold() },
				{ (Item)Prefab.Find("ItemCopperIngot"), new Reagents.Copper() },
				{ (Item)Prefab.Find("ItemCopperOre"), new Reagents.Copper() },
				{ (Item)Prefab.Find("ItemSilverIngot"), new Reagents.Silver() },
				{ (Item)Prefab.Find("ItemSilverOre"), new Reagents.Silver() },
				{ (Item)Prefab.Find("ItemNickelIngot"), new Reagents.Nickel() },
				{ (Item)Prefab.Find("ItemNickelOre"), new Reagents.Nickel() },
				{ (Item)Prefab.Find("ItemCoalOre"), new Reagents.Hydrocarbon() },
				{ (Item)Prefab.Find("ItemSolidFuel"), new Reagents.Hydrocarbon() },
				{ (Item)Prefab.Find("ItemLeadIngot"), new Reagents.Lead() },
				{ (Item)Prefab.Find("ItemLeadOre"), new Reagents.Lead() },
				{ (Item)Prefab.Find("ItemSiliconIngot"), new Reagents.Silicon() },
				{ (Item)Prefab.Find("ItemSiliconOre"), new Reagents.Silicon() },
				{ (Item)Prefab.Find("ItemCobaltOre"), new Reagents.Cobalt() },
				{ (Item)Prefab.Find("ItemUraniumOre"), new Reagents.Uranium() },
				{ (Item)Prefab.Find("ItemSteelIngot"), new Reagents.Steel() },
				{ (Item)Prefab.Find("ItemElectrumIngot"), new Reagents.Electrum() },
				{ (Item)Prefab.Find("ItemInvarIngot"), new Reagents.Invar() },
				{ (Item)Prefab.Find("ItemConstantanIngot"), new Reagents.Constantan() },
				{ (Item)Prefab.Find("ItemSolderIngot"), new Reagents.Solder() },
				{ (Item)Prefab.Find("ItemWaspaloyIngot"), new Reagents.Waspaloy() },
				{ (Item)Prefab.Find("ItemStelliteIngot"), new Reagents.Stellite() },
				{ (Item)Prefab.Find("ItemInconelIngot"), new Reagents.Inconel() },
				{ (Item)Prefab.Find("ItemHastelloyIngot"), new Reagents.Hastelloy() },
				{ (Item)Prefab.Find("ItemAstroloyIngot"), new Reagents.Astroloy() },
				{ (Item)Prefab.Find("ReagentColorRed"), new Reagents.ColorRed() },
				{ (Item)Prefab.Find("ReagentColorGreen"), new Reagents.ColorGreen() },
				{ (Item)Prefab.Find("ReagentColorBlue"), new Reagents.ColorBlue() },
				{ (Item)Prefab.Find("ReagentColorYellow"), new Reagents.ColorYellow() },
				{ (Item)Prefab.Find("ReagentColorOrange"), new Reagents.ColorOrange() },
				{ (Item)Prefab.Find("HumanSkull"), new Reagents.Carbon() },
				{ (Item)Prefab.Find("ItemCharcoal"), new Reagents.Carbon() },
				{ (Item)Prefab.Find("ItemBiomass"), new Reagents.Biomass() },
			};
			foreach (var item in reagentItems)
			{
				var hash = item.Key.GetPrefabHash();
				if (!reagentHashLookup.ContainsKey(hash))
					reagentHashLookup.Add(hash, item.Value);
			}
		}
	}
}
