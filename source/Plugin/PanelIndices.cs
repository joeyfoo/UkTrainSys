namespace Plugin {
	/// <summary>Constants for panel indices (panel atsi values).</summary>
	internal static class PanelIndices {
		/// <summary>The Master Switch On (0) indication.</summary>
		internal const int MasterSwitchOff = 0;
		/// <summary>The Master Switch On (1) indication.</summary>
		internal const int MasterSwitchOn = 1;
		
		/// <summary>The diesel engine running indication.</summary>
		internal const int DieselEngineRunning = 3;
		/// <summary>The diesel engine starter push button indicator.</summary>
		internal const int DieselEngineStartButton = 4;
		/// <summary>The diesel engine stop push button indicator.</summary>
		internal const int DieselEngineStopButton = 5;
		/// <summary>The black AWS indication.</summary>
		internal const int AwsBlack = 6;
		/// <summary>The AWS sunflower indication.</summary>
		internal const int AwsSunflower = 7;
		/// <summary>The AWS reset button.</summary>
		internal const int AwsResetButton = 8;
		/// <summary>The TPWS Brake Demand indicator.</summary>
		internal const int TpwsBrakeDemand = 9;
		/// <summary>The TPWS Temporary Isolation/Fault indicator.</summary>
		internal const int TpwsIsolation = 10;
		/// <summary>The TPWS Train Stop Override button.</summary>
		internal const int TpwsOverride = 11;
		
		/// <summary>The DRA switch/indicator.</summary>
		internal const int Dra = 13;
		/// <summary>The Door Interlock and Hazard indicators.</summary>
		internal const int InterlockHazard = 14;
		/// <summary>The left doors indicator.</summary>
		internal const int DoorsLeft = 15;
		/// <summary>The right doors indicator.</summary>
		internal const int DoorsRight = 16;
		
		/// <summary>The headlight rotary switch.</summary>
		internal const int SwitchHeadLight = 20;
		/// <summary>The taillight rotary switch.</summary>
		internal const int SwitchTailLight = 21;
		/// <summary>The headlight proving indicators.</summary>
		internal const int ProvingHeadLight = 22;
		/// <summary>The taillight proving indicators.</summary>
		internal const int ProvingTailLight = 23;
		/// <summary>The horn lever.</summary>
		internal const int HornLever = 24;
		/// <summary>The guard buzzer indicator light.</summary>
		internal const int GuardBuzzerIndicator = 25;
		
		/// <summary>The Pantograph Up indicator.</summary>
		internal const int PantographUp = 30;
		/// <summary>The Line Volts indicator.</summary>
		internal const int LineVolts = 31;
		
		/// <summary>The Vacuum Circuit Breaker indicator.</summary>
		internal const int VacuumCircuitBreaker = 33;
		
		/// <summary>The BR 8x series AC electric locomotive power handle.</summary>
		internal const int AcLocoPowerHandle = 65;
		
		/// <summary>The wiper rotary switch.</summary>
		internal const int WiperSwitch = 198;
		/// <summary>The windscreen wiper.</summary>
		internal const int Wiper = 199;
		
		/// <summary>The AI driver's hands and whether they are visible or not (global setting).</summary>
		internal const int AiDriverHandsVisible = 50;
		/// <summary>The AI driver's hands and whether they are initialised or not (global setting).</summary>
		internal const int AiDriverHandsInitialised = 51;
		/// <summary>Determines which control the AI driver's left hand is interacting with.</summary>
		internal const int AiDriverLeftHandActiveControl = 52;
		/// <summary>Determines which control the AI driver's right hand is interacting with.</summary>
		internal const int AiDriverRightHandActiveControl = 53;
	}
}
