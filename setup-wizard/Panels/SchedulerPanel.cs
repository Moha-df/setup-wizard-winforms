using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace setup_wizard.Panels
{
    public partial class SchedulerPanel : UserControl
    {
        private Label lblTitle;
        private Label lblStatus;
        private Label lblDescription;
        private Button btnConfigure;
        private ProgressBar progressBar;
        private bool isConfiguring = false;

        // Événement pour notifier que la configuration est terminée
        public event EventHandler<bool> SchedulerCompleted;

        public SchedulerPanel()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblTitle = new Label();
            this.lblStatus = new Label();
            this.lblDescription = new Label();
            this.btnConfigure = new Button();
            this.progressBar = new ProgressBar();

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTitle.Location = new Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new Size(300, 25);
            this.lblTitle.Text = "Configuration du Planificateur de Tâches";
            this.Controls.Add(this.lblTitle);

            // 
            // lblDescription
            // 
            this.lblDescription.AutoSize = true;
            this.lblDescription.Location = new Point(20, 50);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new Size(500, 30);
            this.lblDescription.Text = "Configuration du planificateur de tâches Windows pour que PM2 resurrect se lance automatiquement au démarrage du système.\nCela garantit que votre application redémarre automatiquement après un redémarrage.";
            this.Controls.Add(this.lblDescription);

            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new Point(20, 100);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(400, 15);
            this.lblStatus.Text = "Cliquez sur 'Configurer' pour créer la tâche planifiée";
            this.Controls.Add(this.lblStatus);

            // 
            // btnConfigure
            // 
            this.btnConfigure.Location = new Point(20, 140);
            this.btnConfigure.Name = "btnConfigure";
            this.btnConfigure.Size = new Size(120, 30);
            this.btnConfigure.Text = "Configurer";
            this.btnConfigure.UseVisualStyleBackColor = true;
            this.btnConfigure.Click += new EventHandler(this.btnConfigure_Click);
            this.Controls.Add(this.btnConfigure);

            // 
            // progressBar
            // 
            this.progressBar.Location = new Point(20, 180);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(500, 23);
            this.progressBar.Style = ProgressBarStyle.Continuous;
            this.progressBar.Value = 0;
            this.Controls.Add(this.progressBar);

            // 
            // SchedulerPanel
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Name = "SchedulerPanel";
            this.Size = new Size(640, 280);
        }

        private async void btnConfigure_Click(object sender, EventArgs e)
        {
            if (isConfiguring) return;

            isConfiguring = true;
            btnConfigure.Enabled = false;
            progressBar.Value = 0;

            try
            {
                lblStatus.Text = "Vérification des privilèges administrateur...";
                progressBar.Value = 20;

                if (!IsRunningAsAdministrator())
                {
                    throw new Exception("Privilèges administrateur requis pour configurer le planificateur de tâches");
                }

                lblStatus.Text = "Création de la tâche planifiée...";
                progressBar.Value = 40;

                var createResult = await CreateScheduledTaskAsync();
                if (!createResult)
                {
                    throw new Exception("Échec de la création de la tâche planifiée");
                }

                lblStatus.Text = "Configuration de la tâche...";
                progressBar.Value = 60;

                var configResult = await ConfigureScheduledTaskAsync();
                if (!configResult)
                {
                    throw new Exception("Échec de la configuration de la tâche");
                }

                lblStatus.Text = "Test de la tâche...";
                progressBar.Value = 80;

                var testResult = await TestScheduledTaskAsync();
                if (!testResult)
                {
                    throw new Exception("Échec du test de la tâche");
                }

                progressBar.Value = 100;
                lblStatus.Text = "✅ Planificateur de tâches configuré avec succès ! La tâche PM2Resurrect est maintenant active.";
                await Task.Delay(3000);
                OnConfigurationComplete(true);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Erreur: {ex.Message}";
                progressBar.Value = 0;
                btnConfigure.Enabled = true;
                
                // Afficher une boîte de dialogue d'erreur détaillée
                ShowSchedulerErrorDialog(ex.Message);
            }

            isConfiguring = false;
        }

        private bool IsRunningAsAdministrator()
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

        private async Task<bool> CreateScheduledTaskAsync()
        {
            try
            {
                // Supprimer l'ancienne tâche si elle existe pour éviter les conflits de paramètres
                var deleteProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = "/delete /tn \"PM2Resurrect\" /f",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                try
                {
                    deleteProcess.Start();
                    await deleteProcess.WaitForExitAsync();
                }
                catch { }

                // Exécuter de façon interactive à l'ouverture de session (fenêtre visible)
                var taskAction = "\"%ComSpec%\" /k \"%APPDATA%\\npm\\pm2.cmd\" resurrect";

                var createProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        // Tâche interactive à l'ouverture de session de l'utilisateur courant
                        Arguments = $"/create /tn \"PM2Resurrect\" /tr \"{taskAction}\" /sc onlogon /rl HIGHEST /it /f",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                createProcess.Start();
                await createProcess.WaitForExitAsync();

                return createProcess.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> ConfigureScheduledTaskAsync()
        {
            try
            {
                // Activer la tâche
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = "/change /tn \"PM2Resurrect\" /enable",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestScheduledTaskAsync()
        {
            try
            {
                // Lancer la tâche maintenant
                var runProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = "/run /tn \"PM2Resurrect\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                runProcess.Start();
                await runProcess.WaitForExitAsync();

                // Si la commande /run a réussi (acceptée par le planificateur), on considère le test comme OK
                // car l'action est \"cmd /k\" et ne terminera pas immédiatement.
                return runProcess.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private void ShowSchedulerErrorDialog(string errorMessage)
        {
            string fullMessage = "La configuration du planificateur de tâches a échoué.\n\n";
            fullMessage += $"Erreur: {errorMessage}\n\n";
            fullMessage += "Solutions possibles:\n";
            fullMessage += "• Lancez le wizard en tant qu'administrateur\n";
            fullMessage += "• Vérifiez que le service Planificateur de tâches est actif\n";
            fullMessage += "• Vérifiez que PM2 est installé globalement\n\n";
            fullMessage += "Vous pouvez réessayer la configuration en cliquant sur le bouton 'Configurer'.";

            MessageBox.Show(fullMessage, "Erreur de Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void OnConfigurationComplete(bool success)
        {
            // Désactiver le bouton et afficher le succès
            btnConfigure.Enabled = false;
            btnConfigure.Text = "✅ Terminé";
            
            // Déclencher l'événement pour activer le bouton Next
            SchedulerCompleted?.Invoke(this, success);
        }
    }
}
