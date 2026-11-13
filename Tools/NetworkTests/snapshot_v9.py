"""Independent, exact Wire9 reader. Historical Wire7/8 readers remain unchanged."""
import base64
import hashlib
import struct
from snapshot_v8 import exact


SUPPORTED_RULE_VERSIONS = ('defense-duell-core-v2.4', 'defense-duell-core-v2.4.1')


def validate_version(metadata):
    if metadata['rulesVersion'] not in SUPPORTED_RULE_VERSIONS or metadata['wavePlanVersion'] != 'authored-then-exact-endless-v3':
        raise ValueError('version binding')


class Reader:
    def __init__(self, data):
        self.data, self.offset = data, 0

    def read(self, fmt):
        size = struct.calcsize('<' + fmt)
        if self.offset + size > len(self.data):
            raise ValueError('truncated snapshot')
        value = struct.unpack_from('<' + fmt, self.data, self.offset)
        self.offset += size
        return value

    def one(self, fmt):
        return self.read(fmt)[0]

    def boolean(self):
        value = self.one('B')
        if value not in (0, 1):
            raise ValueError('noncanonical boolean')
        return value

    def big(self):
        n = self.one('i')
        if n < 1 or n > len(self.data) - self.offset:
            raise ValueError('integer length')
        raw = self.data[self.offset:self.offset+n]
        self.offset += n
        if n > 1 and (raw[-1] == 0 and raw[-2] < 128 or raw[-1] == 255 and raw[-2] >= 128):
            raise ValueError('noncanonical integer')
        return int.from_bytes(raw, 'little', signed=True)

    def text(self):
        n = self.one('H')
        if n > 256 or n > len(self.data) - self.offset:
            raise ValueError('map identifier length')
        raw = self.data[self.offset:self.offset+n]
        self.offset += n
        return raw.decode('utf-8', errors='strict')

    def count(self, minimum):
        n = self.one('i')
        if n < 0 or n*minimum > len(self.data)-self.offset:
            raise ValueError('record count')
        return n

    def ledger(self):
        first, endless, n = self.big(), self.boolean(), self.one('H')
        if not 1 <= n <= 200 or n*5 > len(self.data)-self.offset:
            raise ValueError('ledger count')
        return dict(first=first, endless=endless, values=[self.big() for _ in range(n)])


def decode(encoded):
    data = base64.b64decode(encoded, validate=True)
    if not 100 <= len(data) <= 2147482623:
        raise ValueError('snapshot length')
    r = Reader(data)
    if r.read('IH') != (0x39534444, 9):
        raise ValueError('Wire9 header')
    s = dict(zip(('epoch', 'seed'), r.read('QI')))
    s.update(ruleWaves=r.one('i'), ruleStartGold=r.big(), endless=r.boolean(), income=r.one('i'), format=r.one('B'), mapId=r.text())
    s.update(zip(('configRevision', 'rosterRevision', 'connectedMask'), r.read('QQi')))
    s['startGoldGranted'] = r.boolean()
    s.update(zip(('tick', 'play', 'countdown', 'phase', 'outcome', 'winningTeam', 'wavePhase'), r.read('iiiBBiB')))
    s['wave'] = r.big()
    s.update(zip(('preparation', 'waveStart', 'playerCount', 'teamCount'), r.read('iiBB')))
    if s['playerCount'] not in (2, 4, 6) or s['teamCount'] not in (2, 3):
        raise ValueError('player/team shape')
    s['teams'] = [r.one('i') for _ in range(s['teamCount'])]
    s['fields'] = [dict(zip(('cursor', 'nextSpawn', 'nextOrigin', 'paidSender', 'nextPaid'), r.read('iiBii'))) for _ in range(s['playerCount'])]
    s['players'] = []; s['_rematchOffsets'] = []
    for _ in range(s['playerCount']):
        p = dict(zip(('team', 'target'), r.read('BB')))
        p['gold'] = r.big(); s['_rematchOffsets'].append(r.offset+1)
        p.update(ready=r.boolean(), rematch=r.boolean(), speed=r.boolean(), waveReady=r.boolean(), mask=r.one('i'))
        if p['team'] >= s['teamCount']: raise ValueError('player team')
        p['hp'] = s['teams'][p['team']]
        p['nextSend'] = list(r.read('iiii')); p['research'] = [r.big() for _ in range(4)]
        p.update(attack=r.one('i'), remainder=r.big(), archived=r.big())
        for key in ('reserved', 'spawned', 'cancelled'):
            p[key] = r.ledger()
        s['players'].append(p)
    s['towers'] = []
    for _ in range(r.count(30)):
        t = dict(zip(('id', 'owner', 'slot', 'kind'), r.read('iBiB')))
        t.update(level=r.big(), damage=r.big(), range=r.big(), cooldown=r.one('i'), invested=r.big())
        t.update(zip(('x', 'y', 'site', 'priority'), r.read('iiiB'))); s['towers'].append(t)
    s['units'] = []
    for _ in range(r.count(30)):
        u = dict(zip(('id', 'defender', 'sender', 'kind'), r.read('iBBB'))); u['hp'] = r.big()
        u.update(zip(('distance', 'slow'), r.read('ii'))); u['slowUntil'] = r.big()
        u.update(remainder=r.one('i'), origin=r.one('B'), wave=r.big(), research=r.big()); s['units'].append(u)
    s['orders'] = []
    for _ in range(r.count(20)):
        o = dict(zip(('sender', 'target', 'kind', 'remaining', 'nextTick', 'origin'), r.read('BBBiiB')))
        o.update(research=r.big(), price=r.big(), initial=r.one('i'), wave=r.big()); s['orders'].append(o)
    if r.offset != len(data):
        raise ValueError('trailing bytes')
    s['hash'] = hashlib.sha256(data).hexdigest()
    return s


def validate(s, metadata=None):
    def require(value, why):
        if not value:
            raise ValueError(why)
    shapes = {0: (2, 2, 1), 1: (4, 2, 2), 2: (6, 2, 3), 3: (6, 3, 2)}
    require(s['format'] in shapes, 'format')
    count, teams, members = shapes[s['format']]
    require((s['playerCount'], s['teamCount']) == (count, teams), 'rules roster mismatch')
    require(1 <= s['ruleWaves'] <= 200 and s['ruleStartGold'] >= 0 and s['ruleStartGold'].bit_length() <= 65523*8, 'rules numbers')
    require(s['income'] in (1, 2, 3, 5, 10) and s['mapId'], 'income/map')
    require(s['epoch'] > 0 and 0 <= s['play'] <= 2*s['tick'] and s['wave'] >= 1, 'clocks')
    require(s['endless'] or s['wave'] <= s['ruleWaves'], 'finite boundary')
    require(s['connectedMask'] & 1 and not s['connectedMask'] & ~((1 << count)-1), 'connections')
    require(s['phase'] in range(5) and s['wavePhase'] in range(3), 'phases')
    require(s['winningTeam'] == {1: 0, 2: 1, 5: 2}.get(s['outcome'], -1) and s['winningTeam'] < teams, 'winning team')
    require(all(0 <= hp <= 30*members for hp in s['teams']), 'shared health')
    require(s['phase'] not in (1, 2, 3) or s['startGoldGranted'], 'missing grant')
    require(s['phase'] != 0 or not s['startGoldGranted'], 'lobby grant')
    ids = set()
    for p in s['players']:
        require(0 <= p['team'] < teams and 0 <= p['target'] < count, 'team/target')
        require(0 < p['mask'] <= 255 and p['gold'] >= 0 and 0 <= p['remainder'] < 960 and 0 <= p['attack'] <= 2880 and p['archived'] >= 0, 'player economy')
        require(all(x >= 0 for x in p['research']+p['nextSend']), 'player progression')
        for key in ('reserved', 'spawned', 'cancelled'):
            ledger = p[key]
            require(ledger['first'] == (s['wave'] if s['endless'] else 1) and ledger['endless'] == s['endless'] and len(ledger['values']) == (2 if s['endless'] else s['ruleWaves']), 'ledger shape')
            require(all(v >= 0 for v in ledger['values']), 'negative ledger')
        require(p['attack'] == min(2880, p['archived']+sum(p['spawned']['values'])), 'spawn income credit')
    if s['startGoldGranted'] or any(p['ready'] for p in s['players']):
        require(s['connectedMask'] == (1 << count)-1, 'ready incomplete roster')
        require(all(sum(p['team'] == t for p in s['players']) == members for t in range(teams)), 'unbalanced started teams')
    if not s['startGoldGranted']:
        require(not s['towers'] and not s['units'] and not s['orders'], 'lobby entities')
        require(all(p['gold'] == p['attack'] == p['remainder'] == p['archived'] == 0 and not any(p['research']) and not any(p['nextSend']) for p in s['players']), 'lobby economy')
        require(all(not any(p[k]['values']) for p in s['players'] for k in ('reserved', 'spawned', 'cancelled')), 'lobby ledger')
    for entity in s['towers']+s['units']:
        require(entity['id'] > 0 and entity['id'] not in ids, 'entity id'); ids.add(entity['id'])
    for t in s['towers']:
        require(0 <= t['owner'] < count and 0 <= t['kind'] < 8 and t['slot'] >= 0, 'tower ownership')
        require(s['players'][t['owner']]['mask'] & (1 << t['kind']), 'tower loadout')
        require(t['level'] >= 0 and t['damage'] >= 0 and t['range'] >= 0 and t['invested'] > 0, 'tower progression')
        require(t['damage'] == t['range'] == 0 if t['kind'] == 4 else t['level'] == t['damage']+t['range'], 'tower path total')
    for u in s['units']:
        require(0 <= u['sender'] < count and 0 <= u['defender'] < count and 0 <= u['kind'] < 4 and u['hp'] > 0 and u['wave'] == s['wave'], 'unit shape')
        require(u['origin'] in (0, 1), 'unit origin')
        require(u['sender'] == u['defender'] and u['research'] == 0 if u['origin'] == 0 else s['players'][u['sender']]['team'] != s['players'][u['defender']]['team'], 'unit routing')
    for o in s['orders']:
        require(0 <= o['sender'] < count and 0 <= o['target'] < count and 0 <= o['kind'] < 4 and o['origin'] == 1, 'paid order shape')
        require(s['players'][o['sender']]['team'] != s['players'][o['target']]['team'] and 0 < o['remaining'] <= o['initial'] and o['price'] > 0, 'paid order route')
    for owner, p in enumerate(s['players']):
        for index, reserved in enumerate(p['reserved']['values']):
            wave = p['reserved']['first']+index
            remaining = sum(4*o['price']-4*o['price']*(o['initial']-o['remaining'])//o['initial'] for o in s['orders'] if o['sender'] == owner and o['wave'] == wave)
            require(4*reserved == p['spawned']['values'][index]+p['cancelled']['values'][index]+remaining, 'exact paid conservation')
    if metadata is not None:
        validate_version(metadata)
    return s
