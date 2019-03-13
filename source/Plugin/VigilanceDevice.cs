using System;
using System.Reflection;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>Represents a Vigilance Device.</summary>
	internal partial class VigilanceDevice : ElectricalSystem {
		
		// members
		
		/// <summary>The driver inactivity timeout period in milliseconds.</summary>
		internal int InactivityTimeout;
		/// <summary>The Vigilance Device acknowledgement timeout period.</summary>
		internal int CancelTimeout;
		/// <summary>The timer which keeps track of the driver inactivity countdown in milliseconds.</summary>
		private int MyInactivityTimer;
		/// <summary>The timer which keeps track of the Vigilance Device acknowledgement countdown in milliseconds.</summary>
		private int CancelTimer;
		/// <summary>The current warning state of the Vigilance Device.</summary>
		private SafetyStates MySafetyState;
		/// <summary>The last reported position of the power handle.</summary>
		private int LastPowerPosition;
		/// <summary>The last reported position of the brake handle.</summary>
		private int LastBrakePosition;
		/// <summary>Whether or not the reduced cycle time of 45 seconds is used, when the power handle is in notch 6 or 7.</summary>
		internal bool ReducedCycleTime;
		
		// properties
		
		/// <summary>Gets the current warning state of the Vigilance Device.</summary>
		internal SafetyStates SafetyState {
			get { return this.MySafetyState; }
		}
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal VigilanceDevice() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MyBreakerState = SystemBreakerStates.Closed;
			base.RequiredVoltage = 24.0;
			base.RequiredCurrent = 2.0;
			this.InactivityTimeout = 60000;
			this.CancelTimeout = 7000;
			this.MyInactivityTimer = this.InactivityTimeout;
			this.CancelTimer = this.CancelTimeout;
			this.LastPowerPosition = 0;
			this.LastBrakePosition = 0;
			this.ReducedCycleTime = false;
		}
		
		// event handling methods
		
		/// <summary>This method is called when an Automatic Warning System acknowledgement event occurs.</summary>
		internal void HandleAwsAcknowledgement(Object sender, EventArgs e) {
			if (this.MySafetyState == SafetyStates.InactivityTimerActive) {
				/* Only reset the Vigilance Device inactivity timer if no timeout has occured */
				this.Reset();
			}
		}
		
		/// <summary>This method is called when a TPWS isolation event occurs.</summary>
		/// <remarks>Use this event to disable the Vigilance Device when the TPWS is isolated.</remarks>
		internal void HandleTpwsIsolated(Object sender, EventArgs e) {
			this.Isolate();
		}
		
		/// <summary>This method is called when a TPWS no-longer-isolated event occurs.</summary>
		/// <remarks>Use this event to re-enable the Vigilance Device when the TPWS is no longer isolated.</remarks>
		internal void HandleTpwsNotIsolated(Object sender, EventArgs e) {
			this.Reset();
		}
		
		// instance methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal override void Reinitialise(InitializationModes mode) {
			this.ResetInactivityTimer();
			this.CancelTimer = this.CancelTimeout;
			this.MySafetyState = SafetyStates.InactivityTimerActive;
			this.LastPowerPosition = 0;
			this.LastBrakePosition = 0;
		}
		
		/// <summary>Call this method to unconditionally reset the system in order to cancel any warnings. This does not override the behaviour of the Interlock Manager, however.</summary>
		/// <remarks>This method will unconditionally reset the system, regardless of circumstances, so use it carefully. Any conditional reset of the system is handled within
		/// the Update() method instead.</remarks>
		internal override void Reset() {
			/* Unconditionally resets the Vigilance Device to its default state, cancelling any warnings which are already in effect */
			this.ResetInactivityTimer();
			this.CancelTimer = this.CancelTimeout;
			this.MySafetyState = SafetyStates.InactivityTimerActive;
			this.LastPowerPosition = 0;
			this.LastBrakePosition = 0;
		}
		
		/// <summary>This method should be called if the configuration file indicates that this system is to be disabled for this train.</summary>
		internal override void Disable() {
			/* Use the existing virtual implementation; additional functionality can be added to this override method if required */
			base.Disable();
		}
		
		/// <summary>Call this method to unconditionally enable the system if it has been disabled.</summary>
		internal override void Enable() {
			/* Use the existing virtual implementation; additional functionality can be added to this override method if required */
			base.Enable();
		}
		
		/// <summary>This should be called once during each Elapse() call, and defines the behaviour of the system.
		/// Power and brake handle positions should be controlled by issuing demands and requests to the Interlock Manager.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal override void Update(double elapsedTime, int[] panel, System.Text.StringBuilder debugBuilder) {
			// HACK: Prevent any huge values being used when jumping to a new station
			if (elapsedTime < -5000 || elapsedTime > 5000) {
				elapsedTime = 10;
			}
			
			/* If disabled, no processing is done */
			if (base.Enabled) {
				/* Set the conditions under which this system's on behaviour will function */
				if (CabControls.MasterSwitchOn && base.BreakerState == SystemBreakerStates.Closed &&
				    base.PowerState == PowerStates.Nominal && base.OperativeState != OperativeStates.Failed) {
					if (this.MySafetyState != SafetyStates.Isolated) {
						/* Select behaviour depending upon the system state */
						if (this.MySafetyState == SafetyStates.InactivityTimerActive) {
							/* Decrement the inactivity timer */
							if (SoundManager.IsPlaying(SoundIndices.VigilanceBeep)) {
								SoundManager.Stop(SoundIndices.VigilanceBeep);
							}
							/* Check for any change in the state of the power or brake position to reset the Vigilance Device */
							if ((CabControls.PowerPosition == 0 && CabControls.PowerPosition != this.LastPowerPosition) ||
							    (CabControls.BrakePosition == 0 && CabControls.BrakePosition != this.LastBrakePosition )) {
								this.Reset();
							}
							/* Handle the countdown timer */
							this.MyInactivityTimer = this.MyInactivityTimer - (int)elapsedTime;
							if (this.MyInactivityTimer < 0) {
								this.MyInactivityTimer = 0;
								this.MySafetyState = SafetyStates.CancelTimerActive;
							}
						} else if (this.MySafetyState == SafetyStates.CancelTimerActive) {
							/* The inactivity timer expired, so sound vigilance acknowledgement warning */
							SoundManager.Play(SoundIndices.VigilanceBeep, 1.0, 1.0, true);
							/* Handle the countdown timer */
							this.CancelTimer = this.CancelTimer - (int)elapsedTime;
							if (this.CancelTimer < 0) {
								this.CancelTimer = 0;
								this.MySafetyState = SafetyStates.CancelTimerExpired;
							}
						} else if (this.MySafetyState == SafetyStates.CancelTimerExpired) {
							/* The cancellation timer expired, so keep the vigilance beep playing, and issue a brake demand once */
							SoundManager.Play(SoundIndices.VigilanceBeep, 1.0, 1.0, true);
							InterlockManager.DemandBrakeApplication();
							if (Plugin.Diesel.Enabled) {
								InterlockManager.DemandTractionPowerCutoff();
							}
							this.MySafetyState = SafetyStates.BrakeDemandIssued;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} Vig: [INFORMATION] - Vigilance brake demand issued at {3} metres",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										Plugin.TrainLocation.ToString()
									)
								);
							}
						} else if (this.SafetyState == SafetyStates.BrakeDemandIssued) {
							/* A brake demand has been issued, so play the vigilance warning beep, until the
							 * state can be changed via monitoring the DSD pedal */
							SoundManager.Play(SoundIndices.VigilanceBeep, 1.0, 1.0, true);
						}
					}
				}
				/* Remember the current power and brake positions, so we can check if they have changed in the
				 * next call to this method */
				this.LastPowerPosition = CabControls.PowerPosition;
				this.LastBrakePosition = CabControls.BrakePosition;
			}
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("VigT:{0}",
				                          this.MyInactivityTimer.ToString());
			}
		}
		
		internal void Acknowledge() {
			/* Acknowledge the Vigilance Device warning, if certain conditions are met */
			if (this.MySafetyState == SafetyStates.CancelTimerActive || this.MySafetyState == SafetyStates.BrakeDemandIssued && Plugin.TrainSpeed == 0) {
				this.Reset();
				InterlockManager.RequestBrakeReset();
				InterlockManager.RequestTractionPowerReset();
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} {1} {2} Vigilance: [INFORMATION] - Vigilance acknowledged - resetting timer",
							DateTime.Now.TimeOfDay.ToString(),
							this.GetType().Name.ToString(),
							MethodInfo.GetCurrentMethod().ToString()
						)
					);
				}
			}
		}
		
		/// <summary>Call this method to isolate the Vigilance Device. This will only succeed if the reverser is on neutral, and no safety intervention is in effect.
		/// This does not override the behaviour of the Interlock Manager.</summary>
		internal void Isolate() {
			if (CabControls.ReverserPosition == CabControls.ReverserStates.Neutral && this.MySafetyState != SafetyStates.BrakeDemandIssued ||
			    this.MySafetyState != SafetyStates.CancelTimerExpired) {
				this.MySafetyState = SafetyStates.Isolated;
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} {1} {2} Vigilance: [INFORMATION] - Vigilance isolated",
							DateTime.Now.TimeOfDay.ToString(),
							this.GetType().Name.ToString(),
							MethodInfo.GetCurrentMethod().ToString()
						)
					);
				}
			}
		}
		
		/// <summary>Resets the inactivity timer, taking the reduced cycle time and power handle position into account.</summary>
		internal void ResetInactivityTimer() {
			if (this.ReducedCycleTime) {
				/* Reduce the cycle time if the inactivity timer has just been reset, and the power notch is 6 or 7 */
				if (CabControls.PowerPosition == 6 || CabControls.PowerPosition == 7) {
					this.MyInactivityTimer = this.InactivityTimeout - 15000;
				} else {
					this.MyInactivityTimer = this.InactivityTimeout;
				}
			} else {
				this.MyInactivityTimer = this.InactivityTimeout;
			}
		}
	}
}