FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["backend/KitchenDeck.API/KitchenDeck.API.csproj", "backend/KitchenDeck.API/"]
RUN dotnet restore "backend/KitchenDeck.API/KitchenDeck.API.csproj"
COPY . .
WORKDIR /src/backend/KitchenDeck.API
RUN dotnet publish "KitchenDeck.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KitchenDeck.API.dll"]
