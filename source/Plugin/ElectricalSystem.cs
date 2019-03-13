using System;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Represents the features common to all electrical systems.</summary>
	internal abstract class ElectricalSystem : GenericSystem {
		
		// members
		
		/// <summary>The state of the system's circuit breaker.</summary>
		/// <remarks>Use this to connect or disconnect a system from its power supply, or to reset a tripped circuit breaker.</remarks>
		protected SystemBreakerStates MyBreakerState;
		/// <summary>The current power state of the system.</summary>
		internal PowerStates PowerState;
		/// <summary>The voltage required by the system.</summary>
		internal double RequiredVoltage;
		/// <summary>The current required by the system. If a power supply is not able to deliver this amount of current, the device will become inoperative.</summary>
		internal double RequiredCurrent;
		
		// properties
		
		/// <summary>Gets the state of the system's circuit breaker.</summary>
		internal SystemBreakerStates BreakerState {
			get { return this.MyBreakerState; }
		}
		
		// methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal abstract void Reinitialise(InitializationModes mode);
		
		/// <summary>Call this method to unconditionally reset the system in order to cancel any warnings. This does not override the behaviour of the Interlock Manager, however.</summary>
		/// <remarks>This method will unconditionally reset the system, regardless of circumstances, so use it carefully. Any conditional reset of the system is handled within
		/// the Update() method instead.</remarks>
		internal abstract void Reset();
		
		/// <summary>This method should be called if the configuration file indicates that this system is to be disabled for this train.</summary>
		internal virtual void Disable() {
			if (this.MyEnabled) {
				base.MyEnabled = false;
			};
		}
		
		/// <summary>Call this method to unconditionally enable the system if it has been disabled.</summary>
		internal virtual void Enable() {
			if (!this.MyEnabled) {
				base.MyEnabled = true;
			};
		}
		
		/// <summary>This should be called once during each Elapse() call, and defines the behaviour of the system.
		/// Power and brake handle positions should be controlled by issuing demands and requests to the Interlock Manager.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal abstract void Update(double elapsedTime, int[] panel, System.Text.StringBuilder debugBuilder);
	}
}
