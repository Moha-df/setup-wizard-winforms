using System.Diagnostics;
using System.Text.RegularExpressions;

namespace setup_wizard.Utils
{
    public class DependencyInfo
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public string VersionArgs { get; set; }
        public string InstallUrl { get; set; }
        public string DownloadUrl { get; set; }
        public bool IsInstalled { get; set; }
        public string Version { get; set; }
        public string Status { get; set; }
    }

    public static class DependencyChecker
    {
        public static async Task<DependencyInfo> CheckDependencyAsync(DependencyInfo dependency)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = dependency.Command,
                        Arguments = dependency.VersionArgs,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    dependency.IsInstalled = true;
                    dependency.Version = ExtractVersion(output);
                    dependency.Status = "Installed";
                }
                else
                {
                    // Essayer la vérification alternative si la vérification standard échoue
                    var (success, version) = await CheckDependencyAlternativeAsync(dependency);
                    dependency.IsInstalled = success;
                    if (dependency.IsInstalled)
                    {
                        dependency.Version = version;
                        dependency.Status = "Installed";
                    }
                    else
                    {
                        dependency.Version = "";
                        dependency.Status = "Not Found";
                    }
                }
            }
            catch
            {
                // Essayer la vérification alternative en cas d'exception
                var (success, version) = await CheckDependencyAlternativeAsync(dependency);
                dependency.IsInstalled = success;
                if (dependency.IsInstalled)
                {
                    dependency.Version = version;
                    dependency.Status = "Installed";
                }
                else
                {
                    dependency.Version = "";
                    dependency.Status = "Not Found";
                }
            }

            return dependency;
        }

        private static async Task<(bool success, string version)> CheckDependencyAlternativeAsync(DependencyInfo dependency)
        {
            try
            {
                var commonPaths = GetCommonInstallPaths(dependency);
                
                foreach (var path in commonPaths)
                {
                    // Remplacer les variables d'environnement dans le chemin
                    string expandedPath = Environment.ExpandEnvironmentVariables(path);
                    
                    if (File.Exists(expandedPath))
                    {
                        // Tester si l'exécutable fonctionne et capturer la version
                        var process = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = expandedPath,
                                Arguments = dependency.VersionArgs,
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        process.Start();
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                        {
                            string version = ExtractVersion(output);
                            return (true, version);
                        }
                        else
                        {
                            // Log pour le débogage
                            System.Diagnostics.Debug.WriteLine($"Vérification alternative échouée pour {dependency.Name} à {expandedPath}");
                            System.Diagnostics.Debug.WriteLine($"Sortie: {output}");
                            System.Diagnostics.Debug.WriteLine($"Erreur: {error}");
                            System.Diagnostics.Debug.WriteLine($"Code de sortie: {process.ExitCode}");
                        }
                    }
                }
                
                return (false, "");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception lors de la vérification alternative de {dependency.Name}: {ex.Message}");
                return (false, "");
            }
        }

        private static List<string> GetCommonInstallPaths(DependencyInfo dependency)
        {
            return dependency.Name switch
            {
                "Nmap" => new List<string>
                {
                    @"C:\Program Files\Nmap\nmap.exe",
                    @"C:\Program Files (x86)\Nmap\nmap.exe",
                    @"C:\Nmap\nmap.exe"
                },
                "Node.js" => new List<string>
                {
                    @"C:\Program Files\nodejs\node.exe",
                    @"C:\Program Files (x86)\nodejs\node.exe"
                },
                "Git" => new List<string>
                {
                    @"C:\Program Files\Git\bin\git.exe",
                    @"C:\Program Files (x86)\Git\bin\git.exe"
                },
                "Android SDK Tools" => new List<string>
                {
                    @"C:\Program Files\Android\Sdk\platform-tools\adb.exe",
                    @"%ANDROID_HOME%\platform-tools\adb.exe",
                    @"%ANDROID_SDK_ROOT%\platform-tools\adb.exe",
                    @"C:\Users\%USERNAME%\AppData\Local\Android\Sdk\platform-tools\adb.exe",
                    @"C:\Program Files\Android\Android Studio\Sdk\platform-tools\adb.exe",
                    @"C:\Program Files (x86)\Android\Android Studio\Sdk\platform-tools\adb.exe"
                },
                "scrcpy" => new List<string>
                {
                    @"C:\Program Files\scrcpy\scrcpy-win64-v3.3.1\scrcpy.exe",
                    @"C:\Program Files\scrcpy\scrcpy-win32-v3.3.1\scrcpy.exe",
                    @"%SCRCPY_HOME%\scrcpy-win64-v3.3.1\scrcpy.exe",
                    @"%SCRCPY_HOME%\scrcpy-win32-v3.3.1\scrcpy.exe",
                    @"%SCRCPY_HOME%\scrcpy.exe"
                },
                _ => new List<string>()
            };
        }

        private static string ExtractVersion(string output)
        {
            // Patterns spécifiques pour chaque dépendance
            if (output.Contains("Nmap version"))
            {
                // Pattern spécifique pour Nmap
                var nmapMatch = Regex.Match(output, @"Nmap version (\d+\.\d+)", RegexOptions.IgnoreCase);
                if (nmapMatch.Success)
                {
                    return nmapMatch.Groups[1].Value;
                }
            }
            else if (output.Contains("node"))
            {
                // Pattern spécifique pour Node.js
                var nodeMatch = Regex.Match(output, @"v(\d+\.\d+\.\d+)", RegexOptions.IgnoreCase);
                if (nodeMatch.Success)
                {
                    return nodeMatch.Groups[1].Value;
                }
            }
            else if (output.Contains("git version"))
            {
                // Pattern spécifique pour Git
                var gitMatch = Regex.Match(output, @"git version (\d+\.\d+\.\d+)", RegexOptions.IgnoreCase);
                if (gitMatch.Success)
                {
                    return gitMatch.Groups[1].Value;
                }
            }
            else if (output.Contains("scrcpy"))
            {
                // Pattern spécifique pour scrcpy
                var scrcpyMatch = Regex.Match(output, @"scrcpy (\d+\.\d+\.\d+)", RegexOptions.IgnoreCase);
                if (scrcpyMatch.Success)
                {
                    return scrcpyMatch.Groups[1].Value;
                }
            }
            else if (output.Contains("Android Debug Bridge"))
            {
                // Pattern spécifique pour ADB
                var adbMatch = Regex.Match(output, @"Android Debug Bridge version (\d+\.\d+\.\d+)", RegexOptions.IgnoreCase);
                if (adbMatch.Success)
                {
                    return adbMatch.Groups[1].Value;
                }
            }

            // Patterns génériques en fallback
            var patterns = new[]
            {
                @"version\s+(\d+\.\d+\.\d+)",  // version x.y.z
                @"v(\d+\.\d+\.\d+)",           // vx.y.z
                @"(\d+\.\d+\.\d+)",            // x.y.z
                @"(\d+\.\d+)",                 // x.y
                @"(\d+)"                       // x
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(output, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return output.Trim();
        }

        public static List<DependencyInfo> GetDefaultDependencies()
        {
            bool is64Bit = Environment.Is64BitOperatingSystem;
            
            return new List<DependencyInfo>
            {
                new DependencyInfo
                {
                    Name = "Node.js",
                    Command = "node",
                    VersionArgs = "--version",
                    InstallUrl = "https://nodejs.org/",
                    DownloadUrl = is64Bit 
                        ? "https://nodejs.org/dist/v20.11.0/node-v20.11.0-x64.msi"
                        : "https://nodejs.org/dist/v20.11.0/node-v20.11.0-x86.msi",
                    Status = "Unknown"
                },
                new DependencyInfo
                {
                    Name = "Git",
                    Command = "git",
                    VersionArgs = "--version",
                    InstallUrl = "https://git-scm.com/",
                    DownloadUrl = is64Bit
                        ? "https://github.com/git-for-windows/git/releases/download/v2.43.0.windows.1/Git-2.43.0-64-bit.exe"
                        : "https://github.com/git-for-windows/git/releases/download/v2.43.0.windows.1/Git-2.43.0-32-bit.exe",
                    Status = "Unknown"
                },
                new DependencyInfo
                {
                    Name = "Android SDK Tools",
                    Command = "adb",
                    VersionArgs = "version",
                    InstallUrl = "https://developer.android.com/studio#command-tools",
                    DownloadUrl = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip",
                    Status = "Unknown"
                },
                new DependencyInfo
                {
                    Name = "scrcpy",
                    Command = "scrcpy",
                    VersionArgs = "--version",
                    InstallUrl = "https://github.com/Genymobile/scrcpy/releases",
                    DownloadUrl = is64Bit
                        ? "https://github.com/Genymobile/scrcpy/releases/download/v3.3.1/scrcpy-win64-v3.3.1.zip"
                        : "https://github.com/Genymobile/scrcpy/releases/download/v3.3.1/scrcpy-win32-v3.3.1.zip",
                    Status = "Unknown"
                },
                new DependencyInfo
                {
                    Name = "Nmap",
                    Command = "nmap",
                    VersionArgs = "--version",
                    InstallUrl = "https://nmap.org/",
                    DownloadUrl = "https://nmap.org/dist/nmap-7.94-setup.exe",
                    Status = "Unknown"
                }
            };
        }

        public static async Task<List<DependencyInfo>> CheckAllDependenciesAsync()
        {
            var dependencies = GetDefaultDependencies();
            var tasks = dependencies.Select(dep => CheckDependencyAsync(dep));
            await Task.WhenAll(tasks);
            return dependencies;
        }

        public static void OpenDownloadPage(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to open download page: {ex.Message}");
            }
        }

        public static bool IsSystemCompatible()
        {
            try
            {
                var os = Environment.OSVersion;
                var platform = os.Platform;
                var version = os.Version;

                // Check if it's Windows
                if (platform != PlatformID.Win32NT)
                {
                    return false;
                }

                // Check if it's Windows 10 or later (major version 10+)
                return version.Major >= 10;
            }
            catch
            {
                return false;
            }
        }

        public static string GetSystemInfo()
        {
            try
            {
                var os = Environment.OSVersion;
                var platform = os.Platform;
                var version = os.Version;
                var machineName = Environment.MachineName;
                var processorCount = Environment.ProcessorCount;
                var workingSet = Environment.WorkingSet / (1024 * 1024); // MB

                return $"OS: {platform} {version}\n" +
                       $"Machine: {machineName}\n" +
                       $"Processors: {processorCount}\n" +
                       $"Memory: {workingSet} MB";
            }
            catch
            {
                return "Unable to retrieve system information";
            }
        }
    }
}
