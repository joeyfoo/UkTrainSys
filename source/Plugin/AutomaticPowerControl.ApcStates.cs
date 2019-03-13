namespace Plugin {

	/// <summary>Represents the pantograph and vacuum circuit breaker.</summary>
	internal partial class AutomaticPowerControl : ElectricalSystem {
		/// <summary>The possible states of the Automatic Power Control system.</summary>
		internal enum ApcStates {
			/// <summary>The Automatic Power Control system is idle or has commanded the vacuum circuit breaker to re-close. The numerical value of this constant is 0.</summary>
			None = 0,
			/// <summary>The Automatic Power Control system has commanded the vacuum circuit breaker to open. The numerical value of this constant is 1.</summary>
			PowerCutOff = 1,
			/// <summary>The Automatic Power Control system has commanded the vacuum circuit breaker to open with legacy behaviour. The numerical value of this constant is 2.</summary>
			InitiateLegacyPowerCutOff = 2,
			/// <summary>The Automatic Power Control system is processing the legacy behaviour of re-closing the VCB after a specified distance. The numerical value of this constant is 3.</summary>
			LegacyPowerCutOff = 3,
			/// <summary>The Automatic Power Control system is waiting for the APC receiver location to equal the APC beacon location before commanding the vacuum circuit breaker to re-close. The numerical value of this constant is 4.</summary>
			InitiatePowerOn = 4
		}
	}
}