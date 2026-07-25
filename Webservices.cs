using System;
using System.Globalization;
using System.Net;
using System.Windows.Forms;
using iSpyApplication.Utilities;

namespace iSpyApplication
{
    public partial class Webservices : Form
    {
        public static string NL = Environment.NewLine;
        public string EmailAddress = "";
        public string MobileNumber = "";
        public bool SupportsUpnp;
        private bool _loaded;


        public Webservices()
        {
            InitializeComponent();
            RenderResources();
        }

        private void LinkLabel1LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.StartBrowser(MainForm.Webserver+"/newuser.aspx");
        }

        private void WebservicesLoad(object sender, EventArgs e)
        {
            txtWANPort.Value = MainForm.Conf.ServerPort;
            txtUsername.Text = MainForm.Conf.WSUsername;
            txtPassword.Text = MainForm.Conf.WSPassword;
            txtLANPort.Text = MainForm.Conf.LANPort.ToString(CultureInfo.InvariantCulture);
            txtPort.Text = MainForm.Conf.LANPort.ToString(CultureInfo.InvariantCulture);
            chkReroute.Checked = MainForm.Conf.DHCPReroute;
            chkEnableIPv6.Checked = !MainForm.Conf.IPv6Disabled;
            chkBindSpecific.Checked = MainForm.Conf.SpecificIP;

            chkuPNP.Checked = MainForm.Conf.UseUPNP;
            if (!chkuPNP.Checked)
                chkReroute.Checked = chkReroute.Enabled = false;

            int i2 = 0;
            foreach (IPAddress ipadd in MainForm.AddressListIPv4)
            {
                lbIPv4Address.Items.Add(ipadd.ToString());
                if (ipadd.ToString() == MainForm.AddressIPv4)
                    lbIPv4Address.SelectedIndex = i2;
                i2++;
            }
            if (lbIPv4Address.Items.Count > 0 && lbIPv4Address.SelectedIndex == -1)
                lbIPv4Address.SelectedIndex = 0;
            
            int i = 0;
            foreach (IPAddress ipadd in MainForm.AddressListIPv6)
            {
                lbIPv6Address.Items.Add(ipadd.ToString());
                if (ipadd.ToString() == MainForm.AddressIPv6)
                    lbIPv6Address.SelectedIndex = i;

                i++;
            }

            if (i==0)
                tcIPMode.TabPages.RemoveAt(1);


            if (tcIPMode.TabPages.Count == 2)
            {
                switch (MainForm.Conf.IPMode)
                {
                    case "IPv4":
                        tcIPMode.SelectedIndex = 0;
                        break;
                    case "IPv6":
                        tcIPMode.SelectedIndex = 1;
                        break;
                }
            }
            else
            {
                tcIPMode.SelectedIndex = 0;
                MainForm.Conf.IPMode = "IPv4";
            }
            EnableNext();
            _loaded = true;
        }

        private void RenderResources()
        {
            Text = LocRm.GetString("WebAccess");
            button2.Text = LocRm.GetString("Cancel");
            chkReroute.Text = LocRm.GetString("DhcpReroute");
            chkuPNP.Text = LocRm.GetString("AutoConfigureWithUpnp");
            label1.Text = LocRm.GetString("Username");
            label10.Text = LocRm.GetString("LanPort");
            label2.Text = LocRm.GetString("Password");
            label3.Text = LocRm.GetString("WanPort");
            label4.Text = LocRm.GetString("toViewYourRecordedAndLive");
            label5.Text = LocRm.GetString("ifYouAreConnectingMultipl");
            label6.Text = LocRm.GetString("toAccessYourCamerasMicrop");
            label7.Text = LocRm.GetString("usingIpv6IspymightBeAbleT");
            label8.Text = LocRm.GetString("Port");
            linkLabel1.Text = LocRm.GetString("CreateANewAccount");
            linkLabel2.Text = LocRm.GetString("OrManuallyConfigureYourRo");
            Next.Text = LocRm.GetString("Finish");
            tabPage1.Text = LocRm.GetString("Upnpipv4");
            tabPage2.Text = LocRm.GetString("Tunnelingipv6");

            toolTip1.SetToolTip(label3, LocRm.GetString("ToolTip_AccessPortExternal"));
            toolTip1.SetToolTip(label10, LocRm.GetString("ToolTip_AccessPortInternal"));
            toolTip1.SetToolTip(lbIPv4Address, LocRm.GetString("ToolTip_SelectIP"));
            toolTip1.SetToolTip(chkReroute, LocRm.GetString("ToolTip_LANIPMonitor"));

            Text = LocRm.GetString("WebServerSettings");
            btnTroubleshooting.Text = LocRm.GetString("troubleshooting");
            llblHelp.Text = LocRm.GetString("help");
            LocRm.SetString(chkEnableIPv6, "EnableIPv6");
        }


        private void Webservices_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void label4_Click(object sender, EventArgs e)
        {
        }

        private void Button2Click(object sender, EventArgs e)
        {
            Close();
        }

        private void label6_Click(object sender, EventArgs e)
        {
        }

        private void label5_Click(object sender, EventArgs e)
        {
        }

        private void LinkLabel2LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.StartBrowser(MainForm.Website+"/userguide-connecting.aspx#3");
        }

        private void ChkuPnpCheckedChanged(object sender, EventArgs e)
        {
            MainForm.Conf.UseUPNP = chkuPNP.Checked;
            chkReroute.Checked = chkReroute.Enabled = chkuPNP.Checked;
        }

        private void lbIPv4Address_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void llblHelp_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl( MainForm.Website+"/userguide-connecting.aspx");
        }

        private void tcIPMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableNext();

        }

        private void EnableNext()
        {
            switch (tcIPMode.SelectedIndex)
            {
                case 0:
                    Next.Enabled = btnTroubleshooting.Enabled = lbIPv4Address.SelectedIndex != -1;
                    break;
                case 1:
                    Next.Enabled = btnTroubleshooting.Enabled = lbIPv6Address.SelectedIndex != -1;
                    break;
            }
        }

        private void chkEnableIPv6_CheckedChanged(object sender, EventArgs e)
        {
            if (_loaded)
            {
                if (chkEnableIPv6.Checked)
                {
                    MessageBox.Show(this,
                                    LocRm.GetString("IPv6Issues")+Environment.NewLine+LocRm.GetString("IPv6Warning"), LocRm.GetString("Warning"));
                }
                MainForm.Conf.IPv6Disabled = !chkEnableIPv6.Checked;
            }
        }

    }
}