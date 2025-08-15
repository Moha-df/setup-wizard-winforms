using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO; // Added for Path.Combine

namespace setup_wizard.Panels
{
    public partial class PM2InstallPanel : UserControl
    {
        private Label lblTitle;
        private Label lblStatus;
        private ProgressBar progressBar;
        private Button btnInstall;
        // Suppression du bouton btnSkip
        private bool isInstalling = false;

        // Événement pour notifier que l'installation est terminée
        public event EventHandler<bool> InstallationCompleted;

        public PM2InstallPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblTitle = new Label();
            this.lblStatus = new Label();
            this.progressBar = new ProgressBar();
            this.btnInstall = new Button();
            // Suppression du bouton btnSkip

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTitle.Location = new Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new Size(200, 25);
            this.lblTitle.Text = "Installation de PM2";
            this.Controls.Add(this.lblTitle);

            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new Point(20, 60);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(400, 15);
            this.lblStatus.Text = "PM2 est un gestionnaire de processus pour Node.js. Il permet de maintenir vos applications actives et de les redémarrer automatiquement.";
            this.Controls.Add(this.lblStatus);

            // 
            // progressBar
            // 
            this.progressBar.Location = new Point(20, 100);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(400, 23);
            this.progressBar.Style = ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;
            this.Controls.Add(this.progressBar);

            // 
            // btnInstall
            // 
            this.btnInstall.Location = new Point(270, 140); // Centré horizontalement
            this.btnInstall.Name = "btnInstall";
            this.btnInstall.Size = new Size(120, 30);
            this.btnInstall.Text = "Installer PM2";
            this.btnInstall.UseVisualStyleBackColor = true;
            this.btnInstall.Click += new EventHandler(this.btnInstall_Click);
            this.Controls.Add(this.btnInstall);

            // 
            // PM2InstallPanel
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Name = "PM2InstallPanel";
            this.Size = new Size(640, 320);
        }

        private async void btnInstall_Click(object sender, EventArgs e)
        {
            if (isInstalling) return;

            isInstalling = true;
            btnInstall.Enabled = false;
            // Suppression du bouton btnSkip
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                lblStatus.Text = "Vérification de l'installation de PM2...";
                
                // Vérifier si PM2 est déjà installé
                var pm2Version = await GetPM2VersionAsync();
                if (!string.IsNullOrEmpty(pm2Version))
                {
                    lblStatus.Text = $"✅ PM2 est déjà installé (version: {pm2Version})";
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Value = 100;
                    await Task.Delay(2000);
                    OnInstallationComplete(true);
                    return;
                }

                lblStatus.Text = "PM2 n'est pas installé. Installation en cours...";
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 25;

                // Installer PM2 globalement via npm
                var result = await InstallPM2Async();
                
                if (result)
                {
                    progressBar.Value = 100;
                    var newVersion = await GetPM2VersionAsync();
                    lblStatus.Text = $"✅ PM2 installé avec succès ! (version: {newVersion})";
                    await Task.Delay(2000);
                    OnInstallationComplete(true);
                }
                else
                {
                    lblStatus.Text = "❌ Échec de l'installation de PM2";
                    progressBar.Value = 0;
                    btnInstall.Enabled = true;
                    // Suppression du bouton btnSkip
                    
                    // Afficher une boîte de dialogue d'erreur
                    MessageBox.Show(
                        "L'installation de PM2 a échoué.\n\n" +
                        "Causes possibles :\n" +
                        "• Problème de connexion internet\n" +
                        "• npm n'est pas installé ou accessible\n" +
                        "• Privilèges insuffisants\n" +
                        "• Problème avec le registre npm\n\n" +
                        "Vérifiez que npm fonctionne en tapant 'npm --version' dans une console.",
                        "Erreur d'installation PM2",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Erreur: {ex.Message}";
                progressBar.Value = 0;
                btnInstall.Enabled = true;
                // Suppression du bouton btnSkip
                
                // Afficher une boîte de dialogue d'erreur détaillée
                MessageBox.Show(
                        $"Une erreur inattendue s'est produite lors de l'installation de PM2 :\n\n" +
                        $"Détails : {ex.Message}\n\n" +
                        $"Type d'erreur : {ex.GetType().Name}\n\n" +
                        $"Veuillez réessayer ou contacter le support si le problème persiste.",
                        "Erreur inattendue",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
            }

            isInstalling = false;
        }

        private async Task<bool> InstallPM2Async()
        {
            try
            {
                // Vérifier d'abord que npm est accessible avec une approche plus robuste
                string npmPath = await FindNpmPathAsync();
                
                if (string.IsNullOrEmpty(npmPath))
                {
                    MessageBox.Show(
                        "npm n'a pas été trouvé sur votre système.\n\n" +
                        "Vérifiez que :\n" +
                        "• Node.js est bien installé\n" +
                        "• Le PATH système inclut Node.js\n" +
                        "• Vous avez redémarré votre ordinateur après l'installation\n\n" +
                        "Chemins vérifiés :\n" +
                        "• C:\\Program Files\\nodejs\\\n" +
                        "• C:\\Program Files (x86)\\nodejs\\\n" +
                        "• %APPDATA%\\npm\\\n" +
                        "• PATH système",
                        "npm non trouvé",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }

                // Maintenant installer PM2 avec le chemin complet
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = npmPath,
                        Arguments = "install -g pm2",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                    }
                };

                process.Start();
                
                // Capturer la sortie en temps réel
                string output = "";
                string error = "";
                
                process.OutputDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        output += e.Data + "\n";
                };
                
                process.ErrorDataReceived += (sender, e) => {
                    if (!string.IsNullOrEmpty(e.Data))
                        error += e.Data + "\n";
                };
                
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                // Attendre la fin de l'installation
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    // Attendre un peu et vérifier l'installation
                    await Task.Delay(3000);
                    var version = await GetPM2VersionAsync();
                    return !string.IsNullOrEmpty(version);
                }
                else
                {
                    // Afficher les détails de l'erreur npm
                    string errorMessage = "L'installation de PM2 via npm a échoué.\n\n";
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        errorMessage += $"Erreur npm :\n{error}\n\n";
                    }
                    
                    if (!string.IsNullOrEmpty(output))
                    {
                        errorMessage += $"Sortie npm :\n{output}\n\n";
                    }
                    
                    errorMessage += $"Code de sortie : {process.ExitCode}\n";
                    errorMessage += $"npm utilisé : {npmPath}\n\n";
                    errorMessage += "Causes possibles :\n";
                    errorMessage += "• Problème de connexion internet\n";
                    errorMessage += "• Privilèges insuffisants (essayez de lancer en tant qu'administrateur)\n";
                    errorMessage += "• Problème avec le registre npm\n";
                    errorMessage += "• Conflit avec une installation existante\n\n";
                    errorMessage += "Solutions :\n";
                    errorMessage += "• Relancez le setup wizard en tant qu'administrateur\n";
                    errorMessage += "• Vérifiez votre connexion internet\n";
                    errorMessage += "• Essayez d'installer PM2 manuellement : npm install -g pm2";
                    
                    MessageBox.Show(
                        errorMessage,
                        "Erreur d'installation npm",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Afficher l'erreur de lancement du processus
                MessageBox.Show(
                    $"Une erreur inattendue s'est produite :\n\n" +
                    $"Erreur : {ex.Message}\n\n" +
                    $"Vérifiez que :\n" +
                    $"• Node.js est bien installé\n" +
                    $"• Le setup wizard est lancé en tant qu'administrateur\n" +
                    $"• Votre antivirus n'interfère pas",
                    "Erreur inattendue",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                
                return false;
            }
        }

        private async Task<string> FindNpmPathAsync()
        {
            // Chemins possibles pour npm
            string[] possiblePaths = {
                @"C:\Program Files\nodejs\npm.cmd",
                @"C:\Program Files\nodejs\npm.exe",
                @"C:\Program Files (x86)\nodejs\npm.cmd",
                @"C:\Program Files (x86)\nodejs\npm.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "npm.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "npm.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming", "npm", "npm.cmd"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Roaming", "npm", "npm.exe")
            };

            // Vérifier chaque chemin
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Essayer de trouver via PATH système
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = "npm",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    string[] lines = output.Split('\n');
                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        if (!string.IsNullOrEmpty(trimmedLine) && File.Exists(trimmedLine))
                        {
                            return trimmedLine;
                        }
                    }
                }
            }
            catch
            {
                // Ignorer les erreurs de la commande where
            }

            return null;
        }

        private async Task<string> GetPM2VersionAsync()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "pm2",
                        Arguments = "--version",
                        UseShellExecute = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    // Si PM2 est installé, on ne peut pas capturer la sortie avec UseShellExecute
                    // On va donc faire une vérification supplémentaire
                    return await GetPM2VersionAlternativeAsync();
                }
                else
                {
                    // PM2 n'est pas installé ou accessible
                    return null;
                }
            }
            catch
            {
                // PM2 n'est pas installé (c'est normal)
                return null;
            }
        }

        private async Task<string> GetPM2VersionAlternativeAsync()
        {
            try
            {
                // Utiliser une approche alternative pour obtenir la version
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = "/c pm2 --version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output.Trim()))
                {
                    return output.Trim();
                }

                return "Installé (version non détectée)";
            }
            catch
            {
                return "Installé (version non détectée)";
            }
        }

        private void OnInstallationComplete(bool success)
        {
            // Déclencher l'événement pour notifier le formulaire principal
            InstallationCompleted?.Invoke(this, success);
        }
    }
}
