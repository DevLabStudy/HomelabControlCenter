using System;
using System.Management;

namespace HomelabControlCenter
{
    public static class HardwareDiagnosticsEngine
    {
        public static string GenerateAidaReport()
        {
            string report = "=== RAPORT DIAGNOSTYCZNY SYSTEMU (AIDA64 STYLE) ===\n\n";
            int lp = 1;

            try
            {
                report += $"{lp++}. Komputer:\n";
                report += $"    {lp++}. Nazwa komputera: {Environment.MachineName}\n";
                report += $"    {lp++}. Nazwa użytkownika: {Environment.UserName}\n";
                report += $"    {lp++}. System operacyjny: {Environment.OSVersion}\n";
                report += $"    {lp++}. Wersja .NET Runtime: {Environment.Version}\n";

                report += $"{lp++}. Płyta główna & Procesor:\n";
                using (var searcher = new ManagementObjectSearcher("select * from Win32_Processor"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        report += $"    {lp++}. Typ procesora: {obj["Name"]}\n";
                        report += $"    {lp++}. Maksymalna częstotliwość taktowania: {obj["MaxClockSpeed"]} MHz\n";
                        report += $"    {lp++}. Liczba rdzeni: {obj["NumberOfCores"]}\n";
                    }
                }

                using (var searcher = new ManagementObjectSearcher("select * from Win32_BaseBoard"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        report += $"    {lp++}. Nazwa płyty głównej: {obj["Product"]} ({obj["Manufacturer"]})\n";
                        report += $"    {lp++}. Numer seryjny płyty: {obj["SerialNumber"]}\n";
                    }
                }

                report += $"{lp++}. [ BIOS ]:\n";
                using (var searcher = new ManagementObjectSearcher("select * from Win32_BIOS"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        report += $"    {lp++}. Sprzedawca: {obj["Manufacturer"]}\n";
                        report += $"    {lp++}. Wersja BIOS: {obj["SMBIOSBIOSVersion"]}\n";
                        report += $"    {lp++}. Data wydania: {obj["ReleaseDate"]}\n";
                    }
                }

                report += $"{lp++}. Pamięć fizyczna:\n";
                using (var searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
                {
                    long totalRam = 0;
                    foreach (var obj in searcher.Get())
                    {
                        long cap = Convert.ToInt64(obj["Capacity"]);
                        totalRam += cap;
                        report += $"    {lp++}. Kość RAM: {cap / (1024 * 1024 * 1024)} GB, Taktowanie: {obj["Speed"]} MHz\n";
                    }
                    report += $"    {lp++}. Łączny rozmiar pamięci RAM: {totalRam / (1024 * 1024 * 1024)} GB\n";
                }

                report += $"{lp++}. Ekran:\n";
                using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        report += $"    {lp++}. Karta wideo: {obj["Caption"]} (Sterownik: {obj["DriverVersion"]})\n";
                    }
                }

                report += $"{lp++}. Magazyn:\n";
                using (var searcher = new ManagementObjectSearcher("select * from Win32_DiskDrive"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        long size = Convert.ToInt64(obj["Size"]);
                        report += $"    {lp++}. Dysk fizyczny: {obj["Model"]} — {size / (1024 * 1024 * 1024)} GB\n";
                    }
                }

                report += $"{lp++}. Sieć:\n";
                using (var searcher = new ManagementObjectSearcher("select * from Win32_NetworkAdapter where PhysicalAdapter=True"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        report += $"    {lp++}. Karta sieciowa: {obj["Name"]}\n";
                    }
                }
            }
            catch (Exception ex)
            {
                report += $"\n[BŁĄD WMI]: {ex.Message}\n";
            }

            return report;
        }
    }
}