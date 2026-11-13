using System;
using System.Collections.Generic;
using DefenseDuell.Core;
using UnityEngine;

namespace DefenseDuell.Presentation
{
    // Original static landmarks for selected existing blockers; never gameplay geometry.
    // Integration: retain ForestFloor, skip old Terrace/props for Replaces areas, then Append.
    internal static partial class MapLandmarkArt
    {
        internal struct Face
        {
            internal Vector3 A, B, C;
            internal Color Color, ColorB, ColorC;
            internal Face(Vector3 a, Vector3 b, Vector3 c, Color color)
            { A = a; B = b; C = c; Color = ColorB = ColorC = color; }
            internal Face(Vector3 a,Vector3 b,Vector3 c,Color ca,Color cb,Color cc)
            { A=a;B=b;C=c;Color=ca;ColorB=cb;ColorC=cc; }
        }

        internal static void Append(DioramaMeshBuilder builder, MapDefinition map)
        {
            for (int index = 0; index < map.BlockedAreas.Length; index++)
            {
                var faces = Create(map, index);
                if (faces == null) continue;
                foreach (var f in faces) builder.GradientTriangle(f.A, f.B, f.C, f.Color, f.ColorB, f.ColorC);
            }
        }

        internal static bool Replaces(MapDefinition map, int areaIndex)
        {
            return map.FreePlacement && areaIndex >= 0 && areaIndex < map.BlockedAreas.Length
                && ((map.Id == "meadow" && (areaIndex == 0 || areaIndex == 5))
                    || (map.Id == "desert" && (areaIndex == 1 || areaIndex == 6))
                    || (map.Id == "frost" && (areaIndex == 5 || areaIndex == 6))
                    || IsExpandedMap(map));
        }

        // Exposed internally for geometry evidence; coordinates are already world-space.
        internal static List<Face> Create(MapDefinition map, int areaIndex)
        {
            if (!Replaces(map, areaIndex)) return null;
            var shape = new Shape(map.Theme);
            if (IsExpandedMap(map)) ExpandedLandmark(shape,map.Theme,areaIndex,map.BlockedAreas[areaIndex]);
            else if (map.Id == "meadow" && areaIndex == 5) Mill(shape);
            else if (map.Id == "meadow" && areaIndex == 0) Pond(shape, false);
            else if (map.Id == "desert" && areaIndex == 6) Ruin(shape);
            else if (map.Id == "desert" && areaIndex == 1) Mesa(shape);
            else if (map.Id == "frost" && areaIndex == 6) Harbour(shape);
            else if (map.Id == "frost" && areaIndex == 5) Pond(shape, true);
            else return null;
            return Fit(shape.Faces, map.BlockedAreas[areaIndex],IsExpandedMap(map)?6f:2.3f);
        }

        internal static bool IsExpandedMap(MapDefinition map)=>map.Id=="emerald-basin"||map.Id=="amber-canyon"||map.Id=="aurora-bay";

        static Color C(uint value) => new Color(((value >> 16) & 255) / 255f,
            ((value >> 8) & 255) / 255f, (value & 255) / 255f, 1);

        static List<Face> Fit(List<Face> faces, MapRectangle area,float maximumHeight)
        {
            // Bound physical footprint AND height-projected footprint. 70 logical units
            // per height covers >=36 degrees; +/-6 covers the board's slight yaw.
            // This is more conservative than the existing scenery's 55/4 at >=43 degrees.
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue, highest = 0;
            foreach (var f in faces) foreach (var v in new[] { f.A, f.B, f.C })
            {
                minX = Math.Min(minX, v.x * 50 - v.y * 6);
                maxX = Math.Max(maxX, v.x * 50 + v.y * 6);
                minY = Math.Min(minY, -v.z * 50 - v.y * 70);
                maxY = Math.Max(maxY, -v.z * 50);
                highest = Math.Max(highest, v.y);
            }
            const float margin = 10;
            float scale = Math.Min((area.MaxX - area.MinX - margin * 2) / (maxX - minX),
                (area.MaxY - area.MinY - margin * 2) / (maxY - minY));
            if (highest > 0) scale = Math.Min(scale, maximumHeight / highest);
            if (scale <= 0) return null;
            float x = (area.MinX + area.MaxX - (minX + maxX) * scale) * .5f;
            float y = (area.MinY + area.MaxY - (minY + maxY) * scale) * .5f;
            Vector3 origin = new Vector3((x - 500) / 50, .19f, (300 - y) / 50);
            var result = new List<Face>(faces.Count);
            foreach (var f in faces) result.Add(new Face(origin + f.A * scale,
                origin + f.B * scale, origin + f.C * scale, f.Color, f.ColorB, f.ColorC));
            return result;
        }

        static void Mill(Shape s)
        {
            s.Disc(0, .02f, 0, 1.8f, 1.05f, C(0x385C40));
            s.Disc(0, .04f, 0, 1.5f, .86f, C(0x71924B));
            s.Box(-.25f, .55f, 0, 1.12f, 1.1f, .96f, C(0xE8D5A6));
            s.Roof(-.25f, 1.1f, 0, 1.42f, .6f, 1.22f, C(0xAE5436));
            s.Box(-.25f, .52f, -.5f, .12f, 1, .04f, C(0x674832));
            s.Box(-.25f, .38f, -.51f, 1.1f, .07f, .04f, C(0x85613F));
            s.Box(-.25f, .91f, -.51f, 1.1f, .07f, .04f, C(0x85613F));
            s.Box(-.6f, .28f, -.51f, .22f, .5f, .04f, C(0x3A3830));
            s.Box(.04f, .7f, -.51f, .22f, .23f, .04f, C(0xF4BC54));
            // Static mill sails give a unique, readable silhouette without an animator.
            for (int i = 0; i < 4; i++)
            {
                double angle = Math.PI * (.125 + i * .5);
                float dx = (float)Math.Cos(angle), dy = (float)Math.Sin(angle);
                s.Beam(new Vector3(-.25f, 1.15f, -.68f),
                    new Vector3(-.25f + dx, 1.15f + dy, -.68f), .075f, C(0x664833));
                Vector3 p = new Vector3(-.25f + dx * .4f, 1.15f + dy * .4f, -.7f);
                Vector3 q = new Vector3(-.25f + dx, 1.15f + dy, -.7f);
                Vector3 side = new Vector3(-dy * .2f, dx * .2f, 0);
                s.Quad(p, q, q + side, p + side, C(0xF6E7BC));
                s.Quad(p + side, q + side, q, p, C(0xD4BB86));
            }
            for (int i = 0; i < 3; i++)
                s.Tree(.9f + i * .3f, .28f + (i % 2) * .3f, .72f + i * .12f, false);
            s.Box(-1.02f, .12f, -.12f, .42f, .24f, .35f, C(0xC99D48));
            // Large, readable joinery rather than texture noise: every detail is part
            // of this same mesh and is fitted inside the original reserved clearing.
            s.Box(-.25f,.19f,-.505f,.28f,.38f,.045f,C(0x4B3830));
            s.Box(-.25f,.19f,-.534f,.20f,.32f,.025f,C(0x9B7246));
            s.Box(-.25f,.07f,-.59f,.38f,.12f,.22f,C(0xBAA584));
            s.Box(.32f,.65f,.08f,.035f,.33f,.33f,C(0x674832));
            s.Box(.344f,.65f,.08f,.018f,.23f,.23f,C(0x98C5C0));
            s.Box(.358f,.65f,.08f,.018f,.025f,.25f,C(0xFFF3D8));
            s.Box(.358f,.65f,.08f,.018f,.25f,.025f,C(0xFFF3D8));
            s.Box(-.25f,1.105f,0,1.48f,.075f,1.26f,C(0x674832));
            RoofCourses(s,-.25f,1.1f,0,1.42f,.6f,1.22f,C(0xD08455));
            s.Cylinder(-.25f,1.15f,-.74f,.105f,.16f,.09f,10,C(0xE7BC59));
            for(int i=0;i<4;i++)
            {
                float angle=(float)(i*Math.PI*.5+.28),dx=(float)Math.Cos(angle),dy=(float)Math.Sin(angle);
                Vector3 side=new Vector3(-dy*.2f,dx*.2f,0);
                for(int j=1;j<=3;j++)
                {
                    Vector3 a=new Vector3(-.25f+dx*(.4f+j*.15f),1.15f+dy*(.4f+j*.15f),-.715f);
                    s.Beam(a,a+side,.025f,C(0xB99B6B));
                }
            }
        }

        static void Pond(Shape s, bool frost)
        {
            s.Disc(0, .02f, 0, 1.9f, 1.25f, C(frost ? 0x527786u : 0x466A49u));
            s.Disc(0, .06f, 0, 1.67f, 1.04f, C(frost ? 0xDDECE4u : 0xABAE76u));
            s.Disc(0, .08f, 0, 1.48f, .87f, C(frost ? 0x78B8CBu : 0x3B8D91u));
            s.Disc(-.14f, .09f, -.08f, 1.12f, .59f, C(frost ? 0xAFDBDDu : 0x5BAAA0u));
            for (int i = 0; i < 7; i++)
            {
                double a = i * Math.PI * 2 / 7;
                float x = (float)Math.Cos(a) * 1.65f, z = (float)Math.Sin(a) * 1.07f;
                s.Cylinder(x, .18f, z, .23f, .36f, .16f, 5, C(frost ? 0xD4E3E0u : 0x89967Au));
            }
            if (frost)
            {
                for (int i = 0; i < 3; i++)
                    s.Cylinder(-.9f + .4f * i, .45f + i * .12f, .8f, .3f, .9f + i * .24f, 0, 5, C(0x97CDD5));
                s.Beam(new Vector3(-1.15f, .102f, .1f), new Vector3(.8f, .102f, -.25f), .025f, C(0xEEF8ED));
                s.Beam(new Vector3(0, .102f, -.1f), new Vector3(.55f, .102f, .56f), .025f, C(0xEEF8ED));
            }
            else
            {
                s.Tree(-1.3f, .65f, .8f, false);
                for (int i = 0; i < 4; i++)
                    s.Box(1.2f + i * .08f, .22f, -.4f + i * .12f, .045f, .4f, .045f, C(0xB3B765));
                s.Disc(.48f, .102f, .2f, .2f, .13f, C(0x91B569));
            }
            // Broken arcs leave broad quiet water between highlights; no animation,
            // transparency layers or per-frame allocation are needed.
            s.Arc(-.14f,.105f,-.08f,1.04f,.54f,.025f,.05f,1.25f,C(frost?0xDDEFEAu:0x95CEC0u));
            s.Arc(-.14f,.106f,-.08f,.78f,.38f,.018f,2.5f,4.2f,C(frost?0xC0E4E6u:0x81BFB7u));
            for(int i=0;i<5;i++)
            {
                float a=.35f+i*1.21f,x=(float)Math.Cos(a)*1.4f,z=(float)Math.Sin(a)*.85f;
                s.Box(x,.115f,z,.18f,.07f,.13f,C(frost?0xF2F5EBu:0xC2BB92u));
            }
            if(!frost)
            {
                s.Disc(.48f,.108f,.2f,.07f,.055f,C(0xF0D7A0));
                s.Disc(.71f,.104f,.39f,.14f,.10f,C(0x7AA45A));
                s.Disc(.71f,.108f,.39f,.035f,.03f,C(0xE9CDE1));
            }
        }

        static void Ruin(Shape s)
        {
            s.Disc(0, .02f, 0, 1.9f, 1.25f, C(0xA5714D));
            s.Box(0, .12f, 0, 3.2f, .24f, 1.8f, C(0xBD8958));
            s.Box(0, .27f, 0, 2.8f, .16f, 1.5f, C(0xE1B77C));
            s.Box(-.92f, .98f, .08f, .54f, 1.36f, .62f, C(0xD5A367));
            s.Box(.92f, .9f, .08f, .54f, 1.2f, .62f, C(0xD5A367));
            s.Box(-.14f, 1.64f, .08f, 2.13f, .32f, .7f, C(0xECCA8C));
            s.Box(.55f, 1.44f, .08f, .55f, .2f, .62f, C(0xB87B49));
            s.Box(-.92f, .98f, -.24f, .18f, .72f, .025f, C(0x9F673D));
            s.Box(.92f, .9f, -.24f, .18f, .6f, .025f, C(0x9F673D));
            s.Cylinder(-.42f, .47f, -.54f, .23f, .28f, .18f, 6, C(0xECCA8C));
            s.Box(.45f, .46f, -.6f, .45f, .22f, .27f, C(0xC7945E));
            s.Box(-1.5f, .27f, -.6f, .35f, .45f, .4f, C(0xBC834E));
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*.92f;
                s.Box(x,.39f,.08f,.67f,.14f,.74f,C(0xB87B49));
                s.Box(x,1.44f,.08f,.67f,.13f,.74f,C(0xF0CC89));
                for(int row=0;row<3;row++)
                    s.Box(x,.63f+row*.24f,-.239f,.53f,.026f,.018f,C(0xAA734A));
            }
            s.Box(-.14f,1.83f,.08f,2.18f,.09f,.74f,C(0xF0D4A0));
            // A central carved seal and broken step define the ruin at overview scale.
            s.Box(-.14f,1.64f,-.285f,.31f,.25f,.035f,C(0xAE7047));
            s.Box(-.14f,1.64f,-.309f,.19f,.15f,.025f,C(0xEAC789));
            s.Box(0,.12f,-.98f,1.1f,.16f,.32f,C(0xC7945E));
            s.Box(0,.225f,-.82f,.9f,.08f,.26f,C(0xE4BB83));
        }

        static void Mesa(Shape s)
        {
            s.Disc(0, .02f, 0, 1.9f, 1.23f, C(0xBB8D59));
            s.Cylinder(0, .28f, 0, 1.55f, .54f, 1.34f, 7, C(0xAA6042));
            s.Cylinder(0, .68f, 0, 1.34f, .26f, 1.2f, 7, C(0xE4B06E));
            s.Cylinder(0, .99f, 0, 1.2f, .36f, 1.05f, 7, C(0xC47D4F));
            s.Cylinder(0, 1.23f, 0, 1.08f, .12f, 1.03f, 7, C(0xF0CC89));
            s.Cylinder(.84f, .26f, -.86f, .48f, .5f, .32f, 5, C(0xB46D45));
            s.Box(-1.3f, .35f, -.36f, .15f, .7f, .15f, C(0x64835B));
            s.Box(-1.15f, .36f, -.36f, .3f, .12f, .12f, C(0x64835B));
            s.Cylinder(0,.795f,0,1.235f,.045f,1.22f,7,C(0xF0C58A));
            s.Cylinder(-.35f,1.32f,.11f,.38f,.10f,.31f,6,C(0xE9C795));
            for(int i=0;i<3;i++)
            {
                float x=-.60f+i*.40f;
                s.Box(x,1.296f,.26f,.23f,.018f,.035f,C(0xC69462));
            }
            s.Box(.72f,.11f,-.99f,.31f,.17f,.25f,C(0xD3A16C));
        }

        static void Harbour(Shape s)
        {
            s.Disc(0, .02f, 0, 1.9f, 1.25f, C(0x4C7885));
            s.Disc(0, .04f, 0, 1.65f, 1.03f, C(0x83B7C4));
            s.Box(-.78f, .18f, .15f, 1.34f, .28f, 1.68f, C(0x72898C));
            s.Box(-.78f, .34f, .15f, 1.39f, .08f, 1.72f, C(0xDDECE5));
            s.Box(-.8f, .79f, .35f, .84f, .83f, .78f, C(0xAD7654));
            s.Roof(-.8f, 1.21f, .35f, 1.12f, .38f, 1.05f, C(0xE8F1E9));
            s.Box(-.8f, .79f, -.05f, .24f, .3f, .025f, C(0xF2C56C));
            for (int i = 0; i < 6; i++)
                s.Box(.15f + i * .21f, .19f, -.52f, .18f, .12f, .48f, C(i % 2 == 0 ? 0x88745Cu : 0xAA9674u));
            for (int i = 0; i < 3; i++)
                s.Box(.25f + i * .43f, .2f, -.72f, .065f, .4f, .065f, C(0x544D43));
            // Pointed shallow hull with pale gunwale and two bench seats.
            Vector3[] hull={new Vector3(.28f,.16f,.25f),new Vector3(.5f,.16f,.02f),new Vector3(1.12f,.16f,.02f),
                new Vector3(1.42f,.16f,.25f),new Vector3(1.12f,.16f,.49f),new Vector3(.5f,.16f,.49f)};
            for(int i=0;i<hull.Length;i++)
            {
                Vector3 a=hull[i],b=hull[(i+1)%hull.Length];
                s.Quad(a,b,b+Vector3.up*.18f,a+Vector3.up*.18f,C(0x79583E));
                s.Quad(a+Vector3.up*.18f,b+Vector3.up*.18f,b,a,C(0x79583E));
                s.Beam(a+Vector3.up*.18f,b+Vector3.up*.18f,.04f,C(0xBA9A70));
            }
            s.Box(.67f,.25f,.25f,.1f,.05f,.43f,C(0xB09064));
            s.Box(1.04f,.25f,.25f,.1f,.05f,.43f,C(0xB09064));
            s.Box(-.35f,.48f,-.48f,.23f,.25f,.23f,C(0xB18B59));
            s.Tree(-1.48f, .6f, .65f, true);
            RoofCourses(s,-.8f,1.21f,.35f,1.12f,.38f,1.05f,C(0xB7CFCC));
            s.Box(-.8f,1.215f,.35f,1.16f,.07f,1.09f,C(0x6B655C));
            // Window casing and log seams, with an inset pane on both visible sides.
            s.Box(-.8f,.79f,-.064f,.32f,.38f,.035f,C(0x5B493D));
            s.Box(-.8f,.79f,-.087f,.23f,.29f,.025f,C(0xF2C56C));
            s.Box(-.8f,.79f,-.106f,.025f,.30f,.018f,C(0xFFF3D8));
            s.Box(-.36f,.76f,.35f,.035f,.35f,.27f,C(0x5B493D));
            s.Box(-.337f,.76f,.35f,.022f,.25f,.19f,C(0x91BDBE));
            for(int row=0;row<3;row++)s.Box(-.8f,.48f+row*.23f,-.047f,.83f,.025f,.016f,C(0x895E46));
            s.Box(-1.08f,1.42f,.52f,.19f,.43f,.20f,C(0x9C8E81));
            s.Box(-1.08f,1.64f,.52f,.25f,.08f,.26f,C(0xEFF4EB));
            s.Beam(new Vector3(.45f,.37f,.07f),new Vector3(1.18f,.37f,.43f),.035f,C(0xDBC495));
            s.Arc(.82f,.055f,.25f,.72f,.43f,.022f,.2f,2.3f,C(0xC0E1DC));
            s.Box(-.35f,.615f,-.48f,.25f,.035f,.25f,C(0xDDC89B));
        }

        static void RoofCourses(Shape s,float x,float y,float z,float w,float h,float d,Color color)
        {
            // Four raised courses are sufficient to read as crafted shingles at phone
            // size; full individual tiles would introduce clutter and wasted geometry.
            for(int side=-1;side<=1;side+=2)for(int row=1;row<=4;row++)
            {
                float t=row/5f;
                var a=new Vector3(x+side*w*.5f*(1-t),y+h*t+.012f,z-d*.5f);
                s.Beam(a,a+Vector3.forward*d,.022f,color);
            }
            s.Beam(new Vector3(x,y+h+.018f,z-d*.5f),new Vector3(x,y+h+.018f,z+d*.5f),.07f,color);
        }

        sealed class Shape
        {
            internal readonly List<Face> Faces = new List<Face>();
            readonly Color ground;
            internal Shape(int theme){ground=C(theme==1?0xD3B180u:theme==2?0xD8E8E7u:0x719C50u);}
            internal void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
            { Faces.Add(new Face(a, b, c, color)); Faces.Add(new Face(a, c, d, color)); }
            internal void Box(float x, float y, float z, float w, float h, float d, Color color)
            {
                if(Math.Min(w,Math.Min(h,d))>=.14f)
                {BeveledBox(new Vector3(x,y,z),new Vector3(w,h,d),color);return;}
                Vector3 p = new Vector3(x, y, z);
                var v = new[] { new Vector3(-w,-h,-d), new Vector3(w,-h,-d), new Vector3(w,-h,d), new Vector3(-w,-h,d),
                    new Vector3(-w,h,-d), new Vector3(w,h,-d), new Vector3(w,h,d), new Vector3(-w,h,d) };
                for (int i = 0; i < v.Length; i++) v[i] = p + v[i] * .5f;
                Quad(v[0],v[1],v[2],v[3],color); Quad(v[4],v[7],v[6],v[5],color);
                Quad(v[0],v[4],v[5],v[1],color); Quad(v[1],v[5],v[6],v[2],color);
                Quad(v[2],v[6],v[7],v[3],color); Quad(v[3],v[7],v[4],v[0],color);
            }
            void BeveledBox(Vector3 center,Vector3 size,Color color)
            {
                // Only large architectural solids receive bevels. Narrow seams and
                // trim stay simple, so the readable edges cost hundreds, not thousands,
                // of additional triangles per landmark group.
                Vector3 half=size*.5f;float bevel=Math.Min(.045f,Math.Min(half.x,Math.Min(half.y,half.z))*.28f);
                Vector3 inner=half-Vector3.one*bevel;
                Vector3 Axis(int axis,float value){var v=Vector3.zero;v[axis]=value;return v;}
                void T(Vector3 a,Vector3 b,Vector3 c,Vector3 normal,Color paint)
                {
                    if(Vector3.Dot(Vector3.Cross(b-a,c-a),normal)<0){var swap=b;b=c;c=swap;}
                    Faces.Add(new Face(center+a,center+b,center+c,paint));
                }
                void Q(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,Color paint)
                {T(a,b,c,normal,paint);T(a,c,d,normal,paint);}
                for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2)
                {
                    int u=(axis+1)%3,v=(axis+2)%3;Vector3 f=Axis(axis,half[axis]*sign);
                    Q(f-Axis(u,inner[u])-Axis(v,inner[v]),f+Axis(u,inner[u])-Axis(v,inner[v]),
                        f+Axis(u,inner[u])+Axis(v,inner[v]),f-Axis(u,inner[u])+Axis(v,inner[v]),Axis(axis,sign),color);
                }
                Color edge=Color.Lerp(color,C(0xFFF3D8),.12f);
                for(int along=0;along<3;along++)for(int su=-1;su<=1;su+=2)for(int sv=-1;sv<=1;sv+=2)
                {
                    int u=(along+1)%3,v=(along+2)%3;
                    Vector3 a=Axis(u,su*half[u])+Axis(v,sv*inner[v]);
                    Vector3 c=Axis(u,su*inner[u])+Axis(v,sv*half[v]),e=Axis(along,inner[along]);
                    Q(a-e,a+e,c+e,c-e,Axis(u,su)+Axis(v,sv),edge);
                }
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    T(new Vector3(x*half.x,y*inner.y,z*inner.z),new Vector3(x*inner.x,y*half.y,z*inner.z),
                        new Vector3(x*inner.x,y*inner.y,z*half.z),new Vector3(x,y,z),edge);
            }
            internal void Disc(float x,float y,float z,float rx,float rz,Color color)
            {
                const int sides=24;
                for(int i=0;i<sides;i++)
                {
                    double a=i*Math.PI*2/sides,b=(i+1)*Math.PI*2/sides;
                    Color edge=y<=.04f?ground:color;
                    Faces.Add(new Face(new Vector3(x,y,z),new Vector3(x+(float)Math.Cos(b)*rx,y,z+(float)Math.Sin(b)*rz),
                        new Vector3(x+(float)Math.Cos(a)*rx,y,z+(float)Math.Sin(a)*rz),color,edge,edge));
                }
            }
            internal void Arc(float x,float y,float z,float rx,float rz,float width,float from,float to,Color color)
            {
                for(int i=0;i<10;i++)
                {
                    float a=from+(to-from)*i/10,b=from+(to-from)*(i+1)/10;
                    Vector3 P(float t,float scale)=>new Vector3(x+(float)Math.Cos(t)*rx*scale,y,z+(float)Math.Sin(t)*rz*scale);
                    float inner=1-width/Math.Min(rx,rz);
                    Quad(P(a,inner),P(b,inner),P(b,1),P(a,1),color);
                }
            }
            internal void Cylinder(float x,float y,float z,float radius,float height,float topRadius,int sides,Color color)
            {
                for(int i=0;i<sides;i++)
                {
                    double a=i*Math.PI*2/sides,b=(i+1)*Math.PI*2/sides;
                    Vector3 p=new Vector3(x+(float)Math.Cos(a)*radius,y-height/2,z+(float)Math.Sin(a)*radius);
                    Vector3 q=new Vector3(x+(float)Math.Cos(b)*radius,y-height/2,z+(float)Math.Sin(b)*radius);
                    Vector3 r=new Vector3(x+(float)Math.Cos(b)*topRadius,y+height/2,z+(float)Math.Sin(b)*topRadius);
                    Vector3 t=new Vector3(x+(float)Math.Cos(a)*topRadius,y+height/2,z+(float)Math.Sin(a)*topRadius);
                    if(topRadius>0)Quad(p,t,r,q,color);
                    else Faces.Add(new Face(p,t,q,color));
                    if(topRadius>0)Faces.Add(new Face(new Vector3(x,y+height/2,z),r,t,color));
                    Faces.Add(new Face(new Vector3(x,y-height/2,z),p,q,color));
                }
            }
            internal void Roof(float x,float y,float z,float w,float h,float d,Color color)
            {
                Vector3 a=new Vector3(x-w/2,y,z-d/2),b=new Vector3(x+w/2,y,z-d/2),
                    c=new Vector3(x,y+h,z-d/2),e=new Vector3(x-w/2,y,z+d/2),
                    f=new Vector3(x+w/2,y,z+d/2),g=new Vector3(x,y+h,z+d/2);
                Quad(a,e,g,c,color);Quad(c,g,f,b,color);
                Faces.Add(new Face(a,c,b,color));Faces.Add(new Face(e,f,g,color));
            }
            internal void Beam(Vector3 a,Vector3 b,float width,Color color)
            {
                // Axis-independent prism; horizontal ice lines and vertical sails.
                Vector3 direction=b-a;
                Vector3 u=Vector3.Cross(direction, Math.Abs(direction.y)>.9f*direction.magnitude ? Vector3.forward : Vector3.up).normalized*width*.5f;
                Vector3 v=Vector3.Cross(direction,u).normalized*width*.5f;
                Quad(a-u-v,a+u-v,b+u-v,b-u-v,color);Quad(a+u-v,a+u+v,b+u+v,b+u-v,color);
                Quad(a+u+v,a-u+v,b-u+v,b+u+v,color);Quad(a-u+v,a-u-v,b-u-v,b-u+v,color);
                Quad(a-u-v,a-u+v,a+u+v,a+u-v,color);Quad(b-u-v,b+u-v,b+u+v,b-u+v,color);
            }
            internal void Tree(float x,float z,float height,bool frost,float ground=0)
            {
                Cylinder(x,ground+height*.22f,z,.09f,height*.44f,.08f,5,C(0x756044));
                Cylinder(x,ground+height*.59f,z,height*.36f,height*.82f,0,7,C(frost?0x56878Bu:0x3B704Eu));
                if(frost)Cylinder(x,ground+height*.79f,z,height*.24f,height*.46f,0,7,C(0xDAEBE5));
            }
        }
    }
}
