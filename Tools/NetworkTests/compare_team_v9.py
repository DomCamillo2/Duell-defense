"""Compare four/six actual peer reports. Pure fixtures require explicit allow_model and never prove transport."""
import json
from collections import defaultdict
from snapshot_v9 import decode, validate, exact, validate_version


def compare(reports, *, expected_format=None, expected_map=None, expected_income=None, expected_start_gold=None, expected_start_only=None, allow_model=False):
    problems, summaries = [], []
    def check(value, text):
        if not value:
            problems.append(text)
    try:
        check(isinstance(reports, list) and len(reports) in (4, 6), 'four or six independent reports required')
        host = next(r for r in reports if r['role'] == 'host')
        capacity = {1: 4, 2: 6, 3: 6}[host['format']]
        check(len(reports) == capacity and sum(r['role'] == 'host' for r in reports) == 1, 'format/host count')
        check(sorted(r['slot'] for r in reports) == list(range(capacity)), 'unique authoritative slot assignment')
        reports = sorted(reports, key=lambda r: r['slot'])
        check(host['slot'] == 0, 'host slot')
        for key, expected in [('format', expected_format), ('mapIndex', expected_map), ('incomeMultiplier', expected_income), ('startOnly', expected_start_only)]:
            if expected is not None:
                check(host[key] == expected, 'requested '+key)
        if expected_start_gold is not None:
            check(exact(host['startGold']) == exact(expected_start_gold), 'requested exact gold')
        start_only = host['startOnly']
        check(type(start_only) is bool, 'explicit scope boolean')
        rounds = 1 if start_only else 2
        for r in reports:
            validate_version(r)
            check(r['protocol'] == 9 and r['acceptanceSchema'] == 4, 'Wire9/team schema required')
            check(r['mode'] in ('lan', 'relay-dtls') or allow_model and r['mode'] == 'PureDotnetModel', 'actual transport evidence required')
            for key in ('mode', 'format', 'capacity', 'mapIndex', 'incomeMultiplier', 'startGold', 'endless', 'startOnly', 'catalogHash', 'baseContentHash', 'contentHash', 'rulesVersion', 'wavePlanVersion'):
                check(r[key] == host[key], 'peer metadata mismatch '+key)
            check(r['passed'] is True and r['cleanShutdown'] is True and not r['failure'], 'peer failure/unclean shutdown')
            if r['slot'] != 0:
                check(r['disconnectObserved'] is True, 'guest must observe ordered host shutdown')
            check(len(r['rounds']) == rounds, 'round count')
            if r['mode'] == 'relay-dtls':
                check(r['relayAllocationEstablished'] and r['authenticationProfile'] and r['anonymousPlayerFingerprint'], 'actual authenticated Relay allocation')
        if host['mode'] == 'relay-dtls':
            check(len({r['authenticationProfile'] for r in reports}) == capacity and len({r['anonymousPlayerFingerprint'] for r in reports}) == capacity, 'independent Relay identities')
        all_frames = []
        for ri in range(rounds):
            peer_frames = []
            for r in reports:
                entry = r['rounds'][ri]; frames = {}
                last_pub = 0
                for f in entry['frames']:
                    state = validate(decode(f['bytes']), r)
                    seq = exact(f['publicationSequence'], bits=64)
                    check(seq > last_pub or seq == last_pub and frames, 'frame publication order')
                    last_pub = seq
                    check(f['hash'] == state['hash'] and f['epoch'] == state['epoch'] == entry['epoch'] and f['tick'] == state['tick'] and exact(f['wave']) == state['wave'], 'frame identity')
                    check(f['name'] not in frames, 'duplicate frame label')
                    frames[f['name']] = (f, state)
                for name in ('initial', 'configured', 'roster-ready', 'start-granted', 'started-smoke' if start_only else 'actions-complete', *(() if start_only else ('final',))):
                    check(name in frames, 'missing '+name)
                initial = frames['initial'][1]
                check(initial['phase'] == 0 and not initial['startGoldGranted'] and all(p['gold'] == 0 for p in initial['players']), 'fresh lobby resources')
                configured = frames['configured'][1]
                check(configured['mapId'] == ('meadow','desert','frost','emerald-basin','amber-canyon','aurora-bay')[r['mapIndex']], 'host map index/content binding')
                check(configured['configRevision'] == 2 and configured['format'] == r['format'] and configured['income'] == r['incomeMultiplier'] and configured['ruleStartGold'] == exact(r['startGold']) and configured['endless'] == r['endless'], 'confirmed final rules')
                ready = frames['roster-ready'][1]
                check(ready['connectedMask'] == (1 << capacity)-1, 'no fake/missing participant slots')
                check([p['mask'] for p in ready['players']] == [129 | 1 << (p % 7) for p in range(capacity)], 'per-player loadouts')
                started = frames['start-granted'][1]
                check(started['phase'] in (1, 2) and started['startGoldGranted'] and all(p['gold'] == exact(r['startGold']) for p in started['players']), 'exact once-only initial grant')
                check(started['mapId'] == configured['mapId'] and started['rosterRevision'] == ready['rosterRevision'], 'start uses observed map/roster')
                acks = {a['label']: a for a in entry['acks']}
                required = {'ready': 0} if start_only else {'ready': 0, 'build-own': 0, 'foreign-upgrade': 8, 'upgrade-own': 0, 'ally-send': 7, 'paid-target': 0, 'change-target': 0, 'speed-vote': 0}
                for label, code in required.items():
                    check(label in acks and acks[label]['code'] == code, 'required ACK '+label)
                for ack in entry['acks']:
                    check(exact(ack['publicationSequence'], bits=64) > 0 and ack['epoch'] == entry['epoch'] and exact(ack['sequence'], bits=64) > 0, 'ACK identity/publication')
                if start_only:
                    smoke = frames['started-smoke'][1]
                    check(smoke['phase'] in (1, 2) and smoke['startGoldGranted'] and entry['smokeSeconds'] >= 3 and frames['started-smoke'][0]['wall']-frames['start-granted'][0]['wall'] >= 3, 'startup smoke duration')
                    check(not entry['finished'] and not entry['rematchRequested'] and not smoke['towers'] and not smoke['orders'], 'startup scope excludes gameplay completion')
                else:
                    final = frames['final'][1]
                    check(final['phase'] == 3 and final['outcome'] in (1, 2, 3, 5) and final['wave'] == final['ruleWaves'] == 1 and entry['finished'], 'natural last-wave result')
                    check(entry['resultDigest'] == final['hash'], 'result digest')
                    if ri == 0:
                        check(entry['rematchRequested'] and acks.get('rematch', {}).get('code') == 0, 'unanimous rematch ACK')
                peer_frames.append(frames)
            check(len({r['rounds'][ri]['epoch'] for r in reports}) == 1, 'shared round epoch')
            if ri:
                check(reports[0]['rounds'][ri]['epoch'] == reports[0]['rounds'][ri-1]['epoch']+1, 'exact rematch successor')
            if not start_only:
                finals = [f['final'][1] for f in peer_frames]
                check(all((f['teams'], f['winningTeam'], f['outcome'], f['tick'], f['play']) == (finals[0]['teams'], finals[0]['winningTeam'], finals[0]['outcome'], finals[0]['tick'], finals[0]['play']) for f in finals), 'all peer result/health agreement')
            all_frames.append(peer_frames)
            summaries.append(dict(round=ri+1, epoch=reports[0]['rounds'][ri]['epoch'], players=capacity, complete=not start_only))
        # Every required receipt binds to the real authenticated host command sender and exact request.
        for ri, entry in enumerate(host['rounds']):
            proofs, grants, applied, targets = {}, 0, 0, set()
            for proof in entry['authority']:
                c, result, p = proof['command'], proof['result'], proof['player']
                before, after = validate(decode(proof['before']), host), validate(decode(proof['after']), host)
                check(0 <= p < capacity and c['SessionId'] == before['epoch'] and c['Sequence'] == result['Sequence'], 'authenticated command identity')
                key = (p, c['SessionId'], c['Sequence']); check(key not in proofs, 'unexpected duplicate authority request'); proofs[key] = proof
                code, kind = result['Code'], c['Type']
                if code:
                    check(proof['before'] == proof['after'], 'rejection mutated authoritative state'); continue
                if not before['startGoldGranted'] and after['startGoldGranted']:
                    grants += 1
                    check(kind == 4 and all(x['gold'] == exact(host['startGold']) for x in after['players']), 'start grant authority')
                if kind == 10:
                    applied += 1
                    check(p == 0 and exact(c['ExpectedConfigRevision']) == before['configRevision'] and after['configRevision'] == before['configRevision']+1 and not any(x['ready'] for x in after['players']), 'host config authority/reset')
                    check(after['mapId'] == c['RuleMapId'] and after['income'] == c['RuleIncomeMultiplier'] and after['format'] == c['RuleFormat'], 'accepted exact options')
                if kind in (4, 11, 12):
                    check(exact(c['ExpectedConfigRevision']) == before['configRevision'] and exact(c['ExpectedRosterRevision']) == before['rosterRevision'], 'observed roster tokens')
                if kind == 0:
                    added = [t for t in after['towers'] if t['id'] not in {t['id'] for t in before['towers']}]
                    check(len(added) == 1 and added[0]['owner'] == p and added[0]['kind'] == c['Kind'], 'own tower build')
                    check(after['players'][p]['gold'] == before['players'][p]['gold']-added[0]['invested'], 'exact tower debit')
                if kind == 1:
                    old = next(t for t in before['towers'] if t['id'] == c['Target']); new = next(t for t in after['towers'] if t['id'] == c['Target'])
                    check(old['owner'] == new['owner'] == p and new['level'] == old['level']+1 and after['players'][p]['gold'] == before['players'][p]['gold']-(new['invested']-old['invested']), 'own exact upgrade')
                if kind == 3:
                    packet = after['orders'][-1]
                    check(after['orders'][:-1] == before['orders'] and len(after['orders']) == len(before['orders'])+1, 'paid FIFO append')
                    check(packet['sender'] == p and packet['target'] == c['EnemyTarget'] and packet['wave'] == exact(c['ExpectedWave']) and packet['research'] == exact(c['ExpectedResearch']), 'immutable explicit paid destination/quote')
                    check(after['players'][p]['gold'] == before['players'][p]['gold']-packet['price'], 'exact packet debit'); targets.add(p)
                if kind == 13:
                    check(after['players'][p]['target'] == c['Kind'] and before['orders'] == after['orders'], 'target selection rerouted existing paid packets')
            check(applied == 2 and grants == 1, 'two observed config changes and exactly one start grant per round')
            if not start_only:
                check(targets == set(range(capacity)), 'every real peer purchased an explicit enemy packet')
            for r in reports:
                for ack in r['rounds'][ri]['acks']:
                    proof = proofs.get((r['slot'], ack['epoch'], ack['sequence']))
                    check(proof is not None and proof['result']['Code'] == ack['code'], 'ACK lacks matching authenticated host proof')
        # Same publication is the same exact state on every receiving peer; no regressions after rematch.
        sets = []
        for r in reports:
            items, last_seq, last_epoch = {}, 0, 0
            for cp in r['checkpoints']:
                seq = exact(cp['publicationSequence'], bits=64); epoch = exact(cp['epoch'], bits=64)
                check(seq >= last_seq and epoch >= last_epoch, 'accepted stale publication/epoch')
                value = (cp['tick'], cp['hash']); key = (epoch, seq)
                check(key not in items or items[key] == value, 'conflicting same publication')
                items[key] = value; last_seq, last_epoch = seq, epoch
            sets.append(items)
        for peer_index, peer in enumerate(sets[1:], 1):
            shared = sets[0].keys() & peer.keys()
            for ri in range(rounds):
                epoch = reports[0]['rounds'][ri]['epoch']
                minimum = max(exact(all_frames[ri][0]['start-granted'][0]['publicationSequence']),exact(all_frames[ri][peer_index]['start-granted'][0]['publicationSequence']))
                check(sum(e == epoch and seq >= minimum for e,seq in shared) >= 3, 'insufficient shared publications after start')
            check(len(shared) >= 3, 'insufficient shared authoritative publications')
            check(all(sets[0][k] == peer[k] for k in shared), 'host/peer snapshot disagreement')
    except (KeyError, ValueError, TypeError, IndexError, StopIteration, OverflowError) as error:
        check(False, 'Malformed/incomplete Wire9 evidence: '+str(error))
    return dict(passed=not problems, protocol=9, problems=problems, rounds=summaries, actual_transport_proven=not problems and not allow_model, startup_only_proven=not problems and bool(reports and reports[0].get('startOnly')), last_wave_proven=not problems and bool(reports and not reports[0].get('startOnly')))


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser(); parser.add_argument('reports', nargs='+'); parser.add_argument('--output', required=True)
    args = parser.parse_args(); result = compare([json.load(open(path)) for path in args.reports])
    with open(args.output, 'x') as handle: json.dump(result, handle, indent=2)
    print(json.dumps(result, indent=2)); raise SystemExit(0 if result['passed'] else 1)
