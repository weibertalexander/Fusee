using Fusee.Base.Core;
using System.IO;
using Microsoft.Data.Sqlite;

namespace Fusee.Examples.SQLiteViewer.Core
{
    public static class FileManager
    {
        private static string _ptdirectory = "C:/PROGRA~1/PotreeConverter";

        public static string PtDirectory
        {
            get { return _ptdirectory; }
        }

        private static string _ccdirectory = "C:/PROGRA~1/CloudCompare";

        public static string CcDirectory
        {
            get { return _ccdirectory; }
        }

        private static string _dbdirectory = "C:/Praktikum/datenbanken/";
        public static string DbDirectory
        {
            get { return _dbdirectory; }
        }

        private static string _convertedDirectory = "C:/Praktikum/datenbanken/potree";

        public static string ConvertedDirectory
        {
            get { return _convertedDirectory; }
        }

        private static int _footpulseSize = 10;

        public static int FootpulseSize
        {
            get { return _footpulseSize; }
        }

        public static int FootpulseAmount
        {
            get
            {
                SqliteConnection connection = new("Data Source=" + PtRenderingParams.Instance.PathToSqliteFile);
                connection.Open();
                // Create sqlite commands
                SqliteCommand noe = connection.CreateCommand();
                noe.CommandText = "SELECT number_of_entries FROM Metainformation";
                SqliteDataReader noe_reader = noe.ExecuteReader();
                noe_reader.Read();
                return noe_reader.GetInt32(0) * _footpulseSize;
            }
        }

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

        public static string GetConvDir()
        {
            return _convertedDirectory;
        }

        public static string[] GetSqliteFiles()
        {
            return Directory.GetFiles(GetDBDir(), "*.sqlite");
        }

        // Return first footpulse of current database.
        public static int FootpulseStart
        {
            get
            {
                SqliteConnection connection = new("Data Source=" + PtRenderingParams.Instance.PathToSqliteFile);
                connection.Open();
                // Create sqlite commands
                SqliteCommand startfp = connection.CreateCommand();
                startfp.CommandText = "SELECT footpulse_range_start FROM Metainformation";
                SqliteDataReader startfp_reader = startfp.ExecuteReader();
                startfp_reader.Read();
                return startfp_reader.GetInt32(0);
            }
        }

        // Return the last footpulse of current database.
        public static int FootpulseEnd
        {
            get
            {
                SqliteConnection connection = new("Data Source=" + PtRenderingParams.Instance.PathToSqliteFile);
                connection.Open();

                // Create sqlite commands
                SqliteCommand endfp = connection.CreateCommand();
                endfp.CommandText = "SELECT footpulse_range_end FROM Metainformation";
                SqliteDataReader endfp_reader = endfp.ExecuteReader();
                endfp_reader.Read();

                int result = endfp_reader.GetInt32(0);
                endfp_reader.Close();
                return result;

            }
        }

        // Returns the next sqlite file, specified in current database.
        public static string NextFile
        {
            get
            {
                SqliteConnection connection = new("Data Source=" + PtRenderingParams.Instance.PathToSqliteFile);
                connection.Open();

                // Create sqlite commands
                SqliteCommand next = connection.CreateCommand();
                next.CommandText = "SELECT file_name_subsequent FROM Metainformation";
                SqliteDataReader next_reader = next.ExecuteReader();
                next_reader.Read();

                string result = next_reader.GetString(0);
                next_reader.Close();
                return result;
            }
        }
        public static string NextFileFromPath(string path)
        {
            SqliteConnection connection = new("Data Source=" + path);
            connection.Open();

            // Create sqlite commands
            SqliteCommand next = connection.CreateCommand();
            next.CommandText = "SELECT file_name_subsequent FROM Metainformation";
            SqliteDataReader next_reader = next.ExecuteReader();
            next_reader.Read();

            string result = next_reader.GetString(0);
            next_reader.Close();
            if (File.Exists(_dbdirectory + "/" + result))
            {
                Diagnostics.Debug(result);
                return result;
            }
            return "";
        }

        #endregion Getters

        // Opens the explorer window at given location.
        public static void OpenFolderWithExplorer(string path)
        {
            string winpath = path.Replace("/", @"\");  // Windows paths work with backslash only.
            string argument =  winpath + "\"";

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }


        // Generate octree from sqlite database by multiple file conversions (wtf).
        // .sqlite -> .ply -> .laz -> octree
        public static void CreateOctreeFromDB(string filename)
        {
            string nameoffile = Path.GetFileNameWithoutExtension(filename);
            string job = nameoffile.Split("-")[0];
            // Check if file is already converted and delete ply and laz files if they exist.
            if (Directory.Exists($"C:/Praktikum/datenbanken/potree/{job}/{nameoffile}"))
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
                return;
            }
            else
            {
                SqliteConnection connection = new("Data Source=" + filename);
                connection.Open();

                // Create sqlite commands
                SqliteCommand data = connection.CreateCommand();
                data.CommandText = "SELECT data_points FROM Lichtraum";
                SqliteDataReader data_reader = data.ExecuteReader();

                SqliteCommand intensitydata = connection.CreateCommand();
                intensitydata.CommandText = "SELECT intensity FROM Lichtraum";
                SqliteDataReader intensity_reader = intensitydata.ExecuteReader();

                SqliteCommand scannerid = connection.CreateCommand();
                scannerid.CommandText = "SELECT scanner_id FROM Lichtraum";
                SqliteDataReader scannerid_reader = scannerid.ExecuteReader();

                int scanner0 = 0;
                int scanner1 = 0;
                int scanner2 = 0;
                int scanner3 = 0;
                int scanner8 = 0;
                int scanner9 = 0;
                //int error = 0;

                // Count the amount of points per scanner channel.
                while (scannerid_reader.Read())
                {
                    byte[] scanneridblob = (byte[])scannerid_reader.GetValue(0);
                    for (int i = 0; i < scanneridblob.Length; i++)
                    {
                        switch (scanneridblob[i])
                        {
                            case 0:
                                scanner0++;
                                break;
                            case 1:
                                scanner1++;
                                break;
                            case 2:
                                scanner2++;
                                break;
                            case 3:
                                scanner3++;
                                break;
                            case 8:
                                scanner8++;
                                break;
                            case 9:
                                scanner9++;
                                break;
                            default:
                                scanner0++;
                                break;
                        }
                    }
                }

                // Generate ply files.
                CreatePLYFile(scanner0, nameoffile + "_HSPA-Master");
                CreatePLYFile(scanner1, nameoffile + "_HSPA-Slave");
                CreatePLYFile(scanner2, nameoffile + "_HSPB-Master");
                CreatePLYFile(scanner3, nameoffile + "_HSPB-Slave");
                CreatePLYFile(scanner8, nameoffile + "_HRS1");
                CreatePLYFile(scanner9, nameoffile + "_HRS2");

                // Reset scannerid reader.
                scannerid_reader.Close();
                scannerid_reader = scannerid.ExecuteReader();

                // Create fstreams for each file.
                FileStream fstream0 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HSPA-Master.ply", FileMode.Append);
                FileStream fstream1 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HSPA-Slave.ply", FileMode.Append);
                FileStream fstream2 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HSPB-Master.ply", FileMode.Append);
                FileStream fstream3 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HSPB-Slave.ply", FileMode.Append);
                FileStream fstream8 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HRS1.ply", FileMode.Append);
                FileStream fstream9 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HRS2.ply", FileMode.Append);

                // Write data to ply files.
                while (data_reader.Read() && intensity_reader.Read() && scannerid_reader.Read())
                {
                    byte[] datablob = (byte[])data_reader.GetValue(0);
                    byte[] intensityblob = (byte[])intensity_reader.GetValue(0);
                    byte[] scanneridblob = (byte[])scannerid_reader.GetValue(0);

                    for (int i = 0; i < datablob.Length / 12; i++)  // datablob.Length == number of points
                    {
                        // Choose fstream according to scanner channel.
                        FileStream current;
                        switch (scanneridblob[i])
                        {
                            case 0:
                                current = fstream0;
                                break;
                            case 1:
                                current = fstream1;
                                break;
                            case 2:
                                current = fstream2;
                                break;
                            case 3:
                                current = fstream3;
                                break;
                            case 8:
                                current = fstream8;
                                break;
                            case 9:
                                current = fstream9;
                                break;
                            default:
                                current = fstream9;
                                break;
                        }
                        // Write 12 bytes of data to ply.
                        for (int j = 0; j < 12; j++)
                        {
                            current.WriteByte(datablob[12 * i + j]);
                        }

                        // Write 2 bytes of intensity data to ply.
                        for (int j = 0; j < 2; j++)
                        {
                            current.WriteByte(intensityblob[2 * i + j]);
                        }
                    }
                }
                fstream0.Close();
                fstream1.Close();
                fstream2.Close();
                fstream3.Close();
                fstream8.Close();
                fstream9.Close();

                string[] scannerFiles =
                {
                        $"HSPA-Master",
                        $"HSPA-Slave",
                        $"HSPB-Master",
                        $"HSPB-Slave",
                        $"HRS1",
                        $"HRS2"
                    };

                // Generate las files from ply with cloudcompare.
                foreach (string file in scannerFiles)
                {
                    System.Diagnostics.Process processCC = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfoCC = new System.Diagnostics.ProcessStartInfo("cmd.exe");
                    startInfoCC.Arguments = $"/C {_ccdirectory}/CloudCompare.exe -SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}_{file}.ply -AUTO_SAVE OFF -NO_TIMESTAMP -RENAME_SF LAST Intensity -SET_ACTIVE_SF 0 -SF_CONVERT_TO_RGB FALSE -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}_{file}.las";
                    processCC.StartInfo = startInfoCC;
                    processCC.Start();
                    processCC.WaitForExit();
                    processCC.Close();
                    DeletePLY($"{nameoffile}_{file}");
                }

                // Generate octrees from las.
                foreach (string file in scannerFiles)
                {
                    System.Diagnostics.Process processPT = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfoPT = new System.Diagnostics.ProcessStartInfo("cmd.exe");
                    startInfoPT.Arguments = $"/C {_ptdirectory}/PotreeConverter.exe C:/Praktikum/datenbanken/laz/{nameoffile}_{file}.las -o C:/Praktikum/datenbanken/potree/{job}/{nameoffile}/{nameoffile}_{file}";
                    processPT.StartInfo = startInfoPT;
                    processPT.Start();
                    processPT.WaitForExit();
                    processPT.Close();
                    DeleteLAZ($"{nameoffile}_{file}");
                }
            }
        }


        public static async Task<bool> CreateOctreeFromDBAsync(string filename)
        {
            string nameoffile = Path.GetFileNameWithoutExtension(filename);
            string job = nameoffile.Split("-")[0];
            // Check if file is already converted and delete ply and laz files if they exist.
            if (Directory.Exists($"C:/Praktikum/datenbanken/potree/{job}/{nameoffile}"))
            {
                Diagnostics.Debug($"{nameoffile}.sqlite already converted! Skipping.");

                // Delete files.
                if (File.Exists($"C:/Praktikum/datenbanken/ply/{nameoffile}.ply"))
                {
                    DeletePLY(nameoffile);
                }
                if (File.Exists($"C:/Praktikum/datenbanken/laz/{nameoffile}.las"))
                {
                    DeleteLAZ(nameoffile);
                }
                return true;
            }
            else
            {

                //_sqliteData = new SqliteData();
                SqliteConnection connection = new("Data Source=" + filename);
                connection.Open();

                // Create sqlite commands
                SqliteCommand data = connection.CreateCommand();
                data.CommandText = "SELECT data_points FROM Lichtraum";
                SqliteDataReader data_reader = data.ExecuteReader();

                SqliteCommand intensitydata = connection.CreateCommand();
                intensitydata.CommandText = "SELECT intensity FROM Lichtraum";
                SqliteDataReader intensity_reader = intensitydata.ExecuteReader();

                SqliteCommand scannerid = connection.CreateCommand();
                scannerid.CommandText = "SELECT scanner_id FROM Lichtraum";
                SqliteDataReader scannerid_reader = scannerid.ExecuteReader();

                int scanner0 = 0;
                int scanner1 = 0;
                int scanner2 = 0;
                int scanner3 = 0;
                int scanner8 = 0;
                int scanner9 = 0;

                // Count the amount of points per scanner channel.
                while (scannerid_reader.Read())
                {
                    byte[] scanneridblob = (byte[])scannerid_reader.GetValue(0);
                    for (int i = 0; i < scanneridblob.Length; i++)
                    {
                        switch (scanneridblob[i])
                        {
                            case 0:
                                scanner0++;
                                break;
                            case 1:
                                scanner1++;
                                break;
                            case 2:
                                scanner2++;
                                break;
                            case 3:
                                scanner3++;
                                break;
                            case 8:
                                scanner8++;
                                break;
                            case 9:
                                scanner9++;
                                break;
                            default:
                                scanner0++;
                                break;
                        }
                    }
                }

                // Generate ply files.
                CreatePLYFile(scanner0, nameoffile + "_HSPA-Master");
                CreatePLYFile(scanner1, nameoffile + "_HSPA-Slave");
                CreatePLYFile(scanner2, nameoffile + "_HSPB-Master");
                CreatePLYFile(scanner3, nameoffile + "_HSPB-Slave");
                CreatePLYFile(scanner8, nameoffile + "_HRS1");
                CreatePLYFile(scanner9, nameoffile + "_HRS2");

                // Reset scannerid reader.
                scannerid_reader.Close();
                scannerid_reader = scannerid.ExecuteReader();

                // Create fstreams for each file.
                FileStream fstream0 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HSPA-Master.ply", FileMode.Append);
                FileStream fstream1 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HSPA-Slave.ply", FileMode.Append);
                FileStream fstream2 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HSPB-Master.ply", FileMode.Append);
                FileStream fstream3 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HSPB-Slave.ply", FileMode.Append);
                FileStream fstream8 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HRS1.ply", FileMode.Append);
                FileStream fstream9 = new FileStream($"{_dbdirectory}/ply/{nameoffile}_HRS2.ply", FileMode.Append);

                // Write data to ply files.
                while (data_reader.Read() && intensity_reader.Read() && scannerid_reader.Read())
                {
                    byte[] datablob = (byte[])data_reader.GetValue(0);
                    byte[] intensityblob = (byte[])intensity_reader.GetValue(0);
                    byte[] scanneridblob = (byte[])scannerid_reader.GetValue(0);

                    for (int i = 0; i < datablob.Length / 12; i++)  // datablob.Length == number of points
                    {
                        // Choose fstream according to scanner channel.
                        FileStream current;
                        switch (scanneridblob[i])
                        {
                            case 0:
                                current = fstream0;
                                break;
                            case 1:
                                current = fstream1;
                                break;
                            case 2:
                                current = fstream2;
                                break;
                            case 3:
                                current = fstream3;
                                break;
                            case 8:
                                current = fstream8;
                                break;
                            case 9:
                                current = fstream9;
                                break;
                            default:
                                current = fstream9;
                                break;
                        }
                        // Write 12 bytes of data to ply.
                        for (int j = 0; j < 12; j++)
                        {
                            current.WriteByte(datablob[12 * i + j]);
                        }

                        // Write 2 bytes of intensity data to ply.
                        for (int j = 0; j < 2; j++)
                        {
                            current.WriteByte(intensityblob[2 * i + j]);
                        }
                    }
                }
                fstream0.Close();
                fstream1.Close();
                fstream2.Close();
                fstream3.Close();
                fstream8.Close();
                fstream9.Close();

                string[] scannerFiles =
                {
                    $"HSPA-Master",
                    $"HSPA-Slave",
                    $"HSPB-Master",
                    $"HSPB-Slave",
                    $"HRS1",
                    $"HRS2"
                };

                // Generate las files from ply with cloudcompare.
                foreach (string file in scannerFiles)
                {
                    System.Diagnostics.Process processCC = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfoCC = new System.Diagnostics.ProcessStartInfo("cmd.exe");
                    startInfoCC.Arguments = $"/C {_ccdirectory}/CloudCompare.exe -SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}_{file}.ply -AUTO_SAVE OFF -NO_TIMESTAMP -RENAME_SF LAST Intensity -SET_ACTIVE_SF 0 -SF_CONVERT_TO_RGB FALSE -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}_{file}.las";
                    processCC.StartInfo = startInfoCC;
                    processCC.Start();
                    processCC.WaitForExit();
                    processCC.Close();
                    DeletePLY($"{nameoffile}_{file}");
                }

                // Generate octrees from las.
                foreach (string file in scannerFiles)
                {
                    System.Diagnostics.Process processPT = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfoPT = new System.Diagnostics.ProcessStartInfo("cmd.exe");
                    startInfoPT.Arguments = $"/C {_ptdirectory}/PotreeConverter.exe C:/Praktikum/datenbanken/laz/{nameoffile}_{file}.las -o C:/Praktikum/datenbanken/potree/{job}/{nameoffile}/{nameoffile}_{file}";
                    processPT.StartInfo = startInfoPT;
                    processPT.Start();
                    processPT.WaitForExit();
                    processPT.Close();
                    DeleteLAZ($"{nameoffile}_{file}");
                }
                return true;
            }
        }
        // Create necessary directories used for file conversion.
        public static void CreateDirectories()
        {
            Directory.CreateDirectory($"{_dbdirectory}/ply/");
            Directory.CreateDirectory($"{_dbdirectory}/laz/");
            Directory.CreateDirectory($"{_dbdirectory}/potree/");
        }

        // Creates the header of a ply file.
        private static void CreatePLYFile(long amount, string filename)
        {
            Diagnostics.Debug("Pointamount: " + amount);
            string path = $"{_dbdirectory}/ply/{filename}.ply";
            // Write header data.
            string[] header =
            {
                "ply",
                "format binary_little_endian 1.0",
                $"element vertex {amount}",
                "property float x",
                "property float z",
                "property float y",
                "property uint16 intensity",
                "end_header"
            };
            File.WriteAllLines(path, header);
        }

        // Delete ply files after converting to las.
        private static void DeletePLY(string filename)
        {
            File.Delete($"C:/Praktikum/datenbanken/ply/{filename}.ply");
            Diagnostics.Debug($"Deleted {filename}.ply.");
        }

        // Delete las/laz files after converting to octree.
        private static void DeleteLAZ(string filename)
        {
            File.Delete($"C:/Praktikum/datenbanken/laz/{filename}.las");
            Diagnostics.Debug($"Deleted {filename}.las.");
        }
    }
}
