# TogglExcelExporter

Jednostavna WPF aplikacija za izvoz detaljnih Toggl Track vremenskih zapisa u Excel (.xlsx) format.

## Značajke

- Unos API tokena, Workspace ID-a i e-maila (user agent)
- Automatsko pamćenje korisničkih postavki
- Export detaljnih vremenskih zapisa (po danu, projektu, opisu)
- Automatski predodabir datuma od ponedjeljka do petka
- Ikona aplikacije, onemogućeno resizeanje i centriranje prozora
- Klik na status otvara spremljeni Excel

## Kako koristiti

1. Pokreni aplikaciju
2. Unesi svoj API token, Workspace ID i e-mail
3. Odaberi datumski raspon (ili koristi zadani)
4. Klikni **Exportaj**
5. Excel se sprema na Desktop i automatski otvara lokacija datoteke

## Napomena

API mora biti deaktiviran prije exporta aktivnih (trenutno pokrenutih) timera.  
Svi podaci ostaju lokalno i nisu nigdje slani.
