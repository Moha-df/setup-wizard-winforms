using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace setup_wizard.Panels
{
    public partial class WelcomePanel : UserControl
    {
        private Label? lblTitle;
        private Label? lblWelcome;
        private Label? lblDescription;
        private Label? lblFeatures;
        private Label? lblSystemRequirements;

        public WelcomePanel()
        {
            InitializeComponent();
            InitializeWelcomeContent();
        }

        private void InitializeWelcomeContent()
        {
            this.SuspendLayout();

            // Main title
            lblTitle = new Label
            {
                Text = "Bienvenue dans Tablette System V2",
                Location = new Point(20, 20),
                Size = new Size(600, 30),
                Font = new Font(this.Font.FontFamily, 18, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };
            this.Controls.Add(lblTitle);

            // Welcome message
            lblWelcome = new Label
            {
                Text = "Merci d'avoir choisi Tablette System V2 !",
                Location = new Point(20, 60),
                Size = new Size(600, 25),
                Font = new Font(this.Font.FontFamily, 12, FontStyle.Regular)
            };
            this.Controls.Add(lblWelcome);

            // Description
            lblDescription = new Label
            {
                Text = "Cet assistant d'installation vous guidera tout au long du processus d'installation et s'assurera que toutes les dépendances requises sont correctement configurées sur votre système.",
                Location = new Point(20, 90),
                Size = new Size(600, 40),
                Font = new Font(this.Font.FontFamily, 10, FontStyle.Regular)
            };
            this.Controls.Add(lblDescription);

            // Features
            lblFeatures = new Label
            {
                Text = "Fonctionnalités :",
                Location = new Point(20, 150),
                Size = new Size(140, 20),
                Font = new Font(this.Font.FontFamily, 11, FontStyle.Bold)
            };
            this.Controls.Add(lblFeatures);

            var features = new[]
            {
                "• Système de gestion de tablettes",
                "• Visualiseur et gestionnaire PDF",
                "• Organisation des expositions et contenus",
                "• Interface web moderne",
                "• Compatibilité multi-plateformes",
                "• Contrôle à distance des appareils Android"
            };

            for (int i = 0; i < features.Length; i++)
            {
                var featureLabel = new Label
                {
                    Text = features[i],
                    Location = new Point(30, 175 + (i * 20)),
                    Size = new Size(300, 20),
                    Font = new Font(this.Font.FontFamily, 9, FontStyle.Regular)
                };
                this.Controls.Add(featureLabel);
            }

            // System requirements
            lblSystemRequirements = new Label
            {
                Text = "Configuration requise :",
                Location = new Point(350, 150),
                Size = new Size(250, 20),
                Font = new Font(this.Font.FontFamily, 11, FontStyle.Bold)
            };
            this.Controls.Add(lblSystemRequirements);

            var requirements = new[]
            {
                "• Windows 10 ou plus récent",
                "• 4 Go RAM minimum",
                "• 32 Go d'espace disque libre"
            };

            for (int i = 0; i < requirements.Length; i++)
            {
                var reqLabel = new Label
                {
                    Text = requirements[i],
                    Location = new Point(360, 175 + (i * 20)),
                    Size = new Size(250, 20),
                    Font = new Font(this.Font.FontFamily, 9, FontStyle.Regular)
                };
                this.Controls.Add(reqLabel);
            }

            this.ResumeLayout(false);
        }
    }
}
