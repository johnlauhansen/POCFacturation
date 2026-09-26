using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using POC_Facturation.Domain;
using POC_Facturation.Domain.Repositories;
using POC_Facturation.Domain.Services;

namespace POC_Facturation.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IDogRepository _dogRepository;
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoicePdfService _invoicePdfService;

    [ObservableProperty]
    private ObservableCollection<Invoice> _invoices = new();

    [ObservableProperty]
    private ObservableCollection<DogDetail> _dogs = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInvoiceSelected))]
    [NotifyPropertyChangedFor(nameof(IsInvoiceDraft))]
    [NotifyPropertyChangedFor(nameof(IsInvoiceValidated))]
    private Invoice? _selectedInvoice;

    [ObservableProperty]
    private DogDetail? _selectedDogForLine;

    public bool IsInvoiceSelected => SelectedInvoice != null;
    public bool IsInvoiceDraft => SelectedInvoice != null && SelectedInvoice.Status == InvoiceStatus.Draft;
    public bool IsInvoiceValidated => SelectedInvoice != null && SelectedInvoice.Status == InvoiceStatus.Validated;

    public MainWindowViewModel(
        IInvoiceRepository invoiceRepository,
        IDogRepository dogRepository,
        IInvoiceService invoiceService,
        IInvoicePdfService invoicePdfService)
    {
        _invoiceRepository = invoiceRepository;
        _dogRepository = dogRepository;
        _invoiceService = invoiceService;
        _invoicePdfService = invoicePdfService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            var invoiceList = await _invoiceRepository.GetAllAsync();
            Invoices = new ObservableCollection<Invoice>(invoiceList);

            var dogList = await _dogRepository.GetAllAsync();
            Dogs = new ObservableCollection<DogDetail>(dogList);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur de chargement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task CreateDraftInvoiceAsync()
    {
        try
        {
            var newInvoice = new Invoice
            {
                Status = InvoiceStatus.Draft,
                IssueDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(30),
                SellerName = "Élevage du Val de la Sensée",
                SellerSiret = "12345678901234",
                SellerTvaNumber = "FR12345678901",
                SellerAddress = "12 Rue de la Ferme, 59000 Lille",
                CustomerName = "Nouveau Client",
                CustomerAddress = "Adresse du Client",
                IsTvaApplicable = true,
                LineItems = new ObservableCollection<InvoiceLineItem>()
            };

            // Ajouter une ligne d'article par défaut
            newInvoice.LineItems.Add(new InvoiceLineItem
            {
                Description = "Acompte / Vente de chiot",
                Quantity = 1,
                UnitPriceHT = 1000m,
                TvaRate = 20.0m
            });

            // Recalculer les totaux
            newInvoice.TotalHT = newInvoice.LineItems.Sum(l => l.TotalHT);
            newInvoice.TotalTVA = newInvoice.LineItems.Sum(l => l.TotalTVA);
            newInvoice.TotalTTC = newInvoice.LineItems.Sum(l => l.TotalTTC);

            await _invoiceRepository.AddAsync(newInvoice);
            
            // Recharger la liste et sélectionner la nouvelle facture
            await LoadDataAsync();
            SelectedInvoice = Invoices.FirstOrDefault(i => i.Id == newInvoice.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de la création : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task SaveInvoiceAsync()
    {
        if (SelectedInvoice == null) return;

        try
        {
            // Recalculer les totaux avant sauvegarde
            SelectedInvoice.TotalHT = SelectedInvoice.LineItems.Sum(l => l.TotalHT);
            if (SelectedInvoice.IsTvaApplicable)
            {
                SelectedInvoice.TotalTVA = SelectedInvoice.LineItems.Sum(l => l.TotalTVA);
                SelectedInvoice.TotalTTC = SelectedInvoice.LineItems.Sum(l => l.TotalTTC);
            }
            else
            {
                SelectedInvoice.TotalTVA = 0;
                SelectedInvoice.TotalTTC = SelectedInvoice.TotalHT;
            }

            await _invoiceRepository.UpdateAsync(SelectedInvoice);
            await LoadDataAsync();
            MessageBox.Show("Facture sauvegardée avec succès.", "Sauvegarde", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de la sauvegarde : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task ValidateInvoiceAsync()
    {
        if (SelectedInvoice == null) return;

        var result = MessageBox.Show(
            "Êtes-vous sûr de vouloir valider cette facture ? Elle deviendra INALTÉRABLE (impossible à modifier ou supprimer) conformément à la loi fiscale française.",
            "Validation Fiscale Obligatoire",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _invoiceService.ValidateAndSignInvoiceAsync(SelectedInvoice);
                await LoadDataAsync();
                
                // Sélectionner à nouveau la facture validée
                SelectedInvoice = Invoices.FirstOrDefault(i => i.Id == SelectedInvoice.Id);
                
                MessageBox.Show("Facture validée et signée électroniquement de manière inaltérable.", "Validation Réussie", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de validation fiscale : {ex.Message}", "Erreur de Conformité", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    public async Task CreateCreditNoteAsync()
    {
        if (SelectedInvoice == null || SelectedInvoice.Status != InvoiceStatus.Validated) return;

        var result = MessageBox.Show(
            "Voulez-vous générer un Avoir (facture négative de rectification) pour annuler légalement cette facture ?",
            "Génération d'un Avoir",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var creditNote = await _invoiceService.CreateCreditNoteAsync(SelectedInvoice.Id);
                await _invoiceRepository.AddAsync(creditNote);
                
                await LoadDataAsync();
                
                // Sélectionner l'avoir créé (qui est en statut Draft)
                SelectedInvoice = Invoices.FirstOrDefault(i => i.Id == creditNote.Id);
                
                MessageBox.Show("Avoir généré en tant que brouillon. Vous devez le vérifier et le valider officiellement pour l'enregistrer.", "Avoir Généré", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la création de l'avoir : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    public async Task ExportPdfAsync()
    {
        if (SelectedInvoice == null) return;

        try
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Exporter la facture au format PDF",
                Filter = "Document PDF (*.pdf)|*.pdf",
                FileName = $"Facture_{SelectedInvoice.InvoiceNumber.Replace("/", "_")}.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await _invoicePdfService.GenerateInvoicePdfToFileAsync(SelectedInvoice, saveFileDialog.FileName);

                var openResult = MessageBox.Show(
                    $"Le document PDF a été généré avec succès :\n\n{saveFileDialog.FileName}\n\nSouhaitez-vous l'ouvrir immédiatement ?",
                    "Exportation PDF Réussie",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (openResult == MessageBoxResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(saveFileDialog.FileName)
                        {
                            UseShellExecute = true
                        });
                    }
                    catch (Exception openEx)
                    {
                        MessageBox.Show($"Impossible d'ouvrir le lecteur PDF automatiquement : {openEx.Message}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de la génération du PDF : {ex.Message}", "Erreur Export PDF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void AddLineItem(string? parameter = null)
    {
        if (SelectedInvoice == null || SelectedInvoice.Status != InvoiceStatus.Draft) return;

        var newItem = new InvoiceLineItem
        {
            Description = "Ligne de prestation",
            Quantity = 1,
            UnitPriceHT = 500m,
            TvaRate = 20.0m
        };

        if (parameter != "Free" && SelectedDogForLine != null)
        {
            newItem.Description = $"Vente de chien : {SelectedDogForLine.Breed} ({SelectedDogForLine.Color}), Puce I-CAD : {SelectedDogForLine.IcadNumber}";
            newItem.DogDetailId = SelectedDogForLine.Id;
            newItem.DogDetail = SelectedDogForLine;
            newItem.UnitPriceHT = 1200m; // Prix type de vente de chien
        }

        // Ajouter l'item à la collection observable (notifie directement la DataGrid)
        SelectedInvoice.LineItems.Add(newItem);

        // Recalcul des totaux et rafraîchissement des liaisons
        UpdateInvoiceTotals(SelectedInvoice);
    }

    [RelayCommand]
    public void RemoveLineItem(InvoiceLineItem item)
    {
        if (SelectedInvoice == null || SelectedInvoice.Status != InvoiceStatus.Draft || item == null) return;

        // Supprimer l'item de la collection observable (suppression visuelle instantanée dans la DataGrid)
        SelectedInvoice.LineItems.Remove(item);

        // Recalcul des totaux et rafraîchissement des liaisons
        UpdateInvoiceTotals(SelectedInvoice);
    }

    private void UpdateInvoiceTotals(Invoice invoice)
    {
        invoice.TotalHT = invoice.LineItems.Sum(l => l.TotalHT);
        if (invoice.IsTvaApplicable)
        {
            invoice.TotalTVA = invoice.LineItems.Sum(l => l.TotalTVA);
            invoice.TotalTTC = invoice.LineItems.Sum(l => l.TotalTTC);
        }
        else
        {
            invoice.TotalTVA = 0;
            invoice.TotalTTC = invoice.TotalHT;
        }

        // Notifie WPF de rafraîchir l'ensemble des liaisons dépendant de la facture sélectionnée
        OnPropertyChanged(nameof(SelectedInvoice));
        OnPropertyChanged(string.Empty);
    }
}
