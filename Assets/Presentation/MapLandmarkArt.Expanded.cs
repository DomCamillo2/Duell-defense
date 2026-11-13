using System;
using UnityEngine;
using DefenseDuell.Core;

namespace DefenseDuell.Presentation
{
    internal static partial class MapLandmarkArt
    {
        // Native, deterministic geometry. Fit() constrains every vertex and its projected
        // silhouette to the same immutable blocker used by placement on both peers.
        static void ExpandedLandmark(Shape s,int theme,int area,MapRectangle footprint)
        {
            if(area!=4&&area!=5){LandscapeMass(s,theme,area,(float)(footprint.MaxX-footprint.MinX)/(footprint.MaxY-footprint.MinY));return;}
            if(theme==0){if(area==4)EmeraldLake(s);else EmeraldCascade(s);}
            else if(theme==1){if(area==4)CanyonCitadel(s);else AmberOasis(s);}
            else {if(area==4)AuroraBeacon(s);else AuroraGlacier(s);}
        }

        // Broad authored landforms give the reserved ground an unmistakable silhouette.
        // The entire mass (including trees, water and cliff faces) is fitted as one object.
        static void Shelf(Shape s,float x,float z,float rx,float rz,float h,Color rock,Color top)
        {
            const int count=9;var center=new Vector3(x,h*.91f,z);
            Vector3 P(int i,float scale,float height)
            {
                double a=i*Math.PI*2/count;float wobble=1+((i*7)%5-2)*.045f;
                return new Vector3(x+(float)Math.Cos(a)*rx*scale*wobble,height,z+(float)Math.Sin(a)*rz*scale*wobble);
            }
            for(int i=0;i<count;i++)
            {
                var a=P(i,1,.025f);var b=P((i+1)%count,1,.025f);
                var c=P((i+1)%count,.8f,h*(.89f+(i%3)*.055f));var d=P(i,.8f,h*(.89f+((i+2)%3)*.055f));
                s.Quad(a,d,c,b,Color.Lerp(rock,top,(i%3)*.11f));
                s.Faces.Add(new Face(center,c,d,top));
            }
        }
        static void LandscapeMass(Shape s,int theme,int area,float aspect)
        {
            float spread=Math.Max(1.15f,Math.Min(4.6f,aspect*1.45f+1.0f));
            var stone=C(theme==1?0xAE7050u:theme==2?0x658D9Eu:0x6E8267u);
            var top=C(theme==1?0xE4B77Cu:theme==2?0xDCECE8u:0x819D62u);
            s.Disc(0,.02f,0,spread+1.0f,1.75f,C(theme==1?0xAA8055u:theme==2?0x6CA4B3u:0x426B4Du));
            int count=aspect>1.7f?5:3;
            for(int i=0;i<count;i++)
            {
                float x=-spread+2*spread*i/(count-1),z=.28f+(i%2)*.47f;
                float h=.65f+((area+i*3)%5)*.21f;
                Shelf(s,x,z,.9f,.92f,h,stone,top);
                if(theme==1)
                {
                    Shelf(s,x+.08f,z+.06f,.62f,.65f,h+.65f,stone,C(0xECCB92));
                    s.Cylinder(x-.16f,h+.94f,z+.08f,.31f,.72f,.22f,6,C(0xC38D59));
                    if(i%2==0)Palm(s,x+.68f,z-.5f,1.0f);
                }
                else
                {
                    s.Tree(x-.12f,z,h>1?1.8f:2.15f,theme==2,h*.92f);
                    s.Tree(x+.55f,z-.43f,1.0f+(i%2)*.35f,theme==2,.1f);
                    if(theme==2)s.Cylinder(x-.64f,.72f,z-.8f,.32f,1.4f,.06f,5,C(0xA3D5DE));
                }
            }
            // A low, continuous front edge reads as rocky ground, never an invisible blocker.
            for(int i=0;i<8;i++)Shelf(s,-spread-.4f+i*(2*spread+.8f)/7,-.9f+(i%2)*.2f,.35f,.38f,.22f+(i%3)*.09f,stone,top);
            if(theme==2)
            {s.Disc(0,.06f,-.6f,spread*.68f,.66f,C(0x5597AF));s.Arc(0,.075f,-.6f,spread*.56f,.43f,.045f,.1f,2.8f,C(0xC7E7E7));}
        }
        static void EmeraldLake(Shape s)
        {
            s.Disc(0,.02f,0,3.0f,2.05f,C(0x375E4A));
            s.Disc(.05f,.06f,-.3f,2.55f,1.55f,C(0xA2AF78));
            s.Disc(.05f,.085f,-.3f,2.3f,1.35f,C(0x357E8B));
            s.Disc(.2f,.10f,-.45f,1.98f,1.06f,C(0x49AAA4));
            Shelf(s,-.95f,1.05f,1.25f,.83f,1.12f,C(0x6D816A),C(0x9DA779));
            Shelf(s,.68f,1.1f,1.03f,.86f,1.65f,C(0x6C7F69),C(0xA8B38B));
            s.Tree(-1.05f,1.0f,2.1f,false,1.1f);s.Tree(1.0f,1.2f,1.9f,false,1.55f);
            s.Tree(-2.05f,.4f,2.1f,false);s.Tree(2.08f,.2f,1.7f,false);
            for(int i=0;i<3;i++)
            {
                float h=1.30f-i*.39f,z=.67f-i*.36f;
                s.Box(.35f,h*.5f,z,.83f,h,.62f,C(0x80957B));
                s.Box(.35f,h+.02f,z,.61f,.04f,.56f,C(0x6AC7BA));
                s.Box(.35f,h-.22f,z-.32f,.57f,.45f,.025f,C(0xB6E9DA));
            }
            for(int i=0;i<3;i++)s.Arc(.2f,.115f,-.7f,1.32f+i*.19f,.48f+i*.12f,.035f,3.4f,5.8f,C(0x9ADCCD));
            Shelf(s,-.5f,-.55f,.54f,.38f,.18f,C(0x84996E),C(0xA6BD77));s.Tree(-.52f,-.48f,.8f,false,.17f);
        }
        static void CanyonCitadel(Shape s)
        {
            s.Disc(0,.025f,0,2.6f,1.85f,C(0xA9754B));
            Shelf(s,0,.1f,2.3f,1.65f,.65f,C(0xAA6C49),C(0xD7A267));
            Shelf(s,-.2f,.36f,1.65f,1.2f,1.35f,C(0xB77F51),C(0xE7BA7C));
            Shelf(s,-.48f,.62f,.94f,.82f,2.1f,C(0xC48D5C),C(0xEACB91));
            s.Box(-.5f,2.31f,.6f,.93f,.42f,.72f,C(0xDBBA83));
            for(int i=0;i<3;i++)s.Box(-.82f+i*.31f,2.59f,.6f,.19f,.19f,.78f,C(0xF0D8A7));
            s.Box(-.5f,2.32f,.23f,.22f,.32f,.025f,C(0x725247));
            for(int i=0;i<7;i++)s.Box(.35f+i*.13f,.18f+i*.075f,-1.32f+i*.12f,.38f,.18f,.31f,C(0xE0B884));
            Shelf(s,1.45f,.75f,.58f,.67f,1.6f,C(0xA96F4E),C(0xE7BF85));
            s.Cylinder(1.48f,2.03f,.75f,.3f,.88f,.19f,6,C(0xBD8658));
            Palm(s,-1.75f,-.55f,1.25f);
        }

        static void EmeraldSanctuary(Shape s)
        {
            s.Disc(0,.02f,0,2.35f,1.65f,C(0x426D4D));
            s.Disc(0,.055f,0,2.08f,1.42f,C(0x92A46D));
            s.Box(0,.12f,0,3.15f,.24f,2.05f,C(0xA6AE8C));
            s.Box(0,.27f,0,2.85f,.13f,1.8f,C(0xD0CEAA));
            s.Disc(.1f,.35f,-.18f,1.02f,.61f,C(0x577D6C));
            s.Disc(.1f,.36f,-.18f,.88f,.49f,C(0x43A29B));
            s.Arc(.1f,.37f,-.18f,.66f,.34f,.035f,.3f,2.8f,C(0xA1DDD0));
            for(int side=-1;side<=1;side+=2)
            {
                float x=side*1.1f;
                s.Box(x,.79f,.48f,.44f,1.04f,.52f,C(0xBFC4A3));
                s.Box(x,.35f,.48f,.64f,.14f,.69f,C(0xE0DEC1));
                s.Box(x,1.31f,.48f,.63f,.16f,.67f,C(0xE0DEC1));
                s.Box(x,.86f,.208f,.11f,.51f,.035f,C(0x5D9271));
                for(int r=0;r<3;r++)s.Box(x,.56f+r*.23f,.205f,.43f,.025f,.02f,C(0x899B7C));
            }
            s.Box(0,1.46f,.48f,2.75f,.25f,.62f,C(0xC5C9A5));
            s.Box(0,1.61f,.48f,2.87f,.07f,.69f,C(0xE2DFC1));
            s.Cylinder(0,1.48f,.135f,.13f,.065f,.13f,6,C(0xC5A95F));
            for(int i=0;i<4;i++)s.Box(0,.07f+i*.055f,-1.38f+i*.13f,1.1f,.14f,.26f,C(i%2==0?0xC0C4A1u:0xD2D2AFu));
            s.Tree(-1.78f,.54f,1.35f,false);s.Tree(1.78f,.65f,1.02f,false);
            for(int i=0;i<7;i++)
            {
                double a=i*.91;
                s.Disc((float)Math.Cos(a)*1.68f,.085f,(float)Math.Sin(a)*1.1f,.22f,.15f,C(0x709A58));
            }
        }

        static void EmeraldCascade(Shape s)
        {
            s.Disc(0,.02f,0,2.5f,1.55f,C(0x4F7653));
            s.Disc(.35f,.055f,-.4f,1.66f,.83f,C(0xB9B58D));
            s.Disc(.35f,.07f,-.4f,1.45f,.68f,C(0x469B9D));
            s.Disc(.28f,.08f,-.4f,1.1f,.42f,C(0x62B4A9));
            for(int i=0;i<3;i++)
            {
                float h=.34f+i*.34f,z=.05f+i*.35f;
                s.Box(-.4f,h*.5f,z,2.2f-i*.35f,h,.8f,C(i%2==0?0x9AA48Bu:0xB3B59Bu));
                s.Box(-.4f,h+.018f,z,.77f,.035f,.72f,C(0x73C2B9));
                s.Box(-.4f,h-.15f,z-.405f,.58f,.35f,.035f,C(0xAFDDD0));
                s.Box(-.22f,h-.15f,z-.428f,.05f,.33f,.012f,C(0xDBEFE0));
            }
            s.Tree(-1.7f,.56f,1.2f,false);s.Tree(1.72f,.4f,1.05f,false);
            // Small footbridge is decorative and remains wholly inside the pond blocker.
            for(int i=0;i<10;i++)s.Box(-1.04f+i*.23f,.24f,-1.05f,.2f,.095f,.51f,C(i%2==0?0xAD8C5Cu:0xC3A475u));
            for(int side=-1;side<=1;side+=2)
                s.Beam(new Vector3(-1.16f,.52f,-1.05f+side*.3f),new Vector3(1.16f,.52f,-1.05f+side*.3f),.055f,C(0x7F664B));
            for(int i=0;i<3;i++)s.Box(-1.05f+i*1.05f,.31f,-1.36f,.065f,.58f,.065f,C(0x7F664B));
            s.Arc(.35f,.09f,-.4f,1.08f,.42f,.025f,3.5f,5.8f,C(0xB7DFCA));
        }

        static void AmberObservatory(Shape s)
        {
            s.Disc(0,.02f,0,2.3f,1.65f,C(0xAE845A));
            s.Cylinder(0,.13f,0,1.85f,.26f,1.85f,12,C(0xC89B64));
            s.Cylinder(0,.32f,0,1.58f,.13f,1.58f,12,C(0xE0B77A));
            s.Cylinder(0,.53f,0,.45f,.31f,.32f,8,C(0xC19256));
            s.Cylinder(0,.72f,0,.69f,.12f,.69f,12,C(0xF0CC84));
            for(int i=0;i<16;i++)
            {
                float a=i*(float)Math.PI/8,b=(i+1)*(float)Math.PI/8;
                s.Beam(new Vector3((float)Math.Cos(a)*.79f,1.27f+(float)Math.Sin(a)*.65f,0),
                    new Vector3((float)Math.Cos(b)*.79f,1.27f+(float)Math.Sin(b)*.65f,0),.065f,C(0xD7AD59));
            }
            s.Beam(new Vector3(-.65f,1.27f,0),new Vector3(.65f,1.27f,0),.05f,C(0xF4D694));
            s.Beam(new Vector3(0,.67f,0),new Vector3(0,1.87f,0),.05f,C(0xF4D694));
            s.Cylinder(0,1.27f,0,.17f,.22f,.12f,8,C(0x7EB2A6));
            for(int i=0;i<12;i++)
            {
                float a=i*(float)Math.PI/6;
                s.Box((float)Math.Cos(a)*1.28f,.4f,(float)Math.Sin(a)*1.28f,.12f,.03f,.12f,C(0xA77743));
            }
            for(int side=-1;side<=1;side+=2)
            {
                s.Box(side*1.6f,.53f,.8f,.42f,1.06f,.46f,C(0xD1A269));
                s.Box(side*1.6f,1.09f,.8f,.55f,.12f,.58f,C(0xECD09A));
                s.Box(side*1.6f,.63f,.558f,.15f,.39f,.025f,C(0xAB7951));
            }
            for(int i=0;i<4;i++)s.Box(0,.07f+i*.06f,-1.95f+i*.16f,.9f,.13f,.32f,C(0xDFC18F));
        }

        static void AmberOasis(Shape s)
        {
            s.Disc(0,.02f,0,2.4f,1.55f,C(0xAC8958));
            s.Box(0,.12f,0,3.35f,.24f,2.08f,C(0xC6A575));
            s.Box(0,.265f,0,3.08f,.05f,1.8f,C(0xE6CFA0));
            s.Box(0,.3f,0,2.68f,.025f,1.39f,C(0x458F96));
            s.Box(-.2f,.315f,-.06f,2.18f,.012f,1.0f,C(0x6CB4AB));
            for(int i=0;i<8;i++)
                for(int side=-1;side<=1;side+=2)s.Box(-1.4f+i*.4f,.33f,side*.88f,.34f,.09f,.22f,C(i%2==0?0xEAD4ABu:0xCEB385u));
            Palm(s,-1.75f,.6f,1.8f);Palm(s,1.65f,.56f,1.5f);
            s.Box(1.0f,.49f,-.31f,.32f,.34f,.31f,C(0xB77D4E));
            s.Cylinder(1.0f,.73f,-.31f,.22f,.16f,.17f,8,C(0xD7AF71));
            s.Arc(0,.327f,0,.9f,.39f,.03f,.2f,2.5f,C(0xBBE2CE));
            for(int i=0;i<3;i++)s.Box(-.5f+i*.5f,.16f,-1.23f,.39f,.18f,.31f,C(0xD3B98A));
        }
        static void Palm(Shape s,float x,float z,float height)
        {
            s.Cylinder(x,height*.47f,z,.10f,height*.94f,.075f,7,C(0x9D7850));
            for(int i=0;i<6;i++)
            {
                float a=i*(float)Math.PI/3;var start=new Vector3(x,height,z);
                var tip=start+new Vector3((float)Math.Cos(a)*.68f,-.16f,(float)Math.Sin(a)*.68f);
                var side=new Vector3(-(float)Math.Sin(a)*.13f,0,(float)Math.Cos(a)*.13f);
                var mid=(start+tip)*.5f+Vector3.up*.13f;
                s.Quad(start,mid+side,tip,mid-side,C(i%2==0?0x66956Bu:0x83AB72u));
                s.Quad(mid-side,tip,mid+side,start,C(0x527D5D));
            }
        }

        static void AuroraBeacon(Shape s)
        {
            s.Disc(0,.02f,0,2.2f,1.6f,C(0x658F9C));
            s.Disc(.2f,.045f,-.1f,1.85f,1.2f,C(0x8FC4CB));
            s.Box(-.5f,.16f,.1f,1.9f,.32f,1.6f,C(0x8D9D9C));
            s.Box(-.5f,.35f,.1f,1.97f,.08f,1.65f,C(0xE3EDE3));
            s.Cylinder(-.5f,.94f,.1f,.45f,1.16f,.34f,8,C(0xE6E7D4));
            s.Cylinder(-.5f,1.02f,.1f,.425f,.21f,.405f,8,C(0x4C7F8E));
            s.Cylinder(-.5f,1.54f,.1f,.57f,.12f,.57f,8,C(0x6C8991));
            s.Cylinder(-.5f,1.78f,.1f,.36f,.35f,.36f,8,C(0xF2CB7D));
            s.Cylinder(-.5f,2.04f,.1f,.59f,.25f,0,8,C(0x4C7585));
            for(int i=0;i<8;i++)
            {
                float a=i*(float)Math.PI/4;
                s.Box(-.5f+(float)Math.Cos(a)*.34f,1.78f,.1f+(float)Math.Sin(a)*.34f,.035f,.36f,.035f,C(0x506B77));
            }
            s.Box(-.5f,.63f,-.36f,.23f,.44f,.045f,C(0x657B7C));
            s.Box(-.5f,.63f,-.388f,.16f,.34f,.025f,C(0xB8D1CC));
            for(int i=0;i<10;i++)s.Box(.2f+i*.15f,.18f,-.5f,.13f,.10f,.62f,C(i%2==0?0x879089u:0xA2ABA1u));
            for(int i=0;i<4;i++)s.Box(.3f+i*.43f,.25f,-.84f,.065f,.46f,.065f,C(0x697B7D));
            s.Tree(-1.5f,.6f,.85f,true);
            s.Arc(.4f,.065f,-.15f,1.1f,.61f,.025f,.05f,2.0f,C(0xD4ECE4));
        }

        static void AuroraGlacier(Shape s)
        {
            s.Disc(0,.02f,0,2.45f,1.6f,C(0x7394A3));
            s.Disc(.1f,.05f,-.25f,2.05f,1.2f,C(0xD5E7E2));
            s.Disc(.15f,.065f,-.4f,1.68f,.82f,C(0x74B4CA));
            s.Disc(.05f,.075f,-.5f,1.29f,.49f,C(0xA9D7D8));
            for(int i=0;i<5;i++)
            {
                float x=-1.35f+i*.6f,z=.45f+(i%2)*.29f,h=.8f+(i%3)*.4f;
                s.Cylinder(x,h*.5f,z,.48f,h,.18f,5,C(i%2==0?0x98CAD7u:0xB2DCE1u));
                s.Cylinder(x,h+.08f,z,.20f,.16f,0,5,C(0xEFF5E9));
            }
            s.Beam(new Vector3(-1.1f,.09f,-.4f),new Vector3(.6f,.09f,-.65f),.025f,C(0xE3F3EB));
            s.Beam(new Vector3(.6f,.09f,-.65f),new Vector3(1.1f,.09f,-.1f),.02f,C(0xE3F3EB));
            for(int i=0;i<6;i++)s.Box(-1.2f+i*.46f,.12f,-1.2f,.31f,.15f,.26f,C(i%2==0?0xD0E1DAu:0xE7EEE3u));
            s.Tree(1.85f,.35f,.8f,true);
        }
    }
}
