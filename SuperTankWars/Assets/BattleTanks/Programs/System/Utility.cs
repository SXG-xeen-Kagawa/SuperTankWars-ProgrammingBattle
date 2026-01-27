using UnityEngine;

namespace SXG2025
{

    public class Utility
    {

        private static readonly char[] randomChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        /// <summary>
        /// ランダムな文字列を生成 
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string MakeRandomString(int length)
        {
            System.Text.StringBuilder sb = new();
            System.Random rand = new();
            for (int i = 0; i < length; ++i)
            {
                sb.Append(randomChars[rand.Next(randomChars.Length)]);
            }
            return sb.ToString();
        }
    }

}

