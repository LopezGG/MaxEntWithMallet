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
            if (Directory.Exists(outputDir))
            {
                var directory = new DirectoryInfo(outputDir);
                directory.EnumerateFiles()
                    .ToList().ForEach(f => f.Delete());
                directory.EnumerateDirectories()
                    .ToList().ForEach(d => d.Delete(true));
            }

            Directory.CreateDirectory(outputDir);
            List<String> InputLines = new List<string>();
            Dictionary<String, int> WordCount = new Dictionary<string, int>();
            Dictionary<String, int> featuresCount = new Dictionary<string, int>();
            List<List<string>> wordFeatures = new List<List<string>>();
            
            //read the input file and store it in a DS to process later based on word frequency
            ReadInputAndGetWC(trainFile,InputLines, WordCount);
            //process training data
            ProcessData(InputLines,featuresCount,wordFeatures,true,rareThreshold, WordCount);

            var featuresCountSorted = featuresCount.OrderByDescending(x => x.Value);
            StreamWriter sw = new StreamWriter(outputDir+@"\int_feats");
            StreamWriter sw1 = new StreamWriter(outputDir + @"\kept_feats");
            StreamWriter sw2 = new StreamWriter(outputDir + @"\train_voc");
            foreach (var item in featuresCountSorted)
            {
                if ( item.Value >= featThreshold)
                {
                    sw1.WriteLine(item.Key + " " + item.Value);
                    
                }
                sw.WriteLine(item.Key + " " + item.Value);
            }
            sw.Close();
            sw1.Close();
            //write vocabulary file
            //TODO:Change to "comma" when features are created or change the vocab file
            foreach (var item in WordCount.OrderByDescending(x=>x.Value))
            {
                sw2.WriteLine(((item.Key=="comma") ? "," : item.Key) + " " + item.Value);
            }
            sw2.Close();
            //Write down the train file
            WriteFinalTrainTest(outputDir + @"\final_train.vectors.txt", wordFeatures,featThreshold,featuresCount);
           
            
            string line;
            //Read test File
            List<String> InputLinesTest = new List<string>();
            List<List<string>> wordFeaturesTest = new List<List<string>>();
           
            using(StreamReader Sr =new StreamReader(testFile))
            {
                while((line = Sr.ReadLine())!=null)
                {
                    if (String.IsNullOrWhiteSpace(line))
                        continue;
                    line = "BOS/BOS BOS/BOS " + line + @" EOS/EOS EOS/EOS";
                    line = Regex.Replace(line, ",", "comma");
                    InputLinesTest.Add(line);
                }
            }
            //Process Test File to generate vectors
            ProcessData(InputLinesTest, featuresCount, wordFeaturesTest, false, rareThreshold, WordCount);
            WriteFinalTrainTest(outputDir + @"\final_test.vectors.txt", wordFeaturesTest, featThreshold, featuresCount);
            Console.WriteLine("finished processing training");
            Console.ReadLine();
            


        }
        public static void WriteFinalTrainTest(string OutFileName, List<List<string>> wordFeatures, double featThreshold, Dictionary<String, int> featuresCount)
        {
            //Write down the train file
            StreamWriter sw2 = new StreamWriter(OutFileName);
            string temp;
            foreach (var wordList in wordFeatures)
            {
                sw2.Write(wordList[0] + " ");
                for (int i = 1; i < wordList.Count; i++)
                {
                    temp = wordList[i];
                    if (featuresCount.ContainsKey(temp) && featuresCount[temp] >= featThreshold)
                        sw2.Write(temp + " 1 ");
                }
                sw2.WriteLine();
            }
            sw2.Close();
        }
        public static void ProcessData(List<String> InputLines, Dictionary<String, int> featuresCount, List<List<string>> wordFeatures, bool train, double rareThreshold, Dictionary<String, int> WordCount)
        {
            string[] wordlist;
            int tempIndex;
            string curWord, item, tag, line, prevT, prev2T, prevW, prev2W, nextW, next2W, nextItem, next2Item, temp, nextT, next2T;

            for (int j = 0; j < InputLines.Count; j++)
            {
                line = InputLines[j];
                wordlist = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                //initialize. we will atleast have 4 words
                prev2T = prev2W = prevT = prevW = "BOS";
                item = wordlist[2];
                tempIndex = item.LastIndexOf("/");
                curWord = item.Substring(0, tempIndex);
                tag = item.Substring(tempIndex + 1);
                nextItem = wordlist[3];
                tempIndex = nextItem.LastIndexOf("/");
                nextW = nextItem.Substring(0, tempIndex);
                nextT = nextItem.Substring(tempIndex + 1);
                next2Item = wordlist[4];
                tempIndex = next2Item.LastIndexOf("/");
                next2W = next2Item.Substring(0, tempIndex);
                next2T = next2Item.Substring(tempIndex + 1);
               
                //we dont want to read BOS and EOS
                for (int i = 2; i < wordlist.Length - 2; i++)
                {

                    List<String> tempList = new List<string>();

                    tempList.Add(Convert.ToString(j + 1) +"-"+ Convert.ToString(i - 2) + "-" + curWord + " " + tag);
                   
                    temp = String.Format("prevT=" + prevT);
                    AddtoDictAndList(featuresCount, tempList, temp, train);

                    temp = String.Format("prevTwoTags=" + prevT + "+" + prev2T);
                    AddtoDictAndList(featuresCount, tempList, temp, train);

                    temp = String.Format("prevW=" + prevW);
                    AddtoDictAndList(featuresCount, tempList, temp, train);

                    temp = String.Format("prev2W=" + prev2W);
                    AddtoDictAndList(featuresCount, tempList, temp, train);

                    temp = String.Format("nextW=" + nextW);
                    AddtoDictAndList(featuresCount, tempList, temp, train);

                    temp = String.Format("next2W=" + next2W);
                    AddtoDictAndList(featuresCount, tempList, temp, train);

                    if (WordCount.ContainsKey(curWord) && WordCount[curWord] < rareThreshold)
                    {
                        if (curWord.Length >= 2)
                        {
                            temp = "pref=" + curWord.Substring(0, 1);
                            AddtoDictAndList(featuresCount, tempList, temp, train);

                            //TODO:Check on suffux and prefix length
                            temp = "suf=" + curWord.Substring(curWord.Length - 1, 1);
                            AddtoDictAndList(featuresCount, tempList, temp, train);
                        }
                        if (curWord.Length >= 3)
                        {
                            temp = "pref=" + curWord.Substring(0, 3);
                            AddtoDictAndList(featuresCount, tempList, temp, train);

                            //TODO:Check on suffux and prefix length
                            temp = "suf=" + curWord.Substring(curWord.Length - 3, 3);
                            AddtoDictAndList(featuresCount, tempList, temp, train);
                        }
                        if (curWord.Length >= 4)
                        {
                            temp = "pref=" + curWord.Substring(0, 4);
                            AddtoDictAndList(featuresCount, tempList, temp, train);

                            //TODO:Check on suffux and prefix length
                            temp = "suf=" + curWord.Substring(curWord.Length - 4, 4);
                            AddtoDictAndList(featuresCount, tempList, temp, train);
                        }
                        if (curWord.Length >= 2)
                        {
                            temp = "pref=" + curWord.Substring(0, 2);
                            AddtoDictAndList(featuresCount, tempList, temp, train);
                            //TODO
                            temp = "suf=" + curWord.Substring(curWord.Length - 2, 2);
                            AddtoDictAndList(featuresCount, tempList, temp, train);
                        }

                        if (curWord.Any(char.IsUpper))
                        {
                            temp = "containUC";
                            AddtoDictAndList(featuresCount, tempList, temp, train);

                        }
                        if (curWord.Any(char.IsDigit))
                        {
                            temp = "containNum";
                            AddtoDictAndList(featuresCount, tempList, temp, train);

                        }
                        if (curWord.Contains("-"))
                        {
                            temp = "containHypen";
                            AddtoDictAndList(featuresCount, tempList, temp, train);

                        }
                    }
                    else
                    {
                        temp = String.Format("curW=" + curWord);
                        AddtoDictAndList(featuresCount, tempList, temp, train);
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
                    if (i+3 < wordlist.Length-1)
                    {
                        next2Item = wordlist[i + 3];
                        tempIndex = next2Item.LastIndexOf("/");
                        next2W = next2Item.Substring(0, tempIndex);
                        next2T = next2Item.Substring(tempIndex + 1);
                    }  
                }
            }
        }
        public static void AddtoDictAndList(Dictionary<String, int> featuresCount, List<string> tempList, string temp, bool train)
        {
            if (train)
            {
                if (featuresCount.ContainsKey(temp))
                    featuresCount[temp]++;
                else
                    featuresCount.Add(temp, 1);
            }
            tempList.Add(temp);
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
                    for (int i = 2; i < wordtagPair.Length - 2 ; i++)
                    {
                        tempIndex = wordtagPair[i].LastIndexOf("/");
                        word = wordtagPair[i].Substring(0, tempIndex);
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
