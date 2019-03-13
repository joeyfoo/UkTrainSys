namespace Plugin {

	/// <summary>Represents an in-cab blower/fan.</summary>
	internal partial class Blower : ElectricalSystem {
		/// <summary>Possible states of the in-cab blower.</summary>
		internal enum BlowerStates {
			/// <summary>The blower is off. The numerical value of this constant is 0.</summary>
			Off = 0,
			/// <summary>The blower has been turned on. The numerical value of this constant is 1.</summary>
			CommandOn = 1,
			/// <summary>The blower is in the process of spinning up. The numerical value of this constant is 2.</summary>
			SpinningUp = 2,
			/// <summary>The blower is on. The numerical value of this constant is 3.</summary>
			On = 3,
			/// <summary>The blower has been turned off and is in the process of spinning down. The numerical value of this constant is 4.</summary>
			CommandOff = 4,
			/// <summary>The blower is in the process of spinning down. The numerical value of this constant is 5.</summary>
			SpinningDown = 5
		}
	}
}