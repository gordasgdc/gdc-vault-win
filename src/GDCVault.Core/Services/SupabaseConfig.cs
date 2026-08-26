namespace GDCVault.Core.Services;

/// Port 1:1 al SupabaseConfig.cs din GDC Plugin Manager Windows — detaliile
/// backend-ului de analytics (Supabase Postgres, prin PostgREST). Cheia
/// "anon" e sigura de comis: fiecare tabel accesibil cu ea are Row Level
/// Security cu policy INSERT-only pentru `anon`. Acelasi proiect Supabase
/// ca tot ecosistemul GDC.
public static class SupabaseConfig
{
    public const string ProjectUrl = "https://jvxrclpyngdcqnbwvtfn.supabase.co";

    public const string AnonKey =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Imp2eHJjbHB5bmdkY3FuYnd2dGZuIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODcwODMxMDksImV4cCI6MjEwMjY1OTEwOX0.uCLgrVPLhovwdBc82KermRbtWykquWoJmg9WmGk2L-s";

    public static string RestUrl(string table) => $"{ProjectUrl}/rest/v1/{table}";
    public static string RpcUrl(string function) => $"{ProjectUrl}/rest/v1/rpc/{function}";
}
