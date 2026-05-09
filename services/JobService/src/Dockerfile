FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY JobEngine.sln .
COPY shared/Contracts/JobEngine.Shared.Contracts.csproj shared/Contracts/
COPY shared/Common/JobEngine.Shared.Common.csproj shared/Common/
COPY services/JobService/src/JobService.Domain/JobService.Domain.csproj services/JobService/src/JobService.Domain/
COPY services/JobService/src/JobService.Application/JobService.Application.csproj services/JobService/src/JobService.Application/
COPY services/JobService/src/JobService.Infrastructure/JobService.Infrastructure.csproj services/JobService/src/JobService.Infrastructure/
COPY services/JobService/src/JobService.Api/JobService.Api.csproj services/JobService/src/JobService.Api/
RUN dotnet restore services/JobService/src/JobService.Api/JobService.Api.csproj
COPY . .
RUN dotnet publish services/JobService/src/JobService.Api/JobService.Api.csproj \
  -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "JobService.Api.dll"]