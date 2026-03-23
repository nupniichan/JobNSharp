# JobNSharp

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)

An unofficial job scraping library with the aim to aggregate listings from popular job boards into a single API
## Features

- **Multi-site support search**: One endpoint per site, or crawl all providers at once
- **Unified JSON model**: Same shape for title, company, location, salary hints, URL, and more
- **Resilient HTTP**: Automatic retries and circuit breaker for unstable networks
- **Optional proxies**: Configure proxy URLs in `appsettings` when you need them
- **Swagger UI**: Try the API in the browser when `Development` mode is on

## Tech Stack

- **Runtime**: .NET 10
- **Framework**: ASP.NET Core (minimal hosting + controllers)
- **API docs**: Swashbuckle (Swagger / OpenAPI)
- **Language**: C#

## Prerequisites

Before you start, simply make sure you already have the following:

- **[.NET SDK 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)**

Check the SDK:

```bash
dotnet --version
```

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/nupniichan/JobNSharp.git
cd JobNSharp
```

### 2. Restore and run

From the folder that contains `JobNSharp.slnx` (or the `JobNSharp` project folder):

```bash
dotnet restore
dotnet run --project JobNSharp/JobNSharp.csproj
```

By default the **HTTP** profile listens on **http://localhost:5021** and **https://localhost:7272** for **HTTPS** (see `Properties/launchSettings.json`)

### 3. Open Swagger (Development only)

When `ASPNETCORE_ENVIRONMENT` is `Development`, open:

**http://localhost:5021/swagger**

There you can see all endpoints and try them without extra tools.

## API overview

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/sites` | Lists supported sites |
| `GET` | `/api/job/crawl` | Crawls **one** site (query parameters) |
| `GET` | `/api/job/crawl-all` | Crawls **all** sites and merges results |

### Query parameters (`/api/job/crawl` and `/api/job/crawl-all`)

| Parameter | Required | Notes |
|-----------|----------|--------|
| `searchTerm` | Yes | Job keywords |
| `site` | Yes for `/crawl` only | `LinkedIn`, `Indeed`, or `Glassdoor` |
| `city`, `country` | No | Location filters |
| `jobType` | No | Can repeat, e.g. `FullTime`, `PartTime`, `Contract`, `Internship`, `Temporary` |
| `datePosted` | No | `Anytime`, `Past24Hours`, `PastWeek`, `PastMonth` |
| `remote` | No | `true` / `false` |
| `includeDescription` | No | Default `false` |
| `resultWanted` | No | Default `10` |

### Example (single site)

```http
GET http://localhost:5021/api/job/crawl?searchTerm=software%20engineer&site=Indeed&city=Ho%20Chi%20Minh%20City&country=VietNam&resultWanted=5
```

### Example (all sites)

```http
GET http://localhost:5021/api/job/crawl-all?searchTerm=developer&resultWanted=10
```

If one site fails, the API still returns results from the others (failed sites are skipped and logged).

## Configuration

Settings live in `JobNSharp/appsettings.json` (and optional `appsettings.Development.json`).

```json
{
  "Proxy": {
    "Urls": []
  },
  "RateLimit": {
    "MaxConcurrentRequests": 3
  }
}
```

- **`Proxy.Urls`**: Add proxy URLs if your environment needs them; leave empty if not.
- **`RateLimit.MaxConcurrentRequests`**: Maximum concurrent crawl operations.

**Note:** Please use a rotating proxy provider because this repository doesn't support automatic rotation proxy yet.
**Keep in mind that this structure will be refactor to .env use for better security**
## Project structure

```
JobNSharp/
├── JobNSharp/
│   ├── Controllers/       # HTTP API (job crawl, sites list)
│   ├── Interfaces/        # Abstractions for providers and services
│   ├── Models/            # Requests, results, enums
│   ├── Services/          # Crawl orchestration, proxy helper
│   ├── Sites/             # LinkedIn, Indeed, Glassdoor providers
│   ├── Utils/             # Rate limiter, exception handler, helpers, etc
│   ├── Program.cs         # App setup, HttpClient + Polly
│   └── appsettings.json   # Proxy and rate limit settings
├── JobNSharp.slnx
└── README.md
```

## Troubleshooting

- **Port in use**: Change `applicationUrl` in `Properties/launchSettings.json`, or run with another URL:
  `dotnet run --project JobNSharp/JobNSharp.csproj --urls "http://localhost:5055"`
- **Empty or partial results**: Some sites block automated access; proxies or different networks may help, but please remember that always follow each site’s **terms of use** and **robots rules**.
- **SSL / HTTPS**: Use the **https** profile in Visual Studio or `launchSettings.json` if you need HTTPS locally.

## Contributing

1. Fork the repository
2. Create a branch: `git checkout -b feature/your-awesome-idea`
3. Commit your changes
4. Push and open a Pull Request

## Acknowledgements
Special thanks to the [JobSpy](https://github.com/speedyapply/JobSpy) repository for providing valuable insights and inspiration for this repository.

## License
Take a look at MIT license attached in this repository

## Useful links

- [.NET documentation](https://learn.microsoft.com/dotnet/)
- [ASP.NET Core Web API](https://learn.microsoft.com/aspnet/core/web-api/)
- [Polly](https://www.pollydocs.org/)

---

Thanks for visiting my repository, your attention is my pleasure ⸜(｡˃ ᵕ ˂ )⸝♡
