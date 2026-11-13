"""Economy2.1 evidence regressions. Frozen driver models are never real transport proof."""
import base64
import copy
import gzip
import json
from pathlib import Path
import unittest
from compare_v7 import compare
from snapshot_v7 import decode

HERE=Path(__file__).parent
FIXTURE=json.loads(gzip.decompress((HERE/'Fixtures/economy11-driver-model.json.gz').read_bytes()))

class Economy11EvidenceTests(unittest.TestCase):
    def fixture(self):return copy.deepcopy(FIXTURE['host']),copy.deepcopy(FIXTURE['client'])
    def rejected(self,h,c,reason=None):
        result=compare(h,c,True,True,allow_model=True,require_economy11=True)
        self.assertFalse(result['passed'],result)
        if reason:self.assertTrue(any(reason in p for p in result['problems']),result)
    def sale(self,h,replay=False):
        candidates=[]
        for p in h['cycles'][0]['rounds'][0]['authority']:
            if p['player']!=0 or p['command']['Type']!=2 or p['result']['Code']!=0:continue
            before=decode(p['before']);target=p['command']['Target']
            if any(t['id']==target and t['kind']==4 for t in before['towers']):candidates.append(p)
            elif replay and candidates and p['command']==candidates[0]['command']:return p
        return candidates[0]
    def test_complete_model_validates_new_mine_path_but_never_proves_transport(self):
        h,c=self.fixture();self.assertTrue(compare(h,c,True,True,allow_model=True,require_economy11=True)['passed'])
        self.assertFalse(compare(h,c,True,True,require_economy11=True)['passed'])
    def test_old_wire7_evidence_cannot_qualify_as_economy11(self):
        legacy=json.loads(gzip.decompress((HERE/'Fixtures/wire7-driver-model.json.gz').read_bytes()))
        self.rejected(legacy['host'],legacy['client'],'metadata required')
    def test_missing_production_metadata_fails(self):
        for field in ('acceptanceSchema','rulesVersion','refundPercents','unitCooldownTicks','sendWindowTicksByWave','passiveGoldPerSecond','incomeUnitsPerGold'):
            h,c=self.fixture();h.pop(field,None);c.pop(field,None);self.rejected(h,c)
    def test_wrong_class_refund_and_price_fail(self):
        h,c=self.fixture();h['refundPercents']=c['refundPercents']=[65]*7;self.rejected(h,c,'refund')
        h,c=self.fixture();h['towerPrices'][4]=c['towerPrices'][4]=140;self.rejected(h,c,'mine')
    def test_wrong_departure_units_or_old_send_window_fail(self):
        h,c=self.fixture();h['unitCooldownTicks']=c['unitCooldownTicks']=[240,400,600,480];self.rejected(h,c,'cooldown')
        h,c=self.fixture();h['sendWindowTicksByWave']=c['sendWindowTicksByWave']=[240]*10;self.rejected(h,c,'send window')
    def test_missing_sale_rebuild_or_replay_frames_fail(self):
        for name in ('mine-sale-before','mine-sale-after','mine-rebuild-after','mine-sale-replay'):
            h,c=self.fixture();frames=c['cycles'][0]['rounds'][0]['frames'];frames[:]=[f for f in frames if f['name']!=name];self.rejected(h,c,'missing authoritative frame')
    def test_same_rebuilt_entity_or_wrong_refund_summary_fails(self):
        h,c=self.fixture();r=h['cycles'][0]['rounds'][0];r['mineRebuiltId']=r['mineSoldId'];self.rejected(h,c,'identity')
        h,c=self.fixture();h['cycles'][0]['rounds'][0]['mineRefundGold']='104';self.rejected(h,c,'refund summary')
    def test_mine_sale_authority_must_credit_exact40percent(self):
        h,c=self.fixture();sale=self.sale(h);before=decode(sale['before']);wrong=before['players'][0]['gold']+104
        raw=bytearray(base64.b64decode(sale['after']));size=int.from_bytes(raw[57:61],'little',signed=True)
        encoded=wrong.to_bytes((wrong.bit_length()+8)//8,'little',signed=True)
        raw[57:61+size]=len(encoded).to_bytes(4,'little',signed=True)+encoded;sale['after']=base64.b64encode(raw).decode()
        self.rejected(h,c,'sale refund')
    def test_duplicate_sale_requires_host_replay_receipt(self):
        h,c=self.fixture();replay=self.sale(h,True);h['cycles'][0]['rounds'][0]['authority'].remove(replay);self.rejected(h,c,'original/replay')
    def test_ack_without_replicated_rebuild_fails(self):
        h,c=self.fixture();frames=c['cycles'][0]['rounds'][0]['frames'];before=next(f for f in frames if f['name']=='mine-sale-after');after=next(f for f in frames if f['name']=='mine-rebuild-after')
        for key in ('bytes','hash','tick','playTicks'):after[key]=before[key]
        self.rejected(h,c)
    def test_mine_income_metadata_is_checked_against_actual_clocks(self):
        h,c=self.fixture();frame=next(f for f in h['cycles'][0]['rounds'][0]['frames'] if f['name']=='speed-one-start')
        mine_id=next(t['id'] for t in decode(frame['bytes'])['towers'] if t['owner']==0 and t['kind']==4)
        next(t for t in frame['towers'] if t['id']==mine_id)['incomeUnits']='1920';self.rejected(h,c,'income did not follow')

if __name__=='__main__':unittest.main()
