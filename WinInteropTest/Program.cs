using System;
using System.Runtime.InteropServices;

// ReSharper disable InconsistentNaming

namespace WinInteropTest {
	public static class Program {

		[DllImport("kernel32")] static extern IntPtr LoadLibrary(string name);
		[DllImport("kernel32")] static extern void FreeLibrary(IntPtr handle);
		[DllImport("kernel32")] static extern IntPtr GetProcAddress(IntPtr library, string name);

		private static IntPtr LibUser32;
		private static IntPtr LibShCore;

		private delegate IntPtr MonitorFromPointDelegate(Point point, int flags);
		private static MonitorFromPointDelegate MonitorFromPoint;

		private delegate int SetProcessDpiAwarenessDelegate(ProcessDpiAwareness awareness);
		private static SetProcessDpiAwarenessDelegate SetProcessDpiAwareness;
		private delegate int GetScaleFactorForMonitorDelegate(IntPtr monitor, out int factor);
		private static GetScaleFactorForMonitorDelegate GetScaleFactorForMonitor;
		private delegate int GetDpiForMonitorDelegate(IntPtr monitor, MonitorDpiType type, out uint x, out uint y);
		private static GetDpiForMonitorDelegate GetDpiForMonitor;

		private static void LoadLibs() {
			if((LibUser32 = LoadLibrary("user32")) == IntPtr.Zero) Console.WriteLine("User32.dll not found");
			if((LibShCore = LoadLibrary("shcore")) == IntPtr.Zero) Console.WriteLine("ShCore.dll not found");
		}

		private static void FreeLibs() {
			if(LibUser32 != IntPtr.Zero) FreeLibrary(LibUser32);
			if(LibShCore != IntPtr.Zero) FreeLibrary(LibShCore);
		}

		private static void LoadFuncs() {
			LoadFunction(LibUser32, "MonitorFromPoint", out MonitorFromPoint);
			LoadFunction(LibShCore, "SetProcessDpiAwareness", out SetProcessDpiAwareness);
			LoadFunction(LibShCore, "GetScaleFactorForMonitor", out GetScaleFactorForMonitor);
			LoadFunction(LibShCore, "GetDpiForMonitor", out GetDpiForMonitor);
		}

		private static void LoadFunction<T>(IntPtr lib, string name, out T output) where T : class {
			if(lib != IntPtr.Zero) {
				var ptr = GetProcAddress(lib, name);
				if(ptr != IntPtr.Zero) {
					output = Marshal.GetDelegateForFunctionPointer<T>(ptr);
					return;
				}
				Console.WriteLine("Unable to find " + name + "; no entry point with that name");
			}
			else Console.WriteLine("Unable to find " + name + "; library has not been loaded");
			output = null;
		}

		private static void Main() {
			LoadLibs();
			LoadFuncs();

			FindScale();

			FreeLibs();
			Console.ReadLine();
		}

		private static void FindScale() {
			if(MonitorFromPoint == null
				|| SetProcessDpiAwareness == null
				|| GetScaleFactorForMonitor == null
				|| GetDpiForMonitor == null) {
				Console.WriteLine("Missing required function(s)");
				return;
			}

			var monitorHandle = MonitorFromPoint(new Point(), 1);

			Console.WriteLine("Dpi unaware:");

			GetScaleFactorForMonitor(monitorHandle, out int scaleFactor1);
			Console.WriteLine($"Scale:         {scaleFactor1}%");

			GetDpiForMonitor(monitorHandle, MonitorDpiType.MDT_RAW_DPI, out uint rawX1, out uint rawY1);
			Console.WriteLine($"Raw DPI:       {rawX1}|{rawY1}");

			GetDpiForMonitor(monitorHandle, MonitorDpiType.MDT_RAW_DPI, out uint effectiveX1, out uint effectiveY1);
			Console.WriteLine($"Effective DPI: {effectiveX1}|{effectiveY1}");

			GetDpiForMonitor(monitorHandle, MonitorDpiType.MDT_ANGULAR_DPI, out uint angularX1, out uint angularY1);
			Console.WriteLine($"Angular DPI:   {angularX1}|{angularY1}");

			SetProcessDpiAwareness(ProcessDpiAwareness.PROCESS_PER_MONITOR_DPI_AWARE);
			Console.WriteLine("\nDpi aware:");

			GetScaleFactorForMonitor(monitorHandle, out int scaleFactor2);
			Console.WriteLine($"Scale:         {scaleFactor2}%");

			GetDpiForMonitor(monitorHandle, MonitorDpiType.MDT_RAW_DPI, out uint rawX2, out uint rawY2);
			Console.WriteLine($"Raw DPI:       {rawX2}|{rawY2}");

			GetDpiForMonitor(monitorHandle, MonitorDpiType.MDT_RAW_DPI, out uint effectiveX2, out uint effectiveY2);
			Console.WriteLine($"Effective DPI: {effectiveX2}|{effectiveY2}");

			GetDpiForMonitor(monitorHandle, MonitorDpiType.MDT_ANGULAR_DPI, out uint angularX2, out uint angularY2);
			Console.WriteLine($"Angular DPI:   {angularX2}|{angularY2}");

			Console.WriteLine($"\nCalculated scale factor: {100f * effectiveX2 / effectiveX1:F0}%");
		}
	}

	public struct Point {
		public int X, Y;
	}

	public enum MonitorDpiType {
		MDT_EFFECTIVE_DPI = 0,
		MDT_ANGULAR_DPI = 1,
		MDT_RAW_DPI = 2
	}

	public enum ProcessDpiAwareness {
		PROCESS_DPI_UNAWARE,
		PROCESS_SYSTEM_DPI_AWARE,
		PROCESS_PER_MONITOR_DPI_AWARE
	}
}
