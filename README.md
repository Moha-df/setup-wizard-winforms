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
- **Installation non-intrusive** de Nmap avec gestion Npcap

## 🛠️ Prérequis

- Windows 10 ou plus récent
- .NET 8.0 Windows Desktop Runtime
- Droits administrateur (pour l'installation des dépendances)
- Connexion Internet

## 📦 Installation rapide

1. **Téléchargez** la dernière version depuis les [Releases](../../releases)
2. **Lancez** `setup-wizard.exe` en tant qu'administrateur
3. **Suivez** l'assistant d'installation étape par étape

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
└── tonytonychopper.mp4       # Vidéo finale
```

### Compilation

```bash
# Dans Visual Studio 2022
1. Ouvrir setup-wizard.sln
2. Build > Publish > FolderProfile
3. L'exécutable sera dans publish/
```

## 🔐 Configuration GitHub

### ⚠️ Important - Sécurité

Le fichier `github_config.json` inclus contient des **credentials chiffrés pour un projet spécifique**. Ces données ne sont **pas accessibles** sans la clé de déchiffrement.

### 🔄 Réutilisation du code

#### Option 1 : Usage éducatif
Le code peut être utilisé à des fins éducatives pour comprendre :
- L'architecture d'un setup wizard WinForms
- L'intégration WebView2 pour la vidéo
- La gestion des dépendances Windows
- Le chiffrement de credentials

#### Option 2 : Adaptation pour votre projet

1. **Changez l'URL du repository** dans `GitClonePanel.cs` :
```csharp
// Ligne 178
string authUrl = $"https://{HttpUtility.UrlEncode(credentials.Username)}:{HttpUtility.UrlEncode(credentials.Token)}@github.com/VOTRE-USERNAME/VOTRE-REPO.git";
```

2. **Créez votre github_config.json** :
```bash
# Utilisez EncryptCredentials.exe pour générer votre config
EncryptCredentials.exe "votre-username" "votre-token" "github_config.json"
```

3. **Remplacez la vidéo** :
   - Remplacez `tonytonychopper.mp4` par votre vidéo
   - Format recommandé : MP4, 16:9, maximum 10MB

## 🎬 Interface vidéo

L'assistant se termine par une **vidéo intégrée** avec :
- Lecture automatique en boucle
- Volume à 50% par défaut
- Bouton mute/unmute
- Border radius stylé avec ombre
- Serveur HTTP local pour contourner les restrictions de sécurité

### Technologies utilisées
- **WebView2** pour l'affichage HTML5
- **HttpListener** pour servir la vidéo localement
- **CSS moderne** pour le styling

## 📋 Dépendances installées

L'ordre d'installation est optimisé :

1. **Node.js** - Runtime JavaScript (.msi)
2. **Git** - Contrôle de version (.exe)  
3. **Android SDK Tools** - ADB pour Android (.zip)
4. **scrcpy** - Contrôle d'écran Android (.zip)
5. **Nmap** - Scanner réseau (installation manuelle avec Npcap)

## 🏗️ Architecture technique

- **Framework** : .NET 8.0 Windows Forms
- **Vidéo** : WebView2 + HttpListener
- **Sécurité** : Chiffrement AES-256 des credentials
- **Installation** : Support MSI/EXE/ZIP avec extraction automatique
- **Async/Await** : Interface responsive non-bloquante

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

## 📞 Support

Pour toute question technique ou d'utilisation, créez une [Issue](../../issues) avec :
- Description du problème
- Version de Windows
- Logs d'erreur si applicable

---

**Fait avec ❤️ pour automatiser le déploiement de projets**
