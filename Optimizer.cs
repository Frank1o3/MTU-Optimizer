using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

public class PluginMain
{
    string SettingsPath;

    public PluginMain(Window window)
    {
        // Dynamically get Plexity folder under LocalAppData
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        SettingsPath = Path.Combine(localAppData, "Plexity", "download", "ClientSettings", "ClientSettings.json");

        var button = (Button)window.FindName("TestButton");
        var hostBox = (TextBox)window.FindName("HostInput");

        if (button != null)
        {
            button.Click += (s, e) =>
            {
                string host = hostBox?.Text ?? "www.roblox.com";

                int bestMTU = FindBestMTU(host);

                if (bestMTU > 0)
                {
                    MessageBox.Show($"Best MTU: {bestMTU}");
                    UpdateClientSettingsMTU(bestMTU);
                }
                else
                {
                    MessageBox.Show("Could not find a working MTU.");
                }
            };
        }
    }

    private int FindBestMTU(string host)
    {
        int bestMTU = 0;
        double bestLatency = double.MaxValue;

        for (int mtu = 576; mtu <= 1500; mtu += 10)
        {
            double latency = PingHost(host, mtu, out bool success);
            if (success)
            {
                if (mtu > bestMTU || (mtu == bestMTU && latency < bestLatency))
                {
                    bestMTU = mtu;
                    bestLatency = latency;
                }
            }
        }
        return bestMTU;
    }

    private double PingHost(string host, int mtu, out bool success)
    {
        success = false;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ping",
                Arguments = $"-n 1 -f -l {mtu - 28} {host}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var proc = System.Diagnostics.Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (output.Contains("Packet needs to be fragmented") || output.Contains("Request timed out"))
                return double.MaxValue;

            var match = System.Text.RegularExpressions.Regex.Match(output, @"time[=<](\d+)ms");
            if (match.Success)
            {
                success = true;
                return double.Parse(match.Groups[1].Value);
            }
        }
        catch
        {
            // ignore
        }
        return double.MaxValue;
    }

    private void UpdateClientSettingsMTU(int mtuValue)
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                MessageBox.Show($"Settings file not found at: {SettingsPath}");
                return;
            }

            string jsonText = File.ReadAllText(SettingsPath);
            var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

            // Deserialize as dictionary
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonText, options);

            // Update or add the MTU override fast flag key
            dict["DFIntConnectionMTUSize"] = JsonDocument.Parse(mtuValue.ToString()).RootElement;

            string newJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(SettingsPath, newJson);

            MessageBox.Show("ClientSettings.json updated with new MTU flag.");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating settings: {ex.Message}");
        }
    }
}
