using System.Collections.Generic;
using BigInteger=System.Numerics.BigInteger;
using DefenseDuell.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DefenseDuell.Presentation
{
    public sealed partial class GameUI
    {
        UpgradeBatchMode upgradeBatchMode;
        RectTransform upgradeModes;
        Button[] upgradeModeButtons;
        UpgradeBatchQuote displayedPrimaryBatch,displayedSecondaryBatch;
        int batchTowerId=-1;
        BigInteger batchTowerLevel;

        void CreateUpgradeModes(float contextWidth)
        {
            upgradeBatchMode=UpgradeBatchMode.Single;batchTowerId=-1;
            var panel=BattlePanel(contextDock,"Upgrade quantity",contextWidth-250,-48,250,44,HudPanel);
            upgradeModes=panel.rectTransform;upgradeModeButtons=new Button[3];
            UiKit.TextAt(upgradeModes,"KAUF",8,0,42,44,11,HudMuted);
            string[] labels={"1×","10×","Max"};
            for(int i=0;i<3;i++)
            {
                int mode=i;upgradeModeButtons[i]=BattleButton(upgradeModes,labels[i],54+i*64,0,60,44,()=>SetUpgradeMode((UpgradeBatchMode)mode),HudCard,16);
                upgradeModeButtons[i].name="Upgrade quantity "+labels[i];
            }
            upgradeModes.gameObject.SetActive(false);
        }
        void SetUpgradeMode(UpgradeBatchMode mode)
        {
            if(modal!=null||InfoBlocking||UiPointerGate.Active||selectedTower<0||commands.HasPendingUpgrade(selectedTower))return;
            upgradeBatchMode=mode;dirty=true;
            if(session?.State!=null)RefreshSelection(session.State,session.State.Players[(int)session.LocalSlot]);
        }
        void RefreshUpgradeModes(TowerState tower,bool playing)
        {
            if(upgradeModes==null)return;
            bool show=tower!=null&&!showEnemy&&!dragging;
            upgradeModes.gameObject.SetActive(show);
            if(!show){batchTowerId=-1;return;}
            if(batchTowerId!=tower.Id){upgradeBatchMode=UpgradeBatchMode.Single;batchTowerId=tower.Id;}
            batchTowerLevel=tower.Level;
            for(int i=0;i<upgradeModeButtons.Length;i++)
            {
                upgradeModeButtons[i].interactable=playing&&!commands.HasPendingUpgrade(tower.Id);
                UiKit.ButtonColor(upgradeModeButtons[i],i==(int)upgradeBatchMode?HudSelected:HudCard);
            }
        }
        static UpgradeStatPreview[] BatchPreview(GameContent content,UpgradeBatchQuote quote)
        {
            var a=quote.Current;var b=quote.Next;var list=new List<UpgradeStatPreview>();
            void Add(string label,string unit,BigInteger oldValue,BigInteger newValue,int divisor=1)
            {if(oldValue!=newValue)list.Add(new UpgradeStatPreview(label,unit,oldValue,newValue,divisor));}
            Add("Schaden","",a.Damage,b.Damage);Add("Reichweite","",a.Range,b.Range);
            Add("Schusspause","s",a.FireIntervalTicks,b.FireIntervalTicks,content.TickRate);
            Add("Förderung","G/s",a.IncomeUnitsPerSecond,b.IncomeUnitsPerSecond,content.WaveRules.IncomeUnitsPerGold);
            Add("Frostdauer","s",a.SlowDurationTicks,b.SlowDurationTicks,content.TickRate);
            Add("Explosionsradius","",a.SplashRadius,b.SplashRadius);Add("Rüstungsdurchdringung","",a.ArmorPiercing,b.ArmorPiercing);
            Add("Trefferziele","",a.MaxTargets,b.MaxTargets);Add("Sprungreichweite","",a.JumpRange,b.JumpRange);
            return list.ToArray();
        }
        void SubmitDisplayedUpgrade(TowerUpgradePath path)
        {
            if(InfoBlocking||modal!=null||UiPointerGate.Active||selectedTower<0||session?.State==null||commands.HasPendingUpgrade(selectedTower))return;
            TowerState tower=null;foreach(var t in session.State.Towers)if(t.Id==selectedTower&&t.Owner==session.LocalSlot){tower=t;break;}
            if(tower==null||batchTowerId!=tower.Id)return;
            bool secondary=!towers[tower.Kind].IsEconomy&&path==TowerUpgradeRules.SecondaryPath(content,tower.Kind);
            var quote=secondary?displayedSecondaryBatch:displayedPrimaryBatch;
            var button=secondary?rangeUpgradeButton:upgradeButton;
            if(!quote.IsValid){RejectPurchase(quote.Count==0?"Kein Upgrade bezahlbar.":"Für diesen Kauf reicht das Gold nicht.",button);return;}
            if(tower.Level!=batchTowerLevel){RejectPurchase("Der Turm hat sich geändert. Bitte die neuen Werte prüfen.",button);dirty=true;return;}
            if(commands.SessionId!=session.State.SessionId)commands.Reset(session.State.SessionId);
            // Capture the displayed count/price before synchronous local-host mutation.
            var context=PresentationCommandContext.Capture(session.State,session.LocalSlot,CommandType.UpgradeTowerBatch,(int)path,tower.Id,Time.unscaledTimeAsDouble,null,quote.Count);
            ulong sequence=session.SubmitTowerUpgradeBatch(tower.Id,path,quote.Count,batchTowerLevel,quote.Price);
            if(sequence==0){RejectPurchase(session.Message,button);return;}
            commands.Register(sequence,context);
            var result=session.LastResult;
            if(session.IsHost&&result.Sequence==sequence){if(commands.Resolve(result,out var captured))ApplyResolution(result,captured);}
            else notice="Upgrade wird bestätigt …";
            dirty=true;
        }
    }
}
