using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Client.Helper
{
    public class ReadFileLineByLine
    {
        private List<string> _fileAsList;

        public ReadFileLineByLine()
        {
        }

        public List<string> ReadFile(string path)
        {
            _fileAsList = new List<string>();
            var readFile = new string[] {};
            var fileName = path.Split('/');
            try
            {
                readFile = File.ReadAllLines(path);
            }
            catch (Exception)
            {
                var ofd = new OpenFileDialog
                {
                    Filter = "CSV Files (.csv)|*.csv",
                    Multiselect = false,
                    FilterIndex = 1,
                    Title = fileName.Last(),
                    DefaultExt = ".csv",
                    FileName = fileName.Last().Substring(0, fileName.Last().IndexOf('.'))
                };
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    ReadFile(ofd.FileName);
                }
                else
                {
                    Console.WriteLine("'{0}' Kunne ikke findes, stien eksisterer ikke, eller filnavnet er forkert.\n" +
                                      "\nNavnet og stien for Madklub, skulle være\"C://KK24//Madklub.csv\"." +
                                      "\nNavnet og stien for Indkøbslisten, skulle være \"C://KK24//KøkkenIndkøb.csv\".",
                        path);
                    Console.ReadKey();
                    Environment.Exit(0);
                }
            }

            foreach (var line in readFile)
                _fileAsList.Add(line);

            return _fileAsList;
        }
    }
}