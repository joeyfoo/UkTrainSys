using System;
using System.Reflection;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Represents a battery.</summary>
	internal class Battery : PowerSupply {
		
		// TODO: Discharge rate / recharge needs some work!
		
		// members
		
		/// <summary>The rate at which the battery discharges.</summary>
		internal double DischargeRate;
		/// <summary>The initial current rating of the battery after discharge.</summary>
		private double LastRemainingCurrentCapacity;
		/// <summary>True when the battery has been initialised during the first Update() method call.</summary>
		private bool Initialised;
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal Battery() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MaximumOutputVoltage = 200;
			base.MaximumOutputCurrent = 60;
			base.CurrentLoad = 0;
			base.RemainingCurrentCapacity = base.MaximumOutputCurrent;
			base.MyBreakerState = SystemBreakerStates.Closed;
			this.DischargeRate = 0.001;
			this.LastRemainingCurrentCapacity = base.MaximumOutputCurrent;
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
		/// <remarks>If the ampere load exceeds the maximum current rating of the power supply, or the power supply can no longer proved the necessary amperage,
		/// the power supply circuit breaker will trip open.</remarks>
		internal override void CalculateCurrentLoad(double elapsedTime) {
			// HACK: Prevent any huge values being used when jumping to a new station
			if (elapsedTime < -5000 || elapsedTime > 5000) {
				elapsedTime = 10;
			}
			
			/* Calculate the current load on this power supply */
			base.CurrentLoad = 0;
			base.RemainingCurrentCapacity = this.LastRemainingCurrentCapacity;
			if (PowerSupplyManager.SelectedPowerSupply == this) {
				if (base.BreakerState == SystemBreakerStates.Closed && base.OperativeState != OperativeStates.Failed) {
					base.CurrentLoad = PowerSupplyManager.RequiredCurrent;
				}
				base.RemainingCurrentCapacity -= base.CurrentLoad;
				if (base.RemainingCurrentCapacity <= 0 && base.PowerState == PowerStates.Nominal) {
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
				
				if (this.BreakerState == SystemBreakerStates.Closed) {
					/* Discharge the battery */
					this.LastRemainingCurrentCapacity -= elapsedTime * (this.DischargeRate / 1000);
					/* Prevent the battery being incorrectly charged when jumping back to the start of a route */
					if (this.LastRemainingCurrentCapacity > base.MaximumOutputCurrent) {
						this.LastRemainingCurrentCapacity = base.MaximumOutputCurrent;
					}
				}
			}
		}
		
		/// <summary>This should be called once during each Elapse() call.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal override void Update(double elapsedTime, int[] panel, System.Text.StringBuilder debugBuilder) {
			/* Set the initial supply current, in case a new maximum current rating was specified
			 * via the configurarion file */
			if (!this.Initialised) {
				this.LastRemainingCurrentCapacity = base.MaximumOutputCurrent;
				this.Initialised = true;
			}
			
			this.CalculateCurrentLoad(elapsedTime);
			
			/* Recharge the battery */
			if (PowerSupplyManager.SelectedPowerSupply != this) {
				if (this.LastRemainingCurrentCapacity < base.MaximumOutputCurrent) {
					this.LastRemainingCurrentCapacity += elapsedTime * (this.DischargeRate / 1000);
					/* Prevent the battery being overcharged when jumping back to the start of a route */
					if (this.LastRemainingCurrentCapacity > base.MaximumOutputCurrent) {
						this.LastRemainingCurrentCapacity = base.MaximumOutputCurrent;
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
