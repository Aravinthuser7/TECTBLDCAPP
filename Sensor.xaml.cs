//using BLDC_Demo.Controls;
//using BLDC_Demo.Models;
//using System.Windows;
//using System.Windows.Controls;

//namespace BLDC_Demo
//{
//    public partial class Sensor : UserControl
//    {
//        // Modbus register addresses for sensor configuration on the ESP32 side.
//        private const ushort LeftSensor1NpnRegister = 0x0061;  // sensor type: 1 = NPN, 0 = PNP
//        private const ushort LeftSensor1NcRegister = 0x0062;   // contact state: 1 = NC, 0 = NO
//        private const ushort RightSensor1NpnRegister = 0x0063; // sensor type: 1 = NPN, 0 = PNP
//        private const ushort RightSensor1NcRegister = 0x0064;  // contact state: 1 = NC, 0 = NO

//        // TODO: confirm these register addresses — placeholders continuing the sequence.
//        private const ushort LeftSensor2NpnRegister = 0x0065;
//        private const ushort LeftSensor2NcRegister = 0x0066;
//        private const ushort RightSensor2NpnRegister = 0x0067;
//        private const ushort RightSensor2NcRegister = 0x0068;

//        // Set by MainWindow after construction, same pattern as CommMotorSelect.BLLogic.
//        public BL BLLogic { get; set; }

//        public Sensor()
//        {
//            InitializeComponent();
//        }

//        // ══════════════════ LEFT SENSOR 1 ══════════════════
//        private void SaveLeftSensor1Button_Click(object sender, RoutedEventArgs e)
//        {
//            string sensorType = LeftSensor1NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
//            string contactState = LeftSensor1NoRadioButton.IsChecked is true ? "NO" : "NC";

//            BLLogic?.SendWriteCommand(LeftSensor1NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
//            BLLogic?.SendWriteCommand(LeftSensor1NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
//            System.Diagnostics.Debug.WriteLine("Left Sensor 1 NPN and NC command sent");
//        }

//        // ══════════════════ LEFT SENSOR 2 ══════════════════
//        private void SaveLeftSensor2Button_Click(object sender, RoutedEventArgs e)
//        {
//            string sensorType = LeftSensor2NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
//            string contactState = LeftSensor2NoRadioButton.IsChecked is true ? "NO" : "NC";

//            BLLogic?.SendWriteCommand(LeftSensor2NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
//            BLLogic?.SendWriteCommand(LeftSensor2NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
//            System.Diagnostics.Debug.WriteLine("Left Sensor 2 NPN and NC command sent");
//        }

//        // ══════════════════ RIGHT SENSOR 1 ══════════════════
//        private void SaveRightSensor1Button_Click(object sender, RoutedEventArgs e)
//        {
//            string sensorType = RightSensor1NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
//            string contactState = RightSensor1NoRadioButton.IsChecked is true ? "NO" : "NC";

//            BLLogic?.SendWriteCommand(RightSensor1NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
//            BLLogic?.SendWriteCommand(RightSensor1NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
//            System.Diagnostics.Debug.WriteLine("Right Sensor 1 NPN and NC command sent");
//        }

//        // ══════════════════ RIGHT SENSOR 2 ══════════════════
//        private void SaveRightSensor2Button_Click(object sender, RoutedEventArgs e)
//        {
//            string sensorType = RightSensor2NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
//            string contactState = RightSensor2NoRadioButton.IsChecked is true ? "NO" : "NC";

//            BLLogic?.SendWriteCommand(RightSensor2NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
//            BLLogic?.SendWriteCommand(RightSensor2NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
//            System.Diagnostics.Debug.WriteLine("Right Sensor 2 NPN and NC command sent");
//        }
//    }
//}
using BLDC_Demo.Controls;
using BLDC_Demo.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace BLDC_Demo
{
    public partial class Sensor : UserControl
    {
        // Modbus register addresses for sensor configuration on the ESP32 side.
        private const ushort LeftSensor1NpnRegister = 0x0061;  // sensor type: 1 = NPN, 0 = PNP
        private const ushort LeftSensor1NcRegister = 0x0062;   // contact state: 1 = NC, 0 = NO
        private const ushort RightSensor1NpnRegister = 0x0063; // sensor type: 1 = NPN, 0 = PNP
        private const ushort RightSensor1NcRegister = 0x0064;  // contact state: 1 = NC, 0 = NO

        // TODO: confirm these register addresses — placeholders continuing the sequence.
        private const ushort LeftSensor2NpnRegister = 0x0065;
        private const ushort LeftSensor2NcRegister = 0x0066;
        private const ushort RightSensor2NpnRegister = 0x0067;
        private const ushort RightSensor2NcRegister = 0x0068;

        // Set by MainWindow after construction, same pattern as CommMotorSelect.BLLogic.
        public BL BLLogic { get; set; }

        // Keeps the Save buttons in sync with the connection state
        private DispatcherTimer _stateTimer;

        // The radio buttons show what the CARD reports (echo of 0x0061-0x0064). A side the user
        // has changed but not saved yet is left alone, and for 2 s after SET/SAVE the old echo is
        // ignored so the box keeps showing what was just set until the card confirms it.
        private bool _dirtyLeft, _dirtyRight;
        private DateTime _holdLeft = DateTime.MinValue, _holdRight = DateTime.MinValue;

        public Sensor()
        {
            InitializeComponent();

            Loaded += Sensor_Loaded;
            Unloaded += Sensor_Unloaded;
            AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler(OnAnyClick));
        }

        // ══════════════════ CONNECTION STATE ══════════════════
        private void Sensor_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateButtonState();
            SyncFromCard();

            if (_stateTimer == null)
            {
                _stateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
                _stateTimer.Tick += (s, args) => { UpdateButtonState(); SyncFromCard(); };
            }
            _stateTimer.Start();
        }

        private void Sensor_Unloaded(object sender, RoutedEventArgs e)
        {
            _stateTimer?.Stop();
            _dirtyLeft = _dirtyRight = false;   // leaving the page: show the card's values again next time
        }

        // Uses this control's BLLogic if MainWindow set it, otherwise MainWindow's own.
        private BL ActiveBL =>
            BLLogic ?? (Application.Current.MainWindow as MainWindow)?.BLLogic;

        // A radio button the user clicked = unsaved change on that side.
        private void OnAnyClick(object sender, RoutedEventArgs e)
        {
            if (!(e.OriginalSource is RadioButton rb) || rb.Name == null) return;
            if (rb.Name.StartsWith("LeftSensor1")) _dirtyLeft = true;
            else if (rb.Name.StartsWith("RightSensor1")) _dirtyRight = true;
        }

        private void SyncFromCard()
        {
            BL bl = ActiveBL;
            if (bl == null || !bl.IsConnected) return;
            ModbusDevice d = bl.CurrentDevice;
            if (d == null) return;

            if (!_dirtyLeft && DateTime.UtcNow >= _holdLeft)
                ShowSensor(d.LeftSensorNpn, d.LeftSensorNc,
                           LeftSensor1NpnRadioButton, LeftSensor1PnpRadioButton,
                           LeftSensor1NoRadioButton, LeftSensor1NcRadioButton);

            if (!_dirtyRight && DateTime.UtcNow >= _holdRight)
                ShowSensor(d.RightSensorNpn, d.RightSensorNc,
                           RightSensor1NpnRadioButton, RightSensor1PnpRadioButton,
                           RightSensor1NoRadioButton, RightSensor1NcRadioButton);
        }

        // npn / nc: 1 or 0 as reported by the card, -1 = nothing received yet (leave as is).
        private static void ShowSensor(int npn, int nc, RadioButton npnRb, RadioButton pnpRb,
                                       RadioButton noRb, RadioButton ncRb)
        {
            if (npn == 0 || npn == 1) (npn == 1 ? npnRb : pnpRb).IsChecked = true;
            if (nc == 0 || nc == 1) (nc == 1 ? ncRb : noRb).IsChecked = true;
        }

        private bool IsDeviceConnected()
        {
            var bl = ActiveBL;
            return bl != null && bl.IsConnected;
        }

        // Save buttons are enabled only while connected to an IP.
        // Every Button in this control (the four Save buttons) is found through the
        // visual tree, so no x:Name is needed.
        private void UpdateButtonState()
        {
            bool connected = IsDeviceConnected();

            foreach (Button b in FindVisualChildren<Button>(this))
                b.IsEnabled = connected;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is T match)
                    yield return match;

                foreach (T nested in FindVisualChildren<T>(child))
                    yield return nested;
            }
        }

        // ══════════════════ LEFT SENSOR 1 ══════════════════
        private void SaveLeftSensor1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected()) { UpdateButtonState(); return; }

            string sensorType = LeftSensor1NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
            string contactState = LeftSensor1NoRadioButton.IsChecked is true ? "NO" : "NC";

            BLLogic?.SendWriteCommand(LeftSensor1NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
            BLLogic?.SendWriteCommand(LeftSensor1NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
            System.Diagnostics.Debug.WriteLine("Left Sensor 1 NPN and NC command sent");
            _dirtyLeft = false; _holdLeft = DateTime.UtcNow.AddSeconds(2);
        }

        // ══════════════════ LEFT SENSOR 2 ══════════════════
        private void SaveLeftSensor2Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected()) { UpdateButtonState(); return; }

            string sensorType = LeftSensor2NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
            string contactState = LeftSensor2NoRadioButton.IsChecked is true ? "NO" : "NC";

            BLLogic?.SendWriteCommand(LeftSensor2NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
            BLLogic?.SendWriteCommand(LeftSensor2NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
            System.Diagnostics.Debug.WriteLine("Left Sensor 2 NPN and NC command sent");
        }

        // ══════════════════ RIGHT SENSOR 1 ══════════════════
        private void SaveRightSensor1Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected()) { UpdateButtonState(); return; }

            string sensorType = RightSensor1NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
            string contactState = RightSensor1NoRadioButton.IsChecked is true ? "NO" : "NC";

            BLLogic?.SendWriteCommand(RightSensor1NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
            BLLogic?.SendWriteCommand(RightSensor1NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
            System.Diagnostics.Debug.WriteLine("Right Sensor 1 NPN and NC command sent");
            _dirtyRight = false; _holdRight = DateTime.UtcNow.AddSeconds(2);
        }

        // ══════════════════ RIGHT SENSOR 2 ══════════════════
        private void SaveRightSensor2Button_Click(object sender, RoutedEventArgs e)
        {
            if (!IsDeviceConnected()) { UpdateButtonState(); return; }

            string sensorType = RightSensor2NpnRadioButton.IsChecked is true ? "NPN" : "PNP";
            string contactState = RightSensor2NoRadioButton.IsChecked is true ? "NO" : "NC";

            BLLogic?.SendWriteCommand(RightSensor2NpnRegister, sensorType is "NPN" ? (ushort)1 : (ushort)0);
            BLLogic?.SendWriteCommand(RightSensor2NcRegister, contactState is "NC" ? (ushort)1 : (ushort)0);
            System.Diagnostics.Debug.WriteLine("Right Sensor 2 NPN and NC command sent");
        }
    }
}