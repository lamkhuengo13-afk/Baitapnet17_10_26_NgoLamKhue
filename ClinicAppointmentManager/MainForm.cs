using ClinicAppointmentManager.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ClinicAppointmentManager
{
    public class MainForm : Form
    {
        private readonly BindingList<Appointment> _appointments = new();
        private readonly BindingList<Appointment> _visibleAppointments = new();
        private readonly BindingSource _bindingSource = new();
        private readonly ErrorProvider _errorProvider = new();

        private TextBox txtAppointmentId = null!;
        private TextBox txtPatientName = null!;
        private TextBox txtPhone = null!;
        private DateTimePicker dtpAppointment = null!;
        private ComboBox cboDoctor = null!;
        private TextBox txtReason = null!;
        private ComboBox cboStatus = null!;
        private TextBox txtSearch = null!;
        private CheckBox chkFilterDate = null!;
        private DateTimePicker dtpFilterDate = null!;
        private ComboBox cboFilterStatus = null!;
        private DataGridView dgvAppointments = null!;
        private ToolStripStatusLabel lblCount = null!;

        private Appointment? _selectedAppointment;

        private static readonly string[] AppointmentStatuses =
        {
            "Chờ khám", "Đang khám", "Hoàn tất", "Đã hủy"
        };

        private static readonly string[] Doctors =
        {
            "BS. Nguyễn Minh Anh", "BS. Trần Quốc Bảo", "BS. Lê Thu Hà",
            "BS. Phạm Hoàng Nam"
        };

        public MainForm()
        {
            Text = "Clinic Appointment Manager";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1050, 680);
            Size = new Size(1280, 800);
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(245, 247, 250);

            _errorProvider.ContainerControl = this;
            _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            ConfigureGrid();
            LoadSampleData();
            RefreshVisibleAppointments();
        }

        private void BuildInterface()
        {
            var menu = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Tệp");
            var exportItem = new ToolStripMenuItem("Xuất danh sách đang hiển thị ra CSV");
            exportItem.Click += (_, _) => ExportCsv();
            var exitItem = new ToolStripMenuItem("Thoát");
            exitItem.Click += (_, _) => Close();
            fileMenu.DropDownItems.Add(exportItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);
            menu.Items.Add(fileMenu);
            MainMenuStrip = menu;

            var statusStrip = new StatusStrip();
            lblCount = new ToolStripStatusLabel("Số lịch hẹn: 0");
            statusStrip.Items.Add(lblCount);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(14),
                BackColor = BackColor
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var header = new Label
            {
                Text = "QUẢN LÝ LỊCH HẸN PHÒNG KHÁM",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 71, 107),
                Padding = new Padding(0, 4, 0, 14)
            };

            var inputGroup = new GroupBox
            {
                Text = "Thông tin lịch hẹn",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var inputLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 4,
                Padding = new Padding(4),
            };
            inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            inputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < 4; i++)
                inputLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            txtAppointmentId = CreateTextBox();
            txtPatientName = CreateTextBox();
            txtPhone = CreateTextBox();
            dtpAppointment = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                ShowUpDown = true,
                Value = DateTime.Now.AddHours(1)
            };
            cboDoctor = CreateComboBox(Doctors);
            txtReason = CreateTextBox();
            cboStatus = CreateComboBox(AppointmentStatuses);
            cboStatus.SelectedItem = "Chờ khám";

            AddField(inputLayout, 0, 0, "Mã lịch hẹn:", txtAppointmentId);
            AddField(inputLayout, 0, 2, "Họ tên bệnh nhân:", txtPatientName);
            AddField(inputLayout, 1, 0, "Số điện thoại:", txtPhone);
            AddField(inputLayout, 1, 2, "Ngày giờ hẹn:", dtpAppointment);
            AddField(inputLayout, 2, 0, "Bác sĩ:", cboDoctor);
            AddField(inputLayout, 2, 2, "Trạng thái:", cboStatus);
            AddField(inputLayout, 3, 0, "Lý do khám:", txtReason, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 8, 0, 0)
            };
            var btnAdd = CreateButton("Thêm lịch hẹn", Color.FromArgb(28, 130, 90));
            btnAdd.Click += (_, _) => AddAppointment();
            var btnUpdate = CreateButton("Cập nhật", Color.FromArgb(41, 105, 160));
            btnUpdate.Click += (_, _) => UpdateAppointment();
            var btnDelete = CreateButton("Xóa lịch hẹn", Color.FromArgb(180, 55, 55));
            btnDelete.Click += (_, _) => DeleteAppointment();
            var btnClear = CreateButton("Làm mới", Color.FromArgb(100, 110, 120));
            btnClear.Click += (_, _) => ClearInputs();
            buttons.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnClear });
            inputLayout.RowCount = 5;
            inputLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inputLayout.Controls.Add(buttons, 1, 4);
            inputLayout.SetColumnSpan(buttons, 3);
            inputGroup.Controls.Add(inputLayout);

            var listGroup = new GroupBox
            {
                Text = "Danh sách lịch hẹn",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            var listLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            listLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            listLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var filterLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                WrapContents = true,
                Padding = new Padding(0, 0, 0, 8)
            };
            txtSearch = new TextBox { Width = 250, PlaceholderText = "Tìm tên bệnh nhân / số điện thoại..." };
            txtSearch.TextChanged += (_, _) => RefreshVisibleAppointments();

            chkFilterDate = new CheckBox { Text = "Lọc ngày:", AutoSize = true, Margin = new Padding(8, 8, 3, 0) };
            dtpFilterDate = new DateTimePicker
            {
                Width = 135,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = DateTime.Today,
                Enabled = false
            };
            chkFilterDate.CheckedChanged += (_, _) =>
            {
                dtpFilterDate.Enabled = chkFilterDate.Checked;
                RefreshVisibleAppointments();
            };
            dtpFilterDate.ValueChanged += (_, _) => RefreshVisibleAppointments();

            cboFilterStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 150
            };
            cboFilterStatus.Items.Add("Tất cả trạng thái");
            cboFilterStatus.Items.AddRange(AppointmentStatuses);
            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged += (_, _) => RefreshVisibleAppointments();

            var btnResetFilter = CreateButton("Xóa bộ lọc", Color.FromArgb(100, 110, 120));
            btnResetFilter.Click += (_, _) =>
            {
                txtSearch.Clear();
                chkFilterDate.Checked = false;
                cboFilterStatus.SelectedIndex = 0;
                RefreshVisibleAppointments();
            };

            filterLayout.Controls.Add(new Label { Text = "Tìm kiếm:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
            filterLayout.Controls.Add(txtSearch);
            filterLayout.Controls.Add(chkFilterDate);
            filterLayout.Controls.Add(dtpFilterDate);
            filterLayout.Controls.Add(new Label { Text = "Trạng thái:", AutoSize = true, Margin = new Padding(8, 8, 4, 0) });
            filterLayout.Controls.Add(cboFilterStatus);
            filterLayout.Controls.Add(btnResetFilter);

            dgvAppointments = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                MultiSelect = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 },
                DataSource = _bindingSource
            };
            dgvAppointments.SelectionChanged += (_, _) => LoadSelectedAppointment();

            listLayout.Controls.Add(filterLayout, 0, 0);
            listLayout.Controls.Add(dgvAppointments, 0, 1);
            listGroup.Controls.Add(listLayout);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(inputGroup, 0, 1);
            root.Controls.Add(listGroup, 0, 2);

            Controls.Add(root);
            Controls.Add(statusStrip);
            Controls.Add(menu);
            MainMenuStrip = menu;
        }

        private static TextBox CreateTextBox() =>
            new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 12, 5) };

        private static ComboBox CreateComboBox(IEnumerable<string> items)
        {
            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3, 5, 12, 5)
            };
            combo.Items.AddRange(items.Cast<object>().ToArray());
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            return combo;
        }

        private static Button CreateButton(string text, Color color) =>
            new Button
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(115, 34),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(3, 4, 8, 4),
                Padding = new Padding(8, 3, 8, 3),
                Cursor = Cursors.Hand
            };

        private static void AddField(TableLayoutPanel layout, int row, int labelColumn, string labelText, Control control, int columnSpan = 1)
        {
            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 9, 3, 3)
            };
            layout.Controls.Add(label, labelColumn, row);
            layout.Controls.Add(control, labelColumn + 1, row);
            if (columnSpan > 1)
                layout.SetColumnSpan(control, columnSpan);
        }

        private void ConfigureGrid()
        {
            dgvAppointments.Columns.Clear();
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colId", HeaderText = "Mã lịch", DataPropertyName = nameof(Appointment.AppointmentId),
                FillWeight = 70
            });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPatient", HeaderText = "Bệnh nhân", DataPropertyName = nameof(Appointment.PatientName),
                FillWeight = 130
            });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPhone", HeaderText = "Số điện thoại", DataPropertyName = nameof(Appointment.PhoneNumber),
                FillWeight = 95
            });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDate", HeaderText = "Ngày giờ hẹn", DataPropertyName = nameof(Appointment.AppointmentDateTime),
                FillWeight = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" }
            });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDoctor", HeaderText = "Bác sĩ", DataPropertyName = nameof(Appointment.Doctor),
                FillWeight = 115
            });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colReason", HeaderText = "Lý do khám", DataPropertyName = nameof(Appointment.Reason),
                FillWeight = 130
            });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStatus", HeaderText = "Trạng thái", DataPropertyName = nameof(Appointment.Status),
                FillWeight = 80
            });
            _bindingSource.DataSource = _visibleAppointments;
        }

        private void LoadSampleData()
        {
            _appointments.Add(new Appointment
            {
                AppointmentId = "LH001", PatientName = "Nguyễn Minh Châu", PhoneNumber = "0912345678",
                AppointmentDateTime = DateTime.Today.AddHours(14), Doctor = Doctors[0],
                Reason = "Khám tổng quát", Status = "Chờ khám"
            });
            _appointments.Add(new Appointment
            {
                AppointmentId = "LH002", PatientName = "Trần Hoàng Long", PhoneNumber = "0987654321",
                AppointmentDateTime = DateTime.Today.AddDays(1).AddHours(9), Doctor = Doctors[1],
                Reason = "Đau đầu", Status = "Đang khám"
            });
            _appointments.Add(new Appointment
            {
                AppointmentId = "LH003", PatientName = "Lê Thu Trang", PhoneNumber = "0901122334",
                AppointmentDateTime = DateTime.Today.AddDays(2).AddHours(10), Doctor = Doctors[2],
                Reason = "Tái khám", Status = "Hoàn tất"
            });
        }

        private void AddAppointment()
        {
            if (!ValidateInputs()) return;

            var appointment = ReadInputs();
            _appointments.Add(appointment);
            RefreshVisibleAppointments(appointment);
            MessageBox.Show("Đã thêm lịch hẹn thành công.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInputs();
        }

        private void UpdateAppointment()
        {
            if (_selectedAppointment == null)
            {
                MessageBox.Show("Vui lòng chọn lịch hẹn cần cập nhật trong bảng.", "Chưa chọn lịch hẹn",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateInputs()) return;

            string newId = txtAppointmentId.Text.Trim();
            if (_appointments.Any(a => !ReferenceEquals(a, _selectedAppointment) &&
                                       a.AppointmentId.Equals(newId, StringComparison.OrdinalIgnoreCase)))
            {
                _errorProvider.SetError(txtAppointmentId, "Mã lịch hẹn đã tồn tại.");
                return;
            }

            _selectedAppointment.AppointmentId = newId;
            _selectedAppointment.PatientName = txtPatientName.Text.Trim();
            _selectedAppointment.PhoneNumber = txtPhone.Text.Trim();
            _selectedAppointment.AppointmentDateTime = dtpAppointment.Value;
            _selectedAppointment.Doctor = cboDoctor.Text;
            _selectedAppointment.Reason = txtReason.Text.Trim();
            _selectedAppointment.Status = cboStatus.Text;

            RefreshVisibleAppointments(_selectedAppointment);
            MessageBox.Show("Đã cập nhật lịch hẹn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteAppointment()
        {
            if (_selectedAppointment == null)
            {
                MessageBox.Show("Vui lòng chọn lịch hẹn cần xóa.", "Chưa chọn lịch hẹn",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Bạn có chắc muốn xóa lịch hẹn {_selectedAppointment.AppointmentId} của {_selectedAppointment.PatientName}?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            _appointments.Remove(_selectedAppointment);
            _selectedAppointment = null;
            ClearInputs();
            RefreshVisibleAppointments();
        }

        private bool ValidateInputs()
        {
            _errorProvider.Clear();
            bool valid = true;

            string id = txtAppointmentId.Text.Trim();
            string name = txtPatientName.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string reason = txtReason.Text.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                _errorProvider.SetError(txtAppointmentId, "Mã lịch hẹn không được để trống.");
                valid = false;
            }
            else if (_appointments.Any(a => a.AppointmentId.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                                            !ReferenceEquals(a, _selectedAppointment)))
            {
                _errorProvider.SetError(txtAppointmentId, "Mã lịch hẹn phải duy nhất.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                _errorProvider.SetError(txtPatientName, "Vui lòng nhập họ tên bệnh nhân.");
                valid = false;
            }

            // Chấp nhận số điện thoại Việt Nam 10 chữ số, bắt đầu bằng 0.
            if (!Regex.IsMatch(phone, @"^0\d{9}$"))
            {
                _errorProvider.SetError(txtPhone, "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng 0.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                _errorProvider.SetError(txtReason, "Vui lòng nhập lý do khám.");
                valid = false;
            }

            if (dtpAppointment.Value < DateTime.Now)
            {
                _errorProvider.SetError(dtpAppointment, "Ngày giờ hẹn không được nằm trong quá khứ.");
                valid = false;
            }

            if (cboDoctor.SelectedIndex < 0)
            {
                _errorProvider.SetError(cboDoctor, "Vui lòng chọn bác sĩ.");
                valid = false;
            }

            if (cboStatus.SelectedIndex < 0)
            {
                _errorProvider.SetError(cboStatus, "Vui lòng chọn trạng thái.");
                valid = false;
            }

            if (!valid)
                MessageBox.Show("Vui lòng kiểm tra các trường được đánh dấu lỗi.", "Dữ liệu chưa hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

            return valid;
        }

        private Appointment ReadInputs() => new Appointment
        {
            AppointmentId = txtAppointmentId.Text.Trim(),
            PatientName = txtPatientName.Text.Trim(),
            PhoneNumber = txtPhone.Text.Trim(),
            AppointmentDateTime = dtpAppointment.Value,
            Doctor = cboDoctor.Text,
            Reason = txtReason.Text.Trim(),
            Status = cboStatus.Text
        };

        private void RefreshVisibleAppointments(Appointment? selectAfterRefresh = null)
        {
            string search = txtSearch?.Text.Trim() ?? "";
            string status = cboFilterStatus?.SelectedItem?.ToString() ?? "Tất cả trạng thái";
            bool filterDate = chkFilterDate?.Checked ?? false;
            DateTime selectedDate = dtpFilterDate?.Value.Date ?? DateTime.Today;

            var filtered = _appointments.Where(a =>
                (search.Length == 0 ||
                 a.PatientName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                 a.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                (status == "Tất cả trạng thái" || a.Status == status) &&
                (!filterDate || a.AppointmentDateTime.Date == selectedDate))
                .OrderBy(a => a.AppointmentDateTime)
                .ToList();

            _visibleAppointments.RaiseListChangedEvents = false;
            _visibleAppointments.Clear();
            foreach (var appointment in filtered)
                _visibleAppointments.Add(appointment);
            _visibleAppointments.RaiseListChangedEvents = true;
            _visibleAppointments.ResetBindings();

            lblCount.Text = $"Số lịch hẹn đang hiển thị: {_visibleAppointments.Count}";

            if (selectAfterRefresh != null)
            {
                int index = _visibleAppointments.IndexOf(selectAfterRefresh);
                if (index >= 0)
                {
                    dgvAppointments.ClearSelection();
                    dgvAppointments.Rows[index].Selected = true;
                    dgvAppointments.CurrentCell = dgvAppointments.Rows[index].Cells[0];
                    _selectedAppointment = selectAfterRefresh;
                    PopulateInputs(selectAfterRefresh);
                }
                else
                {
                    _selectedAppointment = null;
                }
            }
        }

        private void LoadSelectedAppointment()
        {
            if (dgvAppointments.CurrentRow?.DataBoundItem is Appointment appointment)
            {
                _selectedAppointment = appointment;
                PopulateInputs(appointment);
            }
        }

        private void PopulateInputs(Appointment appointment)
        {
            txtAppointmentId.Text = appointment.AppointmentId;
            txtPatientName.Text = appointment.PatientName;
            txtPhone.Text = appointment.PhoneNumber;
            dtpAppointment.Value = appointment.AppointmentDateTime < dtpAppointment.MinDate
                ? dtpAppointment.MinDate : appointment.AppointmentDateTime;
            cboDoctor.SelectedItem = appointment.Doctor;
            txtReason.Text = appointment.Reason;
            cboStatus.SelectedItem = appointment.Status;
            _errorProvider.Clear();
        }

        private void ClearInputs()
        {
            _selectedAppointment = null;
            _errorProvider.Clear();
            txtAppointmentId.Clear();
            txtPatientName.Clear();
            txtPhone.Clear();
            txtReason.Clear();
            dtpAppointment.Value = DateTime.Now.AddHours(1);
            if (cboDoctor.Items.Count > 0) cboDoctor.SelectedIndex = 0;
            cboStatus.SelectedItem = "Chờ khám";
            dgvAppointments.ClearSelection();
        }

        private void ExportCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Xuất danh sách lịch hẹn",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"LichHen_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                DefaultExt = "csv",
                AddExtension = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true));
                writer.WriteLine("Mã lịch hẹn,Họ tên bệnh nhân,Số điện thoại,Ngày giờ hẹn,Bác sĩ,Lý do khám,Trạng thái");

                foreach (var a in _visibleAppointments)
                {
                    writer.WriteLine(string.Join(",",
                        Csv(a.AppointmentId),
                        Csv(a.PatientName),
                        Csv(a.PhoneNumber),
                        Csv(a.AppointmentDateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)),
                        Csv(a.Doctor),
                        Csv(a.Reason),
                        Csv(a.Status)));
                }

                MessageBox.Show($"Đã xuất {_visibleAppointments.Count} lịch hẹn ra CSV.",
                    "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể xuất file CSV.\nChi tiết: " + ex.Message,
                    "Lỗi xuất file", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string Csv(string? value)
        {
            value ??= "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
