using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace setup_wizard.Utils
{
    public class CredentialEncryptor
    {
        private static readonly string EncryptionKey = "TabletteSystemV2SetupWizard2024!";

        public static void CreateEncryptedConfig(string username, string token, string outputPath)
        {
            try
            {
                // Chiffrer les identifiants
                string encryptedUsername = EncryptString(username);
                string encryptedToken = EncryptString(token);

                // Créer la configuration chiffrée
                var config = new GitHubConfig
                {
                    GitHub = new GitHubCredentials
                    {
                        Username = encryptedUsername,
                        Token = encryptedToken
                    }
                };

                // Sérialiser en JSON
                string jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });

                // Écrire le fichier
                File.WriteAllText(outputPath, jsonContent);

                Console.WriteLine($"✅ Configuration chiffrée créée : {outputPath}");
                Console.WriteLine($"🔐 Username chiffré : {encryptedUsername}");
                Console.WriteLine($"🔐 Token chiffré : {encryptedToken}");
                Console.WriteLine("⚠️  IMPORTANT : Ne partagez JAMAIS ce fichier !");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur lors du chiffrement : {ex.Message}");
            }
        }

        private static string EncryptString(string plainText)
        {
            try
            {
                byte[] key = Encoding.UTF8.GetBytes(EncryptionKey);
                
                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.GenerateIV();
                    
                    using (ICryptoTransform encryptor = aes.CreateEncryptor())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(aes.IV, 0, aes.IV.Length);
                        
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch
            {
                return plainText;
            }
        }

        // Classes de configuration
        public class GitHubConfig
        {
            public GitHubCredentials GitHub { get; set; } = new GitHubCredentials();
        }

        public class GitHubCredentials
        {
            public string Username { get; set; } = "";
            public string Token { get; set; } = "";
        }

        // Point d'entrée pour tester le chiffrement (SUPPRIMÉ pour éviter les conflits)
        // Maintenant on utilise Form1.cs pour créer le fichier de configuration
    }
}
