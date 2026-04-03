FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["atlas test/atlas test.csproj", "atlas test/"]
RUN dotnet restore "atlas test/atlas test.csproj"

COPY . .
WORKDIR "/src/atlas test"
RUN dotnet publish "atlas test.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "atlas test.dll"]

