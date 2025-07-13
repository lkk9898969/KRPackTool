using System.Text;

namespace KartRider.Common.Utilities;

public class Adler32Helper
{
    public static uint GenerateAdler32(byte[] str, uint a1 = 0)
    {
        uint num;
        var num1 = 0;
        var length = (uint)str.Length;
        var num2 = a1 >> 16;
        uint num3 = (ushort)a1;
        if (str.Length == 1)
        {
            var num4 = (int)(str[0] + num3);
            if (num4 >= 65521) num4 -= 65521;
            var num5 = (int)((ulong)num4 + num2);
            if (num5 >= 65521) num5 -= 65521;
            num = (uint)(num4 | (num5 << 16));
        }
        else if (str == null)
        {
            num = 1;
        }
        else if (str.Length < 16)
        {
            if (str.Length != 0)
                do
                {
                    var num6 = num1;
                    num1 = num6 + 1;
                    num3 += str[num6];
                    num2 += num3;
                    length--;
                } while (length != 0);

            if (num3 >= 65521) num3 -= 65521;
            num = num3 | ((num2 % 65521) << 16);
        }
        else
        {
            if (str.Length >= 5552)
            {
                var length1 = (uint)(str.Length / 5552);
                do
                {
                    length -= 5552;
                    var num7 = 347;
                    do
                    {
                        var num8 = (int)(str[num1] + num3);
                        var num9 = (int)(num8 + num2);
                        var num10 = str[num1 + 1] + num8;
                        var num11 = num10 + num9;
                        var num12 = str[num1 + 2] + num10;
                        var num13 = num12 + num11;
                        var num14 = str[num1 + 3] + num12;
                        var num15 = num14 + num13;
                        var num16 = str[num1 + 4] + num14;
                        var num17 = num16 + num15;
                        var num18 = str[num1 + 5] + num16;
                        var num19 = num18 + num17;
                        var num20 = str[num1 + 6] + num18;
                        var num21 = num20 + num19;
                        var num22 = str[num1 + 7] + num20;
                        var num23 = num22 + num21;
                        var num24 = str[num1 + 8] + num22;
                        var num25 = num24 + num23;
                        var num26 = str[num1 + 9] + num24;
                        var num27 = num26 + num25;
                        var num28 = str[num1 + 10] + num26;
                        var num29 = num28 + num27;
                        var num30 = str[num1 + 11] + num28;
                        var num31 = num30 + num29;
                        var num32 = str[num1 + 12] + num30;
                        var num33 = num32 + num31;
                        var num34 = str[num1 + 13] + num32;
                        var num35 = num34 + num33;
                        var num36 = str[num1 + 14] + num34;
                        var num37 = num36 + num35;
                        num3 = (uint)(str[num1 + 15] + num36);
                        num2 = (uint)(num3 + num37);
                        num1 += 16;
                        num7--;
                    } while (num7 != 0);

                    num3 %= 65521;
                    length1--;
                    num2 %= 65521;
                } while (length1 != 0);
            }

            if (length != 0)
            {
                if (length >= 16)
                {
                    var num38 = length >> 4;
                    do
                    {
                        var num39 = (int)(str[num1] + num3);
                        var num40 = (int)(num39 + num2);
                        var num41 = str[num1 + 1] + num39;
                        var num42 = num41 + num40;
                        var num43 = str[num1 + 2] + num41;
                        var num44 = num43 + num42;
                        var num45 = str[num1 + 3] + num43;
                        var num46 = num45 + num44;
                        var num47 = str[num1 + 4] + num45;
                        var num48 = num47 + num46;
                        var num49 = str[num1 + 5] + num47;
                        var num50 = num49 + num48;
                        var num51 = str[num1 + 6] + num49;
                        var num52 = num51 + num50;
                        var num53 = str[num1 + 7] + num51;
                        var num54 = num53 + num52;
                        var num55 = str[num1 + 8] + num53;
                        var num56 = num55 + num54;
                        var num57 = str[num1 + 9] + num55;
                        var num58 = num57 + num56;
                        var num59 = str[num1 + 10] + num57;
                        var num60 = num59 + num58;
                        var num61 = str[num1 + 11] + num59;
                        var num62 = num61 + num60;
                        var num63 = str[num1 + 12] + num61;
                        var num64 = num63 + num62;
                        var num65 = str[num1 + 13] + num63;
                        var num66 = num65 + num64;
                        var num67 = str[num1 + 14] + num65;
                        var num68 = num67 + num66;
                        num3 = (uint)(str[num1 + 15] + num67);
                        length -= 16;
                        num2 = (uint)(num3 + num68);
                        num1 += 16;
                        num38--;
                    } while (num38 != 0);
                }

                while (length != 0)
                {
                    var num69 = num1;
                    num1 = num69 + 1;
                    num3 += str[num69];
                    num2 += num3;
                    length--;
                }

                num3 %= 65521;
                num2 %= 65521;
            }

            num = num3 | (num2 << 16);
        }

        return num;
    }

    public static uint GenerateAdler32_ASCII(string str, uint a1 = 0)
    {
        var num = GenerateAdler32(Encoding.ASCII.GetBytes(str), a1);
        return num;
    }

    public static uint GenerateAdler32_UNICODE(string str, uint a1 = 0)
    {
        var num = GenerateAdler32(Encoding.Unicode.GetBytes(str), a1);
        return num;
    }

    public static int GenerateSimpleAdler32(byte[] bytes)
    {
        uint num = 1;
        uint num1 = 0;
        var numArray = bytes;
        for (var i = 0; i < numArray.Length; i++)
        {
            num = (num + numArray[i]) % 65521;
            num1 = (num1 + num) % 65521;
        }

        return (int)((num1 << 16) + num);
    }
}