namespace Plugin {
	/// <summary>Represents the physical in-cab controls.</summary>
	internal static partial class CabControls {
		/// <summary>Possible states of the wiper rotary switch.</summary>
		internal enum WiperControlStates {
			/// <summary>Wiper rotary switch is in the Off position. The numerical value of this constant is 0.</summary>
			Off = 0,
			/// <summary>Wiper rotary switch is in the On (slow) position. The numerical value of this constant is 1.</summary>
			Slow = 1,
			/// <summary>Wiper rotary switch is in the On (fast) position. The numerical value of this constant is 2.</summary>
			Fast = 2
		}
	}
}