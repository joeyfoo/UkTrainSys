namespace Plugin {
	/// <summary>Possible circuit breaker states.</summary>
	internal enum SystemBreakerStates {
		/// <summary>A circuit breaker is open (turned off). The numerical value of this constant is 0.</summary>
		Open = 0,
		/// <summary>A circuit breaker is closed (turned on). The numerical value of this constant is 1.</summary>
		Closed = 1,
	}
}