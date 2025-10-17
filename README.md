# DamageMeter

A BepInEx mod for MycoPunk that displays real-time damage and kill statistics with DPS calculations.

## Description

This client-side mod adds a comprehensive damage meter HUD to MycoPunk, showing total damage dealt, damage per second (DPS), targets killed, and cores destroyed. The meter uses a rolling 5-second damage window for accurate DPS calculations and provides mission-wide statistics throughout gameplay.

The mod integrates seamlessly with the game's existing HUD system, positioning the meter near the reticle for optimal visibility. Press F5 to toggle the meter's visibility during gameplay. Stats are automatically reset at the start of each mission and provide detailed breakdowns of your combat performance.

## Getting Started

### Dependencies

* MycoPunk (base game)
* [BepInEx](https://github.com/BepInEx/BepInEx) - Version 5.4.2403 or compatible
* .NET Framework 4.8

### Building/Compiling

1. Clone this repository
2. Open the solution file in Visual Studio, Rider, or your preferred C# IDE
3. Build the project in Release mode

Alternatively, use dotnet CLI:
```bash
dotnet build --configuration Release
```

### Installing

**Option 1: Via Thunderstore (Recommended)**
1. Download and install using the Thunderstore Mod Manager
2. Search for "DamageMeter" under MycoPunk community
3. Install and enable the mod

**Option 2: Manual Installation**
1. Ensure BepInEx is installed for MycoPunk
2. Copy `DamageMeter.dll` from the build folder
3. Place it in `<MycoPunk Game Directory>/BepInEx/plugins/`
4. Launch the game

### Executing program

Once the mod is loaded, the damage meter will appear automatically when you start a mission:

1. **View Statistics:**
   - The meter displays in the upper-center area near your reticle
   - Shows total damage dealt and overall DPS since mission start
   - Displays damage dealt in the last 5 seconds and current DPS
   - Tracks total targets killed and rate per second
   - Tracks cores killed and rate per second

2. **Toggle Visibility:**
   - Press F5 during gameplay to show/hide the meter
   - Meter is automatically hidden in menus and non-gameplay screens

3. **Configuration:**
   - Adjust the DPS rolling window time (default 5 seconds) in BepInEx config
   - This affects how smooth or responsive the DPS meter is

All statistics update in real-time as you deal damage and defeat enemies.

## Help

* **Meter not showing?** Make sure you're in a mission and try pressing F5 to toggle visibility
* **Stats not updating?** The mod only tracks damage dealt by the local player
* **Wrong position?** The HUD is positioned relative to your reticle - it will move if your reticle moves
* **Configuration not working?** Restart the game after changing BepInEx config settings
* **Performance issues?** The mod has minimal impact but disable it if you experience issues
* **Not compatible?** This mod patches PlayerData damage callbacks and MissionManager. Other mods modifying these may conflict

## Authors

* Sparroh
* funlennysub (original mod template)
* [@DomPizzie](https://twitter.com/dompizzie) (README template)

## License

* This project is licensed under the MIT License - see the LICENSE.md file for details
