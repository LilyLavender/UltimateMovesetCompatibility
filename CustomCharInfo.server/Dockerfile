FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore ./CustomCharInfo.server.csproj

COPY . .
RUN dotnet publish ./CustomCharInfo.server.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080

# Render sets $PORT at runtime; default to 8080 for local `docker run`.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet CustomCharInfo.server.dll"]
