using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gidsiks.ProcessTimeChecker.InterfaceContractLibrary.Types
{
	public enum EventType
	{
		ProcessStarted	= 00,
		ProcessStopped	= 01,
		WindowEntered	= 10,
		WindowLeaved	= 11,
		IdleStarted		= 20,
		IdleStopped		= 21,
		ActivityStarted = 22,
		ActivityStopped = 23,
	}

	public class EventData
	{
	}
}
