"""Fail-closed Wire10 acceptance comparison. Model reports require an explicit test-only flag."""
from collections import defaultdict
import base64
import hashlib
import math
from snapshot_v10 import decode as decode_wire10, validate as validate_wire10, validate_version
from snapshot_v10 import exact, investment, validate_unit_recipe, recipe_plan
import copy


def wave_limit(metadata):
    validate_version(metadata)
    return 200


def validate_cadence(metadata):
    validate_version(metadata)
    for key,expected in [('unitCooldownTicks',[6,8,12,10,60,90]),('unitPrices',[18,24,65,38,1200,1600]),('unitCounts',[1,1,1,4,1,1]),('researchPrices',[15,20,45,30,350,450])]:
        actual=metadata.get(key)
        if not isinstance(actual,list) or any(type(v) is not int for v in actual) or actual!=expected:raise ValueError('Wire10 metadata differs: '+key)
    if type(metadata.get('spawnIntervalTicks')) is not int or metadata['spawnIntervalTicks']!=2:raise ValueError('spawn interval')
    profiles=metadata.get('towerProfiles')
    if not isinstance(profiles,list) or len(profiles)!=8:raise ValueError('eight tower profiles required')
    for profile,expected in zip(profiles,[8,32,20,42,0,24,36,8]):
        if type(profile.get('fireIntervalTicks')) is not int or profile['fireIntervalTicks']!=expected:raise ValueError('tower cadence')


def _configured_rules(metadata):
    return (metadata['waveCount'], exact(metadata['expectedStartGold']), metadata['ruleEndless'],
            metadata['incomeMultiplier'], metadata['matchFormat'], metadata['mapId'])


def decode(encoded):
    """Project validated Wire10 finite duel ledgers for the retained scenario checks.

    No byte rewrite, version relabel or old decoder is involved. The canonical
    ledger objects remain attached and their cancelled values remain checked.
    """
    state = validate_wire10(decode_wire10(encoded))
    if state['format'] != 0 or state['endless']:
        raise ValueError('finite duel comparator cannot certify team/endless gameplay')
    for player in state['players']:
        player['_ledgers'] = {key: player[key] for key in ('reserved', 'spawned', 'cancelled')}
        for key in player['_ledgers']:
            player[key] = player[key]['values']
    return state


def validate(state, metadata):
    """Additional economy/recipe checks retained from the independent duel audit."""
    def require(condition, why):
        if not condition:
            raise ValueError(why)
    wave_limit(metadata)
    require(state['tick'] >= 0 and 0 <= state['preparation'] <= metadata['preparationTicks']
            and 0 <= state['waveStart'] <= state['play'], 'preparation/clocks')
    require(state['wavePhase'] == 0 or state['preparation'] == 0, 'active preparation')
    require((state['phase'] == 3 and state['outcome'] in (1,2,3)) or
            (state['phase'] == 4 and state['outcome'] == 4) or
            (state['phase'] < 3 and state['outcome'] == 0), 'phase/outcome')
    require(state['mapId'] == metadata['mapId'], 'host-selected map differs from encoded state')
    slots = set()
    for tower in state['towers']:
        owner, kind = tower['owner'], tower['kind']
        require((owner,tower['slot']) not in slots, 'duplicate render slot'); slots.add((owner,tower['slot']))
        require(tower['site'] == -1 and tower['priority'] in (0,1) and tower['cooldown'] >= 0, 'tower placement/priority')
        require(kind != 4 or tower['priority'] == 0, 'mine priority')
        profile = metadata['towerProfiles'][kind]
        require(tower['invested'] == profile['price'] + investment(kind,tower['level'],profile), 'exact tower investment')
    for owner, player in enumerate(state['players']):
        require(player['hp'] <= metadata['expectedBaseHp'] and player['archived'] == 0, 'base HP/finite archived credit')
        require(state['wavePhase'] == 0 or not player['waveReady'], 'stale wave consent')
        orders = [order for order in state['orders'] if order['sender'] == owner]
        previous = state['wave']
        for index, order in enumerate(orders):
            kind = order['kind']
            require(previous <= order['wave'] <= state['ruleWaves'], 'FIFO wave order')
            require(order['price'] == metadata['unitPrices'][kind] and order['initial'] == metadata['unitCounts'][kind], 'packet price/count')
            require(0 <= order['research'] <= player['research'][kind] and order['nextTick'] >= 0, 'frozen packet research/departure')
            require(order['remaining'] == order['initial'] or index == 0 and order['wave'] == state['wave'], 'partial FIFO head')
            previous = order['wave']
        for index, credit in enumerate(player['spawned']):
            wave = player['_ledgers']['spawned']['first'] + index
            require(wave <= state['wave'] or credit == 0, 'future spawn credit')
    for unit in state['units']:
        require(unit['distance'] >= 0 and 600 <= unit['slow'] <= 1000 and unit['slowUntil'] >= 0 and unit['remainder'] >= 0, 'unit motion/slow')
        require(unit['origin'] == 0 or 0 <= unit['research'] <= state['players'][unit['sender']]['research'][unit['kind']], 'unit frozen recipe')
    for unit in state['units']:validate_unit_recipe(unit,state,metadata)
    require(all(f['cursor']<=len(recipe_plan(metadata,state['wave'])) for f in state['fields']),'neutral plan cursor')
    require(state['wavePhase'] != 0 or not state['units'], 'preparation live units')
    return state


def _normalize_report(report):
    """Only parse new BigInteger report scalars; reject floats and legacy JSON ints."""
    report = copy.deepcopy(report)
    wave_limit(report)
    if type(report.get('ruleEndless')) is not bool or report['ruleEndless'] or type(report.get('matchFormat')) is not int or report['matchFormat'] != 0:
        raise ValueError('finite duel report required; team/endless evidence has a separate comparator')
    if type(report.get('incomeMultiplier')) is not int or report['incomeMultiplier'] not in (1,2,3,5,10):
        raise ValueError('invalid income multiplier')
    def scalar(value):
        if not isinstance(value,str):
            raise ValueError('Wire10 BigInteger wave must be a canonical decimal string')
        return exact(value)
    for cycle in report.get('cycles',[]):
        for entry in cycle.get('rounds',[]):
            if entry.get('resultWave') in (None,'') and entry.get('resultObserved') is False:
                entry['resultWave'] = None  # JsonUtility may serialize a null optional string as empty.
            elif entry.get('resultWave') is not None:
                entry['resultWave'] = scalar(entry['resultWave'])
            for frame in entry.get('frames',[]):
                frame['wave'] = scalar(frame['wave'])
            for proof in entry.get('authority',[]):
                command = proof['command']
                command['ExpectedWave'] = scalar(command['ExpectedWave'])
                for key in ('ExpectedResearch','ExpectedConfigRevision','ExpectedRosterRevision','RuleStartGold','UpgradeCount','ExpectedTowerLevel','UpgradePriceLimit'):
                    if not isinstance(command[key],str):
                        raise ValueError('canonical command scalar string required: '+key)
                    exact(command[key],unsigned=key!='RuleStartGold')
                for key in ('SessionId','Sequence'):
                    exact(command[key],bits=64)
                if type(command['RuleEndless']) is not bool:
                    raise ValueError('canonical rule boolean required')
    return report


def compare(host, client, *args, **kwargs):
    try:
        host, client = _normalize_report(host), _normalize_report(client)
    except (ValueError,KeyError,TypeError,AttributeError) as error:
        return {'passed':False,'protocol':10,'problems':['Malformed/incompatible Wire10 duel evidence: '+str(error)],
                'rounds':[], 'startup_only_proven':False,'last_wave_proven':False}
    return _compare(host,client,*args,**kwargs)


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
        if command_type!=14:check(all(exact(command[k])==0 for k in ('UpgradeCount','ExpectedTowerLevel','UpgradePriceLimit')),'non-batch command carried batch scalars')
        check(command_type in range(15), 'unexpected command outside the duel scenario')
        check(command_type == 3 or command['EnemyTarget'] == 0, 'non-send request carried enemy target')
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
            profile = metadata['towerProfiles'][old_tower['kind']]
            price = investment(old_tower['kind'], old_tower['level']+1, profile)-investment(old_tower['kind'], old_tower['level'], profile)
            tower = next(t for t in after['towers'] if t['id'] == target)
            check(new['gold'] == old['gold'] - price and tower['invested'] == old_tower['invested'] + price, 'upgrade price/investment')
            check(tower['level'] == old_tower['level'] + 1, 'upgrade level')
            if old_tower['kind'] != 4:
                secondary = 9 if old_tower['kind'] == 1 else 10 if old_tower['kind'] == 5 else 1
                primary = [3,4,5,6,2,7,8,0][old_tower['kind']]
                check(kind in (primary, secondary) or kind == 0 and old_tower['kind'] != 2, 'class-specific upgrade path')
                field, other = ('range', 'damage') if kind == secondary else ('damage', 'range')
                check(tower[field] == old_tower[field] + 1 and tower[other] == old_tower[other], 'upgrade path')
        elif command_type == 14:
            audit_batch(command, before, after, player, metadata, check)
        elif command_type == 2:
            check(new['gold'] == old['gold'] + old_tower['invested'] * metadata.get('refundPercents', [65] * len(metadata['towerProfiles']))[old_tower['kind']] // 100, 'sale refund')
            check(not any(t['id'] == target for t in after['towers']), 'sale left tower')
        elif command_type == 3:
            wave, research = command['ExpectedWave'], exact(command['ExpectedResearch'])
            check(new['gold'] == old['gold'] - metadata['unitPrices'][kind], 'paid packet debit')
            check(len(after['orders']) == len(before['orders']) + 1 and after['orders'][:-1] == before['orders'], 'paid FIFO append')
            packet = after['orders'][-1]
            check(packet['wave'] == wave and packet['research'] == research and packet['sender'] == player and packet['kind'] == kind and packet['target'] == command['EnemyTarget'], 'accepted quote differs from packet')
            expected_reserved = list(old['reserved']); expected_reserved[wave - old['_ledgers']['reserved']['first']] += metadata['unitPrices'][kind]
            check(new['reserved'] == expected_reserved and new['spawned'] == old['spawned'] and new['cancelled'] == old['cancelled'] and new['attack'] == old['attack'], 'purchase activated income before spawn')
        elif command_type == 8:
            check(target == -1 and exact(command['ExpectedResearch']) == 0 and new['research'][kind] == 1 and new['gold'] == old['gold'] - metadata['researchPrices'][kind], 'research purchase')
            check(before['orders'] == after['orders'] and before['units'] == after['units'], 'research mutated existing recipe')
        elif command_type == 9:
            tower = next(t for t in after['towers'] if t['id'] == target)
            check(tower['priority'] == kind and old['gold'] == new['gold'], 'priority command')
        elif command_type == 6:
            check(new['speed'] == kind and new['gold'] == old['gold'], 'speed vote')
        elif command_type == 7:
            check(new['waveReady'] == kind and target == -1 and before['wave'] == command['ExpectedWave'] and before['wavePhase'] == 0, 'wave readiness')
    return proofs


def _compare_default(host, client, require_complete=False, require_rapid=False, *, allow_model=False, require_economy11=False):
    problems, summaries = [], []
    model = False
    def check(condition, message):
        if not condition and message not in problems:
            problems.append(message)
    try:
        check(host.get('passed') and client.get('passed'), 'At least one player reports failure.')
        check(host.get('protocol') == client.get('protocol') == 10, 'Wire10 evidence is required; older snapshots are incompatible.')
        economy11 = require_economy11 or host.get('acceptanceSchema') == 2 or client.get('acceptanceSchema') == 2
        if economy11:
            for report in (host, client):
                check(report.get('acceptanceSchema') == 3 and wave_limit(report) in (10,200), 'Versioned economy acceptance metadata required.')
                check(report.get('refundPercents') == [65,65,65,65,40,65,65,65], 'Production class refund percentages differ.')
                validate_cadence(report)
                validate_windows(report)
                check(report.get('towerPrices', [])[4:5] == [160], 'Production mine must cost160G.')
                check(report.get('passiveGoldPerSecond') == 4 and report.get('incomeUnitsPerGold') == 960, 'Production income denomination differs.')
            for field in ('acceptanceSchema','rulesVersion','refundPercents','unitCooldownTicks','sendWindowTicksByWave','passiveGoldPerSecond','incomeUnitsPerGold'):
                check(host.get(field) == client.get(field), 'Peers disagree on ' + field)
        model = any(x.get('platform') == 'PureDotnetModel' or str(x.get('unityVersion', '')).startswith('PURE') for x in (host, client))
        check(allow_model or not model, 'Pure driver model is not an actual transport test.')
        check(host.get('role') == 'host' and client.get('role') == 'client', 'Host/client roles missing.')
        for field in ('contentHash', 'catalogHash', 'mode', 'waveCount', 'preparationTicks', 'waveBudget', 'tickRate', 'expectedStartGold', 'expectedBaseHp', 'unitPrices', 'unitCounts', 'researchPrices', 'towerProfiles'):
            check(host.get(field) == client.get(field) and host.get(field) is not None, 'Peers disagree on ' + field)
        check(len(host.get('catalogHash', '')) == len(host.get('contentHash', '')) == 64, 'Missing content/catalog hashes.')
        check((host.get('tickRate'), host.get('waveCount'), host.get('preparationTicks'), host.get('waveBudget')) == (20, 10, 200, host.get('waveBudget')), 'Production wave/control rules were changed.')
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
                    ck(entry.get('number') == number and (entry.get('initialMask0'), entry.get('initialMask1')) == (31, 151), 'round/mask identity')
                    frames = {}
                    for frame in entry.get('frames', []):
                        state = validate(decode(frame['bytes']), metadata)
                        ck(state['hash'] == frame['hash'] and state['epoch'] == frame['epoch'] == entry['epoch'] and state['tick'] == frame['tick'] and state['play'] == frame['playTicks'] and state['wave'] == frame['wave'], 'frame digest/clock identity')
                        ck(tuple(p['mask'] for p in state['players']) == (31, 151), 'asymmetric unlock masks changed')
                        frames[frame['name']] = (frame, state)
                        for tower in state['towers']:
                            samples = [x for x in frame.get('towers', []) if x['id'] == tower['id']]
                            ck(len(samples) == 1, 'computed tower stats missing')
                            if samples and tower['kind'] != 4:
                                profile = metadata['towerProfiles'][tower['kind']]
                                damage = profile['damage'] if not tower['damage'] else profile['upgradeDamage'][tower['damage'] - 1]
                                tower_range = profile['range'] if not tower['range'] else profile['upgradeRange'][tower['range'] - 1]
                                ck(int(samples[0]['damage']) == damage and samples[0]['range'] == tower_range, 'actual mixed-path stats mismatch')
                    peer_frames.append(frames)
                    required = ('initial', 'preparation-start', 'preparation-stable', 'preparation-end', 'automatic-start', 'build-replays', 'mixed-paths', 'sale', 'free-mine', 'speed-complete', 'wave-two-preparation', 'wave-two-frozen', 'old-recipe', 'researched-old-packet', 'new-recipe', 'rapid-after', 'single-wave-ready', 'early-start', 'mixed-origins-and-credit', 'future-before', 'future-after')
                    if economy11:
                        required += ('mine-sale-before','mine-sale-after','mine-rebuild-after','mine-sale-replay')
                        ck(entry.get('mineRebuilt'), 'mine sale/rebuild scenario incomplete')
                    for name in required:
                        ck(name in frames, 'missing authoritative frame ' + name)
                    if any(name not in frames for name in required):
                        continue
                    initial = frames['initial'][1]
                    ck(initial['wave'] == 1 and initial['play'] == 0 and not initial['towers'] and not initial['units'] and not initial['orders'], 'stale initial gameplay state')
                    for p in initial['players']:
                        ck(p['gold'] == 0 and not initial['startGoldGranted'] and p['hp'] == metadata['expectedBaseHp'] and not any(p['research'] + p['reserved'] + p['spawned'] + p['cancelled']) and not p['speed'] and not p['waveReady'] and p['attack'] == p['remainder'] == 0, 'resources/research/votes did not reset')
                    stable_f, stable = frames['preparation-stable']; end_f, end = frames['preparation-end']
                    start_f, start = frames['preparation-start']; active_f, active = frames['automatic-start']
                    ck(end['wave'] == stable['wave'] == start['wave'] == active['wave'] == 1
                       and end['wavePhase'] == 0 and end['preparation'] > 0, 'preparation-end must be an observed preparing state')
                    ck(stable['tick'] < end['tick'] < active['tick']
                       and stable_f['wall'] <= end_f['wall'] <= active_f['wall'], 'preparation-end observation order')
                    ck(end['tick'] + end['preparation'] == start['tick'] + start['preparation'], 'preparation-end countdown deadline')
                    ck(stable['play'] == end['play'] == 0 and stable['players'][player]['gold'] == end['players'][player]['gold'] and stable['players'][player]['remainder'] == end['players'][player]['remainder'], 'preparation generated income/gameplay')
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
                    if economy11:
                        future_before = frames['future-before'][1]
                        ck(future_before['wavePhase'] == 2 and future_before['play'] - future_before['waveStart'] >= metadata['sendWindowTicksByWave'][future_before['wave']-1], 'future quote preceded real send-window end')
                        old_mine = next(t for t in frames['mine-sale-before'][1]['towers'] if t['owner'] == player and t['kind'] == 4)
                        rebuilt = next(t for t in frames['mine-rebuild-after'][1]['towers'] if t['owner'] == player and t['kind'] == 4)
                        ck(entry.get('mineSoldId') == old_mine['id'] and entry.get('mineRebuiltId') == rebuilt['id'] and rebuilt['id'] != old_mine['id'], 'mine sale/rebuild identity')
                        ck(not any(t['id'] == old_mine['id'] for t in frames['mine-sale-after'][1]['towers']), 'sold mine remains replicated')
                        ck(rebuilt['level'] == 0 and rebuilt['invested'] == metadata['towerPrices'][4] and (rebuilt['x'],rebuilt['y']) == (old_mine['x'],old_mine['y']), 'rebuilt mine position/investment')
                        ck(int(entry.get('mineRefundGold','-1')) == old_mine['invested'] * metadata['refundPercents'][4] // 100, 'mine refund summary differs from actual sold investment')
                        ck(any(t['id'] == rebuilt['id'] for t in frames['mine-sale-replay'][1]['towers']), 'old sale replay removed new mine')
                        ck(frames['mine-sale-before'][1]['tick'] >= future['tick'], 'mine recycling happened before rapid/future proof')
                    for ack in entry.get('acks', []):
                        if ack.get('diagnostic'):
                            continue
                        options = proofs.get((str(ack['epoch']), player, ack['sequence']), [])
                        ck(any(x['result']['Code'] == ack['code'] and x['result']['Tick'] == ack['tick'] for x in options), 'received ACK lacks matching host execution')
                    labels = {x['label']: x for x in entry.get('acks', []) if not x.get('diagnostic')}
                    for label in ('build', 'range', 'damage', 'priority', 'sell', 'mine', 'research', 'old-recipe', 'new-recipe', 'wave-ready', 'future'):
                        ck(label in labels and labels[label]['code'] == 0, 'missing accepted ACK ' + label)
                    if economy11:
                        for label in ('mine-sale','mine-rebuild'):
                            ck(label in labels and labels[label]['code'] == 0, 'missing accepted ACK ' + label)
                        sale_proofs = proofs[(str(entry['epoch']),player,labels['mine-sale']['sequence'])]
                        ck(len(sale_proofs) == 2 and [p['result']['Code'] for p in sale_proofs] == [0,0], 'mine sale original/replay proof missing')
                        ck(sale_proofs[0]['command']['Target'] == entry['mineSoldId'], 'mine sale proof targets another entity')
                    for label in ('stale-send', 'stale-research'):
                        ck(label in labels and labels[label]['code'] == 21, 'stale quote/research did not reject without retry')
                    if 'build' in labels:
                        replay = proofs[(str(entry['epoch']), player, labels['build']['sequence'])]
                        ck(len(replay) == 3 and [x['result']['Code'] for x in replay] == [0, 0, 2], 'coordinate original/exact/mutated replay proof missing')
                    for name, speed, votes in (('speed-one', 1, (1, 0)), ('speed-both', 2, (1, 1)), ('speed-withdrawn', 1, (0, 1))):
                        # A stale vote replica can start a second, unfinished window while awaiting
                        # the next vote snapshot. It must not overwrite an already measured window.
                        completed = [x for x in entry.get('clocks', []) if x.get('name') == name and x.get('complete')]
                        ck(bool(completed), 'missing completed speed window ' + name)
                        # Validate every completed sample, so a later good sample cannot hide a bad one.
                        for clock in completed:
                            ck(clock.get('speed') == speed and abs(clock.get('controlsPerSecond', 0) - 20) <= 4 and abs(clock.get('gameplayPerSecond', 0) - 20 * speed) <= 5, 'fixed20/control and active speed window ' + name)
                            elapsed = clock.get('seconds', 0)
                            ck(elapsed >= 2 and abs((clock.get('controlEnd', 0) - clock.get('controlStart', 0)) / max(elapsed, .001) - clock.get('controlsPerSecond', 0)) < .001
                               and abs((clock.get('playEnd', 0) - clock.get('playStart', 0)) / max(elapsed, .001) - clock.get('gameplayPerSecond', 0)) < .001, 'clock rate is not supported by tick/time samples')
                        first, last = frames[name + '-start'][1], frames[name + '-end'][1]
                        ck(tuple(p['speed'] for p in first['players']) == votes, 'speed window consent ' + name)
                        before, after = first['players'][player], last['players'][player]
                        intervals = last['play'] // 20 - first['play'] // 20
                        if economy11:
                            own_ids = {t['id'] for t in first['towers'] if t['owner'] == player}
                            mine_income = sum(int(t['incomeUnits']) for t in frames[name + '-start'][0]['towers'] if t['id'] in own_ids)
                            units = metadata['incomeUnitsPerGold']
                            income = (metadata['passiveGoldPerSecond'] * units + mine_income + before['attack']) * first['income']
                            ck(after['gold'] * units + after['remainder'] == before['gold'] * units + before['remainder'] + income * intervals and before['attack'] == after['attack'] == 0, 'income did not follow active time ' + name)
                        else:
                            ck(after['gold'] == before['gold'] + 5 * intervals and after['remainder'] == before['remainder'] and before['attack'] == after['attack'] == 0, 'income did not follow active time ' + name)
                    if player == 0 and 'speed-approve' in labels:
                        speed_replays = proofs[(str(entry['epoch']), player, labels['speed-approve']['sequence'])]
                        ck(len(speed_replays) >= 2 and decode(speed_replays[-1]['before'])['players'][0]['speed'] == 0, 'old approval was not replayed after withdrawal')
                    if complete and 'final' in frames:
                        final_frame, final_state = frames['final']
                        canonical = bytearray(base64.b64decode(final_frame['bytes']))
                        for offset in final_state['_rematchOffsets']:
                            canonical[offset] = 0
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
        check(False, 'Malformed/incomplete Wire10 evidence: ' + str(error))
    return {'passed': not problems, 'problems': problems, 'protocol': 10, 'acceptanceSchema': host.get('acceptanceSchema',1), 'rulesVersion': host.get('rulesVersion','legacy-unrecorded'), 'rounds': summaries,
            'scope': 'Pure driver-state-machine model only; NO actual transport or cloud assertion.' if allow_model and model else 'Two actual standalone processes via ' + str(host.get('mode')) + '; separate internet access paths, human play and Android performance are not asserted.'}


def _rules(state):
    return state['ruleWaves'], state['ruleStartGold'], state['endless'], state['income'], state['format'], state['mapId']


def _lobby_proofs(entry, metadata, check):
    """Bind rule changes, readiness and one-time initialization to real command snapshots."""
    found = defaultdict(list)
    seen = set()
    for proof in entry.get('authority', []):
        command, result, player = proof['command'], proof['result'], proof['player']
        before, after = validate(decode(proof['before']), metadata), validate(decode(proof['after']), metadata)
        kind, code = command['Type'], result['Code']
        config = exact(command['ExpectedConfigRevision'], bits=64)
        roster = exact(command['ExpectedRosterRevision'], bits=64)
        rule_gold = exact(command['RuleStartGold'], unsigned=False)
        if kind not in (4, 10, 11, 12):
            check(config == roster == 0, 'non-lobby command carried revisions')
        if kind != 10:
            check(command['RuleWaveCount'] == rule_gold == 0 and not command['RuleEndless'] and command['RuleIncomeMultiplier'] == command['RuleFormat'] == 0 and not command['RuleMapId'], 'non-apply command carried rules')
        key = _key(command, player)
        if key in seen:
            continue  # _authority independently verifies exact/changed-payload replay receipts.
        seen.add(key)
        found[(kind, code)].append((command, before, after, player))
        if code:
            check(proof['before'] == proof['after'], 'rejected lobby request mutated state')
            if kind == 4 and code == 24:
                check(config != before['configRevision'] or roster != before['rosterRevision'], 'stale-ready evidence used current revisions')
            if kind == 10 and code == 8:
                check(player == 1, 'not-owner apply proof is not a guest request')
            continue
        if kind == 10:
            check(player == 0 and before['phase'] == after['phase'] == 0, 'Apply authority/phase')
            check(config == before['configRevision'] and roster == 0, 'Apply revision binding')
            check(command['Target'] == -1 and command['Kind'] == 0, 'Apply command shape')
            desired = (command['RuleWaveCount'], rule_gold, command['RuleEndless'], command['RuleIncomeMultiplier'] or 1, command['RuleFormat'], command['RuleMapId'] or before['mapId'])
            check(_rules(after) == desired, 'Apply snapshot differs from exact requested options')
            changed = _rules(before) != desired
            check(after['configRevision'] == before['configRevision'] + int(changed), 'Apply revision/no-op')
            check(after['rosterRevision'] == before['rosterRevision'], 'Apply changed roster revision')
            if changed:
                check(not any(p['ready'] for p in after['players']), 'effective Apply did not reset all Readys')
                found['effective'].append((command, before, after, player))
                if any(p['ready'] for p in before['players']):
                    found['ready-reset'].append((command, before, after, player))
            else:
                check(proof['before'] == proof['after'], 'no-op Apply changed canonical state/readiness')
                found['noop'].append((command, before, after, player))
            check(not after['startGoldGranted'] and all(p['gold'] == 0 for p in after['players']), 'Apply paid starting gold')
        elif kind == 4:
            check(config == before['configRevision'] and roster == before['rosterRevision'], 'Ready revision binding')
            check(before['phase'] == 0 and not before['startGoldGranted'], 'Ready started from non-lobby state')
            check(command['Target'] == -1 and command['Kind'] in (0, 1), 'Ready command shape')
            check(after['players'][player]['ready'] == (command['Kind'] == 0), 'Ready state differs from request')
            check(_rules(after) == _rules(before) and after['configRevision'] == config and after['rosterRevision'] == roster, 'Ready changed rules/revisions')
            if after['startGoldGranted']:
                check(before['connectedMask'] == 3 and all(p['ready'] for p in after['players']), 'start without mutual current readiness')
                check(after['phase'] in (1, 2) and all(p['gold'] == before['ruleStartGold'] for p in after['players']), 'atomic start did not grant exact equal gold')
                found['start'].append((command, before, after, player))
            else:
                check(after['phase'] == 0 and all(p['gold'] == 0 for p in after['players']), 'single Ready granted resources')
        elif kind == 5:
            check(before['phase'] == 3, 'rematch before terminal result')
            if before['epoch'] != after['epoch']:
                check(after['epoch'] == before['epoch'] + 1, 'rematch epoch increment/wrap')
                check(_rules(before) == _rules(after), 'rematch changed confirmed options')
                check(after['phase'] == 0 and after['configRevision'] == after['rosterRevision'] == 0, 'rematch did not reset lobby revisions')
                check(not after['startGoldGranted'] and all(not p['ready'] and not p['rematch'] and p['gold'] == 0 for p in after['players']), 'rematch reused readiness or granted gold')
                found['rematch'].append((command, before, after, player))
    return found


def _publications(host_cycle, client_cycle, check):
    known = {}
    shared = []
    for role, cycle in (('host', host_cycle), ('client', client_cycle)):
        latest = {}
        last_epoch = None
        for item in cycle.get('checkpoints', []):
            epoch = exact(item['epoch'], bits=64)
            seq = exact(item['publicationSequence'], bits=64)
            check(epoch > 0 and seq > 0, 'zero publication identity')
            if last_epoch is not None and epoch != last_epoch:
                check(epoch == last_epoch + 1, role + ' applied an old/unexpected match epoch')
            last_epoch = epoch
            key = epoch, seq
            value = item['tick'], item['hash']
            check(len(item['hash']) == 64, 'publication digest missing')
            if epoch in latest:
                check(seq >= latest[epoch], role + ' applied an older publication')
            latest[epoch] = seq
            if key in known:
                check(known[key][0] == value, 'same publication has conflicting canonical state')
                if known[key][1] != role:
                    shared.append((epoch, seq, value[0], value[1]))
            else:
                known[key] = value, role
    check(len(set(shared)) >= 3, 'fewer than three shared Wire10 publication identities')
    return set(shared)


def _decode_frames(entry, metadata, check):
    frames = {}
    for ack in entry.get('acks', []):
        check(exact(ack['publicationSequence'], bits=64) > 0, 'ACK publication identity missing')
    last_seq, last_tick, last_phase = -1, -1, 0
    prior_state = None
    for frame in entry.get('frames', []):
        state = validate(decode(frame['bytes']), metadata)
        seq = exact(frame['publicationSequence'], bits=64)
        check(frame['name'] not in frames, 'duplicate named frame')
        check(state['hash'] == frame['hash'] and state['epoch'] == exact(frame['epoch']) == exact(entry['epoch'])
              and state['tick'] == frame['tick'] and state['play'] == frame['playTicks'] and state['wave'] == frame['wave'], 'frame digest/clock identity')
        check(seq >= last_seq and state['tick'] >= last_tick, 'frame publication/control regression')
        check(not (last_phase > 0 and state['phase'] == 0), 'started epoch regressed to lobby')
        if prior_state is not None:
            check(state['configRevision'] >= prior_state['configRevision'] and state['rosterRevision'] >= prior_state['rosterRevision'], 'frame lobby revision regression')
            if prior_state['phase'] > 0:
                check(_rules(state) == _rules(prior_state) and state['configRevision'] == prior_state['configRevision'] and state['rosterRevision'] == prior_state['rosterRevision'], 'rules/revisions changed after start')
        prior_state = state
        last_seq, last_tick, last_phase = seq, state['tick'], state['phase']
        frames[frame['name']] = frame, state
    return frames


def _final(entry, frames, metadata, check):
    check('final' in frames, 'missing canonical final frame')
    if 'final' not in frames:
        return False
    frame, state = frames['final']
    canonical = bytearray(base64.b64decode(frame['bytes']))
    for offset in state['_rematchOffsets']:
        canonical[offset] = 0
    check(hashlib.sha256(canonical).hexdigest() == entry.get('resultDigest'), 'final result digest unbound')
    hp = tuple(p['hp'] for p in state['players'])
    outcome = 'Draw' if hp[0] == hp[1] else 'Player0' if hp[0] > hp[1] else 'Player1'
    check(state['phase'] == 3 and state['outcome'] == {'Player0':1,'Player1':2,'Draw':3}[outcome], 'terminal outcome differs from HP')
    check(entry.get('resultObserved') and entry.get('outcome') == outcome
          and hp == (entry.get('resultHp0'),entry.get('resultHp1')), 'terminal summary differs from state')
    check((state['tick'], state['play'], state['wave']) == (entry.get('resultTick'),entry.get('playTicks'),entry.get('resultWave')), 'terminal clocks/wave differ')
    reached = state['wave'] == metadata['waveCount']
    check(reached or min(hp) == 0, 'result before configured final wave without base death')
    return reached



def _started_smoke(entry, frames, metadata, check):
    check(entry.get('startOnlyObserved') is True, 'Startup smoke was not observed.')
    check(entry.get('resultObserved') is False and not entry.get('rematchRequested') and not entry.get('rematchAccepted') and 'final' not in frames
          and not entry.get('resultDigest') and not entry.get('resultWave') and not entry.get('outcome'),
          'Startup-only report contains final/rematch claims.')
    check('started-smoke' in frames and 'start-granted' in frames, 'Missing granted-start/smoke snapshots.')
    if 'started-smoke' not in frames or 'start-granted' not in frames:
        return
    start_frame,start=frames['start-granted']; smoke_frame,smoke=frames['started-smoke']
    elapsed=smoke_frame['wall']-start_frame['wall']
    seconds=entry.get('startedSmokeSeconds')
    check(type(seconds) in (int,float) and math.isfinite(seconds) and math.isfinite(elapsed) and seconds>=3 and elapsed>=3
          and abs(seconds-elapsed)<=.25, 'Startup smoke requires at least three measured wall seconds bound to frame times.')
    check(smoke['phase'] in (1,2) and smoke['startGoldGranted'] and smoke['outcome']==0, 'Startup smoke is not a granted live match.')
    check(_rules(smoke)==_rules(start) and _rules(smoke)==_configured_rules(metadata), 'Startup smoke changed configured rules.')
    check(not smoke['towers'] and not smoke['orders'], 'Startup smoke must not buy buildings or troops.')
    for player in smoke['players']:
        check(not any(player['research']+player['reserved']+player['spawned']+player['cancelled']) and player['attack']==0 and player['remainder']==0,
              'Startup smoke contains paid/researched gameplay.')
        expected=exact(metadata['expectedStartGold'])+metadata['passiveGoldPerSecond']*smoke['income']*(smoke['play']//metadata['tickRate'])
        check(player['gold']==expected, 'Startup smoke gold is not the single grant plus active passive income.')

def _compare(host, client, require_complete=False, require_rapid=False, *, allow_model=False,
            require_economy11=False, require_host_rules=False, expected_waves=None,
            expected_start_gold=None, require_final_wave=True, expected_start_only=None):
    """Wire10 acceptance never treats an old passing report as a new transport run."""
    problems, summaries = [], []
    def check(condition, message):
        if not condition and message not in problems:
            problems.append(message)
    start_only = bool(host.get('hostRulesStartOnly') or client.get('hostRulesStartOnly') or expected_start_only)
    dedicated = bool(host.get('hostRules') or client.get('hostRules') or require_host_rules or start_only)
    model = any(x.get('platform') == 'PureDotnetModel' or str(x.get('unityVersion','')).startswith('PURE') for x in (host,client))
    last_wave_proven = True
    try:
        if expected_start_only is not None:
            check(type(expected_start_only) is bool and start_only == expected_start_only, 'Report start-only mode differs from requested mode.')
        for report in (host,client):
            check(type(report.get('hostRulesStartOnly',False)) is bool and report.get('hostRulesStartOnly',False) == start_only, 'Peers disagree on explicit start-only mode.')
        if start_only:
            check(not require_complete and not require_rapid, 'Startup-only evidence cannot prove complete matches or rapid buying.')
            check(not host.get('completeMatches') and not client.get('completeMatches') and not host.get('rapidBuy') and not client.get('rapidBuy'), 'Startup-only report claims incompatible gameplay coverage.')
        check(host.get('passed') is True and client.get('passed') is True, 'A peer reported failure.')
        check(type(host.get('protocol')) is int and type(client.get('protocol')) is int and host['protocol'] == client['protocol'] == 10, 'Actual Wire10 reports required.')
        check(not model or allow_model, 'Pure driver model is not an actual transport test.')
        check(host.get('role') == 'host' and client.get('role') == 'client', 'Host/client roles missing.')
        for field in ('mode','rulesVersion','catalogHash','baseContentHash','contentHash','wavePlanVersion','waveCount','expectedStartGold',
                      'expectedBaseHp','tickRate','preparationTicks','ruleEndless','incomeMultiplier','matchFormat','mapId','towerPrices','towerProfiles','unitPrices','unitCounts',
                      'refundPercents','unitCooldownTicks','sendWindowTicksByWave','passiveGoldPerSecond','incomeUnitsPerGold'):
            check(host.get(field) is not None and host.get(field) == client.get(field), 'Peers disagree on '+field)
        check(host.get('spawnIntervalTicks') == client.get('spawnIntervalTicks'), 'Peers disagree on spawnIntervalTicks')
        for report in (host,client):
            check(report.get('acceptanceSchema') == 3, 'Wire10 acceptance schema required.')
            maximum_waves = wave_limit(report)
            check(type(report.get('waveCount')) is int and 1 <= report['waveCount'] <= maximum_waves, 'Unsupported configured wave count.')
            check(isinstance(report.get('expectedStartGold'), str), 'Starting gold must remain an exact decimal string.')
            gold = exact(report['expectedStartGold'], bits=65523*8)
            check((report.get('tickRate'),report.get('preparationTicks')) == (20,200), 'Control/preparation rules differ.')
            check(report.get('refundPercents') == [65,65,65,65,40,65,65,65], 'Class refund rules differ.')
            validate_cadence(report)
            validate_windows(report)
            check(report.get('towerPrices',[0]*5)[4] == 160 and report.get('passiveGoldPerSecond') == 4 and report.get('incomeUnitsPerGold') == 960, 'Production economy differs.')
            for field in ('catalogHash','baseContentHash','contentHash'):
                value=report.get(field,'')
                check(len(value)==64 and all(c in '0123456789abcdef' for c in value), 'Missing canonical '+field)
            if expected_waves is not None:
                check(report['waveCount'] == expected_waves, 'Report does not prove requested wave count.')
            if expected_start_gold is not None:
                check(gold == exact(expected_start_gold), 'Report does not prove requested exact starting gold.')
        if not dedicated:
            base = _compare_default(host,client,require_complete,require_rapid,allow_model=allow_model,require_economy11=True)
            for problem in base['problems']:
                check(False,problem)
            summaries = base['rounds']
        else:
            check(host.get('hostRules') is True and client.get('hostRules') is True, 'Both peers must run configured host-rule scenario.')
            for report in (host,client):
                check(report.get('configuredWaves') == report['waveCount'] and exact(report['configuredStartGold']) == exact(report['expectedStartGold']), 'Configured CLI options differ from effective report rules.')
            check(not require_rapid, 'Dedicated host-rule scenario does not prove rapid buying; run default scenario separately.')
        cycles = host.get('requestedCycles',0)
        check(type(cycles) is int and cycles > 0 and client.get('requestedCycles') == cycles
              and len(host.get('cycles',[])) == len(client.get('cycles',[])) == cycles, 'Missing requested transport lifecycles.')
        for h,c in zip(host.get('cycles',[]),client.get('cycles',[])):
            for cycle in (h,c):
                check(cycle.get('connected') and cycle.get('cleanShutdown') and not cycle.get('failure'), 'Connection/shutdown failed.')
                check(cycle.get('sentBytes',0)+cycle.get('receivedBytes',0)>0, 'No measured transport traffic.')
                check(cycle.get('finalContentHash') == host['contentHash'], 'Final effective content hash mismatch.')
            check(c.get('disconnectObserved'), 'Client did not observe host shutdown.')
            check(h.get('mapIndex') == c.get('mapIndex') == host.get('requestedMapIndex') and c.get('initialMapIndex') == 0, 'Host map handshake evidence missing.')
            if host['mode']=='relay-dtls':
                check(h.get('relayAllocationEstablished') and c.get('relayAllocationEstablished'), 'Missing actual Relay allocation/join.')
                for field in ('authenticationProfile','anonymousPlayerFingerprint'):
                    check(h.get(field) and c.get(field) and h[field]!=c[field], 'Relay peers need distinct '+field)
            else:
                check(host['mode']=='lan', 'Unsupported transport mode.')
            shared = _publications(h,c,check)
            hr,cr=h.get('rounds',[]),c.get('rounds',[])
            check(len(hr)==len(cr) and (not dedicated or len(hr)==(1 if start_only else 2)), 'Configured startup needs one round; full scenario needs two rounds and rematch.')
            for number,(a,b) in enumerate(zip(hr,cr),1):
                prefix='Cycle '+str(h.get('cycle'))+', round '+str(number)+': '
                ck=lambda condition,message:check(condition,prefix+message)
                ck(exact(a['epoch'])==exact(b['epoch']) and a.get('number')==b.get('number')==number,'Round identity mismatch.')
                proofs = _authority(a,host,ck) if dedicated else None
                lobby = _lobby_proofs(a,host,ck)
                ck(len(lobby['start'])==1,'Exactly one atomic gold-grant transition per epoch required.')
                if number==1 and len(hr)==2:
                    ck(len(lobby['rematch'])==1,'Exactly one authoritative rematch transition required.')
                frame_sets=[]
                for player,(entry,metadata) in enumerate(((a,host),(b,client))):
                    frames=_decode_frames(entry,metadata,ck);frame_sets.append(frames)
                    if not dedicated and (require_complete or host.get('completeMatches') or client.get('completeMatches')):
                        _final(entry,frames,metadata,ck)
                    if dedicated:
                        receipts={ack['label']:ack for ack in entry.get('acks',[]) if not ack.get('diagnostic')}
                        required_receipts=({'rules-invalid':27,'rules-noop':0,'rules-temporary':0,'rules-apply':0,'lobby-ready':0}
                            if player==0 else {'rules-unauthorized':8,'rules-ready-initial':0,'rules-ready-stale':24,'rules-ready-temporary':0,'rules-ready-final':0})
                        for label,code in required_receipts.items():
                            ck(label in receipts and receipts[label]['code']==code,'Missing received host-rule ACK '+label)
                    if number==1 and len(hr)==2:
                        ck(any(ack.get('label')=='rematch' and ack.get('code')==0 and not ack.get('diagnostic') for ack in entry.get('acks',[])), 'Missing received rematch ACK')
                    ck('initial' in frames and 'start-granted' in frames,'Lobby and granted-start snapshots required.')
                    if 'initial' in frames:
                        initial=frames['initial'][1]
                        ck(initial['phase']==0 and not initial['startGoldGranted'] and all(p['gold']==0 for p in initial['players']),'Initial/rematch lobby already paid.')
                        if number==2:
                            ck(_rules(initial)==_configured_rules(metadata),'Rematch did not retain confirmed options.')
                    if dedicated:
                        ck('configured' in frames,'Confirmed configured lobby frame missing.')
                        if 'configured' in frames:
                            configured=frames['configured'][1]
                            ck(configured['phase']==0 and not configured['startGoldGranted'] and _rules(configured)==_configured_rules(metadata),'Configured lobby options/payout mismatch.')
                    if 'start-granted' in frames:
                        start=frames['start-granted'][1]
                        if len(lobby['start'])==1:
                            accepted=lobby['start'][0][2]
                            ck(start['configRevision']==accepted['configRevision'] and start['rosterRevision']==accepted['rosterRevision'],'Start frame revisions differ from authoritative grant.')
                        ck(_rules(start)==_configured_rules(metadata),'Started options differ from confirmed report.')
                        ck(start['phase'] in (1,2) and start['startGoldGranted'] and start['play']==0
                           and all(p['gold']==exact(metadata['expectedStartGold']) for p in start['players']),'Start payout is not exactly once/equal before gameplay.')
                        ck(start['connectedMask']==3 and all(p['ready'] for p in start['players']),'Started without current two-player consent.')
                    if dedicated:
                        if start_only:
                            _started_smoke(entry,frames,metadata,ck)
                        else:
                            reached=_final(entry,frames,metadata,ck);last_wave_proven &= reached
                            if require_final_wave:
                                ck(reached,'Configured final wave not reached; earlier base death is not last-wave proof.')
                        for ack in entry.get('acks',[]):
                            if ack.get('diagnostic'):continue
                            matches=proofs.get((str(ack['epoch']),player,ack['sequence']),[])
                            ck(any(x['result']['Code']==ack['code'] and x['result']['Tick']==ack['tick'] for x in matches),'Peer ACK lacks matching authority command.')
                            exact(ack['publicationSequence'],bits=64)
                if dedicated:
                    ck(bool(proofs),'Missing real command before/after evidence.')
                    ck(bool(lobby['effective']) and bool(lobby['ready-reset']) and bool(lobby['noop']),'Apply change/no-op/Ready reset evidence missing.')
                    ck(bool(lobby[(4,24)]),'Stale Ready rejection missing.')
                    ck(any(lobby[(10,code)] for code in (15,26,27)),'Invalid Apply rejection missing.')
                    ck(bool(lobby[(10,8)]),'Guest Apply rejection missing.')
                    if start_only:
                        ck(all(p['command']['Type'] in (4,10) for p in a.get('authority',[])), 'Startup-only proof includes gameplay/rematch commands.')
                        granted_sequences=[exact(f['start-granted'][0]['publicationSequence']) for f in frame_sets if 'start-granted' in f]
                        ck(len(granted_sequences)==2 and sum(epoch==exact(a['epoch']) and seq>max(granted_sequences,default=0) for epoch,seq,tick,digest in shared)>=3, 'Missing shared post-start publications.')
                    elif number==1:
                        ck(bool(lobby['rematch']) and all(x.get('rematchRequested') and x.get('rematchAccepted') for x in (a,b)),'Mutual confirmed rematch missing.')
                    else:
                        ck(exact(a['epoch'])==exact(hr[0]['epoch'])+1,'Rematch epoch did not advance exactly once.')
                    if not start_only:
                        for field in ('resultDigest','resultTick','playTicks','resultWave','resultHp0','resultHp1','outcome'):
                            ck(a.get(field) is not None and a.get(field)==b.get(field),'Final peers disagree on '+field)
                    ck(sum(epoch==exact(a['epoch']) for epoch,seq,tick,digest in shared)>=3,'Insufficient shared publications in configured round.')
                    summaries.append({'cycle':h['cycle'],'round':number,'epoch':a['epoch'],'host_authority_commands':len(a.get('authority',[])),
                                      'result':a.get('outcome'),'result_wave':a.get('resultWave'),'configured_waves':host['waveCount']})
    except (ValueError,KeyError,TypeError,IndexError,StopIteration,OverflowError,AttributeError) as error:
        check(False,'Malformed/incomplete Wire10 evidence: '+str(error))
    return {'passed':not problems,'problems':problems,'protocol':10,'acceptanceSchema':3,'rulesVersion':host.get('rulesVersion'),
            'hostRules':dedicated,'hostRulesStartOnly':start_only,'startup_only_proven':start_only and bool(summaries) and not problems,'last_wave_proven':dedicated and not start_only and last_wave_proven and bool(summaries) and not problems, 'rounds':summaries,
            'scope':('Synthetic/model evidence only; NO actual transport or cloud assertion.' if model else
                    'Two standalone processes via '+str(host.get('mode'))+'; separate internet access paths, human play and Android performance are not asserted.') +
                    (' STARTUP ONLY: configured options/readiness/start/shutdown; NO completed match, last-wave or rematch proof.' if start_only else '')}


def audit_batch(command,before,after,player,metadata,check):
    target=command['Target'];old=next((t for t in before['towers'] if t['id']==target),None)
    new=next((t for t in after['towers'] if t['id']==target),None)
    count=exact(command['UpgradeCount']);level=exact(command['ExpectedTowerLevel']);limit=exact(command['UpgradePriceLimit'])
    check(old is not None and new is not None,'batch tower missing')
    if old is None or new is None:return
    check(old['owner']==player and count>0 and level==old['level'],'batch authority/level')
    kind=old['kind'];profile=metadata['towerProfiles'][kind]
    cost=investment(kind,level+count,profile)-investment(kind,level,profile)
    check(0<cost<=limit and old['level']+count==new['level'],'batch exact aggregate cost/level')
    check(after['players'][player]['gold']==before['players'][player]['gold']-cost and new['invested']==old['invested']+cost,'batch atomic debit/investment')
    secondary=9 if kind==1 else 10 if kind==5 else 1
    primary=[3,4,5,6,2,7,8,0][kind];path=command['Kind']
    check(path in (primary,secondary) or path==0 and kind not in (2,4),'batch class path')
    if kind==4:check(new['damage']==new['range']==0,'mine combat path')
    else:
        field,other=('range','damage') if path==secondary else ('damage','range')
        check(new[field]==old[field]+count and new[other]==old[other],'batch path count')
    expected_tower=dict(old);expected_tower['level']+=count;expected_tower['invested']+=cost
    if kind!=4:expected_tower['range' if path==secondary else 'damage']+=count
    check(new==expected_tower,'batch mutated unrelated tower fields')
    check(len(after['towers'])==len(before['towers']),'batch changed tower count')
    for key in ('epoch','tick','play','wave','wavePhase','phase','configRevision','rosterRevision'):
        check(after[key]==before[key],'batch mutated clock/rule '+key)
    check(new['cooldown']==old['cooldown'],'batch reset cooldown')
    for key in ('units','orders','fields','teams'):
        check(after[key]==before[key],'batch mutated '+key)
    for i,p in enumerate(before['players']):
        expected=dict(p)
        if i==player:expected['gold']-=cost
        check(expected==after['players'][i],'batch mutated unrelated player state')
    for t in before['towers']:
        if t['id']!=target:check(t in after['towers'],'batch changed another tower')


def validate_windows(metadata):
    values=metadata.get('sendWindowTicksByWave')
    if not isinstance(values,list) or len(values)!=metadata['waveCount'] or any(type(n) is not int or n<600 for n in values):raise ValueError('versioned send windows missing')
    for row in metadata.get('neutralRecipes',[]):
        wave=exact(row['wave'])
        if wave<=len(values) and values[wave-1]!=max(600,(len(row['plan'])-1)*10+80):raise ValueError('send window differs from actual recipe')
