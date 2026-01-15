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
using LHM = LibreHardwareMonitor.Hardware;

namespace RunCat365
{
    struct GPUInfo
    {
        internal float Load { get; set; }
        internal float Temperature { get; set; }
    }

    internal static class GPUInfoExtension
    {
        internal static List<string> GenerateIndicator(this GPUInfo gpuInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_GPU}:"),
                TreeFormatter.CreateNode($"Usage: {gpuInfo.Load:f1}%", false)
            };

            if (gpuInfo.Temperature > 0)
            {
                resultLines.Add(TreeFormatter.CreateNode($"Temperature: {gpuInfo.Temperature:f1}°C", true));
            }

            return resultLines;
        }
    }

    internal class GPURepository
    {
        private readonly List<GPUInfo> gpuInfoList = [];
        private const int GPU_INFO_LIST_LIMIT_SIZE = 5;
        private const int GPU_RETRY_COUNT = 3;
        private const int GPU_RETRY_DELAY_MS = 50;
        private static float lastValidLoad = 0;
        private static float lastValidTemperature = 0;

        internal bool IsAvailable { get; private set; } = true;

        internal GPURepository()
        {
            IsAvailable = HasGPUHardware();
        }

        private static bool IsGPUHardwareType(LHM.HardwareType hardwareType)
        {
            return hardwareType == LHM.HardwareType.GpuNvidia ||
                   hardwareType == LHM.HardwareType.GpuAmd ||
                   hardwareType == LHM.HardwareType.GpuIntel;
        }

        private static bool HasGPUHardware()
        {
            if (CPURepository.Computer == null)
                return false;

            return CPURepository.Computer.Hardware.Any(h => IsGPUHardwareType(h.HardwareType));
        }

        internal void Update()
        {
            if (!IsAvailable) return;

            var (load, temperature) = GetGPULoadAndTemperature();

            var gpuInfo = new GPUInfo
            {
                Load = load,
                Temperature = temperature
            };

            gpuInfoList.Add(gpuInfo);
            if (GPU_INFO_LIST_LIMIT_SIZE < gpuInfoList.Count)
            {
                gpuInfoList.RemoveAt(0);
            }
        }

        internal GPUInfo? Get()
        {
            if (!IsAvailable || gpuInfoList.Count == 0) return null;

            var latestGpuInfo = gpuInfoList[^1];
            return new GPUInfo
            {
                Load = latestGpuInfo.Load,
                Temperature = latestGpuInfo.Temperature
            };
        }

        private static (float load, float temperature) GetGPULoadAndTemperature()
        {
            if (CPURepository.Computer == null)
                return (lastValidLoad, lastValidTemperature);

            for (int retry = 0; retry < GPU_RETRY_COUNT; retry++)
            {
                foreach (var hardware in CPURepository.Computer.Hardware)
                {
                    if (!IsGPUHardwareType(hardware.HardwareType))
                        continue;

                    try
                    {
                        hardware.Update();
                    }
                    catch
                    {
                        continue;
                    }

                    float? load = null;
                    float? coreTemperature = null;
                    float? hotSpotTemperature = null;

                    foreach (var sensor in hardware.Sensors)
                    {
                        if (!sensor.Value.HasValue)
                            continue;

                        if (sensor.SensorType == LHM.SensorType.Load)
                        {
                            if (sensor.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase) ||
                                sensor.Name.Equals("D3D 3D", StringComparison.OrdinalIgnoreCase) ||
                                sensor.Name.StartsWith("GPU", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!load.HasValue || sensor.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase))
                                    load = sensor.Value.Value;
                            }
                        }
                        else if (sensor.SensorType == LHM.SensorType.Temperature)
                        {
                            if (sensor.Name.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase) ||
                                sensor.Name.Contains("Hotspot", StringComparison.OrdinalIgnoreCase))
                            {
                                hotSpotTemperature = sensor.Value.Value;
                            }
                            else if (sensor.Name.Equals("GPU Core", StringComparison.OrdinalIgnoreCase))
                            {
                                coreTemperature = sensor.Value.Value;
                            }
                        }
                    }

                    var temperature = hotSpotTemperature ?? coreTemperature;

                    if (load.HasValue)
                    {
                        lastValidLoad = Math.Min(100, load.Value);
                        if (temperature.HasValue)
                            lastValidTemperature = temperature.Value;
                        return (lastValidLoad, lastValidTemperature);
                    }
                }

                if (retry < GPU_RETRY_COUNT - 1)
                    Thread.Sleep(GPU_RETRY_DELAY_MS);
            }

            return (lastValidLoad, lastValidTemperature);
        }

        internal void Close()
        {
        }
    }
}
