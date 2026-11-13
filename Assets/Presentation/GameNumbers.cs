using System;
using System.Globalization;
using System.Numerics;

namespace DefenseDuell.Presentation
{
    /// <summary>Integer-only display formatting; prices and command quotes never use this lossy display form.</summary>
    public static class GameNumbers
    {
        public static string Exact(BigInteger value)=>value.ToString(CultureInfo.InvariantCulture);
        public static string Compact(BigInteger value)
        {
            var n=BigInteger.Abs(value);if(n<1000000)return Exact(value);
            string digits=Exact(n),sign=value.Sign<0?"−":"";
            int exponent=((digits.Length-1)/3)*3;
            string suffix=exponent==6?" Mio.":exponent==9?" Mrd.":exponent==12?" Bio.":"e"+exponent;
            var scale=BigInteger.Pow(10,exponent);var hundredths=n*100/scale;
            string fraction=((int)(hundredths%100)).ToString("00",CultureInfo.InvariantCulture).TrimEnd('0');
            return "≈"+sign+Exact(hundredths/100)+(fraction.Length>0?","+fraction:"")+suffix;
        }
        public static string Ratio(BigInteger numerator,int divisor)
        {
            if(divisor<=0)throw new ArgumentOutOfRangeException(nameof(divisor));
            var n=BigInteger.Abs(numerator);if(n/divisor>=1000000)return Compact(numerator/divisor);
            // Round only the display to hundredths; never cast arbitrary-size state to float/double.
            var hundredths=(n*100+divisor/2)/divisor;
            string fraction=((int)(hundredths%100)).ToString("00",CultureInfo.InvariantCulture).TrimEnd('0');
            return (numerator.Sign<0&&hundredths>0?"−":"")+Exact(hundredths/100)+(fraction.Length>0?","+fraction:"");
        }
    }
}
