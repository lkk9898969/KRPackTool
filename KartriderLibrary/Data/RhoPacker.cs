
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.File;
using KartLibrary.Xml;
using KartRider.IO.Packet;
using KRPackTool;
//using RHOParser;

namespace KartRider;

public static class RhoPacker
{
    private static readonly string[] datapack =
    {
        "boss", "character", "dialog", "dialog2", "effect", "etc_", "flyingPet", "gui", "item", "kart_", "myRoom",
        "pet", "sound", "stage", "stuff", "stuff2", "theme", "track", "trackThumb", "track_", "zeta", "zeta_"
    };

    public static void PackTool(string[] args)
    {
        foreach (var arg in args)
        {
            Program.CC = Program.parseCountryCode(Path.GetDirectoryName(arg));
            if (arg.EndsWith(".rho") || arg.EndsWith(".rho5"))
            {
                decode(arg, arg);
            }
            else if (arg.EndsWith("aaa.xml"))
            {
                AAAD(arg);
            }
            else if (arg.EndsWith(".xml"))
            {
                XtoB(arg);
            }
            else if (arg.EndsWith(".bml"))
            {
                BtoX(arg);
            }
            else if (arg.EndsWith(".pk"))
            {
                AAAR(arg);
            }
            else
            {
                if (!Directory.Exists(arg))
                    return;
                var temp = Directory.GetDirectories(arg);
                if (temp.All(dir => datapack.Contains(Path.GetFileName(dir))) && temp.Length != 0)
                {
                    encode(arg, arg);
                }
                else
                {
                    var files = Directory.GetFiles(arg, "*.rho");
                    if (files.Length > 0)
                    {
                        AAAC(arg, files);
                    }
                    else
                    {
                        encodea(arg, arg);
                        var parent = Path.GetDirectoryName(arg);
                        files = Directory.GetFiles(parent, "*.rho");
                        AAAC(parent, files);
                    }
                }
            }
        }
    }

    private static void encodea(string input, string output)
    {
        if (!output.EndsWith(".rho"))
            output += ".rho";

        string baseDir = Path.GetDirectoryName(input) ?? "";
        string dirName = Path.GetFileName(input);
        string targetPath = Path.Combine(baseDir, dirName/*.Replace('_', Path.DirectorySeparatorChar)*/);

        RhoArchive rhoArchive = new RhoArchive();
        GetAllFiles(targetPath, rhoArchive.RootFolder);

        rhoArchive.SaveTo(output);
    }

    private static void GetAllFiles(string folderPath, RhoFolder folder)
    {
        DirectoryInfo dirInfo = new DirectoryInfo(folderPath);

        List<FileInfo> files = new List<FileInfo>(dirInfo.GetFiles());
        files.Sort(new CustomStringComparerFile());
        foreach (FileInfo file in files)
        {
            string extension = Path.GetExtension(file.Name);
            int fileSize = (int)file.Length;
            RhoFile item = new RhoFile();
            item.DataSource = new FileDataSource(file.FullName);
            item.Name = file.Name;
            item.FileEncryptionProperty = GetRhoFilePropertyByExtension(extension, fileSize);
            folder.AddFile(item);
        }

        List<DirectoryInfo> subdirs = new List<DirectoryInfo>(dirInfo.GetDirectories());
        subdirs.Sort(new CustomStringComparerDir());
        foreach (DirectoryInfo subdir in subdirs)
        {
            RhoFolder subFolder = new RhoFolder
            {
                Name = subdir.Name
            };
            folder.AddFolder(subFolder);
            // 递归处理子目录（包括空目录）
            GetAllFiles(subdir.FullName, subFolder);
        }
    }

    private static void encode(string input, string output)
    {
        string aaaPath = Path.Combine(Directory.GetParent(input).FullName, "aaa.pk");
        CountryCode CC = Program.CC;
        var rho5Archive = new Rho5Archive();
        if (!output.EndsWith(".rho5"))
            output += ".rho5";
        var fileInfo = new FileInfo(output);
        if (fileInfo.Directory != null)
        {
            var fullName = fileInfo.Directory.FullName;
            if (!Directory.Exists(fullName))
                Directory.CreateDirectory(fullName);
            var strArray = fileInfo.Name.Replace(".rho5", "").Split("_");
            var dataPackName = strArray[0];
            var dataPackID = 0;
            if (strArray.Length == 2)
                dataPackID = Convert.ToInt32(strArray[1]);
            input = input.Replace("\\", "/");
            if (!input.EndsWith("/"))
                input += "/";
            rho5Archive.SaveFolder(input, dataPackName, fullName, CC, dataPackID);
        }
        else
        {
            Console.WriteLine($"路径不存在：{output}");
        }
    }

    private static void decode(string input, string output)
    {
        if (output.EndsWith(".rho"))
            output = output.Replace(".rho", "");
        if (output.EndsWith(".rho5"))
            output = output.Replace(".rho5", "");
        CountryCode CC = Program.CC;
        string aaaPath = Path.Combine(Directory.GetParent(input).FullName, "aaa.pk");

        PackFolderManager packFolderManager = new PackFolderManager();
        packFolderManager.OpenSingleFile(input, CC);
        Queue<PackFolderInfo> packFolderInfoQueue = new Queue<PackFolderInfo>();
        packFolderInfoQueue.Enqueue(packFolderManager.GetRootFolder());
        packFolderManager.GetRootFolder();
        while (packFolderInfoQueue.Count > 0)
        {
            PackFolderInfo packFolderInfos = packFolderInfoQueue.Dequeue();
            foreach (PackFolderInfo packFolderInfo in packFolderInfos.GetFoldersInfo())
            {
                string fileName = Path.GetFileNameWithoutExtension(packFolderInfo.FolderName);
                RhoFolders(output, output + "/" + fileName, packFolderInfo);
            }
        }
        // 确保输出目录存在
        if (!Directory.Exists(output))
            Directory.CreateDirectory(output);
    }

    private static void RhoFolders(string input, string output, PackFolderInfo rhoFolders)
    {
        // 创建当前目录（包括空目录）
        if (!Directory.Exists(output))
            Directory.CreateDirectory(output);

        if (rhoFolders.GetFilesInfo() != null)
        {
            foreach (var item in rhoFolders.GetFilesInfo())
            {
                string fullName = output + "/" + ReplacePath(item.FullName);
                string Name = Path.GetDirectoryName(fullName);
                if (!Directory.Exists(Name))
                    Directory.CreateDirectory(Name);
                byte[] data = item.GetData();
                using (FileStream fileStream = new FileStream(fullName, FileMode.OpenOrCreate))
                {
                    fileStream.Write(data, 0, data.Length);
                }
            }
        }
        if (rhoFolders.Folders != null)
        {
            foreach (var rhoFolder in rhoFolders.Folders)
            {
                string Folder = output + "/" + rhoFolder.FolderName;
                if (!Directory.Exists(Folder))
                    Directory.CreateDirectory(Folder);
                RhoFolders(input, Folder, rhoFolder);
            }
        }
    }

    private static string ReplacePath(string file)
    {
        return file.IndexOf(".rho") > -1 ? file.Substring(0, file.IndexOf(".rho")).Replace("_", "/") + file.Substring(file.IndexOf(".rho") + 4) : file;
    }

    private static void BtoX(string input)
    {
        if (!File.Exists(input))
            return;
        var data = File.ReadAllBytes(input);
        var bxd = new BinaryXmlDocument();
        bxd.Read(Encoding.GetEncoding("UTF-16"), data);
        var output_bml = bxd.RootTag.ToString();
        var output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
        var filePath = Path.ChangeExtension(input, "xml");
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            fs.Write(output_data, 0, output_data.Length);
        }
    }

    private static void XtoB(string input)
    {
        var xdoc = XDocument.Load(input);
        if (xdoc.Root == null)
            return;
        var childCounts = CountChildren(xdoc.Root, 0, new List<int>());
        using (var reader = XmlReader.Create(input))
        {
            using (var outPacket = new OutPacket())
            {
                var Count = 0;
                while (reader.Read())
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var elementName = reader.Name;
                        var attCount = reader.AttributeCount;
                        outPacket.WriteString(elementName);
                        outPacket.WriteInt();
                        outPacket.WriteInt(attCount);
                        for (var i = 0; i < attCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            var attName = reader.Name;
                            outPacket.WriteString(attName);
                            var attValue = reader.Value;
                            outPacket.WriteString(attValue);
                        }

                        outPacket.WriteInt(childCounts[Count]);
                        Count++;
                        reader.MoveToElement();
                    }

                var byteArray = outPacket.ToArray();
                var filePath = Path.ChangeExtension(input, "bml");
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                }
            }
        }
    }

    public static List<int> CountChildren(XElement element, int level, List<int> childCounts)
    {
        var childCount = element.Elements().Count();
        childCounts.Add(childCount);
        foreach (var child in element.Elements()) CountChildren(child, level + 1, childCounts);
        return childCounts;
    }

    private static void AAAR(string input)
    {
        if (!File.Exists(input))
            return;
        using var fileStream = new FileStream(input, FileMode.Open, FileAccess.Read);
        var binaryReader = new BinaryReader(fileStream);
        var totalLength = binaryReader.ReadInt32();
        var array = binaryReader.ReadKRData(totalLength);
        fileStream.Close();
        var bxd = new BinaryXmlDocument();
        bxd.Read(Encoding.GetEncoding("UTF-16"), array);
        var output_bml = bxd.RootTag.ToString();
        var output_data = Encoding.GetEncoding("UTF-16").GetBytes(output_bml);
        var filePath = Path.ChangeExtension(input, "xml");
        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            fs.Write(output_data, 0, output_data.Length);
        }
    }

    private static void AAAD(string input)
    {
        var xdoc = XDocument.Load(input);
        if (xdoc.Root == null)
            return;
        var childCounts = CountChildren(xdoc.Root, 0, new List<int>());
        byte[] byteArray;
        using (var reader = XmlReader.Create(input))
        {
            using (var outPacket = new OutPacket())
            {
                var Count = 0;
                while (reader.Read())
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var elementName = reader.Name;
                        var attCount = reader.AttributeCount;
                        outPacket.WriteString(elementName);
                        outPacket.WriteInt();
                        outPacket.WriteInt(attCount);
                        for (var i = 0; i < attCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            var attName = reader.Name;
                            outPacket.WriteString(attName);
                            var attValue = reader.Value;
                            outPacket.WriteString(attValue);
                        }

                        outPacket.WriteInt(childCounts[Count]);
                        Count++;
                        reader.MoveToElement();
                    }

                byteArray = outPacket.ToArray();
            }
        }

        var filePath = Path.ChangeExtension(input, "pk");
        using var fileStream = new FileStream(filePath, FileMode.Create);
        {
            var binaryWriter = new BinaryWriter(fileStream);
            binaryWriter.Write(0);
            var KRDataLength = binaryWriter.WriteKRData(byteArray, false, true);
            binaryWriter.BaseStream.Seek(0, SeekOrigin.Begin);
            binaryWriter.Write(KRDataLength);
        }
    }

    private static void AAAC(string input, string[] files)
    {
        string[] whitelist =
        {
                "_I04_sn", "_I05_sn", "_R01_sn", "_R02_sn", "_I02_sn", "_I01_sn", "_I03_sn", "_L01_", "_L02_", "_L03_03_",
                "_L03_", "_L04_", "bazzi_", "arthur_", "bero_", "brodi_", "camilla_", "chris_", "contender_", "crowdr_",
                "CSO_", "dao_", "dizni_", "erini_", "ethi_", "Guazi_", "halloween_", "homrunDao_", "innerWearSonogong_",
                "innerWearWonwon_", "Jianbing_", "kephi_", "kero_", "kwanwoo_", "Lingling_", "lodumani_", "mabi_", "Mahua_",
                "marid_", "mobi_", "mos_", "narin_", "neoul_", "neo_", "nymph_", "olympos_", "panda_", "referee_", "ren_",
                "Reto_", "run_", "zombie_", "santa_", "sophi_", "taki_", "tiera_", "tutu_", "twoTop_", "twotop_", "uni_",
                "wonwon_", "zhindaru_", "zombie_", "flyingBook_", "flyingMechanic_", "flyingRedlight_", "crow_",
                "dragonBoat_", "GiLin_", "maple_", "beach_", "village_", "china_", "factory_", "ice_", "mine_", "nemo_",
                "world_", "forest_", "_I", "_R", "_S", "_F", "_P", "_K", "_D", "_jp", "_A0"
            };
        string[] blacklist = { "character_" };

        var root = new XElement("PackFolder", new XAttribute("name", "KartRider"));
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            var result = fileName;
            foreach (var white in whitelist) result = result.Replace(white, white.Replace("_", "!"));
            foreach (var black in blacklist) result = result.Replace(black.Replace("_", "!"), black);
            var splitParts = result.Split('_');
            var currentFolder = root;
            for (var i = 0; i < splitParts.Length - 1; i++)
            {
                var folderName = splitParts[i];
                var subFolder = currentFolder.Elements("PackFolder")
                    .FirstOrDefault(f => (string)f.Attribute("name") == folderName);
                if (subFolder == null)
                {
                    if (folderName == "character" || folderName == "flyingPet" || folderName == "pet" ||
                        folderName == "track")
                        subFolder = new XElement("PackFolder", new XAttribute("name", folderName),
                            new XAttribute("loadPass", "1"));
                    else
                        subFolder = new XElement("PackFolder", new XAttribute("name", folderName));
                    currentFolder.Add(subFolder);
                }

                currentFolder = subFolder;
            }

            var rho = new Rho(file);
            var rhoKey = rho.GetFileKey();
            var dataHash = rho.GetDataHash();
            var size = rho.baseStream.Length;
            var rhoFolderName = splitParts.Length > 0
                ? Path.ChangeExtension(splitParts[splitParts.Length - 1], null)
                : "";
            var rhoFolder = new XElement("RhoFolder",
                new XAttribute("name", rhoFolderName.Replace('!', '_')),
                new XAttribute("fileName", fileName),
                new XAttribute("key", rhoKey.ToString()),
                new XAttribute("dataHash", dataHash.ToString()),
                new XAttribute("mediaSize", size.ToString()));
            currentFolder.Add(rhoFolder);
            rho.Dispose();
        }

        root.Save(input + "\\aaa.xml");
    }

    #region CRC32_and_Comparers
    private static uint[] _crc32Table = new uint[]
    {
            0u, 1996959894u, 3993919788u, 2567524794u, 124634137u, 1886057615u, 3915621685u, 2657392035u,
            249268274u, 2044508324u, 3772115230u, 2547177864u, 162941995u, 2125561021u, 3887607047u, 2428444049u,
            498536548u, 1789927666u, 4089016648u, 2227061214u, 450548861u, 1843258603u, 4107580753u, 2211677639u,
            325883990u, 1684777152u, 4251122042u, 2321926636u, 335633487u, 1661365465u, 4195302755u, 2366115317u,
            997073096u, 1281953886u, 3579855332u, 2724688242u, 1006888145u, 1258607687u, 3524101629u, 2768942443u,
            901097722u, 1119000684u, 3686517206u, 2898065728u, 853044451u, 1172266101u, 3705015759u, 2882616665u,
            651767980u, 1373503546u, 3369554304u, 3218104598u, 565507253u, 1454621731u, 3485111705u, 3099436303u,
            671266974u, 1594198024u, 3322730930u, 2970347812u, 795835527u, 1483230225u, 3244367275u, 3060149565u,
            1994146192u, 31158534u, 2563907772u, 4023717930u, 1907459465u, 112637215u, 2680153253u, 3904427059u,
            2013776290u, 251722036u, 2517215374u, 3775830040u, 2137656763u, 141376813u, 2439277719u, 3865271297u,
            1802195444u, 476864866u, 2238001368u, 4066508878u, 1812370925u, 453092731u, 2181625025u, 4111451223u,
            1706088902u, 314042704u, 2344532202u, 4240017532u, 1658658271u, 366619977u, 2362670323u, 4224994405u,
            1303535960u, 984961486u, 2747007092u, 3569037538u, 1256170817u, 1037604311u, 2765210733u, 3554079995u,
            1131014506u, 879679996u, 2909243462u, 3663771856u, 1141124467u, 855842277u, 2852801631u, 3708648649u,
            1342533948u, 654459306u, 3188396048u, 3373015174u, 1466479909u, 544179635u, 3110523913u, 3462522015u,
            1591671054u, 702138776u, 2966460450u, 3352799412u, 1504918807u, 783551873u, 3082640443u, 3233442989u,
            3988292384u, 2596254646u, 62317068u, 1957810842u, 3939845945u, 2647816111u, 81470997u, 1943803523u,
            3814918930u, 2489596804u, 225274430u, 2053790376u, 3826175755u, 2466906013u, 167816743u, 2097651377u,
            4027552580u, 2265490386u, 503444072u, 1762050814u, 4150417245u, 2154129355u, 426522225u, 1852507879u,
            4275313526u, 2312317920u, 282753626u, 1742555852u, 4189708143u, 2394877945u, 397917763u, 1622183637u,
            3604390888u, 2714866558u, 953729732u, 1340076626u, 3518719985u, 2797360999u, 1068828381u, 1219638859u,
            3624741850u, 2936675148u, 906185462u, 1090812512u, 3747672003u, 2825379669u, 829329135u, 1181335161u,
            3412177804u, 3160834842u, 628085408u, 1382605366u, 3423369109u, 3138078467u, 570562233u, 1426400815u,
            3317316542u, 2998733608u, 733239954u, 1555261956u, 3268935591u, 3050360625u, 752459403u, 1541320221u,
            2607071920u, 3965973030u, 1969922972u, 40735498u, 2617837225u, 3943577151u, 1913087877u, 83908371u,
            2512341634u, 3803740692u, 2075208622u, 213261112u, 2463272603u, 3855990285u, 2094854071u, 198958881u,
            2262029012u, 4057260610u, 1759359992u, 534414190u, 2176718541u, 4139329115u, 1873836001u, 414664567u,
            2282248934u, 4279200368u, 1711684554u, 285281116u, 2405801727u, 4167216745u, 1634467795u, 376229701u,
            2685067896u, 3608007406u, 1308918612u, 956543938u, 2808555105u, 3495958263u, 1231636301u, 1047427035u,
            2932959818u, 3654703836u, 1088359270u, 936918000u, 2847714899u, 3736837829u, 1202900863u, 817233897u,
            3183342108u, 3401237130u, 1404277552u, 615818150u, 3134207493u, 3453421203u, 1423857449u, 601450431u,
            3009837614u, 3294710456u, 1567103746u, 711928724u, 3020668471u, 3272380065u, 1510334235u, 755167117u
    };

    public static uint ComputeCRC32(string text)
    {
        return ComputeCRC32(Encoding.Unicode.GetBytes(text));
    }

    public static uint ComputeCRC32(byte[] input)
    {
        uint crc = uint.MaxValue;
        for (int i = 0; i < input.Length; i++)
        {
            crc = ((crc >> 8) ^ _crc32Table[(crc ^ input[i]) & 0xFF]);
        }
        return crc ^ 0xFFFFFFFFu;
    }

    private static int Wcscmp(string s1, string s2)
    {
        if (s1 == s2)
            return 0;
        byte[] bytes1 = Encoding.Unicode.GetBytes(s1);
        byte[] bytes2 = Encoding.Unicode.GetBytes(s2);
        Array.Resize(ref bytes1, bytes1.Length + 2);
        Array.Resize(ref bytes2, bytes2.Length + 2);
        int maxLen = Math.Min(bytes1.Length, bytes2.Length) / 2;
        int i = 0;
        while (i < maxLen && BitConverter.ToUInt16(bytes1, i * 2) == BitConverter.ToUInt16(bytes2, i * 2))
        {
            i++;
        }
        if (i >= maxLen)
            return 0;
        ushort c1 = BitConverter.ToUInt16(bytes1, i * 2);
        ushort c2 = BitConverter.ToUInt16(bytes2, i * 2);
        return c1 - c2;
    }

    private class CustomStringComparerDir : IComparer<DirectoryInfo>
    {
        public int Compare(DirectoryInfo d1, DirectoryInfo d2)
        {
            return Wcscmp(d1.Name, d2.Name);
        }
    }

    private class CustomStringComparerFile : IComparer<FileInfo>
    {
        public int Compare(FileInfo f1, FileInfo f2)
        {
            string name1 = Path.GetFileNameWithoutExtension(f1.Name);
            string name2 = Path.GetFileNameWithoutExtension(f2.Name);
            if (name1 == name2)
                return Wcscmp(Path.GetExtension(f1.Name), Path.GetExtension(f2.Name));
            return Wcscmp(name1, name2);
        }
    }

    private static int GetFileTypeByExtension(string ext, int fileSize)
    {
        switch (ext.ToLower())
        {
            case ".1s":
            case ".dds":
            case ".tga":
            case ".bmh":
            case ".bmx":
            case ".f30":
            case ".hdr":
            case ".fft":
            case ".wav":
                return 1;
            case ".uset":
            case ".xml":
                return 3;
            case ".png":
                return (fileSize <= 256) ? 4 : 5;
            case ".kap":
            case ".ogg":
            case ".jpg":
            case ".flac":
            case ".ksv":
                return 5;
            case ".bml":
                return 6;
            default:
                Console.WriteLine("Warning: unknown extension: " + ext);
                return 1;
        }
    }

    private static RhoFileProperty GetRhoFilePropertyByExtension(string ext, int fileSize)
    {
        int fileType = GetFileTypeByExtension(ext, fileSize);
        switch (fileType)
        {
            case 1:
                return RhoFileProperty.Compressed;
            case 3:
                return RhoFileProperty.Encrypted;
            case 4:
                return RhoFileProperty.PartialEncrypted;
            case 5:
                return RhoFileProperty.PartialEncrypted;
            case 6:
                return RhoFileProperty.CompressedEncrypted;
            default:
                return RhoFileProperty.None;
        }
    }
    #endregion
}