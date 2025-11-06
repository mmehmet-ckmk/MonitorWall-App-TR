using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace MonitorWall.App.Services
{
    /// <summary>
    /// Dahua NetSDK basit sarmalayıcı: Login + RealPlayEx (doğrudan HWND'e render).
    /// PlaySDK gerekmez. Yalnız x64 DLL şarttır (dhnetsdk.dll).
    /// </summary>
    public static class DahuaSdk
    {
        private const string Dll = "dhnetsdk.dll";

        // --- Temel türler ---
        [StructLayout(LayoutKind.Sequential)]
        public struct NET_DEVICEINFO_Ex
        {
            public int nAlarmInPortNum;
            public int nAlarmOutPortNum;
            public int nDiskNum;
            public int nDVRType;
            public int nChanNum;           // kanal sayısı
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] sSerialNumber;   // S/N
        }

        public delegate void fDisConnect(IntPtr lLoginID, string pchDVRIP, int nDVRPort, IntPtr dwUser);
        public delegate void fHaveReConnect(IntPtr lLoginID, string pchDVRIP, int nDVRPort, IntPtr dwUser);

        // --- SDK giriş/çıkış ---
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CLIENT_Init(fDisConnect cbDisConnect, IntPtr dwUser);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern void CLIENT_SetAutoReconnect(fHaveReConnect cbAutoConnect, IntPtr dwUser);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern void CLIENT_Cleanup();

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern uint CLIENT_GetLastError();

        // --- Login (eski, ama yaygın) ---
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern IntPtr CLIENT_LoginEx2(
            string pchDVRIP, ushort wDVRPort, string pchUserName, string pchPassword,
            int nSpecCap, IntPtr pCapParam, out NET_DEVICEINFO_Ex pDeviceInfo, out int error);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CLIENT_Logout(IntPtr lLoginID);

        // --- Önizleme: pencereye doğrudan ---
        // rType: 0 = ana akış (real play by channel)
        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CLIENT_RealPlayEx(IntPtr lLoginID, int nChannelID, IntPtr hWnd, int rType);

        [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
        public static extern bool CLIENT_StopRealPlayEx(IntPtr lPlayHandle);

        // --- Basit oturum sınıfı ---
        public class Session : IDisposable
        {
            public IntPtr LoginId = IntPtr.Zero;
            public NET_DEVICEINFO_Ex Info;
            public readonly List<IntPtr> RealHandles = new();
            private readonly SynchronizationContext _ui;

            public Session()
            {
                _ui = SynchronizationContext.Current ?? new SynchronizationContext();
            }

            public int ChannelCount => Info.nChanNum > 0 ? Info.nChanNum : 16;

            public static Session Login(string ip, int port, string user, string pass)
            {
                if (!CLIENT_Init(null, IntPtr.Zero))
                    throw new InvalidOperationException("CLIENT_Init başarısız. (DLL bulunamadı ya da mimari uyumsuz)");

                CLIENT_SetAutoReconnect((lid, host, p, u) => { /* otomatik reconnect */ }, IntPtr.Zero);

                var ses = new Session();
                int err;
                ses.LoginId = CLIENT_LoginEx2(ip, (ushort)port, user, pass, 0, IntPtr.Zero, out ses.Info, out err);
                if (ses.LoginId == IntPtr.Zero)
                {
                    var code = CLIENT_GetLastError();
                    CLIENT_Cleanup();
                    throw new InvalidOperationException($"Login başarısız. SDK hata: {code} / err:{err}");
                }
                return ses;
            }

            public IntPtr StartPreviewTo(IntPtr hwnd, int channelIndex, int streamType = 0)
            {
                // DİKKAT: Dahua SDK kanal index'i genelde 0-based. Bazı cihazlarda 0/1 denemek gerekebilir.
                var handle = CLIENT_RealPlayEx(LoginId, channelIndex, hwnd, streamType);
                if (handle == IntPtr.Zero)
                {
                    // 0-based başarısızsa 1-based dene
                    handle = CLIENT_RealPlayEx(LoginId, channelIndex + 1, hwnd, streamType);
                    if (handle == IntPtr.Zero)
                    {
                        var code = CLIENT_GetLastError();
                        throw new InvalidOperationException($"RealPlayEx başarısız (ch={channelIndex}). SDK:{code}");
                    }
                }
                RealHandles.Add(handle);
                return handle;
            }

            public void StopAllPreviews()
            {
                foreach (var h in RealHandles)
                {
                    try { if (h != IntPtr.Zero) CLIENT_StopRealPlayEx(h); } catch { }
                }
                RealHandles.Clear();
            }

            public void Dispose()
            {
                StopAllPreviews();
                if (LoginId != IntPtr.Zero)
                {
                    try { CLIENT_Logout(LoginId); } catch { }
                    LoginId = IntPtr.Zero;
                }
                try { CLIENT_Cleanup(); } catch { }
            }
        }
    }
}
