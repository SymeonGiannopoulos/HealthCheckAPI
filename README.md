# HealthCheck Project

## Περιγραφή
Αυτό το project περιλαμβάνει:

- Backend: ASP.NET Core API (HealthCheckAPI)
- Frontend: React app (Frontend)
- Database: SQL Server

Το project μπορεί να τρέξει είτε με τοπικό SQL Server είτε με Docker.

---

## Απαιτήσεις
- .NET 7 SDK (για local run)
- Node.js & npm (για frontend, αν τρέχει local)
- SQL Server (local ή Docker)
- Προαιρετικά: Docker Desktop & Docker Compose

---

## Εκτέλεση με Local SQL Server

1. Άλλαξε το connection string στο `appsettings.json`:

"ConnectionStrings": {
  "SqlServerConnection": "Server=.\\HEALTHSQL;Database=HealthCheckDb;Trusted_Connection=True;Encrypt=False;"
}

2. Βεβαιώσου ότι η local SQL Server instance είναι ενεργή και μπορείς να συνδεθείς (π.χ., μέσω SSMS).
3. Τρέξε το backend:

cd HealthCheckAPI
dotnet run

4. Τρέξε το frontend (React):

cd Frontend
npm install
npm start

- Backend: https://localhost:7057
- Frontend: http://localhost:3003

---

## Εκτέλεση με Docker (προαιρετικά)

1. Κάνε clone το repository:

git clone https://github.com/username/HealthCheckProject.git
cd HealthCheckProject

2. Τρέξε τα containers:

docker-compose up --build

- Backend: https://localhost:7057
- Frontend: http://localhost:3003
- SQL Server container: db, port 1433

> Σημείωση: Το Docker θα κατεβάσει όλα τα NuGet πακέτα αυτόματα κατά το build του backend.

---

## Σημαντικές Σημειώσεις
- Το backend έχει CORS ρυθμισμένο για το http://localhost:3003.
- Αν χρησιμοποιείς local SQL Server, φρόντισε να είναι ενεργή η instance και η βάση HealthCheckDb να υπάρχει ή να δημιουργείται αυτόματα.
- Αν θέλεις να αλλάξεις ports ή passwords, τροποποίησε το docker-compose.yml ή το appsettings.json.

---

## Δομή Project

HealthCheckAPI/
├─ HealthCheckAPI/        # Backend
│  ├─ Dockerfile
│  ├─ .dockerignore
│  ├─ Program.cs
│  └─ appsettings.json
├─ Frontend/              # React frontend
│  ├─ Dockerfile
│  └─ .dockerignore
├─ docker-compose.yml
└─ README.md
