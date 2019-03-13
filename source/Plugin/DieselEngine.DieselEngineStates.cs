namespace Plugin {

	/// <summary>Represents a diesel engine.</summary>
	internal partial class DieselEngine : PowerSupply {
		
		/// <summary>The possible states of the diesel engine.</summary>
		internal enum DieselEngineStates {
			/// <summary>The diesel engine is off. The numerical value of this constant is 0.</summary>
			Off = 0,
			/// <summary>The diesel engine start has been initiated. The numerical value of this constant is 1.</summary>
			InitiateStart = 1,
			/// <summary>The diesel engine is starting. The numerical value of this constant is 2.</summary>
			Starting = 2,
			/// <summary>The diesel engine has stalled. The numerical value of this constant is 3.</summary>
			Stalled = 3,
			/// <summary>The diesel engine is running. The numerical value of this constant is 4.</summary>
			Running = 4,
			/// <summary>The diesel engine is stopping. The numerical value of this constant is 5.</summary>
			Stopping = 5
		}
	}
}