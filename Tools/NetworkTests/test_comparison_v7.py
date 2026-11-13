"""Fail-closed Wire7 evidence checks. The frozen model fixture never proves actual transport."""
import base64
import copy
import gzip
import json
from pathlib import Path
import unittest
from compare_v7 import compare
from snapshot_v7 import decode

FIXTURE = json.loads(gzip.decompress((Path(__file__).parent / 'Fixtures/wire7-driver-model.json.gz').read_bytes()))

class Wire7EvidenceTests(unittest.TestCase):
    def fixture(self):
        return copy.deepcopy(FIXTURE['host']), copy.deepcopy(FIXTURE['client'])
    def rejected(self,h,c):
        self.assertFalse(compare(h,c,True,True,allow_model=True)['passed'])
    def test_complete_model_requires_explicit_model_scope(self):
        h,c=self.fixture()
        self.assertTrue(compare(h,c,True,True,allow_model=True)['passed'])
        self.rejected_real = compare(h,c,True,True)
        self.assertFalse(self.rejected_real['passed'])
    def test_old_wire_rejected(self):
        for version in range(7):
            h,c=self.fixture();h['protocol']=c['protocol']=version;self.rejected(h,c)
    def test_missing_result_or_same_epoch_rejected(self):
        h,c=self.fixture();c['cycles'][0]['rounds'][1]['epoch']=1000;self.rejected(h,c)
        h,c=self.fixture();c['cycles'][0]['rounds'][0]['resultDigest']='0'*64;self.rejected(h,c)
    def test_research_quote_mutation_rejected(self):
        h,c=self.fixture()
        proof=next(p for p in h['cycles'][0]['rounds'][0]['authority'] if p['command']['Type']==8 and p['result']['Code']==0)
        proof['command']['ExpectedResearch']='999999999999999999999999999999';self.rejected(h,c)
    def test_paid_fifo_cannot_pass_from_ack_alone(self):
        h,c=self.fixture();frames=c['cycles'][0]['rounds'][0]['frames']
        before=next(f for f in frames if f['name']=='rapid-before');after=next(f for f in frames if f['name']=='rapid-after')
        for key in ('bytes','hash','tick','playTicks'):after[key]=before[key]
        self.rejected(h,c)
    def test_accepted_command_requires_canonical_mutation(self):
        h,c=self.fixture();proof=next(p for p in h['cycles'][0]['rounds'][0]['authority'] if p['command']['Type']==3 and p['result']['Code']==0)
        proof['after']=proof['before'];self.rejected(h,c)
    def test_noncanonical_big_integer_rejected(self):
        h,_=self.fixture();data=bytearray(base64.b64decode(h['cycles'][0]['rounds'][0]['frames'][0]['bytes']))
        n=int.from_bytes(data[57:61],'little',signed=True)
        data[57:61]=(n+1).to_bytes(4,'little');data.insert(61+n,0)
        with self.assertRaises(ValueError):decode(base64.b64encode(data))
    def test_truncated_or_trailing_state_rejected(self):
        h,_=self.fixture();data=base64.b64decode(h['cycles'][0]['rounds'][0]['frames'][0]['bytes'])
        for broken in (data[:-1],data+b'\0'):
            with self.assertRaises(ValueError):decode(base64.b64encode(broken))

if __name__=='__main__':unittest.main()
