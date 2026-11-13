"""Version dispatch tests only: never starts owned or user transport processes."""
import argparse
import copy
import unittest
from unittest.mock import patch,Mock
import run_lan
import run_relay
import run_team_v9


def report(protocol=10,core=None):
    return dict(protocol=protocol,rulesVersion=core or ('defense-duell-core-v2.5' if protocol==10 else 'defense-duell-core-v2.4'),wavePlanVersion='authored-then-counter-profiles-boss10-v4' if protocol==10 else 'authored-then-exact-endless-v3')

class Dispatch16Tests(unittest.TestCase):
    def test_actual_wire10_duel_and_relay_choose_new_comparator(self):
        import compare_duel_v10
        self.assertIs(run_lan.select_report_comparator([report(),report()]),compare_duel_v10.compare)
        self.assertIs(run_relay.compare,run_lan.compare)
    def test_actual_wire9_remains_historical(self):
        import compare_duel_v9
        for core in ('defense-duell-core-v2.4','defense-duell-core-v2.4.1'):
            self.assertIs(run_lan.select_report_comparator([report(9,core),report(9,core)]),compare_duel_v9.compare)
    def test_wire7_and_wire8_retained(self):
        import compare_v7,compare_v8
        for n,fn in [(7,compare_v7.compare),(8,compare_v8.compare)]:
            self.assertIs(run_lan.select_report_comparator([dict(protocol=n),dict(protocol=n)]),fn)
    def test_team_four_and_six_select_actual_wire_version(self):
        import compare_team_v9,compare_team_v10
        for count in (4,6):
            for n,fn in [(9,compare_team_v9.compare),(10,compare_team_v10.compare)]:
                self.assertIs(run_lan.select_report_comparator([report(n) for _ in range(count)],team=True),fn)
        self.assertIs(run_team_v9.compare,run_lan.compare_team)
    def test_mixed_or_unknown_protocol_rejected_without_comparator(self):
        for pair in [(report(9),report(10)),(report(11),report(11)),({},{}),(dict(protocol=True),dict(protocol=True))]:
            with patch('importlib.import_module') as imp:
                result=run_lan.compare(*pair)
                self.assertFalse(result['passed']);self.assertFalse(result['actual_transport_proven']);imp.assert_not_called()
    def test_core_or_plan_mislabel_rejected(self):
        pairs=[(report(10,'defense-duell-core-v2.4'),report(10,'defense-duell-core-v2.4')),(report(9,'defense-duell-core-v2.5'),report(9,'defense-duell-core-v2.5')),(report(9),report(9,'defense-duell-core-v2.4.1'))]
        wrong=report();wrong['wavePlanVersion']='authored-then-exact-endless-v3';pairs.append((wrong,wrong))
        for pair in pairs:self.assertFalse(run_lan.compare(*pair)['passed'])
    def test_requested_protocol_cannot_relabel(self):
        for actual,requested in [(10,'9'),(9,'10')]:
            with patch('importlib.import_module') as imp:
                self.assertFalse(run_lan.compare(report(actual),report(actual),protocol=requested)['passed']);imp.assert_not_called()
    def test_selector_accepts_actual_matching_explicit_protocol(self):
        import compare_duel_v10
        self.assertIs(run_lan.select_report_comparator([report(),report()],protocol='10'),compare_duel_v10.compare)
    def test_duel_arguments_and_report_objects_forwarded_unchanged(self):
        reports=[report(),report()];before=copy.deepcopy(reports);selected=Mock(return_value={'passed':True})
        with patch('importlib.import_module',return_value=Mock(compare=selected)) as imp:
            result=run_lan.compare(*reports,True,False,protocol='auto',require_host_rules=True,expected_waves=200,expected_start_gold='900719925474099312345',expected_start_only=False,allow_model=True)
        self.assertTrue(result['passed']);self.assertEqual(reports,before);imp.assert_called_once_with('compare_duel_v10')
        self.assertEqual(selected.call_args.kwargs['expected_start_gold'],'900719925474099312345');self.assertEqual(selected.call_args.args[2:],(True,False))
    def test_team_arguments_and_four_six_lan_relay_reports_forwarded(self):
        for count in (4,6):
            for mode in ('lan','relay-dtls'):
                reports=[dict(report(),mode=mode,slot=i) for i in range(count)];before=copy.deepcopy(reports);chosen=Mock(return_value={'passed':True})
                with patch('importlib.import_module',return_value=Mock(compare=chosen)) as imp:
                    result=run_lan.compare_team(reports,protocol='10',expected_map=5,expected_start_only=True)
                self.assertTrue(result['passed']);self.assertEqual(reports,before);imp.assert_called_once_with('compare_team_v10');self.assertEqual(chosen.call_args.kwargs,dict(expected_map=5,expected_start_only=True))
    def test_team_mixed_core_invalid_roster_unknown_version_fail_closed(self):
        for reports in ([report()]*2,[report()]*5,[report(8)]*4,[report(9)]*3+[report(10)],[report()]*3+[report(10,'other')]):
            result=run_lan.compare_team(reports);self.assertFalse(result['passed']);self.assertFalse(result['actual_transport_proven'])
    def test_cli_protocol10_and_exact_gold(self):
        parser=argparse.ArgumentParser();run_lan.add_version_arguments(parser)
        a=parser.parse_args(['--protocol','10','--waves','200','--start-gold','900719925474099312345'])
        forwarded=run_lan.rules_arguments(a,parser)
        self.assertEqual(a.protocol,'10');self.assertIn('200',forwarded);self.assertIn('900719925474099312345',forwarded)
        self.assertIn('--duel-host-rules',forwarded)
    def test_existing_startup_only_conflicts_preserved(self):
        parser=argparse.ArgumentParser();run_lan.add_version_arguments(parser)
        a=parser.parse_args(['--protocol','10','--host-rules-start-only']);a.complete_matches=True
        with self.assertRaises(SystemExit):run_lan.rules_arguments(a,parser)
    def test_invalid_explicit_selector_fails_closed(self):
        for selector in ('ten','11',None):self.assertFalse(run_lan.compare(report(),report(),protocol=selector)['passed'])
    def test_neither_missing_model_report_nor_new_protocol_proves_transport(self):
        result=run_lan.compare(report(),report());self.assertFalse(result['passed'])
        self.assertFalse(result.get('actual_transport_proven',False))
        result=run_lan.compare_team([report()]*4);self.assertFalse(result['passed']);self.assertFalse(result['actual_transport_proven'])

if __name__=='__main__':unittest.main(verbosity=2)
