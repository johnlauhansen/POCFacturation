# 🐾 POC Facturation - Élevage Canin

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/Platform-WPF%20Windows-0078D7?logo=windows&logoColor=white)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean%20%2F%20DDD-brightgreen)](#-architecture-de-la-solution)
[![Tests](https://img.shields.io/badge/Tests-xUnit%20%2F%2021%20passing-success?logo=xunit)](POC_Facturation/POC_Facturation.Tests/)
[![Compliance](https://img.shields.io/badge/Conformit%C3%A9-NF525%20%2F%20Art.%20286%20CGI-blue)](#-conformit%C3%A9-l%C3%A9gale--anti-fraude-tva)

Application de bureau moderne sous **WPF** (.NET 10) conçue pour la gestion de facturation d'un élevage canin professionnel, dans le respect strict des réglementations fiscales françaises (**Loi Anti-Fraude TVA** / principes de la norme **NF525** / **Factur-X**).

---

## 📑 Sommaire
- [✨ Fonctionnalités Clés](#-fonctionnalités-clés)
- [🏗️ Architecture de la Solution](#️-architecture-de-la-solution)
- [⚖️ Conformité Légale & Anti-Fraude TVA](#️-conformité-légale--anti-fraude-tva)
- [🛡️ Tolérance aux Pannes & Journalisation (Serilog)](#️-tolérance-aux-pannes--journalisation-serilog)
- [🚀 Démarrage Rapide](#-démarrage-rapide)
- [🧪 Tests Unitaires](#-tests-unitaires)
- [📂 Structure du Dépôt](#-structure-du-dépôt)

---

## ✨ Fonctionnalités Clés

* **Gestion Complète du Cycle de Vie des Factures** :
  * Création de brouillons modifiables (`Draft`).
  * Scellement et validation fiscale inaltérable (`Validated`).
  * Génération d'avoirs rectificatifs négatifs pour annulation légale (`Credit Note`).
* **Gestion Métier Spécifique à l'Élevage Canin** :
  * Sélection interactive parmi les chiots disponibles de l'élevage.
  * Mention obligatoire automatique des identifiants réglementaires : numéro national d'identification **I-CAD** (puce à 15 chiffres / tatouage), inscription au **LOF**, date de naissance, sexe, couleur, race et numéro de passeport européen.
  * Possibilité d'ajouter des lignes de prestations libres (pension, accessoires, cours d'éducation).
* **Régimes Fiscaux & TVA** :
  * Prise en charge de la TVA standard (20%).
  * Prise en charge des micro-entreprises / exonération (mention légale automatique : *« TVA non applicable - article 293 B du CGI »*).
* **Interface Utilisateur Moderne & Accessible** :
  * Vue scindée (*split-screen*) avec liste réactive à gauche et éditeur complet à droite.
  * Mise à jour instantanée des grilles et des totaux via collections observables.
  * Tous les éléments graphiques sont identifiés par `x:Name` et `AutomationProperties.AutomationId` pour la testabilité automatisée (FlaUI, WinAppDriver).

---

## 🏗️ Architecture de la Solution

Le projet est conçu selon les principes de la **Clean Architecture** et du **Domain-Driven Design (DDD)**, avec un découpage strict en couches et une encapsulation des détails de persistance :

```text
+-----------------------------------------------------------------------------------------+
|                                 POC_Facturation (WPF)                                   |
|  - MainWindow (XAML / Code-behind)          - MainWindowViewModel (CommunityToolkit)    |
|  - App.xaml.cs (IoC DI, Serilog init,       - WpfUserNotifier (IUserNotifier impl)      |
|    Global Exception Handlers: UI, Task, AppDomain)                                      |
+--------------------------------------+--------------------------------------------------+
                                       |
                 +---------------------+---------------------+
                 |                                           |
                 v (Appels métiers & gestion d'erreurs)      v (Binding & Modèles)
+---------------------------------+           +-------------------------------------------+
|    POC_Facturation.Services     |           |          POC_Facturation.Domain           |
|  - InvoiceService               |           |  - Entités : Invoice, DogDetail,          |
|    (calcul TVA, signatures SHA) |           |              InvoiceLineItem              |
|  - GlobalExceptionHandler       |---------->|  - Énumérations : InvoiceStatus, Sex      |
|  - Contrats :                   |           |  - Interfaces Dépôts :                    |
|    * IGlobalExceptionHandler    |           |    * IInvoiceRepository                   |
|    * IUserNotifier              |           |    * IDogRepository                       |
+---------------------------------+           +-------------------------------------------+
                 |                                                 ^
                 | Journalisation des erreurs                      | Implémente (Encapsulé)
                 v                                                 |
+---------------------------------+           +--------------------+----------------------+
|        Fichiers de Logs         |           |           POC_Facturation.Data            |
|  ./logs/poc-facturation-*.log   |           |  - DataServiceRegistration (Public DI)    |
|  (Serilog tournant 30 jours)    |           |  - InvoiceDbContext (internal)            |
|                                 |           |  - InvoiceRepository (internal)           |
|                                 |           |  - DogRepository (internal)               |
+---------------------------------+           +--------------------+----------------------+
                                                                   |
                                                                   v Persistance
                                              +--------------------+----------------------+
                                              |            Base SQLite Locale             |
                                              |              facturation.db               |
                                              +-------------------------------------------+
```

### Détail des Projets
1. **`POC_Facturation.Domain`** : Cœur pur sans dépendance externe (.NET 10). Contient les entités (`Invoice`, `DogDetail`, `InvoiceLineItem`), les énumérations (`InvoiceStatus`, `Sex`) et les interfaces de persistance.
2. **`POC_Facturation.Data`** : Couche d'accès aux données EF Core SQLite. Le `InvoiceDbContext` et les dépôts sont déclarés **`internal`** afin d'empêcher toute fuite de requêtes vers la couche graphique. Seule la méthode d'extension d'injection `AddDataServices()` est exposée.
3. **`POC_Facturation.Services`** : Logique métier applicative (calcul des taxes, ordonnancement chronologique des factures, chaînage cryptographique, gestion globale des erreurs).
4. **`POC_Facturation`** : Application WPF moderne utilisant `CommunityToolkit.Mvvm` (Source Generators), Microsoft DI et Serilog.
5. **`POC_Facturation.Tests`** : Suite de tests unitaires automatisés sous xUnit.

---

## ⚖️ Conformité Légale & Anti-Fraude TVA

Le système intègre nativement les trois exigences fondamentales de l'article 286 du CGI et des normes NF525 :

1. **Inaltérabilité** :
   * Une fois le statut `Validated` attribué, la facture est définitivement scellée. Toute tentative de modification ou suppression par l'application est bloquée.
   * La seule correction légale admise est l'émission d'un **Avoir** (*Credit Note*) qui clone la facture avec des montants négatifs.
2. **Chaînage Cryptographique (SHA-256)** :
   * Chaque facture validée calcule une empreinte numérique SHA-256 combinant son numéro, sa date, ses montants et la signature de la facture précédente.
   * Toute tentative de falsification ultérieure en base d'une facture romprait la chaîne de signatures de toutes les factures suivantes.
3. **Séquençage Chronologique sans rupture** :
   * Numérotation continue et irréversible (format : `F-YYYY-XXXX`).

---

## 🛡️ Tolérance aux Pannes & Journalisation (Serilog)

L'application embarque un dispositif de résilience complet empêchant les crashs inopinés :

* **Journalisation quotidienne tournante** : Écriture structurée dans `./logs/poc-facturation-YYYYMMDD.log` avec rétention automatique de 30 jours et vidage ordonné (`Log.CloseAndFlush()`).
* **Interception multi-niveaux** :
  * `DispatcherUnhandledException` : Capture les erreurs du thread UI (`e.Handled = true;`) pour maintenir l'application ouverte.
  * `TaskScheduler.UnobservedTaskException` : Intercepte les erreurs asynchrones orphelines (`e.SetObserved();`).
  * `AppDomain.CurrentDomain.UnhandledException` : Journalisation d'urgence en cas d'erreur de bas niveau.
* **Notification utilisateur non-bloquante** : Boîte de dialogue asynchrone rattachée à la fenêtre active avec protection anti-rebond (*anti-flooding*).

---

## 🚀 Démarrage Rapide

### Prérequis
* [SDK .NET 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) ou supérieur.
* Windows 10/11 (requis pour l'environnement d'exécution WPF).

### Installation & Lancement

1. **Cloner le dépôt :**
   ```bash
   git clone https://github.com/votre-compte/POC_Facturation.git
   cd POC_Facturation
   ```

2. **Restaurer les dépendances et compiler la solution :**
   ```bash
   dotnet build POC_Facturation/POC_Facturation.slnx
   ```

3. **Lancer les tests unitaires :**
   ```bash
   dotnet test POC_Facturation/POC_Facturation.slnx
   ```

4. **Exécuter l'application :**
   ```bash
   dotnet run --project POC_Facturation/POC_Facturation/POC_Facturation.csproj
   ```

> 💡 **Remarque au premier lancement** : La base de données SQLite locale (`facturation.db`) est automatiquement initialisée et pré-remplie avec des chiots de démonstration (LOF et non-LOF) pour tester immédiatement le logiciel.

---

## 🧪 Tests Unitaires

La suite de tests automatisés vérifie exhaustivement :
- La logique de calcul des montants (HT, TVA, TTC).
- Le séquençage sans trou des numéros de facture.
- La robustesse du chaînage des empreintes SHA-256.
- La conformité de l'inaltérabilité (levée d'exceptions si tentative d'altération).
- La résilience et l'isolation du gestionnaire global d'exceptions (`GlobalExceptionHandler`).

Exécution rapide :
```bash
dotnet test
```

---

## 📂 Structure du Dépôt

```text
POC_Facturation/
├── README.md                                  # Documentation d'accueil du dépôt
├── GEMINI.md                                  # Guide des règles d'architecture et conventions
├── POC_Facturation/
│   ├── POC_Facturation.slnx                   # Solution XML .NET moderne
│   ├── POC_Facturation/                       # Projet WPF (UI, XAML, ViewModels)
│   ├── POC_Facturation.Domain/                # Domaine pur (Entités, Enums, Dépôts)
│   ├── POC_Facturation.Data/                  # Persistance SQLite & EF Core encapsulée
│   ├── POC_Facturation.Services/              # Services métier, cryptographie, exceptions
│   └── POC_Facturation.Tests/                 # Tests unitaires (xUnit)
```

---

## 📄 Licence & Mentions

Ce projet est réalisé à titre de démonstration technique (POC) pour la gestion de facturation conforme aux exigences légales françaises de l'élevage canin.
