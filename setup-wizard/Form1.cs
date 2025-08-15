using System.Diagnostics;
using setup_wizard.Panels;

namespace setup_wizard
{
    public partial class Form1 : Form
    {
        private List<UserControl> panels = new List<UserControl>();
        private int currentPanelIndex = 0;
        private Button btnNext = null!;
        private Button btnCancel = null!;
        private Label lblStepInfo = null!;
        private string? clonedProjectPath = null; // Pour stocker le chemin du projet cloné

        public Form1()
        {
            InitializeComponent();
            InitializeWizard();
        }

        private void InitializeWizard()
        {
            this.Text = "Tablette System V2 - Assistant d'Installation";
            this.Size = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Initialize panels
            panels = new List<UserControl>
            {
                new WelcomePanel(),
                new DependenciesCheckPanel(),
                new PM2InstallPanel(),
                new GitClonePanel(),
                new DeploymentPanel(),
                new SchedulerPanel(),
                new FinishPanel()
            };

            // Create navigation controls
            CreateNavigationControls();

            // Show first panel
            ShowPanel(0);
        }

        private void CreateNavigationControls()
        {
            // Step info label
            lblStepInfo = new Label
            {
                Text = "Étape 1 sur " + panels.Count,
                Location = new Point(20, 20),
                Size = new Size(200, 20),
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Bold)
            };
            this.Controls.Add(lblStepInfo);

            // Cancel button
            btnCancel = new Button
            {
                Text = "Annuler",
                Location = new Point(500, 400),
                Size = new Size(80, 30)
            };
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);

            // Next button (repositionné au centre)
            btnNext = new Button
            {
                Text = "Suivant",
                Location = new Point(380, 400), // Plus proche du bouton Cancel
                Size = new Size(80, 30)
            };
            btnNext.Click += (s, e) => NextPanel();
            this.Controls.Add(btnNext);
        }

        private void ShowPanel(int index)
        {
            // Hide current panel
            if (currentPanelIndex < panels.Count)
            {
                panels[currentPanelIndex].Visible = false;
            }

            currentPanelIndex = index;

            // Show new panel
            if (currentPanelIndex < panels.Count)
            {
                UserControl panel = panels[currentPanelIndex];
                panel.Location = new Point(20, 60);
                panel.Size = new Size(640, 320);
                panel.Visible = true;
                this.Controls.Add(panel);

                // S'abonner aux événements spécifiques du panel
                SubscribeToPanelEvents(panel);
                
                // Si c'est le panel de déploiement, lui passer le chemin du projet cloné
                if (panel is setup_wizard.Panels.DeploymentPanel deploymentPanel)
                {
                    // Récupérer le chemin du projet cloné depuis l'étape précédente
                    string clonedProjectPath = GetClonedProjectPath();
                    if (!string.IsNullOrEmpty(clonedProjectPath))
                    {
                        deploymentPanel.SetProjectPath(clonedProjectPath);
                    }
                }

                // Update step info
                lblStepInfo.Text = $"Étape {currentPanelIndex + 1} sur {panels.Count}";

                // Update button states
                btnNext.Text = currentPanelIndex == panels.Count - 1 ? "Terminer" : "Suivant";
                
                // Désactiver le bouton Next sur l'étape PM2 jusqu'à l'installation
                if (currentPanelIndex == 2) // Index de l'étape PM2
                {
                    btnNext.Enabled = false;
                }
                // Désactiver le bouton Next sur l'étape des dépendances jusqu'à ce que tout soit installé
                else if (currentPanelIndex == 1) // Index de l'étape des dépendances
                {
                    btnNext.Enabled = false;
                }
                // Désactiver le bouton Next sur l'étape du clonage GitHub jusqu'à ce que ce soit réussi
                else if (currentPanelIndex == 3) // Index de l'étape du clonage GitHub
                {
                    btnNext.Enabled = false;
                }
                // Désactiver le bouton Next sur l'étape de déploiement jusqu'à ce que ce soit réussi
                else if (currentPanelIndex == 4) // Index de l'étape de déploiement
                {
                    btnNext.Enabled = false;
                }
                // Désactiver le bouton Next sur l'étape du planificateur de tâches jusqu'à ce que ce soit réussi
                else if (currentPanelIndex == 5) // Index de l'étape du planificateur de tâches
                {
                    btnNext.Enabled = false;
                }
                else
                {
                    btnNext.Enabled = true;
                }
            }
        }

        private void SubscribeToPanelEvents(UserControl panel)
        {
            // S'abonner aux événements du PM2InstallPanel
            if (panel is PM2InstallPanel pm2Panel)
            {
                pm2Panel.InstallationCompleted += (sender, success) =>
                {
                    if (success)
                    {
                        // Activer le bouton Next pour que l'utilisateur puisse continuer manuellement
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() => {
                                btnNext.Enabled = true;
                                btnNext.Focus(); // Donner le focus au bouton Next
                            }));
                        }
                        else
                        {
                            btnNext.Enabled = true;
                            btnNext.Focus();
                        }
                    }
                };
            }
            
            // S'abonner aux événements du DependenciesCheckPanel
            if (panel is DependenciesCheckPanel depsPanel)
            {
                depsPanel.AllDependenciesInstalled += (sender, allInstalled) =>
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => {
                            btnNext.Enabled = allInstalled;
                        }));
                    }
                    else
                    {
                        btnNext.Enabled = allInstalled;
                    }
                };
            }
            
            // S'abonner aux événements du GitClonePanel
            if (panel is GitClonePanel gitPanel)
            {
                gitPanel.CloneCompleted += (sender, success) =>
                {
                    if (success)
                    {
                        // Stocker le chemin du projet cloné
                        if (gitPanel is setup_wizard.Panels.GitClonePanel gitClonePanel)
                        {
                            // Récupérer le chemin depuis le panel (on peut l'ajouter comme propriété)
                            clonedProjectPath = GetProjectPathFromGitPanel(gitClonePanel);
                        }
                        
                        // Activer le bouton Next quand le clonage est réussi
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() => {
                                btnNext.Enabled = true;
                                btnNext.Focus();
                            }));
                        }
                        else
                        {
                            btnNext.Enabled = true;
                            btnNext.Focus();
                        }
                    }
                };
            }
            
            // S'abonner aux événements du DeploymentPanel
            if (panel is setup_wizard.Panels.DeploymentPanel deploymentPanel)
            {
                deploymentPanel.DeploymentCompleted += (sender, success) =>
                {
                    if (success)
                    {
                        // Activer le bouton Next quand le déploiement est réussi
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() => {
                                btnNext.Enabled = true;
                                btnNext.Focus();
                            }));
                        }
                        else
                        {
                            btnNext.Enabled = true;
                            btnNext.Focus();
                        }
                    }
                };
            }
            
            // S'abonner aux événements du SchedulerPanel
            if (panel is setup_wizard.Panels.SchedulerPanel schedulerPanel)
            {
                schedulerPanel.SchedulerCompleted += (sender, success) =>
                {
                    if (success)
                    {
                        // Activer le bouton Next quand la configuration est réussie
                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() => {
                                btnNext.Enabled = true;
                                btnNext.Focus();
                            }));
                        }
                        else
                        {
                            btnNext.Enabled = true;
                            btnNext.Focus();
                        }
                    }
                };
            }
        }

        private void NextPanel()
        {
            if (currentPanelIndex < panels.Count - 1)
            {
                ShowPanel(currentPanelIndex + 1);
            }
            else
            {
                // Finish setup
                MessageBox.Show("Installation terminée avec succès !", "Installation Terminée", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
        }
        
        // Méthode pour récupérer le chemin du projet cloné
        private string? GetClonedProjectPath()
        {
            return clonedProjectPath;
        }
        
        // Méthode pour récupérer le chemin depuis le GitClonePanel
        private string GetProjectPathFromGitPanel(setup_wizard.Panels.GitClonePanel gitPanel)
        {
            // Utiliser la propriété publique du GitClonePanel
            return gitPanel.ClonedProjectPath;
        }
    }
}
