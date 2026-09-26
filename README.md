# GLPS – GreenLine Passenger System

ASP.NET MVC 5 (.NET Framework 4.8) admin app for vetting passengers and showing risk analytics
by threat type, airport, terminal and airline.

## Running locally

1. Open `glps.sln` in Visual Studio 2019+ and restore NuGet packages.
2. The database is SQL Server LocalDB (`glpsConnectionString` in `glps/Web.config`).
   Create/upgrade it from the Package Manager Console: `Update-Database`.
3. Add a user row to `dbo.Users` (`EmailID`, `Password`). A plaintext password is accepted once
   and is replaced with a PBKDF2 hash on the first successful login.
4. Run with IIS Express and sign in at `/Home/UserLogin`. Every other page requires login.

## Data model

| Table | Purpose | Linked by |
|---|---|---|
| `bchn_data` | Passenger risk register (terrorism, narcotics, smuggling, illegal immigration, revenue) | `Passport_number` |
| `appin_data` | Flight manifest (passenger on a flight) | `Passport_Number`, `Flight` |
| `asset_details` | Flights: departure/arrival airport, terminal | `Flight` |
| `airport_data` | Airport names | `IATA_code` |
| `airline_details` | Airlines | `letter_code` = flight number prefix |

Risk columns accept a percentage (`0`–`100`, `35%`), `Yes`/`No` (or `1`/`0`), `Low`/`Medium`/`High`,
or the category name itself. A passenger's overall risk is their highest category score;
`>= 50%` counts as high risk.

## Analytics

All charts are built from the database (`glps/Services/RiskAnalytics.cs`):

- **Passenger** – average score per threat type.
- **Airport** – average passenger risk by departure airport.
- **Terminal** – average passenger risk by arrival airport and terminal.
- **Flight** – average passenger risk by airline.
- **Dashboard** – totals, high-risk count and the highest-risk passengers.

## Excel import

`Home/UploadData` imports `bchn_data` rows from an `.xlsx` file (download the template from the
page). The whole file is validated first and nothing is saved if any row has errors.

## Tests

`glps.Tests` (MSTest) covers password hashing, risk scoring, the analytics joins and the Excel importer.
