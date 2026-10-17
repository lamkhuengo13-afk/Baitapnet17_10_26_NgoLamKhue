using LabEquipmentCheckout.Models;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LabEquipmentCheckout
{
    public class MainForm : Form
    {
        private readonly BindingList<EquipmentLoan> _loans = new();
        private readonly BindingList<EquipmentLoan> _visibleLoans = new();
        private readonly BindingSource _bindingSource = new();
        private readonly ErrorProvider _errorProvider = new();

        private TextBox txtLoanId = null!;
        private TextBox txtStudentId = null!;
        private TextBox txtStudentName = null!;
        private ComboBox cboEquipment = null!;
        private NumericUpDown nudQuantity = null!;
        private DateTimePicker dtpBorrowDate = null!;
        private DateTimePicker dtpDueDate = null!;
        private CheckBox chkHasReturnDate = null!;
        private DateTimePicker dtpReturnDate = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboFilterStatus = null!;
        private DataGridView dgvLoans = null!;
        private ToolStripStatusLabel lblTotal = null!;
        private ToolStripStatusLabel lblOverdue = null!;
        private EquipmentLoan? _selectedLoan;

        private static readonly string[] EquipmentOptions =
        {
            "Laptop", "Máy tính để bàn", "Màn hình", "Bàn phím", "Chuột",
            "Bộ định tuyến (Router)", "Switch mạng", "Cáp mạng", "Bộ dụng cụ thực hành"
        };

        private static readonly string[] StatusOptions = { "Tất cả trạng thái", "Đang mượn", "Quá hạn", "Đã trả" };

        public MainForm()
        {
            Text = "Lab Equipment Checkout";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1050, 700);
            Size = new Size(1280, 820);
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(245, 247, 250);

            _errorProvider.ContainerControl = this;
            _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            ConfigureGrid();
            LoadSampleData();
            RefreshVisibleLoans();
        }

        private void BuildInterface()
        {
            var menu = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Tệp");
            var exportItem = new ToolStripMenuItem("Xuất danh sách đang lọc ra CSV");
            exportItem.Click += (_, _) => ExportCsv();
            var exitItem = new ToolStripMenuItem("Thoát");
            exitItem.Click += (_, _) => Close();
            fileMenu.DropDownItems.Add(exportItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);
            menu.Items.Add(fileMenu);
            MainMenuStrip = menu;

            var statusStrip = new StatusStrip();
            lblTotal = new ToolStripStatusLabel("Tổng số phiếu: 0");
            lblOverdue = new ToolStripStatusLabel("Số phiếu quá hạn: 0") { Margin = new Padding(24, 3, 0, 2) };
            statusStrip.Items.Add(lblTotal);
            statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
            statusStrip.Items.Add(lblOverdue);

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
                Text = "QUẢN LÝ MƯỢN TRẢ THIẾT BỊ PHÒNG THỰC HÀNH",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 71, 107),
                Padding = new Padding(0, 4, 0, 14)
            };

            var groupInput = new GroupBox
            {
                Text = "Thông tin phiếu mượn",
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(12),
                Margin = new Padding(0, 0, 0, 12)
            };

            var input = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(4)
            };
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int i = 0; i < 5; i++) input.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            txtLoanId = CreateTextBox();
            txtStudentId = CreateTextBox();
            txtStudentName = CreateTextBox();
            cboEquipment = CreateComboBox(EquipmentOptions);
            nudQuantity = new NumericUpDown
            {
                Dock = DockStyle.Fill, Minimum = 1, Maximum = 10000, Value = 1,
                ThousandsSeparator = true, Margin = new Padding(3, 5, 12, 5)
            };
            dtpBorrowDate = CreateDatePicker(DateTime.Today);
            dtpDueDate = CreateDatePicker(DateTime.Today.AddDays(7));
            chkHasReturnDate = new CheckBox
            {
                Text = "Đã có ngày trả thực tế",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 7, 3, 3)
            };
            dtpReturnDate = CreateDatePicker(DateTime.Today);
            dtpReturnDate.Enabled = false;
            chkHasReturnDate.CheckedChanged += (_, _) => dtpReturnDate.Enabled = chkHasReturnDate.Checked;

            AddField(input, 0, 0, "Mã phiếu:", txtLoanId);
            AddField(input, 0, 2, "Mã sinh viên:", txtStudentId);
            AddField(input, 1, 0, "Họ tên sinh viên:", txtStudentName);
            AddField(input, 1, 2, "Thiết bị:", cboEquipment);
            AddField(input, 2, 0, "Số lượng:", nudQuantity);
            AddField(input, 2, 2, "Ngày mượn:", dtpBorrowDate);
            AddField(input, 3, 0, "Hạn trả:", dtpDueDate);
            AddField(input, 3, 2, "Ngày trả thực tế:", dtpReturnDate);

            var returnCheckPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            returnCheckPanel.Controls.Add(chkHasReturnDate);
            input.Controls.Add(returnCheckPanel, 1, 4);
            input.SetColumnSpan(returnCheckPanel, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 8, 0, 0)
            };
            var btnAdd = CreateButton("Thêm phiếu", Color.FromArgb(28, 130, 90));
            btnAdd.Click += (_, _) => AddLoan();
            var btnUpdate = CreateButton("Cập nhật", Color.FromArgb(41, 105, 160));
            btnUpdate.Click += (_, _) => UpdateLoan();
            var btnDelete = CreateButton("Xóa phiếu", Color.FromArgb(180, 55, 55));
            btnDelete.Click += (_, _) => DeleteLoan();
            var btnMarkReturned = CreateButton("Đánh dấu đã trả", Color.FromArgb(116, 86, 160));
            btnMarkReturned.Click += (_, _) => MarkAsReturned();
            var btnClear = CreateButton("Làm mới", Color.FromArgb(100, 110, 120));
            btnClear.Click += (_, _) => ClearInputs();
            buttons.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnMarkReturned, btnClear });
            input.RowCount = 6;
            input.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            input.Controls.Add(buttons, 1, 5);
            input.SetColumnSpan(buttons, 3);
            groupInput.Controls.Add(input);

            var groupList = new GroupBox
            {
                Text = "Danh sách phiếu mượn",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };
            var listLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            listLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            listLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var filters = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, WrapContents = true,
                Padding = new Padding(0, 0, 0, 8)
            };
            txtSearch = new TextBox { Width = 270, PlaceholderText = "Tìm theo mã sinh viên / tên thiết bị..." };
            txtSearch.TextChanged += (_, _) => RefreshVisibleLoans();
            cboFilterStatus = new ComboBox
            {
                Width = 160, DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboFilterStatus.Items.AddRange(StatusOptions.Cast<object>().ToArray());
            cboFilterStatus.SelectedIndex = 0;
            cboFilterStatus.SelectedIndexChanged += (_, _) => RefreshVisibleLoans();
            var btnReset = CreateButton("Xóa bộ lọc", Color.FromArgb(100, 110, 120));
            btnReset.Click += (_, _) =>
            {
                txtSearch.Clear();
                cboFilterStatus.SelectedIndex = 0;
                RefreshVisibleLoans();
            };
            filters.Controls.Add(new Label { Text = "Tìm kiếm:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
            filters.Controls.Add(txtSearch);
            filters.Controls.Add(new Label { Text = "Tình trạng:", AutoSize = true, Margin = new Padding(8, 8, 4, 0) });
            filters.Controls.Add(cboFilterStatus);
            filters.Controls.Add(btnReset);

            dgvLoans = new DataGridView
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
                DataSource = _bindingSource
            };
            dgvLoans.SelectionChanged += (_, _) => LoadSelectedLoan();

            listLayout.Controls.Add(filters, 0, 0);
            listLayout.Controls.Add(dgvLoans, 0, 1);
            groupList.Controls.Add(listLayout);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(groupInput, 0, 1);
            root.Controls.Add(groupList, 0, 2);

            Controls.Add(root);
            Controls.Add(statusStrip);
            Controls.Add(menu);
        }

        private static TextBox CreateTextBox() =>
            new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3, 5, 12, 5) };

        private static ComboBox CreateComboBox(string[] values)
        {
            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3, 5, 12, 5)
            };
            combo.Items.AddRange(values.Cast<object>().ToArray());
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;
            return combo;
        }

        private static DateTimePicker CreateDatePicker(DateTime value) => new DateTimePicker
        {
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "dd/MM/yyyy",
            Value = value.Date,
            Margin = new Padding(3, 5, 12, 5)
        };

        private static Button CreateButton(string text, Color color) => new Button
        {
            Text = text, AutoSize = true, MinimumSize = new Size(110, 34),
            BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(3, 4, 7, 4), Padding = new Padding(7, 3, 7, 3),
            Cursor = Cursors.Hand
        };

        private static void AddField(TableLayoutPanel layout, int row, int labelColumn, string text, Control control)
        {
            layout.Controls.Add(new Label
            {
                Text = text, AutoSize = true, Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 9, 3, 3)
            }, labelColumn, row);
            layout.Controls.Add(control, labelColumn + 1, row);
        }

        private void ConfigureGrid()
        {
            dgvLoans.Columns.Clear();
            AddColumn("colLoanId", "Mã phiếu", nameof(EquipmentLoan.LoanId), 70);
            AddColumn("colStudentId", "Mã SV", nameof(EquipmentLoan.StudentId), 70);
            AddColumn("colStudentName", "Họ tên sinh viên", nameof(EquipmentLoan.StudentName), 115);
            AddColumn("colEquipment", "Thiết bị", nameof(EquipmentLoan.EquipmentName), 120);
            AddColumn("colQuantity", "SL", nameof(EquipmentLoan.Quantity), 45);
            AddColumn("colBorrowDate", "Ngày mượn", nameof(EquipmentLoan.BorrowDate), 80, "dd/MM/yyyy");
            AddColumn("colDueDate", "Hạn trả", nameof(EquipmentLoan.DueDate), 80, "dd/MM/yyyy");
            AddColumn("colReturnDate", "Ngày trả thực tế", nameof(EquipmentLoan.ActualReturnDate), 95, "dd/MM/yyyy");
            AddColumn("colStatus", "Tình trạng", nameof(EquipmentLoan.Status), 80);
            _bindingSource.DataSource = _visibleLoans;
        }

        private void AddColumn(string name, string header, string property, float weight, string? format = null)
        {
            var column = new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                DataPropertyName = property,
                FillWeight = weight
            };
            if (format != null)
                column.DefaultCellStyle = new DataGridViewCellStyle { Format = format, NullValue = "—" };
            dgvLoans.Columns.Add(column);
        }

        private void LoadSampleData()
        {
            _loans.Add(new EquipmentLoan
            {
                LoanId = "PM001", StudentId = "D19A001", StudentName = "Nguyễn Minh Châu",
                EquipmentName = "Laptop", Quantity = 1, BorrowDate = DateTime.Today.AddDays(-2),
                DueDate = DateTime.Today.AddDays(5)
            });
            _loans.Add(new EquipmentLoan
            {
                LoanId = "PM002", StudentId = "D19A014", StudentName = "Trần Hoàng Long",
                EquipmentName = "Switch mạng", Quantity = 2, BorrowDate = DateTime.Today.AddDays(-12),
                DueDate = DateTime.Today.AddDays(-3)
            });
            _loans.Add(new EquipmentLoan
            {
                LoanId = "PM003", StudentId = "D19A021", StudentName = "Lê Thu Trang",
                EquipmentName = "Bộ định tuyến (Router)", Quantity = 1, BorrowDate = DateTime.Today.AddDays(-4),
                DueDate = DateTime.Today.AddDays(-1), ActualReturnDate = DateTime.Today
            });
        }

        private void AddLoan()
        {
            if (!ValidateInputs()) return;
            var loan = ReadInputs();
            _loans.Add(loan);
            RefreshVisibleLoans(loan);
            MessageBox.Show("Đã thêm phiếu mượn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInputs();
        }

        private void UpdateLoan()
        {
            if (_selectedLoan == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu cần cập nhật.", "Chưa chọn phiếu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!ValidateInputs()) return;

            string id = txtLoanId.Text.Trim();
            if (_loans.Any(x => !ReferenceEquals(x, _selectedLoan) &&
                                x.LoanId.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                _errorProvider.SetError(txtLoanId, "Mã phiếu đã tồn tại.");
                return;
            }

            _selectedLoan.LoanId = id;
            _selectedLoan.StudentId = txtStudentId.Text.Trim();
            _selectedLoan.StudentName = txtStudentName.Text.Trim();
            _selectedLoan.EquipmentName = cboEquipment.Text;
            _selectedLoan.Quantity = (int)nudQuantity.Value;
            _selectedLoan.BorrowDate = dtpBorrowDate.Value.Date;
            _selectedLoan.DueDate = dtpDueDate.Value.Date;
            _selectedLoan.ActualReturnDate = chkHasReturnDate.Checked ? dtpReturnDate.Value.Date : null;

            RefreshVisibleLoans(_selectedLoan);
            MessageBox.Show("Đã cập nhật phiếu mượn.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteLoan()
        {
            if (_selectedLoan == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu cần xóa.", "Chưa chọn phiếu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var answer = MessageBox.Show(
                $"Bạn có chắc muốn xóa phiếu {_selectedLoan.LoanId} của sinh viên {_selectedLoan.StudentName}?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;

            _loans.Remove(_selectedLoan);
            _selectedLoan = null;
            ClearInputs();
            RefreshVisibleLoans();
        }

        private void MarkAsReturned()
        {
            if (_selectedLoan == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu cần đánh dấu đã trả.", "Chưa chọn phiếu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_selectedLoan.ActualReturnDate.HasValue)
            {
                MessageBox.Show("Phiếu này đã được ghi nhận trả thiết bị.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DateTime returnDate = DateTime.Today;
            if (returnDate < _selectedLoan.BorrowDate.Date)
            {
                MessageBox.Show("Ngày trả thực tế không thể trước ngày mượn.", "Dữ liệu không hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedLoan.ActualReturnDate = returnDate;
            RefreshVisibleLoans(_selectedLoan);
            MessageBox.Show($"Đã đánh dấu phiếu {_selectedLoan.LoanId} là Đã trả ngày {returnDate:dd/MM/yyyy}.",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool ValidateInputs()
        {
            _errorProvider.Clear();
            bool valid = true;
            string id = txtLoanId.Text.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                _errorProvider.SetError(txtLoanId, "Mã phiếu không được để trống.");
                valid = false;
            }
            else if (_loans.Any(x => x.LoanId.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                                     !ReferenceEquals(x, _selectedLoan)))
            {
                _errorProvider.SetError(txtLoanId, "Mã phiếu phải duy nhất.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtStudentId.Text))
            {
                _errorProvider.SetError(txtStudentId, "Mã sinh viên không được để trống.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(txtStudentName.Text))
            {
                _errorProvider.SetError(txtStudentName, "Họ tên sinh viên không được để trống.");
                valid = false;
            }
            if (cboEquipment.SelectedIndex < 0)
            {
                _errorProvider.SetError(cboEquipment, "Vui lòng chọn thiết bị.");
                valid = false;
            }
            if (nudQuantity.Value <= 0)
            {
                _errorProvider.SetError(nudQuantity, "Số lượng phải lớn hơn 0.");
                valid = false;
            }
            if (dtpDueDate.Value.Date < dtpBorrowDate.Value.Date)
            {
                _errorProvider.SetError(dtpDueDate, "Hạn trả không được trước ngày mượn.");
                valid = false;
            }
            if (chkHasReturnDate.Checked && dtpReturnDate.Value.Date < dtpBorrowDate.Value.Date)
            {
                _errorProvider.SetError(dtpReturnDate, "Ngày trả thực tế không được trước ngày mượn.");
                valid = false;
            }

            if (!valid)
                MessageBox.Show("Vui lòng sửa các trường được đánh dấu lỗi trước khi lưu.",
                    "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return valid;
        }

        private EquipmentLoan ReadInputs() => new EquipmentLoan
        {
            LoanId = txtLoanId.Text.Trim(),
            StudentId = txtStudentId.Text.Trim(),
            StudentName = txtStudentName.Text.Trim(),
            EquipmentName = cboEquipment.Text,
            Quantity = (int)nudQuantity.Value,
            BorrowDate = dtpBorrowDate.Value.Date,
            DueDate = dtpDueDate.Value.Date,
            ActualReturnDate = chkHasReturnDate.Checked ? dtpReturnDate.Value.Date : null
        };

        private void RefreshVisibleLoans(EquipmentLoan? selectAfterRefresh = null)
        {
            string search = txtSearch?.Text.Trim() ?? "";
            string filterStatus = cboFilterStatus?.SelectedItem?.ToString() ?? "Tất cả trạng thái";
            DateTime today = DateTime.Today;

            var filtered = _loans.Where(x =>
                (search.Length == 0 ||
                 x.StudentId.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                 x.EquipmentName.Contains(search, StringComparison.CurrentCultureIgnoreCase)) &&
                (filterStatus == "Tất cả trạng thái" || x.Status == filterStatus))
                .OrderBy(x => x.DueDate)
                .ToList();

            _visibleLoans.RaiseListChangedEvents = false;
            _visibleLoans.Clear();
            foreach (var loan in filtered) _visibleLoans.Add(loan);
            _visibleLoans.RaiseListChangedEvents = true;
            _visibleLoans.ResetBindings();

            lblTotal.Text = $"Tổng số phiếu: {_loans.Count}";
            lblOverdue.Text = $"Số phiếu quá hạn: {_loans.Count(x => !x.ActualReturnDate.HasValue && x.DueDate.Date < today)}";

            if (selectAfterRefresh != null)
            {
                int index = _visibleLoans.IndexOf(selectAfterRefresh);
                if (index >= 0 && dgvLoans.Rows.Count > index)
                {
                    dgvLoans.ClearSelection();
                    dgvLoans.Rows[index].Selected = true;
                    dgvLoans.CurrentCell = dgvLoans.Rows[index].Cells[0];
                    _selectedLoan = selectAfterRefresh;
                    PopulateInputs(selectAfterRefresh);
                }
                else
                {
                    _selectedLoan = null;
                }
            }
        }

        private void LoadSelectedLoan()
        {
            if (dgvLoans.CurrentRow?.DataBoundItem is EquipmentLoan loan)
            {
                _selectedLoan = loan;
                PopulateInputs(loan);
            }
        }

        private void PopulateInputs(EquipmentLoan loan)
        {
            txtLoanId.Text = loan.LoanId;
            txtStudentId.Text = loan.StudentId;
            txtStudentName.Text = loan.StudentName;
            cboEquipment.SelectedItem = loan.EquipmentName;
            nudQuantity.Value = Math.Clamp(loan.Quantity, (int)nudQuantity.Minimum, (int)nudQuantity.Maximum);
            dtpBorrowDate.Value = loan.BorrowDate.Date;
            dtpDueDate.Value = loan.DueDate.Date;
            chkHasReturnDate.Checked = loan.ActualReturnDate.HasValue;
            dtpReturnDate.Value = loan.ActualReturnDate?.Date ?? DateTime.Today;
            dtpReturnDate.Enabled = chkHasReturnDate.Checked;
            _errorProvider.Clear();
        }

        private void ClearInputs()
        {
            _selectedLoan = null;
            _errorProvider.Clear();
            txtLoanId.Clear();
            txtStudentId.Clear();
            txtStudentName.Clear();
            if (cboEquipment.Items.Count > 0) cboEquipment.SelectedIndex = 0;
            nudQuantity.Value = 1;
            dtpBorrowDate.Value = DateTime.Today;
            dtpDueDate.Value = DateTime.Today.AddDays(7);
            chkHasReturnDate.Checked = false;
            dtpReturnDate.Value = DateTime.Today;
            dtpReturnDate.Enabled = false;
            dgvLoans.ClearSelection();
        }

        private void ExportCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Xuất danh sách phiếu mượn đang lọc",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"PhieuMuon_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                DefaultExt = "csv",
                AddExtension = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true));
                writer.WriteLine("Mã phiếu,Mã sinh viên,Họ tên sinh viên,Thiết bị,Số lượng,Ngày mượn,Hạn trả,Ngày trả thực tế,Tình trạng");
                foreach (var x in _visibleLoans)
                {
                    writer.WriteLine(string.Join(",",
                        Csv(x.LoanId), Csv(x.StudentId), Csv(x.StudentName), Csv(x.EquipmentName),
                        x.Quantity.ToString(CultureInfo.InvariantCulture),
                        Csv(x.BorrowDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                        Csv(x.DueDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                        Csv(x.ActualReturnDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? ""),
                        Csv(x.Status)));
                }
                MessageBox.Show($"Đã xuất {_visibleLoans.Count} phiếu mượn ra CSV.",
                    "Xuất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể xuất CSV.\nChi tiết: " + ex.Message,
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
