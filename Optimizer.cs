using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

public class PluginMain
{
    public PluginMain(Window window)
    {
        var button = (Button)window.FindName("TestButton");
        var hostBox = (TextBox)window.FindName("HostInput");

        if (button != null)
        {
            button.Click += (s, e) =>
            {
                string host = hostBox?.Text ?? "www.roblox.com";

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

                if (bestMTU > 0)
                    MessageBox.Show($"Best MTU: {bestMTU}\nLatency: {bestLatency} ms");
                else
                    MessageBox.Show("Could not find a working MTU.");
            };
        }
    }

    private double PingHost(string host, int mtu, out bool success)
    {
        success = false;
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "ping",
                Arguments = $"-n 1 -f -l {mtu - 28} {host}",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            Process proc = Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (
                output.Contains("Packet needs to be fragmented")
                || output.Contains("Request timed out")
            )
                return double.MaxValue;

            Match match = Regex.Match(output, @"time[=<](\d+)ms");
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
}
