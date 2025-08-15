using System.Diagnostics;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.IO.Compression;

namespace setup_wizard.Utils
{
    public class DependencyInstallResult
    {
        public string DependencyName { get; set; } = "";
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string LogOutput { get; set; } = "";
        public int ExitCode { get; set; }
    }

    public class InstallationResult
    {
        public bool AllSuccessful { get; set; }
        public List<DependencyInstallResult> Results { get; set; } = new List<DependencyInstallResult>();
    }

    public static class DependencyInstaller
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<DependencyInstallResult> InstallDependencyAsync(DependencyInfo dependency, IProgress<string> progress)
        {
            var result = new DependencyInstallResult
            {
                DependencyName = dependency.Name,
                Success = false
            };

            try
            {
                progress.Report($"Téléchargement de {dependency.Name}...");
                
                // Télécharger l'installateur
                string installerPath = await DownloadInstallerAsync(dependency, progress);
                if (string.IsNullOrEmpty(installerPath))
                {
                    result.ErrorMessage = "Échec du téléchargement de l'installateur";
                    return result;
                }

                progress.Report($"Installation de {dependency.Name}...");
                
                // Installer en mode silencieux
                var installResult = await InstallSilentlyAsync(dependency, installerPath, progress);
                result.Success = installResult.Success;
                result.ErrorMessage = installResult.ErrorMessage;
                result.LogOutput = installResult.LogOutput;
                result.ExitCode = installResult.ExitCode;
                
                // Nettoyer le fichier temporaire
                try
                {
                    if (File.Exists(installerPath))
                    {
                        File.Delete(installerPath);
                    }
                }
                catch { /* Ignore cleanup errors */ }

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Erreur lors de l'installation de {dependency.Name}: {ex.Message}";
                progress.Report(result.ErrorMessage);
                return result;
            }
        }

        private static async Task<string> DownloadInstallerAsync(DependencyInfo dependency, IProgress<string> progress)
        {
            try
            {
                // Créer le dossier temporaire
                string tempDir = Path.Combine(Path.GetTempPath(), "TabletteSystemV2_Installers");
                Directory.CreateDirectory(tempDir);

                // Nom du fichier d'installation
                string fileName = GetInstallerFileName(dependency);
                string filePath = Path.Combine(tempDir, fileName);

                progress.Report($"Téléchargement depuis {dependency.DownloadUrl}...");

                // Télécharger le fichier
                byte[] fileBytes = await httpClient.GetByteArrayAsync(dependency.DownloadUrl);
                
                // Vérifier que le fichier n'est pas vide
                if (fileBytes.Length == 0)
                {
                    progress.Report($"❌ Erreur: Fichier téléchargé vide (0 bytes)");
                    return null;
                }

                // Vérifier la taille minimale (au moins 1MB pour un installateur)
                if (fileBytes.Length < 1024 * 1024)
                {
                    progress.Report($"⚠️ Attention: Fichier très petit ({fileBytes.Length} bytes) - possible erreur de téléchargement");
                }

                await File.WriteAllBytesAsync(filePath, fileBytes);

                // Vérifier que le fichier a bien été écrit
                if (!File.Exists(filePath))
                {
                    progress.Report($"❌ Erreur: Fichier non créé après écriture");
                    return null;
                }

                var fileInfo = new FileInfo(filePath);
                progress.Report($"✅ Téléchargement terminé: {filePath} ({fileInfo.Length} bytes)");

                // Vérifier que le fichier est exécutable
                if (!IsValidExecutable(filePath, dependency))
                {
                    progress.Report($"❌ Erreur: Fichier téléchargé n'est pas un exécutable valide");
                    return null;
                }

                return filePath;
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    progress.Report($"❌ Erreur 404: L'URL de téléchargement n'est plus valide");
                    progress.Report($"Veuillez vérifier la disponibilité de {dependency.Name} sur {dependency.InstallUrl}");
                }
                else
                {
                    progress.Report($"❌ Erreur HTTP {ex.StatusCode}: {ex.Message}");
                }
                return null;
            }
            catch (Exception ex)
            {
                progress.Report($"❌ Erreur de téléchargement: {ex.Message}");
                return null;
            }
        }

        private static bool IsValidExecutable(string filePath, DependencyInfo dependency)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                
                // Vérifier l'extension
                string extension = fileInfo.Extension.ToLower();
                bool isValidExtension = extension switch
                {
                    ".exe" => true,
                    ".msi" => true,
                    ".msu" => true,
                    ".zip" => dependency.Name == "Android SDK Tools" || dependency.Name == "scrcpy", // Accepter ZIP pour Android SDK et scrcpy
                    _ => false
                };

                if (!isValidExtension)
                {
                    return false;
                }

                // Vérifier que le fichier n'est pas vide
                if (fileInfo.Length == 0)
                {
                    return false;
                }

                // Pour les fichiers .msi, vérifier qu'ils commencent par la signature MSI
                if (extension == ".msi")
                {
                    using var stream = File.OpenRead(filePath);
                    byte[] header = new byte[4];
                    stream.Read(header, 0, 4);
                    
                    // Signature MSI: 0xD0, 0xCF, 0x11, 0xE0
                    if (header[0] != 0xD0 || header[1] != 0xCF || header[2] != 0x11 || header[3] != 0xE0)
                    {
                        return false;
                    }
                }

                // Pour les fichiers .zip, vérifier qu'ils commencent par la signature ZIP
                if (extension == ".zip")
                {
                    using var stream = File.OpenRead(filePath);
                    byte[] header = new byte[4];
                    stream.Read(header, 0, 4);
                    
                    // Signature ZIP: 0x50, 0x4B, 0x03, 0x04 ou 0x50, 0x4B, 0x05, 0x06 ou 0x50, 0x4B, 0x07, 0x08
                    if (header[0] != 0x50 || header[1] != 0x4B)
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<DependencyInstallResult> InstallSilentlyAsync(DependencyInfo dependency, string installerPath, IProgress<string> progress)
        {
            var result = new DependencyInstallResult
            {
                DependencyName = dependency.Name,
                Success = false
            };

            try
            {
                // Installation spéciale pour les fichiers ZIP (Android SDK Tools et scrcpy)
                string extension = Path.GetExtension(installerPath).ToLower();
                if (extension == ".zip")
                {
                    if (dependency.Name == "scrcpy")
                    {
                        return await InstallScrcpyAsync(dependency, installerPath, progress);
                    }
                    else if (dependency.Name == "Android SDK Tools")
                    {
                        return await InstallAndroidSDKToolsAsync(dependency, installerPath, progress);
                    }
                }

                // Vérifier les privilèges administrateur pour les autres dépendances
                if (!IsRunningAsAdministrator())
                {
                    result.Success = false;
                    result.ErrorMessage = "Privilèges administrateur requis. Veuillez relancer le setup wizard en tant qu'administrateur.";
                    progress.Report(result.ErrorMessage);
                    return result;
                }

                string arguments = GetSilentInstallArguments(dependency);
                
                // Gestion spéciale pour Nmap (pas d'installation silencieuse)
                if (dependency.Name == "Nmap")
                {
                    progress.Report($"⚠️ Nmap nécessite une installation manuelle (pour gérer Npcap). Lancement de l'installateur...");
                    
                    // Lancer l'installateur Nmap en mode visible (pas silencieux)
                    var nmapProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = installerPath,
                            UseShellExecute = true, // Mode visible
                            CreateNoWindow = false
                        }
                    };
                    
                    nmapProcess.Start();
                    
                    // Attendre que l'utilisateur termine l'installation manuellement
                    progress.Report($"📋 Veuillez installer Nmap dans la fenêtre qui s'est ouverte, puis cliquez sur 'Terminer'.");
                    
                    // Retourner un succès temporaire (l'utilisateur doit confirmer)
                    result.Success = true;
                    result.LogOutput = "Installation manuelle de Nmap lancée. Veuillez confirmer une fois terminée.";
                    return result;
                }
                
                progress.Report($"Lancement de l'installation avec les arguments: {arguments}");

                // Utiliser msiexec pour les .msi
                Process process;
                if (extension == ".msi")
                {
                    process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "msiexec",
                            Arguments = $"/i \"{installerPath}\" {arguments}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                }
                else
                {
                    process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = installerPath,
                            Arguments = arguments,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                }

                process.Start();
                
                // Capturer la sortie
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                
                // Attendre la fin de l'installation
                await process.WaitForExitAsync();

                result.ExitCode = process.ExitCode;
                result.LogOutput = $"Sortie: {output}\nErreurs: {error}";

                if (process.ExitCode == 0)
                {
                    progress.Report($"Installation de {dependency.Name} terminée avec succès");
                    
                    // Vérifier que l'installation a vraiment réussi
                    await Task.Delay(2000); // Attendre que les fichiers soient bien écrits
                    progress.Report($"Vérification de l'installation de {dependency.Name}...");
                    
                    bool verified = await VerifyInstallationAsync(dependency);
                    
                    if (verified)
                    {
                        result.Success = true;
                        result.ErrorMessage = "";
                        progress.Report($"✅ {dependency.Name} installé et vérifié avec succès");
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = $"L'installation s'est terminée mais la vérification a échoué. Vérifiez manuellement si {dependency.Name} est bien installé.";
                        progress.Report($"❌ Vérification échouée pour {dependency.Name}");
                    }
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = $"Installation échouée avec le code de sortie: {process.ExitCode}";
                    progress.Report($"Installation de {dependency.Name} a échoué (code: {process.ExitCode})");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Erreur lors de l'installation: {ex.Message}";
                progress.Report(result.ErrorMessage);
            }

            return result;
        }

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static string GetInstallerFileName(DependencyInfo dependency)
        {
            return dependency.Name switch
            {
                "Node.js" => "nodejs-installer.msi",
                "Git" => "git-installer.exe",
                "Nmap" => "nmap-installer.exe",
                "Android SDK Tools" => "android-sdk-tools.zip",
                "scrcpy" => "scrcpy.zip",
                _ => $"installer-{dependency.Name.ToLower()}.exe"
            };
        }

        private static string GetSilentInstallArguments(DependencyInfo dependency)
        {
            return dependency.Name switch
            {
                "Node.js" => "/quiet /norestart /log install.log",
                "Git" => "/VERYSILENT /NORESTART /SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                "Nmap" => "", // Pas d'installation silencieuse pour Nmap (pour permettre l'installation de Npcap)
                _ => "/S" // Mode silencieux générique
            };
        }

        private static async Task<bool> VerifyInstallationAsync(DependencyInfo dependency)
        {
            try
            {
                // Attendre plus longtemps pour que l'installation se termine complètement
                await Task.Delay(5000);
                
                // Vérifier que la dépendance est maintenant installée
                var result = await DependencyChecker.CheckDependencyAsync(dependency);
                
                if (result.IsInstalled)
                {
                    return true;
                }
                
                // Si la vérification échoue, essayer des vérifications alternatives
                return await VerifyInstallationAlternativeAsync(dependency);
            }
            catch (Exception ex)
            {
                // Log l'erreur pour le debug
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification de {dependency.Name}: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> VerifyInstallationAlternativeAsync(DependencyInfo dependency)
        {
            try
            {
                // Vérifier les emplacements d'installation typiques
                var commonPaths = GetCommonInstallPaths(dependency);
                
                foreach (var path in commonPaths)
                {
                    if (Directory.Exists(path) || File.Exists(path))
                    {
                        // Essayer de lancer la commande depuis ce chemin
                        if (await TestCommandFromPathAsync(dependency, path))
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
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
                _ => new List<string>()
            };
        }

        private static async Task<bool> TestCommandFromPathAsync(DependencyInfo dependency, string path)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = dependency.VersionArgs,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return process.ExitCode == 0 && !string.IsNullOrEmpty(output);
            }
            catch
            {
                return false;
            }
        }

        public static async Task<InstallationResult> InstallAllMissingDependenciesAsync(List<DependencyInfo> missingDeps, IProgress<string> progress)
        {
            var results = new List<DependencyInstallResult>();
            
            // Vérifier les privilèges administrateur au début
            if (!IsRunningAsAdministrator())
            {
                progress.Report("⚠️ Privilèges administrateur requis pour l'installation automatique");
                progress.Report("Veuillez relancer le setup wizard en tant qu'administrateur");
                
                // Créer un résultat d'erreur pour chaque dépendance
                foreach (var dep in missingDeps)
                {
                    results.Add(new DependencyInstallResult
                    {
                        DependencyName = dep.Name,
                        Success = false,
                        ErrorMessage = "Privilèges administrateur requis. Relancez en tant qu'administrateur.",
                        ExitCode = -1
                    });
                }
                
                return new InstallationResult
                {
                    AllSuccessful = false,
                    Results = results
                };
            }
            
            foreach (var dep in missingDeps)
            {
                progress.Report($"Installation de {dep.Name}...");
                var result = await InstallDependencyAsync(dep, progress);
                results.Add(result);
                
                if (!result.Success)
                {
                    progress.Report($"Échec de l'installation de {dep.Name}: {result.ErrorMessage}");
                }
                
                // Pause entre les installations
                await Task.Delay(1000);
            }
            
            return new InstallationResult
            {
                AllSuccessful = results.All(r => r.Success),
                Results = results
            };
        }

        private static async Task<DependencyInstallResult> InstallScrcpyAsync(DependencyInfo dependency, string installerPath, IProgress<string> progress)
        {
            var result = new DependencyInstallResult
            {
                DependencyName = dependency.Name,
                Success = false
            };

            try
            {
                progress.Report("Installation de scrcpy...");
                
                // Vérifier les privilèges administrateur
                if (!IsRunningAsAdministrator())
                {
                    result.ErrorMessage = "Privilèges administrateur requis pour installer scrcpy dans Program Files";
                    progress.Report($"❌ {result.ErrorMessage}");
                    return result;
                }
                
                // Créer le dossier d'installation
                string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "scrcpy");
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                progress.Report("Extraction des fichiers...");
                
                // Extraire le fichier ZIP
                string extractDir = Path.Combine(Path.GetTempPath(), "scrcpy_extract");
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }
                Directory.CreateDirectory(extractDir);

                // Utiliser System.IO.Compression pour extraire
                using (var archive = System.IO.Compression.ZipFile.OpenRead(installerPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(extractDir, entry.FullName);
                        string destinationDir = Path.GetDirectoryName(destinationPath);
                        
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(destinationPath, true);
                        }
                    }
                }

                progress.Report("Copie des fichiers...");
                
                // Copier les fichiers extraits vers le dossier d'installation
                string[] extractedFiles = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
                foreach (string file in extractedFiles)
                {
                    string relativePath = file.Substring(extractDir.Length + 1);
                    string destFile = Path.Combine(installDir, relativePath);
                    string destDir = Path.GetDirectoryName(destFile);
                    
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    
                    File.Copy(file, destFile, true);
                }

                progress.Report("Configuration des variables d'environnement...");
                
                // Ajouter au PATH système (Machine) pour que ce soit disponible partout
                // Pour scrcpy, on doit ajouter le dossier contenant l'exécutable
                string scrcpyExePath = Path.Combine(installDir, "scrcpy-win64-v3.3.1");
                if (!Directory.Exists(scrcpyExePath))
                {
                    // Essayer de trouver le bon dossier
                    string[] subdirs = Directory.GetDirectories(installDir);
                    if (subdirs.Length > 0)
                    {
                        scrcpyExePath = subdirs[0]; // Prendre le premier sous-dossier
                        progress.Report($"📁 Dossier scrcpy trouvé: {scrcpyExePath}");
                    }
                }
                
                string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
                if (!machinePath.Contains(scrcpyExePath))
                {
                    string newPath = string.IsNullOrEmpty(machinePath) ? scrcpyExePath : machinePath + ";" + scrcpyExePath;
                    Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
                    progress.Report($"✅ PATH système mis à jour avec {scrcpyExePath}");
                }
                
                // Créer la variable SCRCPY_HOME au niveau système
                Environment.SetEnvironmentVariable("SCRCPY_HOME", installDir, EnvironmentVariableTarget.Machine);
                progress.Report($"✅ Variable d'environnement SCRCPY_HOME configurée au niveau système");

                progress.Report("Nettoyage des fichiers temporaires...");
                
                // Nettoyer
                try
                {
                    if (Directory.Exists(extractDir))
                    {
                        Directory.Delete(extractDir, true);
                    }
                }
                catch { /* Ignore cleanup errors */ }

                result.Success = true;
                result.ErrorMessage = "";
                progress.Report("✅ scrcpy installé avec succès !");
                progress.Report("Note: Redémarrez votre terminal pour que les variables d'environnement soient prises en compte.");

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Erreur lors de l'installation de scrcpy: {ex.Message}";
                progress.Report($"❌ {result.ErrorMessage}");
                return result;
            }
        }

        private static async Task<DependencyInstallResult> InstallAndroidSDKToolsAsync(DependencyInfo dependency, string installerPath, IProgress<string> progress)
        {
            var result = new DependencyInstallResult
            {
                DependencyName = dependency.Name,
                Success = false
            };

            try
            {
                progress.Report("Installation des Android SDK Tools...");
                
                // Vérifier les privilèges administrateur
                if (!IsRunningAsAdministrator())
                {
                    result.ErrorMessage = "Privilèges administrateur requis pour installer Android SDK Tools dans Program Files";
                    progress.Report($"❌ {result.ErrorMessage}");
                    return result;
                }
                
                // Créer le dossier d'installation
                string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "Sdk");
                if (!Directory.Exists(installDir))
                {
                    Directory.CreateDirectory(installDir);
                }

                progress.Report("Extraction des fichiers...");
                
                // Extraire le fichier ZIP
                string extractDir = Path.Combine(Path.GetTempPath(), "android_sdk_extract");
                if (Directory.Exists(extractDir))
                {
                    Directory.Delete(extractDir, true);
                }
                Directory.CreateDirectory(extractDir);

                // Utiliser System.IO.Compression pour extraire
                using (var archive = ZipFile.OpenRead(installerPath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(extractDir, entry.FullName);
                        string destinationDir = Path.GetDirectoryName(destinationPath);
                        
                        if (!Directory.Exists(destinationDir))
                        {
                            Directory.CreateDirectory(destinationDir);
                        }

                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(destinationPath, true);
                        }
                    }
                }

                progress.Report("Copie des fichiers...");
                
                // Copier les fichiers extraits vers le dossier d'installation
                string[] extractedFiles = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
                foreach (string file in extractedFiles)
                {
                    string relativePath = file.Substring(extractDir.Length + 1);
                    string destFile = Path.Combine(installDir, relativePath);
                    string destDir = Path.GetDirectoryName(destFile);
                    
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    
                    File.Copy(file, destFile, true);
                }

                progress.Report("Configuration des variables d'environnement...");
                
                // Ajouter au PATH système (Machine) pour que ce soit disponible partout
                string platformToolsPath = Path.Combine(installDir, "platform-tools");
                string machinePath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "";
                if (!machinePath.Contains(platformToolsPath))
                {
                    string newPath = string.IsNullOrEmpty(machinePath) ? platformToolsPath : machinePath + ";" + platformToolsPath;
                    Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.Machine);
                    progress.Report($"✅ PATH système mis à jour avec {platformToolsPath}");
                }

                // Créer les variables d'environnement Android au niveau système
                Environment.SetEnvironmentVariable("ANDROID_HOME", installDir, EnvironmentVariableTarget.Machine);
                Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT", installDir, EnvironmentVariableTarget.Machine);
                progress.Report($"✅ Variables d'environnement Android configurées au niveau système");

                progress.Report("Nettoyage des fichiers temporaires...");
                
                // Nettoyer
                try
                {
                    if (Directory.Exists(extractDir))
                    {
                        Directory.Delete(extractDir, true);
                    }
                }
                catch { /* Ignore cleanup errors */ }

                result.Success = true;
                result.ErrorMessage = "";
                progress.Report("✅ Android SDK Tools installés avec succès !");
                progress.Report("Note: Redémarrez votre terminal pour que les variables d'environnement soient prises en compte.");

                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Erreur lors de l'installation des Android SDK Tools: {ex.Message}";
                progress.Report($"❌ {result.ErrorMessage}");
                return result;
            }
        }
    }
}
