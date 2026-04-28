using System;
using System.Runtime.InteropServices;


namespace Telemetry
{
    public static class TelemetryNative
    {
        private const string DllName = "TelemetrySystem";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern IntPtr CreateTracker(
            int serializationFormat,
            int persistenceMethod,
            int eventQueuePolicy,
            [MarshalAs(UnmanagedType.LPStr)] string pathEventFile
        );

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void CloseTracker(IntPtr trackerHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyTracker(IntPtr trackerHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TrackEvent(IntPtr trackerHandle, IntPtr eventData);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Flush(IntPtr trackerHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetCurrentEventQueueSize(IntPtr trackerHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateEvent(int numAttributes);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyEvent(IntPtr eventData);
    }

}
