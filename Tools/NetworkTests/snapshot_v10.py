"""Independent, exact Wire10 reader. Historical Wire7/8 readers remain unchanged."""
import base64
import hashlib
import struct
from snapshot_v8 import exact


SUPPORTED_RULE_VERSIONS = ('defense-duell-core-v2.5',)


def validate_version(metadata):
    if type(metadata.get('protocol')) is not int or metadata['protocol']!=10:
        raise ValueError('Wire10 metadata protocol')
    if metadata['rulesVersion'] not in SUPPORTED_RULE_VERSIONS or metadata['wavePlanVersion'] != 'authored-then-counter-profiles-boss10-v4':
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
    if r.read('IH') != (0x41534444, 10):
        raise ValueError('Wire10 header')
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
        p['nextSend'] = list(r.read('iiiiii')); p['research'] = [r.big() for _ in range(6)]
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
        u.update(remainder=r.one('i'), origin=r.one('B'), wave=r.big(), research=r.big(), neutralRecipeIndex=r.one('i')); s['units'].append(u)
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
    require(0<=s['countdown']<=60 and 0<=s['preparation']<=200 and 0<=s['waveStart']<=s['play'],'control bounds')
    require(s['wavePhase']==0 or s['preparation']==0,'active preparation')
    require(s['outcome'] in ((1,2,3,5) if s['phase']==3 else (4,) if s['phase']==4 else (0,)),'phase outcome')
    require(all(f['cursor']>=0 and f['nextSpawn']>=0 and f['nextOrigin'] in (0,1) and 0<=f['paidSender']<count and f['nextPaid']>=0 for f in s['fields']),'spawn fields')
    require(s['wavePhase']!=0 or all(f['cursor']==0 for f in s['fields']),'preparation cursor')
    require(s['winningTeam'] == {1: 0, 2: 1, 5: 2}.get(s['outcome'], -1) and s['winningTeam'] < teams, 'winning team')
    require(all(0 <= hp <= 30*members for hp in s['teams']), 'shared health')
    require(s['phase'] not in (1, 2, 3) or s['startGoldGranted'], 'missing grant')
    require(s['phase'] != 0 or not s['startGoldGranted'], 'lobby grant')
    ids = set()
    for p in s['players']:
        require(0 <= p['team'] < teams and 0 <= p['target'] < count, 'team/target')
        require(0 < p['mask'] <= 255 and bin(p['mask']).count('1') <= 5 and p['gold'] >= 0 and 0 <= p['remainder'] < 960 and 0 <= p['attack'] <= 2880 and p['archived'] >= 0, 'player economy')
        require(len(p['research'])==len(p['nextSend'])==6 and all(x >= 0 for x in p['research']+p['nextSend']), 'player progression')
        require(s['wavePhase']==0 or not p['waveReady'],'stale wave vote')
        for key in ('reserved', 'spawned', 'cancelled'):
            ledger = p[key]
            require(ledger['first'] == (s['wave'] if s['endless'] else 1) and ledger['endless'] == s['endless'] and len(ledger['values']) == (2 if s['endless'] else s['ruleWaves']), 'ledger shape')
            require(all(v >= 0 for v in ledger['values']), 'negative ledger')
        require(p['attack'] == min(2880, p['archived']+sum(p['spawned']['values'])), 'spawn income credit')
        require(s['endless'] or p['archived']==0,'finite archived credit')
        require(all(value==0 or p['spawned']['first']+i<=s['wave'] for i,value in enumerate(p['spawned']['values'])),'future spawn credit')
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
        require(t['invested']==[65,105,85,120,160,125,120,180][t['kind']]+investment(t['kind'],t['level'],{}),'exact tower investment')
        require(t['priority'] in (0,1) and (t['kind']!=4 or t['priority']==0) and 0<=t['cooldown']<=[8,32,20,42,0,24,36,8][t['kind']],'tower cooldown/priority')
    ordinals = set()
    for u in s['units']:
        require(0 <= u['sender'] < count and 0 <= u['defender'] < count and 0 <= u['kind'] < 6 and u['hp'] > 0 and u['wave'] == s['wave'], 'unit shape')
        require(u['origin'] in (0, 1) and u['research']>=0, 'unit origin')
        require(u['distance']>=0 and 600<=u['slow']<=1000 and u['slowUntil']>=0 and 0<=u['remainder']<20000,'unit movement')
        npi = u['neutralRecipeIndex']
        if u['origin'] == 0:
            require(u['kind'] < 4 and 0 <= npi < s['fields'][u['defender']]['cursor'], 'neutral ordinal bounds')
            require((u['defender'], npi) not in ordinals, 'duplicate neutral ordinal')
            ordinals.add((u['defender'], npi))
            if metadata is not None:
                plan = recipe_plan(metadata, s['wave'])
                require(npi < len(plan) and plan[npi] == u['kind'], 'neutral ordinal recipe kind')
        else:
            require(npi == -1, 'paid unit claimed neutral ordinal')
            require(0 <= u['research'] <= s['players'][u['sender']]['research'][u['kind']], 'paid unit frozen research')
        require(u['sender'] == u['defender'] and u['research'] == 0 if u['origin'] == 0 else s['players'][u['sender']]['team'] != s['players'][u['defender']]['team'], 'unit routing')
    prior_orders={}
    for o in s['orders']:
        require(0 <= o['sender'] < count and 0 <= o['target'] < count and 0 <= o['kind'] < 6 and o['origin'] == 1, 'paid order shape')
        previous=prior_orders.get(o['sender'])
        require(s['wave']<=o['wave']<=s['players'][o['sender']]['reserved']['first']+len(s['players'][o['sender']]['reserved']['values'])-1,'order wave ledger bounds')
        require(previous is None or o['wave']>=previous,'FIFO wave monotonic')
        require(o['remaining']==o['initial'] or previous is None and o['wave']==s['wave'],'partial FIFO head')
        prior_orders[o['sender']]=o['wave']
        require(o['nextTick']>=0 and o['research']>=0,'order recipe timing')
        require(s['players'][o['sender']]['team'] != s['players'][o['target']]['team'] and 0 < o['remaining'] <= o['initial'] and o['price'] > 0, 'paid order route')
    for owner, p in enumerate(s['players']):
        for index, reserved in enumerate(p['reserved']['values']):
            wave = p['reserved']['first']+index
            remaining = sum(4*o['price']-4*o['price']*(o['initial']-o['remaining'])//o['initial'] for o in s['orders'] if o['sender'] == owner and o['wave'] == wave)
            require(4*reserved == p['spawned']['values'][index]+p['cancelled']['values'][index]+remaining, 'exact paid conservation')
    prices=[18,24,65,38,1200,1600];counts=[1,1,1,4,1,1]
    for owner,p in enumerate(s['players']):
        alive=sum(4*prices[u['kind']]//counts[u['kind']] for u in s['units'] if u['origin']==1 and u['sender']==owner)
        current=s['wave']-p['spawned']['first']
        require(alive<=p['spawned']['values'][current],'alive paid bodies lack spawned credit')
    if metadata is not None:
        validate_version(metadata)
        for o in s['orders']:
            require(o['price'] == metadata['unitPrices'][o['kind']] and o['initial'] == metadata['unitCounts'][o['kind']], 'paid recipe price/count')
            require(o['research'] <= s['players'][o['sender']]['research'][o['kind']], 'paid order future research')
        for unit in s['units']:validate_unit_recipe(unit,s,metadata)
        plan = recipe_plan(metadata, s['wave'])
        require(all(f['cursor'] <= len(plan) for f in s['fields']), 'neutral cursor exceeds plan')
    return s


def recipe_row(metadata, wave):
    rows=metadata.get('neutralRecipes')
    if not isinstance(rows,list):raise ValueError('Wire10 requires neutralRecipes metadata')
    found={}
    for row in rows:
        if not isinstance(row,dict) or not isinstance(row.get('wave'),str):raise ValueError('recipe wave must be canonical text')
        number=exact(row['wave'])
        if number<1 or str(number)!=row['wave'] or row['wave'] in found:raise ValueError('duplicate/noncanonical recipe wave')
        if not isinstance(row.get('profileId'),str) or not row['profileId']:raise ValueError('recipe profile')
        if type(row.get('boss')) is not bool or type(row.get('superBoss')) is not bool:raise ValueError('recipe boss flags')
        hp=row.get('hpPerKind')
        if not isinstance(hp,list) or len(hp)!=4 or any(not isinstance(v,str) or exact(v)<1 for v in hp):raise ValueError('recipe exact HP')
        if not isinstance(row.get('pressureBudgetHp'),str) or exact(row['pressureBudgetHp'])<0:raise ValueError('recipe budget')
        plan=row.get('plan')
        if not isinstance(plan,list) or not plan or any(type(k) is not int or k not in range(4) for k in plan):raise ValueError('invalid neutral recipe plan')
        index=row.get('bossIndex')
        if type(index) is not int or row['boss'] and not 0<=index<len(plan) or not row['boss'] and index!=-1:raise ValueError('boss ordinal metadata')
        found[row['wave']]=row
    if str(wave) not in found:raise ValueError('missing exact neutral recipe wave '+str(wave))
    return found[str(wave)]


def recipe_plan(metadata, wave):
    return recipe_row(metadata,wave)['plan']


def unit_max_hp(kind,level):
    bases=[34,22,220,26,1000,3200];linear=[4,1,40,3,70,180];num=[1,1,4,1,6,24];den=[2,4,1,2,2,2]
    if kind not in range(6) or level<0:raise ValueError('paid recipe')
    return bases[kind]+linear[kind]*level+num[kind]*level*(level-1)//den[kind]


def validate_unit_recipe(unit,state,metadata):
    if unit['origin']==0:
        row=recipe_row(metadata,state['wave']);n=unit['neutralRecipeIndex']
        if not 0<=n<len(row['plan']) or row['plan'][n]!=unit['kind']:raise ValueError('neutral ordinal recipe kind')
        hp=exact(row['hpPerKind'][unit['kind']])
    else:hp=unit_max_hp(unit['kind'],unit['research'])
    if not 0<unit['hp']<=hp:raise ValueError('unit HP exceeds frozen recipe')


def decode_handshake(data, content_hash):
    if len(data) != 74 or struct.unpack_from('<IH',data) != (0x41444444,10):
        raise ValueError('Wire10 handshake')
    encoded=data[6:70].decode('ascii',errors='strict')
    if encoded != content_hash or len(encoded)!=64 or any(c not in '0123456789abcdef' for c in encoded):
        raise ValueError('content hash mismatch')
    mask=struct.unpack_from('<i',data,70)[0]
    if not 0 < mask <= 255 or bin(mask).count('1')>5:raise ValueError('loadout cap')
    return mask


def decode_command(encoded):
    data=base64.b64decode(encoded,validate=True)
    if not 95<=len(data)<=131072:raise ValueError('Wire10 command length')
    r=Reader(data)
    c=dict(zip(('SessionId','Sequence','Type','Kind','Target','PositionX','PositionY'),r.read('QQBiiii')))
    c.update(ExpectedWave=r.big(),ExpectedResearch=r.big())
    c.update(zip(('ExpectedConfigRevision','ExpectedRosterRevision','RuleWaveCount'),r.read('QQi')))
    c.update(RuleStartGold=r.big(),RuleEndless=r.boolean(),RuleIncomeMultiplier=r.one('i'),RuleFormat=r.one('B'),RuleMapId=r.text(),EnemyTarget=r.one('i'))
    c.update(UpgradeCount=r.big(),ExpectedTowerLevel=r.big(),UpgradePriceLimit=r.big())
    if r.offset!=len(data) or not c['Sequence'] or c['Type'] not in range(15) or c['ExpectedResearch']<0 or c['ExpectedWave']<0 or c['RuleFormat'] not in range(4):
        raise ValueError('command canonical fields')
    if any(c[k]<0 for k in ('UpgradeCount','ExpectedTowerLevel','UpgradePriceLimit')):raise ValueError('negative batch scalar')
    if c['Type']!=14 and any(c[k]!=0 for k in ('UpgradeCount','ExpectedTowerLevel','UpgradePriceLimit')):raise ValueError('unexpected batch scalar')
    if c['Type']==14 and (c['UpgradeCount']==0 or c['UpgradePriceLimit']==0):raise ValueError('empty batch')
    return c


def investment(kind, level, profile):
    """Exact Core2.5 accumulated upgrade price, excluding initial building cost."""
    if type(level) is not int or level<0 or kind not in range(8):raise ValueError('investment arguments')
    if kind!=4:
        bases=[10,18,8,20,0,18,20,24];steps=[5,8,3,9,0,8,8,10]
        return bases[kind]*level+steps[kind]*level*(level-1)//2
    # Sum floor((2n²+27n+70)/8) in constant time, with an eight-step residue cycle.
    polynomial=level*(level+1)*(2*level+1)//3+27*level*(level+1)//2+63*level
    residues=[(8-(2*n*n+27*n+63)%8)%8 for n in range(1,9)]
    return (polynomial+(level//8)*sum(residues)+sum(residues[:level%8]))//8
