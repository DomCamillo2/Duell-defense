"""Completed clock evidence survives an unfinished retry without weakening validation."""
import copy
import gzip
import json
from pathlib import Path
import unittest
from compare_v7 import compare
FIXTURE=json.loads(gzip.decompress((Path(__file__).parent/'Fixtures/economy11-driver-model.json.gz').read_bytes()))
class CompletedClockTests(unittest.TestCase):
    def fixture(self):
        h,c=copy.deepcopy(FIXTURE['host']),copy.deepcopy(FIXTURE['client'])
        clocks=c['cycles'][0]['rounds'][0]['clocks']
        good=next(x for x in clocks if x['name']=='speed-one')
        pending=copy.deepcopy(good);pending.update(complete=False,seconds=0,controlEnd=0,playEnd=0,controlsPerSecond=0,gameplayPerSecond=0)
        clocks.append(pending)
        return h,c,clocks,good
    def result(self,h,c):return compare(h,c,True,True,allow_model=True,require_economy11=True)
    def test_unfinished_later_window_does_not_replace_completed_measurement(self):
        h,c,_,_=self.fixture();self.assertTrue(self.result(h,c)['passed'])
    def test_only_unfinished_window_is_not_evidence(self):
        h,c,clocks,good=self.fixture();clocks.remove(good);r=self.result(h,c)
        self.assertFalse(r['passed']);self.assertTrue(any('missing completed speed window' in x for x in r['problems']))
    def test_missing_entire_window_fails(self):
        h,c,clocks,_=self.fixture();clocks[:]=[x for x in clocks if x['name']!='speed-one'];self.assertFalse(self.result(h,c)['passed'])
    def test_bad_completed_rate_not_hidden_by_good_duplicate(self):
        h,c,clocks,good=self.fixture();clocks.append(copy.deepcopy(good));good['controlsPerSecond']=0;self.assertFalse(self.result(h,c)['passed'])
    def test_bad_completed_arithmetic_not_hidden_by_good_duplicate(self):
        h,c,clocks,good=self.fixture();clocks.append(copy.deepcopy(good));good['controlEnd']+=10;self.assertFalse(self.result(h,c)['passed'])
    def test_too_short_completed_window_still_fails(self):
        h,c,_,good=self.fixture();good['seconds']=1;self.assertFalse(self.result(h,c)['passed'])
    def test_wrong_completed_speed_still_fails(self):
        h,c,_,good=self.fixture();good['speed']=2;self.assertFalse(self.result(h,c)['passed'])
if __name__=='__main__':unittest.main()
