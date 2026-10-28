# .NET

This project uses ASP.NET Core (.NET 8) minimal APIs for the web layer and the
[Kubernetes C# client](https://github.com/kubernetes-client/csharp) to report pod status.

## Layout

| Path | Purpose |
|---|---|
| `admin/ScoreBoard/Program.cs` | Minimal API: score, metrics and status APIs, serves the dashboard |
| `admin/ScoreBoard/Game.cs` | Simulated game state — a team scores every 15 seconds or so |
| `admin/ScoreBoard/RequestMetrics.cs` | Success rate of `/api/score` over a rolling 30s window |
| `admin/ScoreBoard/PodStatus.cs` | Reads the `nba-scoreboard` Deployment and its pods with the pod's service account |
| `admin/ScoreBoard/wwwroot/index.html` | Scoreboard dashboard — polls the APIs below |
| `admin/Surge/Program.cs` | Console app that floods `/api/score` with concurrent requests |
| `admin/manifests/` | Deployment, Service, Route and interceptor Service deployed into each namespace |
| `admin/*.sh` | Instructor-only: deploy to every namespace, halftime surge |
| `manifests/` | Where you create `httpscaledobject.yaml` |

## API

| Endpoint | Purpose |
|---|---|
| `GET /api/score` | Current game score. Handles at most 12 concurrent requests per pod (0.4s each); beyond that returns `503` |
| `GET /api/metrics` | Success rate over the last 30s, as seen by the pod answering the request |
| `GET /api/status` | Replica counts and ready pod names for the `nba-scoreboard` Deployment |
| `GET /api/health` | Liveness check |

## Running the application in dev mode

```shell script
dotnet run --project admin/ScoreBoard
```

The dashboard is then served at `http://localhost:8080/`. Outside the cluster the pod
status panel is empty — `/api/status` needs in-cluster credentials.

## Producing a surge

```shell script
dotnet run --project admin/Surge -- <url> [concurrency] [duration_seconds]
dotnet run --project admin/Surge -- http://localhost:8080/api/score 20 60
```

During the workshop the instructor runs the surge from inside the cluster with
`admin/surge-all.sh`, one surge per participant scoreboard.

## Related Guides

- ASP.NET Core ([guide](https://learn.microsoft.com/aspnet/core/)): Build the application
- Kubernetes C# client ([guide](https://github.com/kubernetes-client/csharp)): Read Deployment and Pod status
- KEDA HTTP Add-on ([guide](https://github.com/kedacore/http-add-on)): Scale deployments from HTTP traffic
