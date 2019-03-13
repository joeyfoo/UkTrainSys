using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>Stores information about a beacon when it is passed over, so that it can be used again when a train with an offset beacon receiver is travelling backwards.</summary>
	/// <remarks>The train's beacon receiver is located at the front of the train. However, some receivers on a train are located
	/// some distance from the front of the train. When travelling backwards over certain beacons, the beacon will be detected only
	/// when the *now rear* of the train passes, which may be too late for realistic behaviour to be simulated. When the train is travelling
	/// backwards, this stored beacon information can be used to offset the trigger point for responding to the beacon accordingly, and
	/// systems which have offset beacon receiver locations, can respond to this modified trigger point, rather than the actual beacon location.</remarks>
	internal static partial class OffsetBeaconReceiverManager {
		
		/// <summary>Stores information about a beacon.</summary>
		internal class PreviousBeacon {
			
			// members
			
			/// <summary>The actual location of the beacon.</summary>
			internal double Location;
			/// <summary>The beacon type.</summary>
			internal int Type;
			/// <summary>The optional data supplied by the beacon.</summary>
			internal int Data;
			/// <summary>The distance from the front of the train, to where the beacon receiver which should respond to this beacon, is located.</summary>
			internal double BeaconReceiverOffset;
			/// <summary>Whether the beacon has been passed over while travelling forwards.</summary>
			internal bool HasBeenPassed;
			
			// constructors
			
			/// <summary>Creates a new instance of this class.</summary>
			/// <param name="location">The location of the beacon.</param>
			/// <param name="type">The beacon type.</param>
			/// <param name="data">The beacon's optional data.</param>
			/// <param name="beaconReceiverOffset">The distance from the front of the train, to where the beacon receiver which should respond to this beacon, is located.</param>
			/// <param name="hasBeenPassed">Whether the beacon has been passed over while travelling forwards.</param>
			internal PreviousBeacon(double location, int type, int data, double beaconReceiverOffset, bool hasBeenPassed) {
				this.Location = location;
				this.Type = type;
				this.Data = data;
				this.BeaconReceiverOffset = beaconReceiverOffset;
				this.HasBeenPassed = hasBeenPassed;
			}
		}
	}
}