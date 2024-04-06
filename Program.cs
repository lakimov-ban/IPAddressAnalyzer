using System.Globalization;

namespace IPAddressAnalyzer
{
    internal abstract class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: IPAddressAnalyzer " +
                                  "--file-log <log_file_path> " +
                                  "--file-output <output_file_path> " +
                                  "[--address-start <start_address> " +
                                  "--address-mask <mask> " +
                                  "--time-start <start_date> " +
                                  "--time-end <end_date>]");                 
                return;
            }

            var logFilePath = "";
            var outputFilePath = "";
            var startAddress = "";
            int? mask = null;
            DateTime? startTime = null;
            DateTime? endTime = null;
            
            for (var i = 0; i < args.Length; i += 2)
            {
                switch (args[i])
                {
                    case "--file-log":
                        logFilePath = args[i + 1];
                        break;
                    case "--file-output":
                        outputFilePath = args[i + 1];
                        break;
                    case "--address-start":
                        startAddress = args[i + 1];
                        break;
                    case "--address-mask":
                        mask = int.Parse(args[i + 1]);
                        break;
                    case "--time-start":
                        startTime = DateTime.ParseExact(args[i + 1], "dd.MM.yyyy", CultureInfo.InvariantCulture);
                        break;
                    case "--time-end":
                        endTime = DateTime.ParseExact(args[i + 1], "dd.MM.yyyy", CultureInfo.InvariantCulture);
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {args[i]}");
                        return;
                }
            }

            if (string.IsNullOrEmpty(logFilePath) || string.IsNullOrEmpty(outputFilePath))
            {
                Console.WriteLine("Log file path and output file path are required.");
                return;
            }

            List<string> lines;
            try
            {
                lines = File.ReadAllLines(logFilePath).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error reading log file: {e.Message}");
                return;
            }

            var filteredAddresses = FilterAddresses(lines, startAddress, mask, startTime, endTime);

            var addressCount = CountAddresses(filteredAddresses);

            try
            {
                using (var writer = new StreamWriter(outputFilePath))
                {
                    foreach (var entry in addressCount)
                    {
                        writer.WriteLine($"{entry.Key}: {entry.Value}");
                    }
                }
                Console.WriteLine("Output written to file successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error writing to output file: {e.Message}");
            }
        }

        private static IEnumerable<string> FilterAddresses(IEnumerable<string> lines, string startAddress, int? mask, DateTime? startTime, DateTime? endTime)
        {
            return from line in lines select line.Split(' ') into parts 
                let ipAddress = parts[0] 
                let timestamp = DateTime.ParseExact(parts[1], "dd.MM.yyyyTHH:mm:ss", CultureInfo.InvariantCulture) 
                where IsAddressInRange(ipAddress, startAddress, mask) && IsTimeInRange(timestamp, startTime, endTime) 
                select ipAddress;
        }

        private static bool IsTimeInRange(DateTime timestamp, DateTime? startTime, DateTime? endTime)
        {
            if (startTime.HasValue && timestamp < startTime.Value) return false;
            return !endTime.HasValue || timestamp <= endTime.Value;
        }
        
        private static bool IsAddressInRange(string ipAddress, string startAddress, int? mask)
        {
            if (!string.IsNullOrEmpty(startAddress)) return IsIpAddressAboveMin(ipAddress,startAddress);
            return !mask.HasValue || IsIpAddressBelowOrEqualToUpperBound(ipAddress, mask.Value);
        }

        private static bool IsIpAddressBelowOrEqualToUpperBound(string ipAddress, int subnetMaskDecimal)
        {
            var ipParts = ipAddress.Split('.');
        
            if (ipParts.Length != 4) return false;

            var binaryIp = "";
        
            foreach (var part in ipParts)
            {
                if (!byte.TryParse(part, out var ipPart))
                    return false;

                binaryIp += Convert.ToString(ipPart, 2).PadLeft(8, '0');
            }

            var significantBits = 32 - subnetMaskDecimal;

            for (var i = 31; i >= significantBits; i--)
            {
                if (binaryIp[i] == '1')
                    return false;
            }

            return true;
        }

        private static bool IsIpAddressAboveMin(string ipAddress, string startAddress)
        {
            var ipParts = ipAddress.Split('.');
            var minIpParts = startAddress.Split('.');
        
            if (ipParts.Length != 4 || minIpParts.Length != 4)
                return false;

            for (var i = 0; i < 4; i++)
            {
                if (!byte.TryParse(ipParts[i], out var ipPart) || !byte.TryParse(minIpParts[i], out var minIpPart))
                    return false;

                if (ipPart < minIpPart)
                    return false;
            }

            return true;
        }

        private static Dictionary<string, int> CountAddresses(IEnumerable<string> addresses)
        {
            var addressCount = new Dictionary<string, int>();
            foreach (var address in addresses)
            {
                if (addressCount.ContainsKey(address))
                {
                    addressCount[address]++;
                }
                else
                {
                    addressCount[address] = 1;
                }
            }
            return addressCount;
        }
    }
}