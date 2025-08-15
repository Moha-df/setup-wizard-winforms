using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace setup_wizard.Panels
{
    public partial class DeploymentPanel : UserControl
    {
        private Label lblTitle;
        private Label lblStatus;
        private Label lblProjectPath;
        private TextBox txtProjectPath;
        private Button btnDeploy;
        private ProgressBar progressBar;
        private bool isDeploying = false;

        // Événement pour notifier que le déploiement est terminé
        public event EventHandler<bool> DeploymentCompleted;

        public DeploymentPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblTitle = new Label();
            this.lblStatus = new Label();
            this.lblProjectPath = new Label();
            this.txtProjectPath = new TextBox();
            this.btnDeploy = new Button();
            this.progressBar = new ProgressBar();

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTitle.Location = new Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new Size(300, 25);
            this.lblTitle.Text = "Déploiement de l'Application";
            this.Controls.Add(this.lblTitle);

            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new Point(20, 50);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(500, 15);
            this.lblStatus.Text = "Prêt à déployer l'application TabletteSystemV2";
            this.Controls.Add(this.lblStatus);

            // 
            // lblProjectPath
            // 
            this.lblProjectPath.AutoSize = true;
            this.lblProjectPath.Location = new Point(20, 80);
            this.lblProjectPath.Name = "lblProjectPath";
            this.lblProjectPath.Size = new Size(150, 15);
            this.lblProjectPath.Text = "Dossier du projet:";
            this.Controls.Add(this.lblProjectPath);

            // 
            // txtProjectPath
            // 
            this.txtProjectPath.Location = new Point(180, 80);
            this.txtProjectPath.Name = "txtProjectPath";
            this.txtProjectPath.Size = new Size(300, 23);
            this.txtProjectPath.Text = "";
            this.txtProjectPath.ReadOnly = true;
            this.txtProjectPath.BackColor = Color.LightGray;
            this.Controls.Add(this.txtProjectPath);

            // 
            // btnDeploy
            // 
            this.btnDeploy.Location = new Point(20, 120);
            this.btnDeploy.Name = "btnDeploy";
            this.btnDeploy.Size = new Size(120, 30);
            this.btnDeploy.Text = "Déployer";
            this.btnDeploy.UseVisualStyleBackColor = true;
            this.btnDeploy.Click += new EventHandler(this.btnDeploy_Click);
            this.Controls.Add(this.btnDeploy);

            // 
            // progressBar
            // 
            this.progressBar.Location = new Point(20, 160);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(500, 23);
            this.progressBar.Style = ProgressBarStyle.Continuous;
            this.progressBar.Value = 0;
            this.Controls.Add(this.progressBar);

            // 
            // DeploymentPanel
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Name = "DeploymentPanel";
            this.Size = new Size(640, 280);
        }

        // Méthode pour définir le chemin du projet cloné
        public void SetProjectPath(string projectPath)
        {
            if (txtProjectPath != null)
            {
                txtProjectPath.Text = projectPath;
            }
        }

        private async void btnDeploy_Click(object sender, EventArgs e)
        {
            if (isDeploying) return;

            // Validation du chemin
            if (string.IsNullOrWhiteSpace(txtProjectPath.Text))
            {
                MessageBox.Show("Aucun dossier de projet défini. Assurez-vous d'avoir cloné le repository à l'étape précédente.", 
                              "Dossier manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!Directory.Exists(txtProjectPath.Text))
            {
                MessageBox.Show("Le dossier du projet n'existe pas. Assurez-vous d'avoir cloné le repository à l'étape précédente.", 
                              "Dossier invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Vérifier que c'est bien le dossier cloné
            string expectedPath = Path.Combine(txtProjectPath.Text, "TabletteSystemV2");
            if (!Directory.Exists(expectedPath))
            {
                MessageBox.Show("Le dossier sélectionné ne semble pas être le bon. Assurez-vous d'avoir cloné le repository à l'étape précédente.", 
                              "Dossier invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(Path.Combine(expectedPath, "package.json")))
            {
                MessageBox.Show("Le dossier ne contient pas de package.json. Assurez-vous d'avoir cloné le bon repository.", 
                              "Projet Node.js invalide", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isDeploying = true;
            btnDeploy.Enabled = false;
            progressBar.Value = 0;

            try
            {
                // Utiliser le dossier TabletteSystemV2 à l'intérieur
                string workingDirectory = expectedPath;

                // Étape 1: npm install --legacy-peer-deps
                lblStatus.Text = "📦 Installation des dépendances...";
                progressBar.Value = 20;
                
                var installResult = await RunNpmCommand("install --legacy-peer-deps", workingDirectory);
                if (!installResult)
                {
                    throw new Exception("L'installation des dépendances a échoué. Vérifiez votre connexion internet et réessayez.");
                }

                // Étape 2: npm run build
                lblStatus.Text = "🔨 Construction de l'application...";
                progressBar.Value = 50;
                
                var buildResult = await RunNpmCommand("run build", workingDirectory);
                if (!buildResult)
                {
                    throw new Exception("La construction de l'application a échoué. Vérifiez que le projet compile correctement.");
                }

                // Étape 3: PM2 start deploy.json
                lblStatus.Text = "🚀 Démarrage de l'application avec PM2...";
                progressBar.Value = 80;
                
                var pm2StartResult = await RunPM2Command("start deploy.json", workingDirectory);
                if (!pm2StartResult)
                {
                    throw new Exception("Le démarrage avec PM2 a échoué. Vérifiez que le fichier deploy.json existe et est valide.");
                }

                // Étape 4: PM2 save
                lblStatus.Text = "💾 Sauvegarde de la configuration PM2...";
                progressBar.Value = 90;
                
                var pm2SaveResult = await RunPM2Command("save", workingDirectory);
                if (!pm2SaveResult)
                {
                    throw new Exception("La sauvegarde PM2 a échoué. L'application peut ne pas redémarrer automatiquement.");
                }

                progressBar.Value = 100;
                lblStatus.Text = "✅ Déploiement terminé avec succès ! L'application est maintenant en cours d'exécution.";
                await Task.Delay(3000);
                OnDeploymentComplete(true);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Erreur lors du déploiement: {ex.Message}";
                progressBar.Value = 0;
                btnDeploy.Enabled = true;
                
                // Afficher une boîte de dialogue d'erreur détaillée
                ShowDeploymentErrorDialog(ex.Message);
            }

            isDeploying = false;
        }

        private async Task<bool> RunNpmCommand(string arguments, string workingDirectory)
        {
            try
            {
                // Trouver le chemin complet de npm
                string npmPath = await FindNpmPathAsync();
                if (string.IsNullOrEmpty(npmPath))
                {
                    throw new Exception("npm n'a pas été trouvé sur votre système. Assurez-vous que Node.js est installé.");
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = npmPath,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                
                // Capturer la sortie pour détecter les erreurs
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    // Analyser l'erreur pour donner un message plus clair
                    if (error.Contains("ENOTFOUND") || error.Contains("network"))
                    {
                        throw new Exception("Problème de connexion réseau. Vérifiez votre connexion internet.");
                    }
                    else if (error.Contains("EACCES") || error.Contains("permission"))
                    {
                        throw new Exception("Problème de permissions. Essayez de lancer le wizard en tant qu'administrateur.");
                    }
                    else if (error.Contains("ENOENT") || error.Contains("not found"))
                    {
                        throw new Exception("Fichier ou dossier manquant. Le projet semble incomplet.");
                    }
                    else
                    {
                        throw new Exception($"Erreur npm: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'exécution de npm: {ex.Message}");
            }
        }

        private async Task<bool> RunPM2Command(string arguments, string workingDirectory)
        {
            try
            {
                // Trouver le chemin complet de pm2
                string pm2Path = await FindPM2PathAsync();
                if (string.IsNullOrEmpty(pm2Path))
                {
                    throw new Exception("PM2 n'a pas été trouvé sur votre système. Assurez-vous que PM2 est installé globalement.");
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pm2Path,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
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

                if (process.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    // Analyser l'erreur PM2
                    if (error.Contains("deploy.json") || error.Contains("not found"))
                    {
                        throw new Exception("Le fichier deploy.json est manquant ou invalide.");
                    }
                    else if (error.Contains("already running"))
                    {
                        // Si PM2 est déjà en cours, c'est OK
                        return true;
                    }
                    else
                    {
                        throw new Exception($"Erreur PM2: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur lors de l'exécution de PM2: {ex.Message}");
            }
        }

        private void ShowDeploymentErrorDialog(string errorMessage)
        {
            string fullMessage = "Le déploiement a échoué.\n\n";
            fullMessage += $"Erreur: {errorMessage}\n\n";
            fullMessage += "Solutions possibles:\n";
            fullMessage += "• Vérifiez votre connexion internet\n";
            fullMessage += "• Assurez-vous que Node.js et npm sont installés\n";
            fullMessage += "• Vérifiez que PM2 est installé globalement\n";
            fullMessage += "• Essayez de lancer le wizard en tant qu'administrateur\n\n";
            fullMessage += "Vous pouvez réessayer le déploiement en cliquant sur le bouton 'Déployer'.";

            MessageBox.Show(fullMessage, "Erreur de Déploiement", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void OnDeploymentComplete(bool success)
        {
            // Désactiver le bouton et afficher le succès
            btnDeploy.Enabled = false;
            btnDeploy.Text = "✅ Terminé";
            
            // Déclencher l'événement pour activer le bouton Next
            DeploymentCompleted?.Invoke(this, success);
        }
        
        // Méthode pour trouver le chemin de npm
        private async Task<string> FindNpmPathAsync()
        {
            try
            {
                // Vérifier d'abord les chemins communs
                string[] commonPaths = {
                    @"C:\Program Files\nodejs\npm.cmd",
                    @"C:\Program Files\nodejs\npm.exe",
                    @"C:\Program Files (x86)\nodejs\npm.cmd",
                    @"C:\Program Files (x86)\nodejs\npm.exe",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\npm\npm.cmd"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\npm\npm.exe")
                };

                foreach (string path in commonPaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                // Utiliser la commande 'where' pour trouver npm
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
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    // Prendre le premier chemin trouvé
                    string[] paths = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (paths.Length > 0)
                    {
                        return paths[0].Trim();
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        
        // Méthode pour trouver le chemin de PM2
        private async Task<string> FindPM2PathAsync()
        {
            try
            {
                // Vérifier d'abord les chemins communs
                string[] commonPaths = {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\npm\pm2.cmd"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\Roaming\npm\pm2.exe"),
                    @"C:\Program Files\nodejs\pm2.cmd",
                    @"C:\Program Files\nodejs\pm2.exe"
                };

                foreach (string path in commonPaths)
                {
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }

                // Utiliser la commande 'where' pour trouver pm2
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "where",
                        Arguments = "pm2",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    // Prendre le premier chemin trouvé
                    string[] paths = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (paths.Length > 0)
                    {
                        return paths[0].Trim();
                    }
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
