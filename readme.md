# UK Train System Cross-platform .NET Plugin for openBVE

This repository contains a **mirror** of Railsimroutes' UK Train System (UkTrainSys) plugin. The original project page is located at [http://railsimroutes.net/libraries/uktrainsys/index.php](http://railsimroutes.net/libraries/uktrainsys/index.php). 


## Downloads

Downloads can be found on [the Release page](https://github.com/joeyfoo/UkTrainSys/releases).

-----

## Project description

**Information below this line is copied from the [project page](http://railsimroutes.net/libraries/uktrainsys/index.php).**

On this page, you can download a new UK Train System cross-platform .NET plugin (UkTrainSys) designed for use with UK trains developed for openBVE. The inspiration behind the development of this plugin, is odakyufan's plugin for his Chashinai Railway project, as well as Simon Gathercole's previous generation of UKXXx.dll range of plugins for BVE 4. This new plugin is designed specifically for openBVE and written in C#, is cross-platform compatible, features the functionality of diesel and electric trains in a single assembly, and will feature more sophisticated simulation once the project develops further. This new plugin is also open source, as well as being released into the public domain.

*Please note that this project is ongoing.* An alpha version of the plugin is available below, complete with source code. This alpha version is not perfect, and is intended primarily for testing and feedback from train developers, but anyone is welcome to give it a try, and non-Windows users of openBVE can also now benefit from plugin functionality with UK electric and diesel multiple units (to begin with).

## Further Information

This plugin is designed to be modular, and aims to simulate the major systems found in a range of UK trains in a realistic way, including the Automatic Warning System, Train Protection and Warning System, Driver Reminder Appliance, the Vigilance Device, and so-on. A simple AI guard is included, along with AI Support, which enables the plugin to assist openBVE's built-in AI human driver in automatically handling systems simulated by the plugin. Animations relating to the AI driver's interactions with cab controls are supported, as well. A range of power supplies are also simulated, including a battery, overhead supply, and a diesel engine. Other features, such as windscreen wipers and failure modes, will follow later.

Trains which use Simon Gathercole's UKMUt.dll, or UKSpt.dll, can use UkTrainSys.dll instead, although some configuration changes may be necessary with the train in question (especially with regard to the guard's buzzer).

If you are an openBVE train developer and decide to use the UkTrainSys plugin, then if you wish, you can contact me to let me know, and I can then let you know whenever the plugin is updated. The development of this plugin is an ongoing project, and sometimes I might make changes which require your train to be updated, if you want to use the latest version of the plugin. I'm not able to develop your train for you, but if a change to the plugin means that your train needs to be updated in order to continue working correctly, you can contact me for assistance, or I can make any configuration changes for you, if necessary. smiley

Current feature summary:

* Automatic Warning System (AWS);
* Train Protection and Warning System (TPWS) - Standard and Plus;
* Driver Reminder Appliance (DRA);
* Vigilance Device with reduced cycle time option;
* Traction and brake interlocks;
* Battery which can be discharged, recharged and overloaded;
* Overhead supply, including neutral sections;
* Pantograph, including ACB/VCB;
* Automatic Power Control;
* Power supply and electrical system circuit breakers (for future use);
* Diesel engine support, with starter motor/shutdown and randomised engine stalls;
* In-cab blower;
* Head and tail lights;
* AI guard for station stop monitoring and buzzer signals;
* AI Support which assists openBVE's AI human driver in automatically handling systems simulated by the plugin (including support for visible in-cab driver's hands and arms).

Planned feature summary:

* Windscreen wipers and rain effects;
* Overhead supplies catering for different supply voltages;
* More advanced diesel engine simulation;
* Continuous Automatic Warning System (CAWS) support
* Third rail power supply support;
* Ammeters and overload protection;
* Tap-changer support;
* ERTMS/ETCS support;
* ATP support;
* RETB support;
* Tripcock support;
* Random failure modes.

## Recent Changes

### Updated 9th February 2011: v0.3.1.91

Fixes:

* If the Driver Reminder Appliance (DRA) is disabled via the UkTrainSys.cfg file, it's no longer possible for the state of the device to be set to SafetyStates.Activated, preventing unwanted Interlock Manager interventions.

### Updated 25th December 2010: v0.3.1.9

Changes:

* Added initial support for diesel trains which use Simon Gathercole's UKSpt.dll. Much of Simon's complex diesel engine model is reproduced by the UkTrainSys plugin, rather than the simple model, which isn't supported. This means that UkTrainSys includes the requirement to hold the engine starter button down until the engines are running, as well as simulating the starter motor, and a percentage likelihood that the engine will stall on starting (default 25%). The diesel engine class is implemented as another kind of power supply, like the battery, so it can provide electrical power, or be overloaded, for example. I'll model fuel consumption at a later date.

* Added diesel engine startup and restart procedures to the AI Support feature.

* Implemented the optional Vigilance Device reduced cycle time of 45 seconds, when the power notch is 6 or 7 (for use with UK Sprinter trains).

* Changed the conditions under which the vigilance device inactivity timer can be reset. Now, the timer is only reset if the power/brake controller is returned to the Off or 0 position - moving between different power or brake notches without returning the controller to 0, no longer resets the timer. Also, sounding the horn will now reset the vigilance timer, too.

* Improved the AI Support's handling of the Driver Reminder Appliance (DRA). Now, the AI Support will only activate the DRA if the train is 150 metres in the rear of the upcoming signal showing a red aspect, or nearer. This distance in the rear of the signal, within which the DRA will be operated by the AI Support, can be configured via the UkTrainSys.cfg file.

* Changed the AI Support related panel indices, to accomodate a solution for the infamous freaky anomalous multiple-arm phenomenon (backwards incompatible change).

* Changed the default Enabled states, so that if no configuration file is found, all systems are disabled by default. The Interlock Manager is also now disabled by default, under these circumstances. This means that if a new configuration file had to be created, then no systems will be simulated, but the train can still be driven successfully.

* Made some adjustments regarding the internal reverser position. The Interlock Manager's DemandTractionPowerCutoff() method, now sets the internal reverser to neutral, as well as setting the internal power handle position to zero, so that regenerative braking is also disabled along with traction power.

* The AI driver now visibly interacts with the reverser handle in many, but not all, cases (backwards incompatible change).

* Expanded the range of optional Data values which UkTrainSys will reconise, when passed as a parameter of a .Beacon 50 command. A new value of 40 informs the plugin of an upcoming terminal station stop, and a value of 41, instructs the AI Support to lower the pantograph (or stop the diesel engine) the next time the train stops and the doors open. This also resolves the situation where openBVE's AI and the plugin repeatedly override each other at a terminal station. .Beacon 50 can also now instruct the AI Support to return the power handle to the off position, prior to a neutral section. Please see the documentation for more details.

* The AI Guard has been expanded to accomodate multiple stopping locations at a station, passed via multiple .Beacon 24 commands. UkTrainSys selects the appropriate beacon to act upon, depending upon how many cars the player's train has. Please see the documentation for more details.

* When a brake demand is issued, emergency brakes are now applied, rather than service brakes.

* The time taken to lower the pantograph is now half of the specified pantograph rising time.

* The simulation of the Train Protection and Warning System (TPWS) is now much more realistic. The previous behaviour, emulated the functionality found in Simon Gathercole's range of Windows-only BVE4 plugins. This was adequate in many cases, however it didn't work in the same way that the real TPWS works in all cases, particularly with regard to the Overspeed Sensor System (OSS) and Trainstop Sensor System (TSS). In the case of the OSS, with Simon's plugin, a single beacon was used, which specified a maximum permitted speed. If the train was travelling above this speed when passing the beacon, a TPWS brake demand was issued. This did not, however, take braking or deceleration curves into account. With the TSS, a single beacon was also used; to the user, this worked prototypically, although it's an over-simplification of how the real system works.

* The new UkTrainSys implementation of TPWS, works just like the real system. An OSS can now be comprised of a pair of induction loops (beacons), each emitting a unique frequency (specified via the optional Data parameter), and the spacing between the beacons determines the permissible speed. UkTrainSys features a pair of OSS timers; each is independent of the other, and each is armed and triggered by specific frequencies. This means that UkTrainSys also supports the interleaving and nesting of OSS induction loops, as well as realistic behaviour when travelling backwards over the beacons.

* UkTrainSys also now supports realistic TSS behaviour. Each TSS can now be comprised of two induction loops, just like in reality, each emitting one of two specific frequencies, as with the OSS induction loops. One frequency arms the TSS, and the other frequency triggers a TSS brake demand, provided that it is detected while still within detection range of the TSS arming induction loop. As with the OSS, the TSS supports a pair of detection processes which operate independently of each other, each armed and triggered by specific frequencies. This also allows for fully realistic behaviour while travelling backwards over TSS installations (more for future use).

* It should be noted, that UkTrainSys does still support the old, legacy behaviour in use on existing routes, but developers creating new routes for openBVE, are encouraged to use the new, realistic TPWS simulation instead, not only because it's more realistic, but because it's more flexible, and possibly more future-proof if openBVE supports networked tracks in future. Please see the documentation for more details.

* The simulation of the Automatic Warning System (AWS) has been enhanced, and now works just like the real AWS. UkTrainSys now supports the detection of the permanent magnet, and the electromagnet, allowing for a fully realistic AWS implementation, using only a single beacon type.

* The new AWS simulation includes the 1000 ms delay timer for detection of the electromagnet. Upon passing the permanent magnet, the AWS is set and the sunflower instrument goes black, and only if the electromagnet is detected, is the AWS reset (i.e. the bell/bing sound is heard). If no electromagnet is detected within the delay period, then an AWS warning is issued, and the sunflower is displayed.

* As with the real AWS, travelling at a very low speed over an AWS inductor associated with a signal showing a clear (green) aspect, will initially trigger an AWS warning (because the electromagnet isn't detected in time), and then, if the warning isn't cancelled, the AWS is reset automatically as the electromagnet is detected (and the bell/bing is then heard).

* The new UkTrainSys AWS simulation also supports AWS suppression, via the insertion of beacons either immediately before or after the permament magnet beacon. This can be used to suppress the AWS in one or both directions of travel, although this feature is more for future use, should openBVE support bi-directionally signalled lines in future.

* As with the TPWS simulation, UkTrainSys does still support the old, legacy AWS implementation in use on existing routes, but again, developers creating new routes for openBVE, are encouraged to use the new AWS simulation instead, for the same reasons as the new TPWS simulation is preferred. Please see the documentation for more details.

Fixes:

* Fixed a crash which would occur, if there was no power supply enabled.
* Fixed an issue, where the Vigliance Device inactivity timer could have an excessively large value assigned, when jumping to a previous station.

### 28th November 2010: v0.3.1.3

Changes:

* Updated the plugin for compatibility with openBVE v1.2.9.20; changed the SetSignal() method implementation.

### 26th November 2010: v0.3.1.2

Changes:

* Support for TPWS+ (Plus) installations is now approved. Please see the documentation for beacon details.
* The AI support now sets and resets the DRA upon stopping the train, depending upon the state of the upcoming aspect.
* Known issue: There's currently a minor problem I've noticed, where at Birmingham New Street on the Cross-City South route for example, the AI arm reaches out to operate the DRA switch, but waits for 10 seconds before actually doing it. It seems that openBVE isn't calling the PerformAI() method for that time period, for some reason.
* Added an "Offset Beacon Receiver Manager", which stores information about certain types of beacon when the SetBeacon() method is called. Specifically handled, are beacons for which in reality, the beacon reciever should not be located at the front of the train, which is where the openBVE "beacon receiver" is located. This allows a train with an offset beacon receiver to travel backwards over beacon locations, with the action associated with the beacon, being triggered at the correct location, as though the actual beacon receiver were not at the front of the train.
* Neutral section, pantograph and air-blast/vacuum circuit breaker (ACB/VCB) simulation has been enhanced. Beacon 20 now allows for both the APC magnets and the neutral section itself to be simulated seperately. If the train passes a pair of APC magnets which causes the Automatic Power Control system to command open the ACB/VCB, and the train's speed is insufficient, such that the train stops, then the pantograph up/reset button can be pressed to re-close the ACB/VCB. If the train's pantograph/APC receiver location is not within the 4 metre long neutral section of overhead line, then overhead power is still available, which will allow the driver to move the train forwards or backwards, depending upon which side of the neutral section the train is stranded on. The driver can then accelerate to a sufficient speed in order to coast through the neutral section successfully this time. If the train's pantograph stops within the 4 metre long neutral section, then there is no overhead power, no matter whether the ACB/VCB is re-closed or not.
* Note: This works whether the train is travelling forwards or backwards, even if the APC receiver is located at the back of the train (as would be the case with an electric loco pushing a rake of coaches and a Driving Van Trailer, from the rear).

Fixed:

* The plugin no longer crashes if AI support is enabled on a route with no sections defined.

### 21st November 2010: v0.3.1.1

Changes:

* Added a timer for checking whether or not the PerformAI() method has been called by the host application within the last 10 seconds. If not, the plugin assumes that the AI driver is no longer enabled in the host application, and the AI arms and hands are no longer shown. [Thanks to Michelle for the suggestion]
* Fixed some issues with the TPWS Isolation feature not working correctly (it hadn't been tested properly - my apologies).
* Added a delay after the AI driver has responded to the guard's ready-to-start buzzer signal, to prevent the AI from taking power, so the left hand has enough time to transition from the buzzer signal button to the traction/brake controller, before the latter is moved on departure.
* Updated the Interlock Manager brake release and traction power conditions.

### 20th November 2010: v0.3.1.0

Changes:

* Added plugin variables in the 50-52 range, to support visible driver's arms and hands interacting with cab controls when openBVE's AI human driver is enabled.
* Debug mode is now disabled by default, if no existing configuration file is found.
* Added support for a guard's buzzer indicator light (plugin variable 25) [Thanks to Steve Green for the suggestion]
* Added plugin variables (15 and 16) to support individual left and right door indicators.
* Added an IsPlaying() method to the SoundManager class, which returns a boolean indicating whether or not a specified sound index is currently playing.
* Guard's buzzer signals are now handled by repeatedly playing a single buzzer sound as many times as is necessary, making the dedicated "set back", "draw forward" and "ready to start" buzzer sounds obsolete. These audio files and associated sound variables are no longer supported by the UkTrainSys plugin. This change has been made, to enable the guard's buzzer indicator light to illuminate whenever a buzzer sound is being played, and to extinguish between each playback of an individual buzzer sound.
* Added the guard's "ready to start" buzzer signal.
* Expanded the AI Support feature to automatically respond to the guard's buzzer signals with the driver's own buzzer response.
* Added support for a new beacon (50), which can instruct the AI driver to perform an action within the cab, depending upon the optional Data parameter. Currently, this beacon can be used to instruct the AI driver to sound the horn, either with a low tone followed by a high tone on the horn, or vice-versa.
* Added support for the Automatic Power Control system and beacon 20, which represents the APC magnets. Both new and legacy beacon 20 behaviour is implemented; if the Data parameter is greater than zero, legacy behaviour is used, which is compatible with existing routes which use this beacon. If the Data parameter is zero or om itted entirely, the new behaviour is used, which requires a .Beacon 20 command both before and after the neutral section.
* Optimised the way in which strings are handled, and reorganised the files within the solution. [Many thanks to Mustafa Selcuk Oral or the feedback]

### 11th November 2010: v0.3.0.2

Changes:

* Discarded the Plugin.TrainSpecifications class in favour of using OpenBveApi.Runtime.VehicleSpecs; altered debug message handling to make use of the StringBuilder class instead, and put an end to apostrophe abuse. [Thanks to Michelle for the feedback ;-)]
* Altered TPWS Overspeed Sensor behaviour, such that the speed of the train only matters at the instant the final OSS loop is passed over. [Thanks to Steve Green for the feedback]
* The plugin now ignores all beacons when jumping to a new station.
* Minor improvements to the horn lever automatic return-to-centre feature.

### 8th November 2010: v0.3.0.0

* Initial release for evaluation and testing.
