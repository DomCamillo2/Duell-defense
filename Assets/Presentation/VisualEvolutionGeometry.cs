using UnityEngine;

namespace DefenseDuell.Presentation
{
    public sealed partial class DioramaResources
    {
        static Color EvolutionPaint(int kind)=>kind==5?C(0x54A89A):kind==6?Orange:kind==7?C(0x38CFC1):ClassPaint(kind);
        static void Plate(DioramaMeshBuilder b,Vector3 center,Vector3 size,Color color,float bevel=.025f)
        { b.Finish=.48f;b.BevelBox(center,size,bevel,color); }
        static void EvolutionTowerBase(DioramaMeshBuilder b,int kind,int tier)
        {
            var paint=EvolutionPaint(kind);
            // Inlaid count marks remain separate from the unlimited numeric HUD level.
            for(int i=0;i<tier;i++)
            {
                float a=(i+.5f)*Mathf.PI/5;
                Plate(b,new Vector3(Mathf.Sin(a)*.575f,.16f,Mathf.Cos(a)*.575f),new Vector3(.065f,.10f,.055f),Gold,.012f);
            }
            if(kind==4)
            {
                // Ore buckets and reinforced gantry, keeping the original rock entrance/cart.
                for(int s=-1;s<=1;s+=2)
                {
                    Plate(b,new Vector3(s*.38f,.72f,.20f),new Vector3(.10f,.35f+tier*.02f,.12f),tier>=3?Steel:LightWood);
                    if(tier>=3)Plate(b,new Vector3(s*.43f,.82f,.19f),new Vector3(.08f,.10f,.22f),Gold);
                    if(tier>=6)Plate(b,new Vector3(s*.37f,1.30f,-.16f),new Vector3(.10f,.73f,.13f),Steel);
                }
                if(tier>=3)Plate(b,new Vector3(0,1.05f,-.04f),new Vector3(.88f,.12f,.22f),Steel);
                if(tier>=6)
                {
                    Plate(b,new Vector3(0,1.67f,-.16f),new Vector3(.91f,.12f,.39f),Gold);
                    Plate(b,new Vector3(.54f,.48f,-.28f),new Vector3(.24f,.43f,.32f),Steel);
                    for(int i=0;i<3;i++){b.Finish=.75f;b.Shard(new Vector3(.51f,.68f,-.37f+i*.08f),new Vector3(.075f,.13f,.07f),Gold);}
                }
                if(tier>=10)
                {
                    Plate(b,new Vector3(0,1.83f,-.18f),new Vector3(.67f,.24f,.33f),Steel);
                    b.Finish=.75f;b.Tube(new Vector3(0,1.83f,.01f),.17f,.115f,.07f,Gold,12,Quaternion.Euler(90,0,0));
                }
                return;
            }
            if(tier>=3)
                for(int s=-1;s<=1;s+=2)Plate(b,new Vector3(s*.48f,.28f,-.26f),new Vector3(.20f,.29f,.30f),paint,.045f);
            if(tier>=6)
            {
                b.Finish=.55f;b.Tube(new Vector3(0,.34f,0),.49f,.43f,.10f,Gold,12);
                for(int s=-1;s<=1;s+=2)Plate(b,new Vector3(s*.43f,.19f,.33f),new Vector3(.22f,.18f,.24f),Steel);
            }
            if(tier>=10)
                for(int i=0;i<4;i++)
                {
                    float a=i*Mathf.PI*.5f+Mathf.PI*.25f;
                    Plate(b,new Vector3(Mathf.Cos(a)*.48f,.42f,Mathf.Sin(a)*.48f),new Vector3(.18f,.19f,.18f),Cream);
                }
        }
        static void EvolutionTowerHead(DioramaMeshBuilder b,int kind,int tier)
        {
            switch(kind)
            {
                case 0: EvolutionRapid(b,tier);break;
                case 1: EvolutionCannon(b,tier);break;
                case 2: EvolutionFrost(b,tier);break;
                case 3: EvolutionBreaker(b,tier);break;
                case 4: EvolutionMineWheel(b,tier);break;
                case 5: EvolutionTesla(b,tier);break;
                case 6: EvolutionBallista(b,tier);break;
                case 7: EvolutionLaser(b,tier);break;
            }
        }
        static void EvolutionRapid(DioramaMeshBuilder b,int tier)
        {
            for(int s=-1;s<=1;s+=2)
            {
                Plate(b,new Vector3(s*.45f,.22f,-.30f),new Vector3(.18f+tier*.008f,.27f+tier*.014f,.25f),Green,.04f);
                for(int i=0;i<(tier+1)/2;i++)Plate(b,new Vector3(s*.52f,.30f,-.40f+i*.075f),new Vector3(.07f,.14f,.035f),Gold,.008f);
                if(tier>=3)Plate(b,new Vector3(s*.44f,.49f,-.20f),new Vector3(.20f,.16f,.43f),Steel,.035f);
                if(tier>=6)Barrel(b,new Vector3(s*.24f,.53f,.36f),.66f,.09f,Quaternion.Euler(90,0,0),Cream);
            }
            if(tier>=6)Barrel(b,new Vector3(0,.74f,.38f),.69f,.09f,Quaternion.Euler(90,0,0),Cream);
            if(tier>=10)
            {
                Plate(b,new Vector3(0,.86f,-.08f),new Vector3(.47f,.13f,.45f),Gold,.04f);
                for(int s=-1;s<=1;s+=2)Plate(b,new Vector3(s*.41f,.71f,-.23f),new Vector3(.14f,.24f,.34f),Green);
            }
        }
        static void EvolutionCannon(DioramaMeshBuilder b,int tier)
        {
            var axis=Quaternion.Euler(80,0,0);
            b.Finish=.65f;b.Tube(new Vector3(0,.30f,.27f),.29f+tier*.006f,.275f,.12f+tier*.006f,Gold,16,axis);
            for(int s=-1;s<=1;s+=2)
            {
                Plate(b,new Vector3(s*.48f,.19f,-.27f),new Vector3(.17f,.25f+tier*.013f,.40f),Orange,.045f);
                if(tier>=3){b.Finish=.65f;b.Cylinder(new Vector3(s*.42f,.22f,.28f),.065f,.46f,Steel,10,-1,axis);}
                if(tier>=6)
                {
                    Plate(b,new Vector3(s*.45f,.49f,-.32f),new Vector3(.22f,.17f,.34f),Gold,.04f);
                    Plate(b,new Vector3(s*.31f,.64f,-.33f),new Vector3(.15f,.12f,.30f),Steel);
                }
            }
            if(tier>=10)
            {
                Plate(b,new Vector3(0,.29f,-.57f),new Vector3(.80f,.50f,.22f),Steel,.055f);
                Plate(b,new Vector3(0,.58f,-.53f),new Vector3(.74f,.12f,.30f),Gold,.035f);
                b.Finish=.65f;b.Tube(new Vector3(0,.34f,.53f),.37f,.29f,.12f,Cream,16,axis);
            }
        }
        static void EvolutionFrost(DioramaMeshBuilder b,int tier)
        {
            // Duration is a crystal reservoir; it does not imply a damage or stun ability.
            for(int s=-1;s<=1;s+=2)
            {
                b.Finish=.82f;b.Shard(new Vector3(s*.31f,.54f,-.12f),new Vector3(.10f+tier*.004f,.22f+tier*.025f,.11f),Blue,Quaternion.Euler(0,0,-s*12));
                if(tier>=3)Plate(b,new Vector3(s*.45f,.34f,-.20f),new Vector3(.18f,.20f,.18f),Cream);
            }
            if(tier>=3){b.Finish=.65f;b.Tube(new Vector3(0,.82f,0),.43f,.39f,.07f,Gold,16);}
            if(tier>=6)
            {
                b.Finish=.7f;b.Tube(new Vector3(0,1.24f,0),.36f,.31f,.08f,Cream,16);
                for(int i=0;i<3;i++)
                {
                    float a=i*Mathf.PI*2/3;b.Finish=.82f;
                    b.Shard(new Vector3(Mathf.Cos(a)*.26f,1.28f,Mathf.Sin(a)*.26f),new Vector3(.10f,.33f,.10f),Blue);
                }
            }
            if(tier>=10)
                for(int i=0;i<3;i++)
                {
                    float a=(i+.5f)*Mathf.PI*2/3;b.Finish=.85f;
                    b.Shard(new Vector3(Mathf.Cos(a)*.21f,1.66f,Mathf.Sin(a)*.21f),new Vector3(.085f,.32f,.085f),Color.Lerp(Blue,Cream,.22f));
                }
        }
        static void EvolutionBreaker(DioramaMeshBuilder b,int tier)
        {
            for(int s=-1;s<=1;s+=2)
            {
                b.Finish=.58f;b.Lathe(new Vector3(s*.47f,.23f,-.27f),new[]{new Vector2(0,-.20f),new Vector2(.12f+tier*.005f,-.20f),new Vector2(.12f+tier*.005f,.23f),new Vector2(0,.23f)},Purple,12);
                b.Finish=.72f;b.Tube(new Vector3(s*.47f,.42f,-.27f),.15f+tier*.005f,.11f,.07f,Gold,12);
                if(tier>=3)Plate(b,new Vector3(s*.33f,.26f,.41f),new Vector3(.09f,.16f,.65f),Purple);
                if(tier>=6)Plate(b,new Vector3(s*.43f,.48f,-.03f),new Vector3(.24f,.13f,.48f),Gold);
                if(tier>=10)
                {
                    Plate(b,new Vector3(s*.37f,.10f,.58f),new Vector3(.17f,.13f,.46f),Steel);
                    Plate(b,new Vector3(s*.37f,.22f,.70f),new Vector3(.10f,.22f,.13f),Gold);
                }
            }
            if(tier>=6)Plate(b,new Vector3(0,.64f,-.22f),new Vector3(.39f,.12f,.32f),Purple);
        }
        static void EvolutionMineWheel(DioramaMeshBuilder b,int tier)
        {
            b.Finish=.65f;b.Tube(new Vector3(0,0,.075f),.30f,.265f,.05f,Gold,16,Quaternion.Euler(90,0,0));
            for(int i=0;i<tier;i++)
            {
                float a=i*Mathf.PI/5;
                Plate(b,new Vector3(Mathf.Cos(a)*.30f,Mathf.Sin(a)*.30f,.065f),new Vector3(.07f,.07f,.065f),tier>=6?Gold:Steel,.012f);
            }
            if(tier>=6){b.Finish=.5f;b.Cylinder(new Vector3(0,0,.11f),.13f,.08f,Steel,12,-1,Quaternion.Euler(90,0,0));}
            if(tier>=10){b.Finish=.8f;b.Shard(new Vector3(0,0,.18f),new Vector3(.065f,.095f,.045f),Gold,Quaternion.Euler(90,0,0));}
        }
        static void EvolutionTesla(DioramaMeshBuilder b,int tier)
        {
            var paint=EvolutionPaint(5);
            for(int s=-1;s<=1;s+=2)
            {
                b.Finish=.6f;b.Cylinder(new Vector3(s*.40f,.30f,-.24f),.105f,.36f+tier*.025f,paint,12);
                b.Finish=.72f;b.Tube(new Vector3(s*.40f,.49f+tier*.0125f,-.24f),.13f,.08f,.065f,Gold,12);
                if(tier>=3){b.Finish=.8f;b.Body(new Vector3(s*.40f,.64f,-.24f),Vector3.one*.13f,Cream,12,6);}
                if(tier>=6)Plate(b,new Vector3(s*.41f,.17f,.19f),new Vector3(.19f,.30f,.24f),paint);
            }
            if(tier>=3){b.Finish=.65f;b.Tube(new Vector3(0,.76f,0),.38f,.34f,.055f,Gold,16);}
            if(tier>=6){b.Finish=.68f;b.Tube(new Vector3(0,1.11f,0),.38f,.32f,.07f,Cream,16);}
            if(tier>=10)
                for(int i=0;i<3;i++)
                {
                    float a=i*Mathf.PI*2/3;
                    Plate(b,new Vector3(Mathf.Cos(a)*.32f,1.20f,Mathf.Sin(a)*.32f),new Vector3(.065f,.40f,.065f),Gold,.012f);
                    b.Finish=.85f;b.Body(new Vector3(Mathf.Cos(a)*.32f,1.43f,Mathf.Sin(a)*.32f),Vector3.one*.075f,Blue,10,5);
                }
        }
        static void EvolutionBallista(DioramaMeshBuilder b,int tier)
        {
            for(int s=-1;s<=1;s+=2)
            {
                Plate(b,new Vector3(s*.25f,.23f,-.26f),new Vector3(.14f,.20f+tier*.01f,.26f),Steel);
                b.Finish=.65f;b.Cylinder(new Vector3(s*.25f,.38f+tier*.005f,-.26f),.095f,.06f,Gold,10);
            }
            int rows=tier>=6?2:tier>=3?1:0;
            for(int row=0;row<rows;row++)for(int s=-1;s<=1;s+=2)
            {
                float y=.53f+row*.20f;
                Plate(b,new Vector3(s*.27f,y,.03f),new Vector3(.58f,.09f,.11f),LightWood,.02f);
                Plate(b,new Vector3(s*.58f,y,.12f),new Vector3(.20f,.09f,.12f),Wood,.02f);
                // Tension strings sit behind the bolt channel, never across its opening.
                var a=new Vector3(s*.65f,y,.12f);var z=new Vector3(0,y,-.35f);
                b.Finish=.12f;b.Cylinder((a+z)*.5f,.012f,Vector3.Distance(a,z),Deep,5,-1,Quaternion.FromToRotation(Vector3.up,z-a));
            }
            if(tier>=10)
            {
                Plate(b,new Vector3(0,.11f,0),new Vector3(.78f,.11f,.68f),Steel);
                for(int s=-1;s<=1;s+=2)Plate(b,new Vector3(s*.55f,.74f,.13f),new Vector3(.19f,.12f,.15f),Gold);
            }
        }
        static void EvolutionPathModule(DioramaMeshBuilder b,int kind,VisualEvolutionPath path)
        {
            bool primary=path==VisualEvolutionPath.Primary||path==VisualEvolutionPath.Mixed;
            bool secondary=path==VisualEvolutionPath.Secondary||path==VisualEvolutionPath.Mixed;
            if(primary)
            {
                if(kind==1)
                {
                    // Side-mounted blast magazines stay outside the tier-ten rear armor.
                    // Keep both sides readable as the head rotates; the scope remains the secondary cue.
                    for(int side=-1;side<=1;side+=2)
                    {
                        Plate(b,new Vector3(side*.54f,.31f,-.13f),new Vector3(.28f,.12f,.24f),Steel,.02f);
                        Plate(b,new Vector3(side*.65f,.48f,.04f),new Vector3(.20f,.32f,.40f),Orange,.035f);
                        Plate(b,new Vector3(side*.65f,.665f,.04f),new Vector3(.22f,.05f,.42f),Gold,.012f);
                        Plate(b,new Vector3(side*.65f,.48f,.255f),new Vector3(.14f,.20f,.025f),Cream,.006f);
                    }
                }
                else if(kind==2){b.Finish=.82f;b.Shard(new Vector3(0,.44f,-.48f),new Vector3(.14f,.34f,.14f),Blue);}
                else if(kind==5){b.Finish=.65f;b.Tube(new Vector3(0,.42f,-.44f),.13f,.08f,.30f,Gold,12);}
                else Plate(b,new Vector3(0,.31f,-.54f),new Vector3(.29f,.27f,.15f),EvolutionPaint(kind),.04f);
            }
            if(secondary)
            {
                if(kind==2){b.Finish=.7f;b.Tube(new Vector3(0,1.04f,0),.52f,.475f,.06f,Gold,16);}
                else if(kind==5){b.Finish=.7f;b.Body(new Vector3(.25f,1.05f,-.18f),new Vector3(.12f,.075f,.12f),Blue,12,5);}
                else
                {
                    if(kind==6)
                    {
                        // Straddle the bolt channel; both feet intersect the original wooden stock.
                        for(int side=-1;side<=1;side+=2)
                            Plate(b,new Vector3(side*.16f,.55f,-.13f),new Vector3(.07f,.35f,.12f),Steel,.012f);
                        Plate(b,new Vector3(0,.71f,-.13f),new Vector3(.40f,.08f,.14f),Steel,.015f);
                    }
                    else if(kind==3)
                        // Reach the original hull, including tiers before the raised roof is added.
                        Plate(b,new Vector3(0,.61f,-.13f),new Vector3(.16f,.28f,.18f),Steel,.025f);
                    Scope(b,new Vector3(kind==1?.30f:0,kind==6?.90f:.95f,-.13f),1);
                }
            }
        }
        static void EvolutionLaser(DioramaMeshBuilder b,int tier)
        {
            var axis=Quaternion.Euler(90,0,0);
            for(int side=-1;side<=1;side+=2)
                Plate(b,new Vector3(side*.33f,.34f,-.13f),new Vector3(.15f,.22f,.56f),EvolutionPaint(7));
            b.Finish=.75f;b.Tube(new Vector3(0,.36f,.43f),.15f+tier*.008f,.10f,.10f,Cream,12,axis);
            if(tier>=3)Plate(b,new Vector3(0,.12f,0),new Vector3(.59f,.13f,.58f),Steel);
            if(tier>=6)for(int side=-1;side<=1;side+=2)
                Plate(b,new Vector3(side*.35f,.54f,-.10f),new Vector3(.13f,.12f,.36f),Gold);
            if(tier>=10){b.Finish=.85f;b.Shard(new Vector3(0,.60f,-.32f),new Vector3(.13f,.28f,.13f),EvolutionPaint(7));}
        }
        static float HighGrowth(int level)=>Mathf.Clamp((level-10)/20f,.05f,1.45f);
        static void HighTowerBase(DioramaMeshBuilder b,int kind,int level)
        {
            float growth=HighGrowth(level);var paint=EvolutionPaint(kind);
            for(int side=-1;side<=1;side+=2)
            {
                // Grounded shoulder armor; fixed building footprint and selection/placement rules.
                Plate(b,new Vector3(side*.56f,.31f,-.12f),new Vector3(.16f,.22f+growth*.16f,.68f),paint,.035f);
                Plate(b,new Vector3(side*.57f,.16f,.34f),new Vector3(.22f,.18f,.30f),Steel,.025f);
                for(int i=0;i<Mathf.Min(5,(level-10+3)/4);i++)
                    Plate(b,new Vector3(side*.66f,.29f+i*.065f,-.28f),new Vector3(.035f,.035f,.20f),Gold,.009f);
            }
            if(level>=20)for(int side=-1;side<=1;side+=2)
                Plate(b,new Vector3(side*.39f,.56f,-.40f),new Vector3(.24f,.19f,.18f),Cream,.03f);
            if(level>=30){b.Finish=.65f;b.Tube(new Vector3(0,.46f,0),.60f,.52f,.09f,Gold,16);}
            if(kind==4)
            {
                // The mine grows into a reinforced industrial pit, never a combat weapon.
                for(int side=-1;side<=1;side+=2)
                    Plate(b,new Vector3(side*.45f,1.34f,-.18f),new Vector3(.12f,.76f+growth*.18f,.19f),Steel);
                if(level>=20)Plate(b,new Vector3(0,2.01f,-.18f),new Vector3(1.10f,.17f,.42f),Gold);
                if(level>=25)for(int side=-1;side<=1;side+=2)
                    Plate(b,new Vector3(side*.57f,.64f,-.30f),new Vector3(.22f,.46f,.34f),paint);
                if(level>=40)Plate(b,new Vector3(0,2.16f,-.18f),new Vector3(.82f,.14f,.34f),Cream);
                if(level>=50)for(int side=-1;side<=1;side+=2)
                    Plate(b,new Vector3(side*.59f,1.27f,-.19f),new Vector3(.16f,.66f,.29f),Gold);
                if(level>=75)Plate(b,new Vector3(0,2.31f,-.18f),new Vector3(.60f,.16f,.32f),Steel);
                if(level>=100){b.Finish=.8f;b.Shard(new Vector3(0,2.53f,-.18f),new Vector3(.15f,.28f,.15f),Gold);}
            }
        }
        static void HighTowerHead(DioramaMeshBuilder b,int kind,int level)
        {
            float g=HighGrowth(level);var paint=EvolutionPaint(kind);var axis=Quaternion.Euler(90,0,0);
            // Silhouette milestones at15/20/25/30; interpolated dimensions reward intervening purchases.
            switch(kind)
            {
                case 0:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Plate(b,new Vector3(side*.53f,.51f,-.25f),new Vector3(.20f,.34f+g*.20f,.40f),Green);
                        if(level>=15)Barrel(b,new Vector3(side*.23f,.87f,.37f),.68f+g*.11f,.08f,axis,Cream);
                        if(level>=25)Plate(b,new Vector3(side*.57f,.88f,-.19f),new Vector3(.16f,.22f,.35f),Gold);
                    }
                    if(level>=20)Plate(b,new Vector3(0,1.02f,-.21f),new Vector3(.59f,.16f,.38f),Steel);
                    break;
                case 1:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Plate(b,new Vector3(side*.56f,.43f,-.19f),new Vector3(.19f,.36f+g*.18f,.43f),Orange);
                        if(level>=20)Plate(b,new Vector3(side*.46f,.71f,-.32f),new Vector3(.28f,.18f,.39f),Steel);
                    }
                    b.Finish=.65f;b.Tube(new Vector3(0,.35f,.67f),.39f+g*.04f,.33f,.11f,Cream,16,Quaternion.Euler(80,0,0));
                    if(level>=25)Plate(b,new Vector3(0,.84f,-.37f),new Vector3(.74f,.15f,.40f),Gold);
                    break;
                case 2:
                    for(int i=0;i<4;i++)
                    {
                        float a=i*Mathf.PI*.5f;b.Finish=.82f;
                        b.Shard(new Vector3(Mathf.Cos(a)*.39f,1.39f,Mathf.Sin(a)*.39f),new Vector3(.11f,.28f+g*.22f,.11f),Blue);
                    }
                    if(level>=20){b.Finish=.7f;b.Tube(new Vector3(0,1.73f,0),.40f,.34f,.07f,Cream,16);}
                    if(level>=25){b.Finish=.85f;b.Shard(new Vector3(0,1.98f,0),new Vector3(.18f,.29f+g*.08f,.18f),Blue);}
                    break;
                case 3:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Plate(b,new Vector3(side*.53f,.41f,-.18f),new Vector3(.19f,.32f+g*.14f,.49f),Purple);
                        Plate(b,new Vector3(side*.32f,.37f,.55f),new Vector3(.12f,.15f,.64f+g*.16f),Steel);
                        if(level>=20)Plate(b,new Vector3(side*.52f,.72f,-.28f),new Vector3(.23f,.13f,.35f),Gold);
                    }
                    if(level>=25)Plate(b,new Vector3(0,.85f,-.17f),new Vector3(.52f,.19f,.45f),Purple);
                    break;
                case 4:
                    b.Finish=.6f;b.Tube(new Vector3(0,0,.19f),.34f+g*.045f,.28f,.09f,Steel,16,axis);
                    for(int i=0;i<6;i++)
                    {
                        float a=i*Mathf.PI/3;Plate(b,new Vector3(Mathf.Cos(a)*.36f,Mathf.Sin(a)*.36f,.24f),new Vector3(.09f,.09f,.08f),Gold,.014f);
                    }
                    if(level>=20){b.Finish=.8f;b.Shard(new Vector3(0,0,.30f),new Vector3(.10f,.14f,.09f),Gold,axis);}
                    break;
                case 5:
                    for(int side=-1;side<=1;side+=2)
                    {
                        b.Finish=.65f;b.Cylinder(new Vector3(side*.54f,.54f,-.16f),.13f,.55f+g*.24f,paint,12);
                        b.Finish=.75f;b.Tube(new Vector3(side*.54f,.84f+g*.12f,-.16f),.17f,.10f,.08f,Gold,12);
                        if(level>=20){b.Finish=.85f;b.Body(new Vector3(side*.54f,1.20f,-.16f),Vector3.one*.13f,Blue,10,5);}
                    }
                    if(level>=25){b.Finish=.7f;b.Tube(new Vector3(0,1.61f,0),.43f,.36f,.09f,Cream,16);}
                    if(level>=30){b.Finish=.85f;b.Body(new Vector3(0,1.83f,0),Vector3.one*.17f,Blue,12,6);}
                    break;
                case 6:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Plate(b,new Vector3(side*.52f,.46f,-.22f),new Vector3(.16f,.32f+g*.14f,.33f),Steel);
                        if(level>=15)
                        {
                            Plate(b,new Vector3(side*.36f,.75f,.05f),new Vector3(.10f,.70f,.15f),Steel);
                            Plate(b,new Vector3(side*.44f,.88f,.05f),new Vector3(.72f,.11f,.15f),LightWood);
                        }
                        if(level>=20)Plate(b,new Vector3(side*.78f,.88f,.08f),new Vector3(.15f,.16f,.21f),Gold);
                        if(level>=25)Plate(b,new Vector3(side*.47f,1.07f,-.11f),new Vector3(.60f,.12f,.14f),Steel);
                    }
                    break;
                case 7:
                    for(int side=-1;side<=1;side+=2)
                    {
                        Plate(b,new Vector3(side*.44f,.42f,-.14f),new Vector3(.20f,.29f+g*.18f,.61f),paint);
                        if(level>=20)Plate(b,new Vector3(side*.40f,.72f,-.22f),new Vector3(.18f,.14f,.47f),Gold);
                    }
                    b.Finish=.85f;b.Tube(new Vector3(0,.36f,.56f),.26f+g*.05f,.17f,.10f,Cream,16,axis);
                    if(level>=25){b.Finish=.85f;b.Shard(new Vector3(0,.88f,-.22f),new Vector3(.16f,.30f,.16f),paint);}
                    break;
            }
            if(level>=30 && kind!=4)
            {
                // Connected crown braces, not free-floating ornaments.
                for(int side=-1;side<=1;side+=2)
                    Plate(b,new Vector3(side*.36f,.86f,-.43f),new Vector3(.12f,.66f,.17f),paint);
                Plate(b,new Vector3(0,1.17f,-.43f),new Vector3(.80f,.13f,.23f),Gold);
            }
            if(level>=40 && kind!=4)for(int side=-1;side<=1;side+=2)
                Plate(b,new Vector3(side*.62f,.47f,-.12f),new Vector3(.14f,.35f,.56f),Cream);
            if(level>=50 && kind!=4)Plate(b,new Vector3(0,1.35f,-.43f),new Vector3(.54f,.15f,.26f),Steel);
            if(level>=75 && kind!=4)for(int side=-1;side<=1;side+=2)
                Plate(b,new Vector3(side*.46f,1.13f,-.43f),new Vector3(.13f,.30f,.21f),Gold);
            if(level>=100 && kind!=4)Plate(b,new Vector3(0,1.49f,-.43f),new Vector3(.39f,.16f,.24f),Cream);
        }
        static void HighTowerPath(DioramaMeshBuilder b,int kind,int primaryBand,int secondaryBand)
        {
            var paint=EvolutionPaint(kind);
            if(primaryBand>0)
            {
                // Power reservoirs, shell magazines, tension winches and focusing capacitors.
                for(int side=-1;side<=1;side+=2)
                {
                    float height=.17f+primaryBand*.045f;
                    Plate(b,new Vector3(side*.64f,.41f+height*.3f,-.37f),new Vector3(.18f,height,.29f),paint,.025f);
                    for(int i=0;i<primaryBand;i++)
                        Plate(b,new Vector3(side*.74f,.32f+i*.044f,-.34f),new Vector3(.028f,.025f,.20f),Gold,.007f);
                    if(primaryBand>=4)
                    {
                        b.Finish=.75f;
                        if(kind==2)b.Shard(new Vector3(side*.64f,.91f,-.37f),new Vector3(.10f,.25f,.10f),Blue);
                        else if(kind==5)b.Body(new Vector3(side*.64f,.94f,-.37f),Vector3.one*.10f,Blue,10,5);
                        else Plate(b,new Vector3(side*.64f,.85f,-.37f),new Vector3(.20f,.12f,.31f),Cream);
                    }
                }
            }
            if(secondaryBand>0)
            {
                float reach=.32f+secondaryBand*.04f;
                if(kind==1)
                {
                    // Broad shell collars communicate area, never a false acquisition-range scope.
                    b.Finish=.68f;b.Tube(new Vector3(0,.35f,.73f),.37f+secondaryBand*.013f,.33f,.10f,Gold,16,Quaternion.Euler(80,0,0));
                }
                else if(kind==2||kind==5)
                {
                    b.Finish=.72f;b.Tube(new Vector3(0,kind==2?1.53f:1.46f,0),reach,reach-.055f,.07f,Gold,16);
                    for(int side=-1;side<=1;side+=2)
                        Plate(b,new Vector3(side*(reach-.06f),1.24f,-.14f),new Vector3(.07f,.56f,.09f),Cream,.014f);
                }
                else
                {
                    // Braced elevated optics remain visible above the primary housing.
                    for(int side=-1;side<=1;side+=2)
                        Plate(b,new Vector3(side*.15f,.94f,-.12f),new Vector3(.07f,.48f,.12f),Steel,.014f);
                    Plate(b,new Vector3(0,1.15f,-.12f),new Vector3(.39f,.08f,.19f),Steel);
                    Scope(b,new Vector3(0,1.28f,-.12f),Mathf.Clamp(secondaryBand/2,0,3));
                }
                if(secondaryBand>=4)for(int side=-1;side<=1;side+=2)
                    Plate(b,new Vector3(side*.38f,1.18f,-.40f),new Vector3(.09f,.13f+secondaryBand*.015f,.27f),Cream);
            }
        }
        static void EvolutionUnit(DioramaMeshBuilder b,int kind,int tier)
        {
            if(kind==2){EvolutionTank(b,tier);return;}
            float scale=kind==3?.76f:1;
            Vector3 P(float x,float y,float z)=>new Vector3(x,y,z)*scale;
            Color paint=kind==3?Purple:kind==1?Blue:Green;
            for(int s=-1;s<=1;s+=2)
            {
                // Leave the original eyes, eyebrows and mouth unobstructed.
                Plate(b,P(s*.34f,.76f,-.045f),P(.15f+tier*.005f,.12f+tier*.005f,.26f),tier>=3?Steel:paint,.025f*scale);
                if(tier>=3)Plate(b,P(s*.36f,.72f,-.12f),P(.17f,.08f,.30f),Gold,.02f*scale);
                if(tier>=6)Plate(b,P(s*.30f,.43f,-.12f),P(.14f,.24f,.20f),Steel,.025f*scale);
                if(tier>=10)Plate(b,P(s*.34f,.81f,-.10f),P(.19f,.12f,.29f),Gold,.025f*scale);
            }
            // Each level adds a visible, bounded rank stud along the rear helmet band.
            for(int i=0;i<tier;i++)
            {
                float a=(i-4.5f)*.22f;
                Plate(b,P(Mathf.Sin(a)*.24f,.89f,-.18f-Mathf.Cos(a)*.08f),P(.04f,.06f,.045f),Gold,.008f*scale);
            }
            if(kind==1)
            {
                if(tier>=3)for(int s=-1;s<=1;s+=2)
                {b.Finish=.75f;b.Shard(P(s*.23f,.68f,-.31f),P(.085f,.29f,.09f),Blue,Quaternion.Euler(-60,0,-s*12));}
                if(tier>=6)Plate(b,P(0,.47f,-.36f),P(.28f,.26f,.16f),Steel,.035f);
                if(tier>=10)for(int s=-1;s<=1;s+=2)
                {b.Finish=.7f;b.Shard(P(s*.20f,.94f,-.25f),P(.055f,.27f,.055f),Gold,Quaternion.Euler(-55,0,0));}
            }
            else if(tier>=6)
                for(int i=0;i<3;i++)
                {b.Finish=.8f;b.Shard(P((i-1)*.13f,1.02f,-.09f),P(.065f,.18f+(i==1?.08f:0),.065f),kind==3?Gold:paint);}
            if(tier>=10&&kind!=1)Plate(b,P(0,.98f,-.12f),P(.36f,.075f,.21f),Gold,.015f*scale);
        }
        static void EvolutionTank(DioramaMeshBuilder b,int tier)
        {
            // A heavier transporter: armor and command cabin, never an invented gun.
            Plate(b,new Vector3(0,.23f,.54f),new Vector3(.76f,.13f,.12f),Steel);
            for(int i=0;i<tier;i++)Plate(b,new Vector3(i%2==0?-.23f:.23f,.66f,-.25f+(i/2)*.115f),new Vector3(.16f,.05f,.065f),Gold,.012f);
            for(int s=-1;s<=1;s+=2)
            {
                if(tier>=3)Plate(b,new Vector3(s*.46f,.52f,0),new Vector3(.27f,.10f,1.11f),Orange,.035f);
                if(tier>=6)for(int i=0;i<3;i++)Plate(b,new Vector3(s*.60f,.39f,-.33f+i*.33f),new Vector3(.055f,.23f,.25f),Steel,.015f);
                if(tier>=10)Plate(b,new Vector3(s*.27f,.64f,.23f),new Vector3(.20f,.22f,.28f),Gold,.035f);
            }
            if(tier>=6)
            {
                Plate(b,new Vector3(0,.82f,-.14f),new Vector3(.43f,.25f,.41f),Steel,.045f);
                Plate(b,new Vector3(0,.87f,.07f),new Vector3(.28f,.06f,.025f),Cream,.008f);
            }
            if(tier>=10)Plate(b,new Vector3(0,.99f,-.14f),new Vector3(.52f,.10f,.48f),Gold,.035f);
        }
    }
}
