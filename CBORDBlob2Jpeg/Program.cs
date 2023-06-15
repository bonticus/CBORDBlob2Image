using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Oracle.ManagedDataAccess.Client;

namespace CBORDBlob2Jpeg
{
    class Program
    {
        static void Main(string[] args)
        {
            //test
            string dbuser = "fillmein";
            string dbpass = "fillmeintoo";
            string folder = @"S:\CBORD\IDCARDS\IMAGE\";
            string sql = "select K.KEYVALUE,DBD_IMAGE from DIEBOLD.PATRONIMAGES p JOIN DIEBOLD.KEYMAPPINGINFO K ON P.PATRONID = K.PATRONID AND K.MEDIATYPE = -1 where p.LASTUPDATE between sysdate-1 and sysdate";
            string strconn = "Data Source=oraclegold.cwu.edu;User Id="+dbuser+";Password="+dbpass+";";
            List<string[]> idList = new List<string[]>();
            using (OracleConnection conn = new OracleConnection(strconn))
            {
                conn.Open();
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    using (IDataReader dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            try
                            {
                                //convert filename to what peoplesoft is expecting. id # 024790985 translates to a filename of 02479098.5_i
                                string filename = dataReader[0].ToString().Substring(0, 8) + '.' + dataReader[0].ToString().Substring(8, 1) + "_i";
                                string cwuid = dataReader[0].ToString();
                                
                                if(dataReader[1].GetType().ToString() != "System.DBNull")
                                {
                                    byte[] byteArray = (Byte[])dataReader[1];
                                
                                    if(File.Exists(folder + filename))
                                    {
                                        File.Delete(folder + filename);
                                    }

                                    using (FileStream fs = new FileStream
                                    (folder + filename, FileMode.CreateNew, FileAccess.Write))
                                    {
                                        fs.Write(byteArray, 0, byteArray.Length);
                                    }
                                    string[] contents = { cwuid, filename };
                                    idList.Add(contents);
                                }
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine(e.ToString());
                            }
                        }
                    }
                }
            }
            Console.WriteLine("IDs copied:");

            //this part is probably unnecessary now, it just sets the network path in the database in case gold gui is still configured to look at a folder/path instead of the db.
            foreach (var id in idList)
            {
                string networkfolder = @"Q:\IDCARDS\IMAGE\";
                Console.WriteLine("ID: " + id[0] + ", Filename: " + id[1]);
                sql = @"update extendedpatroninfotab set dbd_image_path = '" + networkfolder + id[1] + "' where patronid = (select patronid from keymappinginfo where mediatype = '-1' and keyvalue = '" + id[0] + "')";
                Console.WriteLine(sql);
                using (OracleConnection conn = new OracleConnection(strconn))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    try
                    {
                        OracleDataReader reader = cmd.ExecuteReader();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
            }
            Environment.Exit(0);
        }
    }
}
