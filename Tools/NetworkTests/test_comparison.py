"""Wave6 evidence regression. Frozen pure-model fixture never qualifies as actual transport."""
import base64
import copy
import gzip
import hashlib
import json
from pathlib import Path
import struct
import unittest
from compare_v6 import compare
from snapshot_v6 import decode, validate

_FIXTURE = json.loads(gzip.decompress((Path(__file__).parent / 'Fixtures/wave6-driver-model.json.gz').read_bytes()))


def fixture():
    return copy.deepcopy(_FIXTURE['host']), copy.deepcopy(_FIXTURE['client'])


def frame(report, name, number=0):
    return next(f for f in report['cycles'][0]['rounds'][number]['frames'] if f['name'] == name)


def edit_frame(item, offset, fmt, value):
    data = bytearray(base64.b64decode(item['bytes']))
    struct.pack_into('<' + fmt, data, offset, value)
    item['bytes'] = base64.b64encode(data).decode()
    item['hash'] = hashlib.sha256(data).hexdigest()


class EvidenceTests(unittest.TestCase):
    def assertRejected(self, host, client):
        result = compare(host, client, True, True, allow_model=True)
        self.assertFalse(result['passed'], result)
        self.assertTrue(result['problems'])

    def test_complete_model_is_structurally_valid_but_explicitly_not_actual_transport(self):
        host, client = fixture()
        result = compare(host, client, True, True, allow_model=True)
        self.assertTrue(result['passed'], result)
        self.assertIn('NO actual', result['scope'])
        self.assertFalse(compare(host, client, True, True)['passed'])

    def test_all_old_wire_versions_are_rejected(self):
        for version in range(6):
            with self.subTest(version=version):
                h, c = fixture(); h['protocol'] = c['protocol'] = version
                self.assertRejected(h, c)

    def test_missing_completed_round_or_same_epoch_fails(self):
        h, c = fixture(); c['cycles'][0]['rounds'].pop(); self.assertRejected(h, c)
        h, c = fixture(); c['cycles'][0]['rounds'][1]['epoch'] = c['cycles'][0]['rounds'][0]['epoch']; self.assertRejected(h, c)

    def test_bad_source_map_and_content_identity_fails(self):
        for key, value in [('catalogHash', ''), ('contentHash', 'f' * 64), ('requestedMapIndex', 2)]:
            with self.subTest(key=key):
                h, c = fixture(); h[key] = value; self.assertRejected(h, c)
        h, c = fixture(); c['cycles'][0]['initialMapIndex'] = 1; self.assertRejected(h, c)

    def test_wrong_control_or_production_rules_fail(self):
        for key, value in [('tickRate', 40), ('waveCount', 8), ('preparationTicks', 100), ('waveBudget', 1000)]:
            with self.subTest(key=key):
                h, c = fixture(); h[key] = c[key] = value; self.assertRejected(h, c)

    def test_missing_required_frames_and_flags_fail(self):
        for name in ('preparation-stable', 'old-recipe', 'mixed-origins-and-credit', 'future-after', 'mixed-paths'):
            with self.subTest(frame=name):
                h, c = fixture(); c['cycles'][0]['rounds'][0]['frames'].remove(frame(c, name)); self.assertRejected(h, c)
        for flag in ('freshResources', 'scenarioPassed', 'earlyStart', 'automaticPreparation', 'futurePacket'):
            with self.subTest(flag=flag):
                h, c = fixture(); h['cycles'][0]['rounds'][0][flag] = False; self.assertRejected(h, c)

    def test_ack_before_snapshot_cannot_prove_a_rapid_purchase(self):
        h, c = fixture()
        old = frame(c, 'rapid-before'); post = frame(c, 'rapid-after')
        post.update({k: old[k] for k in ('bytes', 'hash', 'tick', 'playTicks', 'wave', 'phase', 'epoch')})
        self.assertRejected(h, c)

    def test_snapshot_hash_and_metadata_tampering_fails(self):
        h, c = fixture(); frame(c, 'rapid-after')['hash'] = '0' * 64; self.assertRejected(h, c)
        h, c = fixture(); frame(c, 'rapid-after')['tick'] += 1; self.assertRejected(h, c)

    def test_rematch_unlock_mask_cannot_reset_to_default(self):
        h, c = fixture(); edit_frame(frame(c, 'initial', 1), 57 + 12, 'i', 31); self.assertRejected(h, c)

    def test_unbounded_count_or_trailing_snapshot_fails(self):
        h, c = fixture(); edit_frame(frame(c, 'initial'), 331, 'H', 65535); self.assertRejected(h, c)
        h, c = fixture(); f = frame(c, 'initial'); data = base64.b64decode(f['bytes']) + b'\x00'; f['bytes'] = base64.b64encode(data).decode(); f['hash'] = hashlib.sha256(data).hexdigest(); self.assertRejected(h, c)

    def test_noncanonical_boolean_and_invalid_ledger_rejected(self):
        h, c = fixture(); edit_frame(frame(c, 'initial'), 57 + 10, 'B', 2); self.assertRejected(h, c)
        h, c = fixture(); edit_frame(frame(c, 'initial'), 57 + 57, 'i', 1); self.assertRejected(h, c)
        h, c = fixture(); edit_frame(frame(c, 'initial'), 57 + 52, 'i', 960); self.assertRejected(h, c)

    def test_rejected_quote_must_leave_authoritative_state_unchanged(self):
        h, c = fixture()
        proof = next(x for x in h['cycles'][0]['rounds'][0]['authority'] if x['result']['Code'] == 21)
        data = bytearray(base64.b64decode(proof['after'])); struct.pack_into('<i', data, 57, struct.unpack_from('<i', data, 57)[0] + 1)
        proof['after'] = base64.b64encode(data).decode(); self.assertRejected(h, c)

    def test_successful_debit_is_recomputed_from_host_before_after(self):
        h, c = fixture()
        proof = next(x for x in h['cycles'][0]['rounds'][0]['authority'] if x['command']['Type'] == 0 and x['result']['Code'] == 0)
        data = bytearray(base64.b64decode(proof['after'])); struct.pack_into('<i', data, 57, struct.unpack_from('<i', data, 57)[0] + 1)
        proof['after'] = base64.b64encode(data).decode(); self.assertRejected(h, c)

    def test_exact_replay_must_return_original_ack_tick(self):
        h, c = fixture(); proofs = h['cycles'][0]['rounds'][0]['authority']; seen = set()
        for proof in proofs:
            key = proof['player'], proof['command']['Sequence']
            if key in seen and proof['result']['Code'] == 0:
                proof['result']['Tick'] += 1; break
            seen.add(key)
        self.assertRejected(h, c)

    def test_changed_coordinate_replay_cannot_be_accepted(self):
        h, c = fixture(); proof = next(x for x in h['cycles'][0]['rounds'][0]['authority'] if x['result']['Code'] == 2); proof['result']['Code'] = 0
        self.assertRejected(h, c)

    def test_client_ack_requires_identical_host_execution(self):
        h, c = fixture(); ack = next(x for x in c['cycles'][0]['rounds'][0]['acks'] if x['label'] == 'research'); ack['tick'] += 1
        self.assertRejected(h, c)

    def test_wrong_upgrade_stat_and_missing_path_ack_fail(self):
        h, c = fixture(); frame(c, 'mixed-paths')['towers'][0]['damage'] += 1; self.assertRejected(h, c)
        h, c = fixture(); acks = c['cycles'][0]['rounds'][0]['acks']; acks[:] = [x for x in acks if x['label'] != 'range']; self.assertRejected(h, c)

    def test_clock_claims_need_real_sample_deltas(self):
        for key, value in [('controlsPerSecond', 40), ('gameplayPerSecond', 20), ('seconds', .2), ('controlEnd', 9999)]:
            with self.subTest(key=key):
                h, c = fixture(); c['cycles'][0]['rounds'][0]['clocks'][1][key] = value; self.assertRejected(h, c)

    def test_final_digest_is_bound_to_snapshot_not_just_peer_agreement(self):
        h, c = fixture(); h['cycles'][0]['rounds'][0]['resultDigest'] = c['cycles'][0]['rounds'][0]['resultDigest'] = 'f' * 64
        self.assertRejected(h, c)

    def test_final_hp_clock_and_result_summary_are_bound(self):
        for field in ('resultHp0', 'resultTick', 'playTicks', 'resultWave'):
            with self.subTest(field=field):
                h, c = fixture(); h['cycles'][0]['rounds'][0][field] += 1; c['cycles'][0]['rounds'][0][field] += 1; self.assertRejected(h, c)

    def test_lobby_only_common_snapshots_cannot_pass(self):
        h, c = fixture()
        for r in (h, c):
            r['cycles'][0]['checkpoints'] = [x for x in r['cycles'][0]['checkpoints'] if x['tick'] < 60]
        self.assertRejected(h, c)

    def test_actual_relay_requires_allocation_and_distinct_accounts(self):
        for missing in ('relayAllocationEstablished', 'authenticationProfile', 'anonymousPlayerFingerprint'):
            with self.subTest(missing=missing):
                h, c = fixture(); h['mode'] = c['mode'] = 'relay-dtls'
                for r, role in ((h, 'host'), (c, 'client')):
                    r['cycles'][0].update(relayAllocationEstablished=True, authenticationProfile=role, anonymousPlayerFingerprint=role + '_fingerprint')
                c['cycles'][0][missing] = False if missing == 'relayAllocationEstablished' else h['cycles'][0][missing]
                self.assertRejected(h, c)

    def test_frame_reader_is_bounded_for_all_truncations(self):
        raw = base64.b64decode(frame(_FIXTURE['host'], 'mixed-origins-and-credit')['bytes'])
        for length in range(len(raw)):
            with self.assertRaises((ValueError, struct.error)):
                decode(base64.b64encode(raw[:length]).decode())

    def test_all_fixture_authoritative_frames_have_exact_ledgers(self):
        count = 0
        for report in (_FIXTURE['host'], _FIXTURE['client']):
            for entry in report['cycles'][0]['rounds']:
                for item in entry['frames']:
                    validate(decode(item['bytes']), report); count += 1
        self.assertEqual(count, 124)


if __name__ == '__main__':
    unittest.main()
