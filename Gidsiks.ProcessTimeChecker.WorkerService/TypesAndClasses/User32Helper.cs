using System.Runtime.InteropServices;

public static class User32Helper
{
	public enum WinEventHookFlags : uint
	{
		WINEVENT_INCONTEXT = 4,
		WINEVENT_OUTOFCONTEXT = 0,
		WINEVENT_SKIPOWNPROCESS = 2,
		WINEVENT_SKIPOWNTHREAD = 1,
	}
	public enum WinEventConstants : uint
	{
		EVENT_MIN = 0x00000001,
		EVENT_MAX = 0x7FFFFFFF,

		EVENT_SYSTEM_FOREGROUND = 0x0003,
	}

	public struct LASTINPUTINFO
	{
		public uint cbSize;

		public uint dwTime;
	}

	[DllImport("User32.dll")]
	public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);


	[DllImport("user32.dll")]
	public static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	public static extern bool SetForegroundWindow(IntPtr hWnd);


	public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hWnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

	[DllImport("user32.dll")]
	public static extern IntPtr SetWinEventHook(
		WinEventConstants eventMin, WinEventConstants eventMax, 
		IntPtr hmodWinEventProc, 
		WinEventDelegate lpfnWinEventProc, 
		uint idProcess, uint idThread, 
		WinEventHookFlags dwFlags
	);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool UnhookWinEvent(IntPtr eventHook);


	[DllImport("User32.dll")]
	public static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
}