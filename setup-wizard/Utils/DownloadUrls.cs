using System;

namespace setup_wizard.Utils
{
    /// <summary>
    /// Configuration centralisée des URLs de téléchargement des dépendances
    /// </summary>
    public static class DownloadUrls
    {
        // URLs alternatives pour Android SDK Tools (si l'URL principale échoue)
        public static readonly string[] AndroidSDKUrls = new[]
        {
            "https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip",
            "https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip",
            "https://dl.google.com/android/repository/commandlinetools-win-11076708_latest.zip"
        };

        // URLs alternatives pour scrcpy
        public static readonly string[] ScrcpyUrls = new[]
        {
            "https://github.com/Genymobile/scrcpy/releases/download/v3.3.1/scrcpy-win64-v3.3.1.zip",
            "https://github.com/Genymobile/scrcpy/releases/download/v3.3.1/scrcpy-win32-v3.3.1.zip"
        };

        // URLs alternatives pour Node.js
        public static readonly string[] NodeJsUrls = new[]
        {
            "https://nodejs.org/dist/v20.11.0/node-v20.11.0-x64.msi",
            "https://nodejs.org/dist/v20.11.0/node-v20.11.0-x86.msi"
        };

        // URLs alternatives pour Git
        public static readonly string[] GitUrls = new[]
        {
            "https://github.com/git-for-windows/git/releases/download/v2.43.0.windows.1/Git-2.43.0-64-bit.exe",
            "https://github.com/git-for-windows/git/releases/download/v2.43.0.windows.1/Git-2.43.0-32-bit.exe"
        };

        // URLs alternatives pour Nmap
        public static readonly string[] NmapUrls = new[]
        {
            "https://nmap.org/dist/nmap-7.94-setup.exe"
        };

        /// <summary>
        /// Obtient l'URL de téléchargement principale pour une dépendance
        /// </summary>
        public static string GetPrimaryUrl(string dependencyName, bool is64Bit = true)
        {
            return dependencyName switch
            {
                "Android SDK Tools" => AndroidSDKUrls[0],
                "scrcpy" => is64Bit ? ScrcpyUrls[0] : ScrcpyUrls[1],
                "Node.js" => is64Bit ? NodeJsUrls[0] : NodeJsUrls[1],
                "Git" => is64Bit ? GitUrls[0] : GitUrls[1],
                "Nmap" => NmapUrls[0],
                _ => throw new ArgumentException($"Dépendance inconnue: {dependencyName}")
            };
        }

        /// <summary>
        /// Obtient toutes les URLs alternatives pour une dépendance
        /// </summary>
        public static string[] GetAlternativeUrls(string dependencyName)
        {
            return dependencyName switch
            {
                "Android SDK Tools" => AndroidSDKUrls,
                "scrcpy" => ScrcpyUrls,
                "Node.js" => NodeJsUrls,
                "Git" => GitUrls,
                "Nmap" => NmapUrls,
                _ => new string[0]
            };
        }

        /// <summary>
        /// Vérifie si une URL est accessible
        /// </summary>
        public static async Task<bool> IsUrlAccessibleAsync(string url)
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var response = await client.SendAsync(new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Head, url));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
