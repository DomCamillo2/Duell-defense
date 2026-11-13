using System;
using BigInteger=System.Numerics.BigInteger;
using UnityEngine;

namespace DefenseDuell.Presentation
{
    // Source candidate, to compile inside Presentation with the shared internal mesh builder.
    // No static Unity ownership: caller registers and releases the returned mesh exactly once.
    public static class EliteUnitGeometry
    {
        public const int FirstKind=4,LastKind=5;
        public const float MaximumHeight=1.95f,MaximumRadius=.98f;
        public const int MaximumTriangles=4300;
        public static int Stage(BigInteger level)=>level<=0?0:level<5?1:level<10?2:level<20?3:level<30?4:5;
        public static int VisualStage(int kind,BigInteger level)=>kind>=FirstKind?Stage(level):(int)BigInteger.Min(10,BigInteger.Max(0,level));
        public static int StageCount(int kind)=>kind>=FirstKind?6:11;
        public static int LevelForVisualStage(int kind,int stage)
        {
            if(stage<0||stage>=StageCount(kind))throw new ArgumentOutOfRangeException(nameof(stage));
            return kind>=FirstKind?new[]{0,1,5,10,20,30}[stage]:stage;
        }
        public static Mesh Build(int kind,BigInteger researchLevel)
        {
            if(kind<FirstKind||kind>LastKind||researchLevel<0)throw new ArgumentOutOfRangeException(nameof(kind));
            var builder=new DioramaMeshBuilder();int stage=Stage(researchLevel);
            if(kind==4)Storm(builder,stage);else Siege(builder,stage);
            return builder.Build((kind==4?"Sturmkoloss":"Belagerungskoloss")+" research-stage"+stage);
        }
        static Color C(uint hex)=>new Color32((byte)(hex>>16),(byte)(hex>>8),(byte)hex,255);
        static readonly Color Steel=C(0x56636C),Dark=C(0x263C50),Gold=C(0xE7BC59),Cream=C(0xFFF3D8),Teal=C(0x38CFC1),Orange=C(0xD98545),Deep=C(0x17262C);
        static void Plate(DioramaMeshBuilder b,Vector3 point,Vector3 size,Color color,float bevel=.035f)
        {b.Finish=.45f;b.BevelBox(point,size,bevel,color);}
        static void Storm(DioramaMeshBuilder b,int stage)
        {
            // Low forward-leaning armored runner: distinct split feet, fins and lit visor.
            // Small exhaust housings indicate speed; no flight/invulnerability implied.
            for(int side=-1;side<=1;side+=2)
            {
                Plate(b,new Vector3(side*.27f,.13f,.15f),new Vector3(.32f,.24f,.70f),Dark);
                Plate(b,new Vector3(side*.27f,.34f,-.03f),new Vector3(.20f,.32f,.25f),Steel);
                Plate(b,new Vector3(side*.27f,.55f,-.10f),new Vector3(.24f,.30f,.32f),Teal,.045f);
                Plate(b,new Vector3(side*.49f,.86f,-.04f),new Vector3(.24f,.41f,.40f),Dark);
                Plate(b,new Vector3(side*.49f,.98f,.12f),new Vector3(.27f,.23f,.36f),Teal,.05f);
                Plate(b,new Vector3(side*.48f,.69f,.23f),new Vector3(.21f,.27f,.28f),Steel);
                b.Finish=.72f;b.Tube(new Vector3(side*.26f,.91f,-.35f),.12f,.075f,.22f,Gold,10,Quaternion.Euler(90,0,0));
                if(stage>=1)Plate(b,new Vector3(side*.42f,1.12f,-.13f),new Vector3(.11f,.30f,.34f),Cream,.025f);
                if(stage>=2)b.Shard(new Vector3(side*.41f,1.24f,-.22f),new Vector3(.09f,.24f,.11f),Teal,Quaternion.Euler(0,0,-side*18));
                if(stage>=3)Plate(b,new Vector3(side*.31f,.25f,.40f),new Vector3(.35f,.12f,.26f),Gold,.025f);
                if(stage>=4)Plate(b,new Vector3(side*.55f,.94f,-.01f),new Vector3(.14f,.40f,.46f),Cream);
                for(int i=0;i<stage;i++)Plate(b,new Vector3(side*.615f,.77f+i*.066f,.06f),new Vector3(.024f,.032f,.22f),Gold,.006f);
            }
            Plate(b,new Vector3(0,.78f,-.03f),new Vector3(.66f,.56f,.48f),Dark,.07f);
            Plate(b,new Vector3(0,.96f,.24f),new Vector3(.51f,.31f,.12f),Teal,.04f);
            Plate(b,new Vector3(0,1.25f,.15f),new Vector3(.45f,.34f,.40f),Steel,.06f);
            Plate(b,new Vector3(0,1.28f,.36f),new Vector3(.37f,.075f,.04f),Teal,.012f);
            Plate(b,new Vector3(0,1.45f,.12f),new Vector3(.27f,.12f,.38f),Gold,.025f);
            if(stage>=3)Plate(b,new Vector3(0,1.04f,-.34f),new Vector3(.33f,.45f,.20f),Cream);
            if(stage>=5){b.Finish=.75f;b.Shard(new Vector3(0,1.64f,.02f),new Vector3(.11f,.18f,.15f),Teal);}
        }
        static void Siege(DioramaMeshBuilder b,int stage)
        {
            // Heavy walker with armor plates, grounded feet and broad shield forearms.
            // Shields are silhouette only: no new damage block/immunity mechanic.
            for(int side=-1;side<=1;side+=2)
            {
                Plate(b,new Vector3(side*.30f,.13f,.08f),new Vector3(.42f,.24f,.64f),Dark);
                Plate(b,new Vector3(side*.30f,.38f,-.02f),new Vector3(.34f,.36f,.34f),Steel);
                Plate(b,new Vector3(side*.30f,.60f,.08f),new Vector3(.37f,.33f,.39f),Orange,.045f);
                Plate(b,new Vector3(side*.58f,1.06f,-.07f),new Vector3(.32f,.36f,.48f),Gold,.05f);
                Plate(b,new Vector3(side*.57f,.76f,.20f),new Vector3(.33f,.51f,.29f),Dark);
                Plate(b,new Vector3(side*.57f,.86f,.37f),new Vector3(.40f,.62f,.17f),Orange,.045f);
                Plate(b,new Vector3(side*.58f,.87f,.47f),new Vector3(.13f,.44f,.045f),Cream,.012f);
                if(stage>=1)Plate(b,new Vector3(side*.33f,.34f,.29f),new Vector3(.40f,.25f,.16f),Gold);
                if(stage>=2)Plate(b,new Vector3(side*.62f,1.29f,-.04f),new Vector3(.34f,.17f,.47f),Cream);
                if(stage>=3)Plate(b,new Vector3(side*.63f,.91f,.48f),new Vector3(.30f,.14f,.13f),Gold,.025f);
                if(stage>=4)Plate(b,new Vector3(side*.47f,.69f,-.30f),new Vector3(.27f,.65f,.22f),Steel);
                for(int i=0;i<stage;i++)Plate(b,new Vector3(side*.79f,.74f+i*.065f,.28f),new Vector3(.035f,.035f,.22f),Gold,.007f);
            }
            Plate(b,new Vector3(0,.84f,-.02f),new Vector3(.91f,.66f,.65f),Dark,.07f);
            Plate(b,new Vector3(0,1.01f,.34f),new Vector3(.72f,.35f,.16f),Steel,.045f);
            Plate(b,new Vector3(0,1.05f,.44f),new Vector3(.48f,.22f,.065f),Orange,.018f);
            Plate(b,new Vector3(0,1.38f,.03f),new Vector3(.52f,.40f,.49f),Dark,.06f);
            Plate(b,new Vector3(0,1.40f,.29f),new Vector3(.38f,.075f,.04f),Orange,.012f);
            Plate(b,new Vector3(0,1.61f,.02f),new Vector3(.60f,.12f,.50f),Gold);
            if(stage>=3)Plate(b,new Vector3(0,1.19f,-.38f),new Vector3(.54f,.42f,.22f),Steel);
            if(stage>=5){b.Finish=.72f;b.Shard(new Vector3(0,1.77f,.02f),new Vector3(.14f,.16f,.14f),Gold);}
        }
    }
}
