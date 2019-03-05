using System;
using System.Runtime.InteropServices;

namespace gop
{
    public class PackageConfig
    {
        public OperatingSystem OperatingSystem { get; set; }
        public string OSArchitecture { get; private set; }
        public string ProcessArchitecture { get; private set; }
        public int ProcessorCount { get; set; }
        public DateTimeOffset CreationTime { get; set; }
        public string Sign { get; set; }

        public static PackageConfig Create()
        {
            var result = new PackageConfig
            {
                CreationTime = DateTimeOffset.Now
            };
            try
            {
                result.OperatingSystem = Environment.OSVersion;
                result.OSArchitecture = Enum.GetName(typeof(RuntimeInformation),RuntimeInformation.OSArchitecture);
                result.ProcessArchitecture = Enum.GetName(typeof(RuntimeInformation), RuntimeInformation.ProcessArchitecture);
                result.ProcessorCount = Environment.ProcessorCount;
            }
            catch { }

            return result;
        }
    }
}