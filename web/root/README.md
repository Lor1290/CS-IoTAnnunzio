# IoT Dashboard — Setup Guide

## Struttura del progetto

```
IoTDashboard/
├── Models/Models.cs          # Modelli del DB (User, Device, Sensor, Alert…)
├── Services/
│   ├── DatabaseService.cs    # Tutte le query Dapper verso MySQL
│   ├── AuthService.cs        # Login, sessioni, logout
│   └── SensorHub.cs          # SignalR hub + polling background
├── Components/
│   ├── App.razor             # Root HTML (PWA manifest incluso)
│   ├── Routes.razor
│   ├── _Imports.razor
│   ├── Layout/MainLayout.razor
│   └── Pages/
│       ├── Login.razor
│       ├── Logout.razor
│       ├── Home.razor         # Dashboard principale
│       ├── Devices.razor      # Lista dispositivi
│       ├── DeviceDetail.razor # Sensori real-time via SignalR
│       └── Alerts.razor       # Alert con acknowledge
├── wwwroot/
│   ├── app.css               # CSS responsive (mobile/tablet/desktop)
│   ├── manifest.json         # PWA manifest
│   ├── js/app.js
│   └── icons/                # ← AGGIUNGI icon-192.png e icon-512.png
├── Properties/launchSettings.json
├── appsettings.json          # Connection string Railway (produzione)
├── appsettings.Development.json  # Connection string locale (gitignore'd)
├── Dockerfile
├── railway.toml
└── IoTDashboard.csproj
```

---

## 1. Dipendenze NuGet

```bash
dotnet add package Dapper --version 2.1.35
dotnet add package MySqlConnector --version 2.3.7
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package Microsoft.AspNetCore.SignalR --version 1.1.0
```

---

## 2. Icone PWA

Crea (o genera) due icone PNG e mettile in `wwwroot/icons/`:
- `icon-192.png` (192×192)
- `icon-512.png` (512×512)

Puoi generarle su https://realfavicongenerator.net/ o usare qualsiasi immagine.

---

## 3. Test in locale

### 3a. MySQL locale (opzionale)
Se vuoi testare offline puoi usare Docker:
```bash
docker run -d \
  --name cs-iot-mysql \
  -e MYSQL_ROOT_PASSWORD=root \
  -e MYSQL_DATABASE=CS_IOT \
  -p 3306:3306 \
  mysql:8.0
```
Poi esegui lo schema SQL del progetto.

### 3b. Oppure: punta direttamente a Railway
Copia la connection string del tuo MySQL Railway e mettila in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "MySql": "Server=roundhouse.proxy.rlwy.net;Port=XXXXX;Database=CS_IOT;User=root;Password=XXXXXXXX;SslMode=Required;"
  }
}
```

### 3c. Avvia
```bash
cd IoTDashboard
dotnet restore
dotnet run
```
Apri http://localhost:5000

---

## 4. Creare il primo utente

Il sistema usa BCrypt per le password. Per creare il primo utente esegui questo
snippet C# in una Console App (o usa uno script SQL con un hash pre-generato):

```csharp
// Script one-shot per inserire admin — eseguilo UNA VOLTA
using BCrypt.Net;
var hash = BCrypt.HashPassword("latuapassword");
Console.WriteLine(hash);
// Poi: INSERT INTO USERS (email, password_hash, full_name, role, is_verified)
//       VALUES ('tu@esempio.com', '<hash>', 'Il Tuo Nome', 'viewer', 1);
```

Oppure usa questo comando MySQL diretto (sostituisci il hash):
```sql
INSERT INTO USERS (email, password_hash, full_name, role, is_verified)
VALUES (
  'admin@scuola.it',
  '$2a$11$XXXX...', -- genera con BCrypt.HashPassword()
  'Admin',
  'viewer',
  1
);
```

---

## 5. Deploy su Railway

### 5a. Configura le variabili d'ambiente su Railway
Nel pannello Railway del tuo servizio Blazor, aggiungi:

```
ConnectionStrings__MySql=Server=...;Port=...;Database=CS_IOT;User=...;Password=...;SslMode=Required;
```
> Nota: Railway injetta `PORT` automaticamente. Il Dockerfile lo legge.

### 5b. Deploy
```bash
# Collega il repo a Railway e il deploy parte automaticamente,
# oppure usa Railway CLI:
railway up
```

### 5c. Verifica
Railway mostrerà l'URL pubblico. Aprilo su telefono → tocca il banner
"Aggiungi alla schermata Home" per installare la PWA.

---

## 6. Aggiornamenti real-time (SignalR)

Il `SensorPollingService` fa polling ogni **5 secondi** sulla tabella
`SENSORSREADING` e manda i nuovi valori via SignalR a tutti i client connessi
alla pagina `/devices/{id}`.

Quando l'ESP32 scrive nel DB, entro 5 s il browser aggiornerà i valori senza
bisogno di ricaricare la pagina.

Puoi ridurre l'intervallo modificando `Task.Delay(TimeSpan.FromSeconds(5))` in
`SensorHub.cs` se vuoi aggiornamenti più veloci.

---

## 7. PWA — Installazione su telefono

1. Apri l'URL del sito su Chrome (Android) o Safari (iOS)
2. Android: tocca "Aggiungi a schermata Home" nel menu del browser
3. iOS: tocca l'icona condividi → "Aggiungi a schermata Home"

L'app si aprirà fullscreen senza barra del browser, come una app nativa.
