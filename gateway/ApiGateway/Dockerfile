FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY JobEngine.sln .
COPY gateway/ApiGateway/ApiGateway.csproj gateway/ApiGateway/
RUN dotnet restore gateway/ApiGateway/ApiGateway.csproj
COPY . .
RUN dotnet publish gateway/ApiGateway/ApiGateway.csproj \
  -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]