# Chart.js Library Setup

This directory should contain the Chart.js library file.

## Manual Download

Since automated downloads may be restricted, please manually download Chart.js:

1. Visit: https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js
2. Save the file as `chart.umd.min.js` in this directory

## Alternative: Using LibMan

If you have network access, you can use LibMan CLI to download:

```bash
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
libman install chart.js@4.4.0 --provider unpkg --destination wwwroot/lib/chartjs --files dist/chart.umd.min.js
```

Or update the root `libman.json` and run `libman restore`.
