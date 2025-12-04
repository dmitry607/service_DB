using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AutoServiceClient
{
    public partial class ServiceOrderForm : Form
    {
        private DatabaseContext _context;
        private ServiceOrder _currentOrder;

        public ServiceOrderForm(DatabaseContext context)
        {
            InitializeComponent();
            _context = context;
            LoadComboBoxes();
            _currentOrder = new ServiceOrder();
        }

        private void InitializeComponent()
        {
            this.Text = "Создание заказа (1:M)";
            this.Size = new System.Drawing.Size(900, 600);
            this.StartPosition = FormStartPosition.CenterScreen;

            var tabControl = new TabControl { Dock = DockStyle.Fill };

            // вкладка 1 Основная информация о заказе
            var tabOrder = new TabPage("Заказ");
            var tabServices = new TabPage("Услуги");
            var tabParts = new TabPage("Детали");

            // вкладка заказа
            var panelOrder = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var lblCar = new Label { Text = "Автомобиль:", Location = new System.Drawing.Point(10, 20), Size = new System.Drawing.Size(100, 25) };
            cmbCars = new ComboBox { Location = new System.Drawing.Point(120, 17), Size = new System.Drawing.Size(300, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblEmployee = new Label { Text = "Сотрудник:", Location = new System.Drawing.Point(10, 60), Size = new System.Drawing.Size(100, 25) };
            cmbEmployees = new ComboBox { Location = new System.Drawing.Point(120, 57), Size = new System.Drawing.Size(300, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblDate = new Label { Text = "Планируемая дата:", Location = new System.Drawing.Point(10, 100), Size = new System.Drawing.Size(100, 25) };
            dtpPlannedDate = new DateTimePicker { Location = new System.Drawing.Point(120, 97), Size = new System.Drawing.Size(200, 25), Value = DateTime.Now.AddDays(1) };

            var lblStatus = new Label { Text = "Статус:", Location = new System.Drawing.Point(10, 140), Size = new System.Drawing.Size(100, 25) };
            cmbStatus = new ComboBox
            {
                Location = new System.Drawing.Point(120, 137),
                Size = new System.Drawing.Size(150, 25),
                Items = { "created", "in_progress", "completed", "cancelled" }
            };
            cmbStatus.SelectedIndex = 0;

            var lblTotal = new Label { Text = "Итоговая сумма:", Location = new System.Drawing.Point(10, 180), Size = new System.Drawing.Size(100, 25) };
            txtTotal = new TextBox { Location = new System.Drawing.Point(120, 177), Size = new System.Drawing.Size(150, 25), ReadOnly = true };

            var btnSaveOrder = new Button
            {
                Text = "Сохранить заказ",
                Location = new System.Drawing.Point(10, 220),
                Size = new System.Drawing.Size(150, 30),
                BackColor = Color.LightGreen
            };
            btnSaveOrder.Click += BtnSaveOrder_Click;

            panelOrder.Controls.AddRange(new Control[] { lblCar, cmbCars, lblEmployee, cmbEmployees, lblDate, dtpPlannedDate,
                lblStatus, cmbStatus, lblTotal, txtTotal, btnSaveOrder });

            // вкладка услуг
            var panelServices = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            dgvServices = new DataGridView
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(550, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            var lblService = new Label { Text = "Услуга:", Location = new System.Drawing.Point(10, 320), Size = new System.Drawing.Size(80, 25) };
            cmbServices = new ComboBox { Location = new System.Drawing.Point(100, 317), Size = new System.Drawing.Size(300, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblQuantity = new Label { Text = "Количество:", Location = new System.Drawing.Point(10, 360), Size = new System.Drawing.Size(80, 25) };
            numQuantity = new NumericUpDown { Location = new System.Drawing.Point(100, 357), Size = new System.Drawing.Size(100, 25), Minimum = 1, Maximum = 100, Value = 1 };

            var btnAddService = new Button { Text = "Добавить услугу", Location = new System.Drawing.Point(220, 357), Size = new System.Drawing.Size(120, 30) };
            btnAddService.Click += BtnAddService_Click;

            var btnRemoveService = new Button { Text = "Удалить услугу", Location = new System.Drawing.Point(350, 357), Size = new System.Drawing.Size(120, 30) };
            btnRemoveService.Click += BtnRemoveService_Click;

            panelServices.Controls.AddRange(new Control[] { dgvServices, lblService, cmbServices, lblQuantity, numQuantity,
                btnAddService, btnRemoveService });

            // вкладка деталей
            var panelParts = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            dgvParts = new DataGridView
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(550, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            var lblPart = new Label { Text = "Деталь:", Location = new System.Drawing.Point(10, 320), Size = new System.Drawing.Size(80, 25) };
            cmbParts = new ComboBox { Location = new System.Drawing.Point(100, 317), Size = new System.Drawing.Size(300, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblPartQuantity = new Label { Text = "Количество:", Location = new System.Drawing.Point(10, 360), Size = new System.Drawing.Size(80, 25) };
            numPartQuantity = new NumericUpDown { Location = new System.Drawing.Point(100, 357), Size = new System.Drawing.Size(100, 25), Minimum = 1, Maximum = 100, Value = 1 };

            var btnAddPart = new Button { Text = "Добавить деталь", Location = new System.Drawing.Point(220, 357), Size = new System.Drawing.Size(120, 30) };
            btnAddPart.Click += BtnAddPart_Click;

            var btnRemovePart = new Button { Text = "Удалить деталь", Location = new System.Drawing.Point(350, 357), Size = new System.Drawing.Size(120, 30) };
            btnRemovePart.Click += BtnRemovePart_Click;

            panelParts.Controls.AddRange(new Control[] { dgvParts, lblPart, cmbParts, lblPartQuantity, numPartQuantity,
                btnAddPart, btnRemovePart });

            tabOrder.Controls.Add(panelOrder);
            tabServices.Controls.Add(panelServices);
            tabParts.Controls.Add(panelParts);

            tabControl.TabPages.Add(tabOrder);
            tabControl.TabPages.Add(tabServices);
            tabControl.TabPages.Add(tabParts);

            this.Controls.Add(tabControl);
        }

        private ComboBox cmbCars, cmbEmployees, cmbStatus, cmbServices, cmbParts;
        private DateTimePicker dtpPlannedDate;
        private TextBox txtTotal;
        private DataGridView dgvServices, dgvParts;
        private NumericUpDown numQuantity, numPartQuantity;

        private void LoadComboBoxes()
        {
            // загрузка автомобилей
            var cars = _context.cars.Include(c => c.Client).ToList();
            cmbCars.DataSource = cars;
            cmbCars.DisplayMember = "Plate";
            cmbCars.ValueMember = "CarId";

            // загрузка сотрудников
            var employees = _context.employees.Where(e => e.is_active).ToList();
            cmbEmployees.DataSource = employees;
            cmbEmployees.DisplayMember = "FIO";
            cmbEmployees.ValueMember = "EmployeeId";

            // загрузка услуг
            var services = _context.services.ToList();
            cmbServices.DataSource = services;
            cmbServices.DisplayMember = "ServiceName";
            cmbServices.ValueMember = "ServiceId";

            // загрузка деталей
            var parts = _context.parts.Where(p => p.stock_quantity > 0).ToList();
            cmbParts.DataSource = parts;
            cmbParts.DisplayMember = "PartName";
            cmbParts.ValueMember = "PartId";
        }

        private void BtnSaveOrder_Click(object sender, EventArgs e)
        {
            try
            {
                _currentOrder.car_id = (int)cmbCars.SelectedValue;
                _currentOrder.employee_id = (int)cmbEmployees.SelectedValue;
                _currentOrder.planned_date = dtpPlannedDate.Value;
                _currentOrder.order_status = cmbStatus.SelectedItem.ToString();
                _currentOrder.total_amount = CalculateTotal();

                _context.service_orders.Add(_currentOrder);
                _context.SaveChanges();

                MessageBox.Show("Заказ успешно сохранен!", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAddService_Click(object sender, EventArgs e)
        {
            if (cmbServices.SelectedItem == null) return;

            var service = (Service)cmbServices.SelectedItem;
            var orderService = new OrderService
            {
                service_id = service.service_id,
                quantity = (int)numQuantity.Value,
                unit_price = service.price
            };

            _currentOrder.OrderServices ??= new List<OrderService>();
            _currentOrder.OrderServices.Add(orderService);
            UpdateServicesGrid();
            UpdateTotal();
        }

        private void BtnRemoveService_Click(object sender, EventArgs e)
        {
            if (dgvServices.SelectedRows.Count > 0)
            {
                var index = dgvServices.SelectedRows[0].Index;
                var partsList = _currentOrder.OrderParts.ToList();
                partsList.RemoveAt(index);
                _currentOrder.OrderParts = partsList;
                UpdateServicesGrid();
                UpdateTotal();
            }
        }

        private void BtnAddPart_Click(object sender, EventArgs e)
        {
            if (cmbParts.SelectedItem == null) return;

            var part = (Part)cmbParts.SelectedItem;
            var orderPart = new OrderPart
            {
                part_id = part.part_id,
                quantity = (int)numPartQuantity.Value,
                unit_price = part.price
            };

            _currentOrder.OrderParts ??= new List<OrderPart>();
            _currentOrder.OrderParts.Add(orderPart);
            UpdatePartsGrid();
            UpdateTotal();
        }

        private void BtnRemovePart_Click(object sender, EventArgs e)
        {
            if (dgvParts.SelectedRows.Count > 0)
            {
                var index = dgvParts.SelectedRows[0].Index;
                var partsList = _currentOrder.OrderParts.ToList();
                partsList.RemoveAt(index);
                _currentOrder.OrderParts = partsList;
                UpdatePartsGrid();
                UpdateTotal();
            }
        }

        private void UpdateServicesGrid()
        {
            dgvServices.DataSource = _currentOrder.OrderServices?.Select(os => new
            {
                Service = _context.services.Find(os.service_id)?.service_name,
                os.quantity,
                os.unit_price,
                Total = os.quantity * os.unit_price
            }).ToList();
        }

        private void UpdatePartsGrid()
        {
            dgvParts.DataSource = _currentOrder.OrderParts?.Select(op => new
            {
                Part = _context.parts.Find(op.part_id)?.part_name,
                op.quantity,
                op.unit_price,
                Total = op.quantity * op.unit_price
            }).ToList();
        }

        private decimal CalculateTotal()
        {
            decimal total = 0;

            if (_currentOrder.OrderServices != null)
                total += _currentOrder.OrderServices.Sum(os => os.quantity * os.unit_price);

            if (_currentOrder.OrderParts != null)
                total += _currentOrder.OrderParts.Sum(op => op.quantity * op.unit_price);

            return total;
        }

        private void UpdateTotal()
        {
            txtTotal.Text = CalculateTotal().ToString("C2");
        }
    }
}