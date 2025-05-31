﻿using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NetDoor
{
    internal class UniversalStatic
    {

        public const string acx_Disconnect = "Disconnected";
        public const string acx_Connect = "Connected";

        public static bool ValidateIP(string addrString)
        {
            IPAddress address;
            if (IPAddress.TryParse(addrString, out address))
                return true;
            else
                return false;
        }
        public static void DrawLineInFooter(Control control, Color color, int thickness)
        {
            int y = control.Height;
            DrawLine(control, color, 0, y, control.Width, y, thickness);
        }
        public static void DrawLine(Control control, Color color, int x, int y, int x1, int y1, int thickness)
        {
            Graphics graphicsObj = control.CreateGraphics();
            graphicsObj.DrawLine(new Pen(color, thickness), x, y, x1, y1);
        }

        public static bool PingTheDevice(string ipAdd)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(ipAdd);

                Ping pingSender = new Ping();
                PingOptions options = new PingOptions();
                options.DontFragment = true;

                // Create a buffer of 32 bytes of data to be transmitted. 
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 120;
                PingReply reply = pingSender.Send(ipAddress, timeout, buffer, options);

                if (reply.Status == IPStatus.Success)
                    return true;
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string IntegerValidation(char[] enteredString, string actualString)
        {
            foreach (char c in enteredString.AsEnumerable())
            {

                if (Char.IsDigit(c))
                { actualString = actualString + c; }
                else
                {
                    actualString.Replace(c, ' ');
                    actualString.Trim();
                }
            }
            return actualString;
        }

        public static void ValidateInteger(TextBox tbx)
        {
            string actualString = string.Empty;
            char[] enteredString = tbx.Text.ToCharArray();
            tbx.Text = IntegerValidation(enteredString, actualString);
            tbx.Select(tbx.Text.Length, 0);
        }
    }
}
