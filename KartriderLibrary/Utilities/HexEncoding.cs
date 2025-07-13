using System;
using System.Globalization;
using System.Text;

namespace KartRider.Common.Utilities;

public class HexEncoding
{
    public static byte[] GetBytes(string pHexString)
    {
        var empty = string.Empty;
        for (var i = 0; i < pHexString.Length; i++)
        {
            var chr = pHexString[i];
            if (IsHexDigit(chr)) empty = string.Concat(empty, chr.ToString());
        }

        if (empty.Length % 2 != 0) empty = empty.Substring(0, empty.Length - 1);
        var num = new byte[empty.Length / 2];
        var num1 = 0;
        for (var j = 0; j < num.Length; j++)
        {
            num[j] = HexToByte(new string(new[] { empty[num1], empty[num1 + 1] }));
            num1 += 2;
        }

        return num;
    }

    public static string GetString(byte[] pArray)
    {
        var stringBuilder = new StringBuilder();
        var numArray = pArray;
        for (var i = 0; i < numArray.Length; i++)
        {
            var num = numArray[i];
            stringBuilder.Append(num.ToString("X2")).Append(" ");
        }

        return stringBuilder.ToString();
    }

    private static byte HexToByte(string pHex)
    {
        if (pHex.Length > 2 ? true : pHex.Length <= 0)
            throw new ArgumentException("hex must be 1 or 2 characters in length");
        return byte.Parse(pHex, NumberStyles.HexNumber);
    }

    public static bool IsHexDigit(char pChar)
    {
        bool flag;
        var num = Convert.ToInt32('A');
        var num1 = Convert.ToInt32('0');
        pChar = char.ToUpper(pChar);
        var num2 = Convert.ToInt32(pChar);
        if (num2 < num ? true : num2 >= num + 6)
            flag = (num2 < num1 ? true : num2 >= num1 + 10) ? false : true;
        else
            flag = true;
        return flag;
    }

    public static string ToStringFromAscii(byte[] pBytes)
    {
        var chrArray = new char[pBytes.Length];
        for (var i = 0; i < pBytes.Length; i++)
            if (pBytes[i] >= 32)
                chrArray[i] = (char)(pBytes[i] & 255);
            else
                chrArray[i] = '.';

        return new string(chrArray);
    }
}