namespace Plugin {

	/// <summary>Represents the pantograph and vacuum circuit breaker.</summary>
	internal partial class Pantograph : ElectricalSystem {
		
		/// <summary>The possible states of the pantograph.</summary>
		internal enum PantographStates {
			/// <summary>The pantograph is lowered. The numerical value of this constant is 0.</summary>
			Lowered = 0,
			/// <summary>The pantograph has been commanded to rise. The numerical value of this constant is 1.</summary>
			CommandUp = 1,
			/// <summary>The pantograph is rising. The numerical value of this constant is 2.</summary>
			Rising = 2,
			/// <summary>The pantograph is raised. The numerical value of this constant is 3.</summary>
			Raised = 3,
			/// <summary>The pantograph has been commanded to lower. The numerical value of this constant is 4.</summary>
			CommandDown = 4,
			/// <summary>The pantograph is being lowered. The numerical value of this constant is .5</summary>
			Lowering = 5
		}
	}
}
