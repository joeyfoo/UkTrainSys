using System;
using System.Reflection;
using OpenBveApi.Runtime;

namespace Plugin {
	
	/// <summary>Represents a diesel engine.</summary>
	internal partial class DieselEngine : PowerSupply {
		
		// members
		
		/// <summary>The current state of the diesel engine.</summary>
		private DieselEngineStates MyDieselEngineState;
		/// <summary>The current state of the diesel engine starter motor.</summary>
		private StarterMotorStates MyStarterMotorState;
		/// <summary>The delay (in milliseconds) before the starter motor loop sound plays, after pressing the starter button.</summary>
		internal int MotorStartDelay;
		/// <summary>The delay (in milliseconds) before the engine loop sound plays, after the engine has initially started up.</summary>
		internal int EngineStartDelay;
		/// <summary>The delay (in milliseconds) before the engine has completely stopped and can be restarted again.</summary>
		internal int EngineRunDownDelay;
		/// <summary>The delay (in milliseconds) before the engine has completely stopped after stalling, and can be started again.</summary>
		internal int EngineStallDelay;
		/// <summary>A timer used to keep track of the starter motor startup and run down, in milliseconds.</summary>
		private int MyMotorTimer;
		/// <summary>A timer used to keep track of the engine startup and run down, in milliseconds.</summary>
		private int MyEngineTimer;
		/// <summary>Whether or not the engine start button has been released, so another engine start attempt can be made.</summary>
		private bool MyLastStartAttemptTerminated;
		/// <summary>A random number generator for use in probability related tasks.</summary>
		private Random MyRandomGenerator;
		/// <summary>A random number of milliseconds by whihc to delay the successful startup of the engine.</summary>
		private int MyRandomEngineStartDelay;
		/// <summary>The likelyhood that the engine will stall on starting, expressed as a percentage.</summary>
		internal int StallProbability;
		/// <summary>Temporarily stores a random value, used in determining the probability that the engine will stall on starting.</summary>
		private int MyTransientStallProbabilityValue;
		/// <summary>The last reported power position.</summary>
		private int MyLastPowerPosition;
		
		// properties
		
		/// <summary>Gets the current state of the pantograph.</summary>
		internal DieselEngineStates DieselEngineState {
			get { return this.MyDieselEngineState; }
		}
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal DieselEngine() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MaximumOutputVoltage = 750;
			base.MaximumOutputCurrent = 3000;
			base.CurrentLoad = 0;
			base.RemainingCurrentCapacity = base.MaximumOutputCurrent;
			base.MyBreakerState = SystemBreakerStates.Closed;
			this.MyDieselEngineState = DieselEngineStates.Off;
			this.MyStarterMotorState = StarterMotorStates.Off;
			this.MotorStartDelay = 400;
			this.EngineStartDelay = 2500;
			this.EngineRunDownDelay = 2000;
			this.EngineStallDelay = 2000;
			this.MyMotorTimer = 0;
			this.MyEngineTimer = 0;
			this.MyLastStartAttemptTerminated = true;
			this.MyRandomGenerator = new Random();
			this.MyRandomEngineStartDelay = 0;
			this.StallProbability = 25;
			this.MyTransientStallProbabilityValue = 0;
			this.MyLastPowerPosition = 0;
		}
		
		// instance methods
		
		/// <summary>This method should be called if the configuration file indicates that this power supply is to be disabled for this train.</summary>
		internal override void Disable() {
			/* Use the existing virtual implementation; additional functionality can be added to this override method if required */
			base.Disable();
		}
		
		/// <summary>Call this method to unconditionally enable the power supply.</summary>
		internal override void Enable() {
			/* Use the existing virtual implementation; additional functionality can be added below if required */
			base.Enable();
		}
		
		/// <summary>Calculates the amperage being demanded by power supply manager and its connected electrical systems.</summary>
		/// <remarks>If the ampere load exceeds the maximum current rating of the power supply, or the power supply can no longer proved the necessary amperage,
		/// the power supply circuit breaker will trip open.</remarks>
		internal override void CalculateCurrentLoad(double elapsedTime) {
			/* Calculate the current load on this power supply */
			base.CurrentLoad = 0;
			base.RemainingCurrentCapacity = base.MaximumOutputCurrent;
			if (PowerSupplyManager.SelectedPowerSupply == this) {
				if (base.BreakerState == SystemBreakerStates.Closed && base.OperativeState != OperativeStates.Failed) {
					base.CurrentLoad = PowerSupplyManager.RequiredCurrent;
				}
				base.RemainingCurrentCapacity -= base.CurrentLoad;
				if (this.RemainingCurrentCapacity <= 0 && this.PowerState == PowerStates.Nominal) {
					/* There is an overload condition, to trip the breaker and set the power state accordingly */
					base.MyBreakerState = SystemBreakerStates.Open;
					base.MyPowerState = PowerStates.Overloaded;
					SoundManager.Play(SoundIndices.Sparks, 1.0, 1.0, false);
					
					if (Plugin.DebugMode) {
						Plugin.ReportLogEntry(
							string.Format(
								"{0} {1} {2} [CONDITION] - Overload",
								DateTime.Now.TimeOfDay.ToString(),
								this.GetType().Name.ToString(),
								MethodInfo.GetCurrentMethod().ToString()
							)
						);
					}
				}
				
				if (base.CurrentLoad <= base.RemainingCurrentCapacity) {
					/* There is no overload condition, so set the power state accordingly */
					if (base.PowerState != PowerStates.Nominal) {
						base.MyPowerState = PowerStates.Nominal;
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} [CONDITION] Overload cleared",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString()
								)
							);
						}
					}
				}
			}
		}
		
		/// <summary>This should be called once during each Elapse() call.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal override void Update(double elapsedTime, int[] panel, System.Text.StringBuilder debugBuilder) {
			/* If disabled, no processing is done */
			if (base.Enabled) {
				this.CalculateCurrentLoad(elapsedTime);
				
				/* Starter motor behaviour */
				if (this.MyStarterMotorState == StarterMotorStates.Starting) {
					if (this.MyMotorTimer == 0) {
						/* Only play this once */
						SoundManager.Play(SoundIndices.DieselEngineStarterMotorSwitchingOn, 1.0, 1.0, false);
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} Diesel Engine: [INFORMATION] - Starter motor turning on",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString()
								)
							);
						}
					}
					this.MyMotorTimer = this.MyMotorTimer + (int)elapsedTime;
					if (this.MyMotorTimer >= this.MotorStartDelay) {
						/* Reset the timer */
						this.MyMotorTimer = 0;
						this.MyStarterMotorState = StarterMotorStates.Running;
					}
				} else if (this.MyStarterMotorState == StarterMotorStates.Running) {
					if (!SoundManager.IsPlaying(SoundIndices.DieselEngineStarterMotorLoop)) {
						SoundManager.Play(SoundIndices.DieselEngineStarterMotorLoop, 1.0, 1.0, true);
					}
					this.MyMotorTimer = this.MyMotorTimer + (int)elapsedTime;
					if (this.MyMotorTimer >= this.MyRandomEngineStartDelay) {
						/* Reset the timer */
						this.MyMotorTimer = 0;
						this.MyStarterMotorState = StarterMotorStates.RunningDown;
						/* Start te diesel engine */
						this.MyDieselEngineState = DieselEngineStates.Starting;
					}
				} else if (this.MyStarterMotorState == StarterMotorStates.RunningDown) {
					if (this.MyMotorTimer == 0) {
						/* Only play this once */
						SoundManager.Play(SoundIndices.DieselEngineStarterMotorRunningDown, 1.0, 1.0, false);
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} Diesel Engine: [INFORMATION] - Starter motor turning off",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString()
								)
							);
						}
					}
					this.MyMotorTimer = this.MyMotorTimer + (int)elapsedTime;
					if (this.MyMotorTimer >= this.EngineStartDelay) {
						/* Keep the starter motor in a non-off state until the engine starts, for the sake of
						 * emulating the indicator light behaviour in existing add-ons */
						/* Reset the timer */
						this.MyMotorTimer = 0;
						this.MyStarterMotorState = StarterMotorStates.Off;
					} else if (this.MyMotorTimer >= 100) {
						/* Slight delay to overlap the stopping of the starter motor loop sound and starting of the motor run down sound */
						SoundManager.Stop(SoundIndices.DieselEngineStarterMotorLoop);
					}
				} else if (this.MyStarterMotorState == StarterMotorStates.StartupInterrupted) {
					if (this.MyMotorTimer == 0) {
						/* Only play this once */
						if (SoundManager.IsPlaying(SoundIndices.DieselEngineStarterMotorLoop)) {
							SoundManager.Play(SoundIndices.DieselEngineStarterMotorRunningDown, 1.0, 1.0, false);
						}
					}
					this.MyMotorTimer = this.MyMotorTimer + (int)elapsedTime;
					if (this.MyMotorTimer >= 100) {
						/* Slight delay to overlap the stopping of the starter motor loop sound and starting of the motor run down sound */
						SoundManager.Stop(SoundIndices.DieselEngineStarterMotorLoop);
						/* Reset the timer */
						this.MyMotorTimer = 0;
						this.MyStarterMotorState = StarterMotorStates.Off;
					}
				}
				
				/* Diesel engine behaviour */
				if (this.DieselEngineState == DieselEngineStates.InitiateStart) {
					/* Start the starter motor */
					if (this.MyStarterMotorState == StarterMotorStates.Off) {
						this.MyStarterMotorState = StarterMotorStates.Starting;
					}
				} else if (this.DieselEngineState == DieselEngineStates.Starting) {
					if (!SoundManager.IsPlaying(SoundIndices.DieselEngineStart)) {
						SoundManager.Play(SoundIndices.DieselEngineStart, 1.0, 1.0, false);
					}
					this.MyEngineTimer = this.MyEngineTimer + (int)elapsedTime;
					if (this.MyEngineTimer >= this.EngineStartDelay) {
						bool startupSuccessful = false;
						/* Reset the timer */
						this.MyEngineTimer = 0;
						if (this.StallProbability > 0) {
							if (this.MyTransientStallProbabilityValue <= this.StallProbability) {
								this.MyDieselEngineState = DieselEngineStates.Stalled;
								this.MyStarterMotorState = StarterMotorStates.StartupInterrupted;
							} else {
								startupSuccessful = true;
							}
						} else {
							startupSuccessful = true;
						}
						if (startupSuccessful) {
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} Diesel Engine: [INFORMATION] - Startup successful",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString()
									)
								);
							}
							/* Make traction and electrical power available */
							InterlockManager.RequestTractionPowerReset();
							PowerSupplyManager.ConnectPowerSupply(Plugin.Diesel);
							if (base.PowerState == PowerStates.Nominal) {
								CabControls.CabLightsOn = true;
							}
							InterlockManager.RequestTractionPowerReset();
							this.MyDieselEngineState = DieselEngineStates.Running;
						}
					}
				} else if (this.DieselEngineState == DieselEngineStates.Stalled) {
					if (this.MyEngineTimer == 0) {
						/* Only play this once */
						SoundManager.Play(SoundIndices.DieselEngineStallOnStart, 1.0, 1.0, false);
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} Diesel Engine: [INFORMATION] - Startup failed - engine has stalled",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString()
								)
							);
						}
					}
					this.MyEngineTimer = this.MyEngineTimer + (int)elapsedTime;
					if (this.MyEngineTimer >= 500) {
						SoundManager.Stop(SoundIndices.DieselEngineStart);
						if (this.MyEngineTimer >= this.EngineStallDelay) {
							/* Reset the timer */
							this.MyEngineTimer = 0;
							this.MyDieselEngineState = DieselEngineStates.Off;
							CabControls.CabLightsOn = false;
							InterlockManager.DemandTractionPowerCutoff();
						}
					}
				} else if (this.DieselEngineState == DieselEngineStates.Running) {
					if (!SoundManager.IsPlaying(SoundIndices.DieselEngineIdle)) {
						SoundManager.Play(SoundIndices.DieselEngineIdle, 1.0, 1.0, true);
					}
				}
				
				/* Off behaviour should work regardless of power supply or failure modes */
				if (this.DieselEngineState == DieselEngineStates.Stopping) {
					if (this.MyEngineTimer == 0) {
						/* Only play this once */
						SoundManager.Play(SoundIndices.DieselEngineStopping, 1.0, 1.0, false);
					}
					this.MyEngineTimer = this.MyEngineTimer + (int)elapsedTime;
					if (this.MyEngineTimer >= this.EngineRunDownDelay) {
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} Diesel Engine: [INFORMATION] - Shutdown completed",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString()
								)
							);
						}
						/* Reset the timer */
						this.MyEngineTimer = 0;
						this.MyDieselEngineState = DieselEngineStates.Off;
						CabControls.CabLightsOn = false;
						InterlockManager.DemandTractionPowerCutoff();
					} else if (this.MyEngineTimer >= 100) {
						/* Slight delay to overlap the stopping of the engine idle sound and starting of the run down sound */
						SoundManager.Stop(SoundIndices.DieselEngineIdle);
						if (SoundManager.IsPlaying(SoundIndices.DieselEngineStart)) {
							SoundManager.Stop(SoundIndices.DieselEngineStart);
						}
					}
				}
				
				/* Handle the engine start button indications */
				if (this.MyStarterMotorState != StarterMotorStates.Off) {
					if (CabControls.DieselEngineStartButtonDepressed) {
						/* Start button is pressed and starter motor is active */
						panel[PanelIndices.DieselEngineStartButton] = 3;
					} else {
						/* Start button is released and starter motor is active */
						panel[PanelIndices.DieselEngineStartButton] = 2;
					}
				} else {
					if (CabControls.DieselEngineStartButtonDepressed) {
						/* Start button is pressed and starter motor is not active */
						panel[PanelIndices.DieselEngineStartButton] = 1;
					} else {
						/* Start button is released and starter motor is not active */
						panel[PanelIndices.DieselEngineStartButton] = 0;
					}
				}
				
				/* Handle the engine stop button indications */
				if (this.DieselEngineState == DieselEngineStates.Running) {
					if (CabControls.DieselEngineStopButtonDepressed) {
						/* Stop button is pressed and engine is running */
						panel[PanelIndices.DieselEngineStopButton] = 1;
					} else {
						/* Stop button is released and engine is running */
						panel[PanelIndices.DieselEngineStopButton] = 0;
					}
				} else {
					if (CabControls.DieselEngineStopButtonDepressed) {
						/* Stop button is pressed and engine is stopped */
						panel[PanelIndices.DieselEngineStopButton] = 3;
					} else {
						/* Stop button is released and engine is stopped */
						panel[PanelIndices.DieselEngineStopButton] = 2;
					}
				}
			}
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("[D: e:{0} es:{1} sms:{2} sd:{3} sp:{4} tspv:{5} rel:{6}]",
				                          base.Enabled.ToString(),
				                          this.DieselEngineState.ToString(),
				                          this.MyStarterMotorState.ToString(),
				                          this.MyRandomEngineStartDelay.ToString(),
				                          this.StallProbability.ToString(),
				                          this.MyTransientStallProbabilityValue.ToString(),
				                          this.MyLastStartAttemptTerminated.ToString());
			}
		}
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal void Reinitialise(InitializationModes mode) {
			if (base.Enabled) {
				this.MyMotorTimer = 0;
				this.MyEngineTimer = 0;
				this.MyLastPowerPosition = 0;
				if (mode == InitializationModes.OnService) {
					this.MyDieselEngineState = DieselEngineStates.Running;
					this.MyLastStartAttemptTerminated = true;
					PowerSupplyManager.ConnectPowerSupply(Plugin.Diesel);
					if (base.PowerState == PowerStates.Nominal) {
						CabControls.CabLightsOn = true;
					}
				} else if (mode == InitializationModes.OnEmergency) {
					this.MyDieselEngineState = DieselEngineStates.Running;
					this.MyLastStartAttemptTerminated = true;
					PowerSupplyManager.ConnectPowerSupply(Plugin.Diesel);
					if (base.PowerState == PowerStates.Nominal) {
						CabControls.CabLightsOn = true;
					}
				} else if (mode == InitializationModes.OffEmergency) {
					this.MyDieselEngineState = DieselEngineStates.Off;
					this.MyLastStartAttemptTerminated = true;
					PowerSupplyManager.ConnectPowerSupply(Plugin.MainBattery);
					CabControls.CabLightsOn = false;
				}
			}
		}
		
		/// <summary>Starts the diesel engine via the starter motor.</summary>
		internal void Start() {
			if (base.Enabled) {
				/* Only allow the engine to start if the master switch is on, the train is stationary,
				 * the reverser is in neutral, and the power handle is in the off position */
				if (CabControls.MasterSwitchOn && Plugin.TrainSpeed == 0 &&
				    CabControls.ReverserPosition == CabControls.ReverserStates.Neutral && CabControls.PowerPosition == 0) {
					/* Can only start the engine if it is already off... */
					if (this.DieselEngineState == DieselEngineStates.Off) {
						/* Generate a random number of milliseconds by which to delay the engine start */
						this.MyRandomEngineStartDelay = MyRandomGenerator.Next(500, 4000);
						/* Generate a random number between 0 and 100, for use in determining the percentage likelyhood of the engine stalling */
						this.MyTransientStallProbabilityValue = MyRandomGenerator.Next(0, 101);
						/* Only allow an engine start if the starter button has been released, following a previous start attempt */
						if (this.MyLastStartAttemptTerminated) {
							this.MyDieselEngineState = DieselEngineStates.InitiateStart;
							this.MyLastStartAttemptTerminated = false;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} Diesel Engine: [INFORMATION] - Startup initiated",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString()
									)
								);
							}
						}
					}
				}
			}
		}
		
		/// <summary>Interrupts or aborts an engine start, when the starter motor is starting or running, but the engine has not yet started.</summary>
		internal void InterruptStartup() {
			/* Inform the engine class that the last start attempt has been ended by releasing the engine start button */
			this.MyLastStartAttemptTerminated = true;
			
			/* The engine start button was released - check to see if the engine startup sequence has been interrupted */
			if (this.MyStarterMotorState == StarterMotorStates.Starting || this.MyStarterMotorState == StarterMotorStates.Running) {
				/* Stop the engine start up sequence if the starter button is released */
				SoundManager.Stop(SoundIndices.DieselEngineStarterMotorSwitchingOn);
				/* Reset timers */
				this.MyMotorTimer = 0;
				this.MyEngineTimer = 0;
				this.MyStarterMotorState = StarterMotorStates.StartupInterrupted;
				this.MyDieselEngineState = DieselEngineStates.Off;
				InterlockManager.DemandTractionPowerCutoff();
				InterlockManager.DemandBrakeApplication();
				if (Plugin.DebugMode) {
					Plugin.ReportLogEntry(
						string.Format(
							"{0} {1} {2} Diesel Engine: [INFORMATION] - Previous startup attempt concluded",
							DateTime.Now.TimeOfDay.ToString(),
							this.GetType().Name.ToString(),
							MethodInfo.GetCurrentMethod().ToString()
						)
					);
				}
			}
		}
		
		/// <summary>Stops the diesel engine if it is running.</summary>
		internal void Stop() {
			if (base.Enabled) {
				if (this.DieselEngineState == DieselEngineStates.Running) {
					this.MyDieselEngineState = DieselEngineStates.Stopping;
					if (Plugin.DebugMode) {
						Plugin.ReportLogEntry(
							string.Format(
								"{0} {1} {2} Diesel Engine: [INFORMATION] - Shutdown initiated",
								DateTime.Now.TimeOfDay.ToString(),
								this.GetType().Name.ToString(),
								MethodInfo.GetCurrentMethod().ToString()
							)
						);
					}
				}
			}
		}
		
		/// <summary>Changes the engine revs according to the new power notch value.</summary>
		/// <param name="powerNotch">The new power notch value.</param>
		/// <remarks>Currently, this method controls playback of the engine revving up and down sounds.</remarks>
		internal void ChangeRev(int powerNotch) {
			if (powerNotch == 1 && this.MyLastPowerPosition == 0 && !InterlockManager.TractionPowerHeldOff && !InterlockManager.BrakesHeldOn) {
				if (!SoundManager.IsPlaying(SoundIndices.DieselEngineRevvingUp)) {
					SoundManager.Play(SoundIndices.DieselEngineRevvingUp, 1.0, 1.0, false);
					SoundManager.Stop(SoundIndices.DieselEngineRevvingDown);
				}
			} else if (powerNotch == 0 && this.MyLastPowerPosition == 1 && !InterlockManager.TractionPowerHeldOff && !InterlockManager.BrakesHeldOn) {
				if (!SoundManager.IsPlaying(SoundIndices.DieselEngineRevvingDown)) {
					SoundManager.Play(SoundIndices.DieselEngineRevvingDown, 1.0, 1.0, false);
					SoundManager.Stop(SoundIndices.DieselEngineRevvingUp);
				}
			}
			this.MyLastPowerPosition = powerNotch;
		}
		
		/// <summary>Cuts the diesel engine revs to idle.</summary>
		/// <remarks>Currently, this method plays the diesel engine revving down sound, and stops the revving up sound if it is playing.</summary>
		internal void CutRevs() {
			if (CabControls.PowerPosition != 0 && Plugin.TrainSpeed > 0) {
				if (!SoundManager.IsPlaying(SoundIndices.DieselEngineRevvingDown)) {
					SoundManager.Play(SoundIndices.DieselEngineRevvingDown, 1.0, 1.0, false);
					SoundManager.Stop(SoundIndices.DieselEngineRevvingUp);
				}
			}
		}
	}
}
