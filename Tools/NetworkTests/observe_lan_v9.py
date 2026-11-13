#!/usr/bin/env python3
"""Read actual LAN announcements without advertising or joining. Run alongside an owned host acceptance process."""
import argparse
import ipaddress
import json
from pathlib import Path
import socket
import struct
import time
import uuid


def decode(data, source):
    if not 109 <= len(data) <= 512: raise ValueError('size')
    if struct.unpack_from('<IH', data) != (0x314c4444, 9): raise ValueError('protocol')
    process = str(uuid.UUID(bytes_le=data[6:22])); epoch, sequence = struct.unpack_from('<QQ', data, 22)
    catalog = data[38:102].decode('ascii'); port, players, capacity, joinable = struct.unpack_from('<HBBB', data, 102)
    offset = 107; labels = []
    for bound in (128, 32):
        length, = struct.unpack_from('<H', data, offset); offset += 2
        if not 1 <= length <= bound or offset+length > len(data): raise ValueError('label size')
        label = data[offset:offset+length].decode('utf-8'); offset += length
        if not label.strip() or any(ord(c) < 32 or c in '<>' for c in label): raise ValueError('label')
        labels.append(label)
    address = ipaddress.ip_address(source)
    if offset != len(data) or address.version != 4 or address.is_multicast or address.is_unspecified: raise ValueError('address/trailing')
    if not epoch or not sequence or not port or capacity not in (2,4,6) or not 1 <= players <= capacity or joinable not in (0,1) or joinable and players >= capacity: raise ValueError('values')
    if len(catalog) != 64 or any(c not in '0123456789abcdef' for c in catalog): raise ValueError('catalog')
    return dict(process=process,epoch=str(epoch),sequence=str(sequence),catalogHash=catalog,sourceIp=source,port=port,players=players,capacity=capacity,joinable=bool(joinable),mapName=labels[0],formatName=labels[1])


def main():
    p=argparse.ArgumentParser();p.add_argument('--output',required=True);p.add_argument('--seconds',type=float,default=20);a=p.parse_args()
    target=Path(a.output)
    if target.exists():p.error('Preserve prior observation; choose a new output file.')
    observations=[];malformed=0
    with socket.socket(socket.AF_INET,socket.SOCK_DGRAM) as sock:
        sock.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEADDR,1)
        # Darwin requires both listeners to opt into REUSEPORT. .NET/Mono does so
        # through ReuseAddress; Python needs this explicit option.
        if hasattr(socket,'SO_REUSEPORT'):sock.setsockopt(socket.SOL_SOCKET,socket.SO_REUSEPORT,1)
        sock.bind(('',19175));sock.settimeout(.25)
        began=time.monotonic()
        while time.monotonic()-began<a.seconds:
            try:data,source=sock.recvfrom(513)
            except socket.timeout:continue
            try:row=decode(data,source[0]);row['wall']=time.monotonic()-began;observations.append(row)
            except (ValueError,UnicodeError,struct.error):malformed+=1
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(dict(scope='Actual UDP observation only; no autojoin, device or UI-reception claim',observations=observations,malformed=malformed),indent=2))
    print(f'{len(observations)} actual compatible Wire9 announcements captured')


if __name__=='__main__':main()
