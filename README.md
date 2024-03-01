# EntropyFix
A modpack including various configurable features, mostly to make the game more physically realistic or visually appealing. Press F6 to configure mod.
### Disclaimer: this project is under unlicense license. You may use it, copy, even steal, whatever you want. The likely future of this mod is to become abandoned after a while, when I'll stop playing Stationeers, so it'd be only for the better for the modding community and for the gamers if you can somehow use it.
### This is currently a BepInEx mod, so you have to manually put this mod into a BepInEx plugins folder, or create a symlink from workshop folder for the game into plugins and patchers folders.
### Workshop page
https://steamcommunity.com/sharedfiles/filedetails/?id=3015218280
# List of features:
### Atmospheric patches
Makes many atmospheric devices more physically correct. The pumping ones are in the first place - volume pumps changed so they actually pumping and compressing their internal volume, their power usage depends on work done on a gas during pumping and limited by their nominal power. Which means, when input has much higher pressure than output, they will only consume 5W, until pressure is equalized. Rate of equalization in this case depends on pump's internal volume. When pressure is nearly equal or negative (meaning that input pressure is less than output pressure), pump's power consumption raises gradually with pressure difference, depending on compression ratio, until it's power consumption equalizes at it's nominal (i.e maximal) power consumption. Gases that are pumped out are getting colder, and gasses that are pumped in are getting warmer, according to ideal gas law.
List of devices: 
**Mixer** - pumps removed, now it equalizes the pressure between output and lowest pressure input.
**Volume Pump** - internal volume 10l, nominal power - 500W, have a reductor.
**Turbo Volume Pump** - internal volume 50l, nominal power - 1500W, have a reductor.
**Active Vent** - internal volume 50l, nominal power - 300W, without reductor.
**Powered Vent** - internal volume 150l, nominal power - 800W, without reductor
**Regulator** - internal volume 10l, nominal power - 500W, have a reductor.
**Advanced Furnace**, applies to all 3 pumps - internal volume regulated, up to 10l, nominal power - 250W, have a reductor.
**Air Conditioner** - pumps removed, but it still able to create a slight pressure difference. Also, power consumption depends on heat transfer, with efficiency at 1:4, meaning that per 1W of used power, air conditioner may move 4J of heat energy. This probably will be changed later on with more physical equations.
### Atmospheric regulator
Allows to set Power and Volume to many atmospheric devices that have pumps, to limit their power or reduce pumping volume, for more manageable plumbing. Also allows creation of passive pressure regulators that reduce gas heating on compression.
### SEGI
A fully-dynamic voxel-based global illumination system. SEGI provides indirect lighting and glossy reflections. Very performance hungry and currently lacks good enough presets. Hopefully, I'll be able to change at least later part later on.
### Heat Exchange patch
Makes heat exchanger more efficient, by increasing it's internal volume, surface area, enabling mixing internal atmosphere both with input and output pipes. Currently, not fully implemented.
### Entropy Fix
Disables thermal radiation for devices placed inside a room. By default, all devices with internal atmosphere dissipate heat through convection and radiation, with later part being regardless if there's an unobstructed view of space to radiate to. This patch makes additional check to ensure that device isn't placed in a room. When it is - the only way for it to dissipate it's internal atmosphere's heat is through convection. With vacuum in the room, this allows to keep heat inside a device indefinitely. Which is especially useful for furnaces.
### Smaller particles
Makes gas particles smaller (the ones that visually appear when tiles have pressure difference).
### No trails
Removes trails from gas particles. With previous patch on, these two make particles look more like a dust.
### Better Atmos Analyzer
Allows atmos analyzer to see more devices with internal atmospheres. This mainly includes heat exchanger, which have more than one internal atmosphere and therefore unable to be displayed on atmos analyzer.
### Advanced Tablet Writeable Mode
Allows IC writing into Mode logic of an Advanced Tablet and switch between cartridges. Also, this patch forces execution of an IC immediately when Advanced Tablet is turned on and resets IC when it's turned off. Making it possible to write an IC program to switch between cartridges.
### Advanced Tablet IC Fix
This patch stops IC execution when Advanced Tablet is turned off. By default, it continues to execute.
