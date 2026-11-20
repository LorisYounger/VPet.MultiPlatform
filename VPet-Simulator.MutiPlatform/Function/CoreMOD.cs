using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Unified.Services;
using VPet_Simulator.Windows.Interface;

namespace VPet_Simulator.MutiPlatform
{
    internal class CoreMOD : IModInfo
    {
        // 宿主自带、MOD 指定跳过或已处理的 DLL 文件名。
        public static HashSet<string> LoadedDLL { get; } = new HashSet<string>()
        {
            "steam_api.dll","steam_api64.dll","libsteam_api.so","libsteam_api.dylib",
            "LinePutScript.dll","Facepunch.Steamworks.dll","Facepunch.Steamworks.Win32.dll", "Facepunch.Steamworks.Win64.dll",
            "VPet_Simulator.Core.Base.dll","VPet_Simulator.Core.MutiPlatform.dll","LinePutScript.Localization.dll",
            "VPet-Simulator.Unified.dll",
            "libSkiaSharp.dll","SkiaSharp.dll","HarfBuzzSharp.dll","libHarfBuzzSharp.dll",
        };
        /// <summary>
        /// 每个窗口用同一入口类型创建自己的插件实例。
        /// </summary>
        private static readonly Dictionary<string, Type> LoadUnifiedPlug = new(StringComparer.OrdinalIgnoreCase);
        public static string? NowLoading = null;
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        /// <summary>
        /// 如果是上传至Steam,则为SteamUserID
        /// </summary>
        public long AuthorID { get; set; } = 0;
        /// <summary>
        /// 上传至Steam的ItemID
        /// </summary>
        public ulong ItemID { get; set; } = 0;
        public string Intro { get; set; } = string.Empty;
        public DirectoryInfo Path { get; set; } = null!;
        public int GameVer { get; set; }
        public int Ver { get; set; }
        public HashSet<string> Tag { get; set; } = new HashSet<string>();
        public bool SuccessLoad = true;
        public DateTime CacheDate;
        public string ErrorMessage = string.Empty;
        public static string INTtoVER(int ver) => ver < 10000 ? $"{ver / 100}.{ver % 100:00}" : $"{ver / 10000}.{ver % 10000 / 100}.{ver % 100:00}";
        public static void LoadImage(MainWindow mw, DirectoryInfo di, string pre = "")
        {
            var index = new ResourceIndex();
            var settings = new List<string>();
            index.AddImages(di, pre, settings);
            foreach (var source in index.Sources)
            {
                mw.ImageSources.AddSource(source.Key, source.Value);
            }
            // 图片锚点等显示设置由窗口保存。
            foreach (var path in settings)
            {
                foreach (var line in new LpsDocument(File.ReadAllText(path)))
                    mw.ImageSources.ImageSetting.AddorReplaceLine(line);
            }
        }
        public static void LoadFile(MainWindow mw, DirectoryInfo di, string pre = "")
        {
            var index = new ResourceIndex();
            index.AddFiles(di, pre);
            foreach (var source in index.Sources)
            {
                mw.FileSources.AddSource(source.Key, source.Value);
            }
        }
        public long sItemID { get; }
        public long sAuthorID { get; }
        public CoreMOD(DirectoryInfo directory, MainWindow mw, long steamItemId = 0, long steamAuthorId = 0)
        {
#if !DEBUG
            try
            {
#endif
            Path = directory;
            sItemID = steamItemId;
            sAuthorID = steamAuthorId;

            var meta = ModInfoReader.Parse(directory)
                ?? throw new FileNotFoundException("找不到 info.lps", System.IO.Path.Combine(directory.FullName, "info.lps"));
            if (meta.Warnings.Count != 0)
                throw new Exception(string.Join("\n", meta.Warnings));
            if (string.IsNullOrEmpty(meta.Name))
                throw new Exception("info.lps 里没有 vupmod 行");

            Name = meta.Name;
            NowLoading = Name;
            Intro = meta.Intro;
            GameVer = meta.GameVer;
            Ver = meta.Ver;
            Author = meta.Author;
            AuthorID = meta.AuthorID;
            ItemID = meta.ItemID;
            CacheDate = meta.CacheDate;
            LoadedDLL.UnionWith(meta.DllSkip);

            //MOD未加载时支持翻译
            foreach (var (culture, line) in meta.PreTranslations)
            {
                List<ILine> ls = new List<ILine>();
                foreach (var sub in line)
                {
                    ls.Add(new Line(sub.Name, sub.info));
                }
                LocalizeCore.AddCulture(culture, ls);
            }

            if (mw.CoreMODs.Any(x => x.Name == Name))
            {
                Name += $"({"MOD名称重复".Translate()})";
                ErrorMessage = "MOD名称重复".Translate();
                return;
            }

            if (!IsOnMOD(mw))
            {
                Tag.Add("该模组已停用");
                Tag.UnionWith(meta.ContentTags);
                return;
            }

            foreach (DirectoryInfo di in Path.EnumerateDirectories())
            {
                switch (di.Name.ToLowerInvariant())
                {
                    case "theme":
                        Tag.Add("theme");
                        if (Directory.Exists(System.IO.Path.Combine(di.FullName, "fonts")))
                            foreach (var str in Directory.EnumerateFiles(System.IO.Path.Combine(di.FullName, "fonts"), "*.ttf"))
                            {
                                mw.Fonts.Add(new IFont(new FileInfo(str)));
                            }

                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new Theme(new LpsDocument(File.ReadAllText(fi.FullName)));
                            var oldtheme = mw.Themes.Find(x => x.xName == tmp.xName);
                            if (oldtheme != null)
                                mw.Themes.Remove(oldtheme);
                            mw.Themes.Add(tmp);
                            //加载图片包
                            DirectoryInfo tmpdi = new DirectoryInfo(System.IO.Path.Combine(di.FullName, tmp.Image));
                            if (tmpdi.Exists)
                            {
                                foreach (FileInfo tmpfi in tmpdi.EnumerateFiles("*.png"))
                                {
                                    tmp.Images.AddSource(tmpfi.Name.ToLowerInvariant().Substring(0, tmpfi.Name.Length - 4), tmpfi.FullName);
                                }
                                foreach (DirectoryInfo fordi in tmpdi.EnumerateDirectories())
                                {
                                    foreach (FileInfo tmpfi in fordi.EnumerateFiles("*.png"))
                                    {
                                        tmp.Images.AddSource(fordi.Name + '_' + tmpfi.Name.ToLowerInvariant().Substring(0, tmpfi.Name.Length - 4), tmpfi.FullName);
                                    }
                                }
                            }
                        }
                        break;
                    case "pet":
                        //宠物模型                           
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            LpsDocument lps = new LpsDocument(File.ReadAllText(fi.FullName));
                            if (lps.First()!.Name.ToLowerInvariant() == "pet")
                            {
                                var name = lps.First()!.Info;
                                if (name == "默认虚拟桌宠")
                                    name = "vup";//旧版本名称兼容

                                var p = mw.Pets.FirstOrDefault(x => x.Name == name);
                                if (p == null)
                                {
                                    Tag.Add("pet");
                                    p = new PetLoader(lps, di);
                                    if (p.Config.Works.Count > 0)
                                        Tag.Add("work");
                                    mw.Pets.Add(p);
                                }
                                else
                                {
                                    if (lps.FindLine("work") != null)
                                    {
                                        Tag.Add("work");
                                    }
                                    var dis = new DirectoryInfo(System.IO.Path.Combine(di.FullName, lps.First()!["path"].Info));
                                    if (dis.Exists && dis.EnumerateDirectories().Any())
                                        Tag.Add("pet");
                                    p.path.Add(dis.FullName);
                                    p.Config.Set(lps);
                                }
                            }
                        }
                        break;
                    case "food":
                        Tag.Add("food");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                            foreach (ILine li in tmp)
                            {
                                if (li.Name != "food")
                                    continue;
                                string tmps = li.Find("name")!.info;
                                mw.Foods.RemoveAll(x => x.Name == tmps);
                                mw.Foods.Add(LPSConvert.DeserializeObject<Food>(li)!);
                            }
                        }
                        break;
                    case "image":
                        Tag.Add("image");
                        LoadImage(mw, di);
                        break;
                    case "file":
                        Tag.Add("file");
                        LoadFile(mw, di);
                        break;
                    case "photo":
                        Tag.Add("photo");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LPS(File.ReadAllText(fi.FullName));
                            foreach (Line li in tmp)
                            {
                                if (li.Name != "photo")
                                    continue;
                                mw.Photos.Add(new Photo(li));
                            }
                        }
                        break;
                    case "text":
                        Tag.Add("text");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            var tmp = new LpsDocument(File.ReadAllText(fi.FullName));
                            foreach (ILine li in tmp)
                            {
                                switch (li.Name.ToLowerInvariant())
                                {
                                    case "lowfoodtext":
                                        mw.LowFoodText.Add(LPSConvert.DeserializeObject<LowText>(li)!);
                                        Tag.Add("lowtext");
                                        break;
                                    case "lowdrinktext":
                                        mw.LowDrinkText.Add(LPSConvert.DeserializeObject<LowText>(li)!);
                                        Tag.Add("lowtext");
                                        break;
                                    case "clicktext":
                                        mw.ClickTexts.Add(LPSConvert.DeserializeObject<ClickText>(li)!);
                                        Tag.Add("clicktext");
                                        break;
                                    case "selecttext":
                                        mw.SelectTexts.Add(LPSConvert.DeserializeObject<SelectText>(li)!);
                                        Tag.Add("selecttext");
                                        break;
                                    case "buff":
                                        mw.BuffEffects.Add(LPSConvert.DeserializeObject<Buff.Effect>(li)!);
                                        break;
                                    case "schedulepackage":
                                        mw.SchedulePackage.Add(LPSConvert.DeserializeObject<ScheduleTask.PackageFull>(li)!);
                                        break;
                                }
                            }
                        }
                        break;
                    case "lang":
                        Tag.Add("lang");
                        foreach (FileInfo fi in di.EnumerateFiles("*.lps"))
                        {
                            LocalizeCore.AddCulture(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length), new LPS_D(File.ReadAllText(fi.FullName)));
                        }
                        foreach (DirectoryInfo dis in di.EnumerateDirectories())
                        {
                            foreach (FileInfo fi in dis.EnumerateFiles("*.lps"))
                            {
                                LocalizeCore.AddCulture(dis.Name, new LPS_D(File.ReadAllText(fi.FullName)));
                            }
                        }

                        if (mw.Set.Language == "null")
                        {
                            LocalizeCore.LoadDefaultCulture();
                        }
                        else
                            LocalizeCore.LoadCulture(mw.Set.Language);
                        break;
                    case "plugin":
                        Tag.Add("plugin");
                        SuccessLoad = true;
                        var loadPath = System.IO.Path.Combine(di.FullName, "load.lps");
                        var loadfile = File.Exists(loadPath) ? new LpsDocument(File.ReadAllText(loadPath)) : new LpsDocument();
                        string authtype = "";
                        foreach (FileInfo tmpfi in di.EnumerateFiles().Where(file => file.Extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)))
                        {
                            // 设置页验签也按同一规则筛选 DLL。
                            if (PluginClassifier.ShouldSkip(tmpfi.Name, loadfile))
                                continue;

                            try
                            {
                                if (LoadUnifiedPlug.TryGetValue(tmpfi.Name, out var unifiedType))
                                {
                                    var loadedPath = unifiedType.Assembly.Location;
                                    if (!string.Equals(loadedPath, tmpfi.FullName, OperatingSystem.IsWindows()
                                        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                                    {
                                        ErrorMessage = $"插件文件名冲突：{tmpfi.FullName} 与已加载的 {loadedPath} 同名。";
                                        SuccessLoad = false;
                                        continue;
                                    }
                                    UnifiedPluginHost.Create(mw, this, unifiedType);
                                    continue;
                                }
                                if (LoadedDLL.Contains(tmpfi.Name))
                                    continue;
                                using var certificate = AuthenticodeHelper.GetVerifiedCertificate(tmpfi.FullName);
                                if (certificate != null)
                                {
                                    //LBGame 信任的证书
                                    if (authtype != "FAIL" && authtype != "[V]")
                                        if (IsLBGameCertificate(certificate))
                                            authtype = "[认证]".Translate();
                                        else
                                            authtype = "[V]";
                                }
                                else
                                {
                                    authtype = "FAIL";
                                    if (!IsPassMOD(mw))
                                    {
                                        SuccessLoad = false;
                                        continue;
                                    }
                                }
                                LoadedDLL.Add(tmpfi.Name);

                                if (PluginClassifier.Classify(tmpfi.FullName) == PluginKind.LegacyWindows)
                                {
                                    ErrorMessage = $"{tmpfi.Name} 仅支持 Windows 版桌宠。";
                                    SuccessLoad = false;
                                    continue;
                                }
                                Assembly dll = Assembly.LoadFrom(tmpfi.FullName);
                                foreach (Type exportedType in dll.GetExportedTypes())
                                {
                                    if (exportedType.BaseType == typeof(VPet_Simulator.Unified.Interface.UnifiedPlugin))
                                    {
                                        if (exportedType.FullName == null) continue;
                                        var n = exportedType.FullName.ToLowerInvariant();
                                        if (!(n.Contains("modmaker") || n.Contains("dlc")))
                                            App.MODType.Add(exportedType.FullName);
                                        LoadUnifiedPlug.Add(tmpfi.Name, exportedType);
                                        UnifiedPluginHost.Create(mw, this, exportedType);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                if (loadfile[tmpfi.Name][(gbol)"ignoreError"])
                                    continue;
                                Console.WriteLine($"{Name}:Load DLL {tmpfi.FullName} failed: {e.Message}");
                                ErrorMessage = e.ToString();
                                SuccessLoad = false;
                            }
                        }
                        if (authtype != "FAIL")
                            Author += authtype;
                        break;
                }
            }
#if !DEBUG
            }
            catch (Exception e)
            {
                Console.WriteLine($"{Name}: {e.Message}");
                ErrorMessage = e.Message;
                Tag.Add("该模组已损坏");
                SuccessLoad = false;
            }
#endif
        }
        public bool IsOnMOD(MainWindow mw) => mw.Set.IsOnMod(Name);
#if DEBUG
        public bool IsPassMOD(MainWindow mw) => true;
#else
        public bool IsPassMOD(MainWindow mw) => mw.Set.IsPassMOD(Name);
#endif

        public void WriteFile()
        {
            LpsDocument modlps = new LpsDocument(File.ReadAllText(System.IO.Path.Combine(Path.FullName, "info.lps")));
            modlps.FindLine("vupmod")!.Info = Name;
            modlps.FindLine("intro")!.Info = Intro;
            modlps.FindSub("gamever")!.InfoToInt = GameVer;
            modlps.FindSub("ver")!.InfoToInt = Ver;
            modlps.FindSub("author")!.Info = Author;
            modlps.FindorAddLine("authorid").InfoToInt64 = AuthorID;
            modlps.FindorAddLine("itemid").info = ItemID.ToString();
            File.WriteAllText(System.IO.Path.Combine(Path.FullName, "info.lps"), modlps.ToString());
        }

        public static bool IsLBGameCertificate(X509Certificate2 certificate)
        {
            return (AuthenticodeHelper.HasDistinguishedName(certificate.SubjectName, "CN=\"Shenzhen Lingban Computer Technology Co., Ltd.\", O=\"Shenzhen Lingban Computer Technology Co., Ltd.\", L=Shenzhen, S=Guangdong Province, C=CN, SERIALNUMBER=91440300MA5H8REU3K, OID.2.5.4.15=Private Organization, OID.1.3.6.1.4.1.311.60.2.1.1=Shenzhen, OID.1.3.6.1.4.1.311.60.2.1.2=Guangdong Province, OID.1.3.6.1.4.1.311.60.2.1.3=CN")
                                        && AuthenticodeHelper.HasDistinguishedName(certificate.IssuerName, "CN=DigiCert Trusted G4 Code Signing RSA4096 SHA384 2021 CA1, O=\"DigiCert, Inc.\", C=US"))
                                        || (AuthenticodeHelper.HasDistinguishedName(certificate.SubjectName, "CN=\"Shenzhen Zero Edition Computer Technology Co., Ltd.\", O=\"Shenzhen Zero Edition Computer Technology Co., Ltd.\", L=Shenzhen, S=Guangdong, C=CN, SERIALNUMBER=91440300MA5H8REU3K, OID.1.3.6.1.4.1.311.60.2.1.1=Shenzhen, OID.1.3.6.1.4.1.311.60.2.1.2=Guangdong, OID.1.3.6.1.4.1.311.60.2.1.3=CN, OID.2.5.4.15=Private Organization")
                                        && AuthenticodeHelper.HasDistinguishedName(certificate.IssuerName, "CN=Certum Extended Validation Code Signing 2021 CA, O=Asseco Data Systems S.A., C=PL"));
        }
    }
    /// <summary>
    /// MOD 开关
    /// </summary>
    public static class ExtensionSetting
    {

        internal static bool IsOnMod(this Setting t, string ModName) => ModSwitchStore.IsOn(t, ModName);
        internal static bool IsPassMOD(this Setting t, string ModName) => ModSwitchStore.IsPassed(t, ModName);
        internal static bool IsMSGMOD(this Setting t, string ModName) => ModSwitchStore.ShouldNotice(t, ModName);
        internal static void OnMod(this Setting t, string ModName) => ModSwitchStore.On(t, ModName);
        internal static void OnModRemove(this Setting t, string ModName) => ModSwitchStore.Off(t, ModName);
        internal static void PassMod(this Setting t, string ModName) => ModSwitchStore.Pass(t, ModName);
        internal static void PassModRemove(this Setting t, string ModName) => ModSwitchStore.PassRemove(t, ModName);
    }
}
