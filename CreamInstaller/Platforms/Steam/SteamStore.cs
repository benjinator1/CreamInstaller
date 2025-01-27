﻿using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if DEBUG
using System;
using CreamInstaller.Forms;
#endif

namespace CreamInstaller.Platforms.Steam;

internal static class SteamStore
{
    private const int CooldownGame = 600;
    private const int CooldownDlc = 1200;

    internal static async Task<List<string>> ParseDlcAppIds(AppData appData)
        => await Task.Run(() =>
        {
            List<string> dlcIds = new();
            if (appData.dlc is null)
                return dlcIds;
            dlcIds.AddRange(from appId in appData.dlc where appId > 0 select appId.ToString(CultureInfo.InvariantCulture));
            return dlcIds;
        });

    internal static async Task<AppData> QueryStoreAPI(string appId, bool isDlc = false, int attempts = 0)
    {
        while (!Program.Canceled)
        {
            attempts++;
            string cacheFile = ProgramData.AppInfoPath + @$"\{appId}.json";
            bool cachedExists = cacheFile.FileExists();
            if (!cachedExists || ProgramData.CheckCooldown(appId, isDlc ? CooldownDlc : CooldownGame))
            {
                string response = await HttpClientManager.EnsureGet($"https://store.steampowered.com/api/appdetails?appids={appId}");
                if (response is not null)
                {
                    IDictionary<string, JToken> apps = (IDictionary<string, JToken>)JsonConvert.DeserializeObject(response);
                    if (apps is not null)
                        foreach (KeyValuePair<string, JToken> app in apps)
                            try
                            {
                                AppDetails appDetails = JsonConvert.DeserializeObject<AppDetails>(app.Value.ToString());
                                if (appDetails is not null)
                                {
                                    AppData data = appDetails.data;
                                    if (!appDetails.success)
                                    {
#if DEBUG
                                        DebugForm.Current.Log(
                                            "Steam store query failed on attempt #" + attempts + " for " + appId + (isDlc ? " (DLC)" : "")
                                          + ": Query unsuccessful (" + app.Value.ToString(Formatting.None) + ")", LogTextBox.Warning);
#endif
                                        if (data is null)
                                            return null;
                                    }
                                    if (data is not null)
                                    {
                                        try
                                        {
                                            cacheFile.WriteFile(JsonConvert.SerializeObject(data, Formatting.Indented));
                                        }
                                        catch
#if DEBUG
                                            (Exception e)
                                        {
                                            DebugForm.Current.Log("Steam store query failed on attempt #" + attempts + " for " + appId + (isDlc ? " (DLC)" : "")
                                                                + ": Unsuccessful serialization (" + e.Message + ")");
                                        }
#else
                                        {
                                            // ignored
                                        }
#endif
                                        return data;
                                    }
#if DEBUG
                                    DebugForm.Current.Log("Steam store query failed on attempt #" + attempts + " for " + appId + (isDlc ? " (DLC)" : "")
                                                        + ": Response data null (" + app.Value.ToString(Formatting.None) + ")");
#endif
                                }
#if DEBUG
                                else
                                    DebugForm.Current.Log("Steam store query failed on attempt #" + attempts + " for " + appId + (isDlc ? " (DLC)" : "")
                                                        + ": Response details null (" + app.Value.ToString(Formatting.None) + ")");
#endif
                            }
                            catch
#if DEBUG
                                (Exception e)
                            {
                                DebugForm.Current.Log("Steam store query failed on attempt #" + attempts + " for " + appId + (isDlc ? " (DLC)" : "")
                                                    + ": Unsuccessful deserialization (" + e.Message + ")");
                            }
#else
                            {
                                // ignored
                            }
#endif
#if DEBUG
                    else
                        DebugForm.Current.Log("Steam store query failed on attempt #" + attempts + " for " + appId + (isDlc ? " (DLC)" : "")
                                            + ": Response deserialization null");
#endif
                }
                else
                {
#if DEBUG
                    DebugForm.Current.Log("Steam store query failed on attempt #" + attempts + " for " + appId + (isDlc ? " (DLC)" : "") + ": Response null",
                        LogTextBox.Warning);
#endif
                }
            }
            if (cachedExists)
                try
                {
                    return JsonConvert.DeserializeObject<AppData>(cacheFile.ReadFile());
                }
                catch
                {
                    cacheFile.DeleteFile();
                }
            if (isDlc)
                break;
            if (attempts > 10)
            {
#if DEBUG
                DebugForm.Current.Log("Failed to query Steam store after 10 tries: " + appId);
#endif
                break;
            }
            Thread.Sleep(1000);
        }
        return null;
    }
}