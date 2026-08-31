using System.Diagnostics;
using System.Windows;
using GDCVault.Client.Services;
using GDCVault.Core.Services;

namespace GDCVault.Client;

/// Oglinda ActivationSheet.swift (Mac). Codul de activare se genereaza
/// manual din Furnizor (Mac-ul lui Cristi, GenerateSerialView.swift,
/// `gdc-vault` in `gdcStandaloneProducts`) dupa mesajul WhatsApp — acelasi
/// flux ca toate celelalte unelte GDC, NU plata automatizata.
public partial class ActivationWindow
{
    private readonly LicenseManager _license;

    public ActivationWindow(LicenseManager license)
    {
        InitializeComponent();
        _license = license;
        MachineIdText.Text = MachineID.Display;
        RefreshPricingText();
        PricingChecker.Shared.PropertyChanged += (_, _) => Dispatcher.Invoke(RefreshPricingText);
    }

    // Preț dinamic (Regula 27) - vezi PricingChecker. Fail-open pe 5 €
    // (valoarea hardcodata anterior) daca pricing.json nu e accesibil.
    private void RefreshPricingText()
    {
        var pricing = PricingChecker.Shared;
        PricingText.Text = pricing.ActivePromo is { } promo
            ? $"🔥 {promo.Label}: {FormatPrice(promo.Price)} (în loc de {FormatPrice(pricing.BasePrice)}) — Licență Lifetime, donație unică, nu un preț de listă."
            : $"Licență Lifetime — {FormatPrice(pricing.EffectivePrice)} donație unică, nu un preț de listă. Mă ajută să acopăr costurile de dezvoltare. Ofertă valabilă în faza de dezvoltare/beta; crește după obținerea certificatelor oficiale de semnare Apple/Windows.";
    }

    private static string FormatPrice(double value)
    {
        var isWhole = value % 1 == 0;
        return $"{(isWhole ? ((long)value).ToString() : value.ToString())}€";
    }

    private void OnCopyClicked(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(MachineID.Display);
        CopyButton.Content = "Copiat";
    }

    // Preț dinamic (Regula 27) - vezi PricingChecker. Fail-open pe 5 €
    // (valoarea hardcodata anterior) daca pricing.json nu e accesibil.
    private void OnWhatsAppClicked(object sender, RoutedEventArgs e)
    {
        var priceText = PricingChecker.Shared.DisplayText;
        var text = $"Bună, vreau să donez {priceText} pentru licența GDC Vault. ID calculator: {MachineID.Display}";
        Process.Start(new ProcessStartInfo(WhatsAppLink.Url(text)) { UseShellExecute = true });
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e) => Close();

    private void OnActivateClicked(object sender, RoutedEventArgs e)
    {
        if (_license.Activate(CodeBox.Text))
        {
            Close();
            return;
        }

        ErrorText.Text = _license.ActivationError;
        ErrorText.Visibility = Visibility.Visible;
    }
}
