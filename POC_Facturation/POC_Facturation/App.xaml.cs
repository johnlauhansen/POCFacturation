using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using POC_Facturation.Data;
using POC_Facturation.Services;

namespace POC_Facturation;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();

        // Initialisation sécurisée de la base de données SQLite (sans exposer le DbContext interne)
        //Appel a DataServiceRegistration
        ServiceProvider.InitializeDatabase();

        // Résolution de l'écran principal depuis le conteneur d'injection de dépendances
        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 1. Détermination du chemin et de la chaîne de connexion SQLite locale
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "facturation.db");
        string connectionString = $"Data Source={dbPath}";

        // 2. Enregistrement de la couche de Données (Solution A : DbContext et Repositories internes)
        services.AddDataServices(connectionString);

        // 3. Enregistrement de la couche Services (Moteur de conformité fiscale)
        services.AddBusinessServices();

        // 4. Enregistrement de la couche WPF (Vues et ViewModels)
        services.AddTransient<MainWindow>();
        
        // Exemples d'enregistrement futurs :
        // services.AddTransient<InvoiceListViewModel>();
        // services.AddTransient<InvoiceEditViewModel>();
    }
}
