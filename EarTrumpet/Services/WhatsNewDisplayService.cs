﻿using System;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.System;

namespace EarTrumpet.Services
{
    public static class WhatsNewDisplayService
    {
        internal static void ShowIfAppropriate()
        {
            if (App.HasIdentity())
            {
                try
                {
                    var currentVersion = PackageVersionToReadableString(Package.Current.Id.Version);
                    var hasShownFirstRun = false;
                    var lastVersion = Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(currentVersion)];
                    if ((lastVersion != null && currentVersion == (string)lastVersion))
                    {
                        return; 
                    }

                    Windows.Storage.ApplicationData.Current.LocalSettings.Values[nameof(currentVersion)] = currentVersion;

                    Version.TryParse(lastVersion?.ToString(), out var oldVersion);
                    if (oldVersion?.Major == Package.Current.Id.Version.Major && oldVersion?.Minor == Package.Current.Id.Version.Minor)
                    {
                        return;
                    }

                    if (!Windows.Storage.ApplicationData.Current.LocalSettings.Values.ContainsKey(nameof(hasShownFirstRun)))
                    {
                        return;
                    }

                    System.Diagnostics.Process.Start("eartrumpet:");
                    }
                catch
                {
                    // In case Windows Storage APIs are not stable (seen in Dev Dashboard) or Process.Start throws, no need to do anything
                }
            }            
        }

        private static string PackageVersionToReadableString(PackageVersion packageVersion)
        {
            return $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
        }
    }
}
