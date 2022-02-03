/*
 *  Jean-Pierre LESUEUR (@DarkCoderSc)
 *  https://twitter.com/darkcodersc
 *  https://github.com/DarkCoderSc
 *  www.phrozen.io
 *  jplesueur@phrozen.io
 *  PHROZEN
 *  
 *  License:
 *      Apache License
 *      Version 2.0, January 2004
 *      http://www.apache.org/licenses/
 *      
 *  Description:
 *      (Work In Progress) Plugin to capture WinLogon Desktop and control WinLogon Mouse / Keyboard.
 *      This Plugin is expected to be used by PowerRemoteDesktop.
 *      
 *      https://github.com/DarkCoderSc/PowerRemoteDesktop
 */

using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace PowerRemoteDesktop_LogonUI
{
    class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetProcessDPIAware();

        static void Main(string[] args)
        {            
            // Check if application is run as System user.
            string currentUserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

            if (currentUserName != "NT AUTHORITY\\SYSTEM")
            {
                throw new Exception("This application must be run as user \"NT AUTHORITY\\SYSTEM\".");
            }

            if (!Helper.CheckActiveTerminalSession())
            {
                Helper.RespawnInActiveTerminalSession();

                return;
            }

            ///
            SetProcessDPIAware();
            /// 

            // Protect from multiple instance
            using (Mutex mutex = new Mutex(false, "Global\\CDB9F615-962B-4B45-A9CD-1A7D7CA40920"))
            {
                if (mutex.WaitOne(0, false))
                {
                    // Create Workers                   
                    Thread desktopThread = new Thread(DesktopThread.process);
                    desktopThread.Start();

                    do
                    {
                        desktopThread.Join(1000);
                    } while (Helper.CheckActiveTerminalSession());

                    if (desktopThread.IsAlive)
                    {
                        desktopThread.Abort();
                    }
                }                
            }

            // If we are not in active terminal session, respawn in correct session
            if (!Helper.CheckActiveTerminalSession())
            {
                Helper.RespawnInActiveTerminalSession();

                return;
            }
        }
    }
}