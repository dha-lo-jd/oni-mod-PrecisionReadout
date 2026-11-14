using System;
using System.Globalization;
using System.Text;

namespace PrecisionReadout;

// Formats a double as the exact decimal expansion of its binary value (rounded half-up at the requested digit).
// Mono's double.ToString("F10") pads with zeros after ~15 significant digits, which would hide the real float value.
internal static class ExactFormat
{
    // A float holds ~7.2 significant decimal digits; digits past this point are simulation rounding noise.
    internal const int TrustedSignificantDigits = 7;

    internal static string Fixed(double value, int digits, bool dimUntrusted)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return value.ToString(CultureInfo.CurrentCulture);
        digits = Math.Max(0, Math.Min(10, digits));

        var negative = value < 0;
        var bits = BitConverter.DoubleToInt64Bits(Math.Abs(value));
        var exp = (int)((bits >> 52) & 0x7FF);
        var frac = (ulong)(bits & 0xFFFFFFFFFFFFFL);
        ulong mantissa;
        int e;
        if (exp == 0) { mantissa = frac; e = -1074; }
        else { mantissa = frac | (1UL << 52); e = exp - 1075; }

        ulong intPart;
        var fracDigits = new int[digits];
        var roundUp = false;
        if (e >= 0)
        {
            if (e > 10) return value.ToString("F" + digits, CultureInfo.CurrentCulture); // >= 2^63: not a game value
            intPart = mantissa << e;
        }
        else
        {
            var shift = -e;
            if (shift > 60)
            {
                // Dropping bits below 2^-60 changes the result by < 1e-18, far below the 10 digits offered.
                var drop = shift - 60;
                mantissa = drop >= 64 ? 0UL : mantissa >> drop;
                shift = 60;
            }
            var mask = (1UL << shift) - 1UL;
            intPart = mantissa >> shift;
            var f = mantissa & mask;
            for (var i = 0; i < digits; i++)
            {
                f *= 10UL; // f < 2^60, so f * 10 < 2^64
                fracDigits[i] = (int)(f >> shift);
                f &= mask;
            }
            roundUp = f >= (1UL << (shift - 1));
        }

        if (roundUp)
        {
            var i = digits - 1;
            while (i >= 0 && fracDigits[i] == 9) { fracDigits[i] = 0; i--; }
            if (i >= 0) fracDigits[i]++; else intPart++;
        }

        var intText = intPart.ToString(CultureInfo.InvariantCulture);
        var nonZero = intPart != 0;
        if (!nonZero) foreach (var d in fracDigits) if (d != 0) { nonZero = true; break; }

        var sb = new StringBuilder();
        if (negative && nonZero) sb.Append('-');

        // Digits are counted from the first non-zero digit; trust ends after TrustedSignificantDigits of them.
        var firstNonZero = -1;
        var seq = 0;
        var dimmed = false;

        void Emit(int digit)
        {
            if (firstNonZero < 0 && digit != 0) firstNonZero = seq;
            if (dimUntrusted && !dimmed && firstNonZero >= 0 && seq - firstNonZero == TrustedSignificantDigits)
            {
                dimmed = true;
                sb.Append("<alpha=#70>");
            }
            sb.Append((char)('0' + digit));
            seq++;
        }

        foreach (var c in intText) Emit(c - '0');
        if (digits > 0)
        {
            sb.Append(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            foreach (var d in fracDigits) Emit(d);
        }
        if (dimmed) sb.Append("<alpha=#FF>");
        return sb.ToString();
    }
}
