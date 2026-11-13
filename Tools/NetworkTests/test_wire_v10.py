"""Wire10 production-codec goldens and fail-closed reader/authority checks. Not transport."""
import base64
import copy
import json
from pathlib import Path
import struct
import unittest
from snapshot_v10 import decode,validate,decode_command,decode_handshake,validate_version,investment
from compare_duel_v10 import audit_batch,validate_cadence,compare as compare_duel, _authority
from compare_team_v10 import compare as compare_team

ROOT=Path(__file__).resolve().parent
G=json.loads((ROOT/'Fixtures/v10/goldens.json').read_text())
META=G['metadata']

class Wire10Tests(unittest.TestCase):
    def test_actual_csharp_frames_six_class_arrays(self):
        for frame in G['frames']:
            with self.subTest(frame=frame['name']):
                s=validate(decode(frame['bytes']),META)
                self.assertTrue(all(len(p['nextSend'])==len(p['research'])==6 for p in s['players']))
        s=validate(decode(next(f['bytes'] for f in G['frames'] if f['name']=='elite-and-neutral')),META)
        self.assertTrue({4,5}.issubset({u['kind'] for u in s['units'] if u['origin']==1}))
        self.assertTrue(all(u['neutralRecipeIndex']==-1 for u in s['units'] if u['origin']==1))
    def test_csharp_rejected_negative_snapshots(self):
        for f in G['negatives']:
            with self.subTest(case=f['name']), self.assertRaises(ValueError):validate(decode(f['bytes']),META)
    def test_valid_boss_ordinal_matches_production_plan(self):
        s=validate(decode(next(f['bytes'] for f in G['frames'] if f['name']=='boss-wave10')),META)
        self.assertEqual(s['wave'],10)
        self.assertTrue(s['units'])
        for u in s['units']:
            self.assertEqual(next(r['plan'] for r in META['neutralRecipes'] if r['wave']=='10')[u['neutralRecipeIndex']],u['kind'])
    def test_missing_plan_or_wrong_plan_fails_closed(self):
        state=decode(next(f['bytes'] for f in G['frames'] if f['name']=='elite-and-neutral'))
        m=copy.deepcopy(META);m.pop('neutralRecipes')
        with self.assertRaises(ValueError):validate(state,m)
        m=copy.deepcopy(META);m['neutralRecipes'][0]['plan']=[3]*len(m['neutralRecipes'][0]['plan'])
        with self.assertRaises(ValueError):validate(state,m)
    def test_truncation_trailing_and_old_version(self):
        for frame in G['frames']:
            raw=base64.b64decode(frame['bytes'])
            for n in sorted(set(list(range(min(96,len(raw))))+list(range(96,len(raw),53)))):
                with self.assertRaises((ValueError,UnicodeError,struct.error)):decode(base64.b64encode(raw[:n]))
            with self.assertRaises(ValueError):decode(base64.b64encode(raw+b'\0'))
            for version in (7,8,9,11):
                bad=bytearray(raw);struct.pack_into('<H',bad,4,version)
                with self.assertRaises(ValueError):decode(base64.b64encode(bad))
    def test_handshake_real_and_loadout_cap(self):
        raw=base64.b64decode(G['handshake']);self.assertEqual(decode_handshake(raw,G['contentHash']),143)
        for mask in (0,63,127,255,-1,256):
            bad=raw[:70]+struct.pack('<i',mask)
            with self.assertRaises(ValueError):decode_handshake(bad,G['contentHash'])
        for mask in (1,31,143,248):self.assertEqual(decode_handshake(raw[:70]+struct.pack('<i',mask),G['contentHash']),mask)
        with self.assertRaises(ValueError):decode_handshake(raw,'0'*64)
        bad=bytearray(raw);struct.pack_into('<H',bad,4,9)
        with self.assertRaises(ValueError):decode_handshake(bad,G['contentHash'])
    def test_real_commands_include_three_exact_batch_integers(self):
        for p in G['proofs']:
            c=decode_command(p['wire'])
            for k,v in c.items():
                expected=p['command'][k]
                if k=='RuleMapId':expected=expected or ''
                elif k in ('SessionId','Sequence','ExpectedWave','ExpectedResearch','ExpectedConfigRevision','ExpectedRosterRevision','RuleStartGold','UpgradeCount','ExpectedTowerLevel','UpgradePriceLimit'):expected=int(expected)
                self.assertEqual(v,expected,(p['label'],k))
            raw=base64.b64decode(p['wire'])
            for n in range(len(raw)):
                with self.assertRaises((ValueError,struct.error,UnicodeError)):decode_command(base64.b64encode(raw[:n]))
            with self.assertRaises(ValueError):decode_command(base64.b64encode(raw+b'\0'))
    def test_batch_canonical_and_replay_atomicity(self):
        proofs={p['label']:p for p in G['proofs']}
        for label in ('batch-mutated-replay','batch-stale','batch-not-owner','batch-price-limit'):
            p=proofs[label];self.assertNotEqual(p['code'],0);self.assertEqual(p['before'],p['after'])
        self.assertEqual(proofs['batch-mutated-replay']['code'],2)
        p=proofs['batch-exact-replay'];self.assertEqual(p['code'],0);self.assertEqual(p['before'],p['after'])
        p=proofs['batch10'];before=validate(decode(p['before']),META);after=validate(decode(p['after']),META)
        errors=[];audit_batch(p['command'],before,after,p['player'],META,lambda ok,text:errors.append(text) if not ok else None)
        self.assertEqual(errors,[])
        for field in ('gold','invested','cooldown','range'):
            bad=copy.deepcopy(after)
            if field=='gold':bad['players'][p['player']]['gold']+=1
            else:bad['towers'][0][field]+=1
            errors=[];audit_batch(p['command'],before,bad,p['player'],META,lambda ok,text:errors.append(text) if not ok else None)
            self.assertTrue(errors,field)
    def test_negative_batch_scalar_and_unrelated_fields_rejected(self):
        p=next(p for p in G['proofs'] if p['label']=='batch10');raw=bytearray(base64.b64decode(p['wire']))
        # Last canonical price has length+value; corrupt signed high byte and require rejection.
        raw[-1]=255
        with self.assertRaises(ValueError):decode_command(base64.b64encode(raw))
        raw=bytearray(base64.b64decode(p['wire']));raw[16]=1
        with self.assertRaises(ValueError):decode_command(base64.b64encode(raw))
    def test_metadata_version_and_cadence_are_not_relabelled(self):
        validate_version(META);validate_cadence(META)
        for key,value in [('rulesVersion','defense-duell-core-v2.4.1'),('wavePlanVersion','authored-then-exact-endless-v3'),('unitCooldownTicks',[6,8,12,10]),('unitPrices',[18,24,65,38])]:
            m=copy.deepcopy(META);m[key]=value
            with self.assertRaises(ValueError):validate_cadence(m)
        for kind in range(8):
            m=copy.deepcopy(META);m['towerProfiles'][kind]['fireIntervalTicks']+=1
            with self.assertRaises(ValueError):validate_cadence(m)
    def test_mine_exact_floor_sum_and_large_integers(self):
        for n in range(250):self.assertEqual(investment(4,n,{}),sum((2*i*i+27*i+70)//8 for i in range(1,n+1)))
        n=10**100;self.assertGreater(investment(0,n,{}),2**64)
        self.assertEqual(investment(7,3,{}),24+34+44)
    def test_team_shapes_and_endless201_use_real_csharp_codec(self):
        frames={f['name']:f for f in G['frames']}
        for name,count,teams in [('team-TwoVsTwo',4,2),('team-ThreeVsThree',6,2),('team-TwoVsTwoVsTwo',6,3)]:
            s=validate(decode(frames[name]['bytes']),META)
            self.assertEqual((s['playerCount'],s['teamCount']),(count,teams))
        s=validate(decode(frames['endless201-codec-fixture']['bytes']),META)
        self.assertEqual(s['wave'],201);self.assertTrue(s['endless'])
        self.assertTrue(all(len(p['reserved']['values'])==2 for p in s['players']))
    def test_all_real_authority_proofs_replay_and_batch_invariants(self):
        authority=[]
        for p in G['proofs']:
            c=copy.deepcopy(p['command'])
            for key in ('SessionId','Sequence','ExpectedWave'):c[key]=int(c[key])
            authority.append(dict(p,result={'Code':p['code'],'Sequence':c['Sequence']},command=c))
        errors=[]
        _authority({'authority':authority},META,lambda ok,text:errors.append(text) if not ok else None)
        self.assertEqual(errors,[])
    def test_hp_and_ordinal_recipe_forgery_rejected(self):
        state=decode(next(f['bytes'] for f in G['frames'] if f['name']=='elite-and-neutral'))
        for origin in (0,1):
            bad=copy.deepcopy(state);u=next(u for u in bad['units'] if u['origin']==origin);u['hp']=10**200
            with self.assertRaises(ValueError):validate(bad,META)
        bad=copy.deepcopy(state);u=next(u for u in bad['units'] if u['origin']==0);u['kind']=3
        with self.assertRaises(ValueError):validate(bad,META)
        bad=copy.deepcopy(META);bad['neutralRecipes'].append(copy.deepcopy(bad['neutralRecipes'][0]))
        with self.assertRaises(ValueError):validate(state,bad)
    def test_old_or_model_reports_never_prove_transport(self):
        self.assertFalse(compare_duel({'protocol':9},{'protocol':9})['passed'])
        self.assertFalse(compare_team([{'protocol':9}]*4)['passed'])
        self.assertEqual(compare_team([{'protocol':9}]*4)['protocol'],10)

if __name__=='__main__':unittest.main(verbosity=2)
