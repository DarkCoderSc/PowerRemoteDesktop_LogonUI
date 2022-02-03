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
    [Serializable]
    class WinApiException : Exception
    {
        public WinApiException(string winApiName): base(String.Format("WinAPI Error -> \"{0}\" with last error: {1}", winApiName, Marshal.GetLastWin32Error().ToString()))
        {
                        
        }
    }
}
