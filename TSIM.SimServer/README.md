## SimServer

This server runs a simulation in real time and periodically publishes snapshots (incremental/full) of the simulation state.

The complete state is made up of:

- Network Database (describes transport infrastructure; presumed to be slow-changing)
- Unit Database (describes vehicle classes, vehicle state and their plans, changes continuously)

In Phase 1, Network Database is assumed immutable, so it only needs to be loaded once.

The databases are published via RabbitMQ in these formats:
- Unit Database Full Binary Snapshot (`UnitDatabase_full.bin`)
