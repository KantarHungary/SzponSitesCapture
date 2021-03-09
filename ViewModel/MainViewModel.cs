using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using ModelHUEF.Entities;
using HtmlAgilityPack;
using ModelHUEF.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Text;
using System.Windows;
using System.Diagnostics;
using System.Threading;
using System.Collections.ObjectModel;

namespace SzponSitesCapture.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        protected readonly IDataService _schd;
        WebBrowser webBrowser1;
        public static string _userAgent = "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 88.0.4324.150 Safari / 537.36";
        string _defaultPath = @"n:\Data\iSzpon\";
        string _logPath = @"C:\Kantar\";

        string _hibasoldalak;
        public string HibasOldalak { get => _hibasoldalak; set { Set(() => HibasOldalak, ref _hibasoldalak, value); } }

        int _secdelay = 6;
        public int Secdelay { get => _secdelay; set { Set(() => Secdelay, ref _secdelay, value); } }

        bool _kellenter = false;
        public bool KellEnter { get => _kellenter; set { Set(() => KellEnter, ref _kellenter, value); } }

        bool _notisrunning = true;
        public bool NotIsRunning { get => _notisrunning; set { Set(() => NotIsRunning, ref _notisrunning, value); } }

        List<string> _webpagesLinks;
        public List<string> WebPagesLinks { get => _webpagesLinks; set { Set(() => WebPagesLinks, ref _webpagesLinks, value); } }

        ObservableCollection<string> _errorLinks;
        public ObservableCollection<string> ErrorLinks { get => _errorLinks; set { Set(() => ErrorLinks, ref _errorLinks, value); } }

        string _errorLink;
        public string ErrorLink { get => _errorLink; set { Set(() => ErrorLink, ref _errorLink, value); } }

        RelayCommand _next;
        public RelayCommand CmdStartCapture => _next ?? (_next = new RelayCommand(async () => {
            // Üres mappa ellenõrzse: Downloads és Iszpon
            NotIsRunning = false;
            await GetLinksFromHtmlText();
            HibasOldalak = "Cmd-ben két cmd között";
            //CaptureSzponPagesFireShot();
            CaptureMozillaPages();
            NotIsRunning = true;
        }));

        RelayCommand _cmdpreopenpages;
        public RelayCommand CmdPreOpenPages => _cmdpreopenpages ?? (_cmdpreopenpages = new RelayCommand(() => { PreOpenPages(); }));

        RelayCommand _cmdujrakepvan;
        public RelayCommand CmdUjraKep => _cmdujrakepvan ?? (_cmdujrakepvan = new RelayCommand(() => { HibasLinkUjraKep(); }));

        RelayCommand _cmdadd;
        public RelayCommand CmdAdd => _cmdadd ?? (_cmdadd = new RelayCommand(() => { AddErroritemToTB(); }));

        RelayCommand _cmdloadprevdatas;
        public RelayCommand CmdLoadPrevDatas => _cmdloadprevdatas ?? (_cmdloadprevdatas = new RelayCommand(() =>
        {
            // LoadPrevDatas
            FileInfo[] _finfos = new DirectoryInfo(_logPath).GetFiles("szponerror*.txt");
            FileInfo _fi = _finfos.OrderBy(f => f.LastAccessTime).FirstOrDefault();

            if (_fi == null) return;
            
            var logFile = File.ReadAllLines(_fi.FullName);

            ErrorLinks = new ObservableCollection<string>(logFile);
            //FileInfo ErrorLog = new FileInfo(_logPath + "szponErrorLog_" + DateTime.Now.ToString(new System.Globalization.CultureInfo("hu-HU")).Replace(".", "").Replace(":", "").Replace(' ', '_') + ".txt");
        }));
        
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            _schd = dataService;

            HibasOldalak = "Hibasoldalak";

            WebPagesLinks = new List<string>();

            //List<WebSite> _szponpages = _schd.GetList<WebSite>().Where(w => w.Name.EndsWith("ON.")).ToList();

            int dskjhf = 7635;
            if (IsInDesignMode)
            {
                ErrorLinks = new ObservableCollection<string>() { "nlc.hu/askjh/", "nlc.hu/tzjtzjrjh/", "kkv.hu/ash/", "nlc.hu/assafafh/", "femina.hu/sdfkjh/" };
            }
            else
            {
                // Code runs "for real"
                /*
                List<WebSite> _szponpages = _schd.GetList<WebSite>().Where(w => w.Name.EndsWith("ON.")).ToList();

                foreach (var item in _szponpages)
                {
                    WebPagesLinks.AddRange(item.LinksArray);
                }

                ErrorLinks = new ObservableCollection<string>() { 10.ToString("d4")+"-"+WebPagesLinks[10],
                    91.ToString("d4") + "-"+WebPagesLinks[91],
                    171.ToString("d4") + "-"+WebPagesLinks[171],
                    191.ToString("d4")+"-"+WebPagesLinks[191],
                    197.ToString("d4")+"-"+WebPagesLinks[197] };*/
            }

            GC.Collect();
        }


        private async Task GetLinksFromHtmlText()
        {
            string whatUrLookingFor = string.Empty;
            string body = "";
            Uri address;
            string WebSiteID;
            string contentType = "application/x-www-form-urlencoded";
            string result;
            System.Windows.Forms.HtmlDocument HtmlDoc;
            HtmlAgilityPack.HtmlDocument HtmlBody;
            List<string> _hrefs = new List<string>();

            
            // teszt idejére kivéve
            List<WebSite> _szponpages = _schd.GetList<WebSite>().Where(w => w.Name.EndsWith("ON.")).ToList();

            foreach (var item in _szponpages)
            {
                WebPagesLinks.AddRange(item.LinksArray);
            }
            


            HibasOldalak = "Kész a lista, házipatika jön";

            string webcim = "https://www.hazipatika.com";
            result = PostHttp(webcim, body, contentType);      //myUri
            WebSiteID = "2604";
            _hrefs = new List<string>();

            HtmlBody = new HtmlAgilityPack.HtmlDocument();
            HtmlBody.LoadHtml(result);

            /// A jobb oldali egyéb - "Népszerû" - linkek kinyerése
            try
            {
                HtmlAgilityPack.HtmlDocument _box = new HtmlAgilityPack.HtmlDocument();
                _box.LoadHtml(HtmlBody.GetElementbyId("box-nepszeru").InnerHtml);

                HtmlNodeCollection nodes = _box.DocumentNode.ChildNodes;
                foreach (HtmlNode node in nodes)
                {
                    HtmlNodeCollection children = node.SelectNodes(".//a");
                    if (children != null)
                        foreach (HtmlNode child in children)
                        {
                            //_hrefs.Add(WebSiteID + "-" + child.GetAttributeValue("href", ""));
                            string _hrefname = child.GetAttributeValue("href", "");
                            _hrefname = _hrefname.Substring(_hrefname.IndexOf("://") + 3);
                            _hrefs.Add(WebSiteID + "-" + _hrefname);
                        }
                }
            }
            catch (Exception _exc)
            {
                System.Windows.MessageBox.Show("A " + webcim + " extra menüiõ nem tölthetõk be.", "Problem", MessageBoxButton.OK);
            }

            /// A felsõ menben szereplõ szponzorált linkek (forgolódó feliratok) kinyerése
            try
            {
                var divsWithText = HtmlBody
                .DocumentNode
                .Descendants("nav")
                .Where(node => node.Descendants()
                .Any(des => des.NodeType == HtmlNodeType.Text))
                .ToList();

                HtmlAgilityPack.HtmlDocument mainnav = new HtmlAgilityPack.HtmlDocument();
                mainnav.LoadHtml(divsWithText[0].InnerHtml);

                var nodesWithSmallerCells = mainnav.DocumentNode.SelectNodes("//div[@class='bottom']");
                if (nodesWithSmallerCells != null)
                    foreach (HtmlNode node in nodesWithSmallerCells)
                    {
                        HtmlNodeCollection children = node.SelectNodes(".//a");
                        if (children != null)
                            foreach (HtmlNode child in children)
                            {
                                //_hrefs.Add(WebSiteID + "-" + child.GetAttributeValue("href", ""));
                                string _hrefname = child.GetAttributeValue("href", "");
                                _hrefname = _hrefname.Substring(_hrefname.IndexOf("://") + 3);
                                _hrefs.Add(WebSiteID + "-" + _hrefname);
                            }
                    }
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("házipatika egyéb aloldalai nem tölthetõk be.", "Izé", MessageBoxButton.OK);
            }

            WebPagesLinks.AddRange(_hrefs);


            

            HibasOldalak = "nlc jön";


            address = new Uri("https://nlc.hu");
            HtmlDoc = await GetPageHTMLAsync(address);
            WebSiteID = "2606";
            _hrefs = new List<string>();

            HtmlBody = new HtmlAgilityPack.HtmlDocument();
            HtmlBody.LoadHtml(HtmlDoc.Body.InnerHtml);

            var divsWithText2 = HtmlBody.DocumentNode
                .Descendants("div")
                .Where(node => node.Descendants()
                .Any(des => des.NodeType == HtmlNodeType.Text))
                .ToList();
            var divsWithInnerHtmlMatching2 =
                divsWithText2
                    .Where(div => div.InnerHtml.Contains("m-dragee__menu"))
                    .ToList();
            whatUrLookingFor = divsWithInnerHtmlMatching2.Last().InnerHtml;


            HtmlAgilityPack.HtmlDocument extranav = new HtmlAgilityPack.HtmlDocument();
            extranav.LoadHtml(whatUrLookingFor);

            HtmlNodeCollection nodesnlc = extranav.DocumentNode.ChildNodes;
            foreach (HtmlNode node in nodesnlc)
            {
                HtmlNodeCollection children = node.SelectNodes(".//a");
                if (children != null)
                    foreach (HtmlNode child in children)
                    {
                        string _hrefname = child.GetAttributeValue("href", "");
                        //_hrefname = _hrefname.Substring(_hrefname.IndexOf("://") + 3);
                        _hrefs.Add(WebSiteID + "-" + _hrefname);
                    }
            }

            WebPagesLinks.AddRange(_hrefs);
            
            

            HibasOldalak = "femina jön";

            address = new Uri("https://femina.hu");
            HtmlDoc = await GetPageHTMLAsync(address);
            WebSiteID = "2439";
            _hrefs = new List<string>();

            HtmlBody = new HtmlAgilityPack.HtmlDocument();
            HtmlBody.LoadHtml(HtmlDoc.Body.InnerHtml);

            try
            {
                var divsWithText = HtmlBody
                .DocumentNode
                .Descendants("nav")
                .Where(node => node.Descendants()
                .Any(des => des.NodeType == HtmlNodeType.Text))
                .ToList();

                HtmlAgilityPack.HtmlDocument mainnav = new HtmlAgilityPack.HtmlDocument();
                mainnav.LoadHtml(divsWithText.Where(l => l.OuterHtml.Contains("extramenu")).FirstOrDefault().InnerHtml);

                HtmlNodeCollection nodesfemina = mainnav.DocumentNode.ChildNodes;
                foreach (HtmlNode node in nodesfemina)
                {
                    HtmlNodeCollection children = node.SelectNodes(".//a");
                    if (children != null)
                        foreach (HtmlNode child in children)
                        {
                            //_hrefs.Add(WebSiteID + "-" + child.GetAttributeValue("href", ""));
                            string _hrefname = child.GetAttributeValue("href", "");
                            //_hrefname = _hrefname.Substring(_hrefname.IndexOf("://") + 3);
                            _hrefs.Add(WebSiteID + "-" + _hrefname);
                        }
                }
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("házipatika egyéb aloldalai nem tölthetõk be.", "Izé", MessageBoxButton.OK);
            }

            WebPagesLinks.AddRange(_hrefs);



            RaisePropertyChanged("WebPagesLinks");
            HibasOldalak = "Kész a linkgyûjtés";
        }

        private async Task<System.Windows.Forms.HtmlDocument> GetPageHTMLAsync(Uri address)
        {
            System.Windows.Forms.HtmlDocument HtmlDoc;

            webBrowser1 = new WebBrowser();
            webBrowser1.ScriptErrorsSuppressed = true;
            webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(mywebBrowser_DocumentCompleted);

            webBrowser1.Navigate(address);

            await Task.Delay(5000);
            HtmlDoc = webBrowser1.Document;
            webBrowser1.Dispose();

            return HtmlDoc;
        }

        private void mywebBrowser_DocumentCompleted(Object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //Until this moment the page is not completely loaded
            System.Windows.Forms.HtmlDocument doc = webBrowser1.Document;
            HtmlElementCollection tagCollection;
            tagCollection = doc.GetElementsByTagName("div");
            //HibasOldalak = doc.GetElementsByTagName("html")[0].InnerHtml;
        }

        private void CaptureSzponPagesFireShot()
        {
            ErrorLinks = new ObservableCollection<string>();
            FileInfo Logtext = new FileInfo(@"C:\Kantar\szponcaptureLog_" + DateTime.Now.ToString(new System.Globalization.CultureInfo("hu-HU")).Replace(".", "").Replace(":", "").Replace(' ', '_') + ".txt");
            string[] _problemasweboldalak = { "2428", "2664", "2606", "2452", "2463" };
            // 2428, 2606, 2452, 2463, 

            /// Hol a #455 idokep vagy instyle
            /// #594 agronaplo vagy babanet -> utolsó agronapló(2452) és 1. babanet összekeveredett
            /// Hol az #596 babanet? -> átkerült másik babanetre.
            /// Valószínûleg ha több fájl van már a Captured mappában, akkor gondot okoz a megfelelõ felismerése. Valahog meg kéne oldani, hogy ne lehessen 2 fájl.
            /// Hol az #600 -> 657 helyébe lépett
            /// Hol az #657 -> 675 helyébe lépett
            /// Hol az #675

            HibasOldalak = "";
            for (int i = 0; i < WebPagesLinks.Count; i++)
            {
                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(WebPagesLinks[i]);

                string _websitecode = WebPagesLinks[i].Substring(0, 4);
                int _delay = _problemasweboldalak.Contains(_websitecode) ? 2 : 1;

                var _proc = Process.Start(WebPagesLinks[i].Substring(5));
                Thread.Sleep(Secdelay * 2000 * _delay);

                SetCursorPos(1690, 30);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(Secdelay * 100);

                SendKeys.SendWait("^+{Y}");     /// ctrl+shift+y
                Thread.Sleep(Secdelay * 4000 * _delay);

                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(" Elvileg ok a képmentés. ");

                FileInfo[] _finfos = new DirectoryInfo(_defaultPath + @"Captured\").GetFiles("*.jpg");
                FileInfo _fi = _finfos.FirstOrDefault();

                if (_fi == null)
                {
                    using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(" Nem sikerült a mentés, nincs fájl. Várunk még egy kicsit. ");

                    Thread.Sleep(Secdelay * 6000);

                    _finfos = new DirectoryInfo(_defaultPath + @"Captured\").GetFiles("*.jpg");

                    if (_finfos.Count() < 1)
                    {
                        SetCursorPos(1690, 30);
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        Thread.Sleep(Secdelay * 100);

                        SendKeys.SendWait("^+{Y}");     /// ctrl+shift+y
                        Thread.Sleep(Secdelay * 7000);

                        using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(" 2. próbálkozás");

                        _finfos = new DirectoryInfo(_defaultPath + @"Captured\").GetFiles("*.jpg");

                        while (_finfos.Count() > 1)
                        {
                            using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine("Túl sok a fájl");

                            FileInfo _filast = _finfos.OrderBy(f => f.LastAccessTime).FirstOrDefault();
                            _filast.Delete();
                        }
                    }

                    _fi = _finfos.FirstOrDefault();

                    HibasOldalak += WebPagesLinks[i] + Environment.NewLine;
                    ErrorLinks.Add(WebPagesLinks[i]);
                    continue;
                }

                SetCursorPos(1690, 30);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(Secdelay * 100);

                SendKeys.SendWait("^w");        /// ctrl+w
                Thread.Sleep(Secdelay * 300);

                string _targetpath = Path.Combine(_defaultPath, Path.GetFileNameWithoutExtension(_fi.Name) + "-" + WebPagesLinks[i].Replace(":", "=").Replace("*", "'-'").Replace("/", "'%'").Replace("?", "'k'") + _fi.Extension);
                //System.Windows.MessageBox.Show(_fi.FullName + Environment.NewLine + _targetpath, "Path", MessageBoxButton.OK);
                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(" Kép átmozgatása - " + _targetpath);

                File.Move(_fi.FullName, _targetpath);
                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(" - sikerült. " + Environment.NewLine);

            }
        }

        public static string PostHttp(string url, string body, string contentType)
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(new Uri(url));

            httpWebRequest.ContentType = contentType;
            httpWebRequest.Method = "POST";
            httpWebRequest.UserAgent = _userAgent;
            httpWebRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            httpWebRequest.Timeout = 20000;

            byte[] btBodys = Encoding.UTF8.GetBytes(body);
            httpWebRequest.ContentLength = btBodys.Length;
            httpWebRequest.GetRequestStream().Write(btBodys, 0, btBodys.Length);

            string responseContent = string.Empty;
            try
            {
                using (HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse())
                {
                    StreamReader streamReader = new StreamReader(response.GetResponseStream());
                    responseContent = streamReader.ReadToEnd();
                    streamReader.Close();
                    response.Close();
                }
            }
            catch (Exception e)
            {
                responseContent = e.ToString();
            }
            finally
            {
                httpWebRequest.Abort();
            }

            return responseContent;
        }

        /// <summary>
        /// Open webpages to check the cookie settings.
        /// </summary>
        private void PreOpenPages()
        {
            List<WebSite> _szponpages = _schd.GetList<WebSite>().Where(w => w.Name.EndsWith("ON.")).ToList();

            foreach (var item in _szponpages)
            {
                WebPagesLinks.Add("www." + item.URL.TrimEnd());
            }

            for (int i = 0; i < WebPagesLinks.Count; i++)
            {
                var _proc = Process.Start(WebPagesLinks[i]);
                Thread.Sleep(500);
            }

            WebPagesLinks = new List<string>();
        }

        private void CaptureMozillaPages()
        {
            FileInfo Logtext = new FileInfo(@"C:\Kantar\szponcaptureLog_" + DateTime.Now.ToString(new System.Globalization.CultureInfo("hu-HU")).Replace(".", "").Replace(":", "").Replace(' ', '_') + ".txt");
            FileInfo ErrorLog = new FileInfo(@"C:\Kantar\szponErrorLog_" + DateTime.Now.ToString(new System.Globalization.CultureInfo("hu-HU")).Replace(".", "").Replace(":", "").Replace(' ', '_') + ".txt");
            string[] _problemasweboldalak = { "2428", "2664", "2606", "2452", "2463", "2439" };
            ErrorLinks = new ObservableCollection<string>();

            // Ctrl+Shift+s
            // 1695, 150 - Teljes oldal gomb
            // 1660, 230 - Donwlod gomb
            // Lehet, hogy kell ENTER is, nem biztos

            for (int i = 0; i < WebPagesLinks.Count; i++)
            {
                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(WebPagesLinks[i]);
                HibasOldalak = WebPagesLinks[i];

                string _websitecode = WebPagesLinks[i].Substring(0, 4);
                int _delay = _problemasweboldalak.Contains(_websitecode) ? 2 : 1;
                
                var _proc = Process.Start(WebPagesLinks[i].Substring(5));
                Thread.Sleep(Secdelay * 1000 * _delay);
                SetCursorPos(1750, 30);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                Thread.Sleep(Secdelay * 100);

                SendKeys.SendWait("^+s");

                Thread.Sleep(Secdelay * 200);

                SetCursorPos(1750, 115);
                Thread.Sleep(Secdelay * 100);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                Thread.Sleep(Secdelay * 2000 * _delay);

                SendKeys.SendWait("{TAB}");
                SendKeys.SendWait("{TAB}"); 
                SendKeys.SendWait("{TAB}");
                SendKeys.SendWait("{ENTER}");

                //SetCursorPos(1660, 230);
                //mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                //mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);

                Thread.Sleep(Secdelay * 1000 * _delay);

                if (KellEnter)
                {
                    SendKeys.SendWait("{ENTER}");
                    Thread.Sleep(Secdelay * 300);
                }

                SendKeys.SendWait("^w");        /// ctrl+w
                Thread.Sleep(Secdelay * 500);

                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine("Elmentve Downloads-ba.");
                
                string downloadmappa = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();

                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(downloadmappa);
                
                FileInfo[] _finfos = new DirectoryInfo(downloadmappa).GetFiles("*.??g");
                //FileInfo _fi = _finfos.FirstOrDefault();
                FileInfo _fi = _finfos.OrderBy(f => f.LastAccessTime).LastOrDefault();

                if (_fi == null)
                {
                    using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(" Nem sikerült a mentés, nincs fájl." + Environment.NewLine);

                    //HibasOldalak += WebPagesLinks[i] + Environment.NewLine;
                    ErrorLinks.Add(i.ToString("d4") + "-" + WebPagesLinks[i]);

                    using (StreamWriter _sw = File.AppendText(ErrorLog.FullName)) _sw.WriteLine(i.ToString("d4") + "-" + WebPagesLinks[i]);

                    continue;
                }

                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(_fi.FullName);

                string _targetfilename = Path.GetFileNameWithoutExtension(_fi.Name.Substring(22))
                    + "-#" + i.ToString("d4")
                    + "-" + WebPagesLinks[i].Replace(":", "=").Replace("*", "'-'").Replace("/", "'%'").Replace("?", "'k'");

                string _targetpath = Path.Combine(_defaultPath, _targetfilename.Substring(0, Math.Min(_targetfilename.Length, 228)) + _fi.Extension);

                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine(_targetpath);

                File.Move(_fi.FullName, _targetpath);

                using (StreamWriter _sw = File.AppendText(Logtext.FullName)) _sw.WriteLine("Sikeres mozgatás." + Environment.NewLine);
            }

            using (StreamWriter _sw = File.AppendText(Logtext.FullName))
            {
                foreach (string item in ErrorLinks)
                {
                    _sw.WriteLine(item);
                }
            }
        }

        private void HibasLinkUjraKep()
        {
            int i = Convert.ToInt32(ErrorLink.Split('-')[0]);

            var _proc = Process.Start(ErrorLink.Substring(10));

            var result = System.Windows.MessageBox.Show("Klikkelj az 'OK'-ra, ha sikerült a képmentés!" + Environment.NewLine
                + "Yes: Kép elmentve" + Environment.NewLine
                + "No: Érdektelen, nincs szponzoráció" + Environment.NewLine
                + "Cancel: Hagyjuk, vissza az egész",
                "Feladat", MessageBoxButton.YesNoCancel);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    try
                    {
                        string downloadmappa = Microsoft.Win32.Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", String.Empty).ToString();

                        FileInfo[] _finfos = new DirectoryInfo(downloadmappa).GetFiles("*.??g");
                        //FileInfo _fi = _finfos.FirstOrDefault();
                        FileInfo _fi = _finfos.OrderBy(f => f.LastAccessTime).LastOrDefault();

                        string _targetfilename = Path.GetFileNameWithoutExtension(_fi.Name.Substring(22))
                            + "-#" + i.ToString("d4")
                            + "-" + WebPagesLinks[i].Replace(":", "=").Replace("*", "'-'").Replace("/", "'%'").Replace("?", "'k'");

                        string _targetpath = Path.Combine(_defaultPath, _targetfilename.Substring(0, Math.Min(_targetfilename.Length, 228)) + _fi.Extension);

                        File.Move(_fi.FullName, _targetpath);
                    }
                    catch (Exception exc)
                    {
                        System.Windows.MessageBox.Show("Hiba türtént a mentett kép feldolgozása során!" + Environment.NewLine + exc.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    break;
                case MessageBoxResult.No:
                    break;
                case MessageBoxResult.Cancel:
                    ErrorLink = "";
                    return;
            }

            ErrorLinks.RemoveAt(0);
            ErrorLink = "";

            // Ha üres a lista, tötöni kéne az ErrorLog fájlt.
            // Az ErrorLog fájl gombnyomásra beötlthetõ, ha van.
        }

        private void AddErroritemToTB()
        {
            ErrorLink = ErrorLinks[0];
        }

    }
}