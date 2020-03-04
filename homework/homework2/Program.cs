using System;

namespace homework2
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            /*解题思路
             * 100个正方形拼成矩形只有四种情况，1*100，2*50， 4*25， 10*10
             * 
             */
            int lengthSide = GetLengthSide(100, 1);
            Console.WriteLine(lengthSide);
        }

        static int GetLengthSide(int m, int n)
        {
            if (m * 100 == n || n * 100 == m)
            {
                return m * 100 == n ? m : n;
            }
            else if(m * 50 == n * 2 || n * 50 == m * 2)
            {
                return m * 50 == n * 2 ? m / 2 : n / 2;
            }
            else if (m * 25 == n * 4 || n * 25 == m * 4)
            {
                return m * 25 == n * 4 ? m / 4 : n / 4;
            }
            else if (m == n)
            {
                return m / 10;
            }
            return 0;
        }
    }
}
