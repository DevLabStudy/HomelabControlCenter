# Homelab Control Center — with Orbit

**Homelab Control Center** to aplikacja desktopowa Windows dla homelabu, diagnostyki sieci i zarządzania serwerami SSH. Integracja **with Orbit** dodaje lekki panel Docker sterowany przez Orbit API — bez opuszczania aplikacji.

## Najważniejsze funkcje

- zarządzanie zapisanymi węzłami SSH i terminalem,
- narzędzia sieciowe: wykrywanie LAN, ping, traceroute, skan TCP i DNS,
- raport diagnostyczny komputera Windows,
- panel Docker **Powered by Orbit**:
  - logowanie do Orbit API,
  - lista kontenerów, obrazów, statusów i portów,
  - odświeżanie danych,
  - uruchamianie, zatrzymywanie i restart kontenerów,
  - otwieranie wystawionej usługi w nowej karcie przeglądarki,
  - przycisk przejścia do pełnego panelu web Orbit,
- ustawienia połączenia Orbit i parowanie mobilne QR,
- gotowy skrypt instalatora Inno Setup.

## Wymagania

- Windows 10/11 64-bit,
- uruchomiony Orbit Control, lokalnie lub na serwerze,
- konto użytkownika w Orbit,
- Rider lub .NET SDK 10 tylko do kompilowania ze źródeł.

## Aktualizacja projektu w Riderze

1. Otwórz `HomelabControlCenter.sln` w Riderze.
2. Wybierz **Git → Pull** i pobierz najnowszy commit z `master`.
3. W Riderze wybierz **Build → Rebuild Solution**.
4. Uruchom aplikację przyciskiem ▶.

Jeśli Rider zgłosi konflikt lokalnych zmian, najpierw wykonaj commit albo użyj **Git → Stash**.

## Konfiguracja Orbit

W aplikacji otwórz **Ustawienia i Parowanie QR** i wpisz:

```text
Adres API: http://localhost:5001
Email: email użytkownika Orbit
Hasło: hasło użytkownika Orbit
```

Jeśli Orbit działa na innym komputerze, użyj adresu osiągalnego z sieci LAN, np. `http://192.168.1.50:5001`. Następnie przejdź do **Docker i Kontenery** i kliknij **Połącz i odśwież**.

Przycisk **Panel web ↗** otwiera pełny Orbit Control w domyślnej przeglądarce. Przycisk **Otwórz usługę ↗** działa dla kontenerów z wystawionym portem HTTP.

## Budowanie wersji produkcyjnej

W terminalu Ridera lub PowerShell:

```powershell
dotnet restore
dotnet publish HomelabControlCenter.sln -c Release -r win-x64 --self-contained true
```

Pliki aplikacji znajdą się w:

```text
HomelabControlCenter\bin\Release\net10.0-windows\win-x64\publish
```

## Instalator Inno Setup

1. Zainstaluj Inno Setup z `https://jrsoftware.org/isinfo.php`.
2. Wykonaj polecenie `dotnet publish` z poprzedniej sekcji.
3. Otwórz `OrbitSetup.iss` w Inno Setup.
4. Kliknij **Build → Compile**.
5. Instalator powstanie w `artifacts\installer\HomelabControlCenter-Setup.exe`.

Można też wskazać własny katalog publikacji:

```powershell
iscc.exe /DSourceDir="C:\sciezka\do\publish" OrbitSetup.iss
```

## Repozytoria

- Orbit Control: https://github.com/DevLabStudy/orbit
- Homelab Control Center: https://github.com/DevLabStudy/HomelabControlCenter
- Kontakt i wsparcie: https://devlabstudy.ovh

## Licencja

Projekt jest rozwijany przez DevLabStudy.
