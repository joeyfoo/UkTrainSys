using System;
using System.Globalization;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Stores information about a beacon when it is passed over, so that it can be used again when the train is travelling backwards, using an offset beacon receiver location.</summary>
	/// <remarks>The train's beacon receiver is located at the front of the train. However, some receivers on a train are located
	/// some distance from the front of the train. When travelling backwards over certain beacons, the beacon will be detected only
	/// when the *now rear* of the train passes, which may be too late for realistic behaviour to be simulated. When the train is travelling
	/// backwards, this historical beacon information can be used to offset the location of the original beacon accordingly, and systems which
	/// have offset beacon receiver locations, can respond to this historical beacon location, rather than the actual beacon.</remarks>
	internal static partial class OffsetBeaconReceiverManager {
		
		// members
		
		/// <summary>An array holding previously passed beacon information.</summary>
		private static PreviousBeacon[] PreviousBeacons = new PreviousBeacon[1];
		/// <summary>Keeps track of the number of beacons about which information is stored.</summary>
		private static int BeaconCount;
		/// <summary>The beacon index which is the next trigger point.</summary>
		private static int ActiveBeaconIndex = -1;
		
		// methods
		
		/// <summary>Re-initialises the class to a default state. Only call this method via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal static void Reinitialise(InitializationModes mode) {
			PreviousBeacons = new PreviousBeacon[1];
			BeaconCount = 0;
			ActiveBeaconIndex = -1;
		}
		
		/// <summary>Stores information about a beacon.</summary>
		/// <param name="location">The location of the beacon.</param>
		/// <param name="type">The beacon type.</param>
		/// <param name="data">The beacon's optional data.</param>
		/// <param name="beaconReceiverOffset">The number of metres ahead of the actual beacon location, where the action normally carried out upon
		/// passing the actual beacon location, should occur.</param>
		/// <remarks>Beacon information is only stored if the train is travelling forwards at the time of addition.</remarks>
		internal static void AddBeacon(double location, int type, int data, double beaconReceiverOffset) {
			if (Plugin.TrainSpeed >= 0) {
				if (PreviousBeacons.Length == BeaconCount) {
					Array.Resize<PreviousBeacon>(ref PreviousBeacons, PreviousBeacons.Length * 2);
				}
				PreviousBeacons[BeaconCount] = new PreviousBeacon(location, type, data, beaconReceiverOffset, false);
				BeaconCount++;
				if (ActiveBeaconIndex < 0) {
					ActiveBeaconIndex = 0;
				}
			}
		}
		
		/// <summary>This should be called once during each Elapse() call.</summary>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal static void Update(System.Text.StringBuilder debugBuilder) {
			if (BeaconCount > 0) {
				if (Plugin.TrainSpeed >= 0) {
					/* Find the first earliest beacon which has yet to be passed */
					for (int i = 0; i < BeaconCount; i++) {
						if (!PreviousBeacons[i].HasBeenPassed) {
							ActiveBeaconIndex = i;
							i = BeaconCount;
						}
					}
					if (ActiveBeaconIndex >= 0) {
						/* Determine at what route location the beacon related event should be triggered */
						double triggerLocation = PreviousBeacons[ActiveBeaconIndex].Location + PreviousBeacons[ActiveBeaconIndex].BeaconReceiverOffset;
						if (Plugin.TrainLocation >= triggerLocation && !PreviousBeacons[ActiveBeaconIndex].HasBeenPassed) {
							/* Call location based system events here */
							switch (PreviousBeacons[ActiveBeaconIndex].Type) {
								case 20:
									if (PreviousBeacons[ActiveBeaconIndex].Data == -1) {
										/* The actual neutral section in the overhead line */
										Plugin.OverheadSupply.SwitchNeutralState();
									} else if (PreviousBeacons[ActiveBeaconIndex].Data <= 0) {
										/* Handle new APC magnet behaviour which requires two beacons */
										Plugin.Apc.PassedMagnet(triggerLocation);
									}
									break;
							}
							PreviousBeacons[ActiveBeaconIndex].HasBeenPassed = true;
						}
					}
				} else if (Plugin.TrainSpeed < 0) {
					if (ActiveBeaconIndex < BeaconCount && ActiveBeaconIndex >= 0) {
						/* Determine at what route location the beacon related event should be triggered */
						double triggerLocation = PreviousBeacons[ActiveBeaconIndex].Location + PreviousBeacons[ActiveBeaconIndex].BeaconReceiverOffset;
						if (PreviousBeacons[ActiveBeaconIndex].HasBeenPassed) {
							if (Plugin.TrainLocation < triggerLocation) {
								/* Call location based system events here */
								switch (PreviousBeacons[ActiveBeaconIndex].Type) {
									case 20:
										if (PreviousBeacons[ActiveBeaconIndex].Data == -1) {
											/* The actual neutral section in the overhead line */
											Plugin.OverheadSupply.SwitchNeutralState();
										} else if (PreviousBeacons[ActiveBeaconIndex].Data <= 0) {
											/* Handle new APC magnet behaviour which requires two beacons */
											Plugin.Apc.PassedMagnet(triggerLocation);
										}
										break;
								}
								PreviousBeacons[ActiveBeaconIndex].HasBeenPassed = false;
							}
						}
					}
					/* Find the latest beacon which has been passed */
					for (int i = BeaconCount - 1; i >= 0; i--) {
						if (Plugin.TrainLocation < PreviousBeacons[i].Location) {
							/* Cancel this beacon so that the train has to pass over the real beacon again
							 * in the forward direction, in order to re-register it */
							PreviousBeacons[i] = null;
							BeaconCount--;
						} else if (PreviousBeacons[i].HasBeenPassed) {
							ActiveBeaconIndex = i;
							i = 0;
						}
					}
				}
			}
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("");
			}
		}
	}
}
