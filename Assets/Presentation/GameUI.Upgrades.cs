using System.Text;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        static string ShortMetricLabel(string label)=>label=="Rüstungsdurchdringung"?"Rüstung":label=="Explosionsradius"?"Radius":label=="Frostdauer"?"Frost":label=="Sprungreichweite"?"Sprung":label;
        static string ShortUpgradeLabel(TowerUpgradePath path)
        {
            switch(path)
            {
                case TowerUpgradePath.Firepower:return "Feuerkraft";
                case TowerUpgradePath.Blast:return "Sprengkraft";
                case TowerUpgradePath.FrostDuration:return "Frostdauer";
                case TowerUpgradePath.ArmorPiercing:return "Durchschlag";
                case TowerUpgradePath.ChainPower:return "Kettenkraft";
                case TowerUpgradePath.PiercePower:return "Bolzenkraft";
                case TowerUpgradePath.Income:return "Förderung";
                case TowerUpgradePath.BlastRadius:return "Radius";
                case TowerUpgradePath.JumpRange:return "Sprungweite";
                case TowerUpgradePath.Range:return "Reichweite";
                default:return "Schaden";
            }
        }
        void OpenUpgradeDetails()
        {
            if(InfoBlocking||modal!=null||dragging||session?.State==null)return;
            TowerState selected=null;
            foreach(var tower in session.State.Towers)if(tower.Id==selectedTower&&tower.Owner==session.LocalSlot){selected=tower;break;}
            if(selected==null)return;
            var copy=new StringBuilder(towers[selected.Kind].Name).Append(" · Stufe ").Append(GameNumbers.Exact(selected.Level));
            copy.Append("\nGold: ").Append(GameNumbers.Exact(session.State.Players[(int)session.LocalSlot].Gold));
            copy.Append("\nKaufmodus: "+(upgradeBatchMode==UpgradeBatchMode.Max?"Max":upgradeBatchMode==UpgradeBatchMode.Ten?"10×":"1×")+" · Kein Mengenrabatt. Max kauft nur die angezeigte bezahlbare Anzahl.\n");
            void Describe(TowerUpgradePath path)
            {
                var quote=TowerUpgradeBatchRules.Quote(content,selected,path,session.State.Players[(int)session.LocalSlot].Gold,upgradeBatchMode);
                copy.Append("\n").Append(TowerUpgradeRules.PathLabel(content,selected,path)).Append(" · ").Append(GameNumbers.Exact(quote.Count)).Append(" Stufen · ").Append(GameNumbers.Exact(quote.Price)).Append(" Gold\n");
                foreach(var metric in BatchPreview(content,quote))
                {
                    copy.Append(metric.Label).Append(": ").Append(GameNumbers.Exact(metric.Current*(path==TowerUpgradePath.Income?content.MatchRules.IncomeMultiplier:1))).Append(" → ").Append(GameNumbers.Exact(metric.Next*(path==TowerUpgradePath.Income?content.MatchRules.IncomeMultiplier:1)));
                    if(metric.Divisor>1)copy.Append(" (je ").Append(metric.Divisor).Append(" = 1 ").Append(metric.Unit).Append(')');
                    else if(metric.Unit.Length>0)copy.Append(' ').Append(metric.Unit);
                    copy.Append('\n');
                }
            }
            Describe(TowerUpgradeRules.PrimaryPath(content,selected.Kind));
            if(!towers[selected.Kind].IsEconomy)Describe(TowerUpgradeRules.SecondaryPath(content,selected.Kind));
            copy.Append("\nVerkaufen: ").Append(GameNumbers.Exact(EconomyRules.RefundGold(content,selected))).Append(" Gold zurück (").Append(EconomyRules.RefundPercent(content,selected.Kind)).Append(" %).");
            OpenReadOnlyDetails("GEBÄUDE & UPGRADES",copy.ToString());
        }
        void OpenResearchDetails()
        {
            if(infoKind<0||!InfoBlocking||session?.State==null)return;
            var q=displayedResearchQuote;int kind=infoKind;
            string copy=units[kind].Name+" · Forschung S"+GameNumbers.Exact(q.Level)+" → S"+GameNumbers.Exact(q.NextLevel)+
                "\nKosten: "+GameNumbers.Exact(q.Price)+" Gold\nLP: "+GameNumbers.Exact(q.Current.MaxHp)+" → "+GameNumbers.Exact(q.Next.MaxHp)+
                "\nTempo: "+q.Current.SpeedPerSecond+" → "+q.Next.SpeedPerSecond+
                "\n\nGilt nur für anschließend gekaufte Truppen. Bereits bezahlte Pakete behalten ihre Forschungsstufe.";
            OpenReadOnlyDetails("FORSCHUNG · EXAKTE WERTE",copy);
        }
        void OpenReadOnlyDetails(string title,string copy)
        {
            float w=Mathf.Min(660,width-48),h=Mathf.Min(540,height-48);OpenModal(title,w,h);
            var viewport=UiKit.Rect("Exact values viewport",modal);UiKit.At(viewport,24,94,w-48,h-118);
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport;
            var contentRect=UiKit.Rect("Exact values",viewport);UiKit.At(contentRect,0,0,w-48,h-118);
            var text=UiKit.TextAt(contentRect,copy,0,0,w-48,h-118,16,HudText,TextAnchor.UpperLeft);text.raycastTarget=true;
            float contentHeight=Mathf.Max(h-118,text.preferredHeight+16);contentRect.sizeDelta=new Vector2(w-48,contentHeight);text.rectTransform.sizeDelta=contentRect.sizeDelta;
            scroll.content=contentRect;scroll.verticalNormalizedPosition=1;
        }
    }
}
