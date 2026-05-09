FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY JobEngine.sln .
COPY shared/Contracts/JobEngine.Shared.Contracts.csproj shared/Contracts/
COPY shared/Common/JobEngine.Shared.Common.csproj shared/Common/
COPY services/ExecutionService/src/ExecutionService.Core/ExecutionService.Core.csproj services/ExecutionService/src/ExecutionService.Core/
COPY services/ExecutionService/src/ExecutionService.Api/ExecutionService.Api.csproj services/ExecutionService/src/ExecutionService.Api/
RUN dotnet restore services/ExecutionService/src/ExecutionService.Api/ExecutionService.Api.csproj
COPY . .
RUN dotnet publish services/ExecutionService/src/ExecutionService.Api/ExecutionService.Api.csproj \
  -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup
USER appuser
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ExecutionService.Api.dll"]