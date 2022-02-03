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
using System.IO;
using System.Drawing;
using System.IO.Pipes;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace PowerRemoteDesktop_LogonUI
{
    class DesktopThread
    {

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetForegroundWindow();

        static Bitmap DesktopSnapshot()
        {
            Screen primaryScreen = Screen.PrimaryScreen;

            Bitmap bitmap = new Bitmap(
                primaryScreen.Bounds.Width,
                primaryScreen.Bounds.Height
            );

            Point location = new Point(
                primaryScreen.Bounds.Location.X,
                primaryScreen.Bounds.Location.Y
            );

            Graphics graphics = Graphics.FromImage(bitmap);

            graphics.CopyFromScreen(location, Point.Empty, bitmap.Size);

            return bitmap;
        }

        public static void process()
        {
            using (WinLogon winLogon = new WinLogon())
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("PowerRemoteDesktop_LogonUI", PipeDirection.Out))
                {                    
                    StreamWriter writer = new StreamWriter(pipeServer);

                    while (true)
                    {
                      
                        try
                        {
                            pipeServer.WaitForConnection();

                            writer.AutoFlush = true;
                           
                            if (GetForegroundWindow() != IntPtr.Zero)
                            {
                                writer.WriteLine("STREAM");
                                ///

                                Bitmap bitmap = DesktopSnapshot();
                                if (bitmap != null)
                                {
                                    System.IO.MemoryStream memoryStream = new MemoryStream();
                                    try
                                    {
                                        bitmap.Save(memoryStream, ImageFormat.Jpeg);

                                        if (memoryStream.Length > 0)
                                        {
                                            writer.WriteLine(Convert.ToBase64String(memoryStream.ToArray()));
                                        }
                                    }
                                    finally
                                    {
                                        bitmap.Dispose();

                                        if (memoryStream != null)
                                        {
                                            memoryStream.Dispose();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                writer.WriteLine("NULL");
                            }

                            if (pipeServer.IsConnected)
                            {
                                pipeServer.Disconnect();
                            }
                        }
                        catch (Exception ex)
                        {
                            break;
                        }
                    }
                }
            }

            return;
        }

    }
}
