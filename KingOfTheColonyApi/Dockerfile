FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY nuget.config ./
COPY KingOfTheColonyApi/KingOfTheColonyApi.csproj KingOfTheColonyApi/

RUN dotnet restore KingOfTheColonyApi/KingOfTheColonyApi.csproj --configfile /src/nuget.config

COPY KingOfTheColonyApi/. KingOfTheColonyApi/

WORKDIR /src/KingOfTheColonyApi
RUN dotnet publish KingOfTheColonyApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV PORT=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "KingOfTheColonyApi.dll"]