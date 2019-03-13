namespace Plugin {
	/// <summary>Represents the physical in-cab controls.</summary>
	internal static partial class CabControls {
		/// <summary>Possible states of the BR 8x series AC electric power handle.</summary>
		internal enum AcLocoPowerHandleStates {
			/// <summary>The power handle is in the Off position. The numerical value of this constant is 0.</summary>
			Off = 0,
			/// <summary>The power handle is in the Run Down position. The numerical value of this constant is 1.</summary>
			RunDown = 1,
			/// <summary>The power handle is in the Notch Down position. The numerical value of this constant is 2.</summary>
			NotchDown = 2,
			/// <summary>The power handle is in the Hold position. The numerical value of this constant is 3.</summary>
			Hold = 3,
			/// <summary>The power handle is in the Notch Up position. The numerical value of this constant is 4.</summary>
			NotchUp = 4,
			/// <summary>The power handle is in the Run Up position. The numerical value of this constant is 5.</summary>
			RunUp = 5
		}
	}
}