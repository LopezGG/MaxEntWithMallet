using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MaxEntMallet
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 5)
                throw new Exception("Incorrrect number of paramters sent in");
            string trainFile = args[0];
            string testFile = args[1];
            double rareThreshold = Convert.ToDouble(args[2]);
            double featThreshold = Convert.ToDouble(args[3]);
            string outputDir = args[4];
            List<String> InputLines = new List<string>();
            Dictionary<String, int> WordCount = new Dictionary<string, int>();
            Dictionary<String, int> featuresCount = new Dictionary<string, int>();
            List<List<string>> wordFeatures = new List<List<string>>();
            string[] wordlist;
            int tempIndex;
            string curWord, item, tag, line, prevT, prev2T, prevW, prev2W, nextW, next2W,nextItem,next2Item,temp,nextT,next2T;
           
            //read the input file and store it in a DS to process later based on word frequency
            ReadInputAndGetWC(trainFile,InputLines, WordCount);
            for (int j = 0; j < InputLines.Count; j++)
            {
                line = InputLines[j];
                wordlist =  line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                //initialize. we will atleast have 4 words
                prev2T = prev2W = prevT = prevW= "BOS";
                item = wordlist[2];
                tempIndex = item.LastIndexOf("/");
                curWord = item.Substring(0, tempIndex);
                tag = item.Substring(tempIndex + 1);
                nextItem = wordlist[3];
                tempIndex = nextItem.LastIndexOf("/");
                nextW = nextItem.Substring(0,tempIndex);
                nextT = nextItem.Substring(tempIndex + 1);
                next2Item = wordlist[4];
                tempIndex = next2Item.LastIndexOf("/");
                next2W = next2Item.Substring(0, tempIndex);
                next2T = next2Item.Substring(tempIndex + 1);
                
                //we dont want to read BOS and EOS
                for (int i = 2; i < wordlist.Length-2; i++)
                {
                    
                    List<String> tempList = new List<string>();
                    tempList.Add(j+1+"-"+i+"-"+curWord + " "+tag);
                    temp = String.Format("prevT="+prevT);
                    if (featuresCount.ContainsKey(temp))
                        featuresCount[temp]++;
                    else
                        featuresCount.Add(temp, 1);
                    tempList.Add(temp+" 1");

                    temp = String.Format("prevTwoTags="+prevT+"+"+ prev2T);
                    if (featuresCount.ContainsKey(temp))
                        featuresCount[temp]++;
                    else
                        featuresCount.Add(temp, 1);
                    tempList.Add(temp + " 1");

                    temp = String.Format("prevW=" + prevW);
                    if (featuresCount.ContainsKey(temp))
                        featuresCount[temp]++;
                    else
                        featuresCount.Add(temp, 1);
                    tempList.Add(temp + " 1");

                    temp = String.Format("prev2W=" + prev2W);
                    if (featuresCount.ContainsKey(temp))
                        featuresCount[temp]++;
                    else
                        featuresCount.Add(temp, 1);
                    tempList.Add(temp + " 1");

                    temp = String.Format("nextW=" + nextW);
                    if (featuresCount.ContainsKey(temp))
                        featuresCount[temp]++;
                    else
                        featuresCount.Add(temp, 1);
                    tempList.Add(temp + " 1");
                    temp = String.Format("next2W=" + next2W);
                    if (featuresCount.ContainsKey(temp))
                        featuresCount[temp]++;
                    else
                        featuresCount.Add(temp, 1);
                    tempList.Add(temp + " 1");
                    if (WordCount[curWord] <= rareThreshold )
                    {
                        if (curWord.Length >= 2)
                        {
                            temp = "pref=" + curWord.Substring(0, 1);
                            if (featuresCount.ContainsKey(temp))
                                featuresCount[temp]++;
                            else
                                featuresCount.Add(temp, 1);
                            tempList.Add(temp + " 1");
                            //TODO:Check on suffux and prefix length
                            temp = "suf=" + curWord.Substring(curWord.Length - 1, 1);
                            if (featuresCount.ContainsKey(temp))
                                featuresCount[temp]++;
                            else
                                featuresCount.Add(temp, 1);
                            tempList.Add(temp + " 1");
                        }
                        
                        if(curWord.Length>=2)
                        {
                            temp = "pref=" + curWord.Substring(0, 2);
                            if (featuresCount.ContainsKey(temp))
                                featuresCount[temp]++;
                            else
                                featuresCount.Add(temp, 1);
                            tempList.Add(temp + " 1");
                            //TODO
                            temp = "suf=" + curWord.Substring(curWord.Length - 2, 2);
                            if (featuresCount.ContainsKey(temp))
                                featuresCount[temp]++;
                            else
                                featuresCount.Add(temp, 1);
                            tempList.Add(temp + " 1");
                        }
                        
                        if(curWord.Any(char.IsUpper))
                        {
                            temp = "containUC";
                            if (featuresCount.ContainsKey(temp))
                                featuresCount[temp]++;
                            else
                                featuresCount.Add(temp, 1);
                            tempList.Add(temp + " 1");
                        }
                        if (curWord.Any(char.IsDigit))
                        {
                            temp = "containNum";
                            if (featuresCount.ContainsKey(temp))
                                featuresCount[temp]++;
                            else
                                featuresCount.Add(temp, 1);
                            tempList.Add(temp + " 1");
                        }
                        if (curWord.Contains("-"))
                        {
                            temp = "containHypen";
                            if (featuresCount.ContainsKey(temp))
                                featuresCount[temp]++;
                            else
                                featuresCount.Add(temp, 1);
                            tempList.Add(temp + " 1");
                        }
                        wordFeatures.Add(tempList);
                        prev2T = prevT;
                        prevT = tag;
                        prev2W = prevW;
                        prevW = curWord;
                        curWord = nextW;
                        tag = nextT;
                        nextW = next2W;
                        nextT = next2T;
                        next2Item = wordlist[i + 2];
                        tempIndex = next2Item.LastIndexOf("/");
                        next2W = next2Item.Substring(0, tempIndex);
                        next2T = next2Item.Substring(tempIndex + 1);
                    }
                }

            }


        }
        public static void ReadInputAndGetWC(string inputPath,List<string> InputLines, Dictionary<String ,int> WordCount)
        {
            string line;
            string[] wordtagPair;
            string word;
            int tempIndex;
            using (StreamReader SR = new StreamReader(inputPath))
            {
                while ((line = SR.ReadLine()) != null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    line = "BOS/BOS BOS/BOS " + line + @" EOS/EOS EOS/EOS";
                    line = Regex.Replace(line, ",", "comma");
                    wordtagPair = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    
                    InputLines.Add(line);
                    foreach (var item in wordtagPair)
                    {
                        tempIndex = item.LastIndexOf("/");
                        word = item.Substring(0, tempIndex);
                        if (WordCount.ContainsKey(word))
                            WordCount[word]++;
                        else
                            WordCount.Add(word, 1);
                    }
                }
            }
        }
    }
}
