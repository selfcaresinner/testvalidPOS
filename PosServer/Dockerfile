FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PosServer.csproj", "."]
RUN dotnet restore "PosServer.csproj"
COPY . .
RUN dotnet build "PosServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PosServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PosServer.dll"]
