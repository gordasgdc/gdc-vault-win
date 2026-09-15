using System.Security.Cryptography;
using System.Text;

namespace GDCVault.Core.Services;

/// Generator de parole. `RandomNumberGenerator` (criptografic), NU `Random`:
/// acela e previzibil dintr-o samanta si n-are ce cauta la parole.
public static class PasswordGenerator
{
    public sealed class Options
    {
        public int Length { get; set; } = 24;
        public bool IncludeUppercase { get; set; } = true;
        public bool IncludeDigits { get; set; } = true;
        public bool IncludeSymbols { get; set; } = true;
        /// Exclude caracterele care se confunda la citit: O/0, l/1/I.
        public bool AvoidAmbiguous { get; set; } = true;
    }

    public static string Generate(Options? options = null)
    {
        options ??= new Options();

        var lower = "abcdefghijkmnopqrstuvwxyz";
        var upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        var digits = "23456789";
        const string symbols = "!@#$%^&*()-_=+[]{};:,.?";

        if (!options.AvoidAmbiguous) { lower += "l"; upper += "IO"; digits += "01"; }

        var pools = new List<string> { lower };
        if (options.IncludeUppercase) pools.Add(upper);
        if (options.IncludeDigits) pools.Add(digits);
        if (options.IncludeSymbols) pools.Add(symbols);

        var length = Math.Max(options.Length, pools.Count);

        // Cate unul din FIECARE set ales, apoi restul la intamplare: altfel o
        // parola "cu simboluri" poate iesi fara niciun simbol, iar regula de
        // complexitate a site-ului o respinge.
        var chars = new List<char>();
        foreach (var pool in pools) chars.Add(pool[RandomNumberGenerator.GetInt32(pool.Length)]);

        var all = string.Concat(pools);
        while (chars.Count < length) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

        // Fisher-Yates cu sursa criptografica: fara amestecare, primele
        // caractere ar fi mereu in ordinea seturilor de mai sus.
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        var sb = new StringBuilder(chars.Count);
        foreach (var c in chars) sb.Append(c);
        return sb.ToString();
    }
}
