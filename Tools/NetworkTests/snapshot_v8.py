"""Independent Wire8 reader: canonical Wire7 body followed by a bounded rules trailer.

Publication sequence belongs to the transport envelope/report, never these bytes.
Historical Wire7 code stays untouched. The body layout is unchanged; Wire8
permits up to200 ledger entries only with its explicitly versioned wave plan.
"""
import base64
import hashlib
import re
import struct
import sys
from snapshot_v7 import validate as validate_body, investment

MAX_GOLD_BYTES = 65523
if hasattr(sys, 'set_int_max_str_digits'):
    sys.set_int_max_str_digits(0)


def exact(value, *, unsigned=True, bits=None):
    """Reject JSON floats/bools and noncanonical decimal strings before arithmetic."""
    if type(value) is int:
        result = value
    elif isinstance(value, str) and len(value) <= 157800 and re.fullmatch(r'0|[1-9][0-9]*' if unsigned else r'0|-?[1-9][0-9]*', value, flags=re.ASCII):
        result = int(value)
    else:
        raise ValueError('noncanonical exact integer')
    if unsigned and result < 0 or bits is not None and result.bit_length() > bits:
        raise ValueError('integer range')
    return result


def decode_body(encoded):
    data = base64.b64decode(encoded, validate=True)
    if not 193 <= len(data) <= 2147482623:
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

    def big():
        nonlocal offset
        n, = read('i')
        if n < 1 or n > len(data) - offset:
            raise ValueError('integer length')
        b = data[offset:offset+n]; offset += n
        if n > 1 and ((b[-1] == 0 and b[-2] < 128) or (b[-1] == 255 and b[-2] >= 128)):
            raise ValueError('noncanonical integer')
        return int.from_bytes(b, 'little', signed=True)

    def count(minimum):
        n, = read('i')
        if n < 0 or n * minimum > len(data) - offset:
            raise ValueError('record count')
        return n

    values = read('QIiiiBBBiii')
    s = dict(zip(('epoch', 'seed', 'tick', 'play', 'countdown', 'phase', 'outcome', 'wavePhase', 'wave', 'preparation', 'waveStart'), values))
    s['fields'] = [dict(zip(('cursor', 'nextSpawn', 'nextOrigin'), read('iiB'))) for _ in range(2)]
    s['players'] = []; s['_rematchOffsets'] = []
    for _ in range(2):
        gold = big()
        s['_rematchOffsets'].append(offset + 5)
        p = dict(zip(('hp', 'ready', 'rematch', 'speed', 'waveReady', 'mask'), read('iBBBBi')))
        p['gold'] = gold
        if any(p[k] not in (0, 1) for k in ('ready', 'rematch', 'speed', 'waveReady')):
            raise ValueError('noncanonical boolean')
        p['nextSend'] = list(read('iiii'))
        p['research'] = [big() for _ in range(4)]
        p['attack'], = read('i')
        p['remainder'] = big()
        ledgers, = read('B')
        if not 1 <= ledgers <= 200:
            raise ValueError('ledger count')
        ledger = [(big(), big()) for _ in range(ledgers)]
        p['reserved'] = [x[0] for x in ledger]
        p['spawned'] = [x[1] for x in ledger]
        s['players'].append(p)
    s['towers'] = []
    for _ in range(count(30)):
        t = dict(zip(('id', 'owner', 'slot', 'kind'), read('iBiB')))
        t.update(level=big(), damage=big(), range=big())
        t['cooldown'], = read('i'); t['invested'] = big()
        t.update(zip(('x', 'y', 'site', 'priority'), read('iiiB'))); s['towers'].append(t)
    s['units'] = []
    for _ in range(count(30)):
        u = dict(zip(('id', 'defender', 'kind'), read('iBB'))); u['hp'] = big()
        u.update(zip(('distance', 'slow'), read('ii'))); u['slowUntil'] = big()
        u.update(zip(('remainder', 'origin', 'wave'), read('iBi'))); u['research'] = big(); s['units'].append(u)
    s['orders'] = []
    for _ in range(count(20)):
        o = dict(zip(('sender', 'kind', 'remaining', 'nextTick', 'origin'), read('BBiiB')))
        o.update(research=big(), price=big()); o.update(zip(('initial', 'wave'), read('ii'))); s['orders'].append(o)
    if offset != len(data):
        raise ValueError('trailing snapshot bytes')
    s['hash'] = hashlib.sha256(data).hexdigest()
    return s


def decode(encoded):
    data = base64.b64decode(encoded, validate=True)
    if len(data) < 227:
        raise ValueError('Wire8 snapshot length')
    length, = struct.unpack_from('<i', data, len(data)-4)
    if length < 30 or length > 4+4+(MAX_GOLD_BYTES+1)+8+8+4+1 or length+4 >= len(data):
        raise ValueError('rules trailer length')
    start = len(data)-4-length
    waves, n = struct.unpack_from('<ii', data, start)
    if n < 1 or n > MAX_GOLD_BYTES+1 or length != 29+n:
        raise ValueError('rules integer length')
    raw = data[start+8:start+8+n]
    if n > 1 and ((raw[-1] == 0 and raw[-2] < 128) or (raw[-1] == 255 and raw[-2] >= 128)):
        raise ValueError('noncanonical rules integer')
    gold = int.from_bytes(raw, 'little', signed=True)
    if gold < 0 or gold.bit_length() > MAX_GOLD_BYTES*8 or not 1 <= waves <= 200:
        raise ValueError('unsupported rules')
    config, roster, connected, granted = struct.unpack_from('<QQiB', data, start+8+n)
    if granted not in (0, 1):
        raise ValueError('noncanonical grant boolean')
    s = decode_body(base64.b64encode(data[:start]))
    s.update(ruleWaves=waves, ruleStartGold=gold, configRevision=config,
             rosterRevision=roster, connectedMask=connected, startGoldGranted=granted)
    s['hash'] = hashlib.sha256(data).hexdigest()
    return s


def wave_limit(metadata):
    plans = {('defense-duell-core-v2.2', 'authored-prefix-only-v1'): 10,
             ('defense-duell-core-v2.3', 'authored-then-linear-v2'): 200}
    try:
        return plans[(metadata['rulesVersion'], metadata['wavePlanVersion'])]
    except (KeyError, TypeError):
        raise ValueError('unsupported explicit core/wave-plan pair')


def validate(s, metadata):
    if not 1 <= s['ruleWaves'] <= wave_limit(metadata):
        raise ValueError('wave count exceeds versioned plan')
    if s['connectedMask'] not in (1, 3):
        raise ValueError('connected roster')
    effective = dict(metadata, waveCount=s['ruleWaves'])
    validate_body(s, effective)
    if s['phase'] == 0:
        if s['startGoldGranted'] or any(p['gold'] for p in s['players']):
            raise ValueError('lobby granted start gold')
        if s['towers'] or s['units'] or s['orders']:
            raise ValueError('lobby gameplay resources')
    elif s['phase'] in (1, 2, 3) and not s['startGoldGranted']:
        raise ValueError('started without gold grant')
    if s['phase'] in (1, 2, 3) and s['connectedMask'] != 3:
        raise ValueError('started without complete roster')
    return s
