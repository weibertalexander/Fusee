using Fusee.Base.Core;
using System.IO;
using Microsoft.Data.Sqlite;
public static class FileManager
{

    private static string _ptdirectory = @"C:/Program Files/PotreeConverter";
    private static string _ccdirectory = @"C:/Program Files/CloudCompare";
    private static string _dbdirectory = "C:/Praktikum/datenbanken/";

    #region Getters
    public static string GetDBDir()
    {
        return _dbdirectory;
    }

    public static string GetCCDir()
    {
        return _ccdirectory;
    }

    public static string GetPTDir()
    {
        return _ptdirectory;
    }
    public static string[] GetSqliteFiles()
    {
        return Directory.GetFiles(GetDBDir(), "*.sqlite");
    }
    #endregion Getters

    // Generate octree from sqlite database by multiple file conversions (wtf).
    // .sqlite -> .ply -> .laz -> octree
    public static void CreateOctreeFromDB(string filename)
    {
        //_sqliteData = new SqliteData();

        string nameoffile = Path.GetFileNameWithoutExtension(filename);

        SqliteConnection connection = new("Data Source=" + filename);
        connection.Open();

        // Create sqlite commands
        SqliteCommand data = connection.CreateCommand();
        data.CommandText = "SELECT data_points FROM Lichtraum";
        SqliteDataReader data_reader = data.ExecuteReader();

        SqliteCommand nop = connection.CreateCommand();
        //nop.CommandText = "SELECT sum(number_of_points) FROM Lichtraum";
        nop.CommandText = "SELECT number_of_points FROM Lichtraum";
        SqliteDataReader nop_reader = nop.ExecuteReader();

        //nop_reader.Read();

        // Check if file is already converted and delete ply and laz files if they exist.
        if (Directory.Exists($"C:/Praktikum/datenbanken/potree/{nameoffile}"))
        {
            Diagnostics.Debug($"{nameoffile}.sqlite already converted! Skipping.");

            // Delete files.
            if (File.Exists($"C:/Praktikum/datenbanken/ply/{nameoffile}.ply"))
            {
                DeletePLY(nameoffile);
            }
            if (File.Exists($"C:/Praktikum/datenbanken/laz/{nameoffile}.laz"))
            {
                DeleteLAZ(nameoffile);
            }
        }
        else
        {
            int rows = 50;
            long am = 0;
            for (int i = 0; i < rows; i++)
            {
                nop_reader.Read();
                am += (long)nop_reader.GetValue(0);
            }
            // Generate ply file.
            //CreatePLYFile((long)nop_reader.GetValue(0), nameoffile);
            CreatePLYFile(am, nameoffile);
            //while (data_reader.Read())

            for (int i = 0; i < rows; i++)
            {
                data_reader.Read();
                byte[] datablob = (byte[])data_reader.GetValue(0);
                WritePLYFile(datablob, nameoffile);
            }

            // Generate laz file from ply using CloudCompare.
            System.Diagnostics.Process processCC = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfoCC = new System.Diagnostics.ProcessStartInfo();
            startInfoCC.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfoCC.FileName = $"{_ccdirectory}/CloudCompare.exe";
            startInfoCC.Arguments = $"-SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}.ply -NO_TIMESTAMP -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}.laz";
            processCC.StartInfo = startInfoCC;
            processCC.Start();
            processCC.WaitForExit();

            // Delete ply file as it was converted to laz.
            //DeletePLY(nameoffile);

            // Generate Octree from LAZ.
            System.Diagnostics.Process processPT = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfoPT = new System.Diagnostics.ProcessStartInfo();
            startInfoPT.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfoPT.FileName = $"{_ptdirectory}/PotreeConverter.exe";
            startInfoPT.Arguments = $"C:/Praktikum/datenbanken/laz/{nameoffile}.laz -o C:/Praktikum/datenbanken/potree/{nameoffile}/";
            processPT.StartInfo = startInfoPT;
            processPT.Start();
            processPT.WaitForExit();

            // Delete laz file as it was converted to octree.
            //DeleteLAZ(nameoffile);
        }
        //Fusee.PointCloud.Potree.V2.Potree2Reader potreeReader = new();
        //IPointCloud cloud = potreeReader.GetPointCloudComponent("C:/Users/alexa/OneDrive/Desktop/Studium/3.Semester_Prax/Praktisches/datenbanken/potree/");
    }

    // Create necessary directories used for file conversion.
    public static void CreateDirectories()
    {
        Directory.CreateDirectory($"{_dbdirectory}/ply/");
        Directory.CreateDirectory($"{_dbdirectory}/laz/");
        Directory.CreateDirectory($"{_dbdirectory}/potree/");
    }

    private static void CreatePLYFile(long amount, string filename)
    {
        Diagnostics.Debug(amount);
        string path = $"{_dbdirectory}/ply/{filename}.ply";
        // Write header data.
        string[] header =
        {
                "ply",
                "format binary_little_endian 1.0",
                $"element vertex {amount}",
                "property float x",
                "property float y",
                "property float z",
                "end_header"
            };
        File.WriteAllLines(path, header);
    }

    private static void WritePLYFile(byte[] data, string filename)
    {
        string path = $"{_dbdirectory}/ply/{filename}.ply";
        FileStream fstream = new FileStream(path, FileMode.Append);
        for (int i = 0; i < data.Length; i++)
        {
            fstream.WriteByte(data[i]);
        }
        //Diagnostics.Debug("Finished writing current row");
        fstream.Close();
    }

    private static void DeletePLY(string filename)
    {
        File.Delete($"C:/Praktikum/datenbanken/ply/{filename}.ply");
        Diagnostics.Debug($"Deleted {filename}.ply.");
    }

    private static void DeleteLAZ(string filename)
    {
        File.Delete($"C:/Praktikum/datenbanken/laz/{filename}.laz");
        Diagnostics.Debug($"Deleted {filename}.laz.");
    }
}
