"""Wire9 regression against snapshots emitted by the actual C# Core/driver/codec.

The fixture is a model, never a live transport claim. No Python snapshot writer
constructs the positive cases; mutations exercise independent failure checks.
"""
import argparse
import base64
import copy
import gzip
import hashlib
import json
from pathlib import Path
import unittest
from compare_duel_v9 import compare, decode, validate
from run_lan import compare as dispatch, add_version_arguments, rules_arguments
from snapshot_v9 import Reader

FIXTURE = Path(__file__).parent/'Fixtures/v9/duel-goldens.json.gz'


def pair(name):
    case = json.loads(gzip.decompress(FIXTURE.read_bytes()))[name]
    return case['host'], case['client']


def first_proof(report, kind, accepted=True):
    return next(p for p in report['cycles'][0]['rounds'][0]['authority']
                if p['command']['Type'] == kind and (p['result']['Code'] == 0) == accepted)


def corrupt_snapshot(frame, mutate):
    data = bytearray(base64.b64decode(frame['bytes']))
    mutate(data)
    frame['bytes'] = base64.b64encode(data).decode()
    frame['hash'] = hashlib.sha256(data).hexdigest()


class DuelWire9Tests(unittest.TestCase):
    def reject(self, host, client, **kwargs):
        result = compare(host, client, allow_model=True, **kwargs)
        self.assertFalse(result['passed'], result)
        self.assertTrue(result['problems'])
        return result

    def test_actual_csharp_goldens_pass_each_supported_scope(self):
        for name in ('default','rules1-zero','rules1-big','rules3','start200'):
            with self.subTest(name=name):
                host, client = pair(name)
                result = compare(host,client,allow_model=True)
                self.assertTrue(result['passed'],result['problems'])
                self.assertEqual(result['protocol'],9)
                self.assertIn('NO actual transport',result['scope'])
                self.assertEqual(result['startup_only_proven'],name=='start200')
                self.assertEqual(result['last_wave_proven'],name.startswith('rules'))

    def test_model_does_not_masquerade_as_transport(self):
        self.assertFalse(compare(*pair('rules1-zero'))['passed'])

    def test_runner_auto_dispatches_real_nine_and_rejects_forced_eight(self):
        host, client = pair('rules1-zero')
        self.assertTrue(dispatch(host,client,allow_model=True)['passed'])
        self.assertFalse(dispatch(host,client,protocol='8',allow_model=True)['passed'])

    def test_protocol_metadata_cannot_relabel_wire_bytes(self):
        host,client=pair('rules1-zero');host['protocol']=client['protocol']=8
        self.reject(host,client)
        self.assertFalse(dispatch(host,client,allow_model=True)['passed'])

    def test_core_and_wave_plan_must_both_be_current(self):
        for key,value in [('rulesVersion','defense-duell-core-v2.3'),('wavePlanVersion','authored-then-linear-v2')]:
            host,client=pair('rules1-zero');host[key]=client[key]=value
            self.reject(host,client)

    def test_wave_report_scalars_are_exact_canonical_strings(self):
        for value in (1,1.0,True,'01','+1','1.0','1e0'):
            host,client=pair('rules1-zero');host['cycles'][0]['rounds'][0]['frames'][0]['wave']=value
            self.reject(host,client)

    def test_wave_summary_is_bound_to_actual_snapshot(self):
        host,client=pair('rules1-zero');host['cycles'][0]['rounds'][0]['frames'][0]['wave']='2'
        self.reject(host,client)

    def test_final_wave_summary_is_not_a_float_or_unbound_string(self):
        for value in (1.0,'2','01'):
            host,client=pair('rules1-zero');host['cycles'][0]['rounds'][0]['resultWave']=value
            self.reject(host,client)

    def test_exact_large_gold_is_not_rounded(self):
        host,client=pair('rules1-big')
        self.assertGreater(int(host['expectedStartGold']),2**63)
        self.reject(host,client,expected_start_gold=str(int(host['expectedStartGold'])+1))
        host['expectedStartGold']=client['expectedStartGold']=float(host['expectedStartGold'])
        self.reject(host,client)

    def test_map_income_format_and_endless_are_bound(self):
        for key,value in [('mapId','invented-map'),('incomeMultiplier',10),('matchFormat',1),('ruleEndless',True)]:
            host,client=pair('rules1-zero');host[key]=client[key]=value
            self.reject(host,client)

    def test_requested_200_and_201_boundary(self):
        host,client=pair('start200')
        self.assertTrue(compare(host,client,allow_model=True,expected_waves=200,expected_start_only=True)['passed'])
        self.reject(host,client,expected_waves=201)
        host['waveCount']=client['waveCount']=201
        self.reject(host,client)

    def test_start_only_never_proves_full_match_or_rematch(self):
        host,client=pair('start200')
        for extra in ({'require_complete':True},{'require_rapid':True},{'expected_start_only':False}):
            self.reject(host,client,**extra)
        host['cycles'][0]['rounds'][0]['resultObserved']=True
        self.reject(host,client)

    def test_unobserved_optional_result_handles_unity_empty_string(self):
        host,client=pair('start200')
        for report in (host,client):report['cycles'][0]['rounds'][0]['resultWave']=''
        self.assertTrue(compare(host,client,allow_model=True)['passed'])
        host['cycles'][0]['rounds'][0]['resultObserved']=True
        self.reject(host,client)

    def test_start_only_needs_measured_wall_duration(self):
        host,client=pair('start200');host['cycles'][0]['rounds'][0]['startedSmokeSeconds']=2.9
        self.reject(host,client)

    def test_clean_shutdown_and_guest_observation_required(self):
        for role,key in [(0,'cleanShutdown'),(1,'disconnectObserved')]:
            host,client=pair('rules1-zero');(host,client)[role]['cycles'][0][key]=False
            self.reject(host,client)

    def test_rule_noop_reset_and_stale_ready_proofs_are_required(self):
        for kind,code in [(4,24),(10,8),(10,27)]:
            host,client=pair('rules1-zero');entry=host['cycles'][0]['rounds'][0]
            entry['authority']=[p for p in entry['authority'] if not (p['command']['Type']==kind and p['result']['Code']==code)]
            self.reject(host,client)

    def test_ready_revisions_and_exact_rule_gold_are_bound(self):
        for field in ('ExpectedConfigRevision','ExpectedRosterRevision'):
            host,client=pair('rules1-zero');first_proof(host,4)['command'][field]='999'
            self.reject(host,client)
        host,client=pair('rules1-zero');first_proof(host,10)['command']['RuleStartGold']='1'
        self.reject(host,client)

    def test_enemy_target_and_quote_are_bound_to_paid_fifo(self):
        for field,value in [('EnemyTarget',0),('ExpectedWave','999999999999999999999999'),('ExpectedResearch','123')]:
            host,client=pair('default');proof=first_proof(host,3)
            proof['command'][field]=proof['player'] if field=='EnemyTarget' else value
            self.reject(host,client)

    def test_received_ack_cannot_claim_different_host_result(self):
        host,client=pair('default');next(a for a in client['cycles'][0]['rounds'][0]['acks'] if not a.get('diagnostic'))['code']=255
        self.reject(host,client)

    def test_conflicting_publication_and_final_digest_fail(self):
        host,client=pair('rules1-zero');client['cycles'][0]['checkpoints'][0]['hash']='0'*64
        self.reject(host,client)
        host,client=pair('rules1-zero');host['cycles'][0]['rounds'][0]['resultDigest']='0'*64
        self.reject(host,client)

    def test_default_requires_tempo_windows_and_rapid_gold_proof(self):
        host,client=pair('default');host['cycles'][0]['rounds'][0]['clocks']=[]
        self.reject(host,client)
        host,client=pair('default');host['cycles'][0]['rounds'][0]['rapidAccepted']=0
        self.reject(host,client)

    def test_rematch_requires_new_epoch_and_both_confirmations(self):
        host,client=pair('rules1-zero');host['cycles'][0]['rounds'][0]['rematchAccepted']=False
        self.reject(host,client)
        host,client=pair('rules1-zero');host['cycles'][0]['rounds'][1]['epoch']=host['cycles'][0]['rounds'][0]['epoch']
        self.reject(host,client)

    def test_snapshot_tampering_fails_even_with_recomputed_hash(self):
        for mutation in (lambda b:b.__setitem__(0,0),lambda b:b.extend(b'\x00'),lambda b:b.__setitem__(-1,127)):
            host,client=pair('rules1-zero')
            corrupt_snapshot(host['cycles'][0]['rounds'][0]['frames'][0],mutation)
            self.reject(host,client)

    def test_cancelled_ledger_is_preserved_and_conservation_checked(self):
        host,_=pair('default')
        proof=first_proof(host,3);state=decode(proof['after'])
        self.assertIn('cancelled',state['players'][proof['player']]['_ledgers'])
        # Decoder validation is exercised against the C# bytes. Independently
        # mutate the raw cancelled ledger scalar, preserving canonical encoding.
        raw=bytearray(base64.b64decode(proof['after']));r=Reader(raw)
        r.read('IHQI');r.one('i');r.big();r.boolean();r.one('i');r.one('B');r.text()
        r.read('QQi');r.boolean();r.read('iiiBBiB');r.big();r.read('ii');players,teams=r.read('BB')
        r.read('i'*teams)
        for _ in range(players):r.read('iiBii')
        r.read('BB');r.big();r.read('BBBBi');r.read('iiii')
        for _ in range(4):r.big()
        r.one('i');r.big();r.big();r.ledger();r.ledger()
        r.big();r.boolean();r.one('H');n=r.one('i');self.assertEqual(n,1)
        raw[r.offset]=1
        with self.assertRaisesRegex(ValueError,'conservation'):
            decode(base64.b64encode(raw).decode())

    def test_full_default_requires_encoded_final_not_only_summary(self):
        host,client=pair('default')
        entry=host['cycles'][0]['rounds'][0]
        entry['frames']=[f for f in entry['frames'] if f['name']!='final']
        self.reject(host,client)

    def test_host_rules_require_received_acks_not_only_host_commands(self):
        host,client=pair('rules1-zero');client['cycles'][0]['rounds'][0]['acks']=[]
        self.reject(host,client)

    def test_rematch_summary_cannot_replace_authoritative_transition(self):
        host,client=pair('default');entry=host['cycles'][0]['rounds'][0]
        entry['authority']=[p for p in entry['authority'] if p['command']['Type']!=5]
        self.reject(host,client)

    def test_runner_accepts_explicit_nine_without_relabeling(self):
        parser=argparse.ArgumentParser();add_version_arguments(parser)
        args=parser.parse_args(['--protocol','9','--host-rules-start-only','--waves','200','--start-gold','0'])
        flags=rules_arguments(args,parser)
        self.assertIn('--duel-host-rules-start-only',flags)
        self.assertEqual(args.protocol,'9')


if __name__=='__main__':unittest.main()
