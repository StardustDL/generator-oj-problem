using System;

namespace gop.Adapters
{
    public class PackageProfile
    {
        public OperatingSystem OperatingSystem { get; set; }
        public bool Is64BitOperatingSystem { get; set; }
        public bool Is64BitProcess { get; set; }
        public int ProcessorCount { get; set; }
        public DateTimeOffset CreationTime { get; set; }
        public string Sign { get; set; }
        public string Platform { get; set; }
        public bool Checked { get; set; }

        public static PackageProfile Create()
        {
            var result = new PackageProfile
            {
                CreationTime = DateTimeOffset.Now,
            };
            try
            {
                result.OperatingSystem = Environment.OSVersion;
                result.ProcessorCount = Environment.ProcessorCount;
                result.Is64BitOperatingSystem = Environment.Is64BitOperatingSystem;
                result.Is64BitProcess = Environment.Is64BitProcess;
            }
            catch { }

            return result;
        }
    }
}