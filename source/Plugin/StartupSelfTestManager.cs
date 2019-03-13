using System;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Manages the startup and self-test sequence.</summary>
	internal static partial class StartupSelfTestManager {
		
		// TODO: It is not yet possible for the startup/self-test procedure to fail rather than succeed.
		
		// members
		/// <summary>The startup self-test sequence duration in milliseconds.</summary>
		private static int SequenceDuration = 1700;
		/// <summary>The state of the train systems with regard to the startup self-test procedure.</summary>
		private static SequenceStates MySequenceState = SequenceStates.Pending;
		/// <summary>The timer used during the startup self-test sequence.</summary>
		private static int MySequenceTimer = SequenceDuration;
		
		// properties
		
		/// <summary>Gets the state of the train systems with regard to the startup self-test procedure.</summary>
		internal static SequenceStates SequenceState {
			get { return MySequenceState; }
		}
		
		/// <summary>Gets the current self-test sequence countdown timer value.</summary>
		internal static int SequenceTimer {
			get { return MySequenceTimer; }
		}
		
		// event handling methods
		
		/// <summary>This method is called when an Automatic Warning System acknowledgement event occurs.</summary>
		internal static void HandleAwsAcknowledgement(Object sender, EventArgs e) {
			if (MySequenceState == SequenceStates.AwaitingDriverInteraction) {
				MySequenceState = SequenceStates.Finalising;
			}
		}
		
		// methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal static void Reinitialise(InitializationModes mode) {
			if (mode == InitializationModes.OnService) {
				MySequenceTimer = SequenceDuration;
				MySequenceState = SequenceStates.Initialised;
			} else if (mode == InitializationModes.OnEmergency) {
				MySequenceTimer = SequenceDuration;
				MySequenceState = SequenceStates.Pending;
			} else if (mode == InitializationModes.OffEmergency) {
				MySequenceTimer = SequenceDuration;
				MySequenceState = SequenceStates.Pending;
			}
		}
		
		/// <summary>This should be called during each Elapse() call.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="handles">The handles of the cab.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal static void Update(double elapsedTime, Handles handles, int[] panel, System.Text.StringBuilder debugBuilder) {
			if (StartupSelfTestManager.MySequenceState == StartupSelfTestManager.SequenceStates.Pending) {
				CabControls.MasterSwitchOn = false;
				/* Check the reverser state to see if the master switch has been set to on */
				if (CabControls.ReverserPosition == CabControls.ReverserStates.Forward || CabControls.ReverserPosition == CabControls.ReverserStates.Backward) {
					StartupSelfTestManager.MySequenceState = StartupSelfTestManager.SequenceStates.WaitingToStart;
				}
			} else if (StartupSelfTestManager.MySequenceState == StartupSelfTestManager.SequenceStates.WaitingToStart) {
				if (CabControls.ReverserPosition == CabControls.ReverserStates.Neutral) {
					/* Turn the master switch on, and begin the startup and self-test procedure */
					CabControls.MasterSwitchOn = true;
					MySequenceState = SequenceStates.Initialising;
					/* Start the in-cab blower */
					if (PowerSupplyManager.SelectedPowerSupply != Plugin.MainBattery) {
						Plugin.Fan.Reset();
					}
					/* Place the Automatic Warning System, and Train Protection and Warning System, into self-test mode */
					Plugin.Aws.SelfTest();
					Plugin.Tpws.SelfTest();
				}
			} else if (MySequenceState != SequenceStates.Initialised) {
				/* Make sure that the master switch is on after reinitialisation */
				CabControls.MasterSwitchOn = true;
				/* Hold the brakes on until the AWS button is depressed */
				if (MySequenceState == SequenceStates.AwaitingDriverInteraction) {
					handles.BrakeNotch = Plugin.TrainSpecifications.BrakeNotches;
				} else if (MySequenceState == SequenceStates.Finalising) {
					SoundManager.Play(SoundIndices.AwsBingling, 1.0, 1.0, false);
					MySequenceState = SequenceStates.Initialised;
				}
				/* Lastly, decrement the timer */
				if (MySequenceState == SequenceStates.Initialising) {
					MySequenceTimer = MySequenceTimer - (int)elapsedTime;
					if (MySequenceTimer < 0) {
						MySequenceTimer = 0;
						MySequenceState = SequenceStates.AwaitingDriverInteraction;
					}
				}
			}
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("");
			}
		}
	}
}
