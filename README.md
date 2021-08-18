# TSIM ![CI status](https://travis-ci.com/mcejp/TSIM.svg?branch=develop)
Transport simulation playground

![screenshot](https://github.com/mcejp/TSIM/blob/develop/screenshots/output.svg)

## How to run (real-time mode)

0. RabbitMQ must be running: `podman build --tag tsim-rabbitmq tsim-rabbitmq && and podman run -it --rm --name rabbitmq -p 5672:5672 -p 15672:15672 -p 15674:15674 tsim-rabbitmq`
1. Open project in Rider
2. Build and execute target "TSIM". This initializes the simulation database from "data/scenario.json" and writes the
   database to "simdb.sqlite" (for the moment hardcoded)
3. Build and execute target "TSIM.WebServer". The database will be loaded and simulation can be observed in the browser.
   The trains are just programmed to try and stop at the closest station until better control architecture
   is implemented.

### Other IDEs (VS Code), command line

    mkdir -p work
    dotnet run --project TSIM -- work/simdb.sqlite \
            --importscenario maps/cern1.json \
            --simulate \
            --rendersvg work/output.svg
    dotnet run --project TSIM.SimServer
    # + in another shell:
    dotnet watch --project TSIM.WebServer run

    # alternatively, to be reachable from the internet
    dotnet run --project TSIM.WebServer --launch-profile Production --urls http://0.0.0.0:5000\;https://0.0.0.0:5001

### Visualizing data post-mortem

Use the provided Jupyter notebook to load and visualize the generated _simlog.csv_.

## How to run tests

   dotnet test TSIM.Tests

## Extracting dataset (Praha subway)

osmosis --read-pbf praha-latest.osm.pbf \
        --tf accept-ways railway=subway \
        --tf reject-relations \
        --used-node \
        --write-xml praha-subway.osm

Then import OSM into QGIS... and do what?

## Installing .NET Core

See: https://dotnet.microsoft.com/download/dotnet/5.0

You will also need host Cairo and Pango libraries (e.g. `dnf install cairo paingo` on RHEL/CentOS/Fedora)

## Editing with QGIS

TODO: add sources to repo

- edit source data
- manually merge cern1_track + cern1_stops into cern1.geojson
- re-run imporscenario
