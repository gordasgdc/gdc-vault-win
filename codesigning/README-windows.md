# codesigning/ — semnare Windows (Self-Signed, testare internă)

Acest document acoperă semnarea Windows pentru GDC Vault (portat din
CGConvertor, 2026-09-06, vezi CLAUDE.md Regula 34).

## De ce Self-Signed, și ce NU rezolvă

Un certificat self-signed **nu elimină avertismentul SmartScreen/"Unknown
publisher"** pentru publicul larg — doar un certificat real de la o CA
publică (cu reputație acumulată) sau un certificat EV fac asta. Self-signed
e util STRICT pentru:
- testare internă (buildurile pe care le rulează Cristi însuși),
- distribuire către un cerc restrâns de colaboratori care importă manual
  certificatul public (`.cer`) în Trusted Root o singură dată.

La lansarea comercială publică, planul e Azure Trusted Signing sau un
certificat EV (HSM cloud) — vezi CLAUDE.md Regula 34 pentru context complet.

## Certificat COMUN pentru toate aplicațiile GDC

Certificatul e **comun pentru toate aplicațiile GDC** (decizie explicită
Cristi) — secretele CI se numesc IDENTIC în toate repo-urile:
`WIN_SELFSIGN_PFX_BASE64` și `WIN_SELFSIGN_PFX_PASSWORD`. Dacă
certificatul a fost deja generat pentru alt repo GDC (ex. CGConvertor),
NU regenera — reîncarcă doar aceleași două secrete (același `.pfx`/
parolă) în acest repo (pasul 2 de mai jos). Un colaborator care a
importat deja `.cer`-ul o singură dată are automat încredere în orice
build viitor, din orice repo GDC.

## Setup unic (o dată, făcut DIRECT de Cristi pe Windows real)

Certificatul (privat, cu cheie) nu trece niciodată prin conversația cu
Claude — la fel ca orice altă parolă/cheie din ecosistem.

1. Pe Windows real (Parallels e suficient), deschide PowerShell **ca
   Administrator** și rulează (DOAR dacă certificatul nu există deja
   dintr-un alt repo GDC):
   ```powershell
   .\codesigning\generate-self-signed-cert.ps1
   ```
   Scriptul cere o parolă nouă (pentru `.pfx`) și produce două fișiere:
   - `gdc-selfsign.pfx` — **PRIVAT**, nu se distribuie, nu se
     comite în git.
   - `gdc-selfsign.cer` — **PUBLIC**, se distribuie colaboratorilor.

2. Încarcă `.pfx`-ul ca secrete GitHub Actions — comenzile exacte sunt
   afișate la finalul scriptului (necesită `gh` CLI autentificat pe acea
   mașină):
   ```powershell
   gh secret set WIN_SELFSIGN_PFX_BASE64 --repo gordasgdc/gdc-vault-win --body $b64
   gh secret set WIN_SELFSIGN_PFX_PASSWORD --repo gordasgdc/gdc-vault-win
   ```

3. Șterge `.pfx`-ul local imediat după (`Remove-Item gdc-selfsign.pfx -Force`)
   — rămâne doar în secretele CI, criptate.

4. Distribuie `gdc-selfsign.cer` colaboratorilor. Pe fiecare
   mașină a lor, o singură dată: dublu-click → **Install Certificate** →
   **Local Machine** → "Place all certificates in the following store" →
   **Trusted Root Certification Authorities**.

Odată făcuți pașii 1-4, **fiecare build viitor din CI** (push pe `main`
sau `workflow_dispatch`) semnează automat `GDCVault.exe` și
`GDCVaultSetup.exe` cu ACELAȘI certificat — colaboratorii nu mai trebuie
să reimporte nimic la versiunile următoare, în niciun repo GDC.

## Ce face CI-ul automat (`.github/workflows/build-windows.yml`)

- Dacă secretele NU sunt setate: build-ul continuă **nesemnat**, exact ca
  până acum — nicio eroare, nicio schimbare de comportament.
- Dacă secretele SUNT setate: după ce `GDCVault.exe` (publish .NET
  self-contained) și installer-ul final (Inno Setup, `GDCVaultSetup.exe`)
  există, ambele sunt semnate cu `signtool.exe` (localizat dinamic din
  Windows Kits, cu timestamp), apoi verificate cu
  `Get-AuthenticodeSignature` — confirmă DOAR că semnătura a fost atașată
  corect, fără să ceară lanț de încredere complet (asta ar eșua mereu pe
  un runner CI proaspăt, care nu are certificatul în Trusted Root —
  normal pentru self-signed, nu un bug). Un eșec real de semnare (fișier
  fără nicio semnătură) tot oprește build-ul (CI roșu).

## Regenerarea certificatului (dacă expiră sau e compromis)

Rulează din nou `generate-self-signed-cert.ps1`, reîncarcă secretele
(pasul 2 de mai sus îi suprascrie pe cei vechi) **în FIECARE repo GDC**,
nu doar în acesta — altfel repo-urile rămân cu cheia veche și build-urile
lor vor fi semnate cu certificate diferite. **Toți colaboratorii trebuie
să reimporte noul `.cer`**, altfel văd din nou avertismentul pentru
versiunile semnate cu noul certificat. Evită regenerarea inutilă — de
asta scriptul folosește o valabilitate de 5 ani.
