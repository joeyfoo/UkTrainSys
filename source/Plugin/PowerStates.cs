namespace Plugin {
	/// <summary>Possible system power states.</summary>
	internal enum PowerStates {
		/// <summary>The system is operating nominally. The numerical value of this constant is 0.</summary>
		Nominal = 0,
		/// <summary>The system is not working because there is insufficient power being provided.
		/// Operation will be restored when enough power is avaiable again, provided the appropriate circuit breakers are closed.
		/// The numerical value of this constant is 1.</summary>
		InsufficientPower = 1,
		/// <summary>The system is overloaded. The numerical value of this constant is 2.</summary>
		Overloaded = 2
	}
}