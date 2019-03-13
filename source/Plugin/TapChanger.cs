using System;
using System.Reflection;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>Represents an Automatic Train Protection System.</summary>
	internal partial class TapChanger : ElectricalSystem {
		
		// TODO: This class is not finished yet, and methods require documentation
		
		// members
		
		/// <summary>The total number taps.</summary>
		private int TapCount;
		/// <summary>The current tap or notch.</summary>
		private int CurrentTap;
		/// <summary>The time taken to change the tap.</summary>
		private int TapIncrementRate;
		/// <summary>A timer for keeping track of the tap change time.</summary>
		private int TapChangeTimer;
		/// <summary>The last reported power notch.</summary>
		private int LastPowerNotch;
		/// <summary>Whether or not the current tap can currently be incremented or decremented.</summary>
		/// <remarks>Use this to allow only one notch change when the power handle is moved to the Notch Up or Down position and left there.</remarks>
		private bool CanChangeTap;
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal TapChanger() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MyBreakerState = SystemBreakerStates.Closed;
			base.RequiredVoltage = 0.0;
			base.RequiredCurrent = 0.0;
			this.TapCount = 38;
			this.CurrentTap = 0;
			this.TapIncrementRate = 580;
			this.TapChangeTimer = 0;
			this.LastPowerNotch = 0;
			this.CanChangeTap = false;
		}
		
		// instance methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal override void Reinitialise(InitializationModes mode) {
			this.Reset();
		}
		
		/// <summary>Call this method to unconditionally reset the system in order to cancel any warnings. This does not override the behaviour of the Interlock Manager, however.</summary>
		/// <remarks>This method will unconditionally reset the system, regardless of circumstances, so use it carefully. Any conditional reset of the system is handled within
		/// the Update() method instead.</remarks>
		internal override void Reset() {
			this.CurrentTap = 0;
			this.TapChangeTimer = 0;
			this.LastPowerNotch = 0;
			this.CanChangeTap = false;
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
				if (CabControls.AcLocoPowerHandlePosition == CabControls.AcLocoPowerHandleStates.NotchUp) {
					if (this.CanChangeTap && this.CurrentTap < this.TapCount) {
						this.CurrentTap++;
						this.CanChangeTap = false;
					}
				} else if (CabControls.AcLocoPowerHandlePosition == CabControls.AcLocoPowerHandleStates.NotchDown) {
					if (this.CanChangeTap && this.CurrentTap > 0) {
						this.CurrentTap--;
						this.CanChangeTap = false;
					}
				} else if (CabControls.AcLocoPowerHandlePosition == CabControls.AcLocoPowerHandleStates.RunUp) {
					if (this.CurrentTap < this.TapCount) {
						this.TapChangeTimer = this.TapChangeTimer + (int)elapsedTime;
						if (this.TapChangeTimer >= this.TapIncrementRate) {
							this.CurrentTap++;
							this.TapChangeTimer = 0;
						}
					}
				} else if (CabControls.AcLocoPowerHandlePosition == CabControls.AcLocoPowerHandleStates.RunDown) {
					if (this.CurrentTap > 0) {
						this.TapChangeTimer = this.TapChangeTimer + (int)elapsedTime;
						if (this.TapChangeTimer >= this.TapIncrementRate) {
							this.CurrentTap--;
							this.TapChangeTimer = 0;
						}
					}
				}
			}
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("");
			}
		}
		
		internal void SetHandle(int powerNotch) {
			if (powerNotch > this.LastPowerNotch) {
				CabControls.AcLocoPowerHandlePosition++;
			} else if (powerNotch < this.LastPowerNotch){
				CabControls.AcLocoPowerHandlePosition--;
			}
			this.LastPowerNotch = powerNotch;
			this.CanChangeTap = true;
		}
	}
}
