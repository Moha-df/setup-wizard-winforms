using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;
using System.Drawing; // Added for Color
using System.Text.Json; // Added for JsonSerializer
using System.Security.Cryptography;
using System.Text;

namespace setup_wizard.Panels
{
    public partial class GitClonePanel : UserControl
    {
        private Label lblTitle;
        private Label lblStatus;
        private Label lblClonePath;
        private TextBox txtClonePath;
        private Button btnBrowse;
        private Button btnClone;
        private ProgressBar progressBar;
        private bool isCloning = false;

        // Événement pour notifier que le clonage est terminé
        public event EventHandler<bool> CloneCompleted;
        
        // Propriété publique pour récupérer le chemin du projet cloné
        public string ClonedProjectPath => txtClonePath.Text;

        private const string REPO_URL = "https://github.com/Moha-df/TabletteSystemV2.git";

        public GitClonePanel()
        {
            InitializeComponent();
            LoadGitHubCredentials();
        }

        private void InitializeComponent()
        {
            this.lblTitle = new Label();
            this.lblStatus = new Label();
            this.lblClonePath = new Label();
            this.txtClonePath = new TextBox();
            this.btnBrowse = new Button();
            this.btnClone = new Button();
            this.progressBar = new ProgressBar();

            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);
            this.lblTitle.Location = new Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new Size(200, 25);
            this.lblTitle.Text = "Clonage du Repository";
            this.Controls.Add(this.lblTitle);

            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new Point(20, 50);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(400, 15);
            this.lblStatus.Text = "Clonage du repository privé TabletteSystemV2 depuis GitHub";
            this.Controls.Add(this.lblStatus);

            // 
            // lblClonePath
            // 
            this.lblClonePath.AutoSize = true;
            this.lblClonePath.Location = new Point(20, 80);
            this.lblClonePath.Name = "lblClonePath";
            this.lblClonePath.Size = new Size(100, 15);
            this.lblClonePath.Text = "Dossier de destination:";
            this.Controls.Add(this.lblClonePath);

            // 
            // txtClonePath
            // 
            this.txtClonePath.Location = new Point(160, 80);
            this.txtClonePath.Name = "txtClonePath";
            this.txtClonePath.Size = new Size(200, 23);
            this.txtClonePath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "TabletteSystemV2");
            this.Controls.Add(this.txtClonePath);

            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new Point(370, 80);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new Size(80, 23);
            this.btnBrowse.Text = "Parcourir";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new EventHandler(this.btnBrowse_Click);
            this.Controls.Add(this.btnBrowse);

            // 
            // btnClone
            // 
            this.btnClone.Location = new Point(20, 120);
            this.btnClone.Name = "btnClone";
            this.btnClone.Size = new Size(120, 30);
            this.btnClone.Text = "Cloner";
            this.btnClone.UseVisualStyleBackColor = true;
            this.btnClone.Click += new EventHandler(this.btnClone_Click);
            this.Controls.Add(this.btnClone);

            // 
            // progressBar
            // 
            this.progressBar.Location = new Point(20, 160);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(400, 23);
            this.progressBar.Style = ProgressBarStyle.Marquee;
            this.progressBar.Visible = false;
            this.Controls.Add(this.progressBar);

            // 
            // GitClonePanel
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Name = "GitClonePanel";
            this.Size = new Size(640, 280);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Sélectionner le dossier de destination pour le clonage";
                folderDialog.SelectedPath = txtClonePath.Text;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    txtClonePath.Text = folderDialog.SelectedPath;
                }
            }
        }

        private async void btnClone_Click(object sender, EventArgs e)
        {
            if (isCloning) return;

            // Validation des champs
            if (string.IsNullOrWhiteSpace(txtClonePath.Text))
            {
                MessageBox.Show("Veuillez sélectionner un dossier de destination", "Champ manquant", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isCloning = true;
            btnClone.Enabled = false;
            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                lblStatus.Text = "Préparation du clonage...";
                
                // Créer le dossier de destination s'il n'existe pas
                if (!Directory.Exists(txtClonePath.Text))
                {
                    Directory.CreateDirectory(txtClonePath.Text);
                }

                // Récupérer les identifiants chiffrés
                var credentials = GetSecureCredentials();
                if (credentials == null)
                {
                    throw new Exception("Impossible de récupérer les identifiants GitHub sécurisés");
                }

                // Construire l'URL avec authentification sécurisée
                string authUrl = $"https://{HttpUtility.UrlEncode(credentials.Username)}:{HttpUtility.UrlEncode(credentials.Token)}@github.com/Moha-df/TabletteSystemV2.git";
                
                lblStatus.Text = "Clonage en cours...";
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 25;

                var result = await CloneRepositoryAsync(authUrl, txtClonePath.Text);
                
                if (result)
                {
                    progressBar.Value = 100;
                    lblStatus.Text = "✅ Repository cloné avec succès !";
                    await Task.Delay(2000);
                    OnCloneComplete(true);
                }
                else
                {
                    lblStatus.Text = "❌ Échec du clonage du repository";
                    progressBar.Value = 0;
                    btnClone.Enabled = true;
                    
                    // Afficher une boîte de dialogue d'erreur avec des solutions
                    ShowCloneFailureDialog();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"❌ Erreur: {ex.Message}";
                progressBar.Value = 0;
                btnClone.Enabled = true;
                
                // Afficher une boîte de dialogue d'erreur détaillée
                ShowCloneFailureDialog(ex);
            }

            isCloning = false;
        }

        private async Task<bool> CloneRepositoryAsync(string repoUrl, string destinationPath)
        {
            try
            {
                // Construire le chemin complet pour le dossier du repository
                string repoFolderPath = Path.Combine(destinationPath, "TabletteSystemV2");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = $"clone {repoUrl} \"{repoFolderPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                
                // Capturer la sortie et les erreurs
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                
                // Attendre la fin du clonage
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    // Vérifier que le dossier a été créé et contient des fichiers
                    // Maintenant le dossier du repo est directement dans repoFolderPath
                    return Directory.Exists(repoFolderPath) && 
                           Directory.GetFiles(repoFolderPath).Length > 0 &&
                           Directory.Exists(Path.Combine(repoFolderPath, ".git"));
                }
                else
                {
                    // Afficher les détails de l'erreur Git
                    ShowGitErrorDetails(process.ExitCode, output, error, repoUrl, destinationPath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Afficher l'erreur de lancement du processus Git
                ShowGitErrorDetails(-1, "", ex.Message, repoUrl, destinationPath);
                return false;
            }
        }

        private void ShowGitErrorDetails(int exitCode, string output, string error, string repoUrl, string destinationPath)
        {
            string errorMessage = $"Erreur Git avec le code de sortie: {exitCode}\n\n";
            
            // Analyser le type d'erreur basé sur le code de sortie et le message
            if (exitCode == 128)
            {
                errorMessage += "🚨 ERREUR CRITIQUE : Problème d'authentification ou d'URL\n\n";
                
                if (error.Contains("Malformed input to a URL function"))
                {
                    errorMessage += "🔍 CAUSE IDENTIFIÉE : Caractères spéciaux dans les identifiants\n";
                    errorMessage += "   • Le nom d'utilisateur ou mot de passe contient des caractères spéciaux\n";
                    errorMessage += "   • Ces caractères ne sont pas compatibles avec les URLs Git\n\n";
                    
                    errorMessage += "💡 SOLUTIONS :\n";
                    errorMessage += "   • Utilisez un mot de passe sans caractères spéciaux (comme &, *, %, #, etc.)\n";
                    errorMessage += "   • Créez un nouveau mot de passe GitHub temporaire\n";
                    errorMessage += "   • Utilisez un token d'accès personnel (PAT) à la place du mot de passe\n\n";
                }
                else if (error.Contains("Authentication failed"))
                {
                    errorMessage += "🔍 CAUSE IDENTIFIÉE : Échec de l'authentification\n";
                    errorMessage += "   • Nom d'utilisateur ou mot de passe incorrect\n";
                    errorMessage += "   • Compte GitHub verrouillé ou expiré\n\n";
                }
                else if (error.Contains("Repository not found"))
                {
                    errorMessage += "🔍 CAUSE IDENTIFIÉE : Repository inaccessible\n";
                    errorMessage += "   • Repository privé sans accès\n";
                    errorMessage += "   • Repository supprimé ou renommé\n\n";
                }
            }
            
            if (!string.IsNullOrEmpty(output))
            {
                errorMessage += "📋 Sortie standard (stdout):\n" + output + "\n\n";
            }
            if (!string.IsNullOrEmpty(error))
            {
                errorMessage += "❌ Sortie d'erreur (stderr):\n" + error + "\n\n";
            }
            
            errorMessage += $"🔗 URL du repository: {repoUrl}\n";
            errorMessage += $"📁 Dossier de destination: {destinationPath}\n\n";
            
            errorMessage += "🛠️ COMMANDE MANUELLE (si le problème persiste):\n";
            errorMessage += $"git clone https://github.com/Moha-df/TabletteSystemV2.git \"{destinationPath}\"\n\n";
            
            errorMessage += "⚠️  NOTE : Pour les repositories privés, utilisez un token d'accès personnel (PAT) :\n";
            errorMessage += "   1. Allez dans GitHub → Settings → Developer settings → Personal access tokens\n";
            errorMessage += "   2. Générez un nouveau token avec les permissions 'repo'\n";
            errorMessage += "   3. Utilisez ce token comme mot de passe";

            MessageBox.Show(errorMessage, "Erreur Git - Code " + exitCode, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowCloneFailureDialog(Exception? ex = null)
        {
            string errorMessage = "Le clonage du repository GitHub a échoué.\n\n";
            
            if (ex != null)
            {
                errorMessage += $"Erreur technique : {ex.Message}\n\n";
            }
            
            errorMessage += "Causes possibles :\n";
            errorMessage += "• Problème de connexion internet\n";
            errorMessage += "• Le repository n'existe pas ou n'est pas accessible\n";
            errorMessage += "• Problème de permissions sur le dossier de destination\n";
            errorMessage += "• Git n'est pas installé sur votre système\n\n";
            
            errorMessage += "Solutions :\n";
            errorMessage += "• Vérifiez votre connexion internet\n";
            errorMessage += "• Essayez un autre dossier de destination\n";
            errorMessage += "• Vérifiez que Git est installé (git --version)\n\n";
            
            errorMessage += "Si le problème persiste, essayez de cloner manuellement :\n";
            errorMessage += $"git clone https://github.com/Moha-df/TabletteSystemV2.git \"{txtClonePath.Text}\"";

            MessageBox.Show(
                errorMessage,
                "Échec du Clonage GitHub",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        private void OnCloneComplete(bool success)
        {
            // Afficher le message de succès et activer le bouton Next
            lblStatus.Text = "✅ Repository cloné avec succès ! Le clonage est terminé.";
            progressBar.Value = 100;
            progressBar.Visible = false;
            btnClone.Enabled = false; // Désactiver le bouton Clone
            
            // Déclencher l'événement pour activer le bouton Next
            CloneCompleted?.Invoke(this, success);
        }

        private void LoadGitHubCredentials()
        {
            try
            {
                string configPath = Path.Combine(Application.StartupPath, "github_config.json");
                if (File.Exists(configPath))
                {
                    string jsonContent = File.ReadAllText(configPath);
                    var config = System.Text.Json.JsonSerializer.Deserialize<GitHubConfig>(jsonContent);
                    
                    if (config != null && !string.IsNullOrEmpty(config.GitHub.Token))
                    {
                        // Utiliser le token du fichier de configuration
                        // txtPassword.Text = config.GitHub.Token; // This line is removed
                        lblStatus.Text = "✅ Token GitHub chargé depuis la configuration";
                    }
                    else
                    {
                        // Utiliser le token hardcodé par défaut
                        // txtPassword.Text = "ghp_votre_token_ici"; // This line is removed
                        lblStatus.Text = "⚠️ Utilisation du token par défaut - Configurez le fichier pour votre token";
                    }
                }
                else
                {
                    // Pas de fichier de config, utiliser les valeurs hardcodées
                    lblStatus.Text = "ℹ️ Utilisation des identifiants par défaut - Créez github_config.json pour votre token";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "⚠️ Erreur lors du chargement de la configuration - Utilisation des valeurs par défaut";
                // Continuer avec les valeurs hardcodées
            }
        }

        private GitHubCredentials? GetSecureCredentials()
        {
            try
            {
                string configPath = Path.Combine(Application.StartupPath, "github_config.json");
                if (File.Exists(configPath))
                {
                    string jsonContent = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<GitHubConfig>(jsonContent);
                    
                    if (config?.GitHub != null)
                    {
                        // Déchiffrer les identifiants
                        string decryptedUsername = DecryptString(config.GitHub.Username);
                        string decryptedToken = DecryptString(config.GitHub.Token);
                        
                        return new GitHubCredentials
                        {
                            Username = decryptedUsername,
                            Token = decryptedToken
                        };
                    }
                }
                
                // Fallback aux identifiants hardcodés (moins sécurisés)
                return new GitHubCredentials
                {
                    Username = "Moha-df",
                    Token = "ghp_votre_token_ici"
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la récupération des identifiants : {ex.Message}", 
                    "Erreur d'authentification", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private string EncryptString(string plainText)
        {
            try
            {
                // Clé de chiffrement (32 bytes pour AES-256)
                byte[] key = Encoding.UTF8.GetBytes("TabletteSystemV2SetupWizard2024!");
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV(); // Génère un vecteur d'initialisation unique
                    
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Écrire le IV en premier
                        ms.Write(aes.IV, 0, aes.IV.Length);
                        
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        
                        // Retourner le IV + données chiffrées en base64
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch
            {
                return plainText; // En cas d'erreur, retourner le texte en clair
            }
        }

        private string DecryptString(string cipherText)
        {
            try
            {
                // Clé de chiffrement (même clé que pour le chiffrement)
                byte[] key = Encoding.UTF8.GetBytes("TabletteSystemV2SetupWizard2024!");
                
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    
                    // Lire le IV (16 premiers bytes)
                    byte[] iv = new byte[16];
                    Array.Copy(cipherBytes, 0, iv, 0, 16);
                    aes.IV = iv;
                    
                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    using (MemoryStream ms = new MemoryStream(cipherBytes, 16, cipherBytes.Length - 16))
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                return cipherText; // En cas d'erreur, retourner le texte chiffré
            }
        }
    }

    // Added for LoadGitHubCredentials
    public class GitHubConfig
    {
        public GitHubCredentials GitHub { get; set; }
    }

    public class GitHubCredentials
    {
        public string Username { get; set; }
        public string Token { get; set; }
    }
}
