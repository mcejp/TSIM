# TSIM ![CI status](https://travis-ci.com/cejpmart/TSIM.svg?branch=develop)
Transport simulation playground

![screenshot](https://github.com/cejpmart/TSIM/blob/develop/screenshots/output.svg)

## How to run (real-time mode)

1. Open project in Rider
2. Build and execute target "TSIM". This initializes the simulation database from "data/scenario.json" and writes the
   database to "simdb.sqlite" (for the moment hardcoded)
3. Build and execute target "TSIM.WebServer". The database will be loaded and simulation can be observed in the browser.
   The trains are just programmed to try and stop at the closest station until better control architecture
   is implemented.

## How to run tests

```sh
/opt/dotnet-sdk-3.0.100-linux-x64/dotnet test TSIM.Tests/TSIM.Tests.csproj
```
