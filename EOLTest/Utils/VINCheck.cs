// Copyright (c) TBD 2026.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EOLTest.Utils
{
    public class VINCheck
    {
        public int VIN_To_Value(char ch)
        {
            switch (ch)
            {
                case '0':
                    return 0;
                case '1':
                case 'A':
                case 'J':
                    return 1;
                case '2':
                case 'B':
                case 'K':
                case 'S':
                    return 2;
                case '3':
                case 'C':
                case 'L':
                case 'T':
                    return 3;
                case '4':
                case 'D':
                case 'M':
                case 'U':
                    return 4;
                case '5':
                case 'E':
                case 'N':
                case 'V':
                    return 5;
                case '6':
                case 'F':
                case 'W':
                    return 6;
                case '7':
                case 'G':
                case 'P':
                case 'X':
                    return 7;
                case '8':
                case 'H':
                case 'Y':
                    return 8;
                case '9':
                case 'R':
                case 'Z':
                    return 9;
                default:
                    return 0;
            }

        }

        public bool VIN_Check_GB(string vin)
        {
            char[] ch = vin.ToCharArray();    //定义VIN码的每一位, 转换过来是UNICODE的数值，转换成数字会对照到UNICODE的表

            int[] VinValue = new int[17];      //定义VIN码每一位数字或字母的对应值
            int[] VinParameter = new int[17];     //定义VIN码不同位置对应的加权函数
            int VIN_Code_Check;                         // 定义VIN码检验位的值（为0～9中的任一数字或字母“X”）
            int Check_Bit;                                      //定义除得的余数值
            int sum;

            //VIN不同位置对应的加权系数
            VinParameter[0] = 8;
            VinParameter[1] = 7;
            VinParameter[2] = 6;
            VinParameter[3] = 5;
            VinParameter[4] = 4;
            VinParameter[5] = 3;
            VinParameter[6] = 2;
            VinParameter[7] = 10;
            VinParameter[8] = 0;
            VinParameter[9] = 9;
            VinParameter[10] = 8;
            VinParameter[11] = 7;
            VinParameter[12] = 6;
            VinParameter[13] = 5;
            VinParameter[14] = 4;
            VinParameter[15] = 3;
            VinParameter[16] = 2;

            //将检验位（第9位）之外的16位每一位的加权系数乘以此位数字或字母的对应值，再将各乘积相加，求得的和被11除
            sum = 0;
            for (int i = 0; i < 17; i++)
            {
                VinValue[i] = VIN_To_Value(ch[i]);
                sum += VinValue[i] * VinParameter[i];
            }
            Check_Bit = sum % 11;

            //除得的余数即为检验位，如果余数是10，检验位应为字母“X"
            VIN_Code_Check = 0;

            if (ch[8] == 'X')
            {
                VIN_Code_Check = 10;
            }
            else
            {
                string ch1 = vin.Substring(8, 1);
                int temp = (int.Parse(ch1));
                if (temp >= 0 && temp <= 9)
                {
                    VIN_Code_Check = temp;
                }
                else
                {
                    VIN_Code_Check = 11;
                }
            }


            if (Check_Bit != VIN_Code_Check)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
