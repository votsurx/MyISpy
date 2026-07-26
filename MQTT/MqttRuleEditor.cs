using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication.MQTT
{
    public partial class MqttRuleEditor : Form
    {
        private MqttRule _rule;
        private bool _loaded;

        // Список всех классов YOLO для фильтра
        private static readonly string[] AllYoloClasses = new[]
        {
            "person", "bicycle", "car", "motorcycle", "airplane", "bus", "train", "truck",
            "boat", "traffic light", "fire hydrant", "stop sign", "parking meter", "bench",
            "bird", "cat", "dog", "horse", "sheep", "cow", "elephant", "bear", "zebra",
            "giraffe", "backpack", "umbrella", "handbag", "tie", "suitcase", "frisbee",
            "skis", "snowboard", "sports ball", "kite", "baseball bat", "baseball glove",
            "skateboard", "surfboard", "tennis racket", "bottle", "wine glass", "cup",
            "fork", "knife", "spoon", "bowl", "banana", "apple", "sandwich", "orange",
            "broccoli", "carrot", "hot dog", "pizza", "donut", "cake", "chair", "couch",
            "potted plant", "bed", "dining table", "toilet", "tv", "laptop", "mouse",
            "remote", "keyboard", "cell phone", "microwave", "oven", "toaster", "sink",
            "refrigerator", "book", "clock", "vase", "scissors", "teddy bear", "hair drier",
            "toothbrush"
        };

        public MqttRule Rule
        {
            get => _rule;
            set
            {
                _rule = value;
                if (_loaded) LoadRuleToUI();
            }
        }

        public MqttRuleEditor(MqttRule rule = null)
        {
            InitializeComponent();
            _rule = rule ?? new MqttRule();
            InitializeCustomComponents();
            _loaded = true;
            LoadRuleToUI();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактор правила MQTT";
            this.Size = new Size(600, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
        }

        private void InitializeCustomComponents()
        {
            var panel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int y = 10;
            int leftMargin = 15;
            int controlWidth = 550;

            // === НАЗВАНИЕ ПРАВИЛА ===
            var lblName = new Label { Text = "Название правила:", Left = leftMargin, Top = y, Width = 200 };
            var txtName = new TextBox { Left = leftMargin, Top = y + 25, Width = controlWidth, Name = "txtName" };
            y += 55;

            // === ТИП СОБЫТИЯ ===
            var lblEvent = new Label { Text = "Тип события:", Left = leftMargin, Top = y, Width = 200 };
            var cmbEvent = new ComboBox { Left = leftMargin, Top = y + 25, Width = controlWidth, Name = "cmbEvent", DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEvent.Items.AddRange(new object[] {
                "Движение (Motion)",
                "Обнаружение YOLO (Detection)",
                "Начало записи (RecordingStart)",
                "Остановка записи (RecordingStop)",
                "Камера онлайн (CameraOnline)",
                "Камера оффлайн (CameraOffline)",
                "Тревога (Alert)",
                "Периодический снапшот (Snapshot)",
                "Ошибка (Error)"
            });
            y += 55;

            // === ТОПИК ===
            var lblTopic = new Label { Text = "Топик (можно использовать переменные):", Left = leftMargin, Top = y, Width = controlWidth };
            var txtTopic = new TextBox { Left = leftMargin, Top = y + 25, Width = controlWidth, Name = "txtTopic", Text = "ispy/{event_type}/{camera_name}" };
            var lblTopicPreview = new Label { Left = leftMargin, Top = y + 50, Width = controlWidth, ForeColor = Color.Gray, Name = "lblPreview", Text = "Превью: ispy/detection/Двор" };
            y += 75;

            // === ФИЛЬТР КАМЕР ===
            var gbCameras = new GroupBox { Text = "Камеры", Left = leftMargin, Top = y, Width = controlWidth, Height = 80 };
            var chkAllCameras = new CheckBox { Text = "Все камеры", Left = 10, Top = 20, Width = 200, Name = "chkAllCameras", Checked = true };
            var btnSelectCameras = new Button { Text = "Выбрать камеры...", Left = 10, Top = 45, Width = 150, Name = "btnSelectCameras", Enabled = false };
            var lblSelectedCameras = new Label { Left = 170, Top = 48, Width = 350, Name = "lblSelectedCameras", Text = "Все камеры", ForeColor = Color.Gray };
            gbCameras.Controls.AddRange(new Control[] { chkAllCameras, btnSelectCameras, lblSelectedCameras });
            y += 90;

            // === ФИЛЬТР ОБЪЕКТОВ (только для Detection) ===
            var gbObjects = new GroupBox { Text = "Фильтр объектов YOLO", Left = leftMargin, Top = y, Width = controlWidth, Height = 80, Name = "gbObjects" };
            var chkAllObjects = new CheckBox { Text = "Все объекты", Left = 10, Top = 20, Width = 200, Name = "chkAllObjects", Checked = true };
            var btnSelectObjects = new Button { Text = "Выбрать объекты...", Left = 10, Top = 45, Width = 150, Name = "btnSelectObjects", Enabled = false };
            var lblSelectedObjects = new Label { Left = 170, Top = 48, Width = 350, Name = "lblSelectedObjects", Text = "Все объекты", ForeColor = Color.Gray };
            gbObjects.Controls.AddRange(new Control[] { chkAllObjects, btnSelectObjects, lblSelectedObjects });
            y += 90;

            // === МИН. УВЕРЕННОСТЬ ===
            var lblConf = new Label { Text = "Минимальная уверенность:", Left = leftMargin, Top = y, Width = 200 };
            var numConf = new NumericUpDown { Left = leftMargin, Top = y + 25, Width = 100, Name = "numConf", Minimum = 0.1m, Maximum = 1.0m, DecimalPlaces = 2, Increment = 0.05m, Value = 0.5m };
            var lblConfPct = new Label { Left = leftMargin + 110, Top = y + 28, Text = "%", Width = 30 };
            y += 55;

            // === ЧТО ОТПРАВЛЯТЬ ===
            var gbPayload = new GroupBox { Text = "Содержимое сообщения", Left = leftMargin, Top = y, Width = controlWidth, Height = 100 };
            var chkJson = new CheckBox { Text = "JSON с данными", Left = 10, Top = 20, Width = 200, Name = "chkJson", Checked = true };
            var chkSnapshot = new CheckBox { Text = "Снапшот", Left = 10, Top = 45, Width = 200, Name = "chkSnapshot" };
            var rbUrl = new RadioButton { Text = "URL", Left = 220, Top = 43, Width = 100, Name = "rbUrl", Checked = true };
            var rbBase64 = new RadioButton { Text = "Base64", Left = 320, Top = 43, Width = 100, Name = "rbBase64" };
            var lblQuality = new Label { Text = "Качество:", Left = 10, Top = 72, Width = 60 };
            var numQuality = new NumericUpDown { Left = 75, Top = 70, Width = 60, Name = "numQuality", Minimum = 10, Maximum = 100, Value = 70 };
            var lblSize = new Label { Text = "Размер:", Left = 150, Top = 72, Width = 50 };
            var numWidth = new NumericUpDown { Left = 200, Top = 70, Width = 60, Name = "numWidth", Minimum = 160, Maximum = 1920, Value = 320 };
            var lblX = new Label { Text = "x", Left = 265, Top = 72, Width = 15 };
            var numHeight = new NumericUpDown { Left = 280, Top = 70, Width = 60, Name = "numHeight", Minimum = 120, Maximum = 1080, Value = 240 };
            gbPayload.Controls.AddRange(new Control[] { chkJson, chkSnapshot, rbUrl, rbBase64, lblQuality, numQuality, lblSize, numWidth, lblX, numHeight });
            y += 110;
                        // === MQTT ФЛАГИ ===
            var gbMqtt = new GroupBox { Text = "MQTT параметры", Left = leftMargin, Top = y, Width = controlWidth, Height = 80 };
            var chkRetain = new CheckBox { Text = "Retained сообщение", Left = 10, Top = 20, Width = 200, Name = "chkRetain" };
            var lblQoS = new Label { Text = "QoS:", Left = 10, Top = 48, Width = 40 };
            var cmbQoS = new ComboBox { Left = 55, Top = 45, Width = 150, Name = "cmbQoS", DropDownStyle = ComboBoxStyle.DropDownList };
            cmbQoS.Items.AddRange(new object[] { "0 - Доставка не гарантирована", "1 - Минимум один раз", "2 - Ровно один раз" });
            cmbQoS.SelectedIndex = 1;
            gbMqtt.Controls.AddRange(new Control[] { chkRetain, lblQoS, cmbQoS });
            y += 90;

            // === ИНТЕРВАЛ (для периодических) ===
            var gbInterval = new GroupBox { Text = "Интервал (для периодических событий)", Left = leftMargin, Top = y, Width = controlWidth, Height = 55, Name = "gbInterval", Visible = false };
            var lblInterval = new Label { Text = "Каждые", Left = 10, Top = 22, Width = 50 };
            var numInterval = new NumericUpDown { Left = 60, Top = 20, Width = 60, Name = "numInterval", Minimum = 1, Maximum = 3600, Value = 30 };
            var lblIntervalSec = new Label { Text = "секунд", Left = 125, Top = 22, Width = 50 };
            gbInterval.Controls.AddRange(new Control[] { lblInterval, numInterval, lblIntervalSec });
            y += 65;

            // === АКТИВНОСТЬ ===
            var chkEnabled = new CheckBox { Text = "Правило активно", Left = leftMargin, Top = y, Width = 200, Name = "chkEnabled", Checked = true, Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold) };
            y += 35;

            // === КНОПКИ ===
            var btnSave = new Button { Text = "💾 Сохранить", Left = leftMargin, Top = y, Width = 130, Height = 35, Name = "btnSave" };
            var btnCancel = new Button { Text = "Отмена", Left = leftMargin + 140, Top = y, Width = 100, Height = 35, Name = "btnCancel" };
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            panel.Controls.AddRange(new Control[] {
                lblName, txtName,
                lblEvent, cmbEvent,
                lblTopic, txtTopic, lblTopicPreview,
                gbCameras,
                gbObjects,
                lblConf, numConf, lblConfPct,
                gbPayload,
                gbMqtt,
                gbInterval,
                chkEnabled,
                btnSave, btnCancel
            });

            this.Controls.Add(panel);

            // Привязка событий
            txtTopic.TextChanged += (s, e) => UpdateTopicPreview();
            cmbEvent.SelectedIndexChanged += (s, e) => OnEventTypeChanged();
            chkAllCameras.CheckedChanged += (s, e) => btnSelectCameras.Enabled = !chkAllCameras.Checked;
            chkAllObjects.CheckedChanged += (s, e) => btnSelectObjects.Enabled = !chkAllObjects.Checked;
            chkSnapshot.CheckedChanged += (s, e) => {
                rbUrl.Enabled = rbBase64.Enabled = numQuality.Enabled =
                numWidth.Enabled = numHeight.Enabled = chkSnapshot.Checked;
            };
        }

        private void UpdateTopicPreview()
        {
            var txtTopic = (TextBox)Controls.Find("txtTopic", true)[0];
            var lblPreview = (Label)Controls.Find("lblPreview", true)[0];

            var preview = txtTopic.Text
                .Replace("{camera_id}", "3")
                .Replace("{camera_name}", "Двор")
                .Replace("{event_type}", "detection")
                .Replace("{object_class}", "person")
                .Replace("{rule_id}", "abc123")
                .Replace("{timestamp}", DateTime.Now.ToString("yyyyMMdd-HHmmss"));

            lblPreview.Text = $"Превью: {preview}";
        }

        private void OnEventTypeChanged()
        {
            var cmbEvent = (ComboBox)Controls.Find("cmbEvent", true)[0];
            var gbObjects = (GroupBox)Controls.Find("gbObjects", true)[0];
            var gbInterval = (GroupBox)Controls.Find("gbInterval", true)[0];

            bool isDetection = cmbEvent.SelectedIndex == 1; // Detection
            bool isPeriodic = cmbEvent.SelectedIndex == 7; // Snapshot

            gbObjects.Visible = isDetection;
            gbInterval.Visible = isPeriodic;

            // Авто-подстановка топика
            var txtTopic = (TextBox)Controls.Find("txtTopic", true)[0];
            if (string.IsNullOrEmpty(txtTopic.Text) || txtTopic.Text == "ispy/{event_type}/{camera_name}")
            {
                txtTopic.Text = cmbEvent.SelectedIndex switch
                {
                    0 => "ispy/motion/{camera_name}",           // Motion
                    1 => "ispy/detect/{camera_id}",              // Detection
                    2 => "ispy/record/{camera_name}/start",      // RecordingStart
                    3 => "ispy/record/{camera_name}/stop",       // RecordingStop
                    4 => "ispy/status/{camera_name}",            // CameraOnline
                    5 => "ispy/status/{camera_name}",            // CameraOffline
                    6 => "ispy/alert/{camera_name}",             // Alert
                    7 => "ispy/snapshot/{camera_name}",          // Snapshot
                    8 => "ispy/error/{camera_name}",             // Error
                    _ => "ispy/{event_type}/{camera_name}"
                };
            }
            UpdateTopicPreview();
        }

        private void LoadRuleToUI()
        {
            var txtName = (TextBox)Controls.Find("txtName", true)[0];
            var cmbEvent = (ComboBox)Controls.Find("cmbEvent", true)[0];
            var txtTopic = (TextBox)Controls.Find("txtTopic", true)[0];
            var chkAllCameras = (CheckBox)Controls.Find("chkAllCameras", true)[0];
            var lblSelectedCameras = (Label)Controls.Find("lblSelectedCameras", true)[0];
            var chkAllObjects = (CheckBox)Controls.Find("chkAllObjects", true)[0];
            var lblSelectedObjects = (Label)Controls.Find("lblSelectedObjects", true)[0];
            var numConf = (NumericUpDown)Controls.Find("numConf", true)[0];
            var chkJson = (CheckBox)Controls.Find("chkJson", true)[0];
            var chkSnapshot = (CheckBox)Controls.Find("chkSnapshot", true)[0];
            var rbUrl = (RadioButton)Controls.Find("rbUrl", true)[0];
            var rbBase64 = (RadioButton)Controls.Find("rbBase64", true)[0];
            var numQuality = (NumericUpDown)Controls.Find("numQuality", true)[0];
            var numWidth = (NumericUpDown)Controls.Find("numWidth", true)[0];
            var numHeight = (NumericUpDown)Controls.Find("numHeight", true)[0];
            var chkRetain = (CheckBox)Controls.Find("chkRetain", true)[0];
            var cmbQoS = (ComboBox)Controls.Find("cmbQoS", true)[0];
            var numInterval = (NumericUpDown)Controls.Find("numInterval", true)[0];
            var chkEnabled = (CheckBox)Controls.Find("chkEnabled", true)[0];

            txtName.Text = _rule.Name;
            cmbEvent.SelectedIndex = (int)_rule.EventType;
            txtTopic.Text = _rule.Topic;

            chkAllCameras.Checked = _rule.CameraIds.Count == 0;
            lblSelectedCameras.Text = chkAllCameras.Checked ? "Все камеры" : $"Выбрано камер: {_rule.CameraIds.Count}";

            chkAllObjects.Checked = _rule.ObjectFilter.Count == 0;
            lblSelectedObjects.Text = chkAllObjects.Checked ? "Все объекты" : string.Join(", ", _rule.ObjectFilter);

            numConf.Value = (decimal)_rule.MinConfidence;
            chkJson.Checked = _rule.IncludeJson;
            chkSnapshot.Checked = _rule.IncludeSnapshot;
            rbUrl.Checked = _rule.SnapshotAsUrl;
            rbBase64.Checked = !_rule.SnapshotAsUrl;
            numQuality.Value = _rule.SnapshotQuality;
            numWidth.Value = _rule.SnapshotWidth;
            numHeight.Value = _rule.SnapshotHeight;
            chkRetain.Checked = _rule.Retain;
            cmbQoS.SelectedIndex = (int)_rule.QoS;
            numInterval.Value = _rule.IntervalSeconds;
            chkEnabled.Checked = _rule.Enabled;

            OnEventTypeChanged();
            UpdateTopicPreview();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var txtName = (TextBox)Controls.Find("txtName", true)[0];
            var cmbEvent = (ComboBox)Controls.Find("cmbEvent", true)[0];
            var txtTopic = (TextBox)Controls.Find("txtTopic", true)[0];
            var chkAllCameras = (CheckBox)Controls.Find("chkAllCameras", true)[0];
            var chkAllObjects = (CheckBox)Controls.Find("chkAllObjects", true)[0];
            var numConf = (NumericUpDown)Controls.Find("numConf", true)[0];
            var chkJson = (CheckBox)Controls.Find("chkJson", true)[0];
            var chkSnapshot = (CheckBox)Controls.Find("chkSnapshot", true)[0];
            var rbUrl = (RadioButton)Controls.Find("rbUrl", true)[0];
            var numQuality = (NumericUpDown)Controls.Find("numQuality", true)[0];
            var numWidth = (NumericUpDown)Controls.Find("numWidth", true)[0];
            var numHeight = (NumericUpDown)Controls.Find("numHeight", true)[0];
            var chkRetain = (CheckBox)Controls.Find("chkRetain", true)[0];
            var cmbQoS = (ComboBox)Controls.Find("cmbQoS", true)[0];
            var numInterval = (NumericUpDown)Controls.Find("numInterval", true)[0];
            var chkEnabled = (CheckBox)Controls.Find("chkEnabled", true)[0];

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название правила!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _rule.Name = txtName.Text;
            _rule.EventType = (MqttEventType)cmbEvent.SelectedIndex;
            _rule.Topic = txtTopic.Text;

            if (!chkAllCameras.Checked)
            {
                // TODO: открыть форму выбора камер
            }

            if (!chkAllObjects.Checked)
            {
                // TODO: открыть форму выбора объектов
            }

            _rule.MinConfidence = (float)numConf.Value;
            _rule.IncludeJson = chkJson.Checked;
            _rule.IncludeSnapshot = chkSnapshot.Checked;
            _rule.SnapshotAsUrl = rbUrl.Checked;
            _rule.SnapshotQuality = (int)numQuality.Value;
            _rule.SnapshotWidth = (int)numWidth.Value;
            _rule.SnapshotHeight = (int)numHeight.Value;
            _rule.Retain = chkRetain.Checked;
            _rule.QoS = (MqttQoS)cmbQoS.SelectedIndex;
            _rule.IntervalSeconds = (int)numInterval.Value;
            _rule.Enabled = chkEnabled.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}