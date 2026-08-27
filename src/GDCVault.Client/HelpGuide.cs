using System;
using System.Diagnostics;
using System.IO;

namespace GDCVault.Client;

/// Deschide PDF-ul de ghid publicat langa exe (vezi GDCVault.Client.csproj,
/// `Content Include="..\..\installer\Instructiuni_Utilizare.pdf"`) — oglinda
/// HelpGuide.swift (Mac). Daca lipseste (build de dezvoltare fara publish
/// complet), arata o eroare clara in loc sa nu faca nimic vizibil.
public static class HelpGuide
{
    public static void Open()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Instructiuni_Utilizare.pdf");
        if (File.Exists(path))
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        else
        {
            new Wpf.Ui.Controls.MessageBox
            {
                Title = "Ghidul nu a fost găsit",
                Content = "Instructiuni_Utilizare.pdf lipsește din acest build (normal doar pe build-uri de dezvoltare locale, nu pe cele publicate)."
            }.ShowDialogAsync();
        }
    }
}
