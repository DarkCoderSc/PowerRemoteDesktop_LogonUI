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
    class Helper
    {
        /*
         * 
         * Windows API Constants
         * 
         */
        public const uint TOKEN_ALL_ACCESS = 0xF01FF;
        public const uint MAXIMUM_ALLOWED = 0x02000000;

        public const Int16 SW_SHOW = 0x5;
        public const Int16 STARTF_USESHOWWINDOW = 0x1;


        /*
         * 
         * Windows API Enum / Structs
         *         
         */

        public enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct WTS_SESSION_INFO
        {
            public UInt32 SessionID;          
            public string pWinStationName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        /*
         * 
         * Windows API Defs
         * 
         */

        // WTSAPI32.DLL

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSQueryUserToken(UInt32 sessionId, out IntPtr Token);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        static extern bool WTSEnumerateSessions(
            IntPtr hServer,
            UInt32 Reserved,
            UInt32 Version,
            out IntPtr ppSessionInfo,
            out UInt32 pCount
        );

        [DllImport("wtsapi32.dll")]
        static extern void WTSFreeMemory(IntPtr pMemory);

        // ADVAPI32.DLL

        

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            IntPtr lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            IntPtr lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            UInt32 DesiredAccess,
            out IntPtr TokenHandle
        );

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public extern static bool DuplicateTokenEx(
            IntPtr hExistingToken,
            uint dwDesiredAccess,
            IntPtr lpTokenAttributes,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
            TOKEN_TYPE TokenType,
            out IntPtr phNewToken
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        public extern static bool SetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            ref UInt32 TokenInformation,
            UInt32 TokenInformationLength
        );

        // KERNEL32.DLL

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 GetCurrentProcessId();

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern bool ProcessIdToSessionId(UInt32 processID, out UInt32 sessionID);
        
        //---
        
        /// <summary>
        /// Check whether or not current process is sitting in active terminal session.
        /// </summary>
        /// <returns>True if we are already sitting in active terminal session otherwise false.</returns>
        public static bool CheckActiveTerminalSession()
        {
            UInt32 sessionId = 0;

            ProcessIdToSessionId(GetCurrentProcessId(), out sessionId);

            return sessionId == GetActiveTerminalSessionId();            
        }

        /// <summary>
        /// Retrieve the active terminal session Id.
        /// </summary>
        public static UInt32 GetActiveTerminalSessionId()
        {
            IntPtr pSessionArray = IntPtr.Zero;
            UInt32 sessionCount = 0;
            try
            {
                if (!WTSEnumerateSessions(IntPtr.Zero, 0, 1, out pSessionArray, out sessionCount))
                {
                    throw new WinApiException("WTSEnumerateSessions");
                }

                UInt32 activeSessionId = 0;
                bool sessionFound = false;

                // Iterate through each session
                for (var i = 0; i < sessionCount; i++)
                {
                    IntPtr pOffset = (IntPtr)((uint)pSessionArray + (i * Marshal.SizeOf(typeof(WTS_SESSION_INFO))));

                    WTS_SESSION_INFO session = (WTS_SESSION_INFO)Marshal.PtrToStructure(pOffset, typeof(WTS_SESSION_INFO));

                    if (session.State == WTS_CONNECTSTATE_CLASS.WTSActive)
                    {
                        activeSessionId = session.SessionID;

                        sessionFound = true;

                        break;
                    }
                }                

                if (!sessionFound)
                {                    
                    throw new Exception("Could not find active terminal session.");
                }

                return activeSessionId;
            }
            finally
            {
                if (pSessionArray != IntPtr.Zero)
                {
                    WTSFreeMemory(pSessionArray);
                }
            }
        }

        /// <summary>
        /// Spawn a new instance of current application in the active terminal session id.
        /// </summary>
        public static void RespawnInActiveTerminalSession()
        {
            IntPtr token = IntPtr.Zero;
            IntPtr newToken = IntPtr.Zero;
            try
            {
                UInt32 sessionId = GetActiveTerminalSessionId();
                               
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ALL_ACCESS, out token))
                {
                    throw new WinApiException("OpenProcessToken");
                }

                if (!DuplicateTokenEx(token, MAXIMUM_ALLOWED, IntPtr.Zero, SECURITY_IMPERSONATION_LEVEL.SecurityIdentification, TOKEN_TYPE.TokenPrimary, out newToken))
                {
                    throw new WinApiException("DuplicateTokenEx");
                }

                if (!SetTokenInformation(newToken, TOKEN_INFORMATION_CLASS.TokenSessionId, ref sessionId, (UInt32)Marshal.SizeOf(sessionId)))
                {
                    throw new WinApiException("SetTokenInformation");
                }

                STARTUPINFO startupInfo = new STARTUPINFO();
                startupInfo.cb = Marshal.SizeOf(startupInfo);
                startupInfo.dwFlags = STARTF_USESHOWWINDOW;
                startupInfo.wShowWindow = SW_SHOW;

                PROCESS_INFORMATION processInformation;

                if (!CreateProcessAsUser(
                    newToken,
                    System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    ref startupInfo,
                    out processInformation
                ))
                {
                    throw new WinApiException("CreateProcessAsUser");
                }
            }
            finally
            {
                if (token != IntPtr.Zero)
                {
                    CloseHandle(token);
                }

                if (newToken != IntPtr.Zero)
                {
                    CloseHandle(newToken);
                }
            }
        }
    }
}
