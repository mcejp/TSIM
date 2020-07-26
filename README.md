# TSIM ![CI status](https://travis-ci.com/mcejp/TSIM.svg?branch=develop)
Transport simulation playground

![screenshot](https://github.com/mcejp/TSIM/blob/develop/screenshots/output.svg)

## How to run (real-time mode)

1. Open project in Rider
2. Build and execute target "TSIM". This initializes the simulation database from "data/scenario.json" and writes the
   database to "simdb.sqlite" (for the moment hardcoded)
3. Build and execute target "TSIM.WebServer". The database will be loaded and simulation can be observed in the browser.
   The trains are just programmed to try and stop at the closest station until better control architecture
   is implemented.

### Other IDEs (VS Code), command line

    mkdir -p work
    dotnet run --project TSIM/TSIM.csproj -- work/simdb.sqlite \
            --importscenario maps/cern1.json \
            --simulate \
            --rendersvg work/output.svg
    dotnet run --project TSIM.WebServer/TSIM.WebServer.csproj

    # alternatively, to be reachable from the internet
    dotnet run --project TSIM.WebServer/TSIM.WebServer.csproj --launch-profile Production --urls http://0.0.0.0:5000\;https://0.0.0.0:5001

### Visualizing data post-mortem

Use the provided Jupyter notebook to load and visualize the generated _simlog.csv_.

## How to run tests

   dotnet test TSIM.Tests/TSIM.Tests.csproj

## Extracting dataset (Praha subway)

osmosis --read-pbf praha-latest.osm.pbf \
        --tf accept-ways railway=subway \
        --tf reject-relations \
        --used-node \
        --write-xml praha-subway.osm

Then import OSM into QGIS... and do what?

## Installing .NET Core

- CentOS 8: `sudo dnf install dotnet-sdk-3.1` per [this link](https://docs.microsoft.com/en-us/dotnet/core/install/linux-centos)
- Other: [see here](https://dotnet.microsoft.com/download/dotnet-core/3.1)
