"""Fail-closed Wave6 acceptance comparison. Model reports require an explicit test-only flag."""
from collections import defaultdict
import base64
import hashlib
from snapshot_v6 import decode, validate


def _key(command, player):
    return str(command['SessionId']), player, command['Sequence']


def _authority(round_entry, metadata, check):
    receipts, proofs = {}, defaultdict(list)
    for item in round_entry.get('authority', []):
        command, result, player = item['command'], item['result'], item['player']
        before, after = validate(decode(item['before']), metadata), validate(decode(item['after']), metadata)
        key = _key(command, player); proofs[key].append(item)
        check(before['epoch'] == command['SessionId'] and result['Sequence'] == command['Sequence'], 'authority command identity')
        if key in receipts:
            original = receipts[key]
            if command == original['command']:
                check(result == original['result'], 'identical replay did not return the exact cached ACK')
            else:
                check(result['Code'] == 2, 'changed-payload replay was not Duplicate')
            check(item['before'] == item['after'], 'replay mutated authoritative state')
            continue
        receipts[key] = item
        if result['Code']:
            check(item['before'] == item['after'], 'rejected request changed canonical state')
            continue
        kind, target, command_type = command['Kind'], command['Target'], command['Type']
        old, new = before['players'][player], after['players'][player]
        if command_type != 5:
            check(after['epoch'] == before['epoch'] and after['tick'] == before['tick'] and after['play'] == before['play'], 'command changed clocks/epoch')
        old_tower = next((t for t in before['towers'] if t['id'] == target), None)
        if command_type == 0:
            check(new['gold'] == old['gold'] - metadata['towerPrices'][kind], 'build charged incorrectly')
            added = [t for t in after['towers'] if t['id'] not in {x['id'] for x in before['towers']}]
            check(len(added) == 1 and added[0]['kind'] == kind and added[0]['owner'] == player and added[0]['site'] == -1
                  and (added[0]['x'], added[0]['y']) == (command['PositionX'], command['PositionY']), 'free build position/identity')
        elif command_type == 1:
            price = metadata['towerProfiles'][old_tower['kind']]['upgradePrices'][old_tower['level']]
            tower = next(t for t in after['towers'] if t['id'] == target)
            check(new['gold'] == old['gold'] - price and tower['invested'] == old_tower['invested'] + price, 'upgrade price/investment')
            check(tower['level'] == old_tower['level'] + 1, 'upgrade level')
            if old_tower['kind'] != 4:
                field, other = ('damage', 'range') if kind == 0 else ('range', 'damage')
                check(tower[field] == old_tower[field] + 1 and tower[other] == old_tower[other], 'upgrade path')
        elif command_type == 2:
            check(new['gold'] == min(10000, old['gold'] + old_tower['invested'] * 65 // 100), 'sale refund')
            check(not any(t['id'] == target for t in after['towers']), 'sale left tower')
        elif command_type == 3:
            wave, research = command['ExpectedWave'], command['ExpectedResearch']
            check(new['gold'] == old['gold'] - metadata['unitPrices'][kind], 'paid packet debit')
            check(len(after['orders']) == len(before['orders']) + 1 and after['orders'][:-1] == before['orders'], 'paid FIFO append')
            packet = after['orders'][-1]
            check(packet['wave'] == wave and packet['research'] == research and packet['sender'] == player and packet['kind'] == kind, 'accepted quote differs from packet')
            expected_reserved = list(old['reserved']); expected_reserved[wave - 1] += metadata['unitPrices'][kind]
            check(new['reserved'] == expected_reserved and new['spawned'] == old['spawned'] and new['attack'] == old['attack'], 'purchase activated income before spawn')
        elif command_type == 8:
            check(target == 0 and new['research'][kind] == 1 and new['gold'] == old['gold'] - metadata['researchPrices'][kind], 'research purchase')
            check(before['orders'] == after['orders'] and before['units'] == after['units'], 'research mutated existing recipe')
        elif command_type == 9:
            tower = next(t for t in after['towers'] if t['id'] == target)
            check(tower['priority'] == kind and old['gold'] == new['gold'], 'priority command')
        elif command_type == 6:
            check(new['speed'] == kind and new['gold'] == old['gold'], 'speed vote')
        elif command_type == 7:
            check(new['waveReady'] == kind and before['wave'] == target and before['wavePhase'] == 0, 'wave readiness')
    return proofs


def compare(host, client, require_complete=False, require_rapid=False, *, allow_model=False):
    problems, summaries = [], []
    model = False
    def check(condition, message):
        if not condition and message not in problems:
            problems.append(message)
    try:
        check(host.get('passed') and client.get('passed'), 'At least one player reports failure.')
        check(host.get('protocol') == client.get('protocol') == 6, 'Wire6 evidence is required; older snapshots are incompatible.')
        model = any(x.get('platform') == 'PureDotnetModel' or str(x.get('unityVersion', '')).startswith('PURE') for x in (host, client))
        check(allow_model or not model, 'Pure driver model is not an actual transport test.')
        check(host.get('role') == 'host' and client.get('role') == 'client', 'Host/client roles missing.')
        for field in ('contentHash', 'catalogHash', 'mode', 'waveCount', 'preparationTicks', 'waveBudget', 'tickRate', 'expectedStartGold', 'expectedBaseHp', 'unitPrices', 'unitCounts', 'researchPrices', 'towerProfiles'):
            check(host.get(field) == client.get(field) and host.get(field) is not None, 'Peers disagree on ' + field)
        check(len(host.get('catalogHash', '')) == len(host.get('contentHash', '')) == 64, 'Missing content/catalog hashes.')
        check((host.get('tickRate'), host.get('waveCount'), host.get('preparationTicks'), host.get('waveBudget')) == (20, 10, 200, 240), 'Production wave/control rules were changed.')
        complete = require_complete or host.get('completeMatches') or client.get('completeMatches')
        rapid = require_rapid or host.get('rapidBuy') or client.get('rapidBuy')
        check(not complete or host.get('completeMatches') and client.get('completeMatches'), 'Both peers must request complete matches.')
        check(not rapid or host.get('rapidBuy') and client.get('rapidBuy'), 'Both peers must request rapid buying.')
        expected_cycles = host.get('requestedCycles', 0)
        check(expected_cycles > 0 and client.get('requestedCycles') == expected_cycles and len(host.get('cycles', [])) == len(client.get('cycles', [])) == expected_cycles, 'Missing lifecycle cycles.')
        for h, c in zip(host.get('cycles', []), client.get('cycles', [])):
            for cycle in (h, c):
                check(cycle.get('connected') and cycle.get('cleanShutdown') and not cycle.get('failure'), 'Connection/shutdown failed: ' + cycle.get('failure', ''))
                check(cycle.get('sentBytes', 0) + cycle.get('receivedBytes', 0) > 0, 'No measured traffic.')
            check(c.get('disconnectObserved'), 'Client did not observe host shutdown.')
            check(h.get('mapIndex') == c.get('mapIndex') == host.get('requestedMapIndex') and c.get('initialMapIndex') == 0, 'Host-selected map synchronization missing.')
            check(h.get('finalContentHash') == c.get('finalContentHash') == host['contentHash'], 'Final map content mismatch.')
            if host.get('mode') == 'relay-dtls':
                check(h.get('relayAllocationEstablished') and c.get('relayAllocationEstablished'), 'No actual Relay allocation/join.')
                for key in ('authenticationProfile', 'anonymousPlayerFingerprint'):
                    check(h.get(key) and c.get(key) and h[key] != c[key], 'Relay requires distinct ' + key)
            elif host.get('mode') != 'lan':
                check(False, 'Unsupported connection mode.')
            hs = {(str(x['epoch']), x['tick'], x['hash']) for x in h.get('checkpoints', [])}
            cs = {(str(x['epoch']), x['tick'], x['hash']) for x in c.get('checkpoints', [])}
            shared = hs & cs
            check(len(shared) >= 3, 'Fewer than three identical authoritative snapshots.')
            hr, cr = h.get('rounds', []), c.get('rounds', [])
            check(len(hr) == len(cr) == (2 if complete else 1), 'Expected one scenario or two full matches with rematch.')
            for number, (a, b) in enumerate(zip(hr, cr), 1):
                prefix = 'Cycle ' + str(h.get('cycle')) + ', round ' + str(number) + ': '
                ck = lambda condition, message: check(condition, prefix + message)
                ck(a.get('epoch') == b.get('epoch') and a.get('epoch', 0) > 0, 'epoch mismatch')
                proofs = _authority(a, host, ck)
                ck(bool(proofs), 'host before/after command proof missing')
                peer_frames = []
                for player, (metadata, entry) in enumerate(((host, a), (client, b))):
                    for flag in ('freshResources', 'scenarioPassed', 'automaticPreparation', 'frozenPreparation', 'earlyStart', 'origins', 'futurePacket'):
                        ck(entry.get(flag), 'missing ' + flag)
                    ck(entry.get('number') == number and (entry.get('initialMask0'), entry.get('initialMask1')) == (63, 127), 'round/mask identity')
                    frames = {}
                    for frame in entry.get('frames', []):
                        state = validate(decode(frame['bytes']), metadata)
                        ck(state['hash'] == frame['hash'] and state['epoch'] == frame['epoch'] == entry['epoch'] and state['tick'] == frame['tick'] and state['play'] == frame['playTicks'], 'frame digest/clock identity')
                        ck(tuple(p['mask'] for p in state['players']) == (63, 127), 'asymmetric unlock masks changed')
                        frames[frame['name']] = (frame, state)
                        for tower in state['towers']:
                            samples = [x for x in frame.get('towers', []) if x['id'] == tower['id']]
                            ck(len(samples) == 1, 'computed tower stats missing')
                            if samples and tower['kind'] != 4:
                                profile = metadata['towerProfiles'][tower['kind']]
                                damage = profile['damage'] if not tower['damage'] else profile['upgradeDamage'][tower['damage'] - 1]
                                tower_range = profile['range'] if not tower['range'] else profile['upgradeRange'][tower['range'] - 1]
                                ck(samples[0]['damage'] == damage and samples[0]['range'] == tower_range, 'actual mixed-path stats mismatch')
                    peer_frames.append(frames)
                    required = ('initial', 'preparation-start', 'preparation-stable', 'preparation-end', 'automatic-start', 'build-replays', 'mixed-paths', 'sale', 'free-mine', 'speed-complete', 'wave-two-preparation', 'wave-two-frozen', 'old-recipe', 'researched-old-packet', 'new-recipe', 'rapid-after', 'single-wave-ready', 'early-start', 'mixed-origins-and-credit', 'future-before', 'future-after')
                    for name in required:
                        ck(name in frames, 'missing authoritative frame ' + name)
                    if any(name not in frames for name in required):
                        continue
                    initial = frames['initial'][1]
                    ck(initial['wave'] == 1 and initial['play'] == 0 and not initial['towers'] and not initial['units'] and not initial['orders'], 'stale initial gameplay state')
                    for p in initial['players']:
                        ck(p['gold'] == metadata['expectedStartGold'] and p['hp'] == metadata['expectedBaseHp'] and not any(p['research'] + p['reserved'] + p['spawned']) and not p['speed'] and not p['waveReady'] and p['attack'] == p['remainder'] == 0, 'resources/research/votes did not reset')
                    stable, end = frames['preparation-stable'][1], frames['preparation-end'][1]
                    ck(stable['play'] == end['play'] == 0 and stable['players'][player]['gold'] == end['players'][player]['gold'] and stable['players'][player]['remainder'] == end['players'][player]['remainder'], 'preparation generated income/gameplay')
                    start_f, start = frames['preparation-start']; active_f, active = frames['automatic-start']
                    ck(196 <= active['tick'] - start['tick'] <= 204 and 9.5 <= active_f['wall'] - start_f['wall'] <= 11, 'preparation was not ten real control seconds')
                    frozen_a, frozen_b = frames['wave-two-preparation'][1], frames['wave-two-frozen'][1]
                    ck(frozen_a['play'] == frozen_b['play'] and frozen_a['players'][player] == frozen_b['players'][player], 'wave-two preparation changed own economy/cooldowns')
                    researched = frames['researched-old-packet'][1]
                    ck(researched['players'][player]['research'][0] == 1 and any(o['sender'] == player and o['research'] == 0 for o in researched['orders']), 'research rewrote old packet')
                    new = frames['new-recipe'][1]
                    ck({o['research'] for o in new['orders'] if o['sender'] == player and o['kind'] == 0} == {0, 1}, 'new and old recipes not both retained')
                    ready = frames['single-wave-ready'][1]
                    ck(ready['wavePhase'] == 0 and ready['players'][0]['waveReady'] and not ready['players'][1]['waveReady'], 'single ready vote started wave')
                    early = frames['early-start'][1]
                    ck(early['wave'] == 2 and early['wavePhase'] == 1 and early['tick'] - frozen_a['tick'] < frozen_a['preparation'] and not any(p['waveReady'] for p in early['players']), 'mutual early start/reset missing')
                    origin = frames['mixed-origins-and-credit'][1]
                    ck(any(u['origin'] == 0 for u in origin['units']) and {u['research'] for u in origin['units'] if u['origin'] == 1 and u['defender'] != player and u['kind'] == 0} >= {0, 1} and origin['players'][player]['attack'] > 0, 'actual neutral/paid spawn or recipe credit missing')
                    future = frames['future-after'][1]
                    ck(any(o['sender'] == player and o['wave'] > future['wave'] for o in future['orders']), 'future packet not authoritatively queued')
                    for ack in entry.get('acks', []):
                        if ack.get('diagnostic'):
                            continue
                        options = proofs.get((str(ack['epoch']), player, ack['sequence']), [])
                        ck(any(x['result']['Code'] == ack['code'] and x['result']['Tick'] == ack['tick'] for x in options), 'received ACK lacks matching host execution')
                    labels = {x['label']: x for x in entry.get('acks', []) if not x.get('diagnostic')}
                    for label in ('build', 'range', 'damage', 'priority', 'sell', 'mine', 'research', 'old-recipe', 'new-recipe', 'wave-ready', 'future'):
                        ck(label in labels and labels[label]['code'] == 0, 'missing accepted ACK ' + label)
                    for label in ('stale-send', 'stale-research'):
                        ck(label in labels and labels[label]['code'] == 21, 'stale quote/research did not reject without retry')
                    if 'build' in labels:
                        replay = proofs[(str(entry['epoch']), player, labels['build']['sequence'])]
                        ck(len(replay) == 3 and [x['result']['Code'] for x in replay] == [0, 0, 2], 'coordinate original/exact/mutated replay proof missing')
                    clock_by_name = {x['name']: x for x in entry.get('clocks', [])}
                    for name, speed, votes in (('speed-one', 1, (1, 0)), ('speed-both', 2, (1, 1)), ('speed-withdrawn', 1, (0, 1))):
                        clock = clock_by_name.get(name, {})
                        ck(clock.get('complete') and clock.get('speed') == speed and abs(clock.get('controlsPerSecond', 0) - 20) <= 4 and abs(clock.get('gameplayPerSecond', 0) - 20 * speed) <= 5, 'fixed20/control and active speed window ' + name)
                        elapsed = clock.get('seconds', 0)
                        ck(elapsed >= 2 and abs((clock.get('controlEnd', 0) - clock.get('controlStart', 0)) / max(elapsed, .001) - clock.get('controlsPerSecond', 0)) < .001
                           and abs((clock.get('playEnd', 0) - clock.get('playStart', 0)) / max(elapsed, .001) - clock.get('gameplayPerSecond', 0)) < .001, 'clock rate is not supported by tick/time samples')
                        first, last = frames[name + '-start'][1], frames[name + '-end'][1]
                        ck(tuple(p['speed'] for p in first['players']) == votes, 'speed window consent ' + name)
                        before, after = first['players'][player], last['players'][player]
                        intervals = last['play'] // 20 - first['play'] // 20
                        ck(after['gold'] == before['gold'] + 5 * intervals and after['remainder'] == before['remainder'] and before['attack'] == after['attack'] == 0, 'income did not follow active time ' + name)
                    if player == 0 and 'speed-approve' in labels:
                        speed_replays = proofs[(str(entry['epoch']), player, labels['speed-approve']['sequence'])]
                        ck(len(speed_replays) >= 2 and decode(speed_replays[-1]['before'])['players'][0]['speed'] == 0, 'old approval was not replayed after withdrawal')
                    if complete and 'final' in frames:
                        final_frame, final_state = frames['final']
                        canonical = bytearray(base64.b64decode(final_frame['bytes']))
                        for owner in range(2):
                            canonical[57 + owner * (57 + 8 * metadata['waveCount']) + 9] = 0
                        ck(hashlib.sha256(canonical).hexdigest() == entry.get('resultDigest'), 'final digest is not bound to canonical encoded result')
                        ck(final_state['tick'] == entry.get('resultTick') and final_state['play'] == entry.get('playTicks') and final_state['wave'] == entry.get('resultWave'), 'final clock/wave summary is not bound to state')
                        hp0, hp1 = final_state['players'][0]['hp'], final_state['players'][1]['hp']
                        outcome = 'Draw' if hp0 == hp1 else 'Player0' if hp0 > hp1 else 'Player1'
                        ck((hp0, hp1) == (entry.get('resultHp0'), entry.get('resultHp1')) and outcome == entry.get('outcome') and final_state['phase'] == 3, 'final HP/outcome is not bound to state')
                    if rapid:
                        before, after = frames['rapid-before'][1], frames['rapid-after'][1]
                        accepted, rejected = entry.get('rapidAccepted', 0), entry.get('rapidGoldRejected', 0)
                        ck(entry.get('rapidPassed') and accepted >= 3 and rejected == 1 and accepted + rejected == entry.get('rapidRequested'), 'rapid ACK count')
                        ck(before['wavePhase'] == after['wavePhase'] == 0 and before['play'] == after['play'] and after['players'][player]['gold'] == before['players'][player]['gold'] - accepted * metadata['unitPrices'][0], 'rapid authoritative gold/state race')
                        ck(sum(o['sender'] == player for o in after['orders']) - sum(o['sender'] == player for o in before['orders']) == accepted, 'rapid paid FIFO state race')
                ck(len(peer_frames) == 2, 'missing peer frames')
                if len(peer_frames) == 2 and all('future-after' in f for f in peer_frames):
                    last = max(f['future-after'][1]['tick'] for f in peer_frames)
                    equal_after = [x for x in shared if x[0] == str(a['epoch']) and x[1] > last]
                    ck(len(equal_after) >= 3, 'fewer than three shared snapshots after completed actions')
                if complete:
                    for entry in (a, b):
                        ck(entry.get('resultObserved') and entry.get('outcome') in ('Player0', 'Player1', 'Draw'), 'no normal complete result')
                        ck(entry.get('resultHp0') == 0 or entry.get('resultHp1') == 0 or entry.get('resultWave') == host['waveCount'], 'result before base death/final wave')
                    for field in ('resultDigest', 'resultTick', 'playTicks', 'resultWave', 'resultHp0', 'resultHp1', 'outcome'):
                        ck(a.get(field) == b.get(field) and a.get(field) is not None, 'final peer mismatch ' + field)
                    ck(len(a.get('resultDigest', '')) == 64, 'final digest missing')
                    if number == 1:
                        ck(all(x.get('rematchRequested') and x.get('rematchAccepted') for x in (a, b)), 'mutual rematch missing')
                    else:
                        ck(a['epoch'] != hr[0]['epoch'] and b['epoch'] != cr[0]['epoch'], 'rematch reused epoch')
                summaries.append({'cycle': h['cycle'], 'round': number, 'epoch': a['epoch'], 'shared_snapshots': sum(x[0] == str(a['epoch']) for x in shared), 'host_authority_commands': len(a.get('authority', [])), 'result': a.get('outcome'), 'result_wave': a.get('resultWave')})
    except (ValueError, KeyError, TypeError, IndexError, StopIteration) as error:
        check(False, 'Malformed/incomplete Wave6 evidence: ' + str(error))
    return {'passed': not problems, 'problems': problems, 'protocol': 6, 'rounds': summaries,
            'scope': 'Pure driver-state-machine model only; NO actual transport or cloud assertion.' if allow_model and model else 'Two actual standalone processes via ' + str(host.get('mode')) + '; separate internet access paths, human play and Android performance are not asserted.'}
