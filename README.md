# Somaliland Health Information System

Hospital Information System for Somaliland, including the **Blood Bank** module (Hargeisa Group Hospital).

## What this system does

- Patient chart, appointments, and clinic orders (HIS)
- Doctor blood orders from Clinic Order (Other → Blood)
- Blood Bank: donors, screening, bags, inventory, and dispense
- Directed and volunteer donation, linked back to the same HIS patient

## Requirements

- Windows with Visual Studio 2022 (or later)
- .NET 6 SDK
- PostgreSQL 12+ on `localhost:5432`

## Setup

1. Create an **empty** PostgreSQL database from `database/empty_schema.sql` (see `database/README.md`). This is tables only — no patients.
2. Copy `KMU.HisOrder.MVC/appsettings.Development.json.example` to `KMU.HisOrder.MVC/appsettings.Development.json`.
3. Point that file at your empty database. **Do not commit it.**
4. Open `KMU.HisProject.sln`.
5. Run the `KMU.HisOrder.MVC` project (https://localhost:9037).

Extra Blood Bank scripts (if you only need Blood Bank tables on an existing HIS database):

- `KMU.HisOrder.MVC/Scripts/BloodBank_CreateTables.sql`
- `KMU.HisOrder.MVC/Scripts/BloodBank_GrantAuths.sql`
- `KMU.HisOrder.MVC/Scripts/BloodBank_Pipeline_Alter.sql`

## Security

Do not put production passwords, API keys, or patient data in GitHub.
Use `appsettings.Development.json` on each machine (ignored by git).
