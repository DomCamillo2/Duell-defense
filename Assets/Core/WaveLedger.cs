using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
namespace DefenseDuell.Core
{
    /// <summary>Finite compatibility array or bounded current/next window. Indexer uses absolute zero-based wave.</summary>
    [Serializable]
    public sealed class WaveLedger : IEnumerable<BigInteger>
    {
        private readonly BigInteger[] values;
        public BigInteger FirstWave { get; private set; }
        public int Length=>values.Length;
        public bool IsEndless { get; }
        public WaveLedger(BigInteger firstWave,BigInteger[] values,bool endless)
        {
            if(firstWave<1||values==null||values.Length<1||values.Length>200||(endless&&values.Length!=2)||(!endless&&firstWave!=1))throw new ArgumentException("Invalid bounded wave ledger");
            FirstWave=firstWave;this.values=(BigInteger[])values.Clone();IsEndless=endless;
        }
        public BigInteger this[BigInteger zeroBasedWave]
        {
            get {var i=zeroBasedWave-(FirstWave-1);if(i<0||i>=values.Length)throw new IndexOutOfRangeException("Wave outside ledger window");return values[(int)i];}
            set {var i=zeroBasedWave-(FirstWave-1);if(i<0||i>=values.Length)throw new IndexOutOfRangeException("Wave outside ledger window");values[(int)i]=value;}
        }
        public BigInteger[] ToArray()=>(BigInteger[])values.Clone();
        public BigInteger Advance()
        {if(!IsEndless)throw new InvalidOperationException("Finite ledger cannot advance");var retired=values[0];values[0]=values[1];values[1]=0;FirstWave++;return retired;}
        public IEnumerator<BigInteger> GetEnumerator()=>((IEnumerable<BigInteger>)values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()=>values.GetEnumerator();
        public static implicit operator WaveLedger(BigInteger[] values)=>new WaveLedger(1,values,false);
        public static WaveLedger Create(GameContent content)=>new WaveLedger(1,new BigInteger[content.MatchRules.Endless?2:content.WaveCount],content.MatchRules.Endless);
    }
}
