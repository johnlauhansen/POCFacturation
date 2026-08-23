using System;

namespace POC_Facturation.Domain;

public enum Sex
{
    Male,
    Female
}

public class DogDetail
{
    public int Id { get; set; }
    public string IcadNumber { get; set; } = string.Empty;       // Numéro de puce électronique (15 chiffres)
    public string TattooNumber { get; set; } = string.Empty;     // Numéro de tatouage (si applicable)
    public string Breed { get; set; } = string.Empty;            // Race / Apparence de race
    public bool IsLof { get; set; } = false;                         // Inscrit au Livre des Origines Français ou non
    public string LofNumber { get; set; } = string.Empty;        // Numéro d'inscription au LOF (le cas échéant)
    public DateTime BirthDate { get; set; }
    public Sex DogSex { get; set; }             // Mâle / Femelle
    public string Color { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;   // Passeport européen (facultatif ou requis)
}
