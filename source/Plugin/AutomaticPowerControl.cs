using System;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>Represents the pantograph and vacuum circuit breaker.</summary>
	internal partial class AutomaticPowerControl : ElectricalSystem {
		
		// members
		
		/// <summary>The distance from the beacon receiver (front of the train) to the location of the Automatic Power Control receiver, in metres.</summary>
		internal  double ReceiverLocation;
		/// <summary>The current state of the Automatic Power Control system.</summary>
		private ApcStates MyApcState;
		/// <summary>The location of the first Automatic Power Control system magnet.</summary>
		private double LegacyFirstMagnetLocation;
		/// <summary>The distance from the first Automatic Power Control system magnet to the second APC magnet.</summary>
		private double LegacySecondMagnetDistance;
		
		// properties
		
		/// <summary>Gets the current state of the Automatic Power Control System.</summary>
		internal ApcStates ApcState {
			get { return this.MyApcState; }
		}
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal AutomaticPowerControl() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MyBreakerState = SystemBreakerStates.Closed;
			base.RequiredVoltage = 24;
			base.RequiredCurrent = 1;
			this.MyApcState = ApcStates.None;
			this.ReceiverLocation = 0;
			this.LegacyFirstMagnetLocation = 0;
			this.LegacySecondMagnetDistance = 0;
		}
		
		// instance methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal override void Reinitialise(InitializationModes mode) {
			this.MyApcState = ApcStates.None;
			this.LegacyFirstMagnetLocation = 0;
			this.LegacySecondMagnetDistance = 0;
		}
		
		/// <summary>Call this method to unconditionally reset the system in order to cancel any warnings. This does not override the behaviour of the Interlock Manager, however.</summary>
		/// <remarks>This method will unconditionally reset the system, regardless of circumstances, so use it carefully. Any conditional reset of the system is handled within
		/// the Update() method instead.</remarks>
		internal override void Reset() { }
		
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
					/* Act upon the APC magnet */
					if (this.MyApcState == ApcStates.PowerCutOff) {
						/* Check for the ACB/VCB being manually reset */
						if (Plugin.Pan.BreakerState == SystemBreakerStates.Closed) {
							this.MyApcState = ApcStates.None;
						}
					} else if (this.MyApcState == ApcStates.InitiateLegacyPowerCutOff) {
						if (Plugin.TrainSpeed >= 0) {
							if (Plugin.TrainLocation >= this.LegacyFirstMagnetLocation + this.ReceiverLocation) {
								Plugin.Pan.VcbOpen();
								this.MyApcState = ApcStates.LegacyPowerCutOff;
							}
						}
					} else if (this.MyApcState == ApcStates.LegacyPowerCutOff) {
						/* Wait until the train passes the second APC magnet location - legacy behaviour */
						if (Plugin.TrainSpeed >= 0) {
							if (Plugin.TrainLocation >= this.LegacyFirstMagnetLocation + this.LegacySecondMagnetDistance + this.ReceiverLocation) {
								/* If the train's APC receiver has passed the second magnet, re-close the vacuum circuit breaker */
								this.MyApcState = ApcStates.None;
								this.LegacyFirstMagnetLocation = 0;
								this.LegacySecondMagnetDistance = 0;
								Plugin.Pan.VcbClose();
							}
						}
					} else if (this.MyApcState == ApcStates.InitiatePowerOn) {
						this.MyApcState = ApcStates.None;
						this.LegacySecondMagnetDistance = 0;
						Plugin.Pan.VcbClose();
					}
				}
			}
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("");
			}
		}
		
		// other methods
		
		/// <summary>Commands the vacuum circuit breaker to open and stay open for a specified distance (legacy behaviour).</summary>
		/// <param name="location">The location of the first APC magnet.</param>
		/// <param name="distance">The distance from the first APC magnet to the second APC magnet.</param>
		/// <remarks>The vacuum circuit breaker will re-close automatically when the location of the second APC magnet is passed.</remarks>
		internal void LegacyPowerCutOff(double location, double distance) {
			/* Only act upon this beacon if the train is travelling forwards */
			if (Plugin.TrainSpeed >= 0) {
				this.LegacyFirstMagnetLocation = location;
				this.LegacySecondMagnetDistance = distance;
				this.MyApcState = ApcStates.InitiateLegacyPowerCutOff;
			}
		}
		
		/// <summary>Commands the vacuum circuit breaker to open if it is closed, or to close if it is open.</summary>
		/// <param name="location">The location of the beacon/APC magnet.</param>
		internal void PassedMagnet(double location) {
			if (this.MyApcState == ApcStates.None) {
				Plugin.Pan.VcbOpen();
				this.MyApcState = ApcStates.PowerCutOff;
			} else {
				this.MyApcState = ApcStates.InitiatePowerOn;
			}
		}
	}
}
