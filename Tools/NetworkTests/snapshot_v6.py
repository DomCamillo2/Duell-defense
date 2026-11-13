"""Independent bounded reader for evidence, not a replacement for the production geometry validator."""
import base64
import hashlib
import struct


def decode(encoded):
    data = base64.b64decode(encoded, validate=True)
    if not 193 <= len(data) <= 16384:
        raise ValueError("snapshot length")
    offset = 0

    def read(fmt):
        nonlocal offset
        size = struct.calcsize('<' + fmt)
        if offset + size > len(data):
            raise ValueError('truncated snapshot')
        values = struct.unpack_from('<' + fmt, data, offset)
        offset += size
        return values

    values = read('QIiiiBBBiii')
    s = dict(zip(('epoch', 'seed', 'tick', 'play', 'countdown', 'phase', 'outcome', 'wavePhase', 'wave', 'preparation', 'waveStart'), values))
    s['fields'] = [dict(zip(('cursor', 'nextSpawn', 'nextOrigin'), read('iiB'))) for _ in range(2)]
    s['players'] = []
    for _ in range(2):
        p = dict(zip(('gold', 'hp', 'ready', 'rematch', 'speed', 'waveReady', 'mask'), read('iiBBBBi')))
        if any(p[k] not in (0, 1) for k in ('ready', 'rematch', 'speed', 'waveReady')):
            raise ValueError('noncanonical boolean')
        p['nextSend'] = list(read('iiii'))
        p['research'] = list(read('iiii'))
        p['attack'], p['remainder'] = read('ii')
        count, = read('B')
        if not 1 <= count <= 10:
            raise ValueError('ledger count')
        ledger = [read('ii') for _ in range(count)]
        p['reserved'] = [x[0] for x in ledger]
        p['spawned'] = [x[1] for x in ledger]
        s['players'].append(p)
    count, = read('H')
    if count > 64:
        raise ValueError('tower count')
    s['towers'] = [dict(zip(('id', 'owner', 'slot', 'kind', 'level', 'damage', 'range', 'cooldown', 'invested', 'x', 'y', 'site', 'priority'), read('iBBBBBBiiiiiB'))) for _ in range(count)]
    count, = read('H')
    if count > 160:
        raise ValueError('unit count')
    s['units'] = [dict(zip(('id', 'defender', 'kind', 'hp', 'distance', 'slow', 'slowUntil', 'remainder', 'origin', 'wave', 'research'), read('iBBiiiiiBii'))) for _ in range(count)]
    count, = read('H')
    if count > 160:
        raise ValueError('order count')
    s['orders'] = [dict(zip(('sender', 'kind', 'remaining', 'nextTick', 'origin', 'research', 'price', 'initial', 'wave'), read('BBiiBiiii'))) for _ in range(count)]
    if offset != len(data):
        raise ValueError('trailing snapshot bytes')
    s['hash'] = hashlib.sha256(data).hexdigest()
    return s


def validate(s, metadata):
    """Recompute ledgers, allocation bounds, path costs and ownership from encoded evidence."""
    def require(condition, text):
        if not condition:
            raise ValueError(text)
    waves = metadata['waveCount']
    require(s['epoch'] > 0 and s['tick'] >= 0 and 0 <= s['play'] <= 2 * s['tick'], 'clocks')
    require(1 <= s['wave'] <= waves and s['wavePhase'] in (0, 1, 2), 'wave')
    require(0 <= s['preparation'] <= metadata['preparationTicks'] and 0 <= s['waveStart'] <= s['play'], 'preparation')
    require(s['wavePhase'] == 0 or s['preparation'] == 0, 'active preparation')
    require(s['phase'] in range(5) and s['outcome'] in range(5), 'phase enums')
    require((s['phase'] == 3 and s['outcome'] in (1, 2, 3)) or (s['phase'] == 4 and s['outcome'] == 4) or (s['phase'] < 3 and s['outcome'] == 0), 'outcome')
    ids, slots = set(), set()
    for tower in s['towers']:
        require(tower['id'] > 0 and tower['id'] not in ids, 'tower id'); ids.add(tower['id'])
        p, k, level = tower['owner'], tower['kind'], tower['level']
        require(p in (0, 1) and 0 <= k < len(metadata['towerProfiles']) and 0 <= level <= 2, 'tower definition')
        require(0 <= tower['slot'] < 32 and (p, tower['slot']) not in slots, 'render slot'); slots.add((p, tower['slot']))
        require(s['players'][p]['mask'] & (1 << k), 'locked tower')
        require(tower['site'] == -1 and tower['priority'] in (0, 1), 'free placement/priority')
        require((k == 4 and tower['damage'] == tower['range'] == tower['priority'] == 0) or (k != 4 and tower['damage'] + tower['range'] == level), 'upgrade path')
        profile = metadata['towerProfiles'][k]
        require(tower['invested'] == profile['price'] + sum(profile['upgradePrices'][:level]), 'investment')
    for p, player in enumerate(s['players']):
        require(player['mask'] in (31, 63, 95, 127) and player['mask'] < 1 << len(metadata['towerProfiles']), 'unlock mask')
        require(0 <= player['gold'] <= 10000 and 0 <= player['hp'] <= metadata['expectedBaseHp'], 'gold/HP')
        require(len(player['reserved']) == len(player['spawned']) == waves and 0 <= player['remainder'] < 960, 'ledger shape/remainder')
        require(all(0 <= x <= 8 for x in player['research']) and all(x >= 0 for x in player['nextSend']), 'research/departures')
        require(s['wavePhase'] == 0 or not player['waveReady'], 'stale readiness')
        orders = [o for o in s['orders'] if o['sender'] == p]
        require(len(orders) <= 80, 'per-player FIFO bound')
        prior = s['wave']
        for index, order in enumerate(orders):
            k = order['kind']
            require(0 <= k < 4 and order['origin'] == 1 and prior <= order['wave'] <= waves, 'packet origin/order')
            require(order['price'] == metadata['unitPrices'][k] and order['initial'] == metadata['unitCounts'][k], 'packet price/initial count')
            require(0 <= order['research'] <= player['research'][k] and 1 <= order['remaining'] <= order['initial'] and order['nextTick'] >= 0, 'frozen packet recipe')
            require(order['remaining'] == order['initial'] or index == 0 and order['wave'] == s['wave'], 'partial FIFO head')
            prior = order['wave']
        for w, (reserved, spawned) in enumerate(zip(player['reserved'], player['spawned']), 1):
            require(0 <= reserved <= metadata['waveBudget'] and 0 <= spawned <= 4 * reserved, 'wave budget')
            require(w <= s['wave'] or spawned == 0, 'future credit')
            outstanding = sum(4 * o['price'] - 4 * o['price'] * (o['initial'] - o['remaining']) // o['initial'] for o in orders if o['wave'] == w)
            require(spawned + outstanding == reserved * 4, 'exact quarter-gold ledger')
        require(player['attack'] == min(2880, sum(player['spawned'])), 'attack income activation/cap')
        own_towers = [t for t in s['towers'] if t['owner'] == p]
        require(sum(t['kind'] == 4 for t in own_towers) <= 2 and sum(t['kind'] != 4 for t in own_towers) <= 30, 'tower capacities')
        field = [u for u in s['units'] if u['defender'] == p]
        require(len(field) <= 80 and all(sum(u['origin'] == origin for u in field) <= 64 for origin in (0, 1)), 'origin/field caps')
    for unit in s['units']:
        require(unit['id'] > 0 and unit['id'] not in ids, 'unit id'); ids.add(unit['id'])
        require(unit['defender'] in (0, 1) and unit['origin'] in (0, 1) and unit['kind'] in range(4), 'unit owner/origin/kind')
        require(unit['wave'] == s['wave'] and unit['hp'] > 0 and unit['distance'] >= 0 and 600 <= unit['slow'] <= 1000, 'unit state')
        require(unit['research'] == 0 if unit['origin'] == 0 else 0 <= unit['research'] <= s['players'][1 - unit['defender']]['research'][unit['kind']], 'origin research')
    require(all(o['sender'] in (0, 1) for o in s['orders']), 'order owner')
    require(s['wavePhase'] != 0 or not s['units'], 'preparation live units')
    return s
