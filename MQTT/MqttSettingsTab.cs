using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication.MQTT
{
    public partial class MqttSettingsTab : UserControl
    {
        private ListBox _rulesList;
        private List<MqttRule> _rules;

        public MqttSettingsTab()
        {
            InitializeComponent();
            _rules = new List<MqttRule>();
        }

        private void InitializeComponent()
        {
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(15);

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // Заголовок
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // Список правил
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Кнопки

            // === ЗАГОЛОВОК ===
            var header = new Label
            {
                Text = "📡 MQTT Правила публикации",
                Font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // === СПИСОК ПРАВИЛ ===
            _rulesList = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font(FontFamily.GenericMonospace, 9),
                HorizontalScrollbar = true,
                BorderStyle = BorderStyle.FixedSingle
            };
            _rulesList.DoubleClick += RulesList_DoubleClick;

            // === КНОПКИ ===
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 10, 0, 0)
            };

            var btnAdd = new Button { Text = "➕ Добавить правило", Width = 160, Height = 30 };
            btnAdd.Click += BtnAdd_Click;

            var btnEdit = new Button { Text = "✏️ Редактировать", Width = 130, Height = 30 };
            btnEdit.Click += BtnEdit_Click;

            var btnDelete = new Button { Text = "🗑️ Удалить", Width = 100, Height = 30 };
            btnDelete.Click += BtnDelete_Click;

            var btnCopy = new Button { Text = "📋 Копировать", Width = 120, Height = 30 };
            btnCopy.Click += BtnCopy_Click;

            var btnPresets = new Button { Text = "📦 Готовые пресеты", Width = 160, Height = 30 };
            btnPresets.Click += BtnPresets_Click;

            var btnExport = new Button { Text = "📤 Экспорт", Width = 100, Height = 30 };
            var btnImport = new Button { Text = "📥 Импорт", Width = 100, Height = 30 };

            buttonPanel.Controls.AddRange(new Control[] {
                btnAdd, btnEdit, btnDelete, btnCopy, btnPresets, btnExport, btnImport
            });

            // === СБОРКА ===
            mainLayout.Controls.Add(header, 0, 0);
            mainLayout.Controls.Add(_rulesList, 0, 1);
            mainLayout.Controls.Add(buttonPanel, 0, 2);

            this.Controls.Add(mainLayout);
        }

        public void LoadRules(List<MqttRule> rules)
        {
            _rules = rules ?? new List<MqttRule>();
            RefreshList();
        }

        public List<MqttRule> GetRules()
        {
            return _rules;
        }

        private void RefreshList()
        {
            _rulesList.Items.Clear();
            foreach (var rule in _rules)
            {
                var status = rule.Enabled ? "🟢" : "⚫";
                var eventIcon = GetEventIcon(rule.EventType);
                var item = $"{status} {eventIcon} {rule.Name}";
                _rulesList.Items.Add(item);
            }
        }

        private string GetEventIcon(MqttEventType eventType)
        {
            return eventType switch
            {
                MqttEventType.Motion => "🏃",
                MqttEventType.Detection => "🤖",
                MqttEventType.RecordingStart => "🔴",
                MqttEventType.RecordingStop => "⏹️",
                MqttEventType.CameraOnline => "✅",
                MqttEventType.CameraOffline => "❌",
                MqttEventType.Alert => "🚨",
                MqttEventType.Snapshot => "📸",
                MqttEventType.Error => "⚠️",
                _ => "📡"
            };
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var editor = new MqttRuleEditor();
            if (editor.ShowDialog() == DialogResult.OK)
            {
                _rules.Add(editor.Rule);
                RefreshList();
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_rulesList.SelectedIndex == -1) return;

            var rule = _rules[_rulesList.SelectedIndex];
            var editor = new MqttRuleEditor(rule);
            if (editor.ShowDialog() == DialogResult.OK)
            {
                _rules[_rulesList.SelectedIndex] = editor.Rule;
                RefreshList();
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_rulesList.SelectedIndex == -1) return;

            if (MessageBox.Show("Удалить выбранное правило?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _rules.RemoveAt(_rulesList.SelectedIndex);
                RefreshList();
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (_rulesList.SelectedIndex == -1) return;

            var original = _rules[_rulesList.SelectedIndex];
            var copy = new MqttRule
            {
                Name = original.Name + " (копия)",
                EventType = original.EventType,
                Topic = original.Topic,
                CameraIds = new List<string>(original.CameraIds),
                ObjectFilter = new List<string>(original.ObjectFilter),
                MinConfidence = original.MinConfidence,
                IncludeJson = original.IncludeJson,
                IncludeSnapshot = original.IncludeSnapshot,
                SnapshotAsUrl = original.SnapshotAsUrl,
                SnapshotQuality = original.SnapshotQuality,
                SnapshotWidth = original.SnapshotWidth,
                SnapshotHeight = original.SnapshotHeight,
                Retain = original.Retain,
                QoS = original.QoS,
                IntervalSeconds = original.IntervalSeconds,
                Enabled = original.Enabled
            };

            _rules.Add(copy);
            RefreshList();
        }

        private void BtnPresets_Click(object sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add("Home Assistant: Статус камер", null, (s, ev) => AddPreset("ha_status"));
            menu.Items.Add("SHomeDART: Детекция объектов", null, (s, ev) => AddPreset("shome_detect"));
            menu.Items.Add("Node-RED: Движение", null, (s, ev) => AddPreset("nodered_motion"));
            menu.Items.Add("Универсальный JSON со снапшотом", null, (s, ev) => AddPreset("universal_json"));
            menu.Items.Add("-");
            menu.Items.Add("Все события для Home Assistant", null, (s, ev) => AddAllPresets());

            var btn = (Button)sender;
            menu.Show(btn, new Point(0, btn.Height));
        }

        private void AddPreset(string presetId)
        {
            var rule = presetId switch
            {
                "ha_status" => new MqttRule
                {
                    Name = "Home Assistant: Статус камер",
                    EventType = MqttEventType.CameraOnline,
                    Topic = "homeassistant/binary_sensor/ispy_{camera_name}/state",
                    Retain = true,
                    IncludeJson = false
                },
                "shome_detect" => new MqttRule
                {
                    Name = "SHomeDART: Детекция",
                    EventType = MqttEventType.Detection,
                    Topic = "ispy/detect/{camera_id}",
                    IncludeJson = true,
                    IncludeSnapshot = true,
                    SnapshotAsUrl = false,
                    SnapshotWidth = 320,
                    SnapshotHeight = 240
                },
                "nodered_motion" => new MqttRule
                {
                    Name = "Node-RED: Движение",
                    EventType = MqttEventType.Motion,
                    Topic = "ispy/motion/{camera_id}",
                    IncludeJson = false
                },
                "universal_json" => new MqttRule
                {
                    Name = "Универсальный JSON",
                    EventType = MqttEventType.Detection,
                    Topic = "ispy/events/{camera_id}",
                    IncludeJson = true,
                    IncludeSnapshot = true,
                    SnapshotAsUrl = true
                },
                _ => new MqttRule()
            };

            _rules.Add(rule);
            RefreshList();
        }

        private void AddAllPresets()
        {
            _rules.Add(new MqttRule { Name = "HA: Статус камер", EventType = MqttEventType.CameraOnline, Topic = "homeassistant/binary_sensor/ispy_{camera_name}/state", Retain = true });
            _rules.Add(new MqttRule { Name = "HA: Движение", EventType = MqttEventType.Motion, Topic = "homeassistant/binary_sensor/ispy_{camera_name}_motion/state" });
            _rules.Add(new MqttRule { Name = "HA: Запись", EventType = MqttEventType.RecordingStart, Topic = "homeassistant/binary_sensor/ispy_{camera_name}_recording/state" });
            _rules.Add(new MqttRule { Name = "HA: Детекция YOLO", EventType = MqttEventType.Detection, Topic = "ispy/detect/{camera_id}", IncludeJson = true });
            RefreshList();
        }

        private void RulesList_DoubleClick(object sender, EventArgs e)
        {
            BtnEdit_Click(sender, e);
        }
    }
}