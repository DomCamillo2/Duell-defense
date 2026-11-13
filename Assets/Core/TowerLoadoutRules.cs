namespace DefenseDuell.Core
{
    public static class TowerLoadoutRules
    {
        public const int MaxSelectedClasses=5;
        public const int DefaultMask=31;
        public static int Count(int mask){int count=0;for(uint bits=(uint)mask;bits!=0;bits&=bits-1)count++;return count;}
        public static bool IsValid(int mask,int towerCount)=>towerCount>=1&&towerCount<=30&&mask>0&&(mask&~((1<<towerCount)-1))==0&&Count(mask)<=MaxSelectedClasses;
        public static int Normalize(int mask,int towerCount)
        {
            if(towerCount<1||towerCount>30)return 1;
            int valid=mask&((1<<towerCount)-1),selected=0,count=0;
            for(int kind=0;kind<towerCount&&count<MaxSelectedClasses;kind++)if((valid&(1<<kind))!=0){selected|=1<<kind;count++;}
            return selected==0?1:selected;
        }
    }
}
