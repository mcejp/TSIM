## SimServer

This server runs a simulation in real time and periodically publishes snapshots (incremental/full) of the simulation state.

The complete state is made up of:

- Network Database (describes transport infrastructure; presumed to be slow-changing)
- Unit Database (describes vehicle classes, vehicle state and their plans, changes continuously)

In Phase 1, Network Database is assumed immutable, so it only needs to be loaded once.

The databases are published via RabbitMQ in these formats:
- Unit Database Full JSON Snapshot (`UnitDatabase_full.json`)
    - example: `[{"Class":{"Name":"generic","AccelMax":0,"DecelMax":0,"VelocityMax":0},"Pos":{"X":3538.2336,"Y":1769.2385,"Z":0},"Velocity":{"X":17.202816,"Y":6.3019185,"Z":0},"Orientation":{"IsIdentity":false,"X":0,"Y":0,"Z":0.17467363,"W":0.9846265}}]`
- Control Agents Full JSON Snapshot (`TrainControl_full.json`)
    - example: `{"0":{"schedulerState":"GOTO_NEAREST_STATION","segmentsToFollow":[{"segmentId":1,"entryEp":1,"segmentLength":54.880363,"goalT":-1},{"segmentId":32,"entryEp":0,"segmentLength":42.900234,"goalT":1}]}}`
