using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.InteropServices;
using System.Web.Services;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace WebSWMM
{
    /// <summary>
    /// WebSWMM 的摘要说明
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // 若要允许使用 ASP.NET AJAX 从脚本中调用此 Web 服务，请取消注释以下行。 
    // [System.Web.Script.Services.ScriptService]
    public class WebSWMM : System.Web.Services.WebService
    {

        long filestream()
        {
            string outputFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.out";
            FileStream bn = new FileStream(outputFilePath, FileMode.Open);
            long m = bn.Length;
            bn.Close();
            return m;



        }


        //GetSwmmVersion—Done5.1
        [DllImport("D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\swmm5.dll", EntryPoint = "swmm_getVersion", CallingConvention = CallingConvention.Cdecl)]
        public static extern int swmm_getVersion();

        //testCustom—Done5.1—测试用1.0版
        [DllImport("D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\swmm5.dll", EntryPoint = "test_custom", CallingConvention = CallingConvention.StdCall)]
        public static extern int test_custom(int a);

        //testCustom—Done5.1—测试用2.0版
        [DllImport("D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\swmm5.dll", EntryPoint = "test_secondary", CallingConvention = CallingConvention.StdCall)]
        public static extern int test_secondary();

        //RunSwmmDll1—Done5.1
        [DllImport("D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\swmm5.dll", EntryPoint = "swmm_run", CallingConvention = CallingConvention.StdCall)]
        public static extern int swmm_run(string f1, string f2, string f3);

        //OpenSwmmOutFile—Done5.1
        [DllImport("D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\swmm5.dll", EntryPoint = "OpenSwmmOutFile", CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenSwmmOutFile(string f4);


        //Done—版本5.1     
        [WebMethod]
        public int Get_Version()
        {


            int version = swmm_getVersion();
            return version;

        }
        //Done—运行5.1
        [WebMethod]
        public int Run_SwmmDll()
        {

            string inpFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.inp";
            string reportFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.rpt";
            string outputFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.out";
            int err = swmm_run(inpFilePath, reportFilePath, outputFilePath);
            return err;




        }
        //Done—打开.out 5.1
        [WebMethod]
        public int Open_SwmmOutFile()
        {

            string outputFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.out";
            int err = OpenSwmmOutFile(outputFilePath);
            return err;

        }
        //Done—获取.out结果5.1
        [WebMethod]
        public float Get_SwmmResult(int numperiod, int type, int lindex, int vindex)
        {

            string outputFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.out";

            float value = 0;

            long numbyte = filestream();

            BinaryReader br = new BinaryReader(new FileStream(outputFilePath, FileMode.Open));

            br.BaseStream.Position = 0;//从0 — IDStartPos

            // 1. Before IDStartPos：这些信息不是很重要，因为完全可以从.rpt文件读取，这里读取了全部
            int magicnumber = br.ReadInt32();//ReadInt32()读取文件前四个字节，即MagicNumber：516114522
            int version = br.ReadInt32();//顺着magicnumber第4个字节往后再读4个字节，即Version：51013
            int flowunits = br.ReadInt32();//顺着version第8个字节往后再读4个字节，即FlowUnits：4( 4代表的流量单位详见附录)
            int numsubcatch = br.ReadInt32();//顺着flowunits第12个字节往后再读4个字节，即NumSubcatch：3
            int numnodes = br.ReadInt32();//顺着numsubcatch第16个字节往后再读4个字节，即NumNodes：5
            int numlinks = br.ReadInt32();//顺着numnodes第20个字节往后再读4个字节，即NumLinks：4
            int numpolluts = br.ReadInt32(); //顺着numlinks第24个字节往后再读4个字节，即NumPolluts：0

            // 2. IDStartPos — InputStartPos：这些信息也不是很重要，因为完全可以从.rpt文件读取，这里不进行全部读取
            int subcatch_length = br.ReadInt32(); //Subcatch[0].id.length，第一个subcatch的ID的字符串长度：4
            char[] subcatch_id = br.ReadChars(subcatch_length);//Subcatch[0].id，第一个集subcatch的ID：ZMJ1
                                                               //Console.WriteLine(subcatch_id);

            // 3. InputStartPos — OutputStartPos：这一部分无价值，这里不进行读取


            // 4. Outputstartpos — end：     重点来了！！！！！！
            int NsubcatchResults = 8 + numpolluts;
            int NnodeResults = 6 + numpolluts;
            int NlinkResults = 5 + numpolluts;
            //计算输出的每一个时间步长所占的字节数！！[ Date = 8; subcatch; nodes; links; system various = 15*4 ]:364
            int BytesPerPeriod = 8 + numsubcatch * NsubcatchResults * 4 + numnodes * NnodeResults * 4 + numlinks * NlinkResults * 4 + 15 * 4; //开头+8是时间Date占的字节 


            //[ 二进制总字节数-16 ] 位置为OutputStartpos所在位置！！！:476

            br.BaseStream.Position = numbyte - 16;
            int OutputStartPos = br.ReadInt32();
            int aim_byteStartPos = OutputStartPos + (BytesPerPeriod * (numperiod - 1));
            if (type == 1)
            {

                br.BaseStream.Position = aim_byteStartPos + 8 + (NsubcatchResults * 4 * (lindex - 1)) + (4 * (vindex - 1));
                value = br.ReadSingle();



            }
            else if (type == 2)
            {

                br.BaseStream.Position = aim_byteStartPos + (8 + (numsubcatch * NsubcatchResults * 4) + (NnodeResults * 4 * (lindex - 1))) + (4 * (vindex - 1));
                value = br.ReadSingle();

            }
            else if (type == 3)
            {
                br.BaseStream.Position = aim_byteStartPos + (8 + (numsubcatch * NsubcatchResults * 4) + (numnodes * NnodeResults * 4) + (NlinkResults * 4 * (lindex - 1))) + (4 * (vindex - 1));
                value = br.ReadSingle();

            }
            else
            {

                return 1;

            }


            br.Close();
            return value;



        }
        //Done—获取.rpt 5.1
        [WebMethod]
        public string[] Get_rptFile()
        {

            string reportFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.rpt";
            string[] lineContend = new string[195];

            if (string.IsNullOrEmpty(reportFilePath))
            {
                return null;
            }

            else
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(reportFilePath, Encoding.Default);
                string line;

                while ((sr.ReadLine()) != null)
                {

                    for (int i = 0; i < 195; i++)
                    {
                        line = sr.ReadLine();
                        lineContend[i] = line;


                    }



                }

                sr.Close();

            }

            return lineContend;


        }
        //Done—获取num_period
        [WebMethod]
        public int Get_SwmmNperiod()
        {
            string outputFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.out";

            long numbyte = filestream();
            BinaryReader br = new BinaryReader(new FileStream(outputFilePath, FileMode.Open));

            br.BaseStream.Position = numbyte - 12;
            int Nperiods = br.ReadInt32();

            br.Close();
            return Nperiods;

        }
        //Done—获取num_subcatch
        [WebMethod]
        public int Get_SwmmNsubcatch()
        {

            string outputFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.out";

            BinaryReader br = new BinaryReader(new FileStream(outputFilePath, FileMode.Open));

            br.BaseStream.Position = 0;//从0 — IDStartPos

            // 1. Before IDStartPos：这些信息不是很重要，因为完全可以从.rpt文件读取，这里读取了全部
            int magicnumber = br.ReadInt32();//ReadInt32()读取文件前四个字节，即MagicNumber：516114522
            int version = br.ReadInt32();//顺着magicnumber第4个字节往后再读4个字节，即Version：51013
            int flowunits = br.ReadInt32();//顺着version第8个字节往后再读4个字节，即FlowUnits：4( 4代表的流量单位详见附录)
            int numsubcatch = br.ReadInt32();//顺着flowunits第12个字节往后再读4个字节，即NumSubcatch：3

            br.Close();
            return numsubcatch;




        }
        //Done—获取num_node
        [WebMethod]
        public int Get_SwmmNnode()
        {
            string outputFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.out";


            BinaryReader br = new BinaryReader(new FileStream(outputFilePath, FileMode.Open));

            br.BaseStream.Position = 0;//从0 — IDStartPos

            // 1. Before IDStartPos：这些信息不是很重要，因为完全可以从.rpt文件读取，这里读取了全部
            int magicnumber = br.ReadInt32();//ReadInt32()读取文件前四个字节，即MagicNumber：516114522
            int version = br.ReadInt32();//顺着magicnumber第4个字节往后再读4个字节，即Version：51013
            int flowunits = br.ReadInt32();//顺着version第8个字节往后再读4个字节，即FlowUnits：4( 4代表的流量单位详见附录)
            int numsubcatch = br.ReadInt32();//顺着flowunits第12个字节往后再读4个字节，即NumSubcatch：3
            int numnodes = br.ReadInt32();//顺着numsubcatch第16个字节往后再读4个字节，即NumNodes：5

            br.Close();
            return numnodes;





        }
        //Done—获取num_link
        [WebMethod]
        public int Get_SwmmNlink()
        {
            string outputFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.out";



            BinaryReader br = new BinaryReader(new FileStream(outputFilePath, FileMode.Open));

            br.BaseStream.Position = 0;//从0 — IDStartPos

            // 1. Before IDStartPos：这些信息不是很重要，因为完全可以从.rpt文件读取，这里读取了全部
            int magicnumber = br.ReadInt32();//ReadInt32()读取文件前四个字节，即MagicNumber：516114522
            int version = br.ReadInt32();//顺着magicnumber第4个字节往后再读4个字节，即Version：51013
            int flowunits = br.ReadInt32();//顺着version第8个字节往后再读4个字节，即FlowUnits：4( 4代表的流量单位详见附录)
            int numsubcatch = br.ReadInt32();//顺着flowunits第12个字节往后再读4个字节，即NumSubcatch：3
            int numnodes = br.ReadInt32();//顺着numsubcatch第16个字节往后再读4个字节，即NumNodes：5
            int numlinks = br.ReadInt32();//顺着numnodes第20个字节往后再读4个字节，即NumLinks：4

            br.Close();
            return numlinks;




        }
        //Done—修改.inp
        [WebMethod]
        public bool FixInpFiles(string title, string id, string type, double parameters)
        {
            string inpputFilePath = "D:\\SWMM_x\\Resource\\sourcecode4\\VS2017-DLL\\Release\\test.inp";
            FileStream fs = new FileStream(inpputFilePath, FileMode.Open, FileAccess.Read);
            StreamReader read = new StreamReader(fs, Encoding.Default);
            string strReadline;
            List<string> list1 = new List<string>();
            while ((strReadline = read.ReadLine()) != null)
            {

                list1.Add(strReadline);



            }
            // Console.WriteLine(list1[5]);
            //6008个
            for (int i = 0; i < 6008; i++)
            {

                if (title == "[SUBCATCHMENTS]" && type == "width")
                {

                    if (list1[i] == title)
                    {

                        for (int j = i + 4; j < 348; j++)
                        {

                            if (list1[j].Contains(id))
                            {

                                //Console.WriteLine(list1[j]);
                                string[] mm = Regex.Split(list1[j], "\\s+", RegexOptions.IgnoreCase);
                                //Console.WriteLine(mm[5]);
                                mm[5] = Convert.ToString(parameters);
                                //Console.WriteLine(mm[5]);
                                string mms = string.Join("            ", mm);
                                //Console.WriteLine(mms);
                                list1[j] = mms;
                                //Console.WriteLine(list1[j]);
                                fs.Close();
                                File.WriteAllLines(inpputFilePath, list1);

                            }

                        }


                    }

                }





            }



            return true;
        }
        //Doing—测试用1.0版—个性化定制DLL（继续研究SWMM源码）
        [WebMethod]
        public int Test_custom()
        {

            int c = test_custom(5);
            return c;


        }
        //Doing—测试用2.0版—个性化定制DLL（继续研究SWMM源码）
        [WebMethod]
        public int Test_secondary()
        {

            int a = test_secondary();
            return a;

        }
       






    }
}

