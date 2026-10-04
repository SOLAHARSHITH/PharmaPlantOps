FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/PlantOps.Api/PlantOps.Api.csproj src/PlantOps.Api/
RUN dotnet restore src/PlantOps.Api/PlantOps.Api.csproj
COPY src/PlantOps.Api/ src/PlantOps.Api/
RUN dotnet publish src/PlantOps.Api/PlantOps.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlantOps.Api.dll"]
