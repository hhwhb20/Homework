using System;
using System.Collections.Generic;
using System.Text;

namespace homework
{
    class Program
    {
        static List<string> test = new List<string>()
        {
            "Welcome to JoyCastle.Let's make an awesome game together.",
            "Focused,hard work is the real key to success. Keep your eyes on the goal,and just keep taking the next step towards completing it.",
        };
        static void Main(string[] args)
        {
            foreach(var str in test)
            {
                Reverse(str);
            }
        }

        static void Reverse(string str)
        {
            StringBuilder sb = new StringBuilder(str);
            List<int> spacelist = new List<int>();
            int start = 0;
            for(int i = 0; i < sb.Length; i++)
            {
                //空格的话，记录一下，用于索引单词坐标
                if(sb[i] == ' ')
                {
                    spacelist.Add(i);
                }
                //开始处理往前的一段字符串
                else if(sb[i] == ',' || sb[i] == '.')
                {
                    StringBuild(sb, str, spacelist, start, i);
                    spacelist.Clear();
                    start = i + 1;
                }
            }
            Console.WriteLine(str);
            Console.WriteLine(sb);
        }

        static void StringBuild(StringBuilder sb, string str, List<int> spacelist, int start, int end)
        {
            int spaceindex = spacelist.Count - 1;
            int startspace;
            int endspace;
            //做安全保护
            if (spaceindex == -1)
            {
                startspace = start - 1;
                endspace = end;
            }
            else
            {
                startspace = spacelist[spaceindex];
                endspace = end;
            }
            int index = startspace + 1;
            for(int i = start; i < end; i++)
            {
                if (index < endspace)
                {
                    sb[i] = str[index];
                    index++;
                }
                else //说明单词走完了，该走下一个单词了
                {
                    spaceindex--;
                    if (spaceindex == -1)
                    {
                        startspace = start - 1;
                        endspace = spacelist[0];
                    }
                    else if(spaceindex == -2)
                    {
                        return;
                    }
                    else
                    {
                        startspace = spacelist[spaceindex];
                        endspace = spacelist[spaceindex + 1];
                    }
                    index = startspace + 1;
                    sb[i] = ' ';
                }
            }
        }
    }
}
