﻿using CreamInstaller.Components;
using CreamInstaller.Utility;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreamInstaller.Resources;

internal static class ScreamAPI
{
    internal static void GetScreamApiComponents(
            this string directory,
            out string api32, out string api32_o,
            out string api64, out string api64_o,
            out string config
        )
    {
        api32 = directory + @"\EOSSDK-Win32-Shipping.dll";
        api32_o = directory + @"\EOSSDK-Win32-Shipping_o.dll";
        api64 = directory + @"\EOSSDK-Win64-Shipping.dll";
        api64_o = directory + @"\EOSSDK-Win64-Shipping_o.dll";
        config = directory + @"\ScreamAPI.json";
    }

    internal static void WriteConfig(StreamWriter writer, SortedList<string, (DlcType type, string name, string icon)> overrideCatalogItems, SortedList<string, (DlcType type, string name, string icon)> entitlements, InstallForm installForm = null)
    {
        writer.WriteLine("{");
        writer.WriteLine("  \"version\": 2,");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"eos_logging\": false,");
        writer.WriteLine("  \"block_metrics\": false,");
        writer.WriteLine("  \"catalog_items\": {");
        writer.WriteLine("    \"unlock_all\": true,");
        if (overrideCatalogItems.Any())
        {
            writer.WriteLine("    \"override\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastOverrideCatalogItem = overrideCatalogItems.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in overrideCatalogItems)
            {
                string id = pair.Key;
                (_, string name, _) = pair.Value;
                writer.WriteLine($"      \"{id}\"{(pair.Equals(lastOverrideCatalogItem) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added override catalog item to ScreamAPI.json with id {id} ({name})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("    ]");
        }
        else
            writer.WriteLine("    \"override\": []");
        writer.WriteLine("  },");
        writer.WriteLine("  \"entitlements\": {");
        writer.WriteLine("    \"unlock_all\": true,");
        writer.WriteLine("    \"auto_inject\": true,");
        if (entitlements.Any())
        {
            writer.WriteLine("    \"inject\": [");
            KeyValuePair<string, (DlcType type, string name, string icon)> lastEntitlement = entitlements.Last();
            foreach (KeyValuePair<string, (DlcType type, string name, string icon)> pair in entitlements)
            {
                string id = pair.Key;
                (_, string name, _) = pair.Value;
                writer.WriteLine($"      \"{id}\"{(pair.Equals(lastEntitlement) ? "" : ",")}");
                if (installForm is not null)
                    installForm.UpdateUser($"Added entitlement to ScreamAPI.json with id {id} ({name})", InstallationLog.Action, info: false);
            }
            writer.WriteLine("    ]");
        }
        else
            writer.WriteLine("    \"inject\": []");
        writer.WriteLine("  }");
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteConfig = true) => await Task.Run(() =>
    {
        directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
        if (File.Exists(api32_o))
        {
            if (File.Exists(api32))
            {
                File.Delete(api32);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted ScreamAPI: {Path.GetFileName(api32)}", InstallationLog.Action, info: false);
            }
            File.Move(api32_o, api32);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Epic Online Services: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(api64_o))
        {
            if (File.Exists(api64))
            {
                File.Delete(api64);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted ScreamAPI: {Path.GetFileName(api64)}", InstallationLog.Action, info: false);
            }
            File.Move(api64_o, api64);
            if (installForm is not null)
                installForm.UpdateUser($"Restored Epic Online Services: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}", InstallationLog.Action, info: false);
        }
        if (deleteConfig && File.Exists(config))
        {
            File.Delete(config);
            if (installForm is not null)
                installForm.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
        }
    });

    internal static async Task Install(string directory, ProgramSelection selection, InstallForm installForm = null, bool generateConfig = true) => await Task.Run(() =>
    {
        directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o, out string config);
        if (File.Exists(api32) && !File.Exists(api32_o))
        {
            File.Move(api32, api32_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Epic Online Services: {Path.GetFileName(api32)} -> {Path.GetFileName(api32_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(api32_o))
        {
            Properties.Resources.EpicOnlineServices32.Write(api32);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(api32)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(api64) && !File.Exists(api64_o))
        {
            File.Move(api64, api64_o);
            if (installForm is not null)
                installForm.UpdateUser($"Renamed Epic Online Services: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}", InstallationLog.Action, info: false);
        }
        if (File.Exists(api64_o))
        {
            Properties.Resources.EpicOnlineServices64.Write(api64);
            if (installForm is not null)
                installForm.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(api64)}", InstallationLog.Action, info: false);
        }
        if (generateConfig)
        {
            IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> overrideCatalogItems = selection.AllDlc.Where(pair => pair.Value.type is DlcType.EpicCatalogItem).Except(selection.SelectedDlc);
            foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> extraDlc) in selection.ExtraSelectedDlc)
                overrideCatalogItems = overrideCatalogItems.Except(extraDlc);
            IEnumerable<KeyValuePair<string, (DlcType type, string name, string icon)>> entitlements = selection.SelectedDlc.Where(pair => pair.Value.type == DlcType.EpicEntitlement);
            foreach ((string id, string name, SortedList<string, (DlcType type, string name, string icon)> _dlc) in selection.ExtraSelectedDlc)
                entitlements = entitlements.Concat(_dlc.Where(pair => pair.Value.type == DlcType.EpicEntitlement));
            if (overrideCatalogItems.Any() || entitlements.Any())
            {
                if (installForm is not null)
                    installForm.UpdateUser("Generating ScreamAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", InstallationLog.Operation);
                File.Create(config).Close();
                StreamWriter writer = new(config, true, Encoding.UTF8);
                WriteConfig(writer,
                    new(overrideCatalogItems.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String),
                    new(entitlements.ToDictionary(pair => pair.Key, pair => pair.Value), PlatformIdComparer.String),
                    installForm);
                writer.Flush();
                writer.Close();
            }
            else if (File.Exists(config))
            {
                File.Delete(config);
                if (installForm is not null)
                    installForm.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", InstallationLog.Action, info: false);
            }
        }
    });
}