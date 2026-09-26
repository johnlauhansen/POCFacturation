# POC Facturation - Élevage Canin

Ce fichier définit les principes d'architecture, les conventions de code et les flux de travail pour le projet **POC Facturation**, un logiciel de facturation WPF pour l'élevage de chiens, conforme aux réglementations de facturation françaises (Loi anti-fraude TVA / principes NF525).

## 🚀 Principes d'Architecture

Le projet utilise une architecture en couches découplées pour garantir la testabilité, la maintenabilité et la conformité légale.

### 1. Organisation de la Solution
Le projet est structuré en couches découplées respectant le principe d'encapsulation stricte pour empêcher le projet WPF de faire des requêtes directes à la base de données (Solution A) :

- **POC_Facturation.Domain** : Noyau central pur (sans dépendance externe). Il contient :
  - Les entités métiers (`Invoice`, `DogDetail`, `InvoiceLineItem`).
  - Les énumérations (`InvoiceStatus`, `Sex`).
  - Les interfaces des dépôts (`IInvoiceRepository`, `IDogRepository`) sous le namespace `Domain.Repositories`.
- **POC_Facturation.Data** : Gère la persistance de manière **encapsulée** :
  - Le `InvoiceDbContext` et les implémentations concrètes des repositories sont déclarés en **`internal`** pour empêcher toute fuite de requêtes EF Core vers les couches supérieures.
  - Expose la méthode d'extension publique `DataServiceRegistration.AddDataServices` pour permettre à WPF d'enregistrer les services au démarrage sans dévoiler le contexte de données.
- **POC_Facturation.Services** : Contient les services applicatifs (validation, chaînage de signatures cryptographiques, numérotation séquentielle).
- **POC_Facturation.WPF** : Application WPF (MVVM avec `CommunityToolkit.Mvvm`) qui communique uniquement via les interfaces définies dans le Domaine et utilise l'injection de dépendances pour câbler le projet de données au démarrage.

#### Schéma d'Architecture Encapsulé (Solution A)
```mermaid
graph TD
    %% Couches de l'application
    WPF[POC_Facturation.WPF] -->|Utilise uniquement les interfaces| Domain[POC_Facturation.Domain]
    WPF -->|Injection au démarrage uniquement| Data[POC_Facturation.Data]
    WPF --> Services[POC_Facturation.Services]
    
    Services --> Domain
    Data -->|Implémente| Domain
    
    %% Base de données
    Data --> SQLite[(Base SQLite)]
    
    %% Rôles et responsabilités
    subgraph POC_Facturation.Domain [Domaine (Pur - Public)]
        DomainEntities[Entités: Invoice, DogDetail, LineItem]
        DomainInterfaces[Interfaces: IInvoiceRepository, IDogRepository]
        DomainEnums[Enums: InvoiceStatus, Sex]
    end
    
    subgraph POC_Facturation.Data [Accès Données (Interne/Encapsulé)]
        DbContext[internal InvoiceDbContext]
        Impl[internal InvoiceRepository / DogRepository]
        Registration[public DataServiceRegistration]
    end

    subgraph POC_Facturation.WPF [Interface Utilisateur (Public)]
        Views[Vues XAML & MVVM]
        ViewModels[ViewModels injectant les interfaces du Domaine]
    end
```

#### Schéma d'Architecture Textuel (ASCII)
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

### 2. Conformité Légale (Loi Anti-Fraude TVA / NF525)
Toute modification apportée à la gestion des factures doit respecter ces trois règles fondamentales :
- **Inaltérabilité** : Une facture au statut `Validated` ne peut plus être modifiée ou supprimée. Toute modification ultérieure doit se faire via un avoir ou une facture rectificative.
- **Chaînage cryptographique** : Chaque facture validée calcule un hash SHA-256 combinant ses propres données (numéro, date, total TTC) et la signature de la facture précédente.
- **Séquençage chronologique** : Les numéros de facture doivent être séquentiels et chronologiques, sans trous (ex: `F-2026-0001`, `F-2026-0002`).

## 💻 Conventions de Développement

### Langage & Frameworks
- **Langage** : C# 10+ (.NET 10.0)
- **UI** : WPF avec XAML propre
- **Framework MVVM** : `CommunityToolkit.Mvvm` (Source Generators pour les `ObservableProperty` et `RelayCommand`)
- **Persistance** : EF Core SQLite (base de données locale légère et rapide pour le POC)
- **Injection de Dépendances** : Microsoft DI (`Microsoft.Extensions.DependencyInjection`).

### Injection de Dépendances & Démarrage de l'Application
Le démarrage de l'application WPF est géré dans `App.xaml.cs` via l'inversion de contrôle :
- Le conteneur configure les services de manière encapsulée :
  - La couche **Données** fournit `AddDataServices(connectionString)` et cache le `DbContext` en `internal`. L'initialisation de la base SQLite se fait via `ServiceProvider.InitializeDatabase()` sans exposer les types internes au projet WPF.
  - La couche **Services** fournit `AddBusinessServices()` pour injecter `IInvoiceService`.
- Dans `App.xaml`, l'attribut `StartupUri` est retiré pour laisser `App.xaml.cs` instancier `MainWindow` depuis le conteneur de services et l'afficher.

### Interface Utilisateur & MVVM (Couche Présentation)
L'interface utilisateur implémente un design moderne et interactif structuré selon le pattern MVVM :
- **`MainWindowViewModel`** : Fait office de contrôleur de flux principal. Il expose :
  - La liste réactive `Invoices` et la liste des chiens disponibles `Dogs` pour la sélection.
  - La commande `CreateDraftInvoiceCommand` pour initier de nouveaux brouillons.
  - La commande `SaveInvoiceCommand` pour mettre à jour la base de données SQLite locale.
  - La commande `ValidateInvoiceCommand` qui met en œuvre l'affichage d'un avertissement d'inaltérabilité fiscale avant de sceller et signer la facture.
  - La commande `CreateCreditNoteCommand` qui permet de cloner une facture validée sous forme d'avoir négatif pour l'annulation légale.
  - Les commandes `AddLineItemCommand` et `RemoveLineItemCommand` pour manipuler les articles (liaison automatique d'un chiot si sélectionné).
- **`MainWindow.xaml`** : Vue en écran divisé (split-screen) :
  - *Gauche* : Liste des factures avec indicateurs de statut visuels colorés (Draft, Validated, Cancelled).
  - *Droite* : Éditeur interactif de factures, tableau dynamique d'articles, et panneau de conformité fiscale (signatures cryptographiques) s'affichant dès la validation de la facture.

### Règle d'Identification des Éléments Graphiques (XAML)
**Règle obligatoire pour toute vue ou élément graphique ajouté au projet :**
Afin de garantir la lisibilité du code XAML, faciliter la navigation, permettre le débogage (Live Visual Tree) et rendre l'interface testable par des outils automatisés (ex: FlaUI, WinAppDriver, Appium), **tout contrôle interactif ou porteur d'information métier DOIT posséder un `x:Name` et un `AutomationProperties.AutomationId` explicites**.
- **`x:Name`** : CamelCase / PascalCase descriptif (ex: `NewInvoiceButton`, `CustomerNameTextBox`, `LineItemsDataGrid`).
- **`AutomationProperties.AutomationId`** : Préfixe de type + nommage métier explicite (ex: `Btn_NewInvoice`, `Txt_CustomerName`, `Grid_LineItems`, `Cmb_DogSelection`, `Picker_IssueDate`, `Chk_IsTvaApplicable`).
- **`ToolTip`** : Recommandé pour tous les boutons iconographiques (ex: `🗑️`) afin d'expliciter leur rôle à l'utilisateur final.

**Exemple de conformité :**
```xml
<Button x:Name="DeleteLineItemButton"
        AutomationProperties.AutomationId="Btn_DeleteLineItem"
        ToolTip="Supprimer cette ligne (brouillon uniquement)"
        Content="🗑️" ... />
```

### Base de Données & Données de Démonstration (Seeding)
Pour que le prototype soit immédiatement fonctionnel, la base SQLite locale est pré-remplie lors du premier lancement avec 3 chiots de démonstration (LOF et non LOF) via la méthode d'extension publique `InitializeDatabase()`.

### Règles Métier Élevage (Vente de Chiens)
Chaque facture de vente de chien doit obligatoirement renseigner :
- Le numéro d'identification national (I-CAD / puce à 15 chiffres ou tatouage).
- Le statut LOF (Livre des Origines Français) et le numéro de LOF le cas échéant (sinon mention "Type [Race]").
- La date de naissance, le sexe (fortement typé par l'énumération `Sex`), la couleur et la race de l'animal.
- Le numéro de passeport européen.

## 🧪 Stratégie de Test

- **Tests Unitaires** : Obligatoires pour la logique de signature cryptographique, le calcul de la TVA, et le générateur de numéros séquentiels.
- **Framework de Test** : xUnit avec FluentAssertions.
- **Principe de test** : Suivre les directives AAA (Arrange-Act-Assert) et tester en priorité les cas limites légaux (ex: interdiction d'éditer une facture validée).

## ⚖️ Conformité Légale & Validation État (Chorus Pro / PPF)

Pour assurer que les factures émises soient acceptées par les plateformes de l'État (Chorus Pro, Portail Public de Facturation - PPF) et conformes à la réglementation française, le projet suit les exigences suivantes :

### 1. Format Technique du Document (Factur-X)
Les factures validées doivent être générées au format hybride **Factur-X** (norme européenne `EN 16931`) :
- **Visuel** : Fichier conforme au standard **PDF/A-3** (archivage pérenne).
- **Données** : Fichier **XML structuré** (ex: `factur-x.xml`) embarqué directement à l'intérieur du PDF.
- **Validation** : Les fichiers générés doivent être testés et validés à l'aide des outils officiels :
  - Le validateur officiel en ligne de la **FNFE-MPE** pour s'assurer de la validité du XML embarqué.
  - Le portail de qualification (bêta) de **Chorus Pro** pour valider l'intégration technique automatique de l'État.

### 2. Attestation Individuelle de Conformité (Loi Anti-Fraude TVA)
Le logiciel étant développé sur mesure pour l'élevage, il fera l'objet d'une **Attestation Individuelle de Conformité** signée par l'éditeur. Pour justifier de cette conformité lors d'un contrôle fiscal, la suite de tests automatisée doit valider empiriquement les points suivants :
- **Test d'Inaltérabilité** : Validation qu'aucune facture passée au statut `Validated` ne peut être modifiée ou supprimée (les tentatives doivent lever une exception par le système).
- **Test d'Intégrité (Chaînage)** : Validation que la modification arbitraire d'un centime ou d'une date sur une facture brise la signature cryptographique SHA-256 de toute la chaîne de factures suivantes.
- **Journal d'Audit** : Génération d'un log d'audit inaltérable enregistrant l'historique et la signature de chaque validation de facture.

## 🛡️ Tolérance aux Pannes & Journalisation (Logging)

L'application intègre un dispositif complet de résilience et de journalisation pour prévenir les crashs inopinés et faciliter le diagnostic en environnement d'exécution :

### 1. Journalisation Résiliente (Serilog)
- **Configuration** : Initialisée dès l'instanciation de l'application dans `App.xaml.cs`.
- **Fichier tournant (Rolling file)** : Logs stockés dans `./logs/poc-facturation-YYYYMMDD.log` avec un intervalle journalier et une politique de rétention automatique de 30 jours.
- **Format structuré** : Horodatage précis, niveau de criticité, contexte source et trace complète de la pile d'appel (`Exception`).
- **Fermeture ordonnée** : Vidage systématique des tampons disques (`Log.CloseAndFlush()`) lors de l'événement `App.OnExit` ou d'un arrêt fatal.

### 2. Dispositif Anti-Crash Multi-Niveaux (WPF / CLR)
Pour éviter tout arrêt brutal du processus en cours d'utilisation, trois niveaux d'interception sont branchés dans `App.xaml.cs` :
- **Thread UI (`DispatcherUnhandledException`)** : Capture les exceptions non gérées du moteur graphique ou des commandes, consigne l'erreur et positionne `e.Handled = true` pour maintenir l'application active.
- **Tâches Asynchrones (`TaskScheduler.UnobservedTaskException`)** : Capture les exceptions orphelines issues du pool de threads / TPL et positionne `e.SetObserved()` pour empêcher la terminaison par le ramasse-miettes (GC).
- **Domaine Global (`AppDomain.CurrentDomain.UnhandledException`)** : Filet de sécurité ultime pour journaliser les erreurs critiques hors thread UI avant toute interruption système.

### 3. Architecture Découplée & Notification Utilisateur
Afin de préserver la pureté de la couche de services et de garantir la testabilité unitaire sans instancier le moteur WPF :
- **`IGlobalExceptionHandler` / `GlobalExceptionHandler`** (couche `POC_Facturation.Services`) : Traite, extrait les causes racines (`GetBaseException()`), formate le message utilisateur et journalise l'anomalie via `ILogger`. Isolé des pannes externes (résilience interne par blocs `try/catch` cloisonnés).
- **`IUserNotifier`** (couche `POC_Facturation.Services`) : Contrat d'abstraction pour alerter l'utilisateur sans dépendance binaire vers WPF (`System.Windows`).
- **`WpfUserNotifier`** (couche `POC_Facturation`) : Implémentation concrète WPF exécutant l'affichage `MessageBox` de manière asynchrone non-bloquante (`Dispatcher.BeginInvoke`), rattachée à la fenêtre active et protégée contre l'empilement de modales (*anti-flooding* via `Interlocked`).
- **Tests Unitaires Découplés** : Vérifiés dans `POC_Facturation.Tests/Services/GlobalExceptionHandlerTests.cs` à l'aide d'un double de test (`FakeUserNotifier`) et de `NullLogger`.
