using RepairServiceTracker.Models;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RepairServiceTracker
{
    public class MainForm : Form
    {
        private readonly BindingList<RepairOrder> _orders = new();
        private readonly BindingList<RepairOrder> _visibleOrders = new();
        private readonly BindingSource _bindingSource = new();
        private readonly ErrorProvider _errorProvider = new();

        private TextBox txtOrderId = null!;
        private TextBox txtCustomerName = null!;
        private TextBox txtPhone = null!;
        private ComboBox cboDeviceType = null!;
        private TextBox txtFault = null!;
        private DateTimePicker dtpReceivedDate = null!;
        private NumericUpDown nudEstimatedCost = null!;
        private ComboBox cboStatus = null!;
        private PictureBox picDevice = null!;
        private Label lblImagePath = null!;
        private TextBox txtSearch = null!;
        private ComboBox cboStatusFilter = null!;
        private DataGridView dgvOrders = null!;
        private ToolStripStatusLabel lblCount = null!;
        private ToolStripStatusLabel lblEstimatedTotal = null!;
        private RepairOrder? _selectedOrder;
        private string? _selectedImagePath;

        private static readonly string[] DeviceTypes =
        {
            "Laptop", "Máy tính để bàn", "Điện thoại", "Máy tính bảng",
            "Màn hình", "Máy in", "Thiết bị mạng", "Khác"
        };

        private static readonly string[] Statuses =
        {
            "Mới tiếp nhận", "Đang sửa", "Chờ linh kiện", "Hoàn tất", "Đã hủy"
        };

        private static readonly string[] FilterOptions =
        {
            "Tất cả trạng thái", "Mới tiếp nhận", "Đang sửa", "Chờ linh kiện", "Hoàn tất", "Đã hủy"
        };

        public MainForm()
        {
            Text = "Repair Service Tracker";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 740);
            Size = new Size(1380, 900);
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(245, 247, 250);

            _errorProvider.ContainerControl = this;
            _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            BuildInterface();
            ConfigureGrid();
            LoadSampleData();
            RefreshVisibleOrders();
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
            lblCount = new ToolStripStatusLabel("Số đơn đang hiển thị: 0");
            lblEstimatedTotal = new ToolStripStatusLabel("Tổng chi phí đơn chưa hoàn tất: 0")
            {
                Margin = new Padding(24, 3, 0, 2)
            };
            statusStrip.Items.Add(lblCount);
            statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
            statusStrip.Items.Add(lblEstimatedTotal);

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
                Text = "QUẢN LÝ ĐƠN ĐĂNG KÝ DỊCH VỤ SỬA CHỮA",
                Dock = DockStyle.Fill,
                AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 71, 107),
                Padding = new Padding(0, 4, 0, 14)
            };

            var groupInput = new GroupBox
            {
                Text = "Thông tin đơn sửa chữa",
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
                RowCount = 6,
                Padding = new Padding(4)
            };
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            input.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            for (int i = 0; i < 6; i++) input.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            txtOrderId = CreateTextBox();
            txtCustomerName = CreateTextBox();
            txtPhone = CreateTextBox();
            cboDeviceType = CreateComboBox(DeviceTypes);
            txtFault = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                MinimumSize = new Size(100, 58),
                Margin = new Padding(3, 5, 12, 5)
            };
            dtpReceivedDate = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = DateTime.Today,
                Margin = new Padding(3, 5, 12, 5)
            };
            nudEstimatedCost = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 0,
                Maximum = 1000000000,
                DecimalPlaces = 0,
                ThousandsSeparator = true,
                Increment = 50000,
                Margin = new Padding(3, 5, 12, 5)
            };
            cboStatus = CreateComboBox(Statuses);

            AddField(input, 0, 0, "Mã đơn:", txtOrderId);
            AddField(input, 0, 2, "Tên khách hàng:", txtCustomerName);
            AddField(input, 1, 0, "Số điện thoại:", txtPhone);
            AddField(input, 1, 2, "Loại thiết bị:", cboDeviceType);
            AddField(input, 2, 0, "Ngày tiếp nhận:", dtpReceivedDate);
            AddField(input, 2, 2, "Chi phí dự kiến (VNĐ):", nudEstimatedCost);
            AddField(input, 3, 0, "Trạng thái:", cboStatus);
            AddField(input, 3, 2, "Mô tả lỗi:", txtFault);

            picDevice = new PictureBox
            {
                Width = 170,
                Height = 125,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                Margin = new Padding(3, 5, 12, 5)
            };
            lblImagePath = new Label
            {
                Text = "Chưa chọn ảnh",
                AutoSize = true,
                MaximumSize = new Size(280, 0),
                ForeColor = Color.DimGray,
                Margin = new Padding(3, 9, 3, 3)
            };
            var imagePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            var btnChooseImage = CreateButton("Chọn ảnh thiết bị", Color.FromArgb(41, 105, 160));
            btnChooseImage.Click += (_, _) => ChooseImage();
            var btnRemoveImage = CreateButton("Bỏ ảnh", Color.FromArgb(100, 110, 120));
            btnRemoveImage.Click += (_, _) => ClearSelectedImage();
            var imageButtons = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(4, 5, 3, 3)
            };
            imageButtons.Controls.Add(btnChooseImage);
            imageButtons.Controls.Add(btnRemoveImage);
            imageButtons.Controls.Add(lblImagePath);
            imagePanel.Controls.Add(picDevice);
            imagePanel.Controls.Add(imageButtons);
            input.Controls.Add(new Label
            {
                Text = "Ảnh thiết bị:", AutoSize = true, Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 9, 3, 3)
            }, 0, 4);
            input.Controls.Add(imagePanel, 1, 4);
            input.SetColumnSpan(imagePanel, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 8, 0, 0)
            };
            var btnAdd = CreateButton("Thêm đơn", Color.FromArgb(28, 130, 90));
            btnAdd.Click += (_, _) => AddOrder();
            var btnUpdate = CreateButton("Cập nhật", Color.FromArgb(41, 105, 160));
            btnUpdate.Click += (_, _) => UpdateOrder();
            var btnDelete = CreateButton("Xóa đơn", Color.FromArgb(180, 55, 55));
            btnDelete.Click += (_, _) => DeleteOrder();
            var btnClear = CreateButton("Làm mới", Color.FromArgb(100, 110, 120));
            btnClear.Click += (_, _) => ClearInputs();
            buttons.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnClear });
            input.Controls.Add(buttons, 1, 5);
            input.SetColumnSpan(buttons, 3);

            var groupList = new GroupBox
            {
                Text = "Danh sách đơn sửa chữa",
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
            txtSearch = new TextBox
            {
                Width = 310,
                PlaceholderText = "Tìm theo mã đơn / tên khách / số điện thoại..."
            };
            txtSearch.TextChanged += (_, _) => RefreshVisibleOrders();
            cboStatusFilter = new ComboBox
            {
                Width = 165,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboStatusFilter.Items.AddRange(FilterOptions.Cast<object>().ToArray());
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (_, _) => RefreshVisibleOrders();
            var btnReset = CreateButton("Xóa bộ lọc", Color.FromArgb(100, 110, 120));
            btnReset.Click += (_, _) =>
            {
                txtSearch.Clear();
                cboStatusFilter.SelectedIndex = 0;
                RefreshVisibleOrders();
            };
            filters.Controls.Add(new Label { Text = "Tìm kiếm:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
            filters.Controls.Add(txtSearch);
            filters.Controls.Add(new Label { Text = "Trạng thái:", AutoSize = true, Margin = new Padding(8, 8, 4, 0) });
            filters.Controls.Add(cboStatusFilter);
            filters.Controls.Add(btnReset);

            dgvOrders = new DataGridView
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
            dgvOrders.SelectionChanged += (_, _) => LoadSelectedOrder();

            listLayout.Controls.Add(filters, 0, 0);
            listLayout.Controls.Add(dgvOrders, 0, 1);
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

        private static Button CreateButton(string text, Color color) => new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(110, 34),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(3, 4, 7, 4),
            Padding = new Padding(7, 3, 7, 3),
            Cursor = Cursors.Hand
        };

        private static void AddField(TableLayoutPanel layout, int row, int labelColumn, string text, Control control)
        {
            layout.Controls.Add(new Label
            {
                Text = text,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(3, 9, 3, 3)
            }, labelColumn, row);
            layout.Controls.Add(control, labelColumn + 1, row);
        }

        private void ConfigureGrid()
        {
            dgvOrders.Columns.Clear();
            AddColumn("colOrderId", "Mã đơn", nameof(RepairOrder.OrderId), 65);
            AddColumn("colCustomer", "Khách hàng", nameof(RepairOrder.CustomerName), 105);
            AddColumn("colPhone", "Số điện thoại", nameof(RepairOrder.PhoneNumber), 90);
            AddColumn("colDevice", "Loại thiết bị", nameof(RepairOrder.DeviceType), 90);
            AddColumn("colFault", "Mô tả lỗi", nameof(RepairOrder.FaultDescription), 130);
            AddColumn("colReceived", "Ngày tiếp nhận", nameof(RepairOrder.ReceivedDate), 85, "dd/MM/yyyy");
            AddColumn("colCost", "Chi phí dự kiến", nameof(RepairOrder.EstimatedCost), 90, "N0");
            AddColumn("colStatus", "Trạng thái", nameof(RepairOrder.Status), 95);
            _bindingSource.DataSource = _visibleOrders;
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
            dgvOrders.Columns.Add(column);
        }

        private void LoadSampleData()
        {
            _orders.Add(new RepairOrder
            {
                OrderId = "SC001",
                CustomerName = "Nguyễn Minh Anh",
                PhoneNumber = "0912345678",
                DeviceType = "Laptop",
                FaultDescription = "Máy nóng, quạt kêu to",
                ReceivedDate = DateTime.Today.AddDays(-2),
                EstimatedCost = 850000,
                Status = "Đang sửa"
            });
            _orders.Add(new RepairOrder
            {
                OrderId = "SC002",
                CustomerName = "Trần Quốc Bảo",
                PhoneNumber = "0987654321",
                DeviceType = "Điện thoại",
                FaultDescription = "Màn hình bị nứt",
                ReceivedDate = DateTime.Today.AddDays(-1),
                EstimatedCost = 1250000,
                Status = "Chờ linh kiện"
            });
            _orders.Add(new RepairOrder
            {
                OrderId = "SC003",
                CustomerName = "Lê Thu Hà",
                PhoneNumber = "0901234567",
                DeviceType = "Máy in",
                FaultDescription = "Kẹt giấy khi in",
                ReceivedDate = DateTime.Today,
                EstimatedCost = 300000,
                Status = "Hoàn tất"
            });
        }

        private void AddOrder()
        {
            if (!ValidateInputs()) return;

            var order = ReadInputs();
            _orders.Add(order);
            RefreshVisibleOrders(order);
            MessageBox.Show("Đã thêm đơn sửa chữa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearInputs();
        }

        private void UpdateOrder()
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần cập nhật.", "Chưa chọn đơn",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!ValidateInputs()) return;

            _selectedOrder.OrderId = txtOrderId.Text.Trim();
            _selectedOrder.CustomerName = txtCustomerName.Text.Trim();
            _selectedOrder.PhoneNumber = txtPhone.Text.Trim();
            _selectedOrder.DeviceType = cboDeviceType.Text;
            _selectedOrder.FaultDescription = txtFault.Text.Trim();
            _selectedOrder.ReceivedDate = dtpReceivedDate.Value.Date;
            _selectedOrder.EstimatedCost = nudEstimatedCost.Value;
            _selectedOrder.Status = cboStatus.Text;
            _selectedOrder.ImagePath = _selectedImagePath;

            RefreshVisibleOrders(_selectedOrder);
            MessageBox.Show("Đã cập nhật đơn sửa chữa.", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteOrder()
        {
            if (_selectedOrder == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần xóa.", "Chưa chọn đơn",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var answer = MessageBox.Show(
                $"Bạn có chắc muốn xóa đơn {_selectedOrder.OrderId} của khách hàng {_selectedOrder.CustomerName}?",
                "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;

            _orders.Remove(_selectedOrder);
            _selectedOrder = null;
            ClearInputs();
            RefreshVisibleOrders();
        }

        private bool ValidateInputs()
        {
            _errorProvider.Clear();
            bool valid = true;
            string id = txtOrderId.Text.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                _errorProvider.SetError(txtOrderId, "Mã đơn không được để trống.");
                valid = false;
            }
            else if (_orders.Any(x => x.OrderId.Equals(id, StringComparison.OrdinalIgnoreCase) &&
                                      !ReferenceEquals(x, _selectedOrder)))
            {
                _errorProvider.SetError(txtOrderId, "Mã đơn phải duy nhất.");
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                _errorProvider.SetError(txtCustomerName, "Tên khách hàng không được để trống.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                _errorProvider.SetError(txtPhone, "Số điện thoại không được để trống.");
                valid = false;
            }
            if (cboDeviceType.SelectedIndex < 0)
            {
                _errorProvider.SetError(cboDeviceType, "Vui lòng chọn loại thiết bị.");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(txtFault.Text))
            {
                _errorProvider.SetError(txtFault, "Mô tả lỗi không được để trống.");
                valid = false;
            }
            if (nudEstimatedCost.Value < 0)
            {
                _errorProvider.SetError(nudEstimatedCost, "Chi phí dự kiến không được âm.");
                valid = false;
            }
            if (cboStatus.SelectedIndex < 0)
            {
                _errorProvider.SetError(cboStatus, "Vui lòng chọn trạng thái.");
                valid = false;
            }

            if (!valid)
                MessageBox.Show("Vui lòng sửa các trường được đánh dấu lỗi trước khi lưu.",
                    "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return valid;
        }

        private RepairOrder ReadInputs() => new RepairOrder
        {
            OrderId = txtOrderId.Text.Trim(),
            CustomerName = txtCustomerName.Text.Trim(),
            PhoneNumber = txtPhone.Text.Trim(),
            DeviceType = cboDeviceType.Text,
            FaultDescription = txtFault.Text.Trim(),
            ReceivedDate = dtpReceivedDate.Value.Date,
            EstimatedCost = nudEstimatedCost.Value,
            Status = cboStatus.Text,
            ImagePath = _selectedImagePath
        };

        private void RefreshVisibleOrders(RepairOrder? selectAfterRefresh = null)
        {
            string search = txtSearch?.Text.Trim() ?? "";
            string filterStatus = cboStatusFilter?.SelectedItem?.ToString() ?? "Tất cả trạng thái";

            var filtered = _orders.Where(x =>
                (search.Length == 0 ||
                 x.OrderId.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                 x.CustomerName.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                 x.PhoneNumber.Contains(search, StringComparison.CurrentCultureIgnoreCase)) &&
                (filterStatus == "Tất cả trạng thái" || x.Status == filterStatus))
                .OrderByDescending(x => x.ReceivedDate)
                .ToList();

            _visibleOrders.RaiseListChangedEvents = false;
            _visibleOrders.Clear();
            foreach (var order in filtered) _visibleOrders.Add(order);
            _visibleOrders.RaiseListChangedEvents = true;
            _visibleOrders.ResetBindings();

            lblCount.Text = $"Số đơn đang hiển thị: {_visibleOrders.Count}";
            decimal pendingTotal = _orders
                .Where(x => x.Status != "Hoàn tất" && x.Status != "Đã hủy")
                .Sum(x => x.EstimatedCost);
            lblEstimatedTotal.Text = $"Tổng chi phí đơn chưa hoàn tất: {pendingTotal:N0} VNĐ";

            if (selectAfterRefresh != null)
            {
                int index = _visibleOrders.IndexOf(selectAfterRefresh);
                if (index >= 0 && dgvOrders.Rows.Count > index)
                {
                    dgvOrders.ClearSelection();
                    dgvOrders.Rows[index].Selected = true;
                    dgvOrders.CurrentCell = dgvOrders.Rows[index].Cells[0];
                    _selectedOrder = selectAfterRefresh;
                    PopulateInputs(selectAfterRefresh);
                }
                else
                {
                    _selectedOrder = null;
                }
            }
        }

        private void LoadSelectedOrder()
        {
            if (dgvOrders.CurrentRow?.DataBoundItem is RepairOrder order)
            {
                _selectedOrder = order;
                PopulateInputs(order);
            }
        }

        private void PopulateInputs(RepairOrder order)
        {
            txtOrderId.Text = order.OrderId;
            txtCustomerName.Text = order.CustomerName;
            txtPhone.Text = order.PhoneNumber;
            cboDeviceType.SelectedItem = order.DeviceType;
            txtFault.Text = order.FaultDescription;
            dtpReceivedDate.Value = order.ReceivedDate.Date;
            nudEstimatedCost.Value = Math.Clamp(order.EstimatedCost, nudEstimatedCost.Minimum, nudEstimatedCost.Maximum);
            cboStatus.SelectedItem = order.Status;
            _selectedImagePath = order.ImagePath;
            ShowImage(_selectedImagePath);
            _errorProvider.Clear();
        }

        private void ChooseImage()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Chọn ảnh thiết bị",
                Filter = "Tệp ảnh|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tất cả tệp|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            _selectedImagePath = dialog.FileName;
            ShowImage(_selectedImagePath);
        }

        private void ShowImage(string? path)
        {
            Image? oldImage = picDevice.Image;
            picDevice.Image = null;
            oldImage?.Dispose();

            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                try
                {
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var temp = Image.FromStream(stream);
                    picDevice.Image = new Bitmap(temp);
                    lblImagePath.Text = Path.GetFileName(path);
                }
                catch
                {
                    lblImagePath.Text = "Không thể mở ảnh đã chọn";
                }
            }
            else
            {
                lblImagePath.Text = "Chưa chọn ảnh";
            }
        }

        private void ClearSelectedImage()
        {
            _selectedImagePath = null;
            ShowImage(null);
        }

        private void ClearInputs()
        {
            _selectedOrder = null;
            _selectedImagePath = null;
            _errorProvider.Clear();
            txtOrderId.Clear();
            txtCustomerName.Clear();
            txtPhone.Clear();
            if (cboDeviceType.Items.Count > 0) cboDeviceType.SelectedIndex = 0;
            txtFault.Clear();
            dtpReceivedDate.Value = DateTime.Today;
            nudEstimatedCost.Value = 0;
            if (cboStatus.Items.Count > 0) cboStatus.SelectedIndex = 0;
            ShowImage(null);
            dgvOrders.ClearSelection();
        }

        private void ExportCsv()
        {
            using var dialog = new SaveFileDialog
            {
                Title = "Xuất danh sách đơn sửa chữa đang hiển thị",
                Filter = "CSV (*.csv)|*.csv",
                FileName = $"DonSuaChua_{DateTime.Now:yyyyMMdd_HHmm}.csv",
                DefaultExt = "csv",
                AddExtension = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(true));
                writer.WriteLine("Mã đơn,Tên khách hàng,Số điện thoại,Loại thiết bị,Mô tả lỗi,Ngày tiếp nhận,Chi phí dự kiến,Trạng thái,Đường dẫn ảnh");
                foreach (var x in _visibleOrders)
                {
                    writer.WriteLine(string.Join(",",
                        Csv(x.OrderId), Csv(x.CustomerName), Csv(x.PhoneNumber), Csv(x.DeviceType),
                        Csv(x.FaultDescription),
                        Csv(x.ReceivedDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)),
                        x.EstimatedCost.ToString("0", CultureInfo.InvariantCulture),
                        Csv(x.Status), Csv(x.ImagePath ?? "")));
                }
                MessageBox.Show($"Đã xuất {_visibleOrders.Count} đơn sửa chữa ra CSV.",
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            picDevice?.Image?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
