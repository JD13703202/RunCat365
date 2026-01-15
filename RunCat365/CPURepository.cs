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
using System.Diagnostics;
using System.Security.Principal;
using LHM = LibreHardwareMonitor.Hardware;

namespace RunCat365
{
    struct CPUInfo
    {
        internal float Total { get; set; }
        internal float User { get; set; }
        internal float Kernel { get; set; }
        internal float Idle { get; set; }
        internal float Temperature { get; set; }
    }

    internal static class CPUInfoExtension
    {
        internal static string GetDescription(this CPUInfo cpuInfo)
        {
            if (cpuInfo.Temperature <= 0)
                return $"{Strings.SystemInfo_CPU}: {cpuInfo.Total:f1}%";
            return $"{Strings.SystemInfo_CPU}: {cpuInfo.Total:f1}% | Temperature: {cpuInfo.Temperature:f1}°C";
        }

        internal static List<string> GenerateIndicator(this CPUInfo cpuInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_CPU}: {cpuInfo.Total:f1}%"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_User}: {cpuInfo.User:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Kernel}: {cpuInfo.Kernel:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Available}: {cpuInfo.Idle:f1}%", true)
            };

            if (cpuInfo.Temperature > 0)
                resultLines.Add(TreeFormatter.CreateNode($"Temperature: {cpuInfo.Temperature:f1}°C", true));

            return resultLines;
        }
    }

    internal class CPURepository
    {
        private readonly PerformanceCounter totalCounter;
        private readonly PerformanceCounter userCounter;
        private readonly PerformanceCounter kernelCounter;
        private readonly PerformanceCounter idleCounter;
        private readonly List<CPUInfo> cpuInfoList = [];
        private const int CPU_INFO_LIST_LIMIT_SIZE = 5;
        private const int TEMPERATURE_RETRY_COUNT = 3;
        private const int TEMPERATURE_RETRY_DELAY_MS = 50;
        private static float lastValidTemperature = 0;

        public static readonly LHM.Computer Computer = new LHM.Computer
        {
            IsCpuEnabled = true,
            IsMotherboardEnabled = true,
            IsGpuEnabled = true
        };

        static CPURepository()
        {
            Computer.Open();
        }

        internal CPURepository()
        {
            totalCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            userCounter = new PerformanceCounter("Processor", "% User Time", "_Total");
            kernelCounter = new PerformanceCounter("Processor", "% Privileged Time", "_Total");
            idleCounter = new PerformanceCounter("Processor", "% Idle Time", "_Total");

            _ = totalCounter.NextValue();
            _ = userCounter.NextValue();
            _ = kernelCounter.NextValue();
            _ = idleCounter.NextValue();
        }

        internal void Update()
        {
            var total = Math.Min(100, totalCounter.NextValue());
            var user = Math.Min(100, userCounter.NextValue());
            var kernel = Math.Min(100, kernelCounter.NextValue());
            var idle = Math.Min(100, idleCounter.NextValue());

            var temperature = GetCPUTemperature();

            var cpuInfo = new CPUInfo
            {
                Total = total,
                User = user,
                Kernel = kernel,
                Idle = idle,
                Temperature = temperature,
            };

            cpuInfoList.Add(cpuInfo);
            if (CPU_INFO_LIST_LIMIT_SIZE < cpuInfoList.Count)
            {
                cpuInfoList.RemoveAt(0);
            }
        }

        internal CPUInfo Get()
        {
            if (cpuInfoList.Count == 0) return new CPUInfo();

            return new CPUInfo
            {
                Total = cpuInfoList.Average(x => x.Total),
                User = cpuInfoList.Average(x => x.User),
                Kernel = cpuInfoList.Average(x => x.Kernel),
                Idle = cpuInfoList.Average(x => x.Idle),
                Temperature = cpuInfoList.Average(x => x.Temperature)
            };
        }

        internal void Close()
        {
            totalCounter.Close();
            userCounter.Close();
            kernelCounter.Close();
            idleCounter.Close();
        }

        public static float GetCPUTemperature()
        {
            if (!IsRunningAsAdministrator())
                return 0;

            if (Computer == null)
                return 0;

            for (int retry = 0; retry < TEMPERATURE_RETRY_COUNT; retry++)
            {
                foreach (var hardware in Computer.Hardware)
                {
                    if (hardware.HardwareType != LHM.HardwareType.Cpu)
                        continue;

                    hardware.Update();
                    foreach (var sensor in hardware.Sensors)
                    {
                        if (sensor.SensorType == LHM.SensorType.Temperature && sensor.Value.HasValue)
                        {
                            lastValidTemperature = sensor.Value.Value;
                            return lastValidTemperature;
                        }
                    }
                }

                if (retry < TEMPERATURE_RETRY_COUNT - 1)
                    Thread.Sleep(TEMPERATURE_RETRY_DELAY_MS);
            }

            return lastValidTemperature;
        }

        private static bool IsRunningAsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
