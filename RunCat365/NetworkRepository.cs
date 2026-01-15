// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using RunCat365.Properties;
using System.Net.NetworkInformation;

namespace RunCat365
{
    struct NetworkInfo
    {
        internal float SentSpeed { get; set; }
        internal float ReceivedSpeed { get; set; }
    }

    internal static class NetworkInfoExtension
    {
        internal static List<string> GenerateIndicator(this NetworkInfo networkInfo)
        {
            return [
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_Network}:"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Sent}: {FormatSpeed(networkInfo.SentSpeed)}", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Received}: {FormatSpeed(networkInfo.ReceivedSpeed)}", true)
            ];
        }

        private static string FormatSpeed(float speedBytes)
        {
            return ((long)speedBytes).ToByteFormatted() + "/s";
        }
    }

    internal class NetworkRepository
    {
        private readonly NetworkInterface networkInterface;
        private long lastSent;
        private long lastReceived;
        private DateTime lastUpdate;
        private NetworkInfo networkInfo;

        internal NetworkRepository()
        {
            networkInterface = GetActiveNetworkInterface()
                ?? throw new InvalidOperationException("No valid network interface found.");
            var stats = networkInterface.GetIPStatistics();
            lastSent = stats.BytesSent;
            lastReceived = stats.BytesReceived;
            lastUpdate = DateTime.UtcNow;
        }

        private static NetworkInterface? GetActiveNetworkInterface()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            return interfaces.FirstOrDefault(IsValidNetworkInterface);
        }

        private static bool IsValidNetworkInterface(NetworkInterface networkInterface)
        {
            if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                networkInterface.OperationalStatus != OperationalStatus.Up)
                return false;

            var description = networkInterface.Description.ToLower();
            return !description.Contains("vpn") &&
                   !description.Contains("tap") &&
                   !description.Contains("virtual") &&
                   !description.Contains("tun");
        }

        internal void Update()
        {
            var stats = networkInterface.GetIPStatistics();
            var now = DateTime.UtcNow;
            var elapsedSec = (now - lastUpdate).TotalSeconds;
            if (elapsedSec > 0)
            {
                networkInfo.SentSpeed = (float)((stats.BytesSent - lastSent) / elapsedSec);
                networkInfo.ReceivedSpeed = (float)((stats.BytesReceived - lastReceived) / elapsedSec);
            }
            lastSent = stats.BytesSent;
            lastReceived = stats.BytesReceived;
            lastUpdate = now;
        }

        internal NetworkInfo Get()
        {
            Update();
            return networkInfo;
        }
    }
}
