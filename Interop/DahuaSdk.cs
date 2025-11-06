using System;
using System.Runtime.InteropServices;

namespace MonitorWall.App.Interop
{
    internal static class DahuaSdk
    {
        private const string DLL = "dhnetsdk.dll"; // çıktı klasöründe olmalı

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct NET_DEVICEINFO_Ex
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
            public byte[] sSerialNumber;
            public int nAlarmInPortNum;
            public int nAlarmOutPortNum;
            public int nDiskNum;
            public int nDVRType;
            public int nChanNum;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NET_IN_LOGIN_WITH_HIGHLEVEL_SECURITY
        {
            [MarshalAs(UnmanagedType.LPStr)] public string szIP;
            public ushort nPort;
            [MarshalAs(UnmanagedType.LPStr)] public string szUserName;
            [MarshalAs(UnmanagedType.LPStr)] public string szPassword;
            public int emSpecCap; // 0
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NET_OUT_LOGIN_WITH_HIGHLEVEL_SECURITY
        {
            public IntPtr nSessionID;
            public NET_DEVICEINFO_Ex stuDeviceInfo;
            public int nError;
        }

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CLIENT_Init(IntPtr cbDisConnect, IntPtr dwUser);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void CLIENT_Cleanup();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CLIENT_LoginWithHighLevelSecurity(
            ref NET_IN_LOGIN_WITH_HIGHLEVEL_SECURITY pInParam,
            ref NET_OUT_LOGIN_WITH_HIGHLEVEL_SECURITY pOutParam);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern bool CLIENT_Logout(IntPtr lLoginID);
    }
}
