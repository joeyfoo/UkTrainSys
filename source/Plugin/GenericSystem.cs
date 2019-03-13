using System;
using OpenBveApi.Runtime;

namespace Plugin {
	
	// -- abstract classes --
	
	/// <summary>Represents the features common to all generic systems.</summary>
	internal abstract class GenericSystem {
		
		// members
		
		/// <summary>The current operative state of the system.</summary>
		/// <remarks>Use this member for determining failure modes.</remarks>
		protected OperativeStates MyOperativeState;
		
		/// <summary>Whether or not the system has had its functionality enabled.</summary>
		/// <remarks>Set this to true if a train is to be equipped with a particular system.</remarks>
		protected bool MyEnabled;
		
		// properties
		
		/// <summary>Gets the current operative state of the system.</summary>
		internal OperativeStates OperativeState {
			get { return this.MyOperativeState; }
		}
		
		/// <summary>Gets whether or not the system is disabled.</summary>
		internal bool Enabled {
			get { return this.MyEnabled; }
		}
	}
}
