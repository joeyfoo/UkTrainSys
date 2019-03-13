using System;
using System.Reflection;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>Represents the pantograph and vacuum circuit breaker.</summary>
	internal partial class Pantograph : ElectricalSystem {
		
		// members
		
		/// <summary>The total time taken to raise the pantograph.</summary>
		internal int UpResetTimeout;
		/// <summary>The timer which keeps track of the pantograph rise countdown in milliseconds.</summary>
		private int UpResetTimer;
		/// <summary>The current state of the pantograph.</summary>
		private PantographStates MyState;
		/// <summary>The distance from the front of the train to the location of the pantograph, in metres.</summary>
		internal  double PantographLocation;
		
		// properties
		
		/// <summary>Gets the current state of the pantograph.</summary>
		internal PantographStates State {
			get { return this.MyState; }
		}
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal Pantograph() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MyBreakerState = SystemBreakerStates.Open;
			base.RequiredVoltage = 0;
			base.RequiredCurrent = 0;
			this.UpResetTimeout = 5000;
			this.UpResetTimer = this.UpResetTimeout;
			this.MyState = PantographStates.Lowered;
			this.PantographLocation = 0;
		}
		
		// instance methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal override void Reinitialise(InitializationModes mode) {
			if (base.Enabled) {
				if (mode == InitializationModes.OnService) {
					this.MyState = PantographStates.Raised;
					base.MyBreakerState = SystemBreakerStates.Closed;
					PowerSupplyManager.ConnectPowerSupply(Plugin.OverheadSupply);
					if (base.PowerState == PowerStates.Nominal) {
						CabControls.CabLightsOn = true;
					}
				} else if (mode == InitializationModes.OnEmergency) {
					this.MyState = PantographStates.Raised;
					base.MyBreakerState = SystemBreakerStates.Closed;
					PowerSupplyManager.ConnectPowerSupply(Plugin.OverheadSupply);
					if (base.PowerState == PowerStates.Nominal) {
						CabControls.CabLightsOn = true;
					}
				} else if (mode == InitializationModes.OffEmergency) {
					this.MyState = PantographStates.Lowered;
					base.MyBreakerState = SystemBreakerStates.Open;
					PowerSupplyManager.ConnectPowerSupply(Plugin.MainBattery);
					CabControls.CabLightsOn = false;
				}
			}
		}
		
		/// <summary>Call this method to unconditionally reset the system in order to cancel any warnings. This does not override the behaviour of the Interlock Manager, however.</summary>
		/// <remarks>This method will unconditionally reset the system, regardless of circumstances, so use it carefully. Any conditional reset of the system is handled within
		/// the Update() method instead.</remarks>
		internal override void Reset() {
			this.MyState = PantographStates.Lowered;
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
			/* If disabled, no processing is done */
			if (base.Enabled) {
				/* Set the conditions under which this system's on behaviour will function */
				if (CabControls.MasterSwitchOn && base.OperativeState != OperativeStates.Failed) {
					/* 
					 * Raising or resetting the pantograph
					 */
					if (this.MyState == PantographStates.CommandUp) {
						/* Play the pantograph up air release sound */
						SoundManager.Play(SoundIndices.PantographUp, 1.0, 1.0, false);
						this.MyState = PantographStates.Rising;
					} else if (this.MyState == PantographStates.Rising) {
						/* Decrement the pantograph raise/lower timer */
						this.UpResetTimer = this.UpResetTimer - (int)elapsedTime;
						if (this.UpResetTimer < 0) {
							this.UpResetTimer = this.UpResetTimeout;
							this.MyState = PantographStates.Raised;
							/* Pantograph is raised, so close the vacuum circuit breaker */
							base.MyBreakerState = SystemBreakerStates.Closed;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} Pantograph: [INFORMATION] - Raised",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString()
									)
								);
							}
							/* Only do the following if there is voltage in the overhead line */
							if (base.PowerState == PowerStates.Nominal && Plugin.OverheadSupply.BreakerState == SystemBreakerStates.Closed) {
								CabControls.CabLightsOn = true;
								/* Set the active power source to the overhead supply */
								PowerSupplyManager.ConnectPowerSupply(Plugin.OverheadSupply);
								/* Play the spark sound representing the pantograph head touching the contact wire */
								SoundManager.Play(SoundIndices.Sparks, 1.0, 1.0, false);
								/* start the in-cab blower */
								Plugin.Fan.Reset();
								InterlockManager.RequestTractionPowerReset();
							}
							InterlockManager.RequestBrakeReset();
						}
					}
				}
				
				/* Off behaviour should work regardless of power supply or failure modes */
				if (this.MyState == PantographStates.CommandDown) {
					/* 
					 * Lowering the pantograph
					 */
					/* Reset the timer to half the value of the up/reset timeout period */
					this.UpResetTimer = this.UpResetTimeout / 2;
					this.MyState = PantographStates.Lowering;
					/* Open the vacuum circuit breaker */
					base.MyBreakerState = SystemBreakerStates.Open;
					if (base.PowerState == PowerStates.Nominal && Plugin.OverheadSupply.BreakerState == SystemBreakerStates.Closed) {
						/* Play the spark sound representing the pantograph head losing touch with the contact wire */
						SoundManager.Play(SoundIndices.Sparks, 1.0, 1.0, false);
					}
					/* The pantograph has lost touch with the contact wire, so set the active power source to the battery */
					CabControls.CabLightsOn = false;
					PowerSupplyManager.ConnectPowerSupply(Plugin.MainBattery);
					InterlockManager.DemandBrakeApplication();
					InterlockManager.DemandTractionPowerCutoff();
					if (Plugin.DebugMode) {
						Plugin.ReportLogEntry(
							string.Format(
								"{0} {1} {2} Pantograph: [INFORMATION] - Lowered",
								DateTime.Now.TimeOfDay.ToString(),
								this.GetType().Name.ToString(),
								MethodInfo.GetCurrentMethod().ToString()
							)
						);
					}
				} else if (this.MyState == PantographStates.Lowering) {
					this.UpResetTimer = this.UpResetTimer - (int)elapsedTime;
					if (this.UpResetTimer < 0) {
						this.UpResetTimer = this.UpResetTimeout;
						this.MyState = PantographStates.Lowered;
					}
				}
				
				/* Monitor the overhead supply for restoration of line voltage */
				if (this.MyState == PantographStates.Raised && base.MyBreakerState == SystemBreakerStates.Closed &&
				    Plugin.OverheadSupply.BreakerState == SystemBreakerStates.Closed && base.PowerState == PowerStates.Nominal) {
					if (PowerSupplyManager.SelectedPowerSupply != Plugin.OverheadSupply) {
						CabControls.CabLightsOn = true;
						PowerSupplyManager.ConnectPowerSupply(Plugin.OverheadSupply);
						if (CabControls.PowerPosition > 0) {
							/* Play the spark sound representing the pantograph head touching the contact wire */
							SoundManager.Play(SoundIndices.Sparks, 1.0, 1.0, false);
						}
						/* start the in-cab blower */
						Plugin.Fan.Reset();
					}
				}
				
				/* Catch-all handling of power supply state */
				if (base.PowerState == PowerStates.Nominal) {
					/* If the pantograph is raised, manifest audio-visual feedback */
					if (PowerSupplyManager.SelectedPowerSupply is OverheadPowerSupply && base.BreakerState == SystemBreakerStates.Closed) {
						panel[PanelIndices.LineVolts] = 1;
					}
					
					if (base.BreakerState == SystemBreakerStates.Open && this.MyState == PantographStates.Raised) {
						/* Only illuminate this indicator if the pantograph is raised */
						panel[PanelIndices.VacuumCircuitBreaker] = 1;
					}
				} else {
					/* If there is a loss of power from the overhead supply, simulate the effects */
					CabControls.CabLightsOn = false;
					PowerSupplyManager.ConnectPowerSupply(Plugin.MainBattery);
					InterlockManager.DemandTractionPowerCutoff();
				}
			}
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("");
			}
		}
		
		// other methods
		
		/// <summary>Raises the pantograph.</summary>
		internal void Raise() {
			/* Set the appropriate state; only allow a pantograph rise or reset if prerequisite conditions are met,
			 * and the pantograph is not already rising */
			if (CabControls.MasterSwitchOn && StartupSelfTestManager.SequenceState >= StartupSelfTestManager.SequenceStates.Initialising &&
			    Plugin.TrainSpeed == 0 && CabControls.ReverserPosition == CabControls.ReverserStates.Neutral && CabControls.PowerPosition == 0) {
				if (this.MyState == PantographStates.Raised) {
					/* Re-close the VCB if the pantograph is already rasied */
					this.VcbClose();
				} else if (this.MyState != PantographStates.Rising && this.MyState != PantographStates.Lowering) {
					this.MyState = PantographStates.CommandUp;
				}
			}
		}
		
		/// <summary>Lowers the pantograph.</summary>
		internal void Lower() {
			/* Only lower the pantograph if it is not already lowering or lowered */
			if (base.MyEnabled && CabControls.MasterSwitchOn && this.MyState != PantographStates.Lowering && this.MyState != PantographStates.Lowered &&
			    this.MyState != PantographStates.Rising) {
				this.MyState = PantographStates.CommandDown;
			}
		}
		
		/// <summary>Opens the vacuum circuit breaker.</summary>
		internal void VcbOpen() {
			if (base.BreakerState != SystemBreakerStates.Open) {
				base.MyBreakerState = SystemBreakerStates.Open;
				SoundManager.Play(SoundIndices.Thwok, 1.0, 1.0, false);
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} {1} {2} Pantograph: [INFORMATION] - Vacuum circuit breaker commanded open",
							DateTime.Now.TimeOfDay.ToString(),
							this.GetType().Name.ToString(),
							MethodInfo.GetCurrentMethod().ToString()
						)
					);
				}
				CabControls.CabLightsOn = false;
				PowerSupplyManager.ConnectPowerSupply(Plugin.MainBattery);
				InterlockManager.DemandTractionPowerCutoff();
			}
		}
		
		/// <summary>Closes the vacuum circuit breaker.</summary>
		internal void VcbClose() {
			if (this.MyBreakerState != SystemBreakerStates.Closed) {
				base.MyBreakerState = SystemBreakerStates.Closed;
				SoundManager.Play(SoundIndices.Thwok, 1.0, 1.0, false);
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} {1} {2} Pantograph: [INFORMATION] - Vacuum circuit breaker commanded closed",
							DateTime.Now.TimeOfDay.ToString(),
							this.GetType().Name.ToString(),
							MethodInfo.GetCurrentMethod().ToString()
						)
					);
				}
				CabControls.CabLightsOn = true;
				PowerSupplyManager.ConnectPowerSupply(Plugin.OverheadSupply);
				InterlockManager.RequestTractionPowerReset();
			}
		}
	}
}
