FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Copy .csproj files FIRST for layer caching
# dotnet restore only re-runs when .csproj changes, not every code change
COPY JobEngine.sln .
COPY shared/Contracts/JobEngine.Shared.Contracts.csproj shared/Contracts/
COPY shared/Common/JobEngine.Shared.Common.csproj shared/Common/
COPY shared/Logging/JobEngine.Shared.Logging.csproj shared/Logging/
COPY services/AuthService/src/AuthService.Domain/AuthService.Domain.csproj services/AuthService/src/AuthService.Domain/
COPY services/AuthService/src/AuthService.Application/AuthService.Application.csproj services/AuthService/src/AuthService.Application/
COPY services/AuthService/src/AuthService.Infrastructure/AuthService.Infrastructure.csproj services/AuthService/src/AuthService.Infrastructure/
COPY services/AuthService/src/AuthService.Api/AuthService.Api.csproj services/AuthService/src/AuthService.Api/
RUN dotnet restore services/AuthService/src/AuthService.Api/AuthService.Api.csproj
COPY . .
RUN dotnet publish services/AuthService/src/AuthService.Api/AuthService.Api.csproj \
  -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Non-root user for security — never run as root in production
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "AuthService.Api.dll"]