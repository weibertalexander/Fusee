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
        public static string dbDirectory
        {
            get { return _dbdirectory; }
        }

        private static string _convertedDirectory = "C:/Praktikum/datenbanken/potree";

        public static string ConvertedDirectory
        {
            get { return _convertedDirectory; }
        }

        public static int _footpulseSize = Constants.FootpulseAmount;

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
        #endregion Getters

        /*
        // Generate multiple pointclouds from one sqlite file.
        public static void CreateOctreesFromDB(string filename)
        {
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

            SqliteCommand idata = connection.CreateCommand();
            idata.CommandText = "SELECT intensity FROM Lichtraum";
            SqliteDataReader intensity_reader = idata.ExecuteReader();

            int fileCounter = 0;
            while (data_reader.Read() && fileCounter < 10)
            {
                string nameoffile = Path.GetFileNameWithoutExtension(filename) + $"_{fileCounter}";

                long pointAmount = 0;
                for (int j = 0; j < _footpulseSize; j++)
                {
                    if (nop_reader.Read())
                    {
                        pointAmount += (long)nop_reader.GetValue(0);
                    }
                }
                // Generate ply file.
                //CreatePLYFile((long)nop_reader.GetValue(0), nameoffile);
                CreatePLYFile(pointAmount, nameoffile);

                // Write first row directly to file as while loop reads next row. Continue with the remaining rows.
                WritePLYFile((byte[])data_reader.GetValue(0), nameoffile);
                for (int j = 1; j < _footpulseSize; j++)
                {
                    if (data_reader.Read())
                    {
                        WritePLYFile((byte[])data_reader.GetValue(0), nameoffile);
                    }
                }

                // Generate laz files from ply using CloudCompare.
                System.Diagnostics.Process processCC = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfoCC = new System.Diagnostics.ProcessStartInfo();
                startInfoCC.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfoCC.FileName = $"{_ccdirectory}/CloudCompare.exe";
                startInfoCC.Arguments = $"-SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}.ply -NO_TIMESTAMP -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}.laz";
                processCC.StartInfo = startInfoCC;
                processCC.Start();
                processCC.WaitForExit();

                // Delete ply file as it was converted to laz.
                DeletePLY(nameoffile);

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
                DeleteLAZ(nameoffile);
                fileCounter++;
            }
        }
        */
        // Generate octree from sqlite database by multiple file conversions (wtf).
        // .sqlite -> .ply -> .laz -> octree
        public static void CreateOctreeFromDB(string filename)
        {
            string nameoffile = Path.GetFileNameWithoutExtension(filename);
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
                return;
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

                SqliteCommand nop = connection.CreateCommand();
                nop.CommandText = "SELECT number_of_points FROM Lichtraum";
                SqliteDataReader nop_reader = nop.ExecuteReader();

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
                    startInfoCC.Arguments = $"/C {_ccdirectory}/CloudCompare.exe -SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}_{file}.ply -AUTO_SAVE OFF -NO_TIMESTAMP -RENAME_SF LAST Intensity -SET_ACTIVE_SF 0 -SF_CONVERT_TO_RGB FALSE -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}_{file}.laz";
                    processCC.StartInfo = startInfoCC;
                    processCC.Start();
                    processCC.WaitForExit();
                    processCC.Close();
                    DeletePLY($"{nameoffile}_{file}");
                }

                /*
                // Generate bat file to convert pointclouds at the same time.
                string plyToLazBat = $"C:/Praktikum/datenbanken/ply/{nameoffile}.bat";
                using (StreamWriter batFile = new StreamWriter(plyToLazBat))
                {
                    batFile.WriteLine($"START {_ccdirectory}/CloudCompare.exe -SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}_HSPA-Master.ply -AUTO_SAVE OFF -NO_TIMESTAMP -RENAME_SF LAST Intensity -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}_HSPA-Master.las");
                    batFile.WriteLine($"START {_ccdirectory}/CloudCompare.exe -SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}_HSPA-Slave.ply -AUTO_SAVE OFF -NO_TIMESTAMP -RENAME_SF LAST Intensity -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}_HSPA-Slave.las");
                    batFile.WriteLine($"START {_ccdirectory}/CloudCompare.exe -SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}_HSPB-Master.ply -AUTO_SAVE OFF -NO_TIMESTAMP -RENAME_SF LAST Intensity -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}_HSPB-Master.las");
                    batFile.WriteLine($"START {_ccdirectory}/CloudCompare.exe -SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}_HSPB-Slave.ply -AUTO_SAVE OFF -NO_TIMESTAMP -RENAME_SF LAST Intensity -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}_HSPB-Slave.las");
                    batFile.WriteLine($"START {_ccdirectory}/CloudCompare.exe -SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}_HRS1.ply -AUTO_SAVE OFF -NO_TIMESTAMP -RENAME_SF LAST Intensity -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}_HRS1.las");
                    batFile.WriteLine($"START {_ccdirectory}/CloudCompare.exe -SILENT -C_EXPORT_FMT LAS -o C:/Praktikum/datenbanken/ply/{nameoffile}_HRS2.ply -AUTO_SAVE OFF -NO_TIMESTAMP -RENAME_SF LAST Intensity -SAVE_CLOUDS FILE C:/Praktikum/datenbanken/laz/{nameoffile}_HRS2.las");
                }
                // Generate laz file from ply using CloudCompare.
                System.Diagnostics.Process processCC = new System.Diagnostics.Process();

                processCC.StartInfo.UseShellExecute = true;

                System.Diagnostics.ProcessStartInfo startInfoCC = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + plyToLazBat);

                startInfoCC.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                processCC.StartInfo = startInfoCC;
                processCC.Start();
                processCC.WaitForExit();
                */

                // Generate octrees from las.
                foreach (string file in scannerFiles)
                {
                    System.Diagnostics.Process processPT = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfoPT = new System.Diagnostics.ProcessStartInfo("cmd.exe");
                    startInfoPT.Arguments = $"/C {_ptdirectory}/PotreeConverter.exe C:/Praktikum/datenbanken/laz/{nameoffile}_{file}.laz -o C:/Praktikum/datenbanken/potree/{nameoffile}/{nameoffile}_{file}";
                    processPT.StartInfo = startInfoPT;
                    processPT.Start();
                    processPT.WaitForExit();
                    processPT.Close();
                    DeleteLAZ($"{nameoffile}_{file}");
                }

                /*
                // Generate bat file to get octrees from laz.
                string lazToOctreeBat = $"C:/Praktikum/datenbanken/laz/{nameoffile}.bat";
                using (StreamWriter batFile = new StreamWriter(lazToOctreeBat))
                {
                    batFile.WriteLine($"START {_ptdirectory}/PotreeConverter.exe C:/Praktikum/datenbanken/laz/{nameoffile}_HSPA-Master.las -o C:/Praktikum/datenbanken/potree/{nameoffile}/{nameoffile}_HSPA-Master");
                    batFile.WriteLine($"START {_ptdirectory}/PotreeConverter.exe C:/Praktikum/datenbanken/laz/{nameoffile}_HSPA-Slave.las -o C:/Praktikum/datenbanken/potree/{nameoffile}/{nameoffile}_HSPA-Slave");
                    batFile.WriteLine($"START {_ptdirectory}/PotreeConverter.exe C:/Praktikum/datenbanken/laz/{nameoffile}_HSPB-Master.las -o C:/Praktikum/datenbanken/potree/{nameoffile}/{nameoffile}_HSPB-Master");
                    batFile.WriteLine($"START {_ptdirectory}/PotreeConverter.exe C:/Praktikum/datenbanken/laz/{nameoffile}_HSPB-Slave.las -o C:/Praktikum/datenbanken/potree/{nameoffile}/{nameoffile}_HSPB-Slave");
                    batFile.WriteLine($"START {_ptdirectory}/PotreeConverter.exe C:/Praktikum/datenbanken/laz/{nameoffile}_HRS1.las -o C:/Praktikum/datenbanken/potree/{nameoffile}/{nameoffile}_HRS1");
                    batFile.WriteLine($"START {_ptdirectory}/PotreeConverter.exe C:/Praktikum/datenbanken/laz/{nameoffile}_HRS2.las -o C:/Praktikum/datenbanken/potree/{nameoffile}/{nameoffile}_HRS2");
                }

                // Generate octrees from LAZ
                System.Diagnostics.Process processPT = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo startInfoPT = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c " + lazToOctreeBat);
                processPT.StartInfo.UseShellExecute = true;
                startInfoPT.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

                processPT.StartInfo = startInfoPT;
                processPT.Start();
                processPT.WaitForExit();
                */


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

        // Write to ply file. Too slow because for every byte an fstream is opened and closed.
        private static void WriteToPLYFile(byte data, string filename, int scannerID)
        {
            string path;
            switch (scannerID)
            {
                // HSPA-Master
                case 0:
                    path = $"{_dbdirectory}/ply/{filename}_HSPA-Master.ply";
                    break;
                // HSPA-Slave
                case 1:
                    path = $"{_dbdirectory}/ply/{filename}_HSPA-Slave.ply";
                    break;
                // HSPB-Master
                case 2:
                    path = $"{_dbdirectory}/ply/{filename}_HSPB-Master.ply";
                    break;
                // HSPB-Slave
                case 3:
                    path = $"{_dbdirectory}/ply/{filename}_HSPB-Slave.ply";
                    break;
                // HRS1
                case 8:
                    path = $"{_dbdirectory}/ply/{filename}_HRS1.ply";
                    break;
                // HRS2
                case 9:
                    path = $"{_dbdirectory}/ply/{filename}_HRS2.ply";
                    break;
                // In case of an unknown error with scanner ids.
                default:
                    path = $"{_dbdirectory}/ply/{filename}_Error.ply";
                    break;
            }

            FileStream fstream = new FileStream(path, FileMode.Append);
            fstream.WriteByte(data);
            fstream.Close();
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
            File.Delete($"C:/Praktikum/datenbanken/laz/{filename}.laz");
            Diagnostics.Debug($"Deleted {filename}.laz.");
        }
    }
}
