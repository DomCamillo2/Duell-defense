using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DefenseDuell.Core;
#if UNITY_EDITOR
using NUnit.Framework;
#endif

namespace DefenseDuell.Tests
{
    public static class MapPlacement15Scenarios
    {
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        public static readonly string[] Ids={"meadow","desert","frost","emerald-basin","amber-canyon","aurora-bay"};
        static readonly string[] LegacyRoutes={
            "30,230;480,230;610,180;780,180;900,280;1010,300;1320,300;1420,400;1420,500;1320,600;960,600;820,510;600,510;460,650;220,650;130,740;220,830;560,830;760,760;1010,800;1270,800;1440,740;1570,740",
            "30,780;180,780;270,680;270,380;390,240;650,240;790,120;1050,120;1200,260;1200,490;1070,610;830,610;690,460;500,460;460,600;600,780;930,840;1210,810;1350,680;1570,680",
            "30,160;350,160;470,260;700,260;830,150;1140,150;1290,280;1290,430;1160,540;900,540;780,440;560,440;400,570;400,750;550,850;820,850;980,740;1140,740;1280,840;1460,840;1570,730"};
        static readonly string[] LegacyBlockers={
            "890,10,1130,210;1210,10,1450,210;10,290,250,490;170,10,330,170;410,10,610,130;570,250,770,450;330,290,490,490;610,570,810,690;1130,370,1290,530",
            "10,10,250,210;1250,10,1490,210;1330,290,1570,490;10,290,210,490;290,730,490,930;330,10,490,170;770,210,1010,410;10,570,130,730;1450,730,1570,890;410,290,610,410",
            "10,210,250,410;10,490,250,690;1370,290,1570,490;490,10,650,210;10,770,170,930;530,570,770,770;850,210,1090,410;1450,10,1570,170;1330,570,1530,690"};
        static readonly int[] BeforeCounts={122,114,141},AfterCounts={155,154,163};
        public static GameContent Content(MapDefinition map)
        {
            var b=GameContent.CreateDefault();return new GameContent(b.Rules,b.Towers,b.Units,map,b.BaseContent.Waves,b.Research,b.WaveRules);
        }
        public static IEnumerable<MapPoint> Samples(MapDefinition map)
        {for(int y=30;y<map.Height-25;y+=60)for(int x=30;x<map.Width-25;x+=60)yield return new MapPoint(x,y);}
        public static double RouteDistanceSquared(MapDefinition map,MapPoint point)
        {
            double nearest=double.MaxValue;var route=map.Waypoints;
            for(int i=1;i<route.Length;i++)
            {
                double dx=route[i].X-route[i-1].X,dy=route[i].Y-route[i-1].Y;
                double px=point.X-route[i-1].X,py=point.Y-route[i-1].Y;
                double t=Math.Max(0,Math.Min(1,(px*dx+py*dy)/(dx*dx+dy*dy)));
                double x=px-t*dx,y=py-t*dy;nearest=Math.Min(nearest,x*x+y*y);
            }
            return nearest;
        }
        public static void StableCatalogAndLegacyRoutes()
        {
            Check(MapCatalog.Count==6,"Six maps required");
            for(int i=0;i<6;i++)
            {
                var map=MapCatalog.Create(i);Content(map).Validate();
                Check(map.Id==Ids[i]&&map.Theme==i%3,"Stable ID/theme "+i);
                Check(map.FreePlacement&&map.MineSlots.Length==0,"Free placement remains available to mines "+i);
                Check(map.Width==(i<3?1600:2400)&&map.Height==(i<3?960:1440),"Map dimensions "+i);
                if(i<3)Check(string.Join(";",map.Waypoints.Select(p=>p.X+","+p.Y))==LegacyRoutes[i],"Existing route changed "+i);
                else Check(map.Length==MapCatalog.Create(i-3).Length,"Large map changed travel distance "+i);
            }
            Check(MapCatalog.Create(3).Name=="Smaragdkessel"&&MapCatalog.Create(4).Name=="Bernsteinschlucht"&&MapCatalog.Create(5).Name=="Nordlichtbucht","New names");
            foreach(int index in new[]{-1,6})
            {bool rejected=false;try{MapCatalog.Create(index);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected,"Invalid catalog index accepted");}
        }
        public static void AllLegacyInsetsRecoverLegalGroundWithoutLosingExistingPlacements()
        {
            int total=0;
            for(int i=0;i<3;i++)
            {
                var map=MapCatalog.Create(i);var oldAreas=LegacyBlockers[i].Split(';').Select(s=>s.Split(',').Select(int.Parse).ToArray())
                    .Select(v=>new MapRectangle(v[0],v[1],v[2],v[3])).ToArray();
                Check(map.BlockedAreas.Length==oldAreas.Length,"Old blocker count changed");total+=oldAreas.Length;
                for(int n=0;n<oldAreas.Length;n++)
                {
                    var old=oldAreas[n];var current=map.BlockedAreas[n];
                    Check(current.MinX==old.MinX+20&&current.MinY==old.MinY+20&&current.MaxX==old.MaxX-20&&current.MaxY==old.MaxY-20,"20-unit inset/order "+i+"/"+n);
                }
                var original=new MapDefinition(map.Waypoints,map.BuildSlots,map.Id,map.Name,map.Theme,map.Width,map.Height,map.MineSlots,map.FreePlacement,oldAreas);
                var before=Content(original);var after=Content(map);var state=new MatchSimulation(after).State;
                int legalBefore=0,legalAfter=0,newlyOpened=0;
                foreach(var point in Samples(map))
                {
                    bool wasLegal=PlacementValidator.ValidatePosition(before,state,PlayerSlot.Zero,0,-1,point)==ErrorCode.None;
                    bool isLegal=PlacementValidator.ValidatePosition(after,state,PlayerSlot.Zero,0,-1,point)==ErrorCode.None;
                    if(wasLegal){legalBefore++;Check(isLegal,"Inset removed a previously legal point");}
                    if(isLegal)legalAfter++;
                    if(isLegal&&!wasLegal)
                    {
                        newlyOpened++;state.Phase=MatchPhase.Playing;state.Players[0].Gold=100000;state.Players[0].AvailableTowerMask=127;
                        Check(PlacementValidator.ValidateBuild(after,state,PlayerSlot.Zero,0,-1,point,out _) == ErrorCode.None,"New ground cannot be purchased");
                    }
                }
                Check(legalBefore==BeforeCounts[i]&&legalAfter==AfterCounts[i]&&newlyOpened==AfterCounts[i]-BeforeCounts[i],"Grid regression "+i+": "+legalBefore+" -> "+legalAfter);
            }
            Check(total==28,"All 28 legacy blockers must be updated");
        }
        public static void LargeLandmarksKeepRoutesClearAndGroundUsable()
        {
            for(int i=3;i<6;i++)
            {
                var map=MapCatalog.Create(i);var content=Content(map);var state=new MatchSimulation(content).State;
                Check(map.BlockedAreas.Length>=6,"Distributed reserved scenery required");
                var areas=map.BlockedAreas;
                long reserved=0;
                foreach(var area in areas)
                {
                    Check(area.MinX>=30&&area.MinY>=30&&area.MaxX<=map.Width-30&&area.MaxY<=map.Height-30,"Scenery outside field");
                    reserved+=(long)(area.MaxX-area.MinX)*(area.MaxY-area.MinY);
                }
                Check(reserved>=map.Width*map.Height/5&&reserved<=map.Width*map.Height*27/100,"Reserved landscape must be visible without consuming buildable ground");
                for(int a=0;a<areas.Length;a++)for(int b=a+1;b<areas.Length;b++)
                    Check(areas[a].MaxX<=areas[b].MinX||areas[b].MaxX<=areas[a].MinX||areas[a].MaxY<=areas[b].MinY||areas[b].MaxY<=areas[a].MinY,"Scenery reservations overlap");
                foreach(var area in areas)
                {
                    for(int y=area.MinY;y<=area.MaxY;y+=10)for(int x=area.MinX;x<=area.MaxX;x+=10)
                        Check(RouteDistanceSquared(map,new MapPoint(x,y))>=100*100,"Scenery intersects route or its clearance");
                    var center=new MapPoint((area.MinX+area.MaxX)/2,(area.MinY+area.MaxY)/2);
                    Check(PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,0,-1,center)==ErrorCode.BlockedArea,"Scenery is buildable");
                }
                int legal=0,near=0;
                foreach(var p in Samples(map))
                {
                    var combat=PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,0,-1,p);
                    Check(combat==PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,4,-1,p),"Mine/combat geometry differs");
                    if(combat!=ErrorCode.None)continue;legal++;if(RouteDistanceSquared(map,p)<=150*150)near++;
                }
                Check(legal>=490&&legal>AfterCounts[i-3]*3&&near>=170,"Large map lacks buildable or useful near-route ground "+i);
                MapPoint exact=default;bool boundary=false;
                for(int n=1;n<map.Waypoints.Length&&!boundary;n++)
                {
                    var a=map.Waypoints[n-1];var b=map.Waypoints[n];if(a.Y!=b.Y)continue;
                    foreach(int side in new[]{-1,1})
                    {
                        var at70=new MapPoint((a.X+b.X)/2,a.Y+side*70);var at69=new MapPoint(at70.X,a.Y+side*69);
                        if(PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,0,-1,at70)!=ErrorCode.None)continue;
                        Check(PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,0,-1,at69)==ErrorCode.OnPath,"69-unit path boundary accepted");
                        exact=at70;boundary=true;break;
                    }
                }
                Check(boundary,"No usable exact 70-unit path boundary found");
                Check(PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,0,-1,new MapPoint(29,500))==ErrorCode.OutsideMap,"Building outside edge margin");
                state.Towers.Add(new TowerState{Id=1,Owner=PlayerSlot.Zero,Kind=0,Position=exact,Slot=0,SiteId=-1});
                Check(PlacementValidator.ValidatePosition(content,state,PlayerSlot.Zero,4,-1,exact)==ErrorCode.TooClose,"Overlap accepted");
                Check(PlacementValidator.ValidatePosition(content,state,PlayerSlot.One,0,-1,exact)==ErrorCode.None,"Separate field incorrectly shares occupancy");
            }
        }
        public static void ExpandedRoutesHaveIndependentNonIntersectingCompositions()
        {
            var profiles=new HashSet<string>();
            for(int index=3;index<6;index++)
            {
                var map=MapCatalog.Create(index);var p=map.Waypoints;
                Check(p.Length>=10,"Authored route requires meaningful bends");
                Check(p.Max(v=>v.X)-p.Min(v=>v.X)>=2200&&p.Max(v=>v.Y)-p.Min(v=>v.Y)>=900,"Route should use the expanded field");
                int diagonal=0;var directions=new List<string>();
                for(int n=1;n<p.Length;n++)
                {
                    int dx=p[n].X-p[n-1].X,dy=p[n].Y-p[n-1].Y;
                    Check(dx!=0||dy!=0,"Zero length route segment");if(dx!=0&&dy!=0)diagonal++;
                    directions.Add(Math.Sign(dx)+","+Math.Sign(dy));
                    for(int m=n+2;m<p.Length;m++)
                    {
                        long Cross(MapPoint a,MapPoint b,MapPoint c)=>(long)(b.X-a.X)*(c.Y-a.Y)-(long)(b.Y-a.Y)*(c.X-a.X);
                        var a=p[n-1];var b=p[n];var c=p[m-1];var d=p[m];
                        bool boxes=Math.Max(Math.Min(a.X,b.X),Math.Min(c.X,d.X))<=Math.Min(Math.Max(a.X,b.X),Math.Max(c.X,d.X))&&Math.Max(Math.Min(a.Y,b.Y),Math.Min(c.Y,d.Y))<=Math.Min(Math.Max(a.Y,b.Y),Math.Max(c.Y,d.Y));
                        Check(!boxes||Math.Sign(Cross(a,b,c))*Math.Sign(Cross(a,b,d))>0||Math.Sign(Cross(c,d,a))*Math.Sign(Cross(c,d,b))>0,"Nonadjacent route segments intersect");
                    }
                }
                Check(diagonal>=4,"Route lacks authored diagonal transitions");
                Check(profiles.Add(string.Join(";",directions)),"Expanded route is only a rescaled/palette copy");
            }
        }
        public static void CatalogCopiesPreserveEffectiveRulesAndAuthoredBase()
        {
            var basis=GameContent.CreateDefault();var gold=BigInteger.Pow(10,40)+123;
            var template=basis.WithMatchRules(new MatchRules(3,gold));var copies=MapCatalog.CreateContents(template);
            Check(copies.Length==6,"Content catalog size");
            var hashes=new HashSet<string>();
            for(int i=0;i<copies.Length;i++)
            {
                copies[i].Validate();Check(copies[i].Map.Id==Ids[i],"Content IDs/order");
                Check(copies[i].MatchStartGold==gold&&copies[i].WaveCount==3&&copies[i].MatchRules.MapId==Ids[i],"Effective rules/map binding lost");
                Check(copies[i].BaseContent.Waves.Length==basis.BaseContent.Waves.Length,"Authored base lost");
                Check(copies[i].WithMatchRules(new MatchRules(10,gold,mapId:Ids[i])).WaveCount==10,"Cannot restore authored waves");
                Check(hashes.Add(copies[i].ContentHash),"Map missing from content hash");
            }
            // Rebuilding a catalog from an already selected large map must not duplicate it
            // into index zero or retain its canonical MapId in every copied rule set.
            var recopy=MapCatalog.CreateContents(copies[4]);
            for(int i=0;i<recopy.Length;i++)Check(recopy[i].Map.Id==Ids[i]&&recopy[i].MatchRules.MapId==Ids[i],"Nonzero template moved catalog indices");
            Check(basis.Map.Id==Ids[0]&&basis.WaveCount==10,"Input template mutated");
        }
        public static void CatalogCopiesRetainTeamEndlessAndIncomeOptions()
        {
            var options=new MatchRules(200,BigInteger.Pow(10,50)+7,endless:true,incomeMultiplier:5,
                format:MatchFormat.ThreeVsThree,mapId:Ids[4]);
            var selected=GameContent.CreateDefault().WithMatchRules(options);
            var copies=MapCatalog.CreateContents(selected);
            for(int i=0;i<copies.Length;i++)
            {
                var rules=copies[i].MatchRules;
                Check(copies[i].Map.Id==Ids[i]&&rules.MapId==Ids[i],"Endless catalog map binding");
                Check(rules.Endless&&rules.WaveCount==200&&copies[i].WaveCount==0,"Endless changed into finite plan");
                Check(rules.IncomeMultiplier==5&&rules.Format==MatchFormat.ThreeVsThree&&rules.PlayerCount==6,"Team/income options lost in map copy");
                Check(rules.StartGold==options.StartGold&&copies[i].BaseContent.Waves.Length==10,"Gold or authored base changed");
                Check(copies[i].ContainsWave(201)&&copies[i].GetWave(201).HpPercent==selected.GetWave(201).HpPercent,"Continuation differs between copied maps");
            }
        }
    }
#if UNITY_EDITOR
    public sealed class MapPlacement15Tests
    {
        [Test] public void StableCatalogAndLegacyRoutes()=>MapPlacement15Scenarios.StableCatalogAndLegacyRoutes();
        [Test] public void AllLegacyInsetsRecoverLegalGroundWithoutLosingExistingPlacements()=>MapPlacement15Scenarios.AllLegacyInsetsRecoverLegalGroundWithoutLosingExistingPlacements();
        [Test] public void LargeLandmarksKeepRoutesClearAndGroundUsable()=>MapPlacement15Scenarios.LargeLandmarksKeepRoutesClearAndGroundUsable();
        [Test] public void ExpandedRoutesHaveIndependentNonIntersectingCompositions()=>MapPlacement15Scenarios.ExpandedRoutesHaveIndependentNonIntersectingCompositions();
        [Test] public void CatalogCopiesPreserveEffectiveRulesAndAuthoredBase()=>MapPlacement15Scenarios.CatalogCopiesPreserveEffectiveRulesAndAuthoredBase();
        [Test] public void CatalogCopiesRetainTeamEndlessAndIncomeOptions()=>MapPlacement15Scenarios.CatalogCopiesRetainTeamEndlessAndIncomeOptions();
    }
#endif
}
