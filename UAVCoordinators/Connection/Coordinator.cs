using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using GMap.NET;
using System.Threading;
using System;
using static UAVCoordinators.Utils;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.IO;

namespace UAVCoordinators
{
    internal class Coordinator : Connection
    {
        private Thread ConnectionThread;
        private bool _connected = false;
        public bool Connected
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get { return _connected; }
        }

        public Coordinator(string ip, string port, string name, MainForm coordinatorsForm)
        {
            ConnectionType = "tcp";
            Ip = ip;
            Port = port;
            Name = name;
            CoordinatorForm = coordinatorsForm;
        }

        // Wait messages from the coordinator and obey:
        private void Listen()
        {
            foreach (var i in CoordinatorForm.Connections)
            {
                if (!(i is Uav)) continue;
                SendMission(i as Uav);
                SendMissionStatus(i as Uav);
            }

            while (Connected)
            {
                string[] info = ReceiveMsg().Split(';');

                if (info[0] == "Mission")
                {
                    var uavInfo = info[1].Split(',');
                    var uav = GetUav(uavInfo);
                    var missionCode = info[2];
                    var fileName = Path.GetDirectoryName(Application.ExecutablePath) + @"\Missions\" + missionCode + uavInfo[0];
                    File.WriteAllText(fileName, info[3]);
                    CoordinatorForm.Invoke((MethodInvoker)delegate {
                        CoordinatorForm.LoadMission(uav, fileName, missionCode);
                    });
                }
                else if (info[0] == "Mission status")
                {
                    var uav = GetUav(info[1].Split(','));
                    var missionCode = info[2];
                    var missionStatus = int.Parse(info[3]);
                    if (uav.MissionCode == missionCode) uav.MissionStatus = missionStatus;
                }
            }
        }

        // Connect with the coordinator:
        public void Connect()
        {
            const string rejectAlert = "Supervision rejected";

            if (!CoordinatorForm.AreaDefined) throw new Exception("You must define the area of operation first");

            string message;

            message = "Supervise me?";
            SendMsg(message);
            message = ReceiveMsg();

            if (message != "I will")
                throw new Exception(rejectAlert);

            message = "";
            foreach (var i in CoordinatorForm.Quadrants)
                message += i.Item1 + ',' + i.Item2 + ';';
            message = message.Remove(message.Length - 1); // Remove the last semicolon

            SendMsg(message);
            message = ReceiveMsg();

            if (message != "Ok")
            {
                if (message == "Forget it") throw new Exception(rejectAlert);

                string[] quads = message.Split(';');
                CoordinatorForm.Quadrants = new List<Tuple<double, double>>();
                foreach (var i in quads)
                {
                    string[] coords = i.Split(',');
                    CoordinatorForm.Quadrants.Add(new Tuple<double, double>(ParseDouble(coords[0]), ParseDouble(coords[1])));
                }
                CoordinatorForm.CompletelyRefreshGrid();
            }

            ConnectionThread = new Thread(new ThreadStart(() => Listen()));
            ConnectionThread.Start();
            _connected = true;
        }

        public void Disconnect()
        {
            SendMsg("Bye, I will quit");

            _connected = false;

            ConnectionThread.Join();
        }

        public void SendMission(Uav uav)
        {
            if (!Connected) return;

            string message = "Mission;";
            message += uav.Name + "," + uav.Ip + "," + uav.Port + "," + uav.ConnectionType + ";";
            message += uav.MissionCode + ";";
            if (uav.MissionFileName != "") message += File.ReadAllText(uav.MissionFileName);
            SendMsg(message);
        }

        public void SendMissionStatus(Uav uav)
        {
            if (!Connected || uav.MissionFileName == "") return;

            string message = "Mission;";
            message += uav.Name + "," + uav.Ip + "," + uav.Port + "," + uav.ConnectionType + ";";
            message += uav.MissionCode + ";";
            message += uav.MissionStatus;
            SendMsg(message);
        }

        private void SendMsg(string s)
        {
            //
        }

        private string ReceiveMsg()
        {
            return "";/////////////
        }

        private Uav GetUav(string[] info)
        {
            return CoordinatorForm.GetConnection(info[0], info[1], info[2], info[3] == "udp" ? 0 : 1) as Uav;
        }
    }
}
