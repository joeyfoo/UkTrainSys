using System;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Represents a set of taillights.</summary>
	internal class Taillights : ElectricalSystem {
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal Taillights() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MyBreakerState = SystemBreakerStates.Closed;
			base.RequiredVoltage = 24.0;
			base.RequiredCurrent = 4.0;
		}
		
		// instance methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal override void Reinitialise(InitializationModes mode) {
			if (mode == InitializationModes.OnService) {
				CabControls.TaillightControlState = CabControls.TaillightControlStates.Off;
			} else if (mode == InitializationModes.OnEmergency) {
				CabControls.TaillightControlState = CabControls.TaillightControlStates.On;
			} else if (mode == InitializationModes.OffEmergency) {
				CabControls.TaillightControlState = CabControls.TaillightControlStates.On;
			}
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
		
		/// <summary>This should be called once during each Elapse() call, and defines the behaviour of the system.
		/// Power and brake handle positions should be controlled by issuing demands and requests to the Interlock Manager.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="sound">The array of sound instructions the plugin initialized in the Load call.</param>
		internal override void Update(double elapsedTime, int[] panel, System.Text.StringBuilder debugBuilder) {
			/* If disabled, no processing is done */
			if (base.Enabled) {
				/* Set the conditions under which this system's on behaviour will function */
				if (base.BreakerState == SystemBreakerStates.Closed && base.PowerState == PowerStates.Nominal && base.OperativeState != OperativeStates.Failed) {
					if (CabControls.TaillightControlState == CabControls.TaillightControlStates.On) {
						panel[PanelIndices.ProvingTailLight] = (int)CabControls.TaillightControlStates.On;
					} else if (CabControls.TaillightControlState == CabControls.TaillightControlStates.Off) {
						panel[PanelIndices.ProvingTailLight] = (int)CabControls.TaillightControlStates.Off;
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
