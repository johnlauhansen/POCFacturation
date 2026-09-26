using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using POC_Facturation.Data;
using POC_Facturation.Services;
using POC_Facturation.ViewModels;

namespace POC_Facturation;

/// <summary>
/// Point d'entrée de l'application WPF avec conteneur IoC, logging Serilog et tolérance aux pannes.
/// </summary>
public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    private IGlobalExceptionHandler? _exceptionHandler;

    public App()
    {
        ConfigureLogging();
        RegisterGlobalExceptionHandlers();
    }

    private void ConfigureLogging()
    {
        string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        string logPath = Path.Combine(logDirectory, "poc-facturation-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                retainedFileCountLimit: 30)
            .CreateLogger();

        Log.Information("Démarrage de l'application POC Facturation");
    }

    private void RegisterGlobalExceptionHandlers()
    {
        // 1. Exceptions non gérées sur le thread UI (WPF Dispatcher)
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 2. Exceptions asynchrones non observées (TPL TaskScheduler)
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // 3. Exceptions non gérées globales (AppDomain)
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            _exceptionHandler = ServiceProvider.GetRequiredService<IGlobalExceptionHandler>();

            // Initialisation sécurisée de la base de données SQLite (sans exposer le DbContext interne)
            ServiceProvider.InitializeDatabase();

            // Résolution et affichage de la fenêtre principale
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Erreur fatale lors de l'initialisation de l'application au démarrage");
            MessageBox.Show($"Erreur fatale lors du démarrage de l'application :\n{ex.Message}", "Erreur de Démarrage", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Intégration de Serilog dans Microsoft.Extensions.Logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });

        // Enregistrement des composants de notification
        services.AddSingleton<IUserNotifier, WpfUserNotifier>();

        // 1. Détermination du chemin et de la chaîne de connexion SQLite locale
        string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "facturation.db");
        string connectionString = $"Data Source={dbPath}";

        // 2. Enregistrement de la couche de Données (Solution A : DbContext et Repositories internes)
        services.AddDataServices(connectionString);

        // 3. Enregistrement de la couche Services (Moteur de conformité fiscale et gestion d'erreurs)
        services.AddBusinessServices();

        // 4. Enregistrement de la couche WPF (Vues et ViewModels)
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            if (_exceptionHandler != null)
            {
                _exceptionHandler.HandleException(e.Exception, "WPF UI Dispatcher", isFatal: false);
            }
            else
            {
                Log.Error(e.Exception, "Exception non gérée sur le Dispatcher UI (avant initialisation du gestionnaire)");
            }
        }
        catch (Exception loggingEx)
        {
            try { Log.Fatal(loggingEx, "Échec lors du traitement de l'exception Dispatcher"); } catch { }
        }
        finally
        {
            // Empêche l'application de crasher
            e.Handled = true;
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        try
        {
            if (_exceptionHandler != null)
            {
                _exceptionHandler.HandleException(e.Exception, "TaskScheduler Asynchrone", isFatal: false);
            }
            else
            {
                Log.Error(e.Exception, "Exception non observée sur TaskScheduler");
            }
        }
        catch (Exception loggingEx)
        {
            try { Log.Fatal(loggingEx, "Échec lors du traitement de l'exception TaskScheduler"); } catch { }
        }
        finally
        {
            // Empêche l'arrêt du processus par le runtime TPL
            e.SetObserved();
        }
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
            {
                if (_exceptionHandler != null)
                {
                    _exceptionHandler.HandleException(ex, "AppDomain Global", isFatal: e.IsTerminating);
                }
                else
                {
                    Log.Fatal(ex, "Exception non gérée dans l'AppDomain (isTerminating={IsTerminating})", e.IsTerminating);
                }
            }
            else
            {
                Log.Fatal("Exception non gérée non-CLR dans l'AppDomain : {ExceptionObject}", e.ExceptionObject);
            }
        }
        catch (Exception loggingEx)
        {
            try { Log.Fatal(loggingEx, "Échec lors de la journalisation AppDomain"); } catch { }
        }
        finally
        {
            if (e.IsTerminating)
            {
                Log.CloseAndFlush();
            }
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            Log.Information("Arrêt ordonné de l'application POC Facturation (Code de sortie: {ExitCode})", e.ApplicationExitCode);
        }
        finally
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
