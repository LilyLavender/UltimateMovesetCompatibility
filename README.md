# Ultimate Moveset Compatibility
Ultimate Moveset Compatibility, or UMC, is a Vue3 SPA + ASP.NET Core 9 API (Azure Web App) + Azure SQL Server project. UMC was made as a platform for moveset creators to showcase their projects and ensure compatibility between movesets.

## Credits
- **Lily**: Main developer behind the frontend and backend of the site.
- **Conceptual help**: Silent, Hinata, Solar, & Okso.
- **Alpha Testers**: Hinata, Moy, Kybbler, & Okso.
- **Beta Testers**: Hudson6CO, Starz_Smash, SpiritNyx, Cattail, Silent, Eriiz, incredibleplays, superevan5, Tofu, BombasticBusiness, milktoastmonika, NanoBuds, PhazoGanon, zrksyd

## Building & publishing
### Building
Run frontend
```
npm run dev
```

Run backend
```
dotnet run
```

### Publishing
Publish frontend
```
npm run deploy
```

Publish backend
```
dotnet build
dotnet publish -c Release -o ./publish
```
Right click `server/publish` > `Deploy to Web App` > `umc-backend`.

DELETE `server/publish`
