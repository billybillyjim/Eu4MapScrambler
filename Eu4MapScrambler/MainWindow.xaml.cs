using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;

namespace Eu4MapScrambler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Bitmap pMap = new Bitmap(@"C:\Users\there\Desktop\EU4ModTools\Map\provinces.bmp");
        DirectBitmap provinceMap;
        List<Province> Provinces = new List<Province>();
        Dictionary<Color, Province> ColorProvincePairs = new Dictionary<Color, Province>();
        List<Province> EmptyProvinces = new List<Province>();

        HashSet<System.Drawing.Color> usedColors = new HashSet<System.Drawing.Color>();
        List<Country> Countries = new List<Country>();
        HashSet<string> usedTags = new HashSet<string>();
        Dictionary<string, Country> countryTagsDic = new Dictionary<string, Country>();

        string modToolsPath = @"C:\Users\there\Desktop\EU4ModTools\";
        string provincesPath = @"C:\Users\there\Desktop\EU4ModTools\Provinces";
        string countryHistoryPath = @"C:\Users\there\Desktop\EU4ModTools\CountryHistories";
        string provinceColorsPath = @"C:\Users\there\Desktop\EU4ModTools\Map\definition.csv";

        Regex religionRegex = new Regex("religion {1,}= {1,}[A-z]{1,}", RegexOptions.None);
        Regex cultureRegex = new Regex("culture {1,}= {1,}[A-z]{1,}", RegexOptions.None);
        Regex ownerRegex = new Regex(@"owner {1,}= {1,}[A-z]{1,}", RegexOptions.None);
        Regex coreRegex = new Regex("add_core {1,}= {1,}[A-Z]{3}", RegexOptions.None);
        Regex controllerRegex = new Regex("controller {1,}= {1,}[A-Z]{3}", RegexOptions.None);
        Regex nativeRegex = new Regex("native_size", RegexOptions.None);
        private static Random random = new Random();
        Progress<int> progressB;
        public int progressStatus;

        public MainWindow()
        {
            InitializeComponent();
            progressB = new Progress<int>(value => this.progressBar.Value = value);
            //LoadMap();
            //LinkProvincesWithColors();

            //LoadProvinces();
            //CalculateAdjacencies();
            RunStartup();
            
            //RemoveValues();
        }
        public async void RunStartup()
        {
            
            await Task.Run(() => { LoadProvincesFromAdjacencyFile(); } );
            await Task.Run(() => { LoadCountries(null, null); });
        }
        private void RemoveValues()
        {
            string[] lines = File.ReadAllLines(modToolsPath + @"\Adjacencies.csv");
            string[] removedValues = File.ReadAllLines(modToolsPath + @"\Remove.txt");
            string newData = "";
            foreach (string line in lines)
            {
                string newline = line;
                foreach(string val in removedValues)
                {
                    newline = newline.Replace("," + val, "");
                }
                newData += newline + "\n";
            }
            File.WriteAllText(modToolsPath + @"\NewAdjacencies.txt", newData);
        }
        private void LoadProvincesFromAdjacencyFile()
        {
            ((IProgress<int>)progressB).Report(0);
            string[] lines = File.ReadAllLines(modToolsPath + @"\Adjacencies.csv");
            float progress = 0;
            int total = lines.Length;
            int totaladj = 0;
            foreach(string line in lines)
            {
                string[] data = line.Split(',');
                string id = data[0];
                string[] rgb = data[1].Split(':');
                Province p = new Province(Color.FromArgb(int.Parse(rgb[0]), int.Parse(rgb[1]), int.Parse(rgb[2])));
                p.id = id;
                foreach (string s in data.Skip(2))
                {
                    p.adjacentProvinceIDs.Add(s);
                    totaladj++;
                }
                progress++;
                Provinces.Add(p);
                Console.WriteLine($"Loading Province IDs... {(progress / total) * 100}%");
                ((IProgress<int>)progressB).Report((int)((progress / total) * 100));
            }
            progress = 0;
            foreach(Province p in Provinces)
            {
                foreach(string id in p.adjacentProvinceIDs)
                {
                    Province a = Provinces.FirstOrDefault(x => x.id == id);
                    if(a == null)
                    {
                        Console.WriteLine("Province was null with id:" + id);
                    }
                    p.adjacentProvinces.Add(a);
                    progress++;
                }
                Console.WriteLine($"Loading Provinces from ids... {(progress / totaladj) * 100}%");
                ((IProgress<int>)progressB).Report((int)((progress / totaladj) * 100));
            }
        }
        private void WriteBitmapToBits(object sender, RoutedEventArgs e)
        {
            byte[] bits = new byte[pMap.Width * pMap.Height * 4];
            for(int i = 0; i < pMap.Width; i++)
            {
                for (int j = 0; j < pMap.Height; j++)
                {
                    Color c = pMap.GetPixel(i, j);
                    bits[(i * 4) + (j * i * 4)] = c.A;
                    bits[(i * 4) + (j * i * 4) + 1] = c.R;
                    bits[(i * 4) + (j * i * 4) + 2] = c.G;
                    bits[(i * 4) + (j * i * 4) + 3] = c.B;
                }
            }
            File.WriteAllBytes("BitArray.txt", bits);
        }
        private void LoadMap()
        {
            int mapSize = pMap.Width * pMap.Height;
            float progress = 0;
            provinceMap = new DirectBitmap(pMap.Width, pMap.Height);
            for (int i = 0; i < provinceMap.Width; i++)
            {
                for (int j = 0; j < provinceMap.Height; j++)
                {
                    provinceMap.SetPixel(i, j, pMap.GetPixel(i, j));
                    progress++;
                }
                Console.WriteLine($"Copying Map... {(progress / mapSize) * 100f}% Complete");
            }
        }
        private void LoadProvinces()
        {
            int mapSize = pMap.Width * pMap.Height;
            float progress = 0;

            for (int i = 0; i < pMap.Width; i++)
            {
                for (int j = 0; j < pMap.Height; j++)
                {
                    progress++;
                    Color c = provinceMap.GetPixel(i, j);
                    if (usedColors.Contains(c) == false)
                    {
                        ColorProvincePairs[c].firstX = i;
                        ColorProvincePairs[c].firstY = j;

                        usedColors.Add(c);
                        Console.WriteLine($"Adding color to pairs:{c.ToString()}");

                    }

                }
                Console.WriteLine($"Adding Provinces... {(progress / mapSize) * 100f}% Complete");
            }
        }
        private void CalculateAdjacencies(object sender = null, RoutedEventArgs e = null)
        {

            float progress = 0;
            foreach(Province p in Provinces)
            {
                progress++;
                for (int i = Math.Max(0, p.firstX - 200); i < Math.Min(p.firstX + 200, provinceMap.Width); i++)
                {
                    for (int j = Math.Max(0, p.firstY - 200); j < Math.Min(p.firstY + 200, provinceMap.Height); j++)
                    {
                        Color c = provinceMap.GetPixel(i, j);
                        if(c == p.color)
                        {
                            if(i > 0)
                            {
                                Color co = provinceMap.GetPixel(i - 1, j);
                                p.AddColor(co);
                                p.AddProvince(ColorProvincePairs[co]);
                            }
                            if(i < provinceMap.Width - 1)
                            {
                                Color co = provinceMap.GetPixel(i + 1, j);
                                p.AddColor(co);
                                p.AddProvince(ColorProvincePairs[co]);
                            }
                            if(j > 0)
                            {
                                Color co = provinceMap.GetPixel(i, j - 1);
                                p.AddColor(co);
                                p.AddProvince(ColorProvincePairs[co]);
                            }
                            if(j < provinceMap.Height - 1)
                            {
                                Color co = provinceMap.GetPixel(i, j + 1);
                                p.AddColor(co);
                                p.AddProvince(ColorProvincePairs[co]);
                            }
                        }
                        
                    }
                }
                Console.WriteLine($"Adding Provinces... {(progress / Provinces.Count) * 100f}% Complete");
                Console.WriteLine($"Added {p.adjacentColors.Count } adjacent provinces to province:{ p.color.ToString() }");

            }
            Console.WriteLine($"There are {Provinces.Count} Provinces.");
            SaveAdjacencies();
        }
        private void SaveAdjacencies()
        {
            StringBuilder sb = new StringBuilder();
            foreach(Province pro in Provinces)
            {
                sb.Append(pro.id);
                sb.Append(',');
                sb.Append(pro.color.R);
                sb.Append(':');
                sb.Append(pro.color.G);
                sb.Append(':');
                sb.Append(pro.color.B);

                foreach(Province p in pro.adjacentProvinces)
                {
                    sb.Append(',');
                    sb.Append(p.id);
                    
                }
                sb.Append('\n');
            }
            File.WriteAllText(modToolsPath + @"\Adjacencies.csv", sb.ToString());
        }

        private void LoadCountries(object sender, RoutedEventArgs e)
        {
            //Take every province, check owner, add to country list if not on list already

            DirectoryInfo di = new DirectoryInfo(provincesPath);
            var files = di.GetFiles();
            float progress = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string text = File.ReadAllText(files[i].FullName);
                MatchCollection mc = ownerRegex.Matches(text);
                MatchCollection nmc = nativeRegex.Matches(text);
                if(mc.Count == 0 || nmc.Count > 0)
                {
                    continue;
                }
                else
                {
                    Match match = mc[0];

                    string tag = match.Value.Substring(match.Value.Length - 3);

                    if (usedTags.Contains(tag) == false)
                    {
                        usedTags.Add(tag);
                        Country c = new Country(tag);
                        countryTagsDic.Add(tag, c);
                        Countries.Add(c);
                        Console.WriteLine($"Adding tag {tag}.");
                    }
                    countryTagsDic[tag].totalOwnedProvinces++;
                    Console.WriteLine(tag + " owns " + files[i].Name + ", total owned provinces:" + countryTagsDic[tag].totalOwnedProvinces);
                    progress++;
                    ((IProgress<int>)progressB).Report((int)((progress / files.Length) * 100));

                    //TODO:Add province to empty provinces if empty.
                    //EmptyProvinces.Add()
                }


            }

            //Set culture and religion based on country files.
            DirectoryInfo dir = new DirectoryInfo(countryHistoryPath);
            files = dir.GetFiles();
            progress = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string tag = files[i].Name.Substring(0, 3);
                if (usedTags.Contains(tag) == false)
                {
                    continue;
                }
                string text = File.ReadAllText(files[i].FullName);
                countryTagsDic[tag].religion = religionRegex.Matches(text)[0].Value;
                countryTagsDic[tag].culture = cultureRegex.Matches(text)[0].Value;
                Console.WriteLine($"Setting {tag} religion: {countryTagsDic[tag].religion} and culture: {countryTagsDic[tag].culture}");
                progress++;
                ((IProgress<int>)progressB).Report((int)((progress / files.Length) * 100));
            }
        }
        private void LinkProvincesWithColors()
        {
            string[] text = File.ReadAllLines(provinceColorsPath);
            foreach(string p in text.Skip(1))
            {
                if (p.Contains("Unused") || p.Contains("RNW"))
                {
                    continue;
                }
                string[] sp = p.Split(';');
                string pid = sp[0];
                string r = sp[1];
                string g = sp[2];
                string b = sp[3];
                
                Color c = Color.FromArgb(255, int.Parse(r), int.Parse(g), int.Parse(b));
                Province pr = new Province(c);
                Provinces.Add(pr);
                ColorProvincePairs.Add(c, pr);
                ColorProvincePairs[c].id = pid;
                
            }
        }
        private List<Province> GetAdjacentProvinces(int amt)
        {
            List<Province> provs = new List<Province>();
            Province p = GetRandomProvince();
            p.used = true;
            provs.Add(p);
            if(amt == 1)
            {
                return provs;
            }
            for (int i = 0; i < amt - 1; i++)
            {
                Province pr = p.GetUnusedAdjacentProvince();
                if (pr == null)
                {
                    foreach(Province pro in provs)
                    {
                        pro.used = false;
                    }
                    return GetAdjacentProvinces(amt);
                }
                pr.used = true;
                provs.Add(pr);
            }
            return provs;
        }
        private void ScrambleCountries(object o, RoutedEventArgs e)
        {
            if(Countries.Count == 0)
            {
                Console.WriteLine("Need to load countries first.");
                return;
            }
            if(Provinces.Count == 0)
            {
                Console.WriteLine("Need to load provinces first.");
                return;
            }
            EmptyProvinces.Clear();
            EmptyProvinces.AddRange(Provinces);
            float progress = 0;
            List<Country> unusedCountries = new List<Country>();
            unusedCountries.AddRange(Countries);

            Country current = unusedCountries[random.Next(unusedCountries.Count)];
            foreach(Country c in unusedCountries)
            {
                if(c.totalOwnedProvinces > current.totalOwnedProvinces)
                {
                    current = c;
                }
            }
            DirectoryInfo di = new DirectoryInfo(provincesPath);
            var files = di.GetFiles();
            ShuffleProvinceOrder();
            foreach (Province p in Provinces)
            {
                
                if(p.owner == "unset" && p.used == false)
                {
                    FileInfo f = files.FirstOrDefault(x => x.Name.StartsWith(p.id));
                    if(f == null)
                    {
                        Console.WriteLine($"No file found with id:{p.id}.");
                        continue;
                    }
                    string oldData = File.ReadAllText(f.FullName);
                    if(ownerRegex.Matches(oldData).Count == 0)
                    {
                        continue;
                    }
                    List<Province> pList = new List<Province>();
                    int found = 1;
                    int lastFound = found;
                    pList.Add(p);
                    p.check = true;
                    while (found < current.totalOwnedProvinces )
                    {
                        List<Province> newFinds = new List<Province>();

                        foreach(Province pro in pList)
                        {
                            foreach (Province adj in pro.adjacentProvinces)
                            {
                                if(adj.check == false)
                                {
                                    adj.check = true;
                                    if(newFinds.Contains(adj) == false)
                                    {
                                        newFinds.Add(adj);
                                        found++;
                                    }

                                    if (found == current.totalOwnedProvinces)
                                    {
                                        break;
                                    }
                                }
                            } 
                        }

                        if(found == 1 || lastFound == found)
                        {
                            break;
                        }
                        lastFound = found;
                        pList.AddRange(newFinds);
                    }

                    foreach(Province adj in pList)
                    {
                        adj.used = true;
                        adj.SetOwner(current);
                        WriteNewProvinceFile(adj);
                        //Console.WriteLine($"Writing province {adj.id} for {current.tag}.");
                        progress++;
                        ((IProgress<int>)progressB).Report((int)((progress / Provinces.Count) * 100));
                    }

                    
                    unusedCountries.Remove(current);
                    if(unusedCountries.Count == 0)
                    {
                        return;
                    }
                    current = unusedCountries[random.Next(unusedCountries.Count)];
                    foreach (Country c in unusedCountries)
                    {
                        if (c.totalOwnedProvinces > current.totalOwnedProvinces)
                        {
                            current = c;
                        }
                    }


                }
            }
            FillOutRemainingMap(10);

        }
        public void ShuffleProvinceOrder()
        {
            int i = Provinces.Count;
            while(i > 1)
            {
                i--;
                int k = random.Next(i + 1);
                Province p = Provinces[k];
                Provinces[k] = Provinces[i];
                Provinces[i] = p;
            }

        }
        public void FillOutRemainingMap(object sender, RoutedEventArgs e)
        {
            FillOutRemainingMap(1);
        }
        public void FillOutRemainingMap(int timesToLoop = 3)
        {
            float progress = 0;
            for(int i = 0; i < timesToLoop; i++)
            {
                Console.WriteLine("Writing empty provinces...");
                foreach (Province p in Provinces)
                {
                    if (p.used == false)
                    {
                        foreach (Province pro in p.adjacentProvinces)
                        {
                            if (pro.used)
                            {
                                Country c = Countries.FirstOrDefault(x => x.tag == pro.owner);
                                if (c == null)
                                {
                                    Console.WriteLine($"Country:{pro.owner} was null.");
                                    continue;
                                }
                                p.used = true;
                                p.SetOwner(c);

                                WriteNewProvinceFile(p);
                                Console.WriteLine($"Writing new province data for {pro.id}");
                                progress++;
                                ((IProgress<int>)progressB).Report((int)((progress / Provinces.Count) * 100));
                            }
                        }
                    }
                }
            }

        }
        private void WriteNewProvinceFile(Province p)
        {
            //GetMatching file
            DirectoryInfo di = new DirectoryInfo(provincesPath);
            var files = di.GetFiles();
            
            FileInfo f = files.FirstOrDefault(x => x.Name.StartsWith(p.id));
            if(f == null)
            {
                Console.WriteLine("Failed to find file with starting id of " + p.id);
            }
            else
            {
                string oldData = File.ReadAllText(f.FullName);
                string newData = oldData;
                foreach(Match m in ownerRegex.Matches(oldData))
                {
                    newData = newData.Replace(m.Value, "owner = " + p.owner);
                }
                foreach(Match m in religionRegex.Matches(oldData))
                {
                    newData = newData.Replace(m.Value, p.religion);
                }
                foreach(Match m in cultureRegex.Matches(oldData))
                {
                    newData = newData.Replace(m.Value, p.culture);
                }
                foreach (Match m in coreRegex.Matches(oldData))
                {
                    newData = newData.Replace(m.Value, "add_core = " + p.owner);
                }
                foreach (Match m in controllerRegex.Matches(oldData))
                {
                    newData = newData.Replace(m.Value, "controller = " + p.owner);
                }

                Console.WriteLine("Writing text for file: " + f.Name);
                File.WriteAllText(provincesPath + @"\New\" + f.Name, newData);
                
            }
            //Change Strings
            //Write New File To location
        }
        private Province GetRandomProvince()
        {
            return EmptyProvinces[random.Next(Provinces.Count)];
        }
    }
}
