using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using Microsoft.Win32;

namespace iSpyApplication.Utilities
{
    /// <summary>
    /// Static class containing methods and properties which are useful when 
    /// using libvlc from a .net application.
    /// </summary>
    public static class VlcHelper
    {
        public static readonly Version MinVersion = new Version(3, 0, 0);

        public static bool VLCAvailable
        {
            get
            {
                return !string.IsNullOrEmpty(VLCLocation);
            }
        }
        public static string VLCLocationAutoDetect = null;

        public static string VLCLocation
        {
            get
            {
                var vlcLoc = MainForm.Conf.VLCLocation;
                if (string.IsNullOrEmpty(vlcLoc))
                {
                    vlcLoc = VLCLocationAutoDetect;
                }
                return string.IsNullOrEmpty(vlcLoc)?null:vlcLoc;
            }
        }

        public static void FindVLC()
        {
            try
            {
                using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\VideoLAN\VLC", RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.QueryValues))
                {
                    if (rk == null)
                    {
                        Logger.LogError("VLC is not installed or registry key not found.");
                        VLCLocationAutoDetect = null;
                        return;
                    }

                    object versionValue = rk.GetValue("Version");
                    object installDirValue = rk.GetValue("InstallDir");

                    if (versionValue == null || installDirValue == null)
                    {
                        Logger.LogError("VLC registry entry is incomplete.");
                        VLCLocationAutoDetect = null;
                        return;
                    }

                    var v = Version.Parse(versionValue.ToString());
                    var dir = installDirValue as string;

                    if (v.CompareTo(MinVersion) >= 0)
                    {
                        Logger.LogMessage("Found VLC in " + dir + " (v" + v + ")", "VLCHelper");
                        VLCLocationAutoDetect = dir;
                    }
                    else
                    {
                        Logger.LogError("VLC version unsupported. Please ensure v" + MinVersion + "+ is installed");
                        VLCLocationAutoDetect = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Couldn't find VLC due to an exception: " + ex.Message);
                VLCLocationAutoDetect = null;
            }

            if (VLCLocation != null)
                Logger.LogMessage("Using VLC in " + VLCLocation, "VLCHelper");
        }

    }
}