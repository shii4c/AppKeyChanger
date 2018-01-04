using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace AppKeyChanger
{
    public class KeyChangeTable
    {
        private KeyOperation[] keyOperationTable_ = new KeyOperation[512];

        public KeyChangeTable(string filePath)
        {
            Regex regex = new Regex(@"^(\d+),([01])\t+(\d+),(\d+),([01])$");
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("#")) { continue; }
                    Match m = regex.Match(line);
                    if (m.Success)
                    {
                        int index = GetTableIndex(int.Parse(m.Groups[1].Value), m.Groups[2].Value == "1");
                        KeyOperation keyOpe = new KeyOperation();
                        keyOpe.VkCode = (byte)int.Parse(m.Groups[3].Value);
                        keyOpe.ScanCode = (byte)int.Parse(m.Groups[4].Value);
                        keyOpe.ShiftPressed = m.Groups[5].Value == "1";
                        if (keyOperationTable_[index] != null)
                        {
                            System.Diagnostics.Debug.WriteLine("??? " + index);
                            System.Windows.Forms.MessageBox.Show("??? " + index);
                        }
                        keyOperationTable_[index] = keyOpe;
                    }
                }
            }
        }

        public KeyOperation GetKeyOperation(int vkCode, bool shiftPressed)
        {
            return keyOperationTable_[GetTableIndex(vkCode, shiftPressed)];
        }

        private int GetTableIndex(int vkCode, bool shiftPressed)
        {
            return vkCode | ((shiftPressed ? 256 : 0));
        }
    }

    public class KeyOperation
    {
        public byte VkCode { get; set; }
        public byte ScanCode { get; set; }
        public bool ShiftPressed { get; set; }
    }
}
