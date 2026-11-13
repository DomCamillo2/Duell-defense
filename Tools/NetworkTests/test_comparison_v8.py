"""Synthetic Wire8 evidence regression; these fixtures NEVER assert a live transport run."""
import argparse
import base64
import copy
import gzip
import hashlib
import json
from pathlib import Path
import struct
import unittest
from compare_v8 import compare
from snapshot_v8 import decode, validate, exact, MAX_GOLD_BYTES
from run_lan import compare as dispatch, exact_start_gold, add_version_arguments, rules_arguments


def big(value):
    n=max(1,(value.bit_length()+8)//8)
    return struct.pack('<i',n)+value.to_bytes(n,'little',signed=True)


def state(epoch=1,waves=10,gold=180):
    return dict(epoch=epoch,seed=71,tick=0,play=0,countdown=0,phase=0,outcome=0,wavePhase=0,wave=1,preparation=200,waveStart=0,
                ruleWaves=waves,ruleStartGold=gold,configRevision=0,rosterRevision=2 if epoch==1 else 0,connectedMask=3,startGoldGranted=0,
                players=[dict(gold=0,hp=30,ready=0,rematch=0,speed=0,waveReady=0,mask=mask) for mask in (63,127)])


def encoded(s):
    data=bytearray(struct.pack('<QIiiiBBBiii',*[s[k] for k in ('epoch','seed','tick','play','countdown','phase','outcome','wavePhase','wave','preparation','waveStart')]))
    data+=struct.pack('<iiBiiB',0,0,0,0,0,0)
    for p in s['players']:
        data+=big(p['gold'])+struct.pack('<iBBBBi',*[p[k] for k in ('hp','ready','rematch','speed','waveReady','mask')])
        data+=struct.pack('<iiii',0,0,0,0)+big(0)*4+struct.pack('<i',0)+big(0)+struct.pack('<B',s['ruleWaves'])
        data+=big(0)*2*s['ruleWaves']
    data+=struct.pack('<iii',0,0,0)
    trailer=struct.pack('<i',s['ruleWaves'])+big(s['ruleStartGold'])+struct.pack('<QQiB',s['configRevision'],s['rosterRevision'],s['connectedMask'],s['startGoldGranted'])
    data+=trailer+struct.pack('<i',len(trailer))
    return base64.b64encode(data).decode()


def frame(name,s,seq):
    raw=encoded(s)
    return dict(name=name,bytes=raw,hash=hashlib.sha256(base64.b64decode(raw)).hexdigest(),epoch=s['epoch'],tick=s['tick'],playTicks=s['play'],wall=s['tick']/20,publicationSequence=str(seq),towers=[])


def fixture(gold=0, waves=1, premature=False):
    # Only unchanged catalog metadata is reused. Every Wire8 frame/proof below is newly constructed.
    old=json.loads(gzip.decompress((Path(__file__).parent/'Fixtures/wire7-driver-model.json.gz').read_bytes()))['host']
    keys=('expectedBaseHp','tickRate','preparationTicks','waveBudget','unitPrices','unitCounts','researchPrices','towerPrices','towerProfiles')
    metadata={k:copy.deepcopy(old[k]) for k in keys}
    metadata.update(protocol=8,acceptanceSchema=3,rulesVersion='defense-duell-core-v2.2',wavePlanVersion='authored-prefix-only-v1',
                    hostRules=True,configuredWaves=waves,configuredStartGold=str(gold),waveCount=waves,expectedStartGold=str(gold),
                    refundPercents=[65,65,65,65,40,65,65],unitCooldownTicks=[12,20,30,24],sendWindowTicksByWave=[600]*waves,
                    passiveGoldPerSecond=4,incomeUnitsPerGold=960,platform='PureDotnetModel',unityVersion='PURE_SYNTHETIC_WIRE8',
                    role='host',mode='lan',passed=True,completeMatches=True,rapidBuy=False,requestedCycles=1,requestedMapIndex=0,
                    catalogHash='a'*64,baseContentHash='b'*64,contentHash='c'*64)
    metadata['towerPrices'][4]=metadata['towerProfiles'][4]['price']=160
    host=copy.deepcopy(metadata);client=copy.deepcopy(metadata);client['role']='client'
    cycle=dict(cycle=1,mapIndex=0,initialMapIndex=0,connected=True,cleanShutdown=True,disconnectObserved=True,failure='',sentBytes=1024,receivedBytes=1024,finalContentHash=host['contentHash'],checkpoints=[],rounds=[])
    host['cycles']=[copy.deepcopy(cycle)];client['cycles']=[copy.deepcopy(cycle)]
    for number in (1,2):
        s=state(number,10 if number==1 else waves,180 if number==1 else gold)
        initial=copy.deepcopy(s);authority=[];acks=[[],[]];sequence=0
        def command(player,kind,code=0,mutate=None,**overrides):
            nonlocal s,sequence
            sequence+=1
            c=dict(SessionId=number,Sequence=sequence,Type=kind,Kind=0,Target=-1,PositionX=0,PositionY=0,ExpectedWave=0,ExpectedResearch='0',
                   ExpectedConfigRevision='0',ExpectedRosterRevision='0',RuleWaveCount=0,RuleStartGold='0')
            c.update(overrides);before=encoded(s)
            if mutate:mutate(s)
            result=dict(Sequence=sequence,Tick=s['tick'],Code=code)
            authority.append(dict(player=player,command=c,result=result,before=before,after=encoded(s)))
            acks[player].append(dict(epoch=number,sequence=sequence,tick=s['tick'],code=code,publicationSequence=str(sequence+1),label='synthetic'))
        def ready(player,code=0,config=None):
            def change(t):t['players'][player]['ready']=1
            command(player,4,code,change if not code else None,ExpectedConfigRevision=str(s['configRevision'] if config is None else config),ExpectedRosterRevision=str(s['rosterRevision']))
        ready(1)
        command(1,10,8,RuleWaveCount=1,RuleStartGold=str(gold))
        command(0,10,27,RuleWaveCount=0,RuleStartGold=str(gold))
        command(0,10,RuleWaveCount=s['ruleWaves'],RuleStartGold=str(s['ruleStartGold']))
        def apply(waves,value):
            prior=s['configRevision']
            def change(t):
                t.update(ruleWaves=waves,ruleStartGold=value,configRevision=prior+1)
                for p in t['players']:p['ready']=0
            command(0,10,mutate=change,RuleWaveCount=waves,RuleStartGold=str(value),ExpectedConfigRevision=str(prior))
        apply(2 if waves!=2 else 1,gold);ready(1,24,config=0);ready(1);apply(waves,gold)
        configured=copy.deepcopy(s);ready(1)
        def start(t):
            t['players'][0]['ready']=1;t.update(phase=1,countdown=60,startGoldGranted=1)
            for p in t['players']:p['gold']=gold
        command(0,4,mutate=start,ExpectedConfigRevision=str(s['configRevision']),ExpectedRosterRevision=str(s['rosterRevision']))
        started=copy.deepcopy(s)
        s.update(phase=3,outcome=2 if premature else 3,tick=900,play=600,countdown=0,wavePhase=2,preparation=0,wave=1 if premature else waves)
        for p in s['players']:p['hp']=24;p['gold']=gold+120
        if premature:s['players'][0]['hp']=0
        final=copy.deepcopy(s)
        entry=dict(number=number,epoch=number,resultObserved=True,resultDigest=frame('final',final,30)['hash'],resultTick=900,playTicks=600,
                   resultWave=1 if premature else waves,resultHp0=0 if premature else 24,resultHp1=24,outcome='Player1' if premature else 'Draw',rematchRequested=number==1,rematchAccepted=number==1,authority=authority,
                   frames=[frame('initial',initial,1),frame('configured',configured,9),frame('start-granted',started,12),frame('final',final,30)])
        if number==1:
            command(0,5,mutate=lambda t:t['players'][0].update(rematch=1))
            command(1,5,mutate=lambda t:(t.clear(),t.update(state(2,waves,gold))))
        for player,report in enumerate((host,client)):
            item=copy.deepcopy(entry);item['acks']=copy.deepcopy(acks[player])
            if player:item['authority']=[]
            report['cycles'][0]['rounds'].append(item)
            report['cycles'][0]['checkpoints'] += [dict(epoch=f['epoch'],tick=f['tick'],hash=f['hash'],publicationSequence=f['publicationSequence']) for f in item['frames']]
    return host,client


def startup_fixture(waves=200,gold=270):
    h,c=fixture(gold=gold,waves=waves)
    for report in (h,c):
        report.update(rulesVersion='defense-duell-core-v2.3',wavePlanVersion='authored-then-linear-v2',
                      hostRulesStartOnly=True,completeMatches=False,rapidBuy=False)
        cycle=report['cycles'][0];entry=cycle['rounds'][0];cycle['rounds']=[entry]
        for key in ('resultDigest','resultTick','resultWave','resultHp0','resultHp1','outcome','playTicks'):
            entry.pop(key,None)
        entry.update(resultObserved=False,rematchRequested=False,rematchAccepted=False,startOnlyObserved=True,startedSmokeSeconds=3)
        entry['authority']=[p for p in entry['authority'] if p['command']['Type'] in (4,10)]
        entry['acks']=[a for a in entry['acks'] if a['sequence']<=10]
        entry['frames']=[f for f in entry['frames'] if f['name']!='final']
        started=entry['frames'][-1];smoke=decode(started['bytes']);smoke.update(tick=60,countdown=0,phase=2)
        entry['frames'].append(frame('started-smoke',smoke,15))
        cycle['checkpoints']=[dict(epoch=f['epoch'],tick=f['tick'],hash=f['hash'],publicationSequence=f['publicationSequence']) for f in entry['frames'][:-1]]
        for tick,seq in ((20,13),(40,14),(60,15)):
            snap=copy.deepcopy(smoke);snap['tick']=tick;f=frame('observed',snap,seq)
            cycle['checkpoints'].append(dict(epoch=f['epoch'],tick=f['tick'],hash=f['hash'],publicationSequence=f['publicationSequence']))
    return h,c


# Actual C# encoder sample, not a transport fixture. WireProtocol.cs SHA-256 at generation:
# cb292193c96152d1f1e4ac6a4b904c61bafe991799c139b5c67bbefdfae0988f
CSHARP_GOLDEN = 'AQAAAAAAAAABAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAAeAAAAAAAAAB8AAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAABAAAAAAEAAAAAAQAAAAAAAAAAAQAAAAADAQAAAAABAAAAAAEAAAAAAQAAAAABAAAAAAEAAAAAAQAAAAAeAAAAAAAAAB8AAAAAAAAAAAAAAAAAAAAAAAAAAQAAAAABAAAAAAEAAAAAAQAAAAAAAAAAAQAAAAADAQAAAAABAAAAAAEAAAAAAQAAAAABAAAAAAEAAAAAAAAAAAAAAAAAAAAAAwAAABEAAAB7AAAAAGH1uau/pFzD8SljHQEAAAAAAAAAAAAAAAAAAAADAAAAAC4AAAA='


class Wire8EvidenceTests(unittest.TestCase):
    def accepted(self,h,c):
        result=compare(h,c,allow_model=True,require_host_rules=True)
        self.assertTrue(result['passed'],result['problems']);return result
    def rejected(self,h,c):
        result=compare(h,c,allow_model=True,require_host_rules=True)
        self.assertFalse(result['passed']);return result
    def test_two_hundred_wave_startup_is_explicitly_not_completion(self):
        h,c=startup_fixture();result=self.accepted(h,c)
        self.assertTrue(result['startup_only_proven']);self.assertFalse(result['last_wave_proven'])
        self.assertIn('STARTUP ONLY',result['scope']);self.assertEqual(len(result['rounds']),1)
        self.assertFalse(compare(h,c,require_host_rules=True,expected_start_only=True)['passed'])
    def test_two_hundred_wave_ledger_and_huge_gold_are_exact(self):
        h,c=startup_fixture(gold=10**70+83);self.accepted(h,c)
        s=validate(decode(h['cycles'][0]['rounds'][0]['frames'][-1]['bytes']),h)
        self.assertEqual(s['ruleWaves'],200);self.assertEqual(len(s['players'][0]['reserved']),200)
        self.assertEqual(s['players'][0]['gold'],10**70+83)
    def test_wave_201_is_rejected_by_reader_and_runner(self):
        with self.assertRaises(ValueError):decode(encoded(state(waves=201)))
        parser=argparse.ArgumentParser();add_version_arguments(parser)
        import contextlib,io
        with contextlib.redirect_stderr(io.StringIO()),self.assertRaises(SystemExit):parser.parse_args(['--waves','201'])
    def test_old_plan_cannot_claim_two_hundred_waves(self):
        h,c=startup_fixture()
        for report in (h,c):report.update(rulesVersion='defense-duell-core-v2.2',wavePlanVersion='authored-prefix-only-v1')
        self.rejected(h,c)
    def test_only_explicit_core_plan_pairs_are_accepted(self):
        for version,plan in [('defense-duell-core-v2.2','authored-then-linear-v2'),('defense-duell-core-v2.3','authored-prefix-only-v1'),('defense-duell-core-v2.4','authored-then-linear-v2')]:
            h,c=startup_fixture()
            for report in (h,c):report.update(rulesVersion=version,wavePlanVersion=plan)
            self.rejected(h,c)
    def test_startup_report_cannot_satisfy_requested_full_game(self):
        h,c=startup_fixture()
        self.assertFalse(compare(h,c,allow_model=True,require_complete=True)['passed'])
        self.assertFalse(compare(h,c,allow_model=True,expected_start_only=False)['passed'])
        for report in (h,c):report['hostRulesStartOnly']=False
        self.rejected(h,c)
    def test_startup_flag_must_agree_between_peers(self):
        h,c=startup_fixture();c.pop('hostRulesStartOnly');self.rejected(h,c)
    def test_startup_needs_measured_smoke_not_only_ready_ack(self):
        for seconds in (2.9,float('inf'),float('nan'),True,100):
            h,c=startup_fixture();c['cycles'][0]['rounds'][0]['startedSmokeSeconds']=seconds;self.rejected(h,c)
        h,c=startup_fixture();c['cycles'][0]['rounds'][0]['frames'].pop();self.rejected(h,c)
    def test_startup_cannot_claim_a_final_wave_or_rematch(self):
        for key,value in [('resultObserved',True),('rematchRequested',True),('resultWave',200),('resultDigest','a'*64)]:
            h,c=startup_fixture();c['cycles'][0]['rounds'][0][key]=value;self.rejected(h,c)
    def test_startup_requires_shared_post_grant_publications(self):
        h,c=startup_fixture();c['cycles'][0]['checkpoints']=c['cycles'][0]['checkpoints'][:3];self.rejected(h,c)
    def test_startup_requires_clean_shutdown(self):
        h,c=startup_fixture();c['cycles'][0]['cleanShutdown']=False;self.rejected(h,c)
    def test_startup_cannot_skip_ready_reset_probes(self):
        h,c=startup_fixture();r=h['cycles'][0]['rounds'][0];r['authority']=[p for p in r['authority'] if p['result']['Code']!=24];self.rejected(h,c)
    def test_cli_startup_flag_and_200_are_forwarded_without_full_game(self):
        p=argparse.ArgumentParser();add_version_arguments(p);args=p.parse_args(['--host-rules-start-only','--waves','200','--start-gold','270'])
        flags=rules_arguments(args,p);self.assertIn('--duel-host-rules-start-only',flags);self.assertIn('200',flags)
        self.assertTrue(args.host_rules)
        import contextlib,io
        args.complete_matches=True
        with contextlib.redirect_stderr(io.StringIO()),self.assertRaises(SystemExit):rules_arguments(args,p)
    def test_actual_csharp_wire8_golden_matches_independent_reader(self):
        h,_=fixture();s=validate(decode(CSHARP_GOLDEN),h)
        self.assertEqual((s['ruleWaves'],s['ruleStartGold'],s['configRevision'],s['rosterRevision']),(3,10**40+123,1,0))
        self.assertEqual(s['startGoldGranted'],0);self.assertEqual(len(s['players'][0]['reserved']),3)
        self.assertTrue(all(p['gold']==0 for p in s['players']))
    def test_new_zero_gold_one_wave_synthetic_flow(self):
        h,c=fixture();self.assertTrue(self.accepted(h,c)['last_wave_proven'])
        self.assertFalse(compare(h,c,require_host_rules=True)['passed'])
    def test_big_integer_start_has_no_float_or_int64_conversion(self):
        h,c=fixture(10**90+137);self.accepted(h,c)
        self.assertTrue(dispatch(h,c,allow_model=True,require_host_rules=True,expected_start_gold=str(10**90+137),expected_waves=1)['passed'])
    def test_early_base_death_is_not_configured_last_wave_proof(self):
        h,c=fixture(waves=3,premature=True)
        result=self.rejected(h,c);self.assertFalse(result['last_wave_proven'])
        diagnostic=compare(h,c,allow_model=True,require_host_rules=True,require_final_wave=False)
        self.assertTrue(diagnostic['passed'],diagnostic['problems']);self.assertFalse(diagnostic['last_wave_proven'])
    def test_missing_confirmed_lobby_frame_rejected(self):
        h,c=fixture();r=c['cycles'][0]['rounds'][0];r['frames']=[f for f in r['frames'] if f['name']!='configured'];self.rejected(h,c)
    def test_cli_metadata_must_match_snapshot_configuration(self):
        h,c=fixture();h['configuredStartGold']=c['configuredStartGold']='1';self.rejected(h,c)
    def test_wrong_requested_options_rejected(self):
        h,c=fixture();self.assertFalse(compare(h,c,allow_model=True,expected_start_gold='1')['passed'])
        self.assertFalse(compare(h,c,allow_model=True,expected_waves=2)['passed'])
    def test_old_report_cannot_be_relabelled_as_actual_wire8(self):
        old=json.loads(gzip.decompress((Path(__file__).parent/'Fixtures/wire7-driver-model.json.gz').read_bytes()))
        self.assertTrue(dispatch(old['host'],old['client'],True,True,allow_model=True)['passed'])
        self.assertFalse(dispatch(old['host'],old['client'],allow_model=True,require_host_rules=True)['passed'])
        old['host']['protocol']=old['client']['protocol']=8
        self.assertFalse(compare(old['host'],old['client'],allow_model=True)['passed'])
    def test_float_bool_and_noncanonical_gold_rejected(self):
        for value in (0.0,True,'00','+1','1e20',' 1','١'):
            h,c=fixture();h['expectedStartGold']=c['expectedStartGold']=value;self.rejected(h,c)
    def test_guest_apply_must_reject(self):
        h,c=fixture();p=next(p for p in h['cycles'][0]['rounds'][0]['authority'] if p['result']['Code']==8);p['result']['Code']=0;self.rejected(h,c)
    def test_invalid_apply_evidence_cannot_be_omitted(self):
        h,c=fixture();r=h['cycles'][0]['rounds'][0];r['authority']=[p for p in r['authority'] if p['result']['Code']!=27];self.rejected(h,c)
    def test_stale_ready_must_bind_an_old_revision(self):
        h,c=fixture();p=next(p for p in h['cycles'][0]['rounds'][0]['authority'] if p['result']['Code']==24);p['command']['ExpectedConfigRevision']='1';self.rejected(h,c)
    def test_ready_cannot_grant_wrong_gold(self):
        h,c=fixture();p=next(p for p in h['cycles'][0]['rounds'][0]['authority'] if p['command']['Type']==4 and decode(p['after'])['startGoldGranted']);p['after']=p['before'];self.rejected(h,c)
    def test_effective_change_requires_revision_increment(self):
        h,c=fixture();p=next(p for p in h['cycles'][0]['rounds'][0]['authority'] if p['command']['Type']==10 and p['result']['Code']==0 and decode(p['after'])['configRevision']==1);p['command']['ExpectedConfigRevision']='9';self.rejected(h,c)
    def test_noop_cannot_mutate_state(self):
        h,c=fixture();p=next(p for p in h['cycles'][0]['rounds'][0]['authority'] if p['command']['Type']==10 and p['result']['Code']==0);p['after']=h['cycles'][0]['rounds'][0]['authority'][0]['before'];self.rejected(h,c)
    def test_missing_start_frame_rejected(self):
        h,c=fixture();r=c['cycles'][0]['rounds'][0];r['frames']=[f for f in r['frames'] if f['name']!='start-granted'];self.rejected(h,c)
    def test_conflicting_same_publication_rejected(self):
        h,c=fixture();c['cycles'][0]['checkpoints'][0]['hash']='d'*64;self.rejected(h,c)
    def test_old_epoch_publication_cannot_reappear_after_rematch(self):
        h,c=fixture();c['cycles'][0]['checkpoints'].append(copy.deepcopy(c['cycles'][0]['checkpoints'][0]));self.rejected(h,c)
    def test_reordered_publication_rejected(self):
        h,c=fixture();cp=c['cycles'][0]['checkpoints'];cp[1],cp[2]=cp[2],cp[1];self.rejected(h,c)
    def test_ack_publication_is_exact_and_present(self):
        for value in (0,1.0,True,'01'):
            h,c=fixture();c['cycles'][0]['rounds'][0]['acks'][0]['publicationSequence']=value;self.rejected(h,c)
    def test_float_protocol_cannot_select_an_integer_version(self):
        h,c=fixture();h['protocol']=c['protocol']=8.0;self.rejected(h,c)
        self.assertFalse(dispatch(h,c,allow_model=True)['passed'])
    def test_publication_float_rejected(self):
        h,c=fixture();c['cycles'][0]['checkpoints'][0]['publicationSequence']=1.0;self.rejected(h,c)
    def test_final_revision_cannot_regress_even_with_rebound_hashes(self):
        h,c=fixture()
        for report in (h,c):
            entry=report['cycles'][0]['rounds'][0];old=entry['frames'][-1];s=decode(old['bytes']);s['configRevision']-=1
            replacement=frame('final',s,int(old['publicationSequence']));entry['frames'][-1]=replacement;entry['resultDigest']=replacement['hash']
            for cp in report['cycles'][0]['checkpoints']:
                if cp['epoch']==1 and cp['publicationSequence']==old['publicationSequence']:cp['hash']=replacement['hash']
        self.rejected(h,c)
    def test_rematch_must_retain_epoch_and_options(self):
        h,c=fixture();c['cycles'][0]['rounds'][1]['epoch']=1;self.rejected(h,c)
    def test_missing_peer_ack_authority_rejected(self):
        h,c=fixture();c['cycles'][0]['rounds'][0]['acks'][0]['sequence']=999;self.rejected(h,c)
    def test_result_digest_bound_to_trailer_too(self):
        h,c=fixture();c['cycles'][0]['rounds'][0]['resultDigest']='e'*64;self.rejected(h,c)
    def test_wrong_protocol_dispatch_rejected(self):
        h,c=fixture();self.assertFalse(dispatch(h,c,protocol='7',allow_model=True)['passed'])
        c['protocol']=7;self.assertFalse(dispatch(h,c,allow_model=True)['passed'])
    def test_relay_requires_distinct_real_profiles(self):
        h,c=fixture();h['mode']=c['mode']='relay-dtls';self.rejected(h,c)
    def test_reader_rejects_truncation_and_trailing_data(self):
        raw=base64.b64decode(encoded(state()))
        for data in (raw[:-1],raw+b'\0',raw[:-4]+struct.pack('<i',-1)):
            with self.assertRaises(ValueError):decode(base64.b64encode(data))
    def test_noncanonical_trailer_gold_rejected(self):
        raw=bytearray(base64.b64decode(encoded(state(gold=1))));length=struct.unpack_from('<i',raw,len(raw)-4)[0];start=len(raw)-4-length
        struct.pack_into('<i',raw,start+4,2);raw.insert(start+9,0);struct.pack_into('<i',raw,len(raw)-4,length+1)
        with self.assertRaises(ValueError):decode(base64.b64encode(raw))
    def test_lobby_cannot_hold_starting_gold(self):
        s=state();s['players'][0]['gold']=180;h,_=fixture()
        with self.assertRaisesRegex(ValueError,'lobby granted'):validate(decode(encoded(s)),h)
    def test_configured_ledger_prefix_required(self):
        raw=bytearray(base64.b64decode(encoded(state())));length=struct.unpack_from('<i',raw,len(raw)-4)[0]
        struct.pack_into('<i',raw,len(raw)-4-length,1);h,_=fixture()
        with self.assertRaisesRegex(ValueError,'ledger'):validate(decode(base64.b64encode(raw)),h)
    def test_cli_preserves_large_decimal_and_infers_mode(self):
        p=argparse.ArgumentParser();add_version_arguments(p);gold=str(10**90+137)
        args=p.parse_args(['--waves','1','--start-gold',gold]);flags=rules_arguments(args,p)
        self.assertIn(gold,flags);self.assertTrue(args.host_rules);self.assertIn('--duel-host-rules',flags)
    def test_cli_rejects_rounding_and_negative_values(self):
        for value in ('1.1','-1','1e20','01','١'):
            with self.assertRaises(argparse.ArgumentTypeError):exact_start_gold(value)
    def test_exact_parser_rejects_float_bool_and_uint64_overflow(self):
        for value in (1.0,True,'01',str(2**64)):
            with self.assertRaises(ValueError):exact(value,bits=64)
    def test_cli_accepts_maximum_gold_magnitude_and_rejects_next_bit(self):
        value=str((1<<(MAX_GOLD_BYTES*8))-1)
        self.assertEqual(exact_start_gold(value),value)
        with self.assertRaises(argparse.ArgumentTypeError):exact_start_gold(str(1<<(MAX_GOLD_BYTES*8)))

if __name__=='__main__':unittest.main()
