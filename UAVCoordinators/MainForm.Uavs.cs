using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using GMap.NET;
using static UAVCoordinators.Utils;

namespace UAVCoordinators
{
    public partial class MainForm : Form
    {
        private object[] GetUavInfo(Uav uav)
        {
            PointLatLng pos;
            float heading;
            int currWp;

            try
            {
                string[] uavInfo = RunPythonProc(@"Apps\CurrentState.py", new string[] { uav.ConnectionType, uav.Ip, uav.Port }).Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                pos = new PointLatLng(ParseDouble(uavInfo[0]), ParseDouble(uavInfo[1]));
                heading = ParseFloat(uavInfo[4]);
                currWp = int.Parse(uavInfo[5]);
            }
            catch (Exception)
            {
                throw new Exception();
            }

            return new object[] { pos, heading, currWp };
        }

        private void LoadMission(Uav uav)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileName = fileDialog.FileName;

                // Generate mission code randomly:
                var rnd = new Random();
                var code = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 16).Select(s => s[rnd.Next(s.Length)]).ToArray());

                LoadMission(uav, fileName, code);
            }
        }

        internal void LoadMission(Uav uav, string fileName, string code)
        {
            string[] lines = File.ReadAllLines(fileName);
            List<PointLatLng> wps = new List<PointLatLng>();
            for (int i = 1; i < lines.Length; i++)
            {
                string[] line = lines[i].Split('\t');
                wps.Add(new PointLatLng(ParseDouble(line[8]), ParseDouble(line[9])));
            }
            uav.WaypointsLL = wps;
            uav.MissionFileName = fileName;
            uav.MissionCode = code;

            Log(RunPythonProc(@"Apps\UploadMission.py", new string[] { uav.ConnectionType, uav.Ip, uav.Port, fileName }));
        }

        internal void SendMissionToSup(Uav uav)
        {
            foreach (var i in Connections)
            {
                if (!(i is Coordinator)) continue;
                (i as Coordinator).SendMission(uav);
            }
        }

        internal void SendMissionStatusToSup(Uav uav)
        {
            foreach (var i in Connections)
            {
                if (!(i is Coordinator)) continue;
                (i as Coordinator).SendMissionStatus(uav);
            }
        }

        private void PauseResumeStartMission(Uav uav, System.Windows.Forms.Button btn)
        {
            string[] args = new string[] { uav.ConnectionType, uav.Ip, uav.Port };
            string app = "";
            int newStatus = 0;
            switch (uav.MissionStatus)
            {
                case 0:
                    app = "StartMission.py"; // not sure
                    newStatus = 1;
                    break;
                case 1:
                    app = "PauseMission.py";
                    newStatus = 2;
                    break;
                case 2:
                    app = "ResumeMission.py";
                    newStatus = 1;
                    break;
            }

            Log(RunPythonProc(@"Apps\" + app, new string[] { uav.ConnectionType, uav.Ip, uav.Port }));
            // check if everything is ok
            uav.MissionStatus = newStatus;
            btn.Text = newStatus == 1 ? "Pause mission" : "Resume mission";
        }

        private void TakeOff(Uav uav)
        {
            Log(RunPythonProc(@"Apps\ArmTakeoffAuto.py", new string[] { uav.ConnectionType, uav.Ip, uav.Port }));
        }

        private void ReturnToHome(Uav uav)
        {
            Log(RunPythonProc(@"Apps\ReturnToHome.py", new string[] { uav.ConnectionType, uav.Ip, uav.Port }));
        }
    }
}
