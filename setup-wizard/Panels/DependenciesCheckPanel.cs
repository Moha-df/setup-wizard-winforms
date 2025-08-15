using System.Diagnostics;
using System.IO;
using setup_wizard.Utils;

namespace setup_wizard.Panels
{
    public partial class DependenciesCheckPanel : UserControl
    {
        private Label? lblTitle;
        private Label? lblDescription;
        private ListView? listDependencies;
        private Button? btnCheckAll;
        private Button? btnInstallSelected;
        private ProgressBar? progressBar;
        private Label? lblStatus;
        private List<DependencyInfo>? dependencies;

        // Événement pour notifier que toutes les dépendances sont installées
        public event EventHandler<bool> AllDependenciesInstalled;

        public DependenciesCheckPanel()
        {
            InitializeComponent();
            InitializeDependenciesContent();
        }



        private void InitializeDependenciesContent()
        {
            this.SuspendLayout();

            // Title
            lblTitle = new Label
            {
                Text = "Vérification des Dépendances",
                Location = new Point(20, 20),
                Size = new Size(300, 25),
                Font = new Font(this.Font.FontFamily, 14, FontStyle.Bold)
            };
            this.Controls.Add(lblTitle);

            // Description
            lblDescription = new Label
            {
                Text = "Les dépendances suivantes sont requises pour Tablette System V2. Vérifiez si elles sont installées et installez celles qui manquent.",
                Location = new Point(20, 50),
                Size = new Size(600, 40),
                Font = new Font(this.Font.FontFamily, 9)
            };
            this.Controls.Add(lblDescription);

            // Dependencies list
            listDependencies = new ListView
            {
                Location = new Point(20, 100),
                Size = new Size(600, 150),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listDependencies.Columns.Add("Dépendance", 150);
            listDependencies.Columns.Add("Statut", 100);
            listDependencies.Columns.Add("Version", 150);
            listDependencies.Columns.Add("Action", 100);
            this.Controls.Add(listDependencies);

            // Check All button
            btnCheckAll = new Button
            {
                Text = "Vérifier Toutes les Dépendances",
                Location = new Point(20, 270),
                Size = new Size(200, 30)
            };
            btnCheckAll.Click += BtnCheckAll_Click;
            this.Controls.Add(btnCheckAll);

            // Install Selected button
            btnInstallSelected = new Button
            {
                Text = "Installer la Sélection",
                Location = new Point(240, 270),
                Size = new Size(150, 30),
                Enabled = false
            };
            btnInstallSelected.Click += BtnInstallSelected_Click;
            this.Controls.Add(btnInstallSelected);

            // Progress bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 310),
                Size = new Size(600, 20),
                Visible = false
            };
            this.Controls.Add(progressBar);

            // Status label
            lblStatus = new Label
            {
                Text = "Prêt à vérifier les dépendances",
                Location = new Point(20, 340),
                Size = new Size(600, 20),
                Font = new Font(this.Font.FontFamily, 9)
            };
            this.Controls.Add(lblStatus);

            // Get dependencies from utility class
            dependencies = DependencyChecker.GetDefaultDependencies();

            foreach (var dep in dependencies)
            {
                var item = new ListViewItem(dep.Name);
                item.SubItems.Add("Inconnu");
                item.SubItems.Add("");
                item.SubItems.Add("Vérifier");
                item.Tag = dep;
                if (listDependencies != null)
                {
                    listDependencies.Items.Add(item);
                }
            }

            this.ResumeLayout(false);
        }

        private async void BtnCheckAll_Click(object? sender, EventArgs e)
        {
            if (btnCheckAll != null) btnCheckAll.Enabled = false;
            if (btnInstallSelected != null) btnInstallSelected.Enabled = false;
            if (progressBar != null) progressBar.Visible = true;
            if (progressBar != null) progressBar.Value = 0;
            if (lblStatus != null) lblStatus.Text = "Vérification des dépendances...";

            int checkedCount = 0;
            if (listDependencies != null)
            {
                foreach (ListViewItem item in listDependencies.Items)
                {
                    var dep = item.Tag as DependencyInfo;
                    if (dep != null)
                    {
                        await CheckDependency(item, dep);
                        checkedCount++;
                        if (progressBar != null) progressBar.Value = (checkedCount * 100) / listDependencies.Items.Count;
                    }
                }
            }

            if (progressBar != null) progressBar.Visible = false;
            if (btnCheckAll != null) btnCheckAll.Enabled = true;
            UpdateInstallButtonState();
            if (lblStatus != null) lblStatus.Text = "Vérification des dépendances terminée";
        }

        private async Task CheckDependency(ListViewItem item, DependencyInfo dep)
        {
            try
            {
                if (lblStatus != null) lblStatus.Text = $"Vérification de {dep.Name}...";
                
                var result = await DependencyChecker.CheckDependencyAsync(dep);
                
                if (result.IsInstalled)
                {
                    item.SubItems[1].Text = "Installé";
                    item.SubItems[2].Text = result.Version;
                    item.SubItems[3].Text = "OK";
                    item.BackColor = Color.LightGreen;
                    
                    if (lblStatus != null) lblStatus.Text = $"{dep.Name} : Installé (v{result.Version})";
                }
                else
                {
                    item.SubItems[1].Text = "Non Trouvé";
                    item.SubItems[2].Text = "";
                    item.SubItems[3].Text = "Installer";
                    item.BackColor = Color.LightCoral;
                    
                    if (lblStatus != null) lblStatus.Text = $"{dep.Name} : Non trouvé";
                }
            }
            catch (Exception ex)
            {
                item.SubItems[1].Text = "Erreur";
                item.SubItems[2].Text = "";
                item.SubItems[3].Text = "Réessayer";
                item.BackColor = Color.Yellow;
                
                if (lblStatus != null) lblStatus.Text = $"Erreur lors de la vérification de {dep.Name} : {ex.Message}";
                
                // Log l'erreur pour le débogage
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification de {dep.Name}: {ex.Message}");
            }
        }

        private async void BtnInstallSelected_Click(object? sender, EventArgs e)
        {
            var missingDeps = new List<DependencyInfo>();
            if (listDependencies != null)
            {
                foreach (ListViewItem item in listDependencies.Items)
                {
                    if (item.SubItems[1].Text == "Non Trouvé")
                    {
                        var dep = item.Tag as DependencyInfo;
                        if (dep != null)
                        {
                            missingDeps.Add(dep);
                        }
                    }
                }
            }

            if (missingDeps.Count > 0)
            {
                var result = MessageBox.Show(
                    $"Les dépendances suivantes doivent être installées :\n\n{string.Join("\n", missingDeps.Select(d => d.Name))}\n\nVoulez-vous les installer automatiquement ?",
                    "Installer les Dépendances",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    await InstallMissingDependenciesAsync(missingDeps);
                }
            }
        }

        private async Task InstallMissingDependenciesAsync(List<DependencyInfo> missingDeps)
        {
            if (btnInstallSelected != null) btnInstallSelected.Enabled = false;
            if (btnCheckAll != null) btnCheckAll.Enabled = false;
            if (progressBar != null) progressBar.Visible = true;
            if (lblStatus != null) lblStatus.Text = "Installation en cours...";

            var progress = new Progress<string>(message =>
            {
                if (lblStatus != null) lblStatus.Text = message;
            });

            try
            {
                var result = await DependencyInstaller.InstallAllMissingDependenciesAsync(missingDeps, progress);
                
                if (result.AllSuccessful)
                {
                    if (lblStatus != null) lblStatus.Text = "Installation terminée avec succès !";
                    MessageBox.Show("Toutes les dépendances ont été installées avec succès !", "Installation Réussie", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Re-vérifier les dépendances
                    await RefreshDependenciesAsync();
                }
                else
                {
                    if (lblStatus != null) lblStatus.Text = "Certaines installations ont échoué";
                    
                    // Afficher les détails des erreurs
                    ShowInstallationDetails(result);
                }
            }
            catch (Exception ex)
            {
                if (lblStatus != null) lblStatus.Text = $"Erreur lors de l'installation: {ex.Message}";
                MessageBox.Show($"Erreur lors de l'installation: {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (progressBar != null) progressBar.Visible = false;
                if (btnInstallSelected != null) btnInstallSelected.Enabled = true;
                if (btnCheckAll != null) btnCheckAll.Enabled = true;
            }
        }

        private async Task RefreshDependenciesAsync()
        {
            if (listDependencies != null)
            {
                foreach (ListViewItem item in listDependencies.Items)
                {
                    var dep = item.Tag as DependencyInfo;
                    if (dep != null)
                    {
                        await CheckDependency(item, dep);
                    }
                }
            }
            UpdateInstallButtonState();
        }

        private void UpdateInstallButtonState()
        {
            bool hasMissingDeps = false;
            if (listDependencies != null)
            {
                foreach (ListViewItem item in listDependencies.Items)
                {
                    if (item.SubItems[1].Text == "Non Trouvé")
                    {
                        hasMissingDeps = true;
                        break;
                    }
                }
            }
            if (btnInstallSelected != null) btnInstallSelected.Enabled = hasMissingDeps;
            
            // Notifier si toutes les dépendances sont installées
            AllDependenciesInstalled?.Invoke(this, !hasMissingDeps);
        }

        private void ShowInstallationDetails(InstallationResult result)
        {
            var failedInstallations = result.Results.Where(r => !r.Success).ToList();
            
            if (failedInstallations.Count == 0) return;

            var details = new List<string>();
            foreach (var failed in failedInstallations)
            {
                details.Add($"❌ {failed.DependencyName}:");
                details.Add($"   Erreur: {failed.ErrorMessage}");
                if (!string.IsNullOrEmpty(failed.LogOutput))
                {
                    details.Add($"   Log: {failed.LogOutput}");
                }
                details.Add($"   Code de sortie: {failed.ExitCode}");
                details.Add("");
            }

            var message = $"Installation partielle - {failedInstallations.Count} dépendance(s) n'ont pas pu être installées:\n\n{string.Join("\n", details)}";
            
            // Créer une fenêtre de dialogue personnalisée pour afficher les détails
            var detailForm = new Form
            {
                Text = "Détails des Erreurs d'Installation",
                Size = new Size(700, 500),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Text = message,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9)
            };

            detailForm.Controls.Add(textBox);
            
            MessageBox.Show(message, "Installation Partielle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
