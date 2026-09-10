# Couppa source

`Couppa.Api` is the single ASP.NET Core MVC application required by `docs/SRS.md`.

It contains:

- Identity authentication/authorization with `User` and `Admin` roles.
- Public Razor pages for home, products and cart.
- Admin Razor pages for products, categories, users, dashboard and reports.
- JSON MVC actions consumed through Fetch API for interactive operations.
- EF Core persistence with SQL Server local mode and PostgreSQL Docker mode,
  guest/user cart merge and IMemoryCache.
- Admin product image upload stored under `Couppa.Api/wwwroot/uploads/products`.

The `Couppa.Api.Tests` project covers service rules and the main MVC flow. Run from this directory with:

```powershell
dotnet test
```
