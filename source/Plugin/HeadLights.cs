using System;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>Represents a set of headlights.</summary>
	internal class Headlights : ElectricalSystem {
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal Headlights() {
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
				CabControls.HeadlightControlState = CabControls.HeadlightControlStates.Day;
			} else if (mode == InitializationModes.OnEmergency) {
				CabControls.HeadlightControlState = CabControls.HeadlightControlStates.Off;
			} else if (mode == InitializationModes.OffEmergency) {
				CabControls.HeadlightControlState = CabControls.HeadlightControlStates.Off;
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
				if (base.BreakerState == SystemBreakerStates.Closed && base.PowerState == PowerStates.Nominal && base.OperativeState != OperativeStates.Failed) {
					if (CabControls.HeadlightControlState == CabControls.HeadlightControlStates.Day) {
						panel[PanelIndices.ProvingHeadLight] = (int)CabControls.HeadlightControlStates.Day;
					} else if (CabControls.HeadlightControlState == CabControls.HeadlightControlStates.MarkerOnly) {
						panel[PanelIndices.ProvingHeadLight] = (int)CabControls.HeadlightControlStates.MarkerOnly;
					} else if (CabControls.HeadlightControlState == CabControls.HeadlightControlStates.Night) {
						panel[PanelIndices.ProvingHeadLight] = (int)CabControls.HeadlightControlStates.Night;
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
