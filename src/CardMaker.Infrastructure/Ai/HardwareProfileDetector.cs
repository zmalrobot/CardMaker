using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CardMaker.AI.Models;
using CardMaker.Application.Ai;
using CardMaker.Contracts.Ai;
using Microsoft.Extensions.Logging;

namespace CardMaker.Infrastructure.Ai;

/// <summary>
/// Rilevatore della memoria fisica totale del sistema con supporto multi-piattaforma nativo per Windows e Linux.
/// </summary>
public sealed partial class HardwareProfileDetector : IHardwareProfileDetector
{
    private readonly ILogger<HardwareProfileDetector>? _logger;

    public HardwareProfileDetector(ILogger<HardwareProfileDetector>? logger = null)
    {
        _logger = logger;
    }

    public long GetTotalPhysicalMemoryBytes()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var memStatus = new MEMORYSTATUSEX
                {
                    dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
                };

                if (GlobalMemoryStatusEx(ref memStatus))
                {
                    return (long)memStatus.ullTotalPhys;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                const string memInfoPath = "/proc/meminfo";
                if (File.Exists(memInfoPath))
                {
                    var lines = File.ReadAllLines(memInfoPath);
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];
                        if (line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase))
                        {
                            var match = Regex.Match(line, @"\d+", RegexOptions.None, TimeSpan.FromSeconds(1));
                            if (match.Success && long.TryParse(match.Value, out var memKb))
                            {
                                return memKb * 1024L;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Impossibile interrogare le statistiche di memoria OS a basso livello: {Message}", ex.Message);
        }

        // Fallback multipiattaforma tramite GC API di .NET
        var gcTotal = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        if (gcTotal > 0)
        {
            return gcTotal;
        }

        // Fallback di sicurezza a 8 GB se non rilevabile
        return 8L * 1024L * 1024L * 1024L;
    }

    public HardwareProfileDto GetHardwareProfile(string modelsDirectory)
    {
        var totalBytes = GetTotalPhysicalMemoryBytes();
        var gb = Math.Round((double)totalBytes / (1024.0 * 1024.0 * 1024.0), 1);
        var recModel = AiModelRegistry.ResolveRecommendedModel(totalBytes);
        var modelPath = Path.Combine(modelsDirectory, recModel.FileName);
        var isDownloaded = File.Exists(modelPath);

        return new HardwareProfileDto(
            TotalPhysicalMemoryBytes: totalBytes,
            TotalPhysicalMemoryGb: gb,
            RecommendedModelKey: recModel.Key,
            RecommendedModelDisplayName: recModel.DisplayName,
            IsModelDownloaded: isDownloaded,
            ModelPath: modelPath);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);
}
