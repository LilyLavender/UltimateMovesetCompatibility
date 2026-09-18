# Ultimate Moveset Compatibility
Ultimate Moveset Compatibility, or UMC, is a platform for moveset creators to showcase their projects and ensure compatibility between movesets.

## Tech stack
- Frontend: Vue 3, Vite, Vuetify 3, Pinia, vue-router (hash mode), hosted on GitHub Pages
- Backend: ASP.NET Core 9, EF Core 9, ASP.NET Identity, hosted on Render
- Database: Neon Postgres
- Image host: Cloudflare R2

## Public API
Read-only endpoints for movesets, series, hooks, compatibility, and plugin lookup are open to anyone. See the [API page](https://lilylavender.github.io/UltimateMovesetCompatibility/#/api) for the Swagger docs.

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

`Git push` to `main`. Render deploys automatically.
