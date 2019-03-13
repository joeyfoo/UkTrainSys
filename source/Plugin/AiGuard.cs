using System;
using OpenBveApi.Runtime;
using Plugin;

namespace Plugin {

	/// <summary>Represents the AI guard.</summary>
	internal static partial class AiGuard {
		
		/// <summary>The current activity being undertaken by the AI guard.</summary>
		internal static AiGuardStates AiGuardState;
		/// <summary>Whether or not a new stopping point location should be calculated.</summary>
		private static bool CanCalculateNewStoppingLocation = true;
		/// <summary>Set to true, when the guard has already sent a buzzer code, such that the buzzer code isn't repeated until the train has moved and stopped once again.</summary>
		private static bool HasAlreadyBuzzed;
		/// <summary>The location of the stop sign as reported by the appropriate beacon, in metres (this should be the location in terms of route distance).</summary>
		private static int StoppingLocation;
		/// <summary>The permitted distance which the driver may underrun the stop sign, in metres.</summary>
		private static int PermittedUnderrun;
		/// <summary>The permitted distance which the driver may overrun the stop sign, in metres.</summary>
		private static int PermittedOverrun;
		/// <summary>Keeps track of the number of buzzer signals sent by the guard.</summary>
		private static int BuzzerSentCount;
		/// <summary>Keeps track of the number of buzzer signals received from the driver.</summary>
		private static int BuzzerReceivedCount;
		/// <summary>A timer for delaying the guard's buzzer response after the doors have closed.</summary>
		private static int DelayTimer;
		/// <summary>Whether the timer for delaying the guard's buzzer response after the doors have closed is currently active.</summary>
		private static bool DelayTimerActive;
		
		// methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal static void Reinitialise(InitializationModes mode) {
			StoppingLocation = 0;
			BuzzerSentCount = 0;
		}
		
		/// <summary>This should be called once during each Elapse() call, and defines the behaviour of the AI guard.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="speed">The current speed of the train, in kilometres per hour.</param>
		/// <param name="location">The current location of train along the route, in metres.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal static void Update(double elapsedTime, double speed, int[] panel, System.Text.StringBuilder debugBuilder) {
			/* Increment the delay timer if required, or reset it */
			if (DelayTimerActive) {
				DelayTimer = DelayTimer + (int)Plugin.TimeElapsed;
			} else if (DelayTimer != 0) {
				DelayTimer = 0;
			}
			
			if (AiGuardState == AiGuardStates.None) {
				if (DelayTimerActive) {
					DelayTimerActive = false;
				}
			} else if (AiGuardState == AiGuardStates.MonitoringStoppingLocation || AiGuardState == AiGuardStates.DrawForward || AiGuardState == AiGuardStates.SetBack) {
				if ((speed > 5 || speed < -5) && HasAlreadyBuzzed) {
					/* Enable the guard to send a buzzer code once again, but only if train reaches 5 km/h in either direction */
					HasAlreadyBuzzed = false;
				}
				if(speed == 0 && !HasAlreadyBuzzed) {
					/* Only execute when the train is stopped */
					if (Plugin.TrainLocation < (StoppingLocation - PermittedUnderrun) && !HasAlreadyBuzzed) {
						/* The driver has underrun the station stop sign, so play the buzzer sound six times,
						 * and prevent the guard from sending more than one signal to the driver */
						if (BuzzerSentCount < 6) {
							AiGuardState = AiGuardStates.DrawForward;
							if (!SoundManager.IsPlaying(SoundIndices.Buzzer)) {
								SoundManager.Play(SoundIndices.Buzzer, 1.0, 1.0, false);
								BuzzerSentCount++;
							}
						} else {
							BuzzerSentCount = 0;
							HasAlreadyBuzzed = true;
							AiGuardState = AiGuardStates.MonitoringStoppingLocation;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} AiGuard: [INFORMATION] Draw forward @ {1} metres",
										DateTime.Now.TimeOfDay.ToString(),
										Plugin.TrainLocation.ToString()
									)
								);
							}
						}
					} else if (Plugin.TrainLocation > (StoppingLocation + PermittedOverrun) && !HasAlreadyBuzzed) {
						/* The driver has overrun the station stop sign, so play the buzzer sound three times,
						 * and prevent the guard from sending more than one signal to the driver */
						if (BuzzerSentCount < 3) {
							AiGuardState = AiGuardStates.SetBack;
							if (!SoundManager.IsPlaying(SoundIndices.Buzzer)) {
								SoundManager.Play(SoundIndices.Buzzer, 1.0, 1.0, false);
								BuzzerSentCount++;
							}
						} else {
							BuzzerSentCount = 0;
							HasAlreadyBuzzed = true;
							AiGuardState = AiGuardStates.MonitoringStoppingLocation;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} AiGuard: [INFORMATION] Set back @ {1} metres",
										DateTime.Now.TimeOfDay.ToString(),
										Plugin.TrainLocation.ToString()
									)
								);
							}
						}
					} else if (Plugin.TrainLocation > StoppingLocation - PermittedUnderrun && Plugin.TrainLocation < StoppingLocation + PermittedOverrun) {
						/* Train has stopped within the limits, so the guard doesn't monitor any more */
						AiGuardState = AiGuardStates.None;
						StoppingLocation = 0;
						CanCalculateNewStoppingLocation = true;
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} AiGuard: [INFORMATION] Stopped successfully within station limits @ {1} metres",
									DateTime.Now.TimeOfDay.ToString(),
									Plugin.TrainLocation.ToString()
								)
							);
						}
					}
				}
				if (StoppingLocation != 0 && Plugin.TrainLocation > StoppingLocation + 500) {
					/* In case no beacon to indicate the end of the station limits is passed, default to a value of 500m after the stop sign location */
					AiGuardState = AiGuardStates.None;
					CanCalculateNewStoppingLocation = true;
					if (Plugin.DebugMode) {
						Plugin.ReportLogEntry(
							string.Format(
								"{0} AiGuard: [INFORMATION] Departed automatically determined default station limits @ {1} metres",
								DateTime.Now.TimeOfDay.ToString(),
								Plugin.TrainLocation.ToString()
							)
						);
					}
				}
			} else if (AiGuardState == AiGuardStates.ReadyToStart) {
				if (DelayTimer > 2000) {
					if (BuzzerSentCount < 2) {
						if (!SoundManager.IsPlaying(SoundIndices.Buzzer)) {
							SoundManager.Play(SoundIndices.Buzzer, 1.0, 1.0, false);
							BuzzerSentCount++;
						}
					} else {
						BuzzerSentCount = 0;
						AiGuardState = AiGuardStates.AwaitingAcknowledgement;
						DelayTimerActive = false;
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} AiGuard: [INFORMATION] Ready to start buzzer signal issued",
									DateTime.Now.TimeOfDay.ToString()
								)
							);
						}
					}
				}
			} else if (AiGuardState == AiGuardStates.AwaitingAcknowledgement) {
				if (BuzzerReceivedCount >= 2) {
					AiGuardState = AiGuardStates.AcknowledgementReceived;
					BuzzerReceivedCount = 0;
				}
				if (Plugin.TrainSpeed != 0) {
					AiGuardState = AiGuardStates.None;
				}
			} else if (AiGuardState == AiGuardStates.AcknowledgementReceived) {
				/* Keep the AI guard in a non-idle state for a brief time before departure */
				DelayTimerActive = true;
				if (DelayTimer > 2000) {
					AiGuardState = AiGuardStates.None;
				}
			}
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("[AiGuard:{0} stoploc:{1} pu:{2} po:{3}]",
				                          AiGuardState.ToString(),
				                          StoppingLocation.ToString(),
				                          PermittedUnderrun.ToString(),
				                          PermittedOverrun.ToString());
			}
		}
		
		/// <summary>Call this method to update the stopping point parameters.</summary>
		internal static void UpdateStopParameters(int stoppingDistance, int permittedUnderrun, int permittedOverrun, int numberOfCars) {
			if (CanCalculateNewStoppingLocation && (Plugin.TrainSpecifications.Cars <= numberOfCars || numberOfCars == 0)) {
				/* Only update the parameters if no existing parameters are still in effect, or the number of cars specified by the beacon
				 * is less than or matches the number of cars in the train, or if no car number was specified */
				PermittedUnderrun = permittedUnderrun;
				PermittedOverrun = permittedOverrun;
				StoppingLocation = (int)Plugin.TrainLocation + stoppingDistance;
				CanCalculateNewStoppingLocation = false;
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} AiGuard: [INFORMATION] Calculating new stopping location - this will be @ {1} metres - number of cars specified is {2} (train has {3} cars)",
							DateTime.Now.TimeOfDay.ToString(),
							StoppingLocation.ToString(),
							numberOfCars.ToString(),
							Plugin.TrainSpecifications.Cars.ToString()
						)
					);
				}
			}
		}
		
		/// <summary>Call this method to make the AI guard issue a ready to start buzzer signal.</summary>
		internal static void IssueReadyToStart() {
			if (Plugin.TrainSpeed == 0) {
				BuzzerSentCount = 0;
				AiGuardState = AiGuardStates.ReadyToStart;
				DelayTimerActive = true;
			}
		}
		
		/// <summary>Call this method to send a buzz from update the stopping point parameters.</summary>
		internal static void BuzzFromDriver() {
			SoundManager.Play(SoundIndices.Buzzer, 1.0, 1.0, false);
			BuzzerReceivedCount++;
		}
	}
}
