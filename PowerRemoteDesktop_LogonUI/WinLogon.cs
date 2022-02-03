/*******************************************************************************************************
 *  Jean-Pierre LESUEUR (@DarkCoderSc)                                                                 *
 *  https://twitter.com/darkcodersc                                                                    *
 *  https://github.com/DarkCoderSc                                                                     *
 *  www.phrozen.io                                                                                     *
 *  jplesueur@phrozen.io                                                                               *
 *  PHROZEN                                                                                            *
 *                                                                                                     *
 *  License:                                                                                           *
 *      Apache License                                                                                 *
 *      Version 2.0, January 2004                                                                      *
 *      http://www.apache.org/licenses/                                                                *
 *******************************************************************************************************/

using System;
using System.Runtime.InteropServices;

namespace PowerRemoteDesktop_LogonUI
{   

    class WinLogon: IDisposable
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr OpenWindowStation(
            [MarshalAs(UnmanagedType.LPTStr)] string WinStationName,
            [MarshalAs(UnmanagedType.Bool)] bool Inherit,
            uint Access
        );

        [DllImport("user32.dll")]
        public static extern bool CloseWindowStation(
            IntPtr winStation
        );

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr OpenDesktop(
            [MarshalAs(UnmanagedType.LPTStr)] string DesktopName,
            uint Flags,
            bool Inherit,
            uint Access
        );

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CloseDesktop(
            IntPtr hDesktop
        );

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetProcessWindowStation();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessWindowStation(
            IntPtr hWinSta
        );

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetThreadDesktop(
            uint dwThreadId
        );

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetThreadDesktop(
            IntPtr hDesktop
        );            

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetCurrentThreadId();

        ///
        
        private IntPtr _oldThreadDesktop = IntPtr.Zero;
        private IntPtr _oldProcessWinStation = IntPtr.Zero;
        private IntPtr _winSta0Station = IntPtr.Zero;
        private IntPtr _winLogonDesktop = IntPtr.Zero;

        private bool disposed = false;

        private void SwitchToWinLogonDesktop()
        {           
            this._winSta0Station = OpenWindowStation("WinSta0", false, 0x2000000);
            if (this._winSta0Station == IntPtr.Zero)
            {
                throw new WinApiException("OpenWindowStation");
            }

            this._oldProcessWinStation = GetProcessWindowStation();

            if (!SetProcessWindowStation(this._winSta0Station))
            {
                throw new WinApiException("SetProcessWindowStation");
            }

            this._winLogonDesktop = OpenDesktop("winlogon", 0, false, 0x2000000);
            if (this._winLogonDesktop == IntPtr.Zero)
            {
                throw new WinApiException("OpenDesktop");
            }

            this._oldThreadDesktop = GetThreadDesktop(GetCurrentThreadId());

            if (!SetThreadDesktop(this._winLogonDesktop))
            {
                throw new WinApiException("SetThreadDesktop");
            }            
        }

        private void RestoreOriginalDesktop()
        {
            if (this._oldThreadDesktop != IntPtr.Zero)
            {
                SetThreadDesktop(this._oldThreadDesktop);
            }

            if (this._winLogonDesktop != IntPtr.Zero)
            {
                CloseDesktop(this._winLogonDesktop);
            }

            if (this._oldProcessWinStation != IntPtr.Zero)
            {
                SetProcessWindowStation(this._oldProcessWinStation);
            }

            if (this._winSta0Station != IntPtr.Zero)
            {
                CloseWindowStation(this._winSta0Station);
            }
        }

        public bool AttachedToWinLogonDesktop()
        {
            return this._winLogonDesktop != IntPtr.Zero;
        }

        public void Dispose()
        {
            Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.RestoreOriginalDesktop();
                }

                this.disposed = true;
            }
        }

        public WinLogon()
        {           
            this.SwitchToWinLogonDesktop();
        }

        ~WinLogon()
        {
            Dispose(disposing: false);
        }
    }
}
