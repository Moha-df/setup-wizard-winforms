using System.IO;

namespace setup_wizard.Panels
{
    public partial class InstallationPanel : UserControl
    {
        private Label? lblTitle;
        private Label? lblDescription;
        private CheckBox? chkCreateDesktopShortcut;
        private CheckBox? chkStartAfterInstall;
        private CheckBox? chkCreateStartMenuShortcut;
        private Label? lblInstallPath;
        private TextBox? txtInstallPath;
        private Button? btnBrowse;
        private ProgressBar? progressBar;
        private Label? lblStatus;

        public InstallationPanel()
        {
            InitializeComponent();
            InitializeInstallationContent();
        }



        private void InitializeInstallationContent()
        {
            // Title
            lblTitle = new Label
            {
                Text = "Paramètres d'Installation",
                Location = new Point(20, 20),
                Size = new Size(300, 25),
                Font = new Font(this.Font.FontFamily, 14, FontStyle.Bold)
            };
            this.Controls.Add(lblTitle);

            // Description
            lblDescription = new Label
            {
                Text = "Configurez vos préférences d'installation et choisissez le répertoire d'installation.",
                Location = new Point(20, 50),
                Size = new Size(600, 40),
                Font = new Font(this.Font.FontFamily, 9)
            };
            this.Controls.Add(lblDescription);

            // Installation path
            lblInstallPath = new Label
            {
                Text = "Répertoire d'installation :",
                Location = new Point(20, 100),
                Size = new Size(150, 20)
            };
            this.Controls.Add(lblInstallPath);

            txtInstallPath = new TextBox
            {
                Location = new Point(20, 125),
                Size = new Size(450, 23),
                Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "TabletteSystemV2")
            };
            this.Controls.Add(txtInstallPath);

            btnBrowse = new Button
            {
                Text = "Parcourir...",
                Location = new Point(480, 125),
                Size = new Size(80, 23)
            };
            btnBrowse.Click += BtnBrowse_Click;
            this.Controls.Add(btnBrowse);

            // Options
            chkCreateDesktopShortcut = new CheckBox
            {
                Text = "Créer un raccourci sur le bureau",
                Location = new Point(20, 170),
                Size = new Size(200, 20),
                Checked = true
            };
            this.Controls.Add(chkCreateDesktopShortcut);

            chkCreateStartMenuShortcut = new CheckBox
            {
                Text = "Créer un raccourci dans le menu Démarrer",
                Location = new Point(20, 195),
                Size = new Size(200, 20),
                Checked = true
            };
            this.Controls.Add(chkCreateStartMenuShortcut);

            chkStartAfterInstall = new CheckBox
            {
                Text = "Démarrer l'application après l'installation",
                Location = new Point(20, 220),
                Size = new Size(250, 20),
                Checked = true
            };
            this.Controls.Add(chkStartAfterInstall);

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 260),
                Size = new Size(600, 20),
                Visible = false
            };
            this.Controls.Add(progressBar);

            // Status label
            lblStatus = new Label
            {
                Text = "Prêt à installer",
                Location = new Point(20, 290),
                Size = new Size(600, 20),
                Font = new Font(this.Font.FontFamily, 9)
            };
            this.Controls.Add(lblStatus);
        }

        private void BtnBrowse_Click(object? sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Sélectionner le répertoire d'installation";
                if (txtInstallPath != null)
                {
                    folderDialog.SelectedPath = txtInstallPath.Text;
                }

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    if (txtInstallPath != null)
                    {
                        txtInstallPath.Text = folderDialog.SelectedPath;
                    }
                }
            }
        }

        public async Task<bool> PerformInstallation()
        {
            if (progressBar != null) progressBar.Visible = true;
            if (progressBar != null) progressBar.Value = 0;
            if (lblStatus != null) lblStatus.Text = "Démarrage de l'installation...";

            try
            {
                // Simulate installation steps
                await SimulateInstallationStep("Création des répertoires...", 20);
                await SimulateInstallationStep("Copie des fichiers...", 40);
                await SimulateInstallationStep("Création des raccourcis...", 60);
                await SimulateInstallationStep("Mise à jour du registre...", 80);
                await SimulateInstallationStep("Finalisation de l'installation...", 100);

                if (lblStatus != null) lblStatus.Text = "Installation terminée avec succès !";
                return true;
            }
            catch (Exception ex)
            {
                if (lblStatus != null) lblStatus.Text = $"Installation échouée : {ex.Message}";
                return false;
            }
            finally
            {
                if (progressBar != null) progressBar.Visible = false;
            }
        }

        private async Task SimulateInstallationStep(string message, int progress)
        {
            if (lblStatus != null) lblStatus.Text = message;
            if (progressBar != null) progressBar.Value = progress;
            await Task.Delay(500); // Simulate work
        }

        public string GetInstallPath()
        {
            return txtInstallPath?.Text ?? string.Empty;
        }

        public bool ShouldCreateDesktopShortcut()
        {
            return chkCreateDesktopShortcut?.Checked ?? false;
        }

        public bool ShouldCreateStartMenuShortcut()
        {
            return chkCreateStartMenuShortcut?.Checked ?? false;
        }

        public bool ShouldStartAfterInstall()
        {
            return chkStartAfterInstall?.Checked ?? false;
        }
    }
}
