using System;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>Represents an in-cab blower/fan.</summary>
	internal partial class Blower : ElectricalSystem {
		
		// members
		
		/// <summary>The delay in milliseconds before the blower sound loops, allowing the blower start and stop sound to play before.</summary>
		internal int Delay;
		/// <summary>The timer used during the startup self-test sequence.</summary>
		private int Timer;
		/// <summary>The current state of the in-cab blower.</summary>
		private BlowerStates MyState;
		
		// properties
		
		/// <summary>Gets the current state of the blower.</summary>
		internal BlowerStates State {
			get { return this.MyState; }
		}
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal Blower() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MyBreakerState = SystemBreakerStates.Closed;
			base.RequiredVoltage = 24.0;
			base.RequiredCurrent = 4.0;
			this.Delay = 1700;
			this.Timer = this.Delay;
			this.MyState = BlowerStates.Off;
		}
		
		// instance methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal override void Reinitialise(InitializationModes mode) {
			if (mode == InitializationModes.OnService) {
				this.MyState = BlowerStates.CommandOn;
			} else if (mode == InitializationModes.OnEmergency) {
				this.MyState = BlowerStates.CommandOff;
			} else if (mode == InitializationModes.OffEmergency) {
				this.MyState = BlowerStates.CommandOff;
			}
		}
		
		/// <summary>Call this method to unconditionally reset the system in order to cancel any warnings. This does not override the behaviour of the Interlock Manager, however.</summary>
		/// <remarks>This method will unconditionally reset the system, regardless of circumstances, so use it carefully. Any conditional reset of the system is handled within
		/// the Update() method instead.</remarks>
		internal override void Reset() {
			/* Set the appropriate state */
			if (CabControls.MasterSwitchOn) {
				this.MyState = BlowerStates.CommandOn;
			}
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
				if (CabControls.MasterSwitchOn && base.BreakerState == SystemBreakerStates.Closed &&
				    base.PowerState == PowerStates.Nominal && base.OperativeState != OperativeStates.Failed) {
					if (this.MyState == BlowerStates.CommandOn) {
						if (base.OperativeState == OperativeStates.Normal) {
							this.Timer = this.Delay;
							this.MyState = BlowerStates.SpinningUp;
							SoundManager.Play(SoundIndices.BlowerLoopStart, 1.0, 1.0, false);
						}
					} else if (this.MyState == BlowerStates.SpinningUp) {
						if (base.OperativeState == OperativeStates.Normal) {
							this.Timer = this.Timer - (int)elapsedTime;
							if (this.Timer < 0) {
								this.Timer = this.Delay;
								this.MyState = BlowerStates.On;
							}
						}
					} else if (this.MyState == BlowerStates.On) {
						if (base.OperativeState == OperativeStates.Normal) {
							SoundManager.Play(SoundIndices.BlowerLoop, 1.0, 1.0, true);
						}
					}
				}
				
				/* Off behaviour should work regardless of power supply or failure modes */
				if (this.MyState == BlowerStates.CommandOff) {
					if (StartupSelfTestManager.SequenceState > StartupSelfTestManager.SequenceStates.Initialising) {
						SoundManager.Play(SoundIndices.BlowerLoopEnd, 1.0, 1.0, false);
					}
					SoundManager.Stop(SoundIndices.BlowerLoop);
					this.Timer = this.Delay;
					this.MyState = BlowerStates.SpinningDown;
				} else if (this.MyState == BlowerStates.SpinningDown) {
					this.Timer = this.Timer - (int)elapsedTime;
					if (this.Timer < 0) {
						this.Timer = this.Delay;
						this.MyState = BlowerStates.Off;
					}
				}
			}
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("");
			}
		}
		
		// other methods
		
		/// <summary>Call this method to turn off a system, if the system supports it. This is not the same as opening the system's circuit breaker.</summary>
		internal void TurnOff() {
			if (this.MyState == BlowerStates.On) {
				this.MyState = BlowerStates.CommandOff;
			}
		}
	}
}