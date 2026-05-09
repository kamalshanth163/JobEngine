FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY JobEngine.sln .
COPY shared/Contracts/JobEngine.Shared.Contracts.csproj shared/Contracts/
COPY shared/Common/JobEngine.Shared.Common.csproj shared/Common/
COPY services/NotificationService/src/NotificationService/NotificationService.csproj services/NotificationService/src/NotificationService/
RUN dotnet restore services/NotificationService/src/NotificationService/NotificationService.csproj
COPY . .
RUN dotnet publish services/NotificationService/src/NotificationService/NotificationService.csproj \
  -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]