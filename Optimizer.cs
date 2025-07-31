using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Net.NetworkInformation;
using System.Threading.Tasks;


public class MTUOptimizerLogic
{
    public MTUOptimizerLogic(Window window)
    {
        var button = (Button)((Grid)window.Content).FindName("TestMTUButton");
        if (button != null)
            button.Click += async (s, e) =>
            {
                var result = await FindOptimalMTU();
                MessageBox.Show($"Optimal MTU: {result.MTU}ms\nLatency: {result.Latency}ms");
            };
    }

    private async Task<(int MTU, double Latency)> FindOptimalMTU()
    {
        int[] mtuValues = { 576, 1400, 1500 }; // Common MTU sizes
        string target = "www.roblox.com"; // Test against Roblox server
        double bestLatency = double.MaxValue;
        int optimalMTU = 1500;

        foreach (var mtu in mtuValues)
        {
            try
            {
                double latency = await TestMTU(target, mtu);
                if (latency > 0 && latency < bestLatency)
                {
                    bestLatency = latency;
                    optimalMTU = mtu;
                }
            }
            catch
            {
                continue;
            }
        }

        return (optimalMTU, bestLatency);
    }

    private async Task<double> TestMTU(string host, int mtu)
    {
        using Ping ping = new Ping();
        byte[] buffer = new byte[mtu - 28]; // Subtract IP+ICMP headers (20+8)
        PingOptions options = new PingOptions { DontFragment = true, Ttl = 128 };

        double totalLatency = 0;
        int successfulPings = 0;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                PingReply reply = await ping.SendPingAsync(host, 1000, buffer, options);
                if (reply.Status == IPStatus.Success)
                {
                    totalLatency += reply.RoundtripTime;
                    successfulPings++;
                }
            }
            catch
            {
                continue;
            }
        }

        return successfulPings > 0 ? totalLatency / successfulPings : double.MaxValue;
    }
}
