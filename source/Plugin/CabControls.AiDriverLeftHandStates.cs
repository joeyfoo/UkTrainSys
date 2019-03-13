namespace Plugin {
	/// <summary>Represents the physical in-cab controls.</summary>
	internal static partial class CabControls {
		/// <summary>Possible states of the AI driver's left arm.</summary>
		internal  enum AiDriverLeftHandStates {
			/// <summary>The AI driver is not interacting with any controls, or is transitioning from one control to another. The numerical value of this constant is 0.</summary>
			None = 0,
			/// <summary>The AI driver is interacting with the power handle. The numerical value of this constant is 1.</summary>
			PowerHandle = 1,
			/// <summary>The AI driver is interacting with the reverser handle. The numerical value of this constant is 2.</summary>
			ReverserHandle = 2,
			/// <summary>The AI driver is interacting with the headlight switch. The numerical value of this constant is 3.</summary>
			HeadlightSwitch = 3,
			/// <summary>The AI driver is interacting with the taillight switch. The numerical value of this constant is 4.</summary>
			TaillightSwitch = 4,
			/// <summary>The AI driver is interacting with the pantograph up/reset button. The numerical value of this constant is 5.</summary>
			PantographUpButton = 5,
			/// <summary>The AI driver is interacting with the pantograph down. The numerical value of this constant is 6.</summary>
			PantographDownButton = 6,
			/// <summary>The AI driver is interacting with the guard's buzzer button. The numerical value of this constant is 7.</summary>
			GuardBuzzerButton = 7
		}
	}
}