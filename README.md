# RideTogether

RideTogether is a carpooling MVP for offering rides and booking available seats. It includes an English-language search powered by a local model through Ollama.

- Browse rides and see how many seats are still available.
- Sign in to offer a ride, edit or delete your own rides, or book a seat on someone else's ride.
- Search with a request such as **Show rides from Gdańsk to Sopot**.

## Project structure

| Path | Contents |
| --- | --- |
| `RideTogether/` | ASP.NET Core API targeting .NET 10, Entity Framework Core, and SQLite |
| `RideTogetherFront/` | Angular 17 frontend |
| `RideTogether.sln` | Backend solution |

## Run locally on Windows

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), Node.js 20.x (20.9 or newer) with npm, and [Ollama](https://docs.ollama.com/windows).

### 1. Prepare SQLite

Place the initialized demo database at `RideTogether/RideTogetherDB.db` if it is supplied separately. The database needs the `User`, `Ride`, and `RideBooking` tables. For the demo below, it also needs two demo accounts and a ride with at least one available seat.

### 2. Start Ollama

Open Ollama and run these commands in PowerShell:

```powershell
ollama pull qwen2.5:3b
Invoke-RestMethod http://localhost:11434/api/tags
```

Check that `qwen2.5:3b` appears in the response. Keep Ollama running on the same machine as the API.

### 3. Start the API

From the repository root, run:

```powershell
cd RideTogether
dotnet dev-certs https --trust
dotnet run --launch-profile https
```

The API listens at `https://localhost:7172`. Start it from the backend directory so the relative SQLite path resolves to the demo database.

### 4. Start Angular

Open a second terminal at the repository root and run:

```powershell
cd RideTogetherFront
npm ci
npm run start-ssl
```

Open [https://localhost:4200](https://localhost:4200). The HTTPS script generates the local development certificate files used by Angular.

## Try the demo

1. Open the ride list and look for a ride from Gdańsk to Sopot with an available seat.
2. Under **Search in English**, enter **Show rides from Gdańsk to Sopot** and select **Search rides**.
3. Sign in as the demo account that does not own the ride and select **Book a seat**. Check the booking confirmation and the updated seat count.
4. Select **Clear search** to return to the full list.

<!-- Before submitting, add the public demo usernames and passwords here.
     Use credentials created specifically for this project. -->

<!-- If the assignment requires a demo video, add a link to the recording here. -->

## How the search works

Angular sends the search text to `POST /Ride/search`. The API asks the local Ollama model to extract the departure, destination, date, and earliest departure time as structured data. The API validates those filters and applies them to the rides and seat availability from SQLite. Relative dates such as “tomorrow” use Warsaw local time.

If the search reports that it is unavailable, check the Ollama model list in step 2. If the search returns no matches, check that the route exists in the full list and has an available seat.

## AI assistance during development

ChatGPT helped draft parts of the booking flow, the Angular search form, and the instructions used to extract filters. At runtime, RideTogether uses the local `qwen2.5:3b` model through Ollama for search.
