# 🏖️ CutiApp — Panduan Lengkap Step by Step
## Aplikasi Pengajuan Cuti | Microservice | .NET 8 + Angular 17 + PostgreSQL

---

## 🗂️ Daftar Isi
1. [Prasyarat & Instalasi Tools](#1-prasyarat--instalasi-tools)
2. [Gambaran Arsitektur](#2-gambaran-arsitektur)
3. [Setup Struktur Folder](#3-setup-struktur-folder)
4. [Membangun AuthService (.NET 8)](#4-membangun-authservice)
5. [Membangun LeaveService (.NET 8)](#5-membangun-leaveservice)
6. [Membangun API Gateway (Ocelot)](#6-membangun-api-gateway)
7. [Setup Docker & PostgreSQL](#7-setup-docker--postgresql)
8. [Membuat EF Core Migrations](#8-membuat-ef-core-migrations)
9. [Membangun Frontend (Angular 17)](#9-membangun-frontend-angular-17)
10. [Menjalankan Seluruh Aplikasi](#10-menjalankan-seluruh-aplikasi)
11. [Pengujian API (Endpoint Lengkap)](#11-pengujian-api)
12. [Akun Demo & Alur Penggunaan](#12-akun-demo--alur-penggunaan)
13. [Troubleshooting](#13-troubleshooting)

---

## 1. Prasyarat & Instalasi Tools

Pastikan semua tools berikut sudah terinstall di komputer Anda:

### Tools yang Dibutuhkan

| Tool | Versi | Download |
|------|-------|---------|
| .NET SDK | 8.0+ | https://dotnet.microsoft.com/download |
| Node.js | 18+ | https://nodejs.org |
| Angular CLI | 17+ | `npm install -g @angular/cli` |
| Docker Desktop | Latest | https://docker.com/products/docker-desktop |
| Git | Latest | https://git-scm.com |

### Verifikasi Instalasi

Buka terminal dan jalankan perintah berikut satu per satu:

```bash
dotnet --version        # Harus menampilkan 8.x.x
node --version          # Harus menampilkan v18.x.x atau lebih
npm --version           # Harus menampilkan 9.x.x atau lebih
ng version              # Harus menampilkan Angular CLI 17.x.x
docker --version        # Harus menampilkan Docker version xx.x.x
docker compose version  # Harus menampilkan Docker Compose version vx.x.x
```

### Install dotnet-ef Tool (untuk migrations)

```bash
dotnet tool install --global dotnet-ef
dotnet ef --version    # Verifikasi: Entity Framework Core .NET Command-line Tools 8.x.x
```

---

## 2. Gambaran Arsitektur

```
┌──────────────────────────────────────────────────────────────┐
│                    ANGULAR FRONTEND :4200                    │
│  Login | Dashboard | Ajukan Cuti | Approval | User Mgmt     │
└────────────────────────┬─────────────────────────────────────┘
                         │ HTTP Request + JWT Bearer Token
                         ▼
┌──────────────────────────────────────────────────────────────┐
│                 API GATEWAY (Ocelot) :5000                   │
│         Route /api/auth → AuthService :5001                  │
│         Route /api/leave → LeaveService :5002                │
│         Route /api/users → AuthService :5001                 │
└──────────┬──────────────────────┬────────────────────────────┘
           │                      │
    ┌──────▼──────┐        ┌──────▼──────┐
    │ AuthService │        │LeaveService │
    │    :5001    │        │    :5002    │
    │  .NET 8 API │        │  .NET 8 API │
    └──────┬──────┘        └──────┬──────┘
           │                      │
    ┌──────▼──────┐        ┌──────▼──────┐
    │   auth_db   │        │   leave_db  │
    │ PostgreSQL  │        │ PostgreSQL  │
    │   :5432     │        │   :5433     │
    └─────────────┘        └─────────────┘

ROLES:
  Employee   → Submit cuti, lihat riwayat, batalkan cuti pending
  Approver   → Approve/reject cuti, lihat semua pengajuan
  SuperAdmin → Semua akses + manajemen user + statistik dashboard
```

---

## 3. Setup Struktur Folder

Buat folder project dari terminal:

```bash
# Buat root folder
mkdir leave-management
cd leave-management

# Buat folder backend
mkdir -p backend/AuthService/Controllers
mkdir -p backend/AuthService/Models
mkdir -p backend/AuthService/Data
mkdir -p backend/AuthService/Services
mkdir -p backend/LeaveService/Controllers
mkdir -p backend/LeaveService/Models
mkdir -p backend/LeaveService/Data
mkdir -p backend/ApiGateway

# Inisiasi proyek .NET untuk setiap service
cd backend

# AuthService
cd AuthService
dotnet new webapi -n AuthService --no-additional-deps --framework net8.0
# Hapus file template yang tidak diperlukan
rm -f WeatherForecast.cs Controllers/WeatherForecastController.cs
cd ..

# LeaveService
cd LeaveService
dotnet new webapi -n LeaveService --no-additional-deps --framework net8.0
rm -f WeatherForecast.cs Controllers/WeatherForecastController.cs
cd ..

# ApiGateway
cd ApiGateway
dotnet new webapi -n ApiGateway --no-additional-deps --framework net8.0
cd ../..
```

---

## 4. Membangun AuthService

### 4.1 Install NuGet Packages

```bash
cd backend/AuthService

dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

### 4.2 Salin File ke AuthService

Salin file-file berikut dari folder yang sudah diberikan:

```
backend/AuthService/
├── AuthService.csproj          ← sudah ada (dari dotnet new)
├── Program.cs                  ← GANTI dengan file yang diberikan
├── appsettings.json            ← GANTI dengan file yang diberikan
├── Dockerfile                  ← BUAT dengan file yang diberikan
├── Controllers/
│   └── AuthController.cs       ← BUAT dengan file yang diberikan
├── Models/
│   └── User.cs                 ← BUAT dengan file yang diberikan
├── Data/
│   └── AuthDbContext.cs        ← BUAT dengan file yang diberikan
└── Services/
    └── TokenService.cs         ← BUAT dengan file yang diberikan
```

### 4.3 Verifikasi Build AuthService

```bash
cd backend/AuthService
dotnet build
# Output: Build succeeded.
cd ../..
```

---

## 5. Membangun LeaveService

### 5.1 Install NuGet Packages

```bash
cd backend/LeaveService

dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

### 5.2 Salin File ke LeaveService

```
backend/LeaveService/
├── LeaveService.csproj         ← sudah ada (dari dotnet new)
├── Program.cs                  ← GANTI dengan file yang diberikan
├── appsettings.json            ← GANTI dengan file yang diberikan
├── Dockerfile                  ← BUAT dengan file yang diberikan
├── Controllers/
│   └── LeaveController.cs      ← BUAT dengan file yang diberikan
├── Models/
│   └── LeaveRequest.cs         ← BUAT dengan file yang diberikan
└── Data/
    └── LeaveDbContext.cs       ← BUAT dengan file yang diberikan
```

### 5.3 Verifikasi Build LeaveService

```bash
cd backend/LeaveService
dotnet build
# Output: Build succeeded.
cd ../..
```

---

## 6. Membangun API Gateway

### 6.1 Install NuGet Packages

```bash
cd backend/ApiGateway

dotnet add package Ocelot --version 22.0.1
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
```

### 6.2 Salin File ke ApiGateway

```
backend/ApiGateway/
├── ApiGateway.csproj           ← sudah ada
├── Program.cs                  ← GANTI dengan file yang diberikan
├── appsettings.json            ← GANTI dengan file yang diberikan
├── ocelot.json                 ← BUAT dengan file yang diberikan
└── Dockerfile                  ← BUAT dengan file yang diberikan
```

### 6.3 Konfigurasi ocelot.json

File `ocelot.json` berisi routing rules:
- `GET/POST /api/auth/**` → AuthService (port 5001)
- `GET/POST/PUT /api/leave/**` → LeaveService (port 5002)
- `GET/POST /api/users/**` → AuthService (port 5001)

### 6.4 Verifikasi Build ApiGateway

```bash
cd backend/ApiGateway
dotnet build
# Output: Build succeeded.
cd ../..
```

---

## 7. Setup Docker & PostgreSQL

### 7.1 Salin docker-compose.yml

Salin file `docker-compose.yml` yang diberikan ke folder `backend/`.

### 7.2 Jalankan Database Saja (untuk development lokal)

Untuk development, jalankan hanya database dengan Docker, lalu jalankan services secara manual:

```bash
cd backend

# Jalankan HANYA database (tanpa build services)
docker compose up auth-db leave-db user-db -d

# Tunggu beberapa detik sampai PostgreSQL ready
docker compose ps
# Semua database harus berstatus "healthy" atau "running"
```

### 7.3 Verifikasi Database Berjalan

```bash
# Cek koneksi ke auth-db
docker exec -it backend-auth-db-1 psql -U postgres -c "\l"
# Harus menampilkan list database termasuk auth_db

# Cek koneksi ke leave-db  
docker exec -it backend-leave-db-1 psql -U postgres -c "\l"
# Harus menampilkan list database termasuk leave_db
```

---

## 8. Membuat EF Core Migrations

> ⚠️ Pastikan database sudah running (Step 7) sebelum menjalankan migration.

### 8.1 Migration AuthService

```bash
cd backend/AuthService

# Buat migration
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# Apply migration ke database
dotnet ef database update

# Output: Done.
```

### 8.2 Migration LeaveService

```bash
cd ../LeaveService

# Buat migration
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# Apply migration ke database
dotnet ef database update

# Output: Done.
```

### 8.3 Verifikasi Tabel Terbuat

```bash
# Cek tabel di auth_db
docker exec -it backend-auth-db-1 psql -U postgres -d auth_db -c "\dt"
# Harus muncul tabel: Users, __EFMigrationsHistory

# Cek tabel di leave_db
docker exec -it backend-leave-db-1 psql -U postgres -d leave_db -c "\dt"
# Harus muncul tabel: LeaveRequests, __EFMigrationsHistory
```

---

## 9. Membangun Frontend Angular 17

### 9.1 Buat Proyek Angular

```bash
# Dari root folder leave-management/
cd frontend

# Buat project Angular baru
ng new leave-app \
  --standalone \
  --routing \
  --style=css \
  --skip-git

cd leave-app
```

### 9.2 Struktur Folder Angular

Buat struktur folder berikut:

```bash
mkdir -p src/app/core
mkdir -p src/app/pages/login
mkdir -p src/app/pages/dashboard
mkdir -p src/app/pages/leave/submit-leave
mkdir -p src/app/pages/leave/my-leaves
mkdir -p src/app/pages/leave/pending-leaves
mkdir -p src/app/pages/leave/all-leaves
mkdir -p src/app/pages/admin/user-management
mkdir -p src/environments
```

### 9.3 Salin Semua File Angular

Salin semua file yang diberikan ke lokasi yang sesuai:

```
src/
├── main.ts                                          ← GANTI
├── index.html                                       ← GANTI
├── styles.css                                       ← GANTI
├── environments/
│   └── environment.ts                               ← BUAT
└── app/
    ├── app.component.ts                             ← GANTI
    ├── app.config.ts                                ← GANTI
    ├── app.routes.ts                                ← BUAT
    ├── core/
    │   ├── auth.service.ts                          ← BUAT
    │   ├── auth.interceptor.ts                      ← BUAT
    │   ├── auth.guard.ts                            ← BUAT
    │   ├── leave.service.ts                         ← BUAT
    │   └── user.service.ts                          ← BUAT
    └── pages/
        ├── login/
        │   └── login.component.ts                  ← BUAT
        ├── dashboard/
        │   └── dashboard.component.ts               ← BUAT
        ├── leave/
        │   ├── submit-leave/
        │   │   └── submit-leave.component.ts        ← BUAT
        │   ├── my-leaves/
        │   │   └── my-leaves.component.ts           ← BUAT
        │   ├── pending-leaves/
        │   │   └── pending-leaves.component.ts      ← BUAT
        │   └── all-leaves/
        │       └── all-leaves.component.ts          ← BUAT
        └── admin/
            └── user-management/
                └── user-management.component.ts     ← BUAT
```

### 9.4 Salin File Konfigurasi

```bash
# Dari folder frontend/leave-app/
# Salin: proxy.conf.json, angular.json, tsconfig.json
```

### 9.5 Install Dependencies

```bash
cd frontend/leave-app
npm install
```

---

## 10. Menjalankan Seluruh Aplikasi

### Opsi A: Jalankan Manual (Direkomendasikan untuk Development)

Buka **4 terminal terpisah**:

#### Terminal 1 — AuthService
```bash
cd backend/AuthService
dotnet run
# Berjalan di: http://localhost:5001
# Swagger UI: http://localhost:5001/swagger
```

#### Terminal 2 — LeaveService
```bash
cd backend/LeaveService
dotnet run
# Berjalan di: http://localhost:5002
# Swagger UI: http://localhost:5002/swagger
```

#### Terminal 3 — API Gateway
```bash
cd backend/ApiGateway
dotnet run
# Berjalan di: http://localhost:5000
```

#### Terminal 4 — Angular Frontend
```bash
cd frontend/leave-app
ng serve
# Berjalan di: http://localhost:4200
```

Buka browser dan akses: **http://localhost:4200**

---

### Opsi B: Jalankan Semua dengan Docker Compose (Production-like)

```bash
cd backend

# Build dan jalankan semua services sekaligus
docker compose up --build -d

# Cek status semua services
docker compose ps

# Lihat logs semua services
docker compose logs -f

# Lihat log service tertentu
docker compose logs -f auth-service
docker compose logs -f leave-service
docker compose logs -f api-gateway
```

Setelah semua container berstatus **running**:
- Frontend: **http://localhost:4200** (jalankan Angular terpisah)
- API Gateway: **http://localhost:5000**
- AuthService: **http://localhost:5001**
- LeaveService: **http://localhost:5002**

#### Stop semua services:
```bash
docker compose down

# Stop DAN hapus volume (data database):
docker compose down -v
```

---

## 11. Pengujian API

### 11.1 Test via Swagger UI

- AuthService: http://localhost:5001/swagger
- LeaveService: http://localhost:5002/swagger

### 11.2 Test via curl

#### Login sebagai SuperAdmin:
```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"superadmin@company.com","password":"Admin@123"}'
```

Simpan `token` dari response, gunakan untuk request selanjutnya.

#### Dapatkan semua users (SuperAdmin only):
```bash
TOKEN="<paste token di sini>"

curl -X GET http://localhost:5001/api/auth/users \
  -H "Authorization: Bearer $TOKEN"
```

#### Login sebagai Employee, lalu ajukan cuti:
```bash
# Step 1: Login
RESPONSE=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"employee@company.com","password":"Employee@123"}')

TOKEN=$(echo $RESPONSE | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

# Step 2: Submit cuti melalui Gateway
curl -X POST http://localhost:5000/api/leave/submit \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "leaveType": "Annual",
    "startDate": "2025-06-01",
    "endDate": "2025-06-05",
    "reason": "Liburan keluarga ke Bali"
  }'
```

#### Login sebagai Approver, approve cuti:
```bash
# Step 1: Login Approver
RESPONSE=$(curl -s -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"approver@company.com","password":"Approver@123"}')

APPROVER_TOKEN=$(echo $RESPONSE | python3 -c "import sys,json; print(json.load(sys.stdin)['token'])")

# Step 2: Lihat pengajuan pending
curl -X GET http://localhost:5000/api/leave/pending \
  -H "Authorization: Bearer $APPROVER_TOKEN"

# Step 3: Approve (ganti LEAVE_ID dengan id dari response di atas)
LEAVE_ID="<id dari step 2>"

curl -X PUT http://localhost:5000/api/leave/approve/$LEAVE_ID \
  -H "Authorization: Bearer $APPROVER_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"isApproved": true, "note": "Disetujui, selamat berlibur!"}'
```

### 11.3 Daftar Lengkap Endpoint

| Method | Endpoint | Role | Keterangan |
|--------|----------|------|------------|
| POST | /api/auth/login | Public | Login, dapat JWT token |
| POST | /api/auth/register | SuperAdmin | Tambah user baru |
| GET | /api/auth/me | All | Info user saat ini |
| GET | /api/auth/users | SuperAdmin | List semua user |
| POST | /api/leave/submit | Employee | Ajukan cuti baru |
| GET | /api/leave/my | Employee | Cuti milik saya |
| PUT | /api/leave/cancel/{id} | Employee | Batalkan cuti pending |
| GET | /api/leave/pending | Approver,SuperAdmin | Daftar cuti pending |
| PUT | /api/leave/approve/{id} | Approver,SuperAdmin | Approve/reject cuti |
| GET | /api/leave/all | Approver,SuperAdmin | Semua cuti + filter |
| GET | /api/leave/stats | SuperAdmin | Statistik dashboard |
| GET | /api/leave/{id} | All | Detail cuti by ID |

---

## 12. Akun Demo & Alur Penggunaan

### Akun Default (di-seed otomatis)

| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | superadmin@company.com | Admin@123 |
| Approver | approver@company.com | Approver@123 |
| Employee | employee@company.com | Employee@123 |

### Alur Penggunaan Normal

```
1. Employee Login
   └─→ Dashboard: lihat statistik cuti pribadi
       └─→ "Ajukan Cuti" → isi form → submit
           └─→ Status: PENDING

2. Approver Login
   └─→ Dashboard: lihat jumlah pending
       └─→ "Pending Approval" → lihat kartu pengajuan
           └─→ Isi catatan opsional → klik ✅ Setujui / ❌ Tolak
               └─→ Status berubah: APPROVED / REJECTED

3. SuperAdmin Login
   └─→ Dashboard: lihat semua statistik + chart per departemen
       └─→ "Semua Pengajuan" → filter by status/dept/tanggal
       └─→ "Manajemen User" → tambah user baru
       └─→ Bisa juga approve/reject seperti Approver
```

---

## 13. Troubleshooting

### ❌ "Connection refused" saat dotnet run

**Penyebab:** Database belum berjalan.

```bash
# Pastikan Docker Desktop berjalan, lalu:
cd backend
docker compose up auth-db leave-db -d
docker compose ps   # Tunggu status: running
```

### ❌ "Migration tidak ditemukan" atau tabel tidak ada

```bash
# Hapus dan recreate migration
cd backend/AuthService
dotnet ef database drop --force
dotnet ef migrations remove
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
dotnet ef database update
```

### ❌ CORS Error di Angular

Pastikan di `Program.cs` setiap service sudah ada:

```csharp
builder.Services.AddCors(o => o.AddPolicy("AllowAll",
    p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
// ...
app.UseCors("AllowAll");
```

Dan pastikan urutan middleware benar:
```csharp
app.UseCors("AllowAll");         // ← SEBELUM
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

### ❌ JWT Token "Unauthorized 401"

Pastikan JWT Key, Issuer, dan Audience di semua service **identik**:

```json
"Jwt": {
  "Key": "SuperSecretKey123456789012345678901234",
  "Issuer": "LeaveApp",
  "Audience": "LeaveAppUsers"
}
```

> ⚠️ Key harus minimal 32 karakter untuk algoritma HmacSha256.

### ❌ Angular "ng: command not found"

```bash
npm install -g @angular/cli@17
# Atau jika permission error di Mac/Linux:
sudo npm install -g @angular/cli@17
```

### ❌ Port sudah digunakan

```bash
# Cari proses yang menggunakan port
lsof -i :5001   # Mac/Linux
netstat -ano | findstr :5001   # Windows

# Kill proses (Mac/Linux)
kill -9 <PID>
```

### ❌ Docker "no space left on device"

```bash
# Bersihkan image dan volume yang tidak terpakai
docker system prune -a --volumes
```

---

## ✅ Checklist Keberhasilan

Setelah mengikuti semua langkah, verifikasi:

- [ ] `dotnet build` berhasil di AuthService, LeaveService, ApiGateway
- [ ] `docker compose up auth-db leave-db -d` berjalan tanpa error
- [ ] `dotnet ef database update` berhasil di kedua service
- [ ] `dotnet run` di AuthService → http://localhost:5001/swagger terbuka
- [ ] `dotnet run` di LeaveService → http://localhost:5002/swagger terbuka
- [ ] `dotnet run` di ApiGateway → http://localhost:5000 merespons
- [ ] `ng serve` di Angular → http://localhost:4200 terbuka
- [ ] Login dengan akun SuperAdmin berhasil
- [ ] Login dengan akun Employee → submit cuti berhasil
- [ ] Login dengan akun Approver → approve/reject berhasil
- [ ] SuperAdmin dapat melihat statistik dan menambah user baru

---

*Dibuat dengan ❤️ — CutiApp Microservice Architecture*
*Stack: .NET 8 | Angular 17 | PostgreSQL | Ocelot API Gateway | Docker*
