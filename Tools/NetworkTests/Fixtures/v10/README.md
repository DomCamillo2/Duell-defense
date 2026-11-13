# Wire10 tooling handoff

Owned files: snapshot_v10.py, compare_duel_v10.py, compare_team_v10.py, test_wire_v10.py; Fixtures/v10 and Evidence16. Historical v9 readers/comparators/runners untouched by this owner. No Assets/Unity/Player changes.

Read API:
- snapshot_v10.decode(base64) parses exact SnapshotMagic0x41534444/version10, six research/cooldown entries, Unit.NeutralRecipeIndex Int32 after ResearchLevel. validate(state,metadata) checks canonical state, max5bits-of8 loadout, exact conservation/credit, ordinal uniqueness/kind/cursor, frozen paid research and HP. Metadata is required for exact neutral recipe/HP checks.
- Metadata rulesVersion defense-duell-core-v2.5 and wavePlanVersion authored-then-counter-profiles-boss10-v4 only. No v9/2.4/2.4.1 compatibility acceptance. neutralRecipes is the Root-approved list of rows: canonical wave/pressureBudgetHp/hpPerKind strings, profileId, plan, boss/superBoss booleans, bossIndex. Capture actual production GetWave recipes; no inferred old wave progression. Missing or duplicate rows fail closed. Plans cover only waves actually observed, not speculative full histories.
- decode_handshake(raw,hash) checks Magic0x41444444,version10, exact hash and legal1..5loadout. decode_command(base64) checks95-byte minimum, canonical BigIntegers and3batch scalars, negative/extra batch values and trailers.
- independent investment() matches current aggregate primary/secondary prices incl Laser24+10steps and mine floor-polynomial. Integer arithmetic stays exact.
- compare_duel_v10.compare(host,client,...) and compare_team_v10.compare(reports,...) retain prior report-level acceptance APIs, now strictWire10/6classes/Cadence/5loadout. Atomic batch proofs require frozen old level, cost ceiling, exact aggregate debit/investment/path increment, unchanged firing cooldown and every unrelated field/player/entity. Rejections/replays cannot mutate snapshots. Team/Endless canonical frames are decoded by commonReader; dedicated finite duel comparator explicitly rejects endless/team gameplay claims.
- Driver metadata needs six unitPrices/counts/researchPrices/cooldowns,spawnIntervalTicks2,eight fireIntervalTicks(Laser8),current neutralRecipes. Duel host/client loadout31/143. Report command dictionaries need UpgradeCount/ExpectedTowerLevel/UpgradePriceLimit canonical decimal strings even for zero. Root owns dispatcher/run scripts and production Driver DTO export.

Reproduce:
```
python3 Tools/NetworkTests/Fixtures/v10/generate.py
PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s Tools/NetworkTests -p test_wire_v10.py -v
```
Generator compiles **copied actual current Core + WireProtocol + WireSnapshotValidator** with the real C# production codec, never a copied old schema encoder. Directly executes exact newly built DLL with hash/hosttrace; verifies no sources changed during run. Creates versioned Evidence16 directories and canonical Fixtures/v10/goldens.json. Before claiming proof for a later Core/Growth update, regenerate and check binding. Initial failed generation used default10-wave basis for wave100 plan; preserved build/run evidence, fixed to actual effective content. Python initial failures (3.9 bit_count unavailable) corrected using bounded bit counting; no weakening of mask rule.

Final focused evidence:15 Python tests,65 C#generation assertions; actual2/4/6shapes,paidElites,frozenResearch,neutral/boss ordinal,atomicBatch/replay andEndless201 **codec fixture** with bounded ledger2. The latter is a constructed canonical state, not a played201-wave match. Negative production snapshots are rejected by actual C#decoder AND independentPythonvalidator.

Limits: No full positive new Wire10 host/client/team transport report has been supplied here. Comparator ports retain prior scenario requirements and must be exercised by Root's actual fresh drivers; tool tests alone do not prove networking. Reader validates metadata-bound recipe HP/shape rather than reimplementing evolving encounter growth, placement geometry or the full combat loop. Actual DLL/sourcehashes are in Evidence16. Historical v9 test results are not current evidence. No Unity/build/device/human-balance claim.
