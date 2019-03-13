using System;
using System.Text;
using System.Globalization;
using System.Reflection;
using OpenBveApi.Runtime;

// TODO: Tests where some systems are disabled

/*
 * Description of how this plugin works:
 * =====================================
 * 
 * This plugin features multiple safety/operational system classes, multiple power supply classes, a power supply manager class, an interlock manager
 * class, a startup/self-test manager, and an offset beacon manager class, a cab controls class, a sound manager class, and AI guard class.
 * 
 * General design principle summary:
 * ---------------------------------
 * 
 *  # Classes representing functional electrical systems have power states, operational states (used as failure modes), and safety systems
 *    have a safety state. These classes can be instantiated.
 *  # Systems are continually updated via calls in the Elapse() method, although inner processing is carried out based upon conditions and states.
 *  # Safety state changes are initiated via methods, usually in the form of a command relevant to the system, called either when the host application
 *    issues an event, or by other systems. These methods can ensure that certain state changes are conditional and safe.
 *  # A system can read but not modify another system's state(s) directly, so the provided methods must be used, unless the state in question
 *    is managed by an outside manager-style class.  It is nevertheless good practice to call a suitable method when interacting with a system.
 *  # Manager-style classes are static, and cannot be instantiated. They can alter relevant states of managed systems.
 * 
 * Cab Controls class:
 * -------------------
 * 
 * This class stores the state of the "physical" in-cab controls (i.e. buttons, switches, handles, and so-on). The states within this class, are
 * changed via the KeyDown(), KeyUp(), SetPower(), SetBrake(), SetReverser(), and DoorChange() methods, as initiated by the host application. The cab
 * controls class is also responsible for determining the state of panel elements which represent physical controls. Safety system classes for example,
 * can also monitor the states within the cab controls class if required.
 * 
 * Safety System classes:
 * ----------------------
 * 
 * Each safety system class derives from the abstract ElectricalSystem class. All electrical systems connect directly to the power supply manager, have
 * required voltage and current values, circuit breakers which can be opened or closed, as well as "operative states", which can be used to determine
 * failure modes (in future).
 * 
 * Safety system classes do not directly control the power and brake handle positions. Rather, they issue "Demands" and "Requests" to the interlock
 * manager class, which itself determines when to lock the brakes on, or cut-off traction power, and also when the power and brake interlocks can be
 * released. Hence, a safety system class can /demand/ that the interlock manager apply the brakes or cut-off traction power immediately, but can only
 * /request/ that the brakes be released or traction power be restored.
 * 
 * Each safety system class has states, and an Update() method. The implementation within the Update() method of a safety system class defines its
 * behaviour based upon the current state of the safety system, and is called once during each Elapse() call, as initiated by the host application. Safety
 * system classes also have non-inherited methods which should be called via the the SetReverser(), SetBrake(), SetPower(), SetBeacon(), KeyDown() and
 * KeyUp() methods, to set safety system states when events from the host application are triggered. Safety systems also implement a Reset() method, with
 * the implied action carried out unconditionally, so should be used only where necessary, however the interlock manager will still retain control of the
 * brake and traction interlocks regardless. The inherited Enable() method should be used if a train is to be equipped with a particular safety system,
 * according to the configuration file. Safety system classes can also check the state of any relevant controls in the cab controls class, and update
 * themselves depending upon the states of those relevant controls, if necessary. Each safety system class has a Reinitialise() method, which should be
 * called via the Initialise() method, as initiated by the host application. This sets the safety system to a default state upon loading a route, or
 * jumping to a station.
 * 
 * Safety system classes directly control the state of relevant in-cab indications and playback of sounds (via the panel[] array and sound manager).
 * 
 * If safety systems need to know the state of other safety systems, then this can be accomplished in two ways. Firstly, the state of a safety system is
 * available via internal read-only properties. Secondly, safety systems can communicate with each other using events. For example, the Automatic Warning
 * System class publishes an event which occurs whenever the AWS reset button has been released. The Vigilance Device, along with the Train Protection and
 * Warning System, subscribe to this event, as they are both tied to the Automatic Warning System. If a safety system needs to change the state of another
 * safety system, it should call an appropriate method.
 * 
 * Interlock Manager:
 * ------------------
 * 
 * The interlock manager class has direct control over forcing the brakes to be applied, cutting off traction power, and determining when the brakes
 * can be released, and traction power restored. Safety system classes can issue brake demands to the interlock manager via its DemandBrakeApplication()
 * method, or demand that traction power be cutoff via the DemandTractionPowerCutoff() method. The Interlock Manager will always act immediately upon
 * these demands. However, safety systems can only make requests for the brake and power interlocks to be released again, via the RequestBrakeReset() and
 * RequestTractionPowerReset() methods. If such an interlock reset is requested, the interlocks will only be released if certain Interlock Manager rules and
 * behaviours permit it at the time of the request. If the reset is not permitted at the time, then the request remains pending, and will be granted whenever
 * the prerequisite conditions are eventually met. The Interlock Manager also checks the states of safety system classes or the cab controls class where
 * necessary, to determine whether a brake or traction power interlock should be released.
 * 
 * Power supply classes:
 * ---------------------
 * 
 * Note: This feature is more for future use, if and when openBVE supports clickable 3D cab controls, as such things as circuit breaker panels can be simulated then.
 * 
 * Power supply simulation has three main aspects; the power supply manager, a range of power supplies, and individual electrical systems. Power is primarily
 * handled via the power supply manager class. Electrical systems are first "connected" to the power supply manager, and then a single power supply can be
 * connected to the power supply manager at any given time, too. The power supply manager monitors all connected electrical systems to determine their total
 * voltage and current requirements. It also monitors the connected power supply to check whether it is able to supply enough current. If the power supply
 * becomes overloaded or otherwise has its circuit breaker tripped open, the power supply manager sets the power state of each connected electrical system
 * accordingly, so they no longer function until power is restored.
 * 
 * Each power supply has a maximum voltage and current rating, and monitors the power requirements of the power supply manager. If the power supply manager
 * current demand exceeds the available amperage of the power supply in question, the power supply will trip open its own circuit breaker in the event of
 * an overload condition. If a power supply's circuit breaker trips, it must be manually closed before the power supply will provide power again.
 * 
 * Train developers should configure power supplies to always have a sufficiently high current rating in all circumstances, until it becomes more practical
 * to simulate the ability to open and close individual circuit breakers by using the mouse to interact with a 3D cab.
 * 
 * Sound playback:
 * ---------------
 * 
 * The sound manager class provides Play(), Stop() and IsPlaying() methods, and deals with the sound handles passed from the host application. Calling the
 * Play() method with a specified sound index, starts playback of the sound, either once only, or looped continuously, with a specified pitch and volume.
 * 
 * If a sound is played without looping, and a subsequent call using the same sound index is made, but the sound hasn't finished playing and neither the
 * pitch or volume are different, the sound is stopped and then played again from the start. If playback of a non-looping sound index has not finished yet,
 * and another play request is made with the same sound index, but the the pitch or volume is different this time, then the sound continues playing with
 * the new pitch or volume.
 *
 * If a sound index is looped, subsequent calls to the Play() method allow for the volume and pitch to be changed, without sound playback stopping and
 * starting again.
 * 
 * The IsPlaying() method returns a boolean indicating whether or not a specified sound index is currently playing.
 * 
 * Offset Beacon Receiver Manager class:
 * -------------------------------------
 * 
 * This class stores information about certain types of beacon when the SetBeacon() method is called. Specifically handled, are beacons for which in reality,
 * the beacon reciever should not be located at the front of the train, which is where the openBVE "beacon receiver" is located. The offset beacon receiver
 * manager can assume the role of issuing trigger points to systems which respond to these beacons. This allows a train with an offset beacon receiver to
 * travel backwards over beacon locations, with the action associated with the beacon, being triggered at the correct location, as though the actual beacon
 * receiver were not at the front of the train.
 * 
 * A new beacon is added to the offset beacon receiver manager via the AddBeacon() method (which is done via the SetBeacon() method, as initiated by the host
 * application when passing a beacon location). A new beacon is only added if the train is travelling forwards at the time that AddBeacon() is called. Several
 * pieces of information are stored - the actual location of the beacon, the beacon type, the beacon's optional data, and a beacon receiver offset value. These
 * values are used to trigger an action associated with the beacon, at a location which takes the offset beacon reciever location into account, whether travelling
 * forwards, or backwards. When the train is travelling backwards, if the actual beacon receiver passes the location of a stored beacon, then the beacon information
 * is deleted from the stored beacon array, so that it can be registered again, if the train later passes the beacon while travelling forwards.
 * 
 * When the Reinitialise() method is called, i.e. when jumping to a station, any beacons stored by the offset beacon receiver manager are deleted. For a beacon
 * to be registered again, it must be added via the SetBeacon() method, when the train is travelling forwards.
 *
 */

namespace Plugin {
	/// <summary>The interface to be implemented by the plugin.</summary>
	public partial class Plugin : IRuntime {
		
		// --- members ---
		
		/// <summary>Whether or not the plugin is in Debug Mode and should output to the debug logfile.</summary>
		internal static bool DebugMode = false;
		/// <summary>The absolute path to the debug logfile.</summary>
		private static string DebugLogFile;
		/// <summary>The absolute path to the plugin file.</summary>
		private static string PluginPath;
		/// <summary>The absolute path to the train folder.</summary>
		private static string TrainPath;
		/// <summary>The absolute path to the configuration file.</summary>
		private static string ConfigFile;
		
		// initialisation
		
		/// <summary>Whether the plugin has been initialised for the first time via the Initialise() method.</summary>
		private static bool Initialised;
		/// <summary>Whether or not openBVE is jumping to a new station.</summary>
		/// <remarks>Set this to true when beacons should be ignored, or the train location needs to be updated before the next Elapse() call.</remarks>
		private static bool JumpingToStation;
		
		// timers
		
		/// <summary>The time elapsed since the last call to the Elapse() method, in milliseconds.</summary>
		internal static double TimeElapsed;
		/// <summary>The in-game time of day, in seconds.</summary>
		internal static double SecondsSinceMidnght;
		/// <summary>The time elapsed since the doors opened, in milliseconds.</summary>
		internal static double DoorsOpenTimer;
		/// <summary>Whether or not the doors ope timer is currently active.</summary>
		internal static bool DoorsOpenTimerActive;
		
		// train
		
		/// <summary>The current speed of the train in kilometres per hour.</summary>
		internal static double TrainSpeed;
		/// <summary>The current location of the train in metres.</summary>
		internal static double TrainLocation;
		/// <summary>The train specifications.</summary>
		internal static VehicleSpecs TrainSpecifications;
		
		// signaling states
		/// <summary>The last signal or section data reported via the SetSignal() method.</summary>
		internal static SignalData LastSignalData;
		
		// panel
		
		private int[] Panel;
		
		// system instantiation
		
		/// <summary>The main battery supply.</summary>
		internal static Battery MainBattery = new Battery();
		/// <summary>The overhead power supply.</summary>
		internal static OverheadPowerSupply OverheadSupply = new OverheadPowerSupply();
		/// <summary>The diesel engine.</summary>
		internal static DieselEngine Diesel = new DieselEngine();
		/// <summary>The Vigilance device.</summary>
		internal static VigilanceDevice Vig = new VigilanceDevice();
		/// <summary>The Automatic Warning System.</summary>
		internal static AutomaticWarningSystem Aws = new AutomaticWarningSystem();
		/// <summary>The Train Protection and Warning System.</summary>
		internal static TrainProtectionWarningSystem Tpws = new TrainProtectionWarningSystem();
		/// <summary>The Train Protection and Warning System.</summary>
		internal static DriverReminderAppliance Dra = new DriverReminderAppliance();
		/// <summary>The Headlights.</summary>
		internal static Headlights Head = new Headlights();
		/// <summary>The Taillights.</summary>
		internal static Taillights Tail = new Taillights();
		/// <summary>The in-cab blower.</summary>
		internal static Blower Fan = new Blower();
		/// <summary>The pantograph.</summary>
		internal static Pantograph Pan = new Pantograph();
		/// <summary>The automatic power control system.</summary>
		internal static AutomaticPowerControl Apc = new AutomaticPowerControl();
		/// <summary>The tap changer.</summary>
		internal static TapChanger Tap = new TapChanger();
		
		// --- interface functions ---
		
		/// <summary>Is called when the plugin is loaded.</summary>
		/// <param name="properties">The properties supplied to the plugin on loading.</param>
		/// <returns>Whether the plugin was loaded successfully.</returns>
		public bool Load(LoadProperties properties) {
			PluginPath = properties.PluginFolder;
			TrainPath = properties.TrainFolder;
			string failureReport;
			if (LoadConfigFile(out failureReport)) {
				if (Plugin.DebugMode) {
					DebugLogFile = System.IO.Path.Combine(TrainPath, "debug.log");
					Plugin.ReportLogEntry(
						string.Format(
							"{0}{2}{0}New Session: {1}{0}{2}{0}",
							Environment.NewLine,
							System.DateTime.Now.ToString(),
							"---------------------------------------------------------------------------"
						)
					);
					Plugin.ReportLogEntry(
						string.Format(
							"{0} {1} {2} [SUCCESS]",
							DateTime.Now.TimeOfDay.ToString(),
							this.GetType().Name.ToString(),
							MethodInfo.GetCurrentMethod().ToString()
						)
					);
				}
				Panel = new int[256];
				properties.Panel = Panel;
				SoundManager.Initialise(properties.PlaySound, 256);
				if (AiSupportFeature.Enabled) {
					properties.AISupport = AISupport.Basic;
				} else {
					properties.AISupport = AISupport.None;
				}
				return true;
			} else {
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} {1} {2} [FAIL] - {3}",
							DateTime.Now.TimeOfDay.ToString(),
							this.GetType().Name.ToString(),
							MethodInfo.GetCurrentMethod().ToString(),
							failureReport
						)
					);
				}
				properties.FailureReason = failureReport;
				return false;
			}
		}
		
		/// <summary>Is called when the plugin is unloaded.</summary>
		public void Unload() {
			if (Plugin.DebugMode) {
				Plugin.ReportLogEntry(
					string.Format(
						"{0}{2}{0}Ended Session: {1}{0}{2}",
						Environment.NewLine,
						System.DateTime.Now.ToString(),
						"---------------------------------------------------------------------------"
					)
				);
			}
		}
		
		/// <summary>Is called after loading to inform the plugin about the specifications of the train.</summary>
		/// <param name="specs">The specifications of the train.</param>
		public void SetVehicleSpecs(VehicleSpecs specs) {
			TrainSpecifications = new VehicleSpecs(specs.PowerNotches, specs.BrakeType, specs.BrakeNotches, specs.HasHoldBrake, specs.Cars);
		}
		
		/// <summary>Is called when the plugin should initialize or reinitialize.</summary>
		/// <param name="mode">The mode of initialization.</param>
		public void Initialize(InitializationModes mode) {
			/* We only want to initialise some things the first time this method is called
			 * by the host application */
			if (!Initialised) {
				
				/* Connect electrical systems to the power supply manager */
				PowerSupplyManager.ConnectSystems(Vig, Aws, Tpws, Dra, Head, Tail, Fan, Pan);
				
				/* Subscribe safety system event handling methods to other safety system events */
				Aws.AwsWarningAcknowledged += new EventHandler(StartupSelfTestManager.HandleAwsAcknowledgement);
				Aws.AwsWarningAcknowledged += new EventHandler(Vig.HandleAwsAcknowledgement);
				Aws.AwsWarningAcknowledged += new EventHandler(Tpws.HandleAwsAcknowledgement);
				Tpws.TpwsAwsBrakeDemandIssued += new EventHandler(Aws.HandleTpwsAwsBrakeDemand);
				Tpws.TpwsTssBrakeDemandIssued += new EventHandler(Aws.HandleTpwsTssBrakeDemand);
				Tpws.TpwsIsolated += new EventHandler(Aws.HandleTpwsIsolated);
				Tpws.TpwsIsolated += new EventHandler(Vig.HandleTpwsIsolated);
				Tpws.TpwsNotIsolated += new EventHandler(Aws.HandleTpwsNotIsolated);
				Tpws.TpwsNotIsolated += new EventHandler(Vig.HandleTpwsNotIsolated);
				
				/* Turn devices on or off, and set states accordingly, depending upon the initialisation mode at the first station */
				if (mode == InitializationModes.OnService) {
					AiSupportFeature.Reinitialise(mode);
					OffsetBeaconReceiverManager.Reinitialise(mode);
					CabControls.MasterSwitchOn = true;
					OverheadSupply.Reinitialise(mode);
					Diesel.Reinitialise(mode);
					StartupSelfTestManager.Reinitialise(mode);
					Tap.Reinitialise(mode);
					Pan.Reinitialise(mode);
					Apc.Reinitialise(mode);
					Vig.Reinitialise(mode);
					Aws.Reinitialise(mode);
					Tpws.Reinitialise(mode);
					Dra.Reinitialise(mode);
					Head.Reinitialise(mode);
					Tail.Reinitialise(mode);
					Fan.Reinitialise(mode);
					AiGuard.Reinitialise(mode);
					InterlockManager.Reinitialise(mode);
				} else if (mode == InitializationModes.OnEmergency) {
					AiSupportFeature.Reinitialise(mode);
					OffsetBeaconReceiverManager.Reinitialise(mode);
					CabControls.MasterSwitchOn = false;
					OverheadSupply.Reinitialise(mode);
					Diesel.Reinitialise(mode);
					StartupSelfTestManager.Reinitialise(mode);
					Tap.Reinitialise(mode);
					Pan.Reinitialise(mode);
					Apc.Reinitialise(mode);
					Vig.Reinitialise(mode);
					Aws.Reinitialise(mode);
					Tpws.Reinitialise(mode);
					Dra.Reinitialise(mode);
					Head.Reinitialise(mode);
					Tail.Reinitialise(mode);
					Fan.Reinitialise(mode);
					AiGuard.Reinitialise(mode);
					InterlockManager.Reinitialise(mode);
				} else if (mode == InitializationModes.OffEmergency) {
					AiSupportFeature.Reinitialise(mode);
					OffsetBeaconReceiverManager.Reinitialise(mode);
					CabControls.MasterSwitchOn = false;
					OverheadSupply.Reinitialise(mode);
					Diesel.Reinitialise(mode);
					StartupSelfTestManager.Reinitialise(mode);
					Tap.Reinitialise(mode);
					Pan.Reinitialise(mode);
					Apc.Reinitialise(mode);
					Vig.Reinitialise(mode);
					Aws.Reinitialise(mode);
					Tpws.Reinitialise(mode);
					Dra.Reinitialise(mode);
					Head.Reinitialise(mode);
					Tail.Reinitialise(mode);
					Fan.Reinitialise(mode);
					AiGuard.Reinitialise(mode);
					InterlockManager.Reinitialise(mode);
				}
				Initialised = true;
			} else {
				AiSupportFeature.Reinitialise(InitializationModes.OnService);
				OffsetBeaconReceiverManager.Reinitialise(InitializationModes.OnService);
				CabControls.MasterSwitchOn = true;
				OverheadSupply.Reinitialise(InitializationModes.OnService);
				Diesel.Reinitialise(InitializationModes.OnService);
				StartupSelfTestManager.Reinitialise(InitializationModes.OnService);
				Tap.Reinitialise(InitializationModes.OnService);
				Pan.Reinitialise(InitializationModes.OnService);
				Apc.Reinitialise(InitializationModes.OnService);
				Vig.Reinitialise(InitializationModes.OnService);
				Aws.Reinitialise(InitializationModes.OnService);
				Tpws.Reinitialise(InitializationModes.OnService);
				Dra.Reinitialise(InitializationModes.OnService);
				Head.Reinitialise(InitializationModes.OnService);
				Tail.Reinitialise(InitializationModes.OnService);
				Fan.Reinitialise(InitializationModes.OnService);
				AiGuard.Reinitialise(InitializationModes.OnService);
				InterlockManager.Reinitialise(InitializationModes.OnService);
			}
			
			/* Set this ignore beacons until the next call to Elapse() */
			JumpingToStation = true;
		}
		
		/// <summary>Is called every frame.</summary>
		/// <param name="data">The data passed to the plugin.</param>
		public void Elapse(ElapseData data) {
			/* Reset panel states */
			for (int i = 0; i < Panel.Length; i++) {
				Panel[i] = 0;
			}
			
			/* Update internally accessible variables */
			TrainSpeed = data.Vehicle.Speed.KilometersPerHour;
			TrainLocation = data.Vehicle.Location;
			TimeElapsed = data.ElapsedTime.Milliseconds;
			SecondsSinceMidnght = data.TotalTime.Seconds;
			
			// HACK: Check to see if the AI is enabled in the host application
			/* Check to see whether the PerformAI() method has been called within a certain time period (the timer is reset to zero on each call).
			 * If it hasn't, the AI driver has very likely been turned off in the host application, so tell the plugin that the AI driver is not enabled */
			if (AiSupportFeature.AiDriverEnabled) {
				/* Increment the AI enabled timer */
				AiSupportFeature.AiDriverEnabledTimer = AiSupportFeature.AiDriverEnabledTimer + (int)TimeElapsed;
				if (AiSupportFeature.AiDriverEnabledTimer > 10000) {
					/* Reset timers */
					AiSupportFeature.AiDriverEnabledTimer = 0;
					AiSupportFeature.Timer = 0;
					AiSupportFeature.TimerActive = false;
					/* Inform the plugin that the AI is not enabled any longer */
					AiSupportFeature.AiDriverEnabled = false;
				}
			}
			
			/* Increment the AI timer if required, or reset it */
			if (AiSupportFeature.TimerActive) {
				AiSupportFeature.Timer = AiSupportFeature.Timer + (int)TimeElapsed;
			} else if (AiSupportFeature.Timer != 0) {
				AiSupportFeature.Timer = 0;
			}
			
			/* Increment the doors open timer if required and play the door open recharge clicks, or reset the timer */
			if (DoorsOpenTimerActive && TrainSpeed == 0) {
				DoorsOpenTimer = DoorsOpenTimer + (int)TimeElapsed;
				if (DoorsOpenTimer > 9000) {
					if (StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.Initialised) {
						if (CabControls.DoorState == DoorStates.Left) {
							SoundManager.Play(SoundIndices.RechargeClicksLeft, 1.0, 1.0, false);
						} else if (CabControls.DoorState == DoorStates.Right) {
							SoundManager.Play(SoundIndices.RechargeClicksRight, 1.0, 1.0, false);
						} else if (CabControls.DoorState == DoorStates.Both) {
							SoundManager.Play(SoundIndices.RechargeClicksLeft, 1.0, 1.0, false);
						}
					}
					DoorsOpenTimerActive = false;
				}
			} else if (DoorsOpenTimer != 0) {
				DoorsOpenTimer = 0;
			}
			
			/* Update systems, and handle debug messages */
			StringBuilder builder = new StringBuilder();
			
			/* Miscellaneous debug messages */
			if (Plugin.DebugMode) {
				builder.AppendFormat("");
			}
			
			/* Update physical cab controls */
			CabControls.Update(TimeElapsed, Panel, builder);
			
			/* Update power supplies */
			MainBattery.Update(TimeElapsed, Panel, builder);
			OverheadSupply.Update(TimeElapsed, Panel, builder);
			Diesel.Update(TimeElapsed, Panel, builder);
			PowerSupplyManager.Update(TimeElapsed, builder);
			
			/* Update the startup self-test manager */
			StartupSelfTestManager.Update(TimeElapsed, data.Handles, Panel, builder);
			
			/* Update electrical systems */
			Vig.Update(TimeElapsed, Panel, builder);
			Aws.Update(TimeElapsed, Panel, builder);
			Tpws.Update(TimeElapsed, Panel, builder);
			Dra.Update(TimeElapsed, Panel, builder);
			Head.Update(TimeElapsed, Panel, builder);
			Tail.Update(TimeElapsed, Panel, builder);
			Fan.Update(TimeElapsed, Panel, builder);
			Tap.Update(TimeElapsed, Panel, builder);
			Pan.Update(TimeElapsed, Panel, builder);
			Apc.Update(TimeElapsed, Panel, builder);
			
			/* Update the AI guard and interlock manager */
			AiGuard.Update(TimeElapsed, TrainSpeed, Panel, builder);
			InterlockManager.Update(TimeElapsed, data.Handles, Panel, builder);
			
			/* Update the offset beacon manager */
			OffsetBeaconReceiverManager.Update(builder);
			
			/* Stop ignoring beacons if we are already doing so, or set any variables which need to know the train location */
			if (JumpingToStation) {
				AiSupportFeature.LastReportedSignalLocation = TrainLocation;
				JumpingToStation = false;
			}
			
			/* Assign the cumulative debug message */
			if (Plugin.DebugMode) {
				data.DebugMessage = builder.ToString();
			}
		}
		
		/// <summary>Is called when the driver changes the reverser.</summary>
		/// <param name="reverser">The new reverser position.</param>
		public void SetReverser(int reverser) {
			CabControls.ReverserPosition = (CabControls.ReverserStates)reverser;
			if (CabControls.ReverserPosition == CabControls.ReverserStates.Neutral) {
				Vig.Isolate();
				if (TrainSpeed != 0) {
					InterlockManager.DemandBrakeApplication();
					InterlockManager.DemandTractionPowerCutoff();
				}
			} else if (CabControls.ReverserPosition == CabControls.ReverserStates.Forward) {
				if (Tpws.SafetyState != TrainProtectionWarningSystem.SafetyStates.Isolated) {
					Vig.Reset();
				}
				InterlockManager.RequestBrakeReset();
				InterlockManager.RequestTractionPowerReset();
			} else if (CabControls.ReverserPosition == CabControls.ReverserStates.Backward) {
				if (Tpws.SafetyState != TrainProtectionWarningSystem.SafetyStates.Isolated) {
					Vig.Reset();
				}
				InterlockManager.RequestBrakeReset();
				InterlockManager.RequestTractionPowerReset();
			}
		}
		
		/// <summary>Is called when the driver changes the power notch.</summary>
		/// <param name="powerNotch">The new power notch.</param>
		public void SetPower(int powerNotch) {
			if (Tap.Enabled) {
				Tap.SetHandle(powerNotch);
			} else {
				if (Diesel.Enabled) {
					Diesel.ChangeRev(powerNotch);
				}
				CabControls.PowerPosition = powerNotch;
			}
		}
		
		/// <summary>Is called when the driver changes the brake notch.</summary>
		/// <param name="brakeNotch">The new brake notch.</param>
		public void SetBrake(int brakeNotch) {
			CabControls.BrakePosition = brakeNotch;
		}
		
		/// <summary>Is called when a virtual key is pressed.</summary>
		/// <param name="key">The virtual key that was pressed.</param>
		public void KeyDown(VirtualKeys key) {
			switch (key) {
				case VirtualKeys.A1:
					/* AWS Reset button */
					CabControls.AwsResetButtonDepressed = true;
					break;
				case VirtualKeys.A2:
					/* DSD pedal */
					CabControls.DsdPedalReleased = true;
					Vig.Acknowledge();
					break;
				case VirtualKeys.B1:
					/* Wiper speed increase */
					CabControls.WiperControlState++;
					SoundManager.Play(SoundIndices.SwitchWiper, 1.0, 1.0, false);
					if (CabControls.WiperControlState > CabControls.WiperControlStates.Fast) {
						CabControls.WiperControlState = CabControls.WiperControlStates.Fast;
					}
					break;
				case VirtualKeys.B2:
					/* Wiper speed decrease */
					CabControls.WiperControlState--;
					SoundManager.Play(SoundIndices.SwitchWiper, 1.0, 1.0, false);
					if (CabControls.WiperControlState < CabControls.WiperControlStates.Off) {
						CabControls.WiperControlState = CabControls.WiperControlStates.Off;
					}
					break;
				case VirtualKeys.C1:
					/* TPWS TSS Temporary override */
					Tpws.TssTemporaryOverride();
					break;
				case VirtualKeys.C2:
					/* TPWS Temporary Isolation */
					CabControls.TpwsTemporaryIsolationDepressed = true;
					Tpws.Isolate();
					break;
				case VirtualKeys.D:
					if (Pan.Enabled && !Diesel.Enabled) {
						/* Pantograph up/reset button */
						CabControls.PantographUpResetButtonDepressed = true;
						Pan.Raise();
					}
					if (Diesel.Enabled && !Pan.Enabled) {
						/* Diesel starter button */
						CabControls.DieselEngineStartButtonDepressed = true;
						Diesel.Start();
					}
					break;
				case VirtualKeys.E:
					if (Pan.Enabled) {
						/* Pantograph down button */
						CabControls.PantographDownButtonDepressed = true;
						Pan.Lower();
						Fan.TurnOff();
					}
					if (Diesel.Enabled) {
						/* Diesel stop button */
						CabControls.DieselEngineStopButtonDepressed = true;
						Diesel.Stop();
					}
					break;
				case VirtualKeys.F:
					/* Headlight rotary switch */
					CabControls.TaillightControlState++;
					if (CabControls.TaillightControlState > CabControls.TaillightControlStates.On) {
						CabControls.TaillightControlState = CabControls.TaillightControlStates.Off;
					}
					SoundManager.Play(SoundIndices.Switch, 1.0, 1.0, false);
					break;
				case VirtualKeys.G:
					/* Taillight rotary switch */
					CabControls.HeadlightControlState++;
					if (CabControls.HeadlightControlState > CabControls.HeadlightControlStates.Night) {
						CabControls.HeadlightControlState = CabControls.HeadlightControlStates.Off;
					}
					SoundManager.Play(SoundIndices.Switch, 1.0, 1.0, false);
					break;
				case VirtualKeys.H:
					/* Driver to guard buzzer button */
					AiGuard.BuzzFromDriver();
					break;
				case VirtualKeys.I:
					break;
				case VirtualKeys.J:
					AiSupportFeature.AiDriverEnabled = true;
					break;
				case VirtualKeys.K:
					break;
				case VirtualKeys.L:
					break;
				case VirtualKeys.S:
					/* DRA switch */
					CabControls.DraButtonPushedIn = !CabControls.DraButtonPushedIn;
					Dra.SwitchState();
					break;
			}
		}
		
		/// <summary>Is called when a virtual key is released.</summary>
		/// <param name="key">The virtual key that was released.</param>
		public void KeyUp(VirtualKeys key) {
			switch (key) {
				case VirtualKeys.A1:
					/* AWS reset button */
					CabControls.AwsResetButtonDepressed = false;
					Aws.AcknowledgeWarning();
					break;
				case VirtualKeys.A2:
					/* DSD pedal */
					CabControls.DsdPedalReleased = false;
					break;
				case VirtualKeys.B1:
					/* Wiper speed increase */
					break;
				case VirtualKeys.B2:
					/* Wiper speed descrease */
					break;
				case VirtualKeys.C1:
					/* TPWS TSS Temporary override */
					break;
				case VirtualKeys.C2:
					/* TPWS Temporary Isolation */
					CabControls.TpwsTemporaryIsolationDepressed = false;
					break;
				case VirtualKeys.D:
					if (Pan.Enabled && !Diesel.Enabled) {
						/* Pantograph up/reset button */
						CabControls.PantographUpResetButtonDepressed = false;
					}
					if (Diesel.Enabled && !Pan.Enabled) {
						/* Diesel starter button */
						CabControls.DieselEngineStartButtonDepressed = false;
						Diesel.InterruptStartup();
					}
					break;
				case VirtualKeys.E:
					if (Pan.Enabled) {
						/* Pantograph down button */
						CabControls.PantographDownButtonDepressed = false;
					}
					if (Diesel.Enabled) {
						/* Diesel stop button */
						CabControls.DieselEngineStopButtonDepressed = false;
					}
					break;
				case VirtualKeys.F:
					/* Headlight rotary switch */
					break;
				case VirtualKeys.G:
					/* Taillight rotary switch */
					break;
				case VirtualKeys.H:
					/* Driver to guard buzzer button */
					break;
				case VirtualKeys.I:
					break;
				case VirtualKeys.J:
					break;
				case VirtualKeys.K:
					break;
				case VirtualKeys.L:
					break;
				case VirtualKeys.S:
					break;
			}
		}
		
		/// <summary>Is called when a horn is played or when the music horn is stopped.</summary>
		/// <param name="type">The type of horn.</param>
		public void HornBlow(HornTypes type) {
			if (type == HornTypes.Primary || type == HornTypes.Music) {
				CabControls.HornState = CabControls.HornStates.Forward;
			} else if (type == HornTypes.Secondary) {
				CabControls.HornState = CabControls.HornStates.Backward;
			}
			Vig.Reset();
		}
		
		/// <summary>Is called when the state of the doors changes.</summary>
		/// <param name="oldState">The old state of the doors.</param>
		/// <param name="newState">The new state of the doors.</param>
		public void DoorChange(DoorStates oldState, DoorStates newState) {
			CabControls.DoorState = newState;
			if (CabControls.DoorState != DoorStates.None) {
				DoorsOpenTimerActive = true;
			} else if (StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.Initialised) {
				AiGuard.IssueReadyToStart();
				InterlockManager.RequestBrakeReset();
				InterlockManager.RequestTractionPowerReset();
			}
		}
		
		/// <summary>Is called when the aspect in the current or in any of the upcoming sections changes, or when passing section boundaries.</summary>
		/// <param name="data">Signal information per section. In the array, index 0 is the current section, index 1 the upcoming section, and so on.</param>
		/// <remarks>The signal array is guaranteed to have at least one element. When accessing elements other than index 0, you must check the bounds of the array first.</remarks>
		public void SetSignal(SignalData[] signal) {
			if (signal.Length > 1) {
				LastSignalData = signal[1];
				AiSupportFeature.LastReportedSignalLocation = TrainLocation;
			}
		}
		
		/// <summary>Is called when the train passes a beacon.</summary>
		/// <param name="beacon">The beacon data.</param>
		public void SetBeacon(BeaconData beacon) {
			if (!JumpingToStation) {
				switch (beacon.Type) {
					case 20:
						if (!Diesel.Enabled && OverheadSupply.Enabled && Pan.Enabled) {
							/* APC magnets and neutral section */
							if (beacon.Optional > 0) {
								/* Handle legacy APC magnet behaviour with only one beacon */
								int secondMagnetDistance;
								int.TryParse(beacon.Optional.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out secondMagnetDistance);
								Apc.LegacyPowerCutOff(TrainLocation, secondMagnetDistance);
							} else if (beacon.Optional == -1) {
								/* Let the Offset Beacon Manager handle this, as we require an offset beacon receiver */
								OffsetBeaconReceiverManager.AddBeacon(TrainLocation, beacon.Type, beacon.Optional, Pan.PantographLocation);
							} else {
								/* Let the Offset Beacon Manager handle this, as we require an offset beacon receiver */
								OffsetBeaconReceiverManager.AddBeacon(TrainLocation, beacon.Type, beacon.Optional, Apc.ReceiverLocation);
							}
							if (Pan.BreakerState == SystemBreakerStates.Open) {
								/* Only set the following to false, after an APC magnet has been reached and the VCB has been commanded open.
								 * 
								 * This ensures that the AI support keeps the power handle in the off position until
								 * the absence of overhead power does the same job */
								AiSupportFeature.UpcomingNeutralSection = false;
							}
						}
						break;
					case 23:
						/* Station limit beacon - whether or not to ignore subsequent station limit beacons */
						if (beacon.Optional != 0) {
							AiGuard.AiGuardState = AiGuard.AiGuardStates.MonitoringStoppingLocation;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} Guard: [INFORMATION] - Monitoring the upcoming stopping location (beacon is at {3} metres)",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										TrainLocation.ToString()
									)
								);
							}
						} else {
							AiGuard.AiGuardState = AiGuard.AiGuardStates.None;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} Guard: [INFORMATION] - Not monitoring the stopping location (beacon is at {3} metres)",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										TrainLocation.ToString()
									)
								);
							}
						}
						break;
					case 24:
						/* Station limit beacon - starting location */
						if (AiGuard.AiGuardState == AiGuard.AiGuardStates.MonitoringStoppingLocation) {
							string optionalData = beacon.Optional.ToString();
							int stoppingDistance, permittedUnderrun, permittedOverrun, numberOfCars;
							if (optionalData.Length >= 7) {
								int.TryParse(optionalData.Substring(0, 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out stoppingDistance);
								int.TryParse(optionalData.Substring(3, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out permittedUnderrun);
								int.TryParse(optionalData.Substring(5, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out permittedOverrun);
								if (optionalData.Length == 9) {
									int.TryParse(optionalData.Substring(7, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out numberOfCars);
								} else {
									numberOfCars = 0;
								}
								AiGuard.UpdateStopParameters(stoppingDistance, permittedUnderrun, permittedOverrun, numberOfCars);
							} else if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} Guard: [ERROR] - Optional data not in the expected format (beacon is at {3} metres)",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										TrainLocation.ToString()
									)
								);
							}
							if (optionalData.Length == 1) {
								int val;
								int.TryParse(optionalData, NumberStyles.Integer, CultureInfo.InvariantCulture, out val);
								if (val == 0) {
									AiGuard.AiGuardState = AiGuard.AiGuardStates.None;
									if (Plugin.DebugMode) {
										Plugin.ReportLogEntry(
											string.Format(
												"{0} {1} {2} Guard: [INFORMATION] - Left the station limits (beacon is at {3} metres)",
												DateTime.Now.TimeOfDay.ToString(),
												this.GetType().Name.ToString(),
												MethodInfo.GetCurrentMethod().ToString(),
												TrainLocation.ToString()
											)
										);
									}
								}
							}
						} else {
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} Guard: [INFORMATION] - Left previous station area (beacon is at {3} metres)",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										TrainLocation.ToString()
									)
								);
							}
						}
						break;
					case 50:
						/* AI Support instruction beacon */
						if (AiSupportFeature.AiDriverEnabled) {
							int instructionValue;
							/* Check to see if the optional data value is in the 900 range or not, and parse accordingly */
							if (beacon.Optional.ToString().Length > 2) {
								int.TryParse(beacon.Optional.ToString().Substring(0, 3), NumberStyles.Integer, CultureInfo.InvariantCulture, out instructionValue);
							} else {
								int.TryParse(beacon.Optional.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out instructionValue);
							}
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} AI Beacon Instruction: [INFORMATION] - AI Instruction [{3}] encountered (beacon is at {4} metres)",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										instructionValue.ToString(),
										TrainLocation.ToString()
									)
								);
							}
							if (instructionValue < 900) {
								switch (instructionValue) {
									case 0:
										AiSupportFeature.SoundHornState = AiSoundHornStates.Forward;
										break;
									case 1:
										AiSupportFeature.SoundHornState = AiSoundHornStates.Backward;
										break;
										
									case 40:
										AiSupportFeature.NextStopIsTheLast = true;
										break;
									case 41:
										AiSupportFeature.LowerPantographAtNextStop = true;
										break;
								}
							} else {
								switch (instructionValue) {
									case 920:
										if (!Diesel.Enabled && OverheadSupply.Enabled) {
											int direction, distanceToNeutralSection;
											if (beacon.Optional.ToString().Length == 9) {
												int.TryParse(beacon.Optional.ToString().Substring(3, 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out direction);
												int.TryParse(beacon.Optional.ToString().Substring(4, 5), NumberStyles.Integer, CultureInfo.InvariantCulture, out distanceToNeutralSection);
												if (direction == 1 && TrainSpeed > 0) {
													AiSupportFeature.LocationOfNextNeutralSection = (int)TrainLocation + distanceToNeutralSection;
													AiSupportFeature.UpcomingNeutralSection = true;
												} else {
													AiSupportFeature.LocationOfNextNeutralSection = 0;
													AiSupportFeature.UpcomingNeutralSection = false;
												}
											} else if (Plugin.DebugMode) {
												Plugin.ReportLogEntry(
													string.Format(
														"{0} {1} {2} [ERROR] - Optional data for AI Instruction [{3}] is not in the expected format (beacon is at {4} metres)",
														DateTime.Now.TimeOfDay.ToString(),
														this.GetType().Name.ToString(),
														MethodInfo.GetCurrentMethod().ToString(),
														instructionValue.ToString(),
														TrainLocation.ToString()
													)
												);
											}
										}
										break;
								}
							}
						}
						break;
					case 44000:
						/* Automatic Warning System magnet for signals */
						if (beacon.Optional == 180) {
							/* This is the south pole of an AWS permanent magnet, so prime the AWS - new prototypical behaviour */
							Aws.Prime();
						} else if (beacon.Optional == 270) {
							/* This is an AWS suppression electromagnet - new prototypical behaviour */
							Aws.Suppress(TrainLocation);
						} else if (beacon.Optional == 360) {
							/* The following if statement must remain nested in the containing if statement */
							if (beacon.Signal.Aspect > 3) {
								/* This is the north pole of the AWS electromagnet - it is energised, so issue a clear indication */
								Aws.IssueClear();
							}
						} else if (beacon.Signal.Aspect <= 3) {
							/* Aspect is restrictive, so issue a warning - this is the legacy fallback behaviour */
							Aws.Prime();
						} else if (beacon.Signal.Aspect > 3) {
							/* Aspect is clear, so issue a clear inducation - this is the legacy fallback behaviour */
							Aws.IssueLegacyClear();
						}
						break;
					case 44001:
						/* Permanently installed Automatic Warning System magnet which triggers a warning whenever passed over.
						 * 
						 * Issue a warning regardless - this is the legacy fallback behaviour ONLY */
						Aws.Prime();
						break;
					case 44002:
						/* Train Protection and Warning System Overspeed Sensor induction loop - associated with signal */
						if (beacon.Signal.Aspect == 0) {
							Tpws.ArmOss(beacon.Optional);
						}
						break;
					case 44003:
						/* Train Protection and Warning System Train Stop Sensor induction loop */
						if (beacon.Signal.Aspect == 0) {
							Tpws.ArmTss(beacon.Optional, TrainLocation);
						} else {
							if (Tpws.SafetyState == TrainProtectionWarningSystem.SafetyStates.None) {
								Tpws.Reset();
							}
						}
						break;
					case 44004:
						/* Train Protection and Warning System Overspeed Sensor induction loop - permanent speed restriction */
						Tpws.ArmOss(beacon.Optional);
						break;
				}
			}
		}
		
		/// <summary>Is called when the plugin should perform the AI.</summary>
		/// <param name="data">The AI data.</param>
		public void PerformAI(AIData data) {
			/* Inform the plugin that the AI driver is activated */
			AiSupportFeature.AiDriverEnabled = true;
			AiSupportFeature.AiDriverEnabledTimer = 0;
			CabControls.InitialiseAiArms();
			
			/* Reset AI driver hand animations to default states */
			CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.PowerHandle;
			CabControls.AiDriverRightHandState = CabControls.AiDriverRightHandStates.None;
			
			if (StartupSelfTestManager.SequenceState != StartupSelfTestManager.SequenceStates.Initialised) {
				/* 
				 * Handle the startup and self-test procedure
				 * ==========================================
				 */
				while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
					data.Handles.BrakeNotch += 1;
				}
				data.Handles.PowerNotch -= 1;
				data.Response = AIResponse.Short;

				if (StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.Pending) {
					/*
					 * Master switch
					 * -------------
					 */
					AiSupportFeature.TimerActive = true;
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.ReverserHandle;
					if (AiSupportFeature.Timer > 2000) {
						/* Move the reverser to forward */
						data.Handles.Reverser = (int)CabControls.ReverserStates.Forward;
						AiSupportFeature.TimerActive = false;
						data.Response = AIResponse.Short;
					}
				} else if (StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.WaitingToStart ||
				           StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.Initialising) {
					/* Move the reverser back to neutral */
					data.Handles.Reverser = (int)CabControls.ReverserStates.Neutral;
					data.Response = AIResponse.Short;
					
				} else if (StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.AwaitingDriverInteraction) {
					/*
					 * Startup and self-test procedure (AWS cancellation)
					 * --------------------------------------------------
					 */
					CabControls.AiDriverRightHandState = CabControls.AiDriverRightHandStates.AwsResetButton;
					AiSupportFeature.TimerActive = true;
					/* Wait for a number of milliseconds before starting to cancel the warning */
					if (AiSupportFeature.Timer > 1000 && !CabControls.AwsResetButtonDepressed) {
						/* Press down the AWS reset button for visual feedback purposes */
						KeyDown(VirtualKeys.A1);
						data.Response = AIResponse.Short;
					} else if (AiSupportFeature.Timer > 1300) {
						/* Release the button and stop the timer after the specified number of milliseconds */
						KeyUp(VirtualKeys.A1);
						AiSupportFeature.TimerActive = false;
						data.Response = AIResponse.Long;
					}
				}

			} else if (!Diesel.Enabled && OverheadSupply.Enabled && Pan.State == Pantograph.PantographStates.Lowered && !AiSupportFeature.LowerPantographAtNextStop) {
				/*
				 * Handle a lowered pantograph
				 * ===========================
				 */
				data.Handles.Reverser = (int)CabControls.ReverserStates.Neutral;
				while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
					data.Handles.BrakeNotch += 1;
				}
				data.Handles.PowerNotch -= 1;
				
				if (TrainSpeed == 0) {
					data.Handles.Reverser = (int)CabControls.ReverserStates.Neutral;
					/* Press and release the pantograph up/reset button */
					AiSupportFeature.TimerActive = true;
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.PantographUpButton;
					if (AiSupportFeature.Timer > 600) {
						data.Handles.Reverser = (int)CabControls.ReverserStates.Neutral;
						KeyDown(VirtualKeys.D);
						KeyUp(VirtualKeys.D);
						AiSupportFeature.TimerActive = false;
					}
				}
				data.Response = AIResponse.Long;

			} else if (Diesel.Enabled && !OverheadSupply.Enabled && Diesel.DieselEngineState == DieselEngine.DieselEngineStates.Off) {
				/*
				 * Handle a shut down engine
				 * =========================
				 */
				data.Handles.Reverser = (int)CabControls.ReverserStates.Neutral;
				while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
					data.Handles.BrakeNotch += 1;
				}
				data.Handles.PowerNotch -= 1;
				data.Response = AIResponse.Short;
				
				if (TrainSpeed == 0) {
					/* Press and release the engine starter button */
					AiSupportFeature.TimerActive = true;
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.PantographUpButton;
					if (AiSupportFeature.Timer > 600) {
						KeyDown(VirtualKeys.D);
						KeyUp(VirtualKeys.D);
						AiSupportFeature.TimerActive = false;
						data.Response = AIResponse.Long;
					}
				}

			} else if ((Diesel.Enabled && Diesel.DieselEngineState != DieselEngine.DieselEngineStates.Running) ||
			           (OverheadSupply.Enabled && Pan.State != Pantograph.PantographStates.Raised)) {
				/*
				 * Keep brakes on and power off if the pantograph is not raised or the diesel engine is not running
				 * ================================================================================================
				 */
				data.Handles.Reverser = (int)CabControls.ReverserStates.Neutral;
				while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
					data.Handles.BrakeNotch += 1;
				}
				data.Handles.PowerNotch -= 1;
				data.Response = AIResponse.Long;

			} else if (Diesel.DieselEngineState == DieselEngine.DieselEngineStates.Running || Pan.State == Pantograph.PantographStates.Raised) {
				/*
				 * Handle anything which requires a traction power supply
				 * ======================================================
				 */
				if ((AiSupportFeature.OperatingReverser || CabControls.ReverserPosition == CabControls.ReverserStates.Neutral) && !AiSupportFeature.NextStopIsTheLast && TrainSpeed == 0) {
					
					/* Set the revereser if necessary - start by ensuring power/brake handle is set
					 * ----------------------------------------------------------------------------
					 */
					while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
						data.Handles.BrakeNotch += 1;
					}
					data.Handles.PowerNotch -= 1;
					data.Response = AIResponse.Short;
					/* Place the AI hand on the reverser handle */
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.ReverserHandle;
					AiSupportFeature.TimerActive = true;
					AiSupportFeature.OperatingReverser = true;
					if (AiSupportFeature.Timer > 1000) {
						CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.ReverserHandle;
						/* Move the reverser handle forward after the AI hand has had time to reach it */
						data.Handles.Reverser = (int)CabControls.ReverserStates.Forward;
						if (AiSupportFeature.Timer > 2000) {
							/* Move the AI hand back to the power handle */
							CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.PowerHandle;
							data.Response = AIResponse.Short;
							if (AiSupportFeature.Timer > 3000) {
								/* Exit... */
								AiSupportFeature.TimerActive = false;
								AiSupportFeature.OperatingReverser = false;
							}
						}
					}
				} else if ((SecondsSinceMidnght < 21600 | SecondsSinceMidnght > 64800) && CabControls.ReverserPosition != CabControls.ReverserStates.Backward &&
				           CabControls.HeadlightControlState != CabControls.HeadlightControlStates.Night) {
					
					/* Handle head and tail lights
					 * ---------------------------
					 */
					/* Time of day is earlier than 06:00 or later than 18:00 */
					if (TrainSpeed == 0) {
						while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
							data.Handles.BrakeNotch += 1;
						}
						data.Handles.PowerNotch -= 1;
						data.Response = AIResponse.Short;
					}
					AiSupportFeature.TimerActive = true;
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.HeadlightSwitch;
					/* Wait for a number of milliseconds before starting to cancel the warning */
					if (AiSupportFeature.Timer > 600) {
						/* Release the button and stop the timer after the specified number of milliseconds */
						while (CabControls.HeadlightControlState != CabControls.HeadlightControlStates.Night) {
							KeyDown(VirtualKeys.G);
							AiSupportFeature.TimerActive = false;
							data.Response = AIResponse.Long;
						}
					}
				} else if ((SecondsSinceMidnght > 21600 && SecondsSinceMidnght < 64800) && CabControls.ReverserPosition != CabControls.ReverserStates.Backward &&
				           CabControls.HeadlightControlState != CabControls.HeadlightControlStates.Day) {
					/* Time of day is earlier than 06:00 or later than 18:00 */
					if (TrainSpeed == 0) {
						while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
							data.Handles.BrakeNotch += 1;
						}
						data.Handles.PowerNotch -= 1;
						data.Response = AIResponse.Short;
					}
					AiSupportFeature.TimerActive = true;
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.HeadlightSwitch;
					/* Wait for a number of milliseconds before starting to cancel the warning */
					if (AiSupportFeature.Timer > 600) {
						/* Release the button and stop the timer after the specified number of milliseconds */
						while (CabControls.HeadlightControlState != CabControls.HeadlightControlStates.Day) {
							KeyDown(VirtualKeys.G);
							AiSupportFeature.TimerActive = false;
							data.Response = AIResponse.Long;
						}
					}
				} else if ((SecondsSinceMidnght < 21600 | SecondsSinceMidnght > 64800) && CabControls.ReverserPosition != CabControls.ReverserStates.Backward &&
				           CabControls.TaillightControlState != CabControls.TaillightControlStates.Off) {
					/* Time of day is earlier than 06:00 or later than 18:00 */
					if (TrainSpeed == 0) {
						while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
							data.Handles.BrakeNotch += 1;
						}
						data.Handles.PowerNotch -= 1;
						data.Response = AIResponse.Short;
					}
					AiSupportFeature.TimerActive = true;
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.TaillightSwitch;
					/* Wait for a number of milliseconds before starting to cancel the warning */
					if (AiSupportFeature.Timer > 600) {
						/* Release the button and stop the timer after the specified number of milliseconds */
						while (CabControls.TaillightControlState != CabControls.TaillightControlStates.Off) {
							KeyDown(VirtualKeys.F);
							AiSupportFeature.TimerActive = false;
							data.Response = AIResponse.Long;
						}
					}
				} else if ((SecondsSinceMidnght > 21600 && SecondsSinceMidnght < 64800) && CabControls.ReverserPosition != CabControls.ReverserStates.Backward &&
				           CabControls.TaillightControlState != CabControls.TaillightControlStates.Off) {
					/* Time of day is earlier than 06:00 or later than 18:00 */
					if (TrainSpeed == 0) {
						while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
							data.Handles.BrakeNotch += 1;
						}
						data.Handles.PowerNotch -= 1;
						data.Response = AIResponse.Short;
					}
					AiSupportFeature.TimerActive = true;
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.TaillightSwitch;
					/* Wait for a number of milliseconds before starting to cancel the warning */
					if (AiSupportFeature.Timer > 600) {
						/* Release the button and stop the timer after the specified number of milliseconds */
						while (CabControls.TaillightControlState != CabControls.TaillightControlStates.Off) {
							KeyDown(VirtualKeys.F);
							AiSupportFeature.TimerActive = false;
							data.Response = AIResponse.Long;
						}
					}
				} else if (Aws.SafetyState == AutomaticWarningSystem.SafetyStates.CancelTimerActive) {
					/*
					 * Handle the Automatic Warning System
					 * -----------------------------------
					 */
					CabControls.AiDriverRightHandState = CabControls.AiDriverRightHandStates.AwsResetButton;
					AiSupportFeature.TimerActive = true;
					/* Wait for a number of milliseconds before starting to cancel the warning */
					if (AiSupportFeature.Timer > 500 && !CabControls.AwsResetButtonDepressed) {
						/* Press down the AWS reset button for visual feedback purposes */
						KeyDown(VirtualKeys.A1);
						data.Response = AIResponse.Short;
					} else if (AiSupportFeature.Timer > 800) {
						/* Release the button and stop the timer after the specified number of milliseconds */
						KeyUp(VirtualKeys.A1);
						AiSupportFeature.TimerActive = false;
						data.Response = AIResponse.Long;
					}

				} else if (Vig.SafetyState == VigilanceDevice.SafetyStates.CancelTimerActive || (AiSupportFeature.TimerActive && CabControls.DsdPedalReleased)) {
					/*
					 * Handle the Vigilance Device
					 * ---------------------------
					 */
					if (TrainSpeed == 0) {
						while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
							data.Handles.BrakeNotch += 1;
						}
						data.Handles.PowerNotch -= 1;
						data.Response = AIResponse.Short;
					}
					AiSupportFeature.TimerActive = true;
					/* Wait for a number of milliseconds before starting to cancel the warning */
					if (AiSupportFeature.Timer > 800 && !CabControls.DsdPedalReleased) {
						/* Release the DSD pedal for visual feedback purposes */
						KeyDown(VirtualKeys.A2);
						data.Response = AIResponse.Short;
					} else if (AiSupportFeature.Timer > 1000) {
						/* Press down the DSD pedal and stop the timer after the specified number of milliseconds */
						KeyUp(VirtualKeys.A2);
						AiSupportFeature.TimerActive = false;
						data.Response = AIResponse.Long;
					}

				} else if (AiGuard.AiGuardState == AiGuard.AiGuardStates.ReadyToStart || AiGuard.AiGuardState == AiGuard.AiGuardStates.DrawForward
				           || AiGuard.AiGuardState == AiGuard.AiGuardStates.SetBack) {
					/*
					 * Wait until the guard's buzzer signal has finished before proceeding
					 * --------------------------------------------------------------------
					 */
					while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
						data.Handles.BrakeNotch += 1;
					}
					data.Handles.PowerNotch -= 1;
					data.Response = AIResponse.Short;
				} else if (AiGuard.AiGuardState == AiGuard.AiGuardStates.AwaitingAcknowledgement) {
					/*
					 * Issue two buzzer signals to the guard before proceeding
					 * -------------------------------------------------------
					 */
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.GuardBuzzerButton;
					while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
						data.Handles.BrakeNotch += 1;
					}
					data.Handles.PowerNotch -= 1;
					data.Response = AIResponse.Short;
					AiSupportFeature.TimerActive = true;
					/* Wait for a moment before buzzing the guard */
					if (AiSupportFeature.Timer > 1000) {
						/* Push the buzzer button, provided the buzzer isn't already sounding */
						if (!SoundManager.IsPlaying(SoundIndices.Buzzer)) {
							KeyDown(VirtualKeys.H);
							KeyUp(VirtualKeys.H);
						}
					}
				} else if (AiGuard.AiGuardState == AiGuard.AiGuardStates.AcknowledgementReceived) {
					/*
					 * Keep the brakes applied for a brief time after ackowledging the guard's
					 * buzzer signal, to allow time for the left hand to return to the power handle
					 * ----------------------------------------------------------------------------
					 */
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.PowerHandle;
					while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
						data.Handles.BrakeNotch += 1;
					}
					data.Handles.PowerNotch -= 1;
					data.Response = AIResponse.Short;
					/* Disable the AI timer that was started while the AI guard was awaiting acknowledgement */
					AiSupportFeature.TimerActive = false;
				} else if (Dra.SafetyState == DriverReminderAppliance.SafetyStates.Activated) {
					/*
					 * Handle the Driver Reminder Appliance if it is set
					 * -------------------------------------------------
					 */
					while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
						data.Handles.BrakeNotch += 1;
					}
					data.Handles.PowerNotch -= 1;
					data.Response = AIResponse.Short;
					
					/* Only consider resetting the DRA if the train is stationary, and the doors are closed */
					if (LastSignalData != null && TrainSpeed == 0 && CabControls.DoorState == DoorStates.None) {
						/* Always reset the DRA if the train is greater than 100 metres in the rear of the upcoming signal,
						 * and conditionally reset the DRA if the train is beyond 100 metres in the rear of the upcoming signal, and the aspect is not red */
						if (TrainLocation < LastSignalData.Distance + AiSupportFeature.LastReportedSignalLocation - AiSupportFeature.MaximumDraActivationDistance ||
						    (TrainLocation > LastSignalData.Distance + AiSupportFeature.LastReportedSignalLocation - AiSupportFeature.MaximumDraActivationDistance && LastSignalData.Aspect != 0)) {
							if (AiGuard.AiGuardState == AiGuard.AiGuardStates.AwaitingAcknowledgement ||
							    AiGuard.AiGuardState == AiGuard.AiGuardStates.AcknowledgementReceived ||
							    AiGuard.AiGuardState == AiGuard.AiGuardStates.None) {
								AiSupportFeature.TimerActive = true;
								CabControls.AiDriverRightHandState = CabControls.AiDriverRightHandStates.DraSwitch;
								if (AiSupportFeature.Timer > 2000) {
									/* Push the DRA switch */
									KeyDown(VirtualKeys.S);
									AiSupportFeature.TimerActive = false;
									data.Response = AIResponse.Long;
								}
							}
						}
					}

				} else if (Dra.SafetyState == DriverReminderAppliance.SafetyStates.Deactivated &&
				           LastSignalData != null && LastSignalData.Aspect == 0 &&
				           TrainLocation > LastSignalData.Distance + AiSupportFeature.LastReportedSignalLocation - AiSupportFeature.MaximumDraActivationDistance && TrainSpeed == 0) {
					/*
					 * Handle the Driver Reminder Appliance after awareness of a red aspect and stopping
					 * ---------------------------------------------------------------------------------
					 * 
					 * Note: Only set the DRA if greater than 100 metres in the rear of the signal
					 */
					AiSupportFeature.TimerActive = true;
					CabControls.AiDriverRightHandState = CabControls.AiDriverRightHandStates.DraSwitch;
					if (AiSupportFeature.Timer > 2000) {
						/* Push the DRA switch */
						KeyDown(VirtualKeys.S);
						AiSupportFeature.TimerActive = false;
						data.Response = AIResponse.Long;
					}

				} else if (Tpws.SafetyState == TrainProtectionWarningSystem.SafetyStates.TssBrakeDemand) {
					/*
					 * Handle the Train Protection and Warning System
					 * ----------------------------------------------
					 */
					CabControls.AiDriverRightHandState = CabControls.AiDriverRightHandStates.AwsResetButton;
					AiSupportFeature.TimerActive = true;
					/* Wait for a number of milliseconds before starting to cancel the warning */
					if (AiSupportFeature.Timer > 800 && !CabControls.AwsResetButtonDepressed) {
						/* Press down the AWS reset button for visual feedback purposes */
						KeyDown(VirtualKeys.A1);
						data.Response = AIResponse.Short;
					} else if (AiSupportFeature.Timer > 1000) {
						/* Release the button and stop the timer after the specified number of milliseconds */
						KeyUp(VirtualKeys.A1);
						AiSupportFeature.TimerActive = false;
						data.Response = AIResponse.Long;
					}
				} else if (Tpws.SafetyState == TrainProtectionWarningSystem.SafetyStates.BrakeDemandAcknowledged) {
					/* TPWS brake demand is in effect, so apply the brakes */
					while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
						data.Handles.BrakeNotch += 1;
					}
					data.Handles.PowerNotch -= 1;
					data.Response = AIResponse.Short;
				} else if (Tpws.SafetyState == TrainProtectionWarningSystem.SafetyStates.BrakesAppliedCountingDown) {
					/* TPWS brake demand has brought etrain to a halt; keep brakes applied until automatic timeout reset */
					while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
						data.Handles.BrakeNotch += 1;
					}
					data.Handles.PowerNotch -= 1;
					data.Response = AIResponse.Short;
				} else if (InterlockManager.BrakesHeldOn && TrainSpeed != 0) {
					/*
					 * Handle returning the power handle to zero if the reverser was set to neutral when in motion
					 * -------------------------------------------------------------------------------------------
					 */
					data.Handles.Reverser = (int)CabControls.ReverserStates.Neutral;
					while (data.Handles.PowerNotch > 0) {
						data.Handles.PowerNotch -= 1;
					}
					data.Response = AIResponse.Short;
				} else if (CabControls.DoorState != DoorStates.None && TrainSpeed != 0) {
					/*
					 * Handle a door open while train moving situation
					 * -----------------------------------------------
					 */
					while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
						data.Handles.BrakeNotch += 1;
					}
					data.Handles.PowerNotch -= 1;
					data.Response = AIResponse.Short;
				} else if (InterlockManager.TractionPowerHeldOff && TrainSpeed != 0) {
					/*
					 * Handle returning the power handle to zero if the traction power interlock is active while in motion
					 * ---------------------------------------------------------------------------------------------------
					 */
					while (data.Handles.PowerNotch > 0) {
						data.Handles.PowerNotch -= 1;
					}
					data.Response = AIResponse.Short;
				} else if (AiSupportFeature.SoundHornState == AiSoundHornStates.Forward || AiSupportFeature.SoundHornState == AiSoundHornStates.Wait) {
					/*
					 * Sound the horn; push lever forward first, then backwards
					 * --------------------------------------------------------
					 */
					CabControls.AiDriverRightHandState = CabControls.AiDriverRightHandStates.HornLever;
					AiSupportFeature.TimerActive = true;
					if (AiSupportFeature.Timer > 600 && AiSupportFeature.SoundHornState != AiSoundHornStates.Wait) {
						CabControls.HornState = CabControls.HornStates.AutoForwardFirst;
						AiSupportFeature.SoundHornState = AiSoundHornStates.Wait;
						data.Response = AIResponse.Long;
					} else if (AiSupportFeature.Timer > CabControls.HornCentreTimeout * 2) {
						AiSupportFeature.TimerActive = false;
						AiSupportFeature.SoundHornState = AiSoundHornStates.None;
					}
				} else if (AiSupportFeature.SoundHornState == AiSoundHornStates.Backward || AiSupportFeature.SoundHornState == AiSoundHornStates.Wait) {
					/*
					 * Sound the horn; push lever forward first, then backwards
					 * --------------------------------------------------------
					 */
					CabControls.AiDriverRightHandState = CabControls.AiDriverRightHandStates.HornLever;
					AiSupportFeature.TimerActive = true;
					if (AiSupportFeature.Timer > 600 && AiSupportFeature.SoundHornState != AiSoundHornStates.Wait) {
						CabControls.HornState = CabControls.HornStates.AutoBackwardFirst;
						AiSupportFeature.SoundHornState = AiSoundHornStates.Wait;
						data.Response = AIResponse.Long;
					} else if (AiSupportFeature.Timer > CabControls.HornCentreTimeout * 2) {
						AiSupportFeature.TimerActive = false;
						AiSupportFeature.SoundHornState = AiSoundHornStates.None;
					}
				} else if (AiSupportFeature.LowerPantographAtNextStop && TrainSpeed == 0 && CabControls.DoorState != DoorStates.None &&
				           (Pan.State == Pantograph.PantographStates.Raised || Diesel.DieselEngineState == DieselEngine.DieselEngineStates.Running)) {
					/*
					 * Lower the pantograph or stop the diesel engine, if instructed to do so by a beacon
					 * ==================================================================================
					 */
					while (data.Handles.BrakeNotch <= TrainSpecifications.BrakeNotches) {
						data.Handles.BrakeNotch += 1;
					}
					data.Handles.PowerNotch -= 1;
					
					/* Press and release the pantograph down button */
					AiSupportFeature.TimerActive = true;
					CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.PantographDownButton;
					if (AiSupportFeature.Timer > 600) {
						KeyDown(VirtualKeys.E);
						KeyUp(VirtualKeys.E);
						AiSupportFeature.TimerActive = false;
					}
					data.Response = AIResponse.Long;
				} else {
					AiSupportFeature.TimerActive = false;
				}
				
				/* Handle the neutral section power handle to off behaviour, depending upon tap changer presence */
				if (AiSupportFeature.UpcomingNeutralSection) {
					if (Tap.Enabled) {
						if (AiSupportFeature.CalculatePowerHandleOffLocation(40)) {
							data.Handles.PowerNotch -= 1;
							data.Response = AIResponse.Short;
						}
					} else {
						if (AiSupportFeature.CalculatePowerHandleOffLocation(5)) {
							data.Handles.PowerNotch -= 1;
							data.Response = AIResponse.Short;
						}
					}
				}
			}
		}
	}
}