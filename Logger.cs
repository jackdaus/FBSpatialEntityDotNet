using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StereoKit;
using StereoKit.Framework;

namespace ARInventory
{
    public class Logger : IStepper
    {
        public bool Enabled { get; set; }


        private Pose windowPose = new Pose(-0.1f, -0.15f, -0.4f, Quat.LookAt(new Vec3(-0.1f, -0.15f, -0.4f), Input.Head.position, Vec3.UnitY));
        private Vec2 windowSize = new Vec2(0.3f);
        private List<string> logList = new List<string>();
        private string logText;

        public bool Initialize()
        {
            Log.Filter = LogLevel.None;
            Log.Subscribe(onLog);
            return true;
        }

        public void Shutdown()
        {
        }

        public void Step()
        {
            UI.WindowBegin("Log", ref windowPose, windowSize);
            UI.Text(logText);
            UI.WindowEnd();
        }

        private void onLog(LogLevel level, string text)
        {
            logList.Add(text);
            
            logText = string.Join("", logList.Reverse<string>().Take(6).Reverse());
        }
    }
}
