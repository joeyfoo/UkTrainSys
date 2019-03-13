namespace Plugin {
	/// <summary>Represents the physical in-cab controls.</summary>
	internal static partial class CabControls {
		/// <summary>Possible states of the headlights</summary>
		internal enum HeadlightControlStates {
			/// <summary>Headlights are off. The numerical value of this constant is 0.</summary>
			Off = 0,
			/// <summary>Headlights are showing the daytime configuration. The numerical value of this constant is 1.</summary>
			Day = 1,
			/// <summary>Headlights are showing the marker lights only. The numerical value of this constant is 2.</summary>
			MarkerOnly = 2,
			/// <summary>Headlights are showing the nighttime configuration. The numerical value of this constant is 3.</summary>
			Night = 3
		}
	}
}