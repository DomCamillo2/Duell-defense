using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text.Json;
using DefenseDuell.Core;
using DefenseDuell.Networking;

// Produces codec evidence only. No Unity scene, transport, socket, Relay or billing.
internal static class Golden
{
    private static int Main(string[] args)
    {
        if (args.Length != 1 || File.Exists(args[0]))
            throw new ArgumentException("Pass a NEW output JSON path; frozen evidence is never overwritten.");
        var catalog = MapCatalog.CreateContents(GameContent.CreateDefault());
        var simulation = new MatchSimulation(catalog[1]);
        var command = new CommandEnvelope {
            SessionId = 1, Sequence = 1, Type = CommandType.ApplyMatchRules, Target = -1,
            RuleWaveCount = 3, RuleStartGold = BigInteger.Pow(10, 40) + 123
        };
        var result = simulation.Execute(PlayerSlot.Zero, command);
        if (!result.Accepted) throw new InvalidOperationException("Golden Apply failed: " + result.Code);
        string catalogHash = WireProtocol.CatalogHash(catalog);
        var evidence = new {
            protocol = (int)WireProtocol.Version,
            baseHash = catalog[1].BaseContentHash,
            effectiveHash = simulation.Content.ContentHash,
            catalogHash,
            waveCount = 3,
            startGold = command.RuleStartGold.ToString(CultureInfo.InvariantCulture),
            snapshot = Convert.ToBase64String(WireProtocol.EncodeSnapshot(simulation.State)),
            welcome = Convert.ToBase64String(WireProtocol.EncodeWelcome(catalogHash, 1, 9, simulation.State, simulation.Content, 17)),
            command = Convert.ToBase64String(WireProtocol.EncodeCommand(command)),
            ack = Convert.ToBase64String(WireProtocol.EncodeResult(1, result, 17))
        };
        File.WriteAllText(args[0], JsonSerializer.Serialize(evidence));
        return 0;
    }
}
