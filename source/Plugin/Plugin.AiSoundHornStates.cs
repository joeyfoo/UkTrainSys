using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>The interface to be implemented by the plugin.</summary>
	public partial class Plugin : IRuntime {

		/// <summary>Possible states of the AI driver's horn lever interaction.</summary>
		internal enum AiSoundHornStates {
			/// <summary>The AI driver is not interacting with the horn. The numerical value of this constant is 0.</summary>
			None = 0,
			/// <summary>The AI driver should push the horn lever forward first, and then pull it backwards. The numerical value of this constant is 1.</summary>
			Forward = 1,
			/// <summary>The AI driver should pull the horn lever backwards first, and then push it forwards. The numerical value of this constant is 2.</summary>
			Backward = 2,
			/// <summary>The AI driver should wait before removing hand from horn lever. The numerical value of this constant is 3.</summary>
			Wait = 3
		}
	}
}