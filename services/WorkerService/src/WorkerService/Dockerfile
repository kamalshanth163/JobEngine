FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY JobEngine.sln .
COPY shared/Contracts/JobEngine.Shared.Contracts.csproj shared/Contracts/
COPY shared/Common/JobEngine.Shared.Common.csproj shared/Common/
COPY services/WorkerService/src/WorkerService/WorkerService.csproj services/WorkerService/src/WorkerService/
RUN dotnet restore services/WorkerService/src/WorkerService/WorkerService.csproj
COPY . .
RUN dotnet publish services/WorkerService/src/WorkerService/WorkerService.csproj \
  -c Release -o /app/publish --no-restore

# dotnet/runtime not aspnet — 60MB smaller, no Kestrel/ASP.NET overhead
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WorkerService.dll"]