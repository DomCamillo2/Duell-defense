import base64
import copy
import gzip
import json
from pathlib import Path
import unittest
from compare_team_v9 import compare
from snapshot_v9 import decode, validate


class TeamEvidenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        with gzip.open(Path(__file__).parent/'Fixtures/v9/team-models.json.gz','rt') as handle: cls.groups = json.load(handle)
    def fixture(self, key='3-False'): return copy.deepcopy(self.groups[key])
    def reject(self, reports): self.assertFalse(compare(reports, allow_model=True)['passed'])
    def test_all_six_real_core_model_groups_cross_language(self):
        for name, reports in self.groups.items():
            result = compare(reports, allow_model=True)
            self.assertTrue(result['passed'], (name, result['problems']))
            self.assertFalse(result['actual_transport_proven'])
    def test_models_never_claim_actual_transport(self):
        for reports in self.groups.values(): self.assertFalse(compare(reports)['passed'])
    def test_unique_slots_required(self):
        x=self.fixture();x[-1]['slot']=x[-2]['slot'];self.reject(x)
    def test_all_six_peers_required(self): self.reject(self.fixture()[:-1])
    def test_peer_failure_rejected(self):
        x=self.fixture();x[2]['failure']='transport dropped';self.reject(x)
    def test_clean_shutdown_required(self):
        x=self.fixture();x[0]['cleanShutdown']=False;self.reject(x)
    def test_guest_observes_shutdown(self):
        x=self.fixture();x[1]['disconnectObserved']=False;self.reject(x)
    def test_exact_options_bound(self):
        for field,value in [('incomeMultiplier',5),('startGold','999'),('mapIndex',0),('format',2)]:
            x=self.fixture();x[1][field]=value;self.reject(x)
    def test_forged_expected_map_rejected(self):
        x=self.fixture()
        for r in x:r['mapIndex']=0
        self.reject(x)
    def test_required_ack_cannot_be_removed(self):
        x=self.fixture();x[2]['rounds'][0]['acks']=[a for a in x[2]['rounds'][0]['acks'] if a['label']!='paid-target'];self.reject(x)
    def test_ack_must_have_host_authority(self):
        x=self.fixture();x[0]['rounds'][0]['authority']=x[0]['rounds'][0]['authority'][1:];self.reject(x)
    def test_paid_target_bound_to_command(self):
        x=self.fixture();proof=next(p for p in x[0]['rounds'][0]['authority'] if p['command']['Type']==3 and p['result']['Code']==0);proof['command']['EnemyTarget']=proof['player'];self.reject(x)
    def test_sender_cannot_be_relabelled(self):
        x=self.fixture();proof=next(p for p in x[0]['rounds'][0]['authority'] if p['command']['Type']==3 and p['result']['Code']==0);proof['player']=5;self.reject(x)
    def test_publication_cannot_regress(self):
        x=self.fixture();x[1]['checkpoints'][-1]['publicationSequence']='1';self.reject(x)
    def test_conflicting_hash_same_publication(self):
        x=self.fixture();x[1]['checkpoints'][-1]['hash']='0'*64;self.reject(x)
    def test_float_publication_rejected(self):
        x=self.fixture();x[1]['checkpoints'][-1]['publicationSequence']=1.0;self.reject(x)
    def test_noncanonical_wave_rejected(self):
        x=self.fixture();x[1]['rounds'][0]['frames'][0]['wave']='01';self.reject(x)
    def test_rematch_evidence_required(self):
        x=self.fixture();x[1]['rounds'][0]['rematchRequested']=False;self.reject(x)
    def test_start_only_does_not_prove_last_wave(self):
        x=self.fixture('3-True');r=compare(x,allow_model=True);self.assertTrue(r['startup_only_proven']);self.assertFalse(r['last_wave_proven'])
    def test_start_only_duration_required(self):
        x=self.fixture('3-True');x[1]['rounds'][0]['smokeSeconds']=0;self.reject(x)
    def test_post_start_shared_publications_required(self):
        x=self.fixture('3-True');limit=int(next(f for f in x[1]['rounds'][0]['frames'] if f['name']=='start-granted')['publicationSequence']);x[1]['checkpoints']=[c for c in x[1]['checkpoints'] if int(c['publicationSequence'])<limit];self.reject(x)
    def test_truncation_and_trailing_bytes_rejected(self):
        raw=base64.b64decode(self.fixture()[0]['rounds'][0]['frames'][0]['bytes'])
        for length in range(len(raw)):
            with self.assertRaises(ValueError):decode(base64.b64encode(raw[:length]))
        with self.assertRaises(ValueError):decode(base64.b64encode(raw+b'\0'))
    def test_mutated_team_shapes_and_rules_rejected(self):
        state=decode(self.fixture()[0]['rounds'][0]['frames'][0]['bytes'])
        for key,value in [('playerCount',4),('format',9),('income',4),('ruleWaves',201),('endless',0),('winningTeam',0)]:
            invalid=copy.deepcopy(state);invalid[key]=value
            if key=='endless':invalid['wave']=201
            with self.assertRaises(ValueError,msg=key):validate(invalid)
    def test_shared_hp_and_ledger_corruption_rejected(self):
        state=decode(self.fixture()[0]['rounds'][0]['frames'][0]['bytes'])
        for mutate in [lambda s:s['teams'].__setitem__(0,61),lambda s:s['players'][0]['reserved']['values'].__setitem__(0,1),lambda s:s['players'][0].__setitem__('mask',256),lambda s:s['players'][0].__setitem__('gold',1)]:
            invalid=copy.deepcopy(state);mutate(invalid)
            with self.assertRaises(ValueError):validate(invalid)


if __name__=='__main__':unittest.main()
