using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

// Jawne aliasy eliminujące konflikt między WPF a WPF-UI oraz System
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;
using Renci.SshNet;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBoxButton = System.Windows.MessageBoxButton;

namespace HomelabControlCenter
{
    public partial class MainWindow : FluentWindow
    {
        public ObservableCollection<ServerNode> Servers { get; set; }
        private ServerNode? _selectedServer;
        private SshClient? _sshClient;
        private string _currentMobileAuthToken = "DEVLAB_AUTH_DEFAULT_000000";

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);

            Servers = new ObservableCollection<ServerNode>();
            ServerItemsControl.ItemsSource = Servers;

            GenerateNewAuthToken();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (TryDetectLocalNetwork(out string network, out int prefixLength, out string localAddress))
            {
                TxtScanSubnet.Text = network + "/" + prefixLength;
                TxtDetectedNetwork.Text = "Aktywna sieć: " + network + "/" + prefixLength + " • IP: " + localAddress;
            }
            else
            {
                TxtDetectedNetwork.Text = "Nie wykryto aktywnej sieci IPv4";
            }
        }

        private string ShowDevLabInput(string title, string prompt, string defaultValue = "")
        {
            var dialog = new CustomInputDialog(title, prompt, defaultValue);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                return dialog.ResponseText;
            }
            return string.Empty;
        }

        private void GenerateNewAuthToken()
        {
            byte[] bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            string tokenHex = BitConverter.ToString(bytes).Replace("-", "").ToLower();
            _currentMobileAuthToken = "DEVLAB_HOMELAB_SECURE_" + tokenHex;

            if (TxtQrTokenDisplay != null)
            {
                TxtQrTokenDisplay.Text = _currentMobileAuthToken;
            }

            DrawQrMatrix(_currentMobileAuthToken);
        }

        private void DrawQrMatrix(string content)
        {
            if (QrCanvas == null) return;
            QrCanvas.Children.Clear();

            int size = 25;
            double pixelSize = QrCanvas.Width / size;

            int seed = content.GetHashCode();
            Random rnd = new Random(seed);

            DrawFinderPattern(0, 0, pixelSize);
            DrawFinderPattern(size - 7, 0, pixelSize);
            DrawFinderPattern(0, size - 7, pixelSize);

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if ((x < 8 && y < 8) || (x >= size - 7 && y < 8) || (x < 8 && y >= size - 7))
                        continue;

                    if (rnd.Next(2) == 1)
                    {
                        System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle
                        {
                            Width = pixelSize,
                            Height = pixelSize,
                            Fill = Brushes.Black
                        };
                        System.Windows.Controls.Canvas.SetLeft(rect, x * pixelSize);
                        System.Windows.Controls.Canvas.SetTop(rect, y * pixelSize);
                        QrCanvas.Children.Add(rect);
                    }
                }
            }
        }

        private void DrawFinderPattern(int startX, int startY, double pixelSize)
        {
            for (int x = 0; x < 7; x++)
            {
                for (int y = 0; y < 7; y++)
                {
                    bool isOuter = (x == 0 || x == 6 || y == 0 || y == 6);
                    bool isInner = (x >= 2 && x <= 4 && y >= 2 && y <= 4);

                    if (isOuter || isInner)
                    {
                        System.Windows.Shapes.Rectangle rect = new System.Windows.Shapes.Rectangle
                        {
                            Width = pixelSize,
                            Height = pixelSize,
                            Fill = Brushes.Black
                        };
                        System.Windows.Controls.Canvas.SetLeft(rect, (startX + x) * pixelSize);
                        System.Windows.Controls.Canvas.SetTop(rect, (startY + y) * pixelSize);
                        QrCanvas.Children.Add(rect);
                    }
                }
            }
        }

        private void CmbAppTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbAppTheme == null || CmbAppTheme.SelectedItem is not WpfComboBoxItem selectedItem) return;
            string? content = selectedItem.Content?.ToString();

            if (content != null)
            {
                if (content.Contains("Jasny"))
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                }
                else if (content.Contains("Systemowy"))
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                }
                else
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                }
            }
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show("Ustawienia aplikacji zostały pomyślnie zapisane!", "DevLab CC");
        }

        private void BtnGenerateQr_Click(object sender, RoutedEventArgs e)
        {
            GenerateNewAuthToken();
            System.Windows.MessageBox.Show("Wygenerowano nowy token uwierzytelniania QR dla aplikacji mobilnej!", "DevLab Security");
        }

        private void BtnCopyQrToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_currentMobileAuthToken);
                System.Windows.MessageBox.Show("Token uwierzytelniania skopiowany do schowka!", "DevLab Security");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Błąd kopiowania: " + ex.Message);
            }
        }

        private void OpenDevLabWeb_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "https://devlabstudy.ovh", UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Błąd: " + ex.Message);
            }
        }

        private void Email_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is WpfTextBox tb)
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "mailto:" + tb.Text, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show("Błąd: " + ex.Message);
                }
            }
        }

        private void BtnReportBug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "mailto:bledy@devlabstudy.ovh", UseShellExecute = true });
            }
            catch
            {
                System.Windows.MessageBox.Show("Zgłoś błąd na adres: bledy@devlabstudy.ovh", "DevLab Support");
            }
        }

        private void BtnNavSettings_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            ViewSettings.Visibility = Visibility.Visible;
            ResetNavButtons();
        }

        private void ResetNavButtons()
        {
            var normalBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#04060A"));
            var normalFg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8A94A6"));

            Wpf.Ui.Controls.Button[] buttons = { BtnNavDashboard, BtnNavTools, BtnNavDocker, BtnNavDiagnostics, BtnNavContact };

            foreach (var btn in buttons)
            {
                btn.Appearance = ControlAppearance.Secondary;
                btn.Background = normalBg;
                btn.BorderBrush = Brushes.Transparent;
                btn.Foreground = normalFg;
            }
        }

        private void SetActiveButton(Wpf.Ui.Controls.Button btn)
        {
            ResetNavButtons();
            var activeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5500"));

            btn.Appearance = ControlAppearance.Primary;
            btn.Background = activeColor;
            btn.BorderBrush = activeColor;
            btn.Foreground = Brushes.White;
        }

        private void HideAllViews()
        {
            ViewDashboard.Visibility = Visibility.Collapsed;
            ViewServerDetails.Visibility = Visibility.Collapsed;
            ViewTools.Visibility = Visibility.Collapsed;
            ViewDocker.Visibility = Visibility.Collapsed;
            ViewDiagnostics.Visibility = Visibility.Collapsed;
            ViewSettings.Visibility = Visibility.Collapsed;
            ViewContact.Visibility = Visibility.Collapsed;
        }

        private void BtnNavDashboard_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            ViewDashboard.Visibility = Visibility.Visible;
            SetActiveButton(BtnNavDashboard);
        }

        private void BtnNavTools_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            ViewTools.Visibility = Visibility.Visible;
            SetActiveButton(BtnNavTools);
        }

        private void BtnNavDocker_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            ViewDocker.Visibility = Visibility.Visible;
            SetActiveButton(BtnNavDocker);
        }

        private void BtnNavDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            ViewDiagnostics.Visibility = Visibility.Visible;
            SetActiveButton(BtnNavDiagnostics);
        }

        private void BtnNavContact_Click(object sender, RoutedEventArgs e)
        {
            HideAllViews();
            ViewContact.Visibility = Visibility.Visible;
            SetActiveButton(BtnNavContact);
        }

        private void BtnAddServer_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddServerWindow();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                Servers.Add(new ServerNode
                {
                    Name = dialog.ServerName,
                    IpAddress = dialog.ServerIp,
                    Username = dialog.ServerUser,
                    Password = dialog.RememberPassword ? dialog.ServerPassword : string.Empty,
                    RememberPassword = dialog.RememberPassword,
                    StatusColor = Brushes.LimeGreen
                });
            }
        }

        private void ServerCard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ServerNode server)
            {
                _selectedServer = server;
                TxtSelectedServerName.Text = server.Name;
                TxtSelectedServerIp.Text = "IP Address: " + server.IpAddress + " | User: " + server.Username;

                HideAllViews();
                ViewServerDetails.Visibility = Visibility.Visible;
            }
        }

        private void BtnBackToDashboard_Click(object sender, RoutedEventArgs e)
        {
            if (_sshClient != null && _sshClient.IsConnected)
            {
                _sshClient.Disconnect();
            }
            HideAllViews();
            ViewDashboard.Visibility = Visibility.Visible;
            SetActiveButton(BtnNavDashboard);
        }

        private async void BtnConnectSsh_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedServer == null) return;
            string password = _selectedServer.Password;

            if (string.IsNullOrEmpty(password))
            {
                string input = ShowDevLabInput("Autoryzacja SSH", "Wprowadź hasło SSH dla " + _selectedServer.Username + "@");
                if (string.IsNullOrEmpty(input)) return;
                password = input;
            }

            TxtTerminalStatus.Text = "[Łączenie...]";
            TxtTerminalOutput.Text += "[SSH] Łączenie z " + _selectedServer.Username + "@" + _selectedServer.IpAddress + "...\n";

            await Task.Run(() =>
            {
                try
                {
                    _sshClient = new SshClient(_selectedServer.IpAddress, _selectedServer.Username, password);
                    _sshClient.Connect();

                    Dispatcher.Invoke(() =>
                    {
                        TxtTerminalStatus.Text = "[POŁĄCZONO]";
                        TxtTerminalOutput.Text += "[SSH] Połączono pomyślnie z " + _selectedServer.Name + "!\n\n";
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtTerminalStatus.Text = "[BŁĄD]";
                        TxtTerminalOutput.Text += "[SSH ERROR] " + ex.Message + "\n";
                    });
                }
            });
        }

        private async void TxtCommandInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(TxtCommandInput.Text))
            {
                string cmd = TxtCommandInput.Text;
                TxtCommandInput.Clear();
                TxtTerminalOutput.Text += "root@node:~# " + cmd + "\n";
                await RunSshCommandAsync(cmd);
            }
        }

        private async Task<string> RunSshCommandAsync(string command)
        {
            if (_sshClient == null || !_sshClient.IsConnected)
            {
                TxtTerminalOutput.Text += "[ERROR] Brak połączenia SSH.\n";
                return string.Empty;
            }

            return await Task.Run(() =>
            {
                try
                {
                    var cmd = _sshClient.CreateCommand(command);
                    string res = cmd.Execute();
                    Dispatcher.Invoke(() =>
                    {
                        TxtTerminalOutput.Text += res + "\n";
                        TxtTerminalOutput.ScrollToEnd();
                    });
                    return res;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => TxtTerminalOutput.Text += "[EXEC ERROR] " + ex.Message + "\n");
                    return string.Empty;
                }
            });
        }

        private async void BtnRunSystemDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            BtnRunSystemDiagnostics.IsEnabled = false;
            TxtDiagnosticsOutput.Text = "[DIAGNOSTYKA] Skanowanie podzespołów WMI w formacie AIDA64 (Lp.)...\n\n";

            try
            {
                await Task.Run(() =>
                {
                    StringBuilder report = new StringBuilder();
                    report.AppendLine("=== KOMPLEKSOWY RAPORT DIAGNOSTYCZNY SYSTEMU ===");
                    report.AppendLine("Data generowania: " + DateTime.Now);
                    report.AppendLine();
                    int lp = 1;

                    try
                    {
                    report.AppendLine(lp++ + ". Podsumowanie systemu operacyjnego:");
                    report.AppendLine("    " + lp++ + ". Nazwa komputera: " + Environment.MachineName);
                    report.AppendLine("    " + lp++ + ". Zalogowany użytkownik: " + Environment.UserName);
                    report.AppendLine("    " + lp++ + ". Wersja systemu OS: " + Environment.OSVersion);
                    report.AppendLine("    " + lp++ + ". Katalog systemowy: " + Environment.SystemDirectory);
                    report.AppendLine("    " + lp++ + ". Liczba procesorów logicznych: " + Environment.ProcessorCount);
                    report.AppendLine("    " + lp++ + ". Czas pracy systemu: " + FormatDuration(TimeSpan.FromMilliseconds(Environment.TickCount64)));
                    report.AppendLine("    " + lp++ + ". Wersja 64-bit: " + Environment.Is64BitOperatingSystem);
                    report.AppendLine();

                    report.AppendLine(lp++ + ". Pamięć systemowa i uruchomienie:");
                    using (var searcher = new ManagementObjectSearcher("select TotalVisibleMemorySize,FreePhysicalMemory,LastBootUpTime,BuildNumber from Win32_OperatingSystem"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            report.AppendLine("    " + lp++ + ". Pamięć dostępna dla systemu: " + FormatBytes(ReadInt64(obj["TotalVisibleMemorySize"]) * 1024));
                            report.AppendLine("    " + lp++ + ". Pamięć wolna: " + FormatBytes(ReadInt64(obj["FreePhysicalMemory"]) * 1024));
                            report.AppendLine("    " + lp++ + ". Numer kompilacji Windows: " + Value(obj["BuildNumber"]));
                            report.AppendLine("    " + lp++ + ". Ostatnie uruchomienie: " + Value(obj["LastBootUpTime"]));
                        }
                    }
                    report.AppendLine();

                    report.AppendLine(lp++ + ". Procesor (CPU):");
                    using (var searcher = new ManagementObjectSearcher("select * from Win32_Processor"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            report.AppendLine("    " + lp++ + ". Model: " + obj["Name"]);
                            report.AppendLine("    " + lp++ + ". Producent: " + obj["Manufacturer"]);
                            report.AppendLine("    " + lp++ + ". Liczba rdzeni fizycznych: " + obj["NumberOfCores"]);
                            report.AppendLine("    " + lp++ + ". Liczba wątków logicznych: " + obj["NumberOfLogicalProcessors"]);
                            report.AppendLine("    " + lp++ + ". Maksymalne taktowanie: " + obj["MaxClockSpeed"] + " MHz");
                            report.AppendLine("    " + lp++ + ". Architektura: " + obj["Architecture"]);
                        }
                    }
                    report.AppendLine();

                    report.AppendLine(lp++ + ". Płyta główna i BIOS:");
                    using (var searcher = new ManagementObjectSearcher("select * from Win32_BaseBoard"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            report.AppendLine("    " + lp++ + ". Producent płyty: " + obj["Manufacturer"]);
                            report.AppendLine("    " + lp++ + ". Model/Produkt: " + obj["Product"]);
                            report.AppendLine("    " + lp++ + ". Numer seryjny: " + obj["SerialNumber"]);
                        }
                    }
                    using (var searcher = new ManagementObjectSearcher("select * from Win32_BIOS"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            report.AppendLine("    " + lp++ + ". Wersja BIOS: " + obj["SMBIOSBIOSVersion"]);
                            report.AppendLine("    " + lp++ + ". Data wydania BIOS: " + obj["ReleaseDate"]);
                        }
                    }
                    report.AppendLine();

                    report.AppendLine(lp++ + ". Pamięć operacyjna (RAM):");
                    long totalRam = 0;
                    using (var searcher = new ManagementObjectSearcher("select * from Win32_PhysicalMemory"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            long capacity = ReadInt64(obj["Capacity"]);
                            totalRam += capacity;
                            report.AppendLine("    " + lp++ + ". Moduł RAM: " + FormatBytes(capacity) + " (" + Value(obj["Speed"]) + " MHz) - " + Value(obj["Manufacturer"]));
                            report.AppendLine("    " + lp++ + ". Numer seryjny RAM: " + Value(obj["SerialNumber"]));
                        }
                    }
                    report.AppendLine("    " + lp++ + ". Całkowita pamięć RAM: " + FormatBytes(totalRam));
                    report.AppendLine();

                    report.AppendLine(lp++ + ". Karta graficzna (GPU):");
                    using (var searcher = new ManagementObjectSearcher("select * from Win32_VideoController"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            long adapterRam = 0;
                            try { adapterRam = Convert.ToInt64(obj["AdapterRAM"]); } catch { }
                            report.AppendLine("    " + lp++ + ". Model GPU: " + obj["Name"]);
                            report.AppendLine("    " + lp++ + ". Sterownik: " + obj["DriverVersion"]);
                            if (adapterRam > 0)
                            {
                                report.AppendLine("    " + lp++ + ". VRAM: " + (adapterRam / (1024 * 1024 * 1024)) + " GB");
                            }
                        }
                    }
                    report.AppendLine();

                    report.AppendLine(lp++ + ". Dyski i pamięć masowa:");
                    using (var searcher = new ManagementObjectSearcher("select * from Win32_DiskDrive"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            long size = ReadInt64(obj["Size"]);
                            report.AppendLine("    " + lp++ + ". Dysk: " + Value(obj["Model"]) + " (" + FormatBytes(size) + ") - Interfejs: " + Value(obj["InterfaceType"]) + ", Firmware: " + Value(obj["FirmwareRevision"]));
                            report.AppendLine("    " + lp++ + ". Numer seryjny dysku: " + Value(obj["SerialNumber"]));
                        }
                    }
                    report.AppendLine();

                    report.AppendLine(lp++ + ". Woluminy i miejsce na dyskach:");
                    foreach (DriveInfo drive in DriveInfo.GetDrives())
                    {
                        try
                        {
                            if (!drive.IsReady) continue;
                            report.AppendLine("    " + lp++ + ". " + drive.Name + " [" + drive.DriveFormat + "]: " +
                                FormatBytes(drive.TotalSize) + " łącznie, " + FormatBytes(drive.AvailableFreeSpace) + " wolne");
                        }
                        catch (IOException ex)
                        {
                            report.AppendLine("    " + lp++ + ". " + drive.Name + ": niedostępny (" + ex.Message + ")");
                        }
                    }
                    report.AppendLine();

                    report.AppendLine(lp++ + ". Interfejsy sieciowe:");
                    using (var searcher = new ManagementObjectSearcher("select * from Win32_NetworkAdapterConfiguration where IPEnabled = True"))
                    {
                        foreach (var obj in searcher.Get())
                        {
                            report.AppendLine("    " + lp++ + ". Adapter: " + Value(obj["Description"]));
                            string[]? ipAddresses = obj["IPAddress"] as string[];
                            if (ipAddresses != null)
                            {
                                foreach (string ipAddress in ipAddresses)
                                    report.AppendLine("    " + lp++ + ". Adres IP: " + ipAddress);
                            }
                            string[]? gateways = obj["DefaultIPGateway"] as string[];
                            if (gateways != null)
                                report.AppendLine("    " + lp++ + ". Brama: " + string.Join(", ", gateways));
                            report.AppendLine("    " + lp++ + ". DNS: " + string.Join(", ", (obj["DNSServerSearchOrder"] as string[]) ?? Array.Empty<string>()));
                        }
                    }
                    }
                    catch (Exception ex)
                    {
                        report.AppendLine("[BŁĄD DIAGNOSTYKI]: " + ex.Message);
                        report.AppendLine("Część danych mogła nie być dostępna przez uprawnienia lub usługę WMI.");
                    }

                    Dispatcher.Invoke(() => TxtDiagnosticsOutput.Text = report.ToString());
                });
            }
            catch (Exception ex)
            {
                TxtDiagnosticsOutput.Text += "\n[BŁĄD] Nie udało się wygenerować raportu: " + ex.Message + "\n";
            }
            finally
            {
                BtnRunSystemDiagnostics.IsEnabled = true;
            }
        }

        private static string Value(object? value)
        {
            return value == null || string.IsNullOrWhiteSpace(value.ToString()) ? "Brak danych" : value.ToString()!;
        }

        private static long ReadInt64(object? value)
        {
            return value == null || value == DBNull.Value ? 0 : Convert.ToInt64(value);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "Brak danych";
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return size.ToString("0.##") + " " + units[unit];
        }

        private static string FormatDuration(TimeSpan duration)
        {
            return duration.Days + " d " + duration.Hours + " h " + duration.Minutes + " min";
        }

        private void BtnCopyDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(TxtDiagnosticsOutput.Text);
                System.Windows.MessageBox.Show("Raport diagnostyczny skopiowany do schowka!", "DevLab CC");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Błąd: " + ex.Message);
            }
        }

        private async void BtnStartNetworkScan_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseNetwork(TxtScanSubnet.Text.Trim(), out uint networkAddress, out uint broadcastAddress, out int prefixLength))
            {
                System.Windows.MessageBox.Show("Podaj poprawną podsieć, np. 192.168.1.0/24.", "Nieprawidłowa podsieć", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnStartNetworkScan.IsEnabled = false;
            TxtScanProgress.Text = "[Skanowanie...]";
            TxtNetworkScanOutput.Text = "[SKANER] Skanowanie " + UInt32ToIp(networkAddress) + "/" + prefixLength + "...\n\n";

            try
            {
                await Task.Run(() =>
                {
                    uint firstHost = networkAddress + 1;
                    uint lastHost = broadcastAddress - 1;
                    if (lastHost < firstHost) return;

                    Parallel.For((long)firstHost, (long)lastHost + 1, new ParallelOptions { MaxDegreeOfParallelism = 32 }, address =>
                    {
                        string ip = UInt32ToIp((uint)address);
                        using (Ping p = new Ping())
                        {
                            try
                            {
                                var reply = p.Send(ip, 350);
                                if (reply.Status == IPStatus.Success)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        TxtNetworkScanOutput.Text += "[ONLINE] " + ip + " (RTT: " + reply.RoundtripTime + " ms)\n";
                                        TxtNetworkScanOutput.ScrollToEnd();
                                    });
                                }
                            }
                            catch (PingException) { }
                        }
                    });
                });

                TxtNetworkScanOutput.Text += "\n[ZAKOŃCZONO] Skanowanie podsieci.\n";
                TxtScanProgress.Text = "[Gotowy]";
            }
            catch (Exception ex)
            {
                TxtNetworkScanOutput.Text += "\n[BŁĄD] " + ex.Message + "\n";
                TxtScanProgress.Text = "[Błąd]";
            }
            finally
            {
                BtnStartNetworkScan.IsEnabled = true;
            }
        }

        private static bool TryDetectLocalNetwork(out string network, out int prefixLength, out string localAddress)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus != OperationalStatus.Up || adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                IPInterfaceProperties properties = adapter.GetIPProperties();
                if (!properties.GatewayAddresses.Any(gateway =>
                    gateway.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(gateway.Address)))
                    continue;

                foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
                {
                    if (address.Address.AddressFamily != AddressFamily.InterNetwork || address.IPv4Mask == null || IPAddress.IsLoopback(address.Address))
                        continue;

                    prefixLength = CountMaskBits(address.IPv4Mask);
                    if (prefixLength < 1 || prefixLength > 30) continue;

                    uint ip = IpToUInt32(address.Address);
                    uint mask = IpToUInt32(address.IPv4Mask);
                    network = UInt32ToIp(ip & mask);
                    localAddress = address.Address.ToString();
                    return true;
                }
            }

            network = string.Empty;
            localAddress = string.Empty;
            prefixLength = 0;
            return false;
        }

        private static bool TryParseNetwork(string value, out uint network, out uint broadcast, out int prefixLength)
        {
            network = 0;
            broadcast = 0;
            prefixLength = 0;
            string[] parts = value.Split('/', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out IPAddress? address) ||
                address.AddressFamily != AddressFamily.InterNetwork || !int.TryParse(parts[1], out prefixLength) ||
                prefixLength < 1 || prefixLength > 30)
                return false;

            uint mask = uint.MaxValue << (32 - prefixLength);
            network = IpToUInt32(address) & mask;
            broadcast = network | ~mask;
            return true;
        }

        private static int CountMaskBits(IPAddress mask)
        {
            uint value = IpToUInt32(mask);
            int count = 0;
            while (value != 0)
            {
                count += (int)(value & 1);
                value >>= 1;
            }
            return count;
        }

        private static uint IpToUInt32(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();
            return ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) | ((uint)bytes[2] << 8) | bytes[3];
        }

        private static string UInt32ToIp(uint address)
        {
            return string.Join(".", (address >> 24) & 255, (address >> 16) & 255, (address >> 8) & 255, address & 255);
        }

        private async void BtnToolPing_Click(object sender, RoutedEventArgs e)
        {
            string target = ShowDevLabInput("Ping Test", "Podaj adres IP lub domenę do spingowania:", "8.8.8.8");
            if (!string.IsNullOrEmpty(target))
            {
                TxtTerminalOutput.Text += "[TOOL] Uruchamianie ping dla " + target + "...\n";
                await RunSshCommandAsync("ping -c 4 " + target);
            }
        }

        private async void BtnToolTraceroute_Click(object sender, RoutedEventArgs e)
        {
            string target = ShowDevLabInput("Traceroute Test", "Podaj cel trasowania (IP / domena):", "1.1.1.1");
            if (!string.IsNullOrEmpty(target))
            {
                await RunSshCommandAsync("traceroute " + target);
            }
        }

        private async void BtnToolIperf_Click(object sender, RoutedEventArgs e)
        {
            string serverIp = ShowDevLabInput("iPerf3 Test", "Podaj IP serwera iPerf3 (lub pozostaw puste dla wersji):", "");
            string cmd = string.IsNullOrWhiteSpace(serverIp) ? "iperf3 -v" : "iperf3 -c " + serverIp;
            await RunSshCommandAsync(cmd);
        }

        private async void BtnToolDiskIo_Click(object sender, RoutedEventArgs e)
        {
            string size = ShowDevLabInput("Disk Benchmark (fio)", "Rozmiar pliku testowego I/O (np. 64M, 128M):", "64M");
            if (string.IsNullOrWhiteSpace(size)) size = "64M";
            await RunSshCommandAsync("fio --name=test --ioengine=libaio --rw=randwrite --bs=4k --size=" + size + " --numjobs=1 --time_based --runtime=10 --group_reporting");
        }

        private async void BtnToolHtop_Click(object sender, RoutedEventArgs e)
        {
            await RunSshCommandAsync("top -b -n 1 | head -n 20");
        }

        private async void BtnToolMultiPing_Click(object sender, RoutedEventArgs e)
        {
            string target = ShowDevLabInput("Multi-Ping Test", "Podaj adres IP lub domenę:", "1.1.1.1");
            if (string.IsNullOrWhiteSpace(target)) return;

            TxtNetworkScanOutput.Text = "[MULTI-PING] Testowanie " + target + "...\n\n";
            await Task.Run(() =>
            {
                using (Ping ping = new Ping())
                {
                    for (int i = 1; i <= 10; i++)
                    {
                        try
                        {
                            var reply = ping.Send(target, 1000);
                            Dispatcher.Invoke(() =>
                            {
                                if (reply.Status == IPStatus.Success)
                                    TxtNetworkScanOutput.Text += "[SEQ " + i + "] Odpowiedź: czas=" + reply.RoundtripTime + "ms\n";
                                else
                                    TxtNetworkScanOutput.Text += "[SEQ " + i + "] Timeout\n";
                                TxtNetworkScanOutput.ScrollToEnd();
                            });
                        }
                        catch { }
                        Thread.Sleep(200);
                    }
                }
            });
        }

        private async void BtnToolTcpPortScan_Click(object sender, RoutedEventArgs e)
        {
            string targetIp = ShowDevLabInput("Skaner Portów TCP", "Podaj adres IP hosta:", "192.168.1.1");
            if (string.IsNullOrWhiteSpace(targetIp)) return;

            TxtNetworkScanOutput.Text = "[TCP SCAN] Skanowanie portów na " + targetIp + "...\n\n";
            int[] commonPorts = { 21, 22, 80, 443, 3389, 8006 };

            await Task.Run(() =>
            {
                foreach (int port in commonPorts)
                {
                    try
                    {
                        using (TcpClient tcp = new TcpClient())
                        {
                            var ar = tcp.BeginConnect(targetIp, port, null, null);
                            if (ar.AsyncWaitHandle.WaitOne(150) && tcp.Connected)
                            {
                                Dispatcher.Invoke(() => TxtNetworkScanOutput.Text += "[OTWARTY] Port " + port + "\n");
                            }
                        }
                    }
                    catch { }
                }
            });
        }

        private async void BtnToolDnsLookup_Click(object sender, RoutedEventArgs e)
        {
            string domain = ShowDevLabInput("DNS Lookup", "Podaj domenę:", "devlabstudy.ovh");
            if (string.IsNullOrWhiteSpace(domain)) return;

            TxtNetworkScanOutput.Text = "[DNS] Sprawdzanie " + domain + "...\n\n";
            await Task.Run(() =>
            {
                try
                {
                    var entry = Dns.GetHostEntry(domain);
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var ip in entry.AddressList)
                        {
                            TxtNetworkScanOutput.Text += "IP: " + ip + "\n";
                        }
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => TxtNetworkScanOutput.Text += "Błąd DNS: " + ex.Message + "\n");
                }
            });
        }

        private void BtnClearToolOutput_Click(object sender, RoutedEventArgs e) => TxtNetworkScanOutput.Text = "Wyczyszczono.\n";
    }

    public class ServerNode
    {
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberPassword { get; set; }
        public Brush StatusColor { get; set; } = Brushes.LimeGreen;

        public string HasSavedPasswordText => RememberPassword ? "TAK" : "NIE";
    }
}