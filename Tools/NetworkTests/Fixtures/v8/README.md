# Wire8 codec evidence

`golden.json` is the byte-identical copy of a fresh sample produced by the actual C# Core2.2/Wire8 encoder during 0.14 integration. It contains a three-wave lobby with exact configured gold `10000000000000000000000000000000000000123`, revision1 and wallets0, plus Welcome/Command/ACK encodings. The Python test embeds the identical snapshot bytes; neither tests nor this fixture depend on `/tmp`.

`Golden.cs` isolates the same generator from the larger temporary C# test harness. It compiles the repository's production Core and codec files directly, with no Unity or NUnit dependencies. This extracted minimal project has not been separately built; the archived bytes and the original full harness output were independently decoded and validated. The original harness was executed by the networking owner.

With .NET8 on PATH, from the repository root:

```sh
dotnet run --project Tools/NetworkTests/Fixtures/v8/Golden.csproj -- /tmp/wire8-golden-reproduced.json
cmp Tools/NetworkTests/Fixtures/v8/golden.json /tmp/wire8-golden-reproduced.json
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s Tools/NetworkTests -p 'test_comparison*.py'
```

Use a fresh output path. Different production sources may legitimately produce different bytes; do not overwrite the frozen fixture or relabel it. The minimal generator is supplied as a reproduction recipe, not as an additional executed build claim.

`python-evidence.json` records86 passing Python tests (50 historical,36 new), this cross-language sample and four newly executed production-Core/driver model comparisons. Models and codec samples do not prove sockets, Unity scenes, Relay, Android, separate internet connections or human gameplay. The comparator rejects model reports unless `allow_model=True` is explicitly requested. Actual LAN/Relay acceptance must use freshly generated standalone-process reports in a new evidence directory.

Historical Wire6/Wire7 readers, comparators and fixtures remain untouched.
