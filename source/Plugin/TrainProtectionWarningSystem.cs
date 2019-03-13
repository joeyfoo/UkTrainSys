using System;
using System.Reflection;
using OpenBveApi.Runtime;

namespace Plugin {

	/// <summary>Represents a Train Protection and Warning System.</summary>
	internal partial class TrainProtectionWarningSystem : ElectricalSystem {
		
		// members
		
		/// <summary>The Train Protection and Warning System brakes-applied-after-emergency-stop timeout period, in milliseconds.</summary>
		internal int BrakesAppliedTimeout;
		/// <summary>The timer which keeps track of the Train Protection and Warning System brakes-applied-after-emergency-stop countdown, in milliseconds.</summary>
		private int BrakesAppliedTimer;
		/// <summary>The Train Protection and Warning System TSS Override timeout period, in milliseconds.</summary>
		internal int TssOverrideTimeout;
		/// <summary>The timer which keeps track of the Train Protection and Warning System TSS Override countdown, in milliseconds.</summary>
		private int TssOverrideTimer;
		/// <summary>The maximum distance in metres, between a pair of Train Protection and Warning System Trainstop Sensor System induction loops, within which the loops can be treated as a valid TSS.</summary>
		private double TssMaximumSpacing;
		/// <summary>Whether or not the Train Protection and Warning System Trainstop Sensor System Normal Direction arming frequency has been detected.</summary>
		private bool TssNdActive;
		/// <summary>The location of the last detected Train Protection and Warning System TSS Normal Direction arming frequency.</summary>
		private double TssNdLastLocation;
		/// <summary>Whether or not the Train Protection and Warning System Trainstop Sensor System Opposite Direction arming frequency has been detected.</summary>
		private bool TssOdActive;
		/// <summary>The location of the last detected Train Protection and Warning System TSS Normal Direction arming frequency.</summary>
		private double TssOdLastLocation;
		/// <summary>The Overspeed sensor timeout period, in milliseconds.</summary>
		internal int OssTimeout;
		/// <summary>The timer which keeps track of the Train Protection and Warning System Overspeed Sensor System Normal Direction countdown, in milliseconds.</summary>
		private int OssTimerNd;
		/// <summary>Whether or not the Train Protection and Warning System Overspeed Sensor System Normal Direction timer, is active.</summary>
		private bool OssTimerNdActive;
		/// <summary>The timer which keeps track of the Train Protection and Warning System Overspeed Sensor System Opposite Direction countdown, in milliseconds.</summary>
		private int OssTimerOd;
		/// <summary>Whether or not the Train Protection and Warning System Overspeed Sensor System Opposite Direction timer, is active.</summary>
		private bool OssTimerOdActive;
		/// <summary>The current Train Protection and Warning System speed limit (in kilometres per hour), as recevied via the last Overspeed Sensor.</summary>
		private double MyLegacyOssLastSpeed;
		/// <summary>The blink rate of the TPWS Brake Demand indicator in milliseconds.</summary>
		internal int IndicatorBlinkRate;
		/// <summary>The timer which keeps track of the blinking TPWS Brake Demand indicator in milliseconds.</summary>
		private int IndicatorBlinkTimer;
		/// <summary>The current safety state of this system.</summary>
		private SafetyStates MySafetyState;
		
		// properties
		
		/// <summary>Gets the current warning state of the Train Protection and Warning System.</summary>
		internal SafetyStates SafetyState {
			get { return this.MySafetyState; }
		}
		
		/// <summary>Gets the current warning state of the Train Protection and Warning System.</summary>
		internal double OssLastSpeed {
			get { return this.MyLegacyOssLastSpeed; }
		}
		
		// constructors
		
		/// <summary>Creates a new instance of this class.</summary>
		/// <remarks>Any default values are assigned in this constructor - they will be used if none are loaded from the configuration file,
		/// and they will determine what values are written into a new configuration file if necessary.</remarks>
		internal TrainProtectionWarningSystem() {
			base.MyOperativeState = OperativeStates.Normal;
			base.MyEnabled = false;
			base.MyBreakerState = SystemBreakerStates.Closed;
			base.RequiredVoltage = 24.0;
			base.RequiredCurrent = 4.0;
			this.BrakesAppliedTimeout = 60000;
			this.BrakesAppliedTimer = this.BrakesAppliedTimeout;
			this.TssOverrideTimeout = 20000;
			this.TssOverrideTimer = this.TssOverrideTimeout;
			this.TssMaximumSpacing = 2;
			this.TssNdActive = false;
			this.TssNdLastLocation = 0;
			this.TssOdActive = false;
			this.TssOdLastLocation = 0;
			this.OssTimeout = 974;
			this.OssTimerNd = 0;
			this.OssTimerNdActive = false;
			this.OssTimerOd = 0;
			this.OssTimerOdActive = false;
			this.MyLegacyOssLastSpeed = 0;
			this.IndicatorBlinkRate = 300;
			this.IndicatorBlinkTimer = 0;
			this.MySafetyState = SafetyStates.None;
		}
		
		// events
		
		/// <summary>Event signalling that an AWS initiated TPWS Brake Demand has been issued.</summary>
		internal event EventHandler TpwsAwsBrakeDemandIssued;
		/// <summary>Event signalling that a TPWS TSS Brake Demand has been issued.</summary>
		internal event EventHandler TpwsTssBrakeDemandIssued;
		/// <summary>Event signalling that the TPWS has been isolated.</summary>
		internal event EventHandler TpwsIsolated;
		/// <summary>Event signalling that the TPWS is no longer isolated.</summary>
		internal event EventHandler TpwsNotIsolated;
		
		// event publishing methods
		
		/// <summary>Publishes an event signalling that an AWS initiated TPWS Brake Demand has been issued.</summary>
		private void OnTpwsAwsBrakeDemandIssued(EventArgs e) {
			EventHandler handler = TpwsAwsBrakeDemandIssued;
			if (handler != null) {
				handler(this, e);
			}
			if (Plugin.DebugMode) {
				Plugin.ReportLogEntry(
					string.Format(
						"{0} {1} {2}  Tpws: [EVENT] - TPWS TSS Brake Demand issued at {3} metres",
						DateTime.Now.TimeOfDay.ToString(),
						this.GetType().Name.ToString(),
						MethodInfo.GetCurrentMethod().ToString(),
						Plugin.TrainLocation.ToString()
					)
				);
			}
		}
		
		/// <summary>Publishes an event signalling that a TPWS TSS Brake Demand has been issued.</summary>
		private void OnTpwsTssBrakeDemandIssued(EventArgs e) {
			EventHandler handler = TpwsTssBrakeDemandIssued;
			if (handler != null) {
				handler(this, e);
			}
			if (Plugin.DebugMode) {
				Plugin.ReportLogEntry(
					string.Format(
						"{0} {1} {2} Tpws: [EVENT] - TPWS TSS Brake Demand issued at {3} metres",
						DateTime.Now.TimeOfDay.ToString(),
						this.GetType().Name.ToString(),
						MethodInfo.GetCurrentMethod().ToString(),
						Plugin.TrainLocation.ToString()
					)
				);
			}
		}
		
		/// <summary>Publishes an event signalling that the TPWS has been temporarily isolated.</summary>
		private void OnTpwsIsolated(EventArgs e) {
			EventHandler handler = TpwsIsolated;
			if (handler != null) {
				handler(this, e);
			}
			if (Plugin.DebugMode) {
				Plugin.ReportLogEntry(
					string.Format(
						"{0} {1} {2} Tpws: [EVENT] - TPWS is isolated at {3} metres",
						DateTime.Now.TimeOfDay.ToString(),
						this.GetType().Name.ToString(),
						MethodInfo.GetCurrentMethod().ToString(),
						Plugin.TrainLocation.ToString()
					)
				);
			}
		}
		
		/// <summary>Publishes an event signalling that the TPWS is no longer isolated.</summary>
		private void OnTpwsNotIsolated(EventArgs e) {
			EventHandler handler = TpwsNotIsolated;
			if (handler != null) {
				handler(this, e);
			}
			if (Plugin.DebugMode) {
				Plugin.ReportLogEntry(
					string.Format(
						"{0} {1} {2} Tpws: [EVENT] - TPWS is no longer isolated at {3} metres",
						DateTime.Now.TimeOfDay.ToString(),
						this.GetType().Name.ToString(),
						MethodInfo.GetCurrentMethod().ToString(),
						Plugin.TrainLocation.ToString()
					)
				);
			}
		}
		
		// event handling methods
		
		/// <summary>This method is called when an Automatic Warning System acknowledgement event occurs.</summary>
		internal void HandleAwsAcknowledgement(Object sender, EventArgs e) {
			if (CabControls.MasterSwitchOn && base.BreakerState == SystemBreakerStates.Closed && base.OperativeState != OperativeStates.Failed &&
			    base.Enabled && base.PowerState == PowerStates.Nominal) {
				/* Only acknowledge the brake demand if it is in effect, or the TPWS is in self-test mode */
				if (this.MySafetyState == SafetyStates.TssBrakeDemand) {
					this.MySafetyState = SafetyStates.BrakeDemandAcknowledged;
				} else if (this.MySafetyState == SafetyStates.SelfTest) {
					this.Reset();
				}
			}
		}
		
		// instance methods
		
		/// <summary>Re-initialises the system to its default state. This will also clear any active warnings, so if calling this method externally,
		/// only do so via the Plugin.Initialize() method.</summary>
		/// <param name="mode">The initialisation mode as set by the host application.</param>
		internal override void Reinitialise(InitializationModes mode) {
			this.MyLegacyOssLastSpeed = 0;
			this.BrakesAppliedTimer = this.BrakesAppliedTimeout;
			this.TssOverrideTimer = this.TssOverrideTimeout;
			this.TssNdActive = false;
			this.TssNdLastLocation = 0;
			this.TssOdActive = false;
			this.TssOdLastLocation = 0;
			this.OssTimerNd = 0;
			this.OssTimerNdActive = false;
			this.OssTimerOd = 0;
			this.OssTimerOdActive = false;
			this.MySafetyState = SafetyStates.None;
		}
		
		/// <summary>Call this method to unconditionally reset the system in order to cancel any warnings. This does not override the behaviour of the Interlock Manager, however.</summary>
		/// <remarks>This method will unconditionally reset the system, regardless of circumstances, so use it carefully. Any conditional reset of the system is handled within
		/// the Update() method instead.</remarks>
		internal override void Reset() {
			/* Unconditionally resets the Train Protection and Warning System, cancelling any warnings which are already in effect */
			this.Reinitialise(InitializationModes.OnService);
			InterlockManager.RequestBrakeReset();
		}
		
		/// <summary>This method should be called if the configuration file indicates that this system is to be disabled for this train.</summary>
		internal override void Disable() {
			/* Unconditionally disables the Vigilance Device, cancelling any warnings which are already in effect */
			this.Reset();
			/* Call the existing virtual implementation as well */
			base.Disable();
		}
		
		/// <summary>Call this method to unconditionally enable the system if it has been disabled.</summary>
		internal override void Enable() {
			/* Use the existing virtual implementation; additional functionality can be added to this override method if required */
			base.Enable();
		}
		
		/// <summary>This should be called once during each Elapse() call, and defines the behaviour of the system.
		/// Power and brake handle positions should be controlled by issuing demands and requests to the Interlock Manager.</summary>
		/// <param name="elapsedTime">The elapsed time since the last call to this method in milliseconds.</param>
		/// <param name="panel">The array of panel variables the plugin initialized in the Load call.</param>
		/// <param name="debugBuilder">A string builder object, to which debug text can be appended.</param>
		internal override void Update(double elapsedTime, int[] panel, System.Text.StringBuilder debugBuilder) {
			/* If disabled, no processing is done */
			if (base.Enabled) {
				/* Set the conditions under which this system's on behaviour will function */
				if (CabControls.MasterSwitchOn && base.BreakerState == SystemBreakerStates.Closed &&
				    base.PowerState == PowerStates.Nominal && base.OperativeState != OperativeStates.Failed) {
					if (this.MySafetyState == SafetyStates.SelfTest) {
						/* The TPWS is in self-test mode, so illuminate the indicators at the right time */
						if (StartupSelfTestManager.SequenceTimer <= 0) {
							panel[PanelIndices.TpwsBrakeDemand] = 1;
							panel[PanelIndices.TpwsIsolation] = 1;
							panel[PanelIndices.TpwsOverride] = 1;
						}
					} else if (this.MySafetyState == SafetyStates.LegacyOssArmed) {
						/* TPWS OSS is enabled with legacy behaviour, so check the train's current speed, and issue a Brake Demand if travelling too fast */
						if (Plugin.TrainSpeed > this.MyLegacyOssLastSpeed) {
							this.IssueBrakeDemand();
							this.MyLegacyOssLastSpeed = 0;
						} else {
							this.MySafetyState = SafetyStates.None;
						}
					} else if (this.MySafetyState == SafetyStates.OssArmed) {
						/* TPWS OSS is armed, so handle the OSS timers */
						if (this.OssTimerNdActive) {
							this.OssTimerNd = this.OssTimerNd + (int)elapsedTime;
						}
						if (this.OssTimerOdActive) {
							this.OssTimerOd = this.OssTimerOd + (int)elapsedTime;
						}
						if (this.OssTimerNd > this.OssTimeout) {
							/* The OSS ND timer has expired and no matching trigger loop has been detected, so the train is travelling within the permitted speed */
							this.OssTimerNd = 0;
							this.OssTimerNdActive = false;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} TPWS: [INFORMATION] - OSS ND timer expired - resetting. Train speed is {3} km/h at location {4} metres",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										Plugin.TrainSpeed.ToString(),
										Plugin.TrainLocation.ToString()
									)
								);
							}
						}
						if (this.OssTimerOd > this.OssTimeout) {
							/* The OSS OD timer has expired and no matching trigger loop has been detected, so the train is travelling within the permitted speed */
							this.OssTimerOd = 0;
							this.OssTimerOdActive = false;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} TPWS: [INFORMATION] - OSS OD timer expired - resetting. Train speed is {3} km/h at location {4} metres",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										Plugin.TrainSpeed.ToString(),
										Plugin.TrainLocation.ToString()
									)
								);
							}
						}
						if (!this.OssTimerNdActive && !this.OssTimerOdActive) {
							/* Disarm the OSS when neither ND or OD timer is active */
							this.MySafetyState = SafetyStates.None;
						}
					} else if (this.MySafetyState == SafetyStates.TssArmed) {
						/* Check whether the maximum allowable distance between a TSS arming and trigger loop has been exceeded, and if so, reset the TSS */
						if (this.TssNdActive) {
							if (Plugin.TrainLocation >= this.TssNdLastLocation + this.TssMaximumSpacing || Plugin.TrainLocation < this.TssNdLastLocation - this.TssMaximumSpacing) {
								/* The TSS ND timer has expired and no matching trigger loop has been detected, so the train has not encountered a valid TSS */
								this.TssNdActive = false;
								this.TssNdLastLocation = 0;
								if (Plugin.DebugMode) {
									Plugin.ReportLogEntry(
										string.Format(
											"{0} {1} {2} TPWS: [INFORMATION] - TSS ND detection range exceeded - resetting. Train speed is {3} km/h at location {4} metres",
											DateTime.Now.TimeOfDay.ToString(),
											this.GetType().Name.ToString(),
											MethodInfo.GetCurrentMethod().ToString(),
											Plugin.TrainSpeed.ToString(),
											Plugin.TrainLocation.ToString()
										)
									);
								}
							}
						}
						if (this.TssOdActive) {
							if (Plugin.TrainLocation >= this.TssOdLastLocation + this.TssMaximumSpacing || Plugin.TrainLocation < this.TssOdLastLocation - this.TssMaximumSpacing) {
								/* The TSS OD timer has expired and no matching trigger loop has been detected, so the train has not encountered a valid TSS */
								this.TssOdActive = false;
								this.TssOdLastLocation = 0;
								if (Plugin.DebugMode) {
									Plugin.ReportLogEntry(
										string.Format(
											"{0} {1} {2} TPWS: [INFORMATION] - TSS OD detection range exceeded - resetting. Train speed is {3} km/h at location {4} metres",
											DateTime.Now.TimeOfDay.ToString(),
											this.GetType().Name.ToString(),
											MethodInfo.GetCurrentMethod().ToString(),
											Plugin.TrainSpeed.ToString(),
											Plugin.TrainLocation.ToString()
										)
									);
								}
							}
						}
						if (!this.TssNdActive && !this.TssOdActive) {
							/* Disarm the TSS when neither ND or OD detection is present */
							this.MySafetyState = SafetyStates.None;
						}
					} else if (this.MySafetyState == SafetyStates.TemporaryOverride) {
						/* The TPWS Temporary Override is active */
						panel[PanelIndices.TpwsOverride] = 1;
						/*  Handle the countdown timer */
						this.TssOverrideTimer = this.TssOverrideTimer - (int)elapsedTime;
						if (this.TssOverrideTimer < 0) {
							this.TssOverrideTimer = this.TssOverrideTimeout;
							this.MySafetyState = SafetyStates.None;
						}
					} else if (this.MySafetyState == SafetyStates.TssBrakeDemand) {
						/* A TPWS Brake Demand has been issued.
						 * Increment the blink timer to enable the Brake Demand indicator to flash */
						this.IndicatorBlinkTimer = this.IndicatorBlinkTimer + (int)elapsedTime;
						if (this.IndicatorBlinkTimer >= 0 && IndicatorBlinkTimer < this.IndicatorBlinkRate) {
							panel[PanelIndices.TpwsBrakeDemand] = 1;
						} else if (this.IndicatorBlinkTimer >= (this.IndicatorBlinkRate * 2)) {
							this.IndicatorBlinkTimer = 0;
						}
					} else if (this.MySafetyState == SafetyStates.BrakeDemandAcknowledged) {
						/* The TPWS Brake Demand indication has been acknowledged by pressing the AWS Reset button,
						 * so stop the blinking light, wait for the train to stop, and start the timer */
						panel[PanelIndices.TpwsBrakeDemand] = 1;
						if (Plugin.TrainSpeed == 0) {
							this.MySafetyState = SafetyStates.BrakesAppliedCountingDown;
						}
					} else if (this.MySafetyState == SafetyStates.BrakesAppliedCountingDown) {
						/* The train has been brought to a stand, so wait for the timeout to expire
						 * before stopping the TPWS safety intervention */
						panel[PanelIndices.TpwsBrakeDemand] = 1;
						InterlockManager.DemandBrakeApplication();
						if (Plugin.Diesel.Enabled) {
							InterlockManager.DemandTractionPowerCutoff();
						}
						/* Handle the countdown timer */
						this.BrakesAppliedTimer = this.BrakesAppliedTimer - (int)elapsedTime;
						if (this.BrakesAppliedTimer < 0) {
							this.BrakesAppliedTimer = this.BrakesAppliedTimeout;
							this.MySafetyState = SafetyStates.None;
							InterlockManager.RequestBrakeReset();
							InterlockManager.RequestTractionPowerReset();
						}
					} else if (this.MySafetyState == SafetyStates.Isolated) {
						/* The TPWS has been isolated */
						panel[PanelIndices.TpwsIsolation] = 1;
					}
				} else if (base.OperativeState == OperativeStates.Failed) {
					/* Increment the blink timer to enable the isolation/fault indicator to flash */
					this.IndicatorBlinkTimer = this.IndicatorBlinkTimer + (int)elapsedTime;
					if (this.IndicatorBlinkTimer >= 0 && IndicatorBlinkTimer < this.IndicatorBlinkRate) {
						panel[PanelIndices.TpwsIsolation] = 1;
					} else if (this.IndicatorBlinkTimer >= (this.IndicatorBlinkRate * 2)) {
						this.IndicatorBlinkTimer = 0;
					}
				}
			}
			
			/* Add any information to display via openBVE's in-game debug interface mode below */
			if (Plugin.DebugMode) {
				debugBuilder.AppendFormat("[Tpws:{0} | OSS NdAct:{1} NdT:{2} OdAct:{3} OdT:{4} | TSS NdAct:{5} NdL:{6} OdAct:{7} OdL:{8}]",
				                          this.MySafetyState.ToString(),
				                          this.OssTimerNdActive.ToString(),
				                          this.OssTimerNd.ToString(),
				                          this.OssTimerOdActive.ToString(),
				                          this.OssTimerOd.ToString(),
				                          this.TssNdActive.ToString(),
				                          this.TssNdLastLocation.ToString(),
				                          this.TssOdActive.ToString(),
				                          this.TssOdLastLocation.ToString());
			}
		}
		
		// other methods
		
		/// <remarks>This method should be called via the SetBeacon() method, upon passing a TPWS OSS arming induction loop.</remarks>
		/// <remarks>This method can also be used to invoke legacy OSS speed limit behaviour.</remarks>
		internal void ArmOss(int frequency) {
			/* First, set the conditions necessary for this method to succeed */
			if (CabControls.MasterSwitchOn && base.BreakerState == SystemBreakerStates.Closed && base.OperativeState != OperativeStates.Failed &&
			    base.Enabled && base.PowerState == PowerStates.Nominal) {
				/* Next check the necessary safety system states before processing - we don't want OSS to be
				 * activated if there's a brake demand already in effect */
				if (this.MySafetyState != SafetyStates.Isolated &&
				    this.MySafetyState != SafetyStates.TssBrakeDemand && this.MySafetyState != SafetyStates.BrakeDemandAcknowledged) {
					/* Process legacy behaviour as well as prototypical arming frequencies */
					if (frequency < 60000) {
						/* Legacy OSS non-timer behaviour */
						if (this.MySafetyState != SafetyStates.OssArmed && this.MySafetyState != SafetyStates.LegacyOssArmed) {
							this.MyLegacyOssLastSpeed = (double)frequency;
							this.MySafetyState = SafetyStates.LegacyOssArmed;
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} TPWS: [INFORMATION] - OSS trigger (legacy mode) - permitted speed is {3} km/h at {4} metres - train speed is {5} km/h",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										frequency.ToString(),
										Plugin.TrainLocation.ToString(),
										Plugin.TrainSpeed.ToString()
									)
								);
							}
						}
					} else if (frequency == 64250 && !this.OssTimerNdActive) {
						/* New prototypical OSS arming behaviour - f1 normal direction frequency */
						this.OssTimerNd = 0;
						this.OssTimerNdActive = true;
						this.MySafetyState = SafetyStates.OssArmed;
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} TPWS: [INFORMATION] - OSS arming frequency f1 detected at location {4} metres - train speed is {3} km/h, OSS ND armed and timer is started",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString(),
									Plugin.TrainSpeed.ToString(),
									Plugin.TrainLocation.ToString()
								)
							);
						}
					} else if (frequency == 64750 && !this.OssTimerOdActive) {
						/* New prototypical OSS arming behaviour - f4 opposite direction frequency */
						this.OssTimerOd = 0;
						this.OssTimerOdActive = true;
						this.MySafetyState = SafetyStates.OssArmed;
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} TPWS: [INFORMATION] - OSS arming frequency f4 detected at location {4} metres - train speed is {3} km/h, OSS OD armed and timer is started",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString(),
									Plugin.TrainSpeed.ToString(),
									Plugin.TrainLocation.ToString()
								)
							);
						}
					}
					
					/* Next, process new prototypical OSS trigger frequencies */
					if (frequency == 65250) {
						/* New prototypical OSS trigger behaviour - f2 normal direction frequency */
						if (this.OssTimerNd > 0 && this.OssTimerNd <= this.OssTimeout) {
							/* The OSS ND timer is still active, so the train is travelling too fast - reset the OSS ND timer and issue an OSS brake demand */
							this.OssTimerNd = 0;
							this.OssTimerNdActive = false;
							this.IssueBrakeDemand();
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} TPWS: [INFORMATION] - OSS trigger frequency f2 detected at location {4} metres - train speed is {3} km/h, OSS ND timeout has not expired - issuing TPWS OSS Brake Demand",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										Plugin.TrainSpeed.ToString(),
										Plugin.TrainLocation.ToString()
									)
								);
							}
						}
					} else if (frequency == 65750) {
						/* New prototypical OSS trigger behaviour - f5 opposite direction frequency */
						if (this.OssTimerOd > 0 && this.OssTimerOd <= this.OssTimeout) {
							/* The OSS OD timer is still active, so the train is travelling too fast - reset the OSS OD timer and issue an OSS brake demand */
							this.OssTimerOd = 0;
							this.OssTimerOdActive = false;
							this.IssueBrakeDemand();
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} TPWS: [INFORMATION] - OSS trigger frequency f5 detected at location {4} metres - train speed is {3} km/h, OSS OD timeout has not expired - issuing TPWS OSS Brake Demand",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										Plugin.TrainSpeed.ToString(),
										Plugin.TrainLocation.ToString()
									)
								);
							}
						}
					}
				}
			}
		}
		
		/// <remarks>This method should be called via the SetBeacon() method, upon passing a TPWS TSS arming induction loop.</remarks>
		/// <remarks>This method can also be used to invoke legacy TSS behaviour.</remarks>
		internal void ArmTss(int frequency, double location) {
			/* First, set the conditions necessary for this method to succeed */
			if (CabControls.MasterSwitchOn && base.BreakerState == SystemBreakerStates.Closed && base.OperativeState != OperativeStates.Failed &&
			    base.Enabled && base.PowerState == PowerStates.Nominal) {
				/* Next check the necessary safety system states before processing - we don't want TSS to be
				 * activated if there's a brake demand already in effect */
				if (this.MySafetyState != SafetyStates.Isolated &&
				    this.MySafetyState != SafetyStates.TssBrakeDemand && this.MySafetyState != SafetyStates.BrakeDemandAcknowledged) {
					if (frequency < 60000) {
						/* Legacy TSS non-timer behaviour */
						this.IssueBrakeDemand();
					} else if (frequency == 66250 && !this.TssNdActive) {
						/* New prototypical TSS arming behaviour - f3 normal direction frequency */
						this.TssNdLastLocation = location;
						this.TssNdActive = true;
						this.MySafetyState = SafetyStates.TssArmed;
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} TPWS: [INFORMATION] - TSS arming frequency f3 detected at location {4} metres - train speed is {3} km/h, TSS ND armed and timer is started",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString(),
									Plugin.TrainSpeed.ToString(),
									Plugin.TrainLocation.ToString()
								)
							);
						}
					} else if (frequency == 66750 && !this.TssOdActive) {
						/* New prototypical TSS arming behaviour - f6 opposite direction frequency */
						this.TssOdLastLocation = location;
						this.TssOdActive = true;
						this.MySafetyState = SafetyStates.TssArmed;
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} TPWS: [INFORMATION] - TSS arming frequency f6 detected at location {4} metres - train speed is {3} km/h, TSS OD armed and timer is started",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString(),
									Plugin.TrainSpeed.ToString(),
									Plugin.TrainLocation.ToString()
								)
							);
						}
					}
					
					/* Next, process new prototypical TSS trigger frequencies */
					if (frequency == 65250) {
						/* New prototypical TSS trigger behaviour - f2 normal direction frequency */
						if (this.TssNdActive && Plugin.TrainLocation <= (this.TssNdLastLocation + this.TssMaximumSpacing)) {
							/* The TSS ND detection is still active, so this is a valid TSS - reset the state and issue a TSS brake demand */
							this.TssNdActive = false;
							this.TssNdLastLocation = 0;
							this.IssueBrakeDemand();
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} TPWS: [INFORMATION] - TSS trigger frequency f2 detected at location {4} metres - TSS ND timer is still active - issuing TPWS TSS Brake Demand",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										Plugin.TrainSpeed.ToString(),
										Plugin.TrainLocation.ToString()
									)
								);
							}
						}
					} else if (frequency == 65750) {
						/* New prototypical TSS trigger behaviour - f5 opposite direction frequency */
						if (this.TssOdActive && Plugin.TrainLocation <= (this.TssOdLastLocation + this.TssMaximumSpacing)) {
							/* The TSS OD detection is still active, so this is a valid TSS - reset the state and issue a TSS brake demand */
							this.TssOdActive = false;
							this.TssNdLastLocation = 0;
							this.IssueBrakeDemand();
							if (Plugin.DebugMode) {
								Plugin.ReportLogEntry(
									string.Format(
										"{0} {1} {2} TPWS: [INFORMATION] - TSS trigger frequency f5 detected at location {4} metres - TSS OD timer is still active - issuing TPWS TSS Brake Demand",
										DateTime.Now.TimeOfDay.ToString(),
										this.GetType().Name.ToString(),
										MethodInfo.GetCurrentMethod().ToString(),
										Plugin.TrainSpeed.ToString(),
										Plugin.TrainLocation.ToString()
									)
								);
							}
						}
					}
				}
			}
		}
		
		/// <summary>Call this method to issue a Train Protection and Warning System Train Stop Sensor (TSS) Brake Demand.</summary>
		/// <remarks>This method should be called via the SetBeacon() method, upon passing a TPWS TSS trigger induction loop. This method can also be called internally.</remarks>
		internal void IssueBrakeDemand() {
			/* First, set the conditions necessary for this method to succeed */
			if (CabControls.MasterSwitchOn && base.BreakerState == SystemBreakerStates.Closed && base.OperativeState != OperativeStates.Failed &&
			    base.Enabled && base.PowerState == PowerStates.Nominal) {
				if (this.MySafetyState == SafetyStates.TemporaryOverride) {
					/* If the TPWS TSS Override timer is active, ignore this brake demand, and instead, reset the TPWS */
					this.Reset();
				} else if (this.MySafetyState != SafetyStates.Isolated) {
					/* Only set a brake demand state, if a brake demand isn't already in effect */
					if (this.MySafetyState != SafetyStates.TssBrakeDemand && this.MySafetyState != SafetyStates.BrakeDemandAcknowledged) {
						/* Reset any remaining active OSS timers or TSS detection states */
						this.Reset();
						/* Issue the brake demand */
						this.MySafetyState = SafetyStates.TssBrakeDemand;
						InterlockManager.DemandBrakeApplication();
						if (Plugin.Diesel.Enabled) {
							InterlockManager.DemandTractionPowerCutoff();
						}
						if (Plugin.DebugMode) {
							Plugin.ReportLogEntry(
								string.Format(
									"{0} {1} {2} TPWS: [INFORMATION] - TSS induction loop - Brake Demand issued at {3} metres",
									DateTime.Now.TimeOfDay.ToString(),
									this.GetType().Name.ToString(),
									MethodInfo.GetCurrentMethod().ToString(),
									Plugin.TrainLocation.ToString()
								)
							);
						}
						/* Raise an event signalling that a TPWS Brake Demand has been made, for event subscribers (such as the AWS). */
						if (Plugin.Aws.SafetyState == AutomaticWarningSystem.SafetyStates.CancelTimerActive ||
						    Plugin.Aws.SafetyState == AutomaticWarningSystem.SafetyStates.CancelTimerExpired) {
							/* AWS initiated this (using this event leads to the AWS warning horn not being suppressed) */
							this.OnTpwsAwsBrakeDemandIssued(new EventArgs());
						} else {
							/* A TPWS sensor intiated this (using this event, leads to the AWS warning horn being suppressed */
							this.OnTpwsTssBrakeDemandIssued(new EventArgs());
						}
						
					}
				}
			}
		}
		
		/// <remarks>This method attempts to isolate the Train Protection and Warning System, if certain prerequisite conditions permit it.</remarks>
		internal void Isolate() {
			/* First, set the conditions necessary for this method to succeed */
			if (CabControls.MasterSwitchOn && base.BreakerState == SystemBreakerStates.Closed && base.OperativeState != OperativeStates.Failed &&
			    base.Enabled && base.PowerState == PowerStates.Nominal) {
				/* If certain prerequisite conditions are met, set the TPWS state to isolated */
				if (StartupSelfTestManager.SequenceState == StartupSelfTestManager.SequenceStates.Initialised &&
				    this.MySafetyState != SafetyStates.TssBrakeDemand && this.MySafetyState != SafetyStates.BrakeDemandAcknowledged
				    && this.MySafetyState != SafetyStates.BrakesAppliedCountingDown && this.MySafetyState != SafetyStates.Isolated) {
					/* Only allow the TPWS Isolation to succeed if other tied-in and enabled safety system states are appropriate */
					bool canIsolate;
					if (Plugin.Aws.Enabled) {
						if (Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.CancelTimerExpired &&
						    Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.TpwsAwsBrakeDemandIssued &&
						    Plugin.Aws.SafetyState != AutomaticWarningSystem.SafetyStates.TpwsTssBrakeDemandIssued) {
							canIsolate = true;
						} else {
							canIsolate = false;
						}
					} else {
						canIsolate = true;
					}
					if (Plugin.Vig.Enabled) {
						if (CabControls.ReverserPosition == CabControls.ReverserStates.Neutral &&
						    Plugin.Vig.SafetyState != VigilanceDevice.SafetyStates.BrakeDemandIssued ||
						    Plugin.Vig.SafetyState != VigilanceDevice.SafetyStates.CancelTimerExpired) {
							canIsolate = true;
						} else {
							canIsolate = false;
						}
					} else {
						canIsolate = true;
					}
					if (canIsolate) {
						this.Reset();
						this.MySafetyState = SafetyStates.Isolated;
						this.OnTpwsIsolated(new EventArgs());
					}
				} else if (this.MySafetyState == SafetyStates.Isolated) {
					/* The TPWS is already isolated, to re-enable it */
					this.Reset();
					this.OnTpwsNotIsolated(new EventArgs());
				}
			}
		}
		
		/// <remarks>This method attempts to activate the TPWS Train Stop Sensor (TSS) Override, if certain prerequisite conditions permit it.</remarks>
		internal void TssTemporaryOverride() {
			/* First, set the conditions necessary for this method to succeed */
			if (CabControls.MasterSwitchOn && base.BreakerState == SystemBreakerStates.Closed && base.OperativeState != OperativeStates.Failed &&
			    base.Enabled && base.PowerState == PowerStates.Nominal) {
				/* If certain prerequisite conditions are met, enable the TPWS TSS Override */
				if (this.MySafetyState != SafetyStates.TssBrakeDemand && this.MySafetyState != SafetyStates.BrakeDemandAcknowledged &&
				    this.MySafetyState != SafetyStates.BrakesAppliedCountingDown && this.MySafetyState != SafetyStates.Isolated &&
				    this.MySafetyState != SafetyStates.TemporaryOverride) {
					this.MySafetyState = SafetyStates.TemporaryOverride;
				} else if (this.MySafetyState == SafetyStates.TemporaryOverride) {
					/* The TPWS TSS Override is already activated, so deactivate it this time */
					this.Reset();
				}
			}
		}
		
		/// <summary>Call this function to put the Train Protection and Warning System into self-test mode. This should only be done via the StartupSelfTestManager.Update() method.</summary>
		internal void SelfTest() {
			this.MySafetyState = SafetyStates.SelfTest;
		}
	}
}
