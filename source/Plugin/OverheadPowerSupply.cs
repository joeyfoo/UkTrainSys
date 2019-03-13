using System;
using System.Reflection;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Represents an overhead power supply and traction power package, including input voltage and current from the overhead line, as well as
	/// output current and voltage after being stepped-down and rectified.</summary>
	internal class OverheadPowerSupply : PowerSupply {
		
		// TODO: Input voltage and current (at the contact wire) is not currently used
		// TODO: No distinction between AC and DC input or output is made yet
		
		// members
		
		/// <summary>The maximum voltage which can be collected from the overhead catenary system.</summary>
		internal double MaximumInputVoltage;
		/// <summary>The maximum current which can be collected from the overhead catenary system.</summary>
		internal double MinimumInputVoltage;
		/// <summary>Whether or not a neutral section is currenly being simulated.</summary>
		private bool NeutralSectionStateChange;
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal OverheadPowerSupply() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MaximumOutputVoltage = 750;
			base.MaximumOutputCurrent = 3000;
			base.CurrentLoad = 0;
			base.RemainingCurrentCapacity = base.MaximumOutputCurrent;
			base.MyBreakerState = SystemBreakerStates.Closed;
			this.MaximumInputVoltage = 29000;
			this.MinimumInputVoltage = 17500;
		}
		
		// instance methods
		
		/// <summary>This method should be called if the configuration file indicates that this power supply is to be disabled for this train.</summary>
		internal override void Disable() {
			/* Use the existing virtual implementation; additional functionality can be added to this override method if required */
			base.Disable();
		}
		
		/// <summary>Call this method to unconditionally enable the power supply.</summary>
		internal override void Enable() {
			/* Use the existing virtual implementation; additional functionality can be added below if required */
			base.Enable();
		}
		
		/// <summary>Calculates the amperage being demanded by power supply manager and its connected electrical systems.</summary>
		/// <remarks>If the ampere load exceeds the maximum current rating of the power supply, the power supply circuit breaker will trip open.</remarks>
		internal override void CalculateCurrentLoad(double elapsedTime) {
			/* Calculate the current load on this power supply */
			base.CurrentLoad = 0;
			base.RemainingCurrentCapacity = base.MaximumOutputCurrent;
			if (PowerSupplyManager.SelectedPowerSupply == this) {
				if (base.BreakerState == SystemBreakerStates.Closed && base.OperativeState != OperativeStates.Failed) {
					base.CurrentLoad = PowerSupplyManager.RequiredCurrent;
				}
				base.RemainingCurrentCapacity -= base.CurrentLoad;
				if (this.RemainingCurrentCapacity <= 0 && this.PowerState == PowerStates.Nominal) {
					/* There is an overload condition, to trip the breaker and set the power state accordingly */
					base.MyBreakerState = SystemBreakerStates.Open;
					base.MyPowerState = PowerStates.Overloaded;
					SoundManager.Play(SoundIndices.Sparks, 1.0, 1.0, false);
					
					if (Plugin.DebugMode) {
						Plugin.ReportLogEntry(
							string.Format(
								"{0} {1} {2} [CONDITION] - Overload",
								DateTime.Now.TimeOfDay.ToString(),
								this.GetType().Name.ToString(),
								MethodInfo.GetCurrentMethod().ToString()
							)
						);
					}
				}
				
				if (base.CurrentLoad <= base.RemainingCurrentCapacity) {
					/* There is no overload condition, so set the power state accordingly */
					if (base.PowerState != PowerStates.Nominal) {
						base.MyPowerState = PowerStates.Nominal;
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} [CONDITION] Overload cleared",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString()
								)
							);
						}
					}
				}
			}
		}
		
		/// <summary>This should be called once during each Elapse() call.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="sound">The array of sound instructions the plugin initialized in the Load call.</param>
		internal override void Update(double elapsedTime, int[] panel, System.Text.StringBuilder debugBuilder) {
			/* If disabled, no processing is done */
			if (base.Enabled) {
				this.CalculateCurrentLoad(elapsedTime);
				
				/* Simulate a neutral section in the overhead line */
				if (this.NeutralSectionStateChange) {
					if (base.MyBreakerState == SystemBreakerStates.Closed) {
						base.MyBreakerState = SystemBreakerStates.Open;
						InterlockManager.DemandTractionPowerCutoff();
						this.NeutralSectionStateChange = false;
					} else {
						base.MyBreakerState = SystemBreakerStates.Closed;
						InterlockManager.RequestTractionPowerReset();
						this.NeutralSectionStateChange = false;
					}
				}
				
				/* Add any information to display via openBVE's in-game debug interface mode below */
				if (Plugin.DebugMode) {
					debugBuilder.AppendFormat("");
				}
			}
		}
		
		/// <summary>This should be called to simulate the absence of power due to a neutral section.</summary>
		internal void SwitchNeutralState() {
			if (base.Enabled) {
				this.NeutralSectionStateChange = true;
			}
		}
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal void Reinitialise(InitializationModes mode) {
			/* Re-enable the power supply if jumping to a new station */
			base.MyBreakerState = SystemBreakerStates.Closed;
		}
	}
}
