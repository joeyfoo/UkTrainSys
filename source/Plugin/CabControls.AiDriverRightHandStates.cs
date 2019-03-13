namespace Plugin {
	/// <summary>Represents the physical in-cab controls.</summary>
	internal static partial class CabControls {
		/// <summary>Possible states of the AI driver's right arm.</summary>
		internal enum AiDriverRightHandStates {
			/// <summary>The AI driver is not interacting with any controls, or is transitioning from one control to another. The numerical value of this constant is 0.</summary>
			None = 0,
			/// <summary>The AI driver is interacting with the AWS reset button. The numerical value of this constant is 1.</summary>
			AwsResetButton = 1,
			/// <summary>The AI driver is interacting with the DRA switch. The numerical value of this constant is 2.</summary>
			DraSwitch = 2,
			/// <summary>The AI driver is interacting with the horn lever. The numerical value of this constant is 3.</summary>
			HornLever = 3
		}
	}
}