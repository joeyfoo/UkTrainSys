using System;
using OpenBveApi.Runtime;

namespace Plugin {
	
	// -- train controls --
	
	/// <summary>Represents the physical in-cab controls.</summary>
	internal static partial class CabControls {
		
		/// <summary>The current power handle position.</summary>
		internal static int PowerPosition;
		/// <summary>The current brake notch.</summary>
		internal static int BrakePosition;
		
		/// <summary>The current reverser position.</summary>
		internal static ReverserStates ReverserPosition;
		
		/// <summary>The current state of the horn.</summary>
		internal static HornStates HornState;
		/// <summary>The horn lever return-to-centre position timeout period.</summary>
		internal static int HornCentreTimeout = 1000;
		/// <summary>The timer which keeps track of when the horn should return to its centre position in milliseconds.</summary>
		private static int HornCentreTimer;
		/// <summary>The timer which keeps track of the automatic two-tone horn duration in milliseconds.</summary>
		private static int HornAutoTimer;
		/// <summary>The last position the horn lever was in.</summary>
		private static int HornLastPosition;
		
		/// <summary>The current state of the headlight switch.</summary>
		internal static HeadlightControlStates HeadlightControlState;
		
		/// <summary>The current state of the taillight switch.</summary>
		internal static TaillightControlStates TaillightControlState;
		
		/// <summary>The current state of the wiper rotary switch.</summary>
		internal static WiperControlStates WiperControlState;
		
		/// <summary>The state of the train's doors.</summary>
		internal static DoorStates DoorState;
		
		/// <summary>Whether or not the DSD pedal has been released (acknowledges the Vigilance Device).</summary>
		internal static bool DsdPedalReleased;
		
		/// <summary>Whether the AWS reset button is depressed.</summary>
		internal static bool AwsResetButtonDepressed;
		
		/// <summary>Whether the DRA button has been pushed in (system deactivated).</summary>
		internal static bool DraButtonPushedIn;
		
		/// <summary>Whether the Pantograph Up/Reset button is depressed.</summary>
		internal static bool PantographUpResetButtonDepressed;
		
		/// <summary>Whether the Pantograph Down button is depressed.</summary>
		internal static bool PantographDownButtonDepressed;
		
		/// <summary>Whether the TPWS Temporary Isolation button is depressed.</summary>
		internal static bool TpwsTemporaryIsolationDepressed;
		
		/// <summary>Whether the diesel engine starter button is depressed.</summary>
		internal static bool DieselEngineStartButtonDepressed;
		
		/// <summary>Whether the diesel engine stop button is depressed.</summary>
		internal static bool DieselEngineStopButtonDepressed;
		
		/// <summary>Whether or not the Master Switch is on.</summary>
		internal static bool MasterSwitchOn;
		
		/// <summary>Whether or not the in-cab lighting is on.</summary>
		internal static bool CabLightsOn;
		
		/// <summary>The power handle of a BR 8x series AC electric locomotive.</summary>
		internal static AcLocoPowerHandleStates AcLocoPowerHandlePosition;
		
		/// <summary>The state of the AI driver's left arm.</summary>
		internal static AiDriverLeftHandStates AiDriverLeftHandState;
		
		/// <summary>The state of the AI driver's right arm.</summary>
		internal static AiDriverRightHandStates AiDriverRightHandState;
		
		/// <summary>Whether or not the AI arm state initialisation process is completed.</summary>
		private static AiArmInitialisationStates MyAiArmInitialisationState = AiArmInitialisationStates.Pending;
		
		/// <summary>A timer used during the AI arm state initialisation process.</summary>
		private static int MyAiArmsInitialisationTimer;
		
		/// <summary>Updates panel elements which represent in-cab controls.</summary>
		/// <param name="panel"></param>
		internal static void Update(double elapsedTime, int[] panel, System.Text.StringBuilder debugBuilder) {
			
			/* Increment the AI initialisation timer if required, or reset it and set the state to initialised */
			if (MyAiArmInitialisationState == AiArmInitialisationStates.Initialising) {
				MyAiArmsInitialisationTimer = MyAiArmsInitialisationTimer + (int)elapsedTime;
				if (MyAiArmsInitialisationTimer > 1000) {
					MyAiArmsInitialisationTimer = 0;
					MyAiArmInitialisationState = AiArmInitialisationStates.Initialised;
				}
			}
			
			/* AWS reset button */
			if (AwsResetButtonDepressed) {
				panel[PanelIndices.AwsResetButton] = 1;
			}
			
			if (Plugin.AiSupportFeature.AiDriverEnabled) {
				/* AI driver's hands - global panel variable specifying whether they are visible or not */
				panel[PanelIndices.AiDriverHandsVisible] = 1;
				if (MyAiArmInitialisationState == AiArmInitialisationStates.Initialised) {
					/* Global panel variable specifying whether the AI driver's animation states have been initialised or not */
					panel[PanelIndices.AiDriverHandsInitialised] = 1;
				}
				
				/* Left arm and hand
				 * -----------------
				 */
				if (AiDriverLeftHandState == AiDriverLeftHandStates.PowerHandle) {
					/* AI driver's hand - power handle */
					panel[PanelIndices.AiDriverLeftHandActiveControl] = (int)AiDriverLeftHandStates.PowerHandle;
				} else if (AiDriverLeftHandState == AiDriverLeftHandStates.ReverserHandle) {
					/* AI driver's hand - reverser handle */
					panel[PanelIndices.AiDriverLeftHandActiveControl] = (int)AiDriverLeftHandStates.ReverserHandle;
				} else if (AiDriverLeftHandState == AiDriverLeftHandStates.HeadlightSwitch) {
					/* AI driver's hand - headlight switch */
					panel[PanelIndices.AiDriverLeftHandActiveControl] = (int)AiDriverLeftHandStates.HeadlightSwitch;
				} else if (AiDriverLeftHandState == AiDriverLeftHandStates.TaillightSwitch) {
					/* AI driver's hand - taillight switch */
					panel[PanelIndices.AiDriverLeftHandActiveControl] = (int)AiDriverLeftHandStates.TaillightSwitch;
				} else if (AiDriverLeftHandState == AiDriverLeftHandStates.PantographUpButton) {
					/* AI driver's hand - pantograph up button */
					panel[PanelIndices.AiDriverLeftHandActiveControl] = (int)AiDriverLeftHandStates.PantographUpButton;
				} else if (AiDriverLeftHandState == AiDriverLeftHandStates.PantographDownButton) {
					/* AI driver's hand - pantograph down button */
					panel[PanelIndices.AiDriverLeftHandActiveControl] = (int)AiDriverLeftHandStates.PantographDownButton;
				} else if (AiDriverLeftHandState == AiDriverLeftHandStates.GuardBuzzerButton) {
					/* AI driver's hand - pressing the guard signal button */
					panel[PanelIndices.AiDriverLeftHandActiveControl] = (int)AiDriverLeftHandStates.GuardBuzzerButton;
				}
				
				/* Right arm and hand
				 * ------------------
				 */
				if (AiDriverRightHandState == AiDriverRightHandStates.AwsResetButton) {
					/* AI driver's hand - AWS reset button */
					panel[PanelIndices.AiDriverRightHandActiveControl] = (int)AiDriverRightHandStates.AwsResetButton;
				} else if (AiDriverRightHandState == AiDriverRightHandStates.DraSwitch) {
					/* AI driver's hand - DRA switch */
					panel[PanelIndices.AiDriverRightHandActiveControl] = (int)AiDriverRightHandStates.DraSwitch;
				} else if (AiDriverRightHandState == AiDriverRightHandStates.HornLever) {
					/* AI driver's hand - horn lever */
					panel[PanelIndices.AiDriverRightHandActiveControl] = (int)AiDriverRightHandStates.HornLever;
				}
			}
			
			/* BR 8x series AC electric loco power handle state */
			if (Plugin.Tap.Enabled) {
				panel[PanelIndices.AcLocoPowerHandle] = (int)AcLocoPowerHandlePosition;
			}
			
			/* Pantograph up state */
			if (CabLightsOn) {
				panel[PanelIndices.PantographUp] = 1;
			}
			
			/* Horn state */
			if (HornState == HornStates.Forward || HornState == HornStates.Backward) {
				/* Manual horn blow */
				if ((int)HornState != HornLastPosition) {
					HornCentreTimer = HornCentreTimeout;
				}
				HornCentreTimer = HornCentreTimer - (int)elapsedTime;
				if (HornCentreTimer < 0) {
					HornCentreTimer = HornCentreTimeout;
					HornState = HornStates.Centre;
				}
				HornLastPosition = (int)HornState;
			} else if (HornState == HornStates.AutoForwardFirst) {
				/* Beacon initiated two-tone automatic horn blow */
				if (HornAutoTimer == 0) {
					SoundManager.Play(SoundIndices.HornAutoLowTone, 1.0, 1.0, false);
				}
				HornAutoTimer = HornAutoTimer + (int)elapsedTime;
				if (HornAutoTimer > HornCentreTimeout) {
					HornAutoTimer = 0;
					SoundManager.Play(SoundIndices.HornAutoHighTone, 1.0, 1.0, false);
					HornState = HornStates.Backward;
				}
			} else if (HornState == HornStates.AutoBackwardFirst) {
				/* Beacon initiated two-tone automatic horn blow */
				if (HornAutoTimer == 0) {
					SoundManager.Play(SoundIndices.HornAutoHighTone, 1.0, 1.0, false);
				}
				HornAutoTimer = HornAutoTimer + (int)elapsedTime;
				if (HornAutoTimer > HornCentreTimeout) {
					HornAutoTimer = 0;
					SoundManager.Play(SoundIndices.HornAutoLowTone, 1.0, 1.0, false);
					HornState = HornStates.Forward;
				}
			}
			
			/* Door state */
			if (CabControls.DoorState == DoorStates.None) {
				panel[PanelIndices.InterlockHazard] = 1;
			} else if (CabControls.DoorState == DoorStates.Left) {
				panel[PanelIndices.DoorsLeft] = 1;
			} else if (CabControls.DoorState == DoorStates.Right) {
				panel[PanelIndices.DoorsRight] = 1;
			}
			
			/* Illuminate the guard's buzzer indicator light when the buzzer is sounding */
			if (SoundManager.IsPlaying(SoundIndices.Buzzer)) {
				panel[PanelIndices.GuardBuzzerIndicator] = 1;
			}
			
			/* Master switch state */
			if (MasterSwitchOn) {
				panel[PanelIndices.MasterSwitchOn] = 1;
				panel[PanelIndices.MasterSwitchOff] = 0;
			} else {
				panel[PanelIndices.MasterSwitchOn] = 0;
				panel[PanelIndices.MasterSwitchOff] = 1;
			}
			
			/* Miscellaneous */
			panel[PanelIndices.SwitchHeadLight] = (int)HeadlightControlState;
			panel[PanelIndices.SwitchTailLight] = (int)TaillightControlState;
			if (HornState == HornStates.Centre) {
				panel[PanelIndices.HornLever] = (int)HornStates.Centre;
			} else if (HornState == HornStates.Forward || HornState == HornStates.AutoForwardFirst) {
				panel[PanelIndices.HornLever] = (int)HornStates.Forward;
			} else if (HornState == HornStates.Backward || HornState == HornStates.AutoBackwardFirst) {
				panel[PanelIndices.HornLever] = (int)HornStates.Backward;
			}
			panel[PanelIndices.WiperSwitch] = (int)WiperControlState;
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("");
			}
		}
		
		/// <summary>Initialises or reinitialises the AI driver-related panel states.</summary>
		/// <remarks>Call this method each time the PerformAI() method is called.</remarks>
		internal static void InitialiseAiArms() {
			/* Handle state initialisation for when the AI arm animations are first enabled */
			if (MyAiArmInitialisationState == AiArmInitialisationStates.Pending) {
				MyAiArmInitialisationState = AiArmInitialisationStates.Initialising;
			}
			/* Reset AI driver hand animations to default states */
			CabControls.AiDriverLeftHandState = CabControls.AiDriverLeftHandStates.PowerHandle;
			CabControls.AiDriverRightHandState = CabControls.AiDriverRightHandStates.None;
		}
	}
}
