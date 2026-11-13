using System;
using System.Collections.Generic;
using DefenseDuell.Core;
using UnityEngine;

namespace DefenseDuell.Presentation
{
    /// <summary>
    /// Original, deterministic scenery for the playable board. All scenery is merged into
    /// the existing terrain mesh; routes, tower coordinates and combat remain authoritative.
    /// </summary>
    internal static class ScenicTerrain
    {
        const float Ground = .18f;

        struct Palette
        {
            public Color Grass, LightGrass, Earth, Rock, RockTop, Trail, Shoulder, Leaf, LightLeaf, Shade;
            public Palette(int theme)
            {
                // Quiet, matte surroundings derived from the shared portrait/model palette.
                Color stone=C(0xB5A089),ivory=C(0xFFF3D8),green=C(0x79B54B),orange=C(0xD98545);
                Grass = C(theme == 1 ? 0xD3B180u : theme == 2 ? 0xD8E8E7u : 0x719C50u);
                LightGrass = C(theme == 1 ? 0xE4C99Au : theme == 2 ? 0xEDF2E9u : 0xA1BF6Du);
                Earth = C(theme == 1 ? 0xA87C58u : theme == 2 ? 0x728F9Cu : 0x967D5Fu);
                Rock = theme == 1 ? Color.Lerp(stone,orange,.42f) : theme == 2 ? C(0x718C9A) : stone;
                RockTop = theme == 2 ? C(0xD5E3DF) : Color.Lerp(stone,ivory,theme == 1 ? .45f : .5f);
                Trail = C(theme == 1 ? 0xF0D6AAu : theme == 2 ? 0x8BB4BDu : 0xEBCB8Fu);
                Shoulder = C(theme == 1 ? 0xBB975Fu : theme == 2 ? 0x6C96A3u : 0x8D9D5Du);
                Leaf = C(theme == 1 ? 0x688A59u : theme == 2 ? 0x3C777Bu : 0x35684Du);
                LightLeaf = theme == 1 ? C(0x9EAD70) : theme == 2 ? C(0xA9CBC4) : Color.Lerp(green,ivory,.2f);
                Shade = C(theme == 1 ? 0x4F4038u : theme == 2 ? 0x526A76u : 0x203C35u);
            }
        }

        static Color C(uint hex) => DioramaResources.C(hex);
        static float Noise(int index, int seed)
        {
            uint value = unchecked((uint)(index * 374761393 + seed * 668265263));
            value = (value ^ (value >> 13)) * 1274126177u;
            return (value & 65535) / 65535f;
        }

        internal static void Build(DioramaMeshBuilder b, MapDefinition map, int width, int height)
        {
            var palette = new Palette(map.Theme);
            var path = map.Waypoints;
            var slots = map.BuildSlots;
            if(map.FreePlacement)
            {
                var allSlots=slots;var mineSites=map.MineSlots;slots=new MapPoint[mineSites.Length];
                for(int i=0;i<mineSites.Length;i++)slots[i]=allSlots[mineSites[i]];
            }
            Vector3 center = DioramaResources.Point(new MapPoint(width / 2, height / 2), 0);
            float halfWidth = width / 100f + 1.9f, halfDepth = height / 100f + 1.9f;
            Island(b, center, halfWidth, halfDepth, palette);
            MeadowSurface(b,center,halfWidth-.68f,halfDepth-.68f,palette);
            GroundCover(b, center, halfWidth, halfDepth, palette, map.Theme);
            BorderLandscape(b,width,height,palette,map.Theme);

            // The broad shoulders and thin ruts follow exactly the simulation's polyline.
            b.Ribbon(path, 1.08f, Ground + .006f, Color.Lerp(palette.Grass,palette.Shoulder,.38f));
            b.Ribbon(path, .78f, Ground + .008f, palette.Shoulder);
            b.Ribbon(path, .62f, Ground + .022f, Color.Lerp(palette.Earth, palette.Trail, .6f));
            b.Ribbon(path, .53f, Ground + .031f, palette.Trail);
            TrailDetails(b, path, slots, palette, map.Theme,map.FreePlacement);
            FineGroundDetails(b,map,palette);
            var fixedSlots=map.BuildSlots;
            for(int i=0;i<fixedSlots.Length;i++)
            {
                if(!map.IsMineSlot(i))continue;
                Vector3 ore=DioramaResources.Point(fixedSlots[i],Ground+.012f);
                SoftDisc(b,ore,new Vector2(.84f,.72f),Color.Lerp(palette.Earth,C(0xD4AD50),.65f),palette.Grass);
                for(int vein=0;vein<3;vein++)
                    b.Box(ore+new Vector3(-.5f+vein*.5f,.008f,.18f-vein*.14f),new Vector3(.09f,.014f,.61f),C(0xD8BD68),Quaternion.Euler(0,25+vein*27,0));
            }

            // Rocks and tall trees use a conservative projected-footprint test as well as
            // physical clearance, so their crowns cannot cover a build pad or enemy lane.
            if(map.FreePlacement) ReservedScenery(b,map,palette);
            int sceneryCount = map.FreePlacement?0:width >= 1400 ? 720 : 360;
            var planted = new List<Vector3>(96);
            for (int i = 0; i < sceneryCount; i++)
            {
                int x = Mathf.RoundToInt(-15 + Noise(i, 23 + map.Theme) * (width + 30));
                int y = Mathf.RoundToInt(-15 + Noise(i, 71 + map.Theme) * (height + 30));
                var p = new MapPoint(x, y);
                float size = .76f + Noise(i, 139) * .63f;
                bool rock = i % 8 == 0;
                float tall = rock ? size * 1.3f : size * (map.Theme==1?1.6f:2.12f);
                float radius = rock ? size * 1.3f : size * (map.Theme==1&&i%3==0?.85f:.68f);
                if (!ClearScenery(p, radius, tall, path, slots)) continue;
                bool crowded=false;
                for(int j=0;j<planted.Count;j++)
                    if(Vector2.Distance(new Vector2(p.X,p.Y),new Vector2(planted[j].x,planted[j].y))<(radius*50+planted[j].z)*.72f){crowded=true;break;}
                if(crowded)continue;
                planted.Add(new Vector3(p.X,p.Y,radius*50));
                Vector3 v = DioramaResources.Point(p, Ground);
                Shadow(b, v, radius * 1.3f, palette.Grass, tall, palette.Shade);
                if (rock)
                {
                    Boulder(b, v, new Vector3(size, size * 1.13f, size * .77f), palette, i);
                    Boulder(b, v + new Vector3(size * .72f, 0, -.15f), new Vector3(.45f, .38f, .42f) * size, palette, i + 2);
                }
                else if (map.Theme == 1) Cactus(b, v, size, palette, i);
                else Pine(b, v, size, palette, map.Theme == 2, i);
                if(planted.Count>=(width>=1400?90:46))break;
            }

            // Small plants give the open areas texture without obscuring battle silhouettes.
            for (int i = 0; i < (width >= 1400 ? 130 : 72); i++)
            {
                var p = new MapPoint(Mathf.RoundToInt(20 + Noise(i, 217) * (width - 40)),
                    Mathf.RoundToInt(20 + Noise(i, 311) * (height - 40)));
                if (!ClearScenery(p, .52f, .5f, path, slots)) continue;
                if (LandmarkGround(map,p,.52f)) continue;
                Vector3 v = DioramaResources.Point(p, Ground + .01f);
                if (i % 5 == 0) Boulder(b, v, new Vector3(.3f, .23f, .27f), palette, i);
                else Shrub(b, v, .32f + Noise(i, 137) * .2f, palette, i);
            }

            if(!map.FreePlacement)Landmarks(b, width, height, path, slots, palette, map.Theme);
            EntryAndKeep(b, path, palette, map.Theme);
            MapLandmarkArt.Append(b, map);
        }

        // The landscape continues under the interface. It is a separate mesh so distant
        // decoration never shrinks the playable camera fit or becomes a collision rule.
        internal static void BuildBackdrop(DioramaMeshBuilder b,MapDefinition map)
        {
            var p=new Palette(map.Theme);Vector3 center=DioramaResources.Point(new MapPoint(map.Width/2,map.Height/2),-.96f);
            const float extent=90;
            Color baseColor=Color.Lerp(p.Grass,p.Shade,.24f);
            b.SurfaceQuad(center+new Vector3(-extent,0,-extent),center+new Vector3(-extent,0,extent),
                center+new Vector3(extent,0,extent),center+new Vector3(extent,0,-extent),baseColor);
            float w=map.Width/100f,d=map.Height/100f;
            for(int i=0;i<130;i++)
            {
                float angle=i*2.399963f,radius=5+Noise(i,121)*32;
                Vector3 v=center+new Vector3(Mathf.Cos(angle)*(w+radius),.004f,Mathf.Sin(angle)*(d+radius));
                SoftDisc(b,v,new Vector2(2+Noise(i,67)*6,1.5f+Noise(i,87)*4),
                    Color.Lerp(baseColor,i%3==0?p.Earth:p.LightGrass,.12f+Noise(i,69)*.13f),baseColor,12);
                if(i%3!=0||Mathf.Abs(v.x-center.x)<w+4&&Mathf.Abs(v.z-center.z)<d+5)continue;
                float size=.8f+Noise(i,39)*1.2f;
                Shadow(b,v,size*.8f,baseColor,size*2,p.Shade);
                if(map.Theme==1)Boulder(b,v,new Vector3(size,size*1.4f,size),p,i);
                else Pine(b,v,size,p,map.Theme==2,i);
            }
            // A soft contact band anchors the raised battlefield in its surrounding biome.
            for(int side=0;side<4;side++)for(int i=0;i<24;i++)
            {
                float t=(i+.5f)/24;Vector3 v=center+new Vector3(side<2?(side==0?-w-1.9f:w+1.9f):Mathf.Lerp(-w,w,t),.018f,
                    side<2?Mathf.Lerp(-d,d,t):(side==2?-d-1.9f:d+1.9f));
                SoftDisc(b,v,new Vector2(side<2?1.3f:1.8f,side<2?1.8f:1.3f),Color.Lerp(baseColor,p.Shade,.28f),baseColor);
            }
        }
        static void MeadowSurface(DioramaMeshBuilder b,Vector3 center,float w,float d,Palette p)
        {
            // Keep roughly the same world-space shading detail on the larger boards.
            // This is still one static merged mesh, not extra renderers or bitmap terrain.
            int nx=w>20?72:48,nz=d>12?45:30;
            Color Tint(float x,float z)
            {
                float n=(Mathf.Sin(x*.41f+Mathf.Sin(z*.32f)*1.4f)+Mathf.Cos(z*.57f-x*.18f))*.25f+.5f;
                return Color.Lerp(Color.Lerp(p.Grass,p.Leaf,.1f),Color.Lerp(p.Grass,p.LightGrass,.28f),n);
            }
            for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
            {
                float x0=Mathf.Lerp(-w,w,x/(float)nx),x1=Mathf.Lerp(-w,w,(x+1f)/nx);
                float z0=Mathf.Lerp(-d,d,z/(float)nz),z1=Mathf.Lerp(-d,d,(z+1f)/nz);
                Vector3 a=center+new Vector3(x0,Ground+.0004f,z0),c=center+new Vector3(x1,Ground+.0004f,z1);
                Vector3 bb=center+new Vector3(x0,Ground+.0004f,z1),dd=center+new Vector3(x1,Ground+.0004f,z0);
                b.GradientTriangle(a,bb,c,Tint(x0,z0),Tint(x0,z1),Tint(x1,z1));
                b.GradientTriangle(a,c,dd,Tint(x0,z0),Tint(x1,z1),Tint(x1,z0));
            }
        }
        static void FineGroundDetails(DioramaMeshBuilder b,MapDefinition map,Palette p)
        {
            // All added details are low ground cover, not new invisible obstacles.
            var route=map.Waypoints;
            int detailCount=map.Width>=2000?560:280;
            for(int i=0;i<detailCount;i++)
            {
                var point=new MapPoint(Mathf.RoundToInt(20+Noise(i,1701)*(map.Width-40)),Mathf.RoundToInt(20+Noise(i,1703)*(map.Height-40)));
                if(!ClearScenery(point,.14f,.08f,route,Array.Empty<MapPoint>(),46))continue;
                if(LandmarkGround(map,point,.14f))continue;
                Vector3 v=DioramaResources.Point(point,Ground+.009f);
                Color leaf=Color.Lerp(p.Grass,p.LightLeaf,.2f+Noise(i,1711)*.28f);
                if(map.Theme==1)
                {
                    b.SurfaceTriangle(v+new Vector3(-.16f,0,-.03f),v+new Vector3(0,0,.065f),v+new Vector3(.19f,0,-.01f),Color.Lerp(p.Grass,p.Earth,.19f));
                }
                else
                {
                    for(int j=0;j<3;j++)
                    {
                        float a=(i*.71f+j*2.1f);Vector3 tip=v+new Vector3(Mathf.Cos(a)*.09f,.04f+Noise(i+j,1723)*.06f,Mathf.Sin(a)*.09f);
                        b.SurfaceTriangle(v+new Vector3(-.025f,0,0),tip,v+new Vector3(.025f,0,0),leaf);
                        if(map.Theme==0&&i%13==0)b.Ball(tip,Vector3.one*.027f,i%2==0?C(0xFFF0A2):C(0xD8BCF1),5,3);
                    }
                }
            }
        }

        static bool LandmarkGround(MapDefinition map,MapPoint point,float radius)
        {
            // Generic scatter must not grow through authored water, steps or cabins.
            // This only filters decoration; the authoritative blocker is untouched.
            var areas=map.BlockedAreas;float r=radius*50;
            for(int i=0;i<areas.Length;i++)
            {
                if(!MapLandmarkArt.Replaces(map,i))continue;
                var area=areas[i];
                if(point.X+r>=area.MinX&&point.X-r<=area.MaxX&&point.Y+r>=area.MinY&&point.Y-r<=area.MaxY)return true;
            }
            return false;
        }

        static void Island(DioramaMeshBuilder b, Vector3 center, float w, float d, Palette p)
        {
            // Irregular bevels and three strata replace the perfectly rectangular slab.
            const int sides = 48;
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = Edge(i, sides, w, d), next = Edge(i + 1, sides, w, d);
                Vector3 topA = center + a + Vector3.up * Ground;
                Vector3 topB = center + next + Vector3.up * Ground;
                Vector3 shoulderA = center + a * 1.012f + Vector3.down * .04f;
                Vector3 shoulderB = center + next * 1.012f + Vector3.down * .04f;
                Vector3 lowA = center + a * .997f + Vector3.down * .92f;
                Vector3 lowB = center + next * .997f + Vector3.down * .92f;
                Vector3 bottomA = center + a * .992f + Vector3.down * 1.85f;
                Vector3 bottomB = center + next * .992f + Vector3.down * 1.85f;
                b.SurfaceTriangle(center + Vector3.up * Ground, topB, topA, p.Grass);
                b.SurfaceQuad(topA, topB, shoulderB, shoulderA, Color.Lerp(p.Grass, p.Earth, .23f));
                b.SurfaceQuad(shoulderA, shoulderB, lowB, lowA, Color.Lerp(p.Earth, p.RockTop, .12f + .1f * Noise(i, 37)));
                b.SurfaceQuad(lowA, lowB, bottomB, bottomA, Color.Lerp(p.Earth, p.Rock, .42f));
            }
        }

        static Vector3 Edge(int index, int count, float w, float d)
        {
            float a = index * Mathf.PI * 2 / count;
            float x = Mathf.Cos(a), z = Mathf.Sin(a);
            // Rounded corners stay completely outside the logical playable rectangle.
            int quadrant=(index%count)/(count/4);
            float sx=quadrant==0||quadrant==3?1:-1,sz=quadrant<2?1:-1;
            const float radius=.66f;
            return new Vector3(sx*(w-radius)+x*radius,0,sz*(d-radius)+z*radius);
        }

        static void GroundCover(DioramaMeshBuilder b, Vector3 center, float w, float d, Palette p, int theme)
        {
            // Low-contrast, broad organic patches; no checkerboard or separate GameObjects.
            int patchCount=w>20?176:92;
            for (int i = 0; i < patchCount; i++)
            {
                float x = (Noise(i, 421) * 2 - 1) * (w - 1.5f);
                float z = (Noise(i, 631) * 2 - 1) * (d - 1.5f);
                float radius = .45f + Noise(i, 733) * 1.35f;
                Color color = Color.Lerp(p.Grass, i % 3 == 0 ? p.Earth : p.LightGrass, .06f + Noise(i, 997) * .09f);
                SoftDisc(b, center + new Vector3(x, Ground + .001f + i * .00001f, z), new Vector2(radius, radius * .65f), color, p.Grass, 10);
            }
        }

        static void SoftDisc(DioramaMeshBuilder b, Vector3 center, Vector2 radius, Color inner, Color outer, int sides = 12)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, next = (i + 1) * Mathf.PI * 2 / sides;
                Vector3 p = center + new Vector3(Mathf.Cos(a) * radius.x, 0, Mathf.Sin(a) * radius.y);
                Vector3 q = center + new Vector3(Mathf.Cos(next) * radius.x, 0, Mathf.Sin(next) * radius.y);
                b.GradientTriangle(center, q, p, inner, outer, outer);
            }
        }

        static void Shadow(DioramaMeshBuilder b, Vector3 v, float radius, Color ground, float height, Color shade)
        {
            Color dark = Color.Lerp(ground, shade, .4f);
            SoftDisc(b, v + new Vector3(.16f * height, .009f, -.09f * height),
                new Vector2(radius * 1.1f, radius * .73f), dark, ground);
            SoftDisc(b, v + Vector3.up * .011f, new Vector2(radius * .6f, radius * .45f),
                Color.Lerp(ground, shade, .58f), dark, 10);
        }

        static void ReservedScenery(DioramaMeshBuilder b,MapDefinition map,Palette p)
        {
            // These are the same immutable rectangles used by PlacementValidator. Every
            // solid/crown silhouette is constrained to them; there are no invisible
            // decorative blockers elsewhere on a free-placement map.
            var areas=map.BlockedAreas;
            for(int index=0;index<areas.Length;index++)
            {
                MapRectangle area=areas[index];
                float width=area.MaxX-area.MinX,height=area.MaxY-area.MinY;
                Vector3 center=DioramaResources.Point(new MapPoint((area.MinX+area.MaxX)/2,(area.MinY+area.MaxY)/2),Ground+.006f);
                Color soil=Color.Lerp(p.Grass,index%3==0?p.Rock:p.Leaf,.22f);
                ForestFloor(b,center,width/100f,height/100f,soil,p.Grass);
                if (MapLandmarkArt.Replaces(map,index)) continue;
                float terrace=Mathf.Min(.7f,height/320f);
                Terrace(b,area,terrace,p,index);
                int count=index%3==0?3:7;
                for(int j=0;j<count;j++)
                {
                    int seed=index*17+j;
                    bool rock=index%3==0;
                    float radiusFactor=rock?1.3f:map.Theme==1&&seed%3==0?.85f:map.Theme==1?.68f:.53f;
                    float heightFactor=rock?1.7f:map.Theme==1?1.6f:2.12f;
                    // Both board cameras use 45 degrees. 55 logical units of projected
                    // height per world unit leaves margin down to about 43 degrees.
                    float maxSize=Mathf.Min(1.8f,Mathf.Min((width-20-terrace*4)/(radiusFactor*100+heightFactor*4),
                        (height-20-terrace*55)/(radiusFactor*100+heightFactor*55)));
                    if(maxSize<=.08f)continue;
                    float size=maxSize*(j<3?1:.72f+Noise(seed,83)*.24f);
                    float radius=radiusFactor*size,tall=heightFactor*size;
                    float logicalRadius=radius*50;
                    float x=Mathf.Lerp(area.MinX,area.MaxX,.12f+Noise(seed,101)*.76f);
                    float y=Mathf.Lerp(area.MinY,area.MaxY,.23f+Noise(seed,179)*.66f);
                    x=Mathf.Clamp(x,area.MinX+logicalRadius+(tall+terrace)*4+8,area.MaxX-logicalRadius-8);
                    y=Mathf.Clamp(y,area.MinY+logicalRadius+(tall+terrace)*55+8,area.MaxY-logicalRadius-8);
                    var point=new MapPoint(Mathf.RoundToInt(x),Mathf.RoundToInt(y));
                    Vector3 v=DioramaResources.Point(point,Ground+TerraceHeight(area,terrace,x,y));
                    Shadow(b,v,radius,soil,tall,p.Shade);
                    if(rock)
                    {
                        Boulder(b,v,new Vector3(size,size*1.55f,size*.77f),p,seed);
                        Boulder(b,v+new Vector3(size*.72f,0,-.15f),new Vector3(.45f,.38f,.42f)*size,p,seed+2);
                    }
                    else if(map.Theme==1)Cactus(b,v,size,p,seed);
                    else Pine(b,v,size,p,map.Theme==2,seed);
                }
                // Low plants fill the marked undergrowth, making the reserved ground
                // legible instead of leaving a bare, apparently buildable rectangle.
                for(int j=0;j<18;j++)
                {
                    float x=Mathf.Lerp(area.MinX+24+terrace*4,area.MaxX-24,Noise(j,index+41));
                    float y=Mathf.Lerp(area.MinY+30+terrace*55,area.MaxY-24,Noise(j,index+211));
                    Shrub(b,DioramaResources.Point(new MapPoint(Mathf.RoundToInt(x),Mathf.RoundToInt(y)),Ground+TerraceHeight(area,terrace,x,y)),.32f,p,index*19+j);
                }
            }
        }

        static void BorderLandscape(DioramaMeshBuilder b,int width,int height,Palette p,int theme)
        {
            // A continuous backdrop outside the logical board gives the playable clearings
            // a forest/canyon setting. Tall silhouettes project away from the northern edge.
            for(int i=0;i<=20;i++)
            {
                if(i%7==3||i%7==4)continue;
                float size=1.12f+Noise(i,829)*.48f;
                Vector3 v=DioramaResources.Point(new MapPoint(Mathf.RoundToInt(i*width/20f+(Noise(i,137)-.5f)*46),Mathf.RoundToInt(-47-Noise(i,877)*14)),Ground);
                Shadow(b,v,.7f,p.Grass,size*2.12f,p.Shade);
                if(theme==1)
                {
                    Boulder(b,v,new Vector3(.65f,1.65f+Noise(i,919)*.8f,.67f),p,i+101);
                    Cactus(b,v+new Vector3(.48f,0,-.1f),.85f,p,i);
                }
                else Pine(b,v,size,p,theme==2,i+101);
                Shrub(b,v+new Vector3(.52f,0,-.42f),.43f,p,i);
                if(i%3==0)Boulder(b,v+new Vector3(.48f,0,.15f),new Vector3(.5f,.72f,.44f),p,i+87);
            }
            for(int side=0;side<2;side++)
                for(int i=0;i<11;i++)
                {
                    if((i+side*2)%5==2)continue;
                    int edge=58+Mathf.RoundToInt(Noise(i,side+761)*12);
                    Vector3 v=DioramaResources.Point(new MapPoint(side==0?-edge:width+edge,Mathf.RoundToInt(30+i*(height-60)/10f+(Noise(i,781)-.5f)*34)),Ground);
                    if(i%3==0||theme==1)Boulder(b,v,new Vector3(.57f,1.4f+Noise(i,571)*.8f,.72f),p,i+side*37);
                    else Pine(b,v, .85f+Noise(i,631)*.35f,p,theme==2,i+side*53);
                    Shrub(b,v+new Vector3(side==0?.35f:-.35f,0,.3f),.33f,p,i);
                }
            // Foreground stays low: at 43+ degrees these rocks cannot cover legal towers.
            for(int i=0;i<18;i++)
            {
                if(i%5==2||i%5==3)continue;
                Vector3 v=DioramaResources.Point(new MapPoint(Mathf.RoundToInt(i*width/17f+(Noise(i,833)-.5f)*60),height+59+Mathf.RoundToInt(Noise(i,879)*15)),Ground);
                Boulder(b,v,new Vector3(.38f+Noise(i,913)*.08f,.36f,.34f),p,i+211);
                Shrub(b,v+new Vector3(.36f,0,0),.27f,p,i);
            }
        }

        static float HillHeight(MapRectangle area,float rise,float x,float y)
        {
            float left=area.MinX+rise*4+5,right=area.MaxX-5;
            float near=area.MinY+rise*55+5,far=area.MaxY-5;
            if(x<=left||x>=right||y<=near||y>=far)return 0;
            float nx=(x-(left+right)*.5f)/((right-left)*.5f);
            float ny=(y-(near+far)*.5f)/((far-near)*.5f);
            float radius=Mathf.Sqrt(nx*nx+ny*ny);
            // Rounded shoulders, irregular rocky flanks and a grassy upper clearing.
            float edge=1+.055f*Mathf.Sin(nx*9+ny*5)+.035f*Mathf.Cos(ny*13-nx*4);
            float t=Mathf.Clamp01((edge-radius)/.38f);
            return rise*t*t*(3-2*t);
        }

        static float TerraceHeight(MapRectangle area,float rise,float x,float y)
        {
            // Match the mesh triangles exactly so trunks and stones sit on the hillside.
            float gx=Mathf.Clamp((x-area.MinX)/(area.MaxX-area.MinX)*12,0,11.999f);
            float gy=Mathf.Clamp((y-area.MinY)/(area.MaxY-area.MinY)*10,0,9.999f);
            int ix=Mathf.FloorToInt(gx),iy=Mathf.FloorToInt(gy);
            float tx=gx-ix,ty=gy-iy;
            float x0=Mathf.Lerp(area.MinX,area.MaxX,ix/12f),x1=Mathf.Lerp(area.MinX,area.MaxX,(ix+1)/12f);
            float y0=Mathf.Lerp(area.MinY,area.MaxY,iy/10f),y1=Mathf.Lerp(area.MinY,area.MaxY,(iy+1)/10f);
            float a=HillHeight(area,rise,x0,y0),bb=HillHeight(area,rise,x1,y0);
            float c=HillHeight(area,rise,x1,y1),d=HillHeight(area,rise,x0,y1);
            return tx>=ty?a+(bb-a)*tx+(c-bb)*ty:a+(d-a)*ty+(c-d)*tx;
        }

        static void Terrace(DioramaMeshBuilder b,MapRectangle area,float rise,Palette p,int seed)
        {
            for(int iy=0;iy<10;iy++)for(int ix=0;ix<12;ix++)
            {
                float x0=Mathf.Lerp(area.MinX,area.MaxX,ix/12f),x1=Mathf.Lerp(area.MinX,area.MaxX,(ix+1)/12f);
                float y0=Mathf.Lerp(area.MinY,area.MaxY,iy/10f),y1=Mathf.Lerp(area.MinY,area.MaxY,(iy+1)/10f);
                float ha=HillHeight(area,rise,x0,y0),hb=HillHeight(area,rise,x1,y0);
                float hc=HillHeight(area,rise,x1,y1),hd=HillHeight(area,rise,x0,y1);
                if(Mathf.Max(Mathf.Max(ha,hb),Mathf.Max(hc,hd))<.002f)continue;
                Vector3 a=new Vector3((x0-500)/50,Ground+ha+.002f,(300-y0)/50);
                Vector3 bb=new Vector3((x1-500)/50,Ground+hb+.002f,(300-y0)/50);
                Vector3 c=new Vector3((x1-500)/50,Ground+hc+.002f,(300-y1)/50);
                Vector3 d=new Vector3((x0-500)/50,Ground+hd+.002f,(300-y1)/50);
                Color rock=Color.Lerp(p.Rock,p.RockTop,.35f+Noise(ix+iy*12,seed)*.25f);
                Color ca=Color.Lerp(rock,p.Grass,Mathf.Clamp01((ha/rise-.35f)*2));
                Color cb=Color.Lerp(rock,p.Grass,Mathf.Clamp01((hb/rise-.35f)*2));
                Color cc=Color.Lerp(rock,p.Grass,Mathf.Clamp01((hc/rise-.35f)*2));
                Color cd=Color.Lerp(rock,p.Grass,Mathf.Clamp01((hd/rise-.35f)*2));
                b.GradientTriangle(a,bb,c,ca,cb,cc);b.GradientTriangle(a,c,d,ca,cc,cd);
            }
        }

        static void ForestFloor(DioramaMeshBuilder b,Vector3 center,float width,float depth,Color inner,Color outer)
        {
            // Broad irregular feathering avoids rendering the logical blocker AABB as
            // a dark rectangular tile. Foliage and shadows still occupy the same area.
            const int sides=18;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,next=(i+1)*Mathf.PI*2/sides;
                float ra=.87f+Noise(i,463)*.12f,rb=.87f+Noise((i+1)%sides,463)*.12f;
                Vector3 va=center+new Vector3(Mathf.Cos(a)*width*ra,0,Mathf.Sin(a)*depth*ra);
                Vector3 vb=center+new Vector3(Mathf.Cos(next)*width*rb,0,Mathf.Sin(next)*depth*rb);
                b.GradientTriangle(center,vb,va,inner,outer,outer);
            }
        }

        static void Pine(DioramaMeshBuilder b, Vector3 v, float size, Palette p, bool snow, int seed)
        {
            b.Cylinder(v + Vector3.up * size * .32f, size * .095f, size * .64f, C(0x81634D), 6);
            for (int tier = 0; tier < 4; tier++)
            {
                float radius = size * (.47f - tier * .086f);
                float y = size * (.57f + tier * .38f);
                Color color = Color.Lerp(p.Leaf, p.LightLeaf, .10f + tier * .13f + Noise(seed, 13) * .06f);
                // Sculpted boughs: alternating low tips, a curved shoulder and a narrow
                // crown. The original radius/height envelope is deliberately retained.
                const int sides=8;
                Vector3 crown=v+Vector3.up*(y+size*.35f);
                for(int i=0;i<sides;i++)
                {
                    float a=(i/(float)sides+seed*.086f+tier*.033f)*Mathf.PI*2;
                    float next=a+Mathf.PI*2/sides;
                    Vector3 radialA=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                    Vector3 radialB=new Vector3(Mathf.Cos(next),0,Mathf.Sin(next));
                    Vector3 lowA=v+radialA*radius+Vector3.up*(y-size*(i%2==0?.35f:.27f));
                    Vector3 lowB=v+radialB*radius+Vector3.up*(y-size*(i%2==0?.27f:.35f));
                    Vector3 highA=v+radialA*radius*.63f+Vector3.up*(y+size*.04f);
                    Vector3 highB=v+radialB*radius*.63f+Vector3.up*(y+size*.04f);
                    Color tip=Color.Lerp(color,p.LightLeaf,.12f),shade=Color.Lerp(color,p.Shade,.10f);
                    b.GradientTriangle(lowA,highA,highB,shade,color,color);
                    b.GradientTriangle(lowA,highB,lowB,shade,color,shade);
                    b.GradientTriangle(highA,crown,highB,color,tip,color);
                    b.SurfaceTriangle(v+Vector3.up*(y-size*.20f),lowA,lowB,shade);
                }
                if (snow && tier > 0)
                    b.Cylinder(v + Vector3.up * (y + size * .12f), radius * .76f, size * .5f,
                        C(0xF3F2E4), 7, radius * .1f, Quaternion.Euler(0, seed * 31 + tier * 12, 0));
            }
        }

        static void Boulder(DioramaMeshBuilder b, Vector3 origin, Vector3 scale, Palette p, int seed)
        {
            const int sides = 7;
            Vector3[] bottom = new Vector3[sides], mid = new Vector3[sides], top = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float angle = (i / (float)sides + Noise(seed, 9)) * Mathf.PI * 2;
                float radius = .82f + Noise(i + seed * 7, 29) * .25f;
                Vector3 radial = new Vector3(Mathf.Cos(angle) * scale.x, 0, Mathf.Sin(angle) * scale.z) * radius;
                bottom[i] = origin + radial;
                mid[i] = origin + radial * .96f + Vector3.up * scale.y * (.62f + Noise(i, seed + 31) * .14f);
                top[i] = origin + radial * .70f + new Vector3(-.12f * scale.x, scale.y, .05f * scale.z);
            }
            Vector3 crown = origin + new Vector3(-.08f * scale.x, scale.y * 1.055f, .03f * scale.z);
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                Color side = Color.Lerp(p.Rock, p.RockTop, Noise(i, seed) * .12f);
                b.SurfaceQuad(bottom[i], mid[i], mid[n], bottom[n], side);
                b.SurfaceQuad(mid[i], top[i], top[n], mid[n], Color.Lerp(side, p.RockTop, .5f));
                b.SurfaceTriangle(crown, top[n], top[i], Color.Lerp(p.RockTop, p.LightGrass, .025f + Noise(i, seed + 7) * .04f));
            }
            // A short irregular ridge sells a weathered rock face at mobile resolution.
            b.Box(crown + new Vector3(.03f, .006f, -.07f), new Vector3(scale.x * .64f, .014f, .025f),
                Color.Lerp(p.Rock, p.RockTop, .24f), Quaternion.Euler(0, seed * 27, 0));
        }

        static void Cactus(DioramaMeshBuilder b, Vector3 v, float size, Palette p, int seed)
        {
            if (seed % 3 == 0) { Boulder(b, v, new Vector3(size * .7f, size * 1.4f, size * .55f), p, seed); return; }
            b.Cylinder(v + Vector3.up * size * .65f, .18f * size, size * 1.3f, p.Leaf, 7, .13f * size);
            for (int side = -1; side <= 1; side += 2)
            {
                float y = size * (side < 0 ? .62f : .85f);
                b.Box(v + new Vector3(side * size * .27f, y, 0), new Vector3(size * .52f, size * .14f, size * .15f), p.LightLeaf);
                b.Cylinder(v + new Vector3(side * size * .46f, y + size * .2f, 0), size * .12f, size * .49f, p.Leaf, 6, size * .08f);
            }
            b.Ball(v + Vector3.up * size * 1.32f, Vector3.one * size * .13f, C(0xD98545), 6, 3);
        }

        static void Shrub(DioramaMeshBuilder b, Vector3 v, float size, Palette p, int seed)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = (i / 3f + seed * .17f) * Mathf.PI * 2;
                Vector3 pos = v + new Vector3(Mathf.Cos(a) * size * .4f, size * .3f, Mathf.Sin(a) * size * .4f);
                b.Cylinder(pos, size * .5f, size * .48f, Color.Lerp(p.Leaf, p.LightLeaf, .38f + i * .2f), 5, size * .1f,
                    Quaternion.Euler(Mathf.Sin(a) * 28, seed * 21, Mathf.Cos(a) * 28));
            }
            if (seed % 7 == 0)
                b.Ball(v + new Vector3(.12f, size * .57f, -.08f), Vector3.one * .075f, C(0x9574BC), 5, 3);
        }

        static void TrailDetails(DioramaMeshBuilder b, MapPoint[] path, MapPoint[] slots, Palette p, int theme,bool freePlacement)
        {
            int serial = 0;
            for (int segment = 1; segment < path.Length; segment++)
            {
                Vector3 a = DioramaResources.Point(path[segment - 1], Ground + .033f);
                Vector3 next = DioramaResources.Point(path[segment], Ground + .033f);
                Vector3 direction = (next - a).normalized, side = Vector3.Cross(Vector3.up, direction);
                float length = Vector3.Distance(a, next);
                for (float distance = .6f; distance < length - .35f; distance += .95f)
                {
                    int n = serial++;
                    Vector3 v = a + direction * distance;
                    float offset = (Noise(n, 47) - .5f) * .73f;
                    b.Box(v + side * offset, new Vector3(.09f + Noise(n, 51) * .12f, .008f, .035f),
                        Color.Lerp(p.Trail, p.Earth, .22f), Quaternion.LookRotation(direction));
                    if (n % 4 != 0) continue;
                    Vector3 verge = v + side * (n % 8 == 0 ? .92f : -.92f);
                    MapPoint point = Logical(verge);
                    if (!ClearScenery(point, .19f, .3f, path, slots, 36)) continue;
                    Boulder(b, verge - Vector3.up * .035f, new Vector3(.17f, .13f, .16f), p, n);
                    Vector3 fence=v+side*(n%32==0?1.2f:-1.2f);
                    if (!freePlacement && n % 16 == 0 && ClearScenery(Logical(fence),.5f,.48f,path,slots,28))
                    {
                        for(int post=-1;post<=1;post+=2)
                            b.Box(fence+direction*post*.38f+Vector3.up*.21f,new Vector3(.075f,.42f,.075f),C(0xD5BD87));
                        b.Box(fence+Vector3.up*.25f,new Vector3(.06f,.075f,.84f),C(0xB69765),Quaternion.LookRotation(direction));
                        b.Box(fence+Vector3.up*.39f,new Vector3(.055f,.065f,.84f),C(0xDFCE9C),Quaternion.LookRotation(direction));
                    }
                }
            }
        }

        static void Landmarks(DioramaMeshBuilder b, int width, int height, MapPoint[] path, MapPoint[] slots, Palette p, int theme)
        {
            // Large silhouette groups are deliberately at the rim, leaving the central
            // build space useful. Keep every group's entire radius outside gameplay.
            for (int i = 0; i < 8; i++)
            {
                int x = Mathf.RoundToInt(width * (.08f + (i % 4) * .28f));
                int y = i < 4 ? 18 : height - 18;
                var point = new MapPoint(x, y);
                if (!ClearScenery(point, 2.1f, 2.4f, path, slots)) continue;
                Vector3 v = DioramaResources.Point(point, Ground);
                Shadow(b, v, 1.85f, p.Grass, 2, p.Shade);
                Boulder(b, v, new Vector3(1.55f, 1.52f, 1.0f), p, i * 11);
                Boulder(b, v + new Vector3(.9f, 0, .5f), new Vector3(.9f, .87f, .76f), p, i * 11 + 3);
                if (theme != 1) Pine(b, v + new Vector3(-.7f, .15f, .2f), .84f, p, theme == 2, i);
            }
        }

        static void EntryAndKeep(DioramaMeshBuilder b, MapPoint[] path, Palette p, int theme)
        {
            Vector3 entry = DioramaResources.Point(path[0], Ground + .02f);
            Vector3 enterDirection = (DioramaResources.Point(path[1]) - DioramaResources.Point(path[0])).normalized;
            Vector3 enterSide = Vector3.Cross(Vector3.up, enterDirection);
            Quaternion entranceRotation = Quaternion.LookRotation(enterDirection);
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 post = entry + enterSide * .68f * i;
                b.Cylinder(post + Vector3.up * .42f, .16f, .84f, p.RockTop, 6);
                b.Box(post + Vector3.up * .98f, new Vector3(.07f, .8f, .07f), C(0x81634D));
                b.Box(post + enterSide * .16f + Vector3.up * 1.15f, new Vector3(.35f, .37f, .035f), C(0xD98545), entranceRotation);
            }

            Vector3 keep = DioramaResources.Point(path[path.Length - 1], Ground + .03f);
            b.Cylinder(keep, .74f, .13f, p.Rock, 12);
            b.Box(keep + Vector3.up * .46f, new Vector3(.93f, .92f, .94f), C(0xB5A089));
            b.Cylinder(keep + Vector3.up * 1.14f, .85f, .56f, theme == 1 ? C(0xD98545) : C(0x477D83), 4, .1f, Quaternion.Euler(0,45,0));
            b.Box(keep + new Vector3(0,.39f,-.48f),new Vector3(.33f,.58f,.025f),C(0x203C35));
            b.Box(keep + new Vector3(.48f,.53f,-.14f),new Vector3(.025f,.22f,.2f),C(0xFFF3D8));
            b.Cylinder(keep + new Vector3(.3f,1.46f,.06f), .032f, .9f, C(0x6B5943), 5);
            b.Box(keep + new Vector3(.52f,1.72f,.06f),new Vector3(.42f,.26f,.025f),C(0x56BDCE));
            // Broad masonry, timber door and roof courses remain readable when the map
            // is enlarged. These are merged with terrain, not extra objects or textures.
            for(int side=-1;side<=1;side+=2)
            {
                b.BevelBox(keep+new Vector3(side*.405f,.48f,-.405f),new Vector3(.18f,.94f,.18f),.025f,p.RockTop);
                for(int row=0;row<3;row++)
                    b.Box(keep+new Vector3(side*.466f,.20f+row*.25f,0),new Vector3(.015f,.024f,.80f),Color.Lerp(p.Rock,p.RockTop,.25f));
            }
            b.BevelBox(keep+new Vector3(0,.09f,-.60f),new Vector3(.51f,.12f,.24f),.025f,p.RockTop);
            b.Box(keep+new Vector3(0,.36f,-.503f),new Vector3(.25f,.49f,.025f),C(0x9C724C));
            b.Box(keep+new Vector3(0,.34f,-.522f),new Vector3(.26f,.055f,.018f),C(0xD8BD82));
            b.Box(keep+new Vector3(0,.36f,-.525f),new Vector3(.018f,.47f,.014f),C(0x634B39));
            b.BevelBox(keep+new Vector3(.481f,.53f,-.14f),new Vector3(.035f,.30f,.28f),.01f,C(0x634B39));
            b.Box(keep+new Vector3(.503f,.53f,-.14f),new Vector3(.015f,.22f,.20f),C(0xE6C56C));
            b.Box(keep+new Vector3(.514f,.53f,-.14f),new Vector3(.012f,.025f,.21f),C(0xFFF3D8));
            b.Box(keep+new Vector3(.514f,.53f,-.14f),new Vector3(.012f,.23f,.025f),C(0xFFF3D8));
            b.BevelBox(keep+Vector3.up*.91f,new Vector3(1.04f,.10f,1.04f),.03f,C(0x665848));
            Color roof=theme==1?C(0xD98545):C(0x477D83);
            for(int course=0;course<3;course++)
            {
                float t=.19f+course*.23f;
                float radius=Mathf.Lerp(.85f,.1f,t)+.014f;
                b.Tube(keep+Vector3.up*(.86f+.56f*t),radius,radius-.025f,.025f,Color.Lerp(roof,C(0xFFF3D8),.19f),4,Quaternion.Euler(0,45,0));
            }
        }

        internal static bool ClearScenery(MapPoint p, float radius, float height, MapPoint[] path, MapPoint[] slots, float routeMargin = 43)
        {
            float r = radius * 50;
            var center = new Vector2(p.X, p.Y);
            // Camera looks from negative Z: raised silhouettes project toward smaller Y.
            // A 1.4 slope is conservative for camera elevations of at least 36 degrees.
            var crown = center + new Vector2(-height * 4, -height * 50 * 1.4f);
            for (int i = 1; i < path.Length; i++)
            {
                var a = new Vector2(path[i-1].X, path[i-1].Y);
                var next = new Vector2(path[i].X, path[i].Y);
                float limit = routeMargin + r;
                if (SegmentDistance(center, crown, a, next) < limit) return false;
            }
            for (int i = 0; i < slots.Length; i++)
                if (DistanceToSegment(new Vector2(slots[i].X, slots[i].Y), center, crown) < r + 48) return false;
            return true;
        }

        static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 next)
        {
            Vector2 delta = next - a;
            float t = delta.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector2.Dot(point - a, delta) / delta.sqrMagnitude) : 0;
            return (point - a - delta * t).magnitude;
        }

        static float SegmentDistance(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
        {
            Vector2 ab=b-a,cd=d-c,ac=c-a;
            float cross=ab.x*cd.y-ab.y*cd.x;
            if(Mathf.Abs(cross)>.0001f)
            {
                float t=(ac.x*cd.y-ac.y*cd.x)/cross;
                float u=(ac.x*ab.y-ac.y*ab.x)/cross;
                if(t>=0&&t<=1&&u>=0&&u<=1)return 0;
            }
            return Mathf.Min(Mathf.Min(DistanceToSegment(a,c,d),DistanceToSegment(b,c,d)),
                Mathf.Min(DistanceToSegment(c,a,b),DistanceToSegment(d,a,b)));
        }

        static MapPoint Logical(Vector3 world) => new MapPoint(Mathf.RoundToInt(world.x * 50 + 500), Mathf.RoundToInt(300 - world.z * 50));
    }
}
