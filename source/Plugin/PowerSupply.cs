using System;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Represents the features common to all power supplies.</summary>
	internal abstract class PowerSupply : GenericSystem {
		
		// members
		
		/// <summary>The maximum voltage rating of the power supply.</summary>
		internal double MaximumOutputVoltage;
		/// <summary>The maximum current rating of the power supply.</summary>
		internal double MaximumOutputCurrent;
		/// <summary>The present total current being drawn from the power supply.</summary>
		protected double CurrentLoad;
		/// <summary>The remaining current capacity which the power supply has spare.</summary>
		protected double RemainingCurrentCapacity;
		/// <summary>The state of the power supply's circuit breaker.</summary>
		protected SystemBreakerStates MyBreakerState;
		/// <summary>The current power state of the system.</summary>
		protected PowerStates MyPowerState;
		
		// properties
		
		/// <summary>Gets the state of the system's circuit breaker.</summary>
		internal SystemBreakerStates BreakerState {
			get { return this.MyBreakerState; }
		}
		
		/// <summary>Gets the current power state of the system.</summary>
		internal PowerStates PowerState {
			get { return this.MyPowerState; }
		}
		
		// methods
		
		/// <summary>This method should be called if the configuration file indicates that this power supply is to be disabled for this train.</summary>
		internal virtual void Disable() {
			if (this.MyEnabled) {
				base.MyEnabled = false;
			};
		}
		
		/// <summary>Call this method to unconditionally enable the power supply.</summary>
		internal virtual void Enable() {
			if (!this.MyEnabled) {
				base.MyEnabled = true;
			};
		}
		
		/// <summary>Calculates the amperage being demanded by power supply manager and its connected electrical systems.</summary>
		/// <remarks>If the ampere load exceeds the maximum current rating of the power supply, or the power supply can no longer proved the necessary amperage,
		/// the power supply circuit breaker will trip open.</remarks>
		internal abstract void CalculateCurrentLoad(double elapsedTime);
		
		/// <summary>This should be called once during each Elapse() call.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="handles">The handles of the cab.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal abstract void Update(double elapsedTime, int[] panel, System.Text.StringBuilder debugBuilder);
	}
}
