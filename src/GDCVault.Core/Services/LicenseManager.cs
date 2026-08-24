namespace GDCVault.Core.Services;

/// Oglinda LicenseManager.swift (Mac): proba de 15 zile la prima lansare,
/// apoi licenta Lifetime (5€, pret promotional beta) activata printr-un
/// cod generat manual din Furnizor (GenerateSerialView.swift,
/// `gdc-vault` in `gdcStandaloneProducts`) — acelasi flux WhatsApp ca
/// toate celelalte unelte GDC, NU plata automatizata.
///
/// DECIZIE DE PRODUS: la fel ca pe Mac, NU blocam accesul la intrarile
/// deja salvate dupa expirarea probei — doar crearea de intrari NOI
/// (`+ Adauga aplicatie`) verifica `IsUnlocked`. Vezi MainWindow.xaml.cs.
public sealed class LicenseManager
{
    public static readonly LicenseManager Shared = new();
    public const string ProductId = "gdc-vault";
    public const int TrialDurationDays = 15;

    public bool IsLicensed { get; private set; }
    public long LicenseExpiresAt { get; private set; } // 0 = perpetuu
    public bool LicenseMachineLocked { get; private set; }
    public string? ActivationError { get; private set; }

    public event Action? Changed;

    private static string TrialStartFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GDC Vault", "trial-start.txt");

    private static string ActivationFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GDC Vault", "license.txt");

    private DateTimeOffset _trialStart;

    private LicenseManager()
    {
        EnsureTrialStarted();
        LoadSavedLicense();
    }

    private void EnsureTrialStarted()
    {
        var path = TrialStartFilePath;
        if (File.Exists(path) && long.TryParse(File.ReadAllText(path).Trim(), out var unixSeconds))
        {
            _trialStart = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return;
        }

        _trialStart = DateTimeOffset.Now;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, _trialStart.ToUnixTimeSeconds().ToString());
    }

    /// Zile intregi ramase din proba, rotunjit in sus.
    public int TrialDaysRemaining
    {
        get
        {
            var elapsed = DateTimeOffset.Now - _trialStart;
            var remaining = TimeSpan.FromDays(TrialDurationDays) - elapsed;
            return Math.Max(0, (int)Math.Ceiling(remaining.TotalDays));
        }
    }

    public bool IsTrialActive => TrialDaysRemaining > 0;

    /// Verificat inainte de a permite crearea unei intrari NOI (nu si
    /// vizualizarea/editarea celor existente).
    public bool IsUnlocked => IsLicensed || IsTrialActive;

    public bool Activate(string code)
    {
        ActivationError = null;
        var trimmed = code.Trim();
        try
        {
            var payload = LicenseCore.Validate(trimmed, ProductId);
            SaveLicense(trimmed);
            ApplyLicense(payload.ExpiresAt, payload.MachineLocked);
            Changed?.Invoke();
            return true;
        }
        catch (LicenseCore.ValidationError error)
        {
            ActivationError = MessageFor(error.Kind);
            Changed?.Invoke();
            return false;
        }
    }

    public void Deactivate()
    {
        IsLicensed = false;
        LicenseExpiresAt = 0;
        LicenseMachineLocked = false;
        var path = ActivationFilePath;
        if (File.Exists(path)) File.Delete(path);
        Changed?.Invoke();
    }

    private void LoadSavedLicense()
    {
        var path = ActivationFilePath;
        if (!File.Exists(path)) return;
        var code = File.ReadAllText(path).Trim();
        try
        {
            var payload = LicenseCore.Validate(code, ProductId);
            ApplyLicense(payload.ExpiresAt, payload.MachineLocked);
        }
        catch (LicenseCore.ValidationError)
        {
            // Cod salvat invalid/expirat — ramanem nelicentiati, fara sa aruncam mai departe.
        }
    }

    private void ApplyLicense(long expiresAt, bool machineLocked)
    {
        IsLicensed = true;
        LicenseExpiresAt = expiresAt;
        LicenseMachineLocked = machineLocked;
    }

    private static void SaveLicense(string code)
    {
        var path = ActivationFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, code);
    }

    private static string MessageFor(LicenseCore.ValidationErrorKind kind) => kind switch
    {
        LicenseCore.ValidationErrorKind.MalformedCode => "Cod invalid — verifică să nu lipsească vreun caracter.",
        LicenseCore.ValidationErrorKind.BadSignature => "Semnătura codului nu se potrivește.",
        LicenseCore.ValidationErrorKind.WrongProduct => "Codul e valid, dar pentru alt produs GDC.",
        LicenseCore.ValidationErrorKind.WrongMachine => "Codul e blocat pe alt calculator.",
        LicenseCore.ValidationErrorKind.Expired => "Codul a expirat.",
        _ => "Cod invalid.",
    };
}
