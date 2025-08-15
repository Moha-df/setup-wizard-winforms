# Tablette System V2 - Setup Wizard

![Windows](https://img.shields.io/badge/Windows-10%2B-blue?logo=windows)
![.NET](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)
![License](https://img.shields.io/badge/License-Educational-green)

Un assistant d'installation moderne en C# WinForms pour automatiser le déploiement de projets avec gestion des dépendances, clonage GitHub, et interface vidéo intégrée.

## 🎯 Fonctionnalités

- **Installation automatique des dépendances** : Node.js, Git, Android SDK Tools (ADB), scrcpy, Nmap
- **Clonage GitHub sécurisé** avec authentification chiffrée
- **Gestion PM2** pour les processus Node.js
- **Tâches planifiées Windows** automatiques
- **Interface vidéo finale** avec WebView2 et serveur HTTP local
- **Installation non-intrusive** de Nmap

## 🛠️ Prérequis

- Windows 10 ou plus récent
- .NET 8.0 Windows Desktop Runtime
- Visual Studio 2022


## 🔧 Développement

### Structure du projet

```
setup-wizard/
├── Panels/                    # Écrans de l'assistant
│   ├── WelcomePanel.cs       # Écran d'accueil
│   ├── DependenciesCheckPanel.cs  # Vérification dépendances
│   ├── PM2InstallPanel.cs    # Installation PM2
│   ├── GitClonePanel.cs      # Clonage GitHub
│   ├── DeploymentPanel.cs    # Déploiement
│   ├── SchedulerPanel.cs     # Tâches planifiées
│   └── FinishPanel.cs        # Vidéo finale
├── Utils/                     # Utilitaires
│   ├── DependencyChecker.cs  # Vérification dépendances
│   └── DependencyInstaller.cs # Installation dépendances
├── github_config.json        # Configuration GitHub (chiffrée)
```

### Compilation

```bash
# Dans Visual Studio 2022
1. Ouvrir setup-wizard.sln
2. Build > Publish 
3. L'exécutable sera dans publish/
```

## 🔐 Configuration GitHub

### ⚠️ Important - Sécurité

Le fichier `github_config.json` n'est pas inclus et contient des **credentials chiffrés pour un projet spécifique**.

### 🔄 Réutilisation du code

#### Usage éducatif
Le code peut être utilisé à des fins éducatives pour comprendre :
- L'architecture d'un setup wizard WinForms
- L'intégration WebView2 pour la vidéo
- La gestion des dépendances Windows
- Le chiffrement de credentials

####  Adaptation pour votre projet

1. **Changez l'URL du repository** dans `GitClonePanel.cs` :
```csharp
// Ligne 178
string authUrl = $"https://{HttpUtility.UrlEncode(credentials.Username)}:{HttpUtility.UrlEncode(credentials.Token)}@github.com/VOTRE-USERNAME/VOTRE-REPO.git";
```

2. **Créez votre github_config.json** :



- **Utilisation sécurisée (chiffré)** :
Utilisez la classe `EncryptCredentials` du projet pour générer un fichier chiffré.

**⚠️ Note** : Sans ce fichier, l'assistant utilisera les credentials par défaut (qui ne fonctionneront pas pour votre repository).

```json
{
  "GitHub": {
    "Username": "votre-username-github",
    "Token": "ghp_votre_token_github_ici"
  }
}
```


## 📋 Dépendances installées

1. **Node.js** - Runtime JavaScript (.msi)
2. **Git** - Contrôle de version (.exe)  
3. **Android SDK Tools** - ADB pour Android (.zip)
4. **scrcpy** - Contrôle d'écran Android (.zip)
5. **Nmap** - Scanner réseau

## 🏗️ Architecture

### Technologies principales
- **Framework** : .NET 8.0 Windows Forms
- **Interface vidéo** : WebView2 + HttpListener intégré
- **Sécurité** : Chiffrement AES-256 des credentials
- **Installation** : Support MSI/EXE/ZIP avec extraction automatique
- **UI** : Interface async/await non-bloquante

### Fonctionnalités avancées
- 🔧 **Gestion automatique des dépendances** avec ordre optimisé
- 🔐 **Authentification GitHub sécurisée** (chiffrée ou plain text)
- ⚙️ **Tâches planifiées Windows** pour PM2
- 🎬 **Lecteur vidéo intégré** avec serveur HTTP local
- 📦 **Installation silencieuse** pour la plupart des outils

## 📜 Licence

Ce projet est fourni à des fins **éducatives et de démonstration**. 

### Utilisation autorisée :
- ✅ Apprentissage et formation
- ✅ Inspiration pour vos propres projets
- ✅ Adaptation avec vos propres credentials/repositories

### Non autorisé :
- ❌ Utilisation commerciale sans permission
- ❌ Redistribution des credentials inclus
- ❌ Accès aux repositories privés du projet original

## 🤝 Contribution

Les contributions sont les bienvenues ! N'hésitez pas à :
- Signaler des bugs
- Proposer des améliorations
- Ajouter de nouvelles fonctionnalités

## 📚 Ressources utiles

**Documentation technique :**
- [WebView2 Documentation](https://docs.microsoft.com/en-us/microsoft-edge/webview2/)
- [.NET 8.0 Windows Forms](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)
- [GitHub Personal Access Tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token)

**Dépendances installées :**
- [Node.js](https://nodejs.org/) - Runtime JavaScript
- [PM2](https://pm2.keymetrics.io/) - Process manager
- [Android SDK Platform Tools](https://developer.android.com/studio/releases/platform-tools) - ADB et outils
- [scrcpy](https://github.com/Genymobile/scrcpy) - Contrôle d'écran Android