using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace AppKeyChanger
{
    public partial class MainAndSettingForm : Form
    {
        private KeyboardHook kbdHook_;
        private IntPtr foregroundHwnd_;
        private string foregroundProcessName_ = "";
        private string foregroundWinText_ = "";
        private bool isShiftPressed_ = false;
        private int pressedShiftVkCode_;
        private int pressedShiftScanCode_;
        private KeyChangeTable keyChangeTable_;
        private bool isCloseRequested_ = false;
        private Regex regexProcessName_;
        private Regex regexWindowText_;
        private string appliedProcessName_;
        private string appliedWindowText_;

        public MainAndSettingForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string appFile = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string appPath = System.IO.Directory.GetParent(appFile).FullName;
            keyChangeTable_ = new KeyChangeTable(appPath + "\\keychange.tbl");

            Apply();
            
            kbdHook_ = new KeyboardHook();
            kbdHook_.LowLevelKeyboardEvent += KbdHook__LowLevelKeyboardEvent;
        }

        private bool Apply()
        {
            try
            {
                Regex regexProcessName = txtProcessName.Text == "" ? null : new Regex(txtProcessName.Text);
                Regex regexWindowText = txtWindowText.Text == "" ? null : new Regex(txtWindowText.Text);
                regexProcessName_ = regexProcessName;
                regexWindowText_ = regexWindowText;
                appliedProcessName_ = txtProcessName.Text;
                appliedWindowText_ = txtWindowText.Text;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
            return true;
        }

        private int KbdHook__LowLevelKeyboardEvent(int nCode, int wParam, ref KeyboardHook.KBDLLHOOKSTRUCT kbdHookInfo, ref bool isCancel)
        {
            // shift key が押されたり離されたりした場合...
            if (kbdHookInfo.vkCode == 160 || kbdHookInfo.vkCode == 161)
            {
                // ※shift キーに関する状態を変更（左右の shift が同時に押された場合への対処はしていない...）
                isShiftPressed_ = (kbdHookInfo.flags & 128) == 0;
                pressedShiftScanCode_ = kbdHookInfo.scanCode;
                pressedShiftVkCode_ = kbdHookInfo.vkCode;
                return 0;
            }

            // 物理キーボード以外からの入力は無視する
            // (無視しないと、このアプリから送信したものまで再処理してしまうので)
            if ((kbdHookInfo.flags & 16) != 0) { return 0; }

            if (foregroundHwnd_ == this.Handle) { return 0; }
            if (regexProcessName_ != null && !regexProcessName_.IsMatch(foregroundProcessName_)) { return 0; }
            if (regexWindowText_ != null && !regexWindowText_.IsMatch(foregroundWinText_)) { return 0; }

            System.Diagnostics.Debug.WriteLine("!!");
            // 変換処理
            bool isPress = (kbdHookInfo.flags & 128) == 0;
            KeyOperation keyOpe = keyChangeTable_.GetKeyOperation(kbdHookInfo.vkCode, isShiftPressed_);
            if (keyOpe != null)
            {
                SendKey(keyOpe.VkCode, keyOpe.ScanCode, isPress, keyOpe.ShiftPressed);
                isCancel = true;
            }

            return 0;
        }

        // キー操作の送信
        // ※ SendInput を使ったほうが安全なような気がするけど、現時点では keybd_event を使用して実装
        private void SendKey(int vk, int scanCode, bool isPress, bool shiftState)
        {
            bool isShiftPressed = isShiftPressed_;
            int pressedShiftVkCode = pressedShiftVkCode_;
            int pressedShiftScanCode = pressedShiftScanCode_;

            // --------------------------------------------------------------
            // shift key 前処理
            // --------------------------------------------------------------
            if (shiftState && !isShiftPressed)
            {
                // shift キーが押されていない状態のときに、shift キーを押した状態にしたい場合
                WinAPI.keybd_event(160, 42, 0, 0);
                pressedShiftVkCode = 160;
                pressedShiftScanCode = 42;
            }
            else if (!shiftState && isShiftPressed_)
            {
                // shift キーが押された状態のときに、shift キーを押していない状態にしたい場合
                WinAPI.keybd_event((byte)pressedShiftVkCode, (byte)pressedShiftScanCode, WinAPI.KEYEVENTF_KEYUP, 0);
            }

            // --------------------------------------------------------------
            // キー操作を送る
            // --------------------------------------------------------------
            WinAPI.keybd_event((byte)vk, (byte)scanCode, isPress ? 0 : WinAPI.KEYEVENTF_KEYUP, 0);

            // --------------------------------------------------------------
            // shift key あと処理
            // --------------------------------------------------------------
            if (shiftState && !isShiftPressed)
            {
                // shift キーが押されていない状態のときに、shift キーを押した状態にしたい場合
                WinAPI.keybd_event((byte)pressedShiftVkCode, (byte)pressedShiftScanCode, WinAPI.KEYEVENTF_KEYUP, 0);
            }
            else if (!shiftState && isShiftPressed)
            {
                // shift キーが押された状態のときに、shift キーを押していない状態にしたい場合
                // でやったことを元に戻す
                WinAPI.keybd_event((byte)pressedShiftVkCode, (byte)pressedShiftScanCode, 0, 0);
            }
        }

        #region UI イベントハンドラー

        private void timer1_Tick(object sender, EventArgs e)
        {
            IntPtr hWnd = WinAPI.GetForegroundWindow();
            if (hWnd == this.Handle)
            {
                foregroundHwnd_ = hWnd;
                return;
            }
            WinAPI.GetWindowText(hWnd, sb, sb.Capacity);
            foregroundWinText_ = sb.ToString();
            txtFgWindowText.Text = foregroundWinText_;
            if (hWnd == foregroundHwnd_) { return; }
            foregroundHwnd_ = hWnd;

            uint pid;
            WinAPI.GetWindowThreadProcessId(hWnd, out pid);
            IntPtr hProcess = WinAPI.OpenProcess(WinAPI.ProcessAccessFlags.All, false, pid);
            if (hProcess != IntPtr.Zero)
            {
                WinAPI.GetModuleBaseName(hProcess, IntPtr.Zero, sb, sb.Capacity);
                foregroundProcessName_ = sb.ToString();
            }
            else
            {
                foregroundProcessName_ = "";
            }
            WinAPI.CloseHandle(hProcess);

            txtFgProcessName.Text = foregroundProcessName_;
        }
        private StringBuilder sb = new StringBuilder(1024);

        private void settingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Visible = true;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            isCloseRequested_ = true;
            Close();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !isCloseRequested_)
            {
                Visible = false;
                e.Cancel = true;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (Apply())
            {
                Visible = false;
            }
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            Apply();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            txtProcessName.Text = appliedProcessName_;
            txtWindowText.Text = appliedWindowText_;
        }

        #endregion
    }
}
