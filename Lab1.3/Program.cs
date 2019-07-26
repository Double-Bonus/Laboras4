using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Lab3._3
{
    class Program
    {
        const string htmlDataFile = @"Studentai.html";
        const string csvDataFile = @"Studentai.csv";
        const string wrongDataFile = @"KlaidingiDuomenys.csv";

        const string tablePattern = "<table.*?>(.*?)</table>";
        const string thPattern = "<th.*?>(.*?)</th>";
        const string trPattern = "<tr>(.*?)</tr>";
        const string tdPattern = "<td.*?>(.*?)</td>";

        const string csvSeparator = ";";

        enum Fields
        {
            MemberType, MemberName, MemberSurname, BirthDate,
            MemberPhone
        };

        static void Main(string[] args)
        {
            //CultureInfo.CurrentCulture = new CultureInfo("lt-LT");
            Console.OutputEncoding = Encoding.GetEncoding(1257);

            Program p = new Program();
            string[] csvLines = p.ConvertToCsv(htmlDataFile);
            csvLines = p.FixPhoneNumbers(csvLines, (int)Fields.MemberPhone);
            csvLines = p.FixNames(csvLines, (int)Fields.MemberName);
            csvLines = p.FixNames(csvLines, (int)Fields.MemberSurname);
            csvLines = p.RemoveTags(csvLines);
            csvLines = p.FixDate(csvLines, (int)Fields.BirthDate);


            int wrongCount;
            string[] wrongDataLines = p.GetLinesWithWrongDates(csvLines, (int)Fields.BirthDate, out wrongCount);
            p.WriteLinesToFile(wrongDataFile, wrongDataLines, wrongCount);

            int correctCount;
            string[] correctDataLines = p.RemoveWrongDataLines(csvLines, wrongDataLines, wrongCount, out correctCount);
            p.WriteLinesToFile(csvDataFile, correctDataLines, correctCount);


            foreach (string line in csvLines)
            {
                Console.WriteLine(line);
            }

            Console.ReadLine();

        }

        private string[] ConvertToCsv(string fileName)
        {
            string fileContent = File.ReadAllText(fileName);
            MatchCollection table_matches = Regex.Matches(fileContent, tablePattern,
                RegexOptions.Singleline);
            //ištraukiam visą lentelę, kitkas mums neįdomu.
            string tableString = table_matches[0].Value;
            //ieškome visų lentelės eilučių
            MatchCollection tr_matches = Regex.Matches(tableString, trPattern, RegexOptions.Singleline);
            string[] csvLines = new string[tr_matches.Count];
            //pirmoje eilutėje ieškome antraščių
            MatchCollection th_matches = Regex.Matches(tr_matches[0].Value, thPattern, RegexOptions.Singleline);
            //imame pirmąją antraštę
            string title = th_matches[0].Groups[1].Value;
            csvLines[0] = title;

            //einame per likusias eilutes
            for (int i = 0; i < tr_matches.Count; i++)
            {
                StringBuilder line = new StringBuilder();
                MatchCollection td_matches = Regex.Matches(tr_matches[i].Value, tdPattern, RegexOptions.Singleline);

                for (int j = 0; j < td_matches.Count; j++) //einame per stulpelius
                {
                    //nuskaitom langelio reikšmę irdedam į eilutę, po jos dedam skyriklį
                    line.Append(td_matches[j].Groups[1].Value).Append(csvSeparator);
                }

                csvLines[i] = line.ToString();
            }

            return csvLines;
        }

        private string[] RemoveTags(string[] csvLines)
        {
            for (int i = 0; i < csvLines.Length; i++)
            {
                csvLines[i] = Regex.Replace(csvLines[i], "<.*?>", string.Empty);
            }
            return csvLines;
        }


        private string[] FixNames(string[] csvLines, int fieldToCorrect)
        {
            for (int i = 1; i < csvLines.Length; i++)
            {
                string[] stringFields = csvLines[i].Split(csvSeparator[0]);
                if (Char.IsLower(stringFields[fieldToCorrect][0]))
                {
                    stringFields[fieldToCorrect] = stringFields[fieldToCorrect].Substring(0,
                        1).ToUpper() + stringFields[fieldToCorrect].Substring(1).ToLower();
                    csvLines[i] = string.Join(csvSeparator, stringFields);
                }
            }
            return csvLines;
        }


        public bool IsPhoneNumber(string number)
        {
            if (number[0] != '+') return false;
            if (number.Length != 12) return false;

            for (int i = 1; i < number.Length; i++)
            {
                if ((number[i] < '0') || (number[i] > '9')) return false;
            }
            return true;
        }


        private string[] FixPhoneNumbers(string[] csvLines, int fieldToCorrect)
        {
            for (int i = 1; i < csvLines.Length; i++)
            {
                string[] stringFields = csvLines[i].Split(csvSeparator[0]);
                if (!IsPhoneNumber(stringFields[fieldToCorrect]))
                {
                    if (stringFields[fieldToCorrect][0] == '8')
                    {
                        stringFields[fieldToCorrect] = "+370" + stringFields[fieldToCorrect].Substring(1);
                        csvLines[i] = string.Join(csvSeparator, stringFields);
                    }
                }
            }
            return csvLines;
        }

        public bool IsValidDate(string date)
        {
            return Regex.Match(date, @"\b(\d{4})(-)(0[1-9]|1[0-2])(-)(0[1-9]|[12]\d|30|31)\b").Success;
        }


        private string[] FixDate(string[] csvLines, int fieldToCorrect)
        {
            for (int i = 1; i < csvLines.Length; i++)
            {
                string[] stringFields = csvLines[i].Split(csvSeparator[0]);
                if (!IsValidDate(stringFields[fieldToCorrect]))
                {
                    
                    if (stringFields[fieldToCorrect].Length == 8 && !stringFields[fieldToCorrect].Contains('-'))
                    {
                        stringFields[fieldToCorrect] =  stringFields[fieldToCorrect].Substring(0, 4) + '-' + stringFields[fieldToCorrect].Substring(4, 2) +
                            '-' + stringFields[fieldToCorrect].Substring(6, 2);
                        csvLines[i] = string.Join(csvSeparator, stringFields);
                    } 
                }
            }
            return csvLines;
        }


        private string[] GetLinesWithWrongDates(string[] csvLines, int fieldToCheck, out int wrongCount)
        {
            string[] wrongDataLines = new string[csvLines.Length];

            wrongCount = 0;
            for (int i = 1; i < csvLines.Length; i++)
            {
                string[] stringFields = csvLines[i].Split(csvSeparator[0]);
                if (!IsValidDate(stringFields[fieldToCheck]))
                {
                    wrongDataLines[wrongCount++] = csvLines[i];
                }
            }
            return wrongDataLines;
        }



        private void WriteLinesToFile(string dataFile, string[] data, int dataCount)
        {
            using (var fileHandle = File.CreateText(dataFile))
            {
                for (int i = 0; i < dataCount; i++)
                {
                    fileHandle.WriteLine(data[i]);
                }
            }
        }

        private string[] RemoveWrongDataLines(string[] csvLines, string[] wrongDataLines, int wrongCount, out int correctCount)
        {
            string[] correctDataLines = new string[csvLines.Length];

            correctCount = 0;
            for (int i = 1; i < csvLines.Length; i++)
            {
                bool foundEqual = false;
                for (int j = 0; j < wrongCount; j++)
                {
                    if (csvLines[i].Equals(wrongDataLines[j]))
                    {
                        foundEqual = true;
                    }
                }
                if (!foundEqual)
                    correctDataLines[correctCount++] = csvLines[i];
            }
            return correctDataLines;
        }




    }
}
