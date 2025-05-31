using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Runtime.InteropServices; // Windows API erişimi için
namespace NetDoor
{
    public partial class Master : Form
    {
        // Windows API Hotkey fonksiyonları
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Hotkey ID (herhangi bir numara olabilir, tek olması yeterli)
        private const int HOTKEY_ID = 1;
        private const uint MOD_NONE = 0x0000; // Modifiye tuş kullanmıyoruz (Ctrl, Alt vs. olmadan)
        private const uint VK_F10 = 0x79; // F10 tuşu


        DeviceManipulator manipulator = new DeviceManipulator();
        public ZkemClient objZkeeper;
        private bool isDeviceConnected = false;
        private bool isAdminAuthenticated = false;

        public bool IsDeviceConnected
        {
            get { return isDeviceConnected; }
            set
            {
                isDeviceConnected = value;
                if (isDeviceConnected)
                {
                    ShowStatusBar("The device is connected !!", true);
                    btnConnect.Text = "Disconnect";
                    ToggleControls(true);
                }
                else
                {
                    ShowStatusBar("The device is diconnected !!", true);
                    objZkeeper.Disconnect();
                    btnConnect.Text = "Connect";
                    ToggleControls(false);
                }
            }
        }
        private void ToggleControls(bool value)
        {
            btnBeep.Enabled = value;
            btnRestartDevice.Enabled = false;
            btnLogout.Enabled = false;
            tbxDeviceIP.ReadOnly = !isAdminAuthenticated;
            tbxPort.ReadOnly = !isAdminAuthenticated;
            tbxMachineNumber.ReadOnly = !isAdminAuthenticated;
        }
        public Master()
        {
            InitializeComponent();
            ToggleControls(false);
            ShowStatusBar(string.Empty, true);
        }

        private void RaiseDeviceEvent(object sender, string actionType)
        {
            switch (actionType)
            {
                case UniversalStatic.acx_Disconnect:
                    {
                        ShowStatusBar("The device is switched off", true);
                        btnConnect.Text = "Connect";
                        ToggleControls(false);
                        break;
                    }

                default:
                    break;
            }
        }
        private async void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                this.Cursor = Cursors.WaitCursor;
                ShowStatusBar(string.Empty, true);

                lblConnecting.Visible = true; // Kullanıcıyı bilgilendir

                if (IsDeviceConnected)
                {
                    IsDeviceConnected = false;
                    this.Cursor = Cursors.Default;
                    lblConnecting.Visible = false; // Bağlantı kapatıldığında gizle
                    return;
                }

                string ipAddress = tbxDeviceIP.Text.Trim();
                string port = tbxPort.Text.Trim();
                if (ipAddress == string.Empty || port == string.Empty)
                    throw new Exception("The Device IP Address and Port is mandotory !!");

                int portNumber = 4370;
                if (!int.TryParse(port, out portNumber))
                    throw new Exception("Not a valid port number");

                bool isValidIpA = UniversalStatic.ValidateIP(ipAddress);
                if (!isValidIpA)
                    throw new Exception("The Device IP is invalid !!");

                isValidIpA = UniversalStatic.PingTheDevice(ipAddress);
                if (!isValidIpA)
                    throw new Exception("The device at " + ipAddress + ":" + port + " did not respond!!");

                objZkeeper = new ZkemClient(RaiseDeviceEvent);
                // Bağlantıyı asenkron olarak başlat
                await Task.Delay(500); // Küçük bir gecikme ver, böylece yazı daha net görünür.
                IsDeviceConnected = objZkeeper.Connect_Net(ipAddress, portNumber);

                if (IsDeviceConnected)
                {
                    string deviceInfo = manipulator.FetchDeviceInfo(objZkeeper, int.Parse(tbxMachineNumber.Text.Trim()));
                    lblDeviceInfo.Text = deviceInfo;
                    if (isAdminAuthenticated)
                    {
                        btnLogout.Enabled = true;
                        btnRestartDevice.Enabled = true;
                    }
                }
                
            }
            catch (Exception ex)
            {
                ShowStatusBar(ex.Message, false);
            }
            lblConnecting.Visible = false; // Bağlantı tamamlandığında veya başarısız olduğunda gizle
            this.Cursor = Cursors.Default;

        }
        public void ShowStatusBar(string message, bool type)
        {
            if (message.Trim() == string.Empty)
            {
                lblStatus.Visible = false;
                return;
            }

            lblStatus.Visible = true;
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.White;

            if (type)
                lblStatus.BackColor = Color.FromArgb(79, 208, 154);
            else
                lblStatus.BackColor = Color.FromArgb(230, 112, 134);
        }
        private void btnPingDevice_Click(object sender, EventArgs e)
        {
            ShowStatusBar(string.Empty, true);

            string ipAddress = tbxDeviceIP.Text.Trim();

            bool isValidIpA = UniversalStatic.ValidateIP(ipAddress);
            if (!isValidIpA)
                throw new Exception("The Device IP is invalid !!");

            isValidIpA = UniversalStatic.PingTheDevice(ipAddress);
            if (isValidIpA)
                ShowStatusBar("The device is active", true);
            else
                ShowStatusBar("Could not read any response", false);
        }
        private void pnlHeader_Paint(object sender, PaintEventArgs e)
        { UniversalStatic.DrawLineInFooter(pnlHeader, Color.FromArgb(204, 204, 204), 2); }

        private void btnRestartDevice_Click(object sender, EventArgs e)
        {

            DialogResult rslt = MessageBox.Show("Do you wish to restart the device now ??", "Restart Device", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (rslt == DialogResult.Yes)
            {
                if (objZkeeper.RestartDevice(int.Parse(tbxMachineNumber.Text.Trim())))
                    ShowStatusBar("The device is being restarted, Please wait...", true);
                else
                    ShowStatusBar("Operation failed,please try again", false);
                ShowStatusBar("The device is diconnected !!", true);
                objZkeeper.Disconnect();
                btnConnect.Text = "Connect";
                ToggleControls(false);
                btnLogout.Enabled = true;
            }
        }
        private void tbxPort_TextChanged(object sender, EventArgs e)
        { UniversalStatic.ValidateInteger(tbxPort); }

        private void tbxMachineNumber_TextChanged(object sender, EventArgs e)
        { UniversalStatic.ValidateInteger(tbxMachineNumber); }

        private async void btnBeep_Click(object sender, EventArgs e)
        {
            bool result = objZkeeper.ACUnlock(1, 3); // 3 saniye açık tut
            bool result2 = objZkeeper.PlayVoiceByIndex(3); // Geçici Beep Sesi (AO!)
            label4Status.Text = $"Kapı: {(result ? "Açıldı" : "Açılamadı")} | Buzzer: {(result2 ? "Çaldı" : "Çalamadı")}";
            label4Status.ForeColor = (result && result2) ? Color.Green : Color.Red;
            await Task.Delay(3000);
            label4Status.Text = "";
        }

        private void btnAdmin_Click(object sender, EventArgs e)
        {
            string storedHashedPassword = "$2a$11$tERSnjCFE.oMVkMBg.vIt.w.w8EguFEqh.ClPJVxQrvd2F3rntQw6"; // bcrypt hash

            using (var passwordForm = new Form())
            {
                passwordForm.Text = "Admin Girişi";
                passwordForm.Size = new Size(300, 150);
                passwordForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                passwordForm.StartPosition = FormStartPosition.CenterParent;

                Label lbl = new Label() { Left = 10, Top = 10, Text = "Şifre:", AutoSize = true };
                TextBox txtPassword = new TextBox() { Left = 10, Top = 30, Width = 260, PasswordChar = '*' };
                Button btnOk = new Button() { Text = "Giriş", Left = 100, Width = 80, Top = 60, DialogResult = DialogResult.OK };

                passwordForm.Controls.Add(lbl);
                passwordForm.Controls.Add(txtPassword);
                passwordForm.Controls.Add(btnOk);

                passwordForm.AcceptButton = btnOk;

                if (passwordForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (BCrypt.Net.BCrypt.Verify(txtPassword.Text, storedHashedPassword)) // Şifreyi doğrula
                        {
                            isAdminAuthenticated = true;
                            tbxDeviceIP.ReadOnly = false;
                            tbxPort.ReadOnly = false;
                            tbxMachineNumber.ReadOnly = false;
                            btnLogout.Enabled = true; // Çıkış butonu aktif hale getirildi
                            if (isDeviceConnected)
                            {
                                btnRestartDevice.Enabled = true; // Cihaz bağlıysa restart butonunu aktif hale getir
                            }

                            // Admin butonu güncellemesi
                            btnAdmin.Text = "Admin ✓";
                            btnAdmin.BackColor = Color.Green;
                            btnAdmin.ForeColor = Color.White;

                            MessageBox.Show("Admin girişi başarılı!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Hatalı şifre!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Şifre doğrulama sırasında bir hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            isAdminAuthenticated = false;

            // Ancak IP bilgisi korunsun
            string currentIP = tbxDeviceIP.Text;

            tbxDeviceIP.ReadOnly = true;
            tbxPort.ReadOnly = true;
            tbxMachineNumber.ReadOnly = true;
            btnRestartDevice.Enabled = false;
            btnLogout.Enabled = false;

            // Admin butonunu eski haline getir
            btnAdmin.Text = "Admin";
            btnAdmin.BackColor = SystemColors.Control;
            btnAdmin.ForeColor = SystemColors.ControlText;

            // IP adresini geri yükle
            tbxDeviceIP.Text = currentIP;

            MessageBox.Show("Admin oturumu kapatıldı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void Master_Load(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Properties.Settings.Default.SavedIP))
            {
                tbxDeviceIP.Text = Properties.Settings.Default.SavedIP;
            }
            // Bağlanma sürecini başlat
            lblConnecting.Visible = true;
            await Task.Delay(1000); // 1 saniye bekleyerek UI güncellenmesini sağla
            btnConnect.PerformClick();

            // F10 tuşunu global kısayol olarak kaydet
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_NONE, VK_F10);

        }

        private void tbxDeviceIP_TextChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SavedIP = tbxDeviceIP.Text.Trim();
            Properties.Settings.Default.Save();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                // F10'a basıldığında kapıyı açma butonunu çalıştır
                btnBeep_Click(null, null);
            }
        }
        private void Master_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Uygulama kapanırken hotkey'i temizle
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }
    }
}
