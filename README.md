## NOFFB - Force Feedback Plugin for Nuclear Option

# CURRENTLY KNOWN TO WORK WITH:
- Microsoft Sidewinder Force Feedback 2
 
(although should work with any FFB Joystick that conforms to standard X axis for Roll FFB, Y axis for Pitch FFB)


## installation and setup:
- step0: Installation is possible via [NOMM - Nuclear Option Mod Manager](https://github.com/Combat787/NuclearOptionModManager/) - [DOWNLOAD](https://github.com/Combat787/NuclearOptionModManager/releases/latest)

[download and install the mod.](https://github.com/KopterBuzz/NOFFB/releases/latest) 
unpack the zipfile content into Plugins, or create a NOFFB folder inside Plugins and unpack everything there
also install [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases/download/v18.4.1/BepInEx.ConfigurationManager_BepInEx5_v18.4.1.zip) if not already installed

- step 1: run the NOFFBController.exe

this is a workaround because the way I was doing FFB effects was crashing the game previously, so had to offload to a separate process with its own domain, so when it bugs out it can't crash the game.
as seen on the screenshot, it will list FFB-capable devices. enter the index number of the one you want to the FFB effects to go to.

NOFFBController.exe talks to the NOFFB plugin via a local UDP network socket, that's how it receives instructions from the game to generate forces on your peripherals.
When you run it the first time Windows Defender may block the EXE because it is unsigned. Just let it run anyway.
It will also ask if you want it to be able to talk on private network. Also allow this. It doesn't go out to internet, only localhost:5001.

NOFFBController.exe may not launch unless you have Microsoft Desktop Runtime 10 installed on your PC, sorry about that.

!!NOFFBController.exe may force disable autocenter on your flight stick!!
i will come up with something to handle this better

- step 2: in configuration manager you will see relevant settings

FBW_Debugui shows/hides  the debug visualizer.
the green dot  is your stick position, the cyan dot is the direction and magnitude of the FBW pushback force

FFB_Gain 0.0 - 1.0 is a master gain multiplier for all FFB forces,

FFB_xAxisInvert - if the forces appear to be inverted on the ROLL axis, enable this
FFB_yAxisInvert - if the forces appear to be inverted on the PITCH axis, enable this

FFB_FBWPushBack_Factor - governs how quickly the pushback force starts to ramp up.

How FBW Pushback currently works: if the player stick input is greater than what the input filter enforced for the aircraft, it will start to generate an appropriate counter-force on the respective axis.

More effects will be implemented later, doing this the right way crash-free was a much bigger challenge than anticipated  