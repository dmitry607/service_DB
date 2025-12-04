using Microsoft.EntityFrameworkCore;
using ServiceApp;
using System;
using System.Linq;
using System.Windows.Forms;

namespace AutoServiceClient
{
    public partial class MainForm : Form
    {
        private DatabaseContext _context;
        private string _currentTable;

        public MainForm()
        {
            InitializeComponent();
            _context = new DatabaseContext();
            LoadTablesList();
        }

        private void InitializeComponent()
        {
            this.Text = "Автосервис - Клиентское приложение";
            this.Size = new System.Drawing.Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // панель управления
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = SystemColors.ControlDark
            };

            var lblTable = new Label
            {
                Text = "Таблица:",
                Location = new System.Drawing.Point(10, 15),
                Size = new System.Drawing.Size(60, 25)
            };

            var cmbTables = new ComboBox
            {
                Location = new System.Drawing.Point(80, 12),
                Size = new System.Drawing.Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbTables.SelectedIndexChanged += CmbTables_SelectedIndexChanged;

            var btnAdd = new Button
            {
                Text = "Добавить",
                Location = new System.Drawing.Point(250, 10),
                Size = new System.Drawing.Size(100, 30)
            };
            btnAdd.Click += BtnAdd_Click;

            var btnEdit = new Button
            {
                Text = "Изменить",
                Location = new System.Drawing.Point(360, 10),
                Size = new System.Drawing.Size(100, 30)
            };
            btnEdit.Click += BtnEdit_Click;

            var btnDelete = new Button
            {
                Text = "Удалить",
                Location = new System.Drawing.Point(470, 10),
                Size = new System.Drawing.Size(100, 30)
            };
            btnDelete.Click += BtnDelete_Click;

            
            var btnSearch = new Button
            {
                Text = "Поиск",
                Location = new System.Drawing.Point(800, 10),
                Size = new System.Drawing.Size(100, 30)
            };
            btnSearch.Click += BtnSearch_Click;

            // кнопки для отчетов
            var btnReport1 = new Button
            {
                Text = "Отчет 1: Заказы",
                Location = new System.Drawing.Point(910, 10),
                Size = new System.Drawing.Size(120, 30),
                BackColor = Color.LightSkyBlue
            };
            btnReport1.Click += BtnReport1_Click;

            var btnReport2 = new Button
            {
                Text = "Отчет 2: Детали",
                Location = new System.Drawing.Point(1040, 10),
                Size = new System.Drawing.Size(120, 30),
                BackColor = Color.LightSkyBlue
            };
            btnReport2.Click += BtnReport2_Click;

            var btnReport3 = new Button
            {
                Text = "Отчет 3: Клиенты",
                Location = new System.Drawing.Point(910, 45),
                Size = new System.Drawing.Size(120, 30),
                BackColor = Color.LightSkyBlue
            };
            btnReport3.Click += BtnReport3_Click;

            var btnComplexForm = new Button
            {
                Text = "Заказ (1:M)",
                Location = new System.Drawing.Point(1040, 45),
                Size = new System.Drawing.Size(120, 30),
                BackColor = Color.LightGreen
            };
            btnComplexForm.Click += BtnComplexForm_Click;

            panel.Controls.AddRange(new Control[] { lblTable, cmbTables, btnAdd, btnEdit, btnDelete,
                /*btnFilter, btnSort,*/ btnSearch, btnReport1, btnReport2, btnReport3, btnComplexForm });

            DataGridView
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            this.Controls.Add(dataGridView);
            this.Controls.Add(panel);

            this.dataGridView = dataGridView;
            this.cmbTables = cmbTables;
        }

        private DataGridView dataGridView;
        private ComboBox cmbTables;

        private void LoadTablesList()
        {
            cmbTables.Items.AddRange(new string[]
            {
                "Поставщики",
                "Детали",
                "Клиенты",
                "Автомобили",
                "Сотрудники",
                "Услуги",
                "Заказы",
                "Платежи"
            });
        }

        private void CmbTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            _currentTable = cmbTables.SelectedItem.ToString();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                switch (_currentTable)
                {
                    case "Поставщики":
                        dataGridView.DataSource = _context.suppliers.ToList();
                        break;
                    case "Детали":
                        dataGridView.DataSource = _context.parts
                            .Include(p => p.Supplier)
                            .Select(p => new
                            {
                                p.part_id,
                                p.part_name,
                                p.part_number,
                                p.price,
                                p.stock_quantity,
                                p.min_stock_level,
                                Supplier = p.Supplier.company_name
                            }).ToList();
                        break;
                    case "Клиенты":
                        dataGridView.DataSource = _context.clients.ToList();
                        break;
                    case "Автомобили":
                        dataGridView.DataSource = _context.cars
                            .Include(c => c.Client)
                            .Select(c => new
                            {
                                c.car_id,
                                c.plate,
                                c.brand,
                                c.model,
                                Client = c.Client.fio,
                                c.year_of_manufacture,
                                c.mileage
                            }).ToList();
                        break;
                    case "Сотрудники":
                        dataGridView.DataSource = _context.employees.ToList();
                        break;
                    case "Услуги":
                        dataGridView.DataSource = _context.services.ToList();
                        break;
                    case "Заказы":
                        dataGridView.DataSource = _context.service_orders
                            .Include(so => so.Car)
                            .Include(so => so.employee)
                            .Select(so => new
                            {
                                so.order_id,
                                Car = so.Car.plate,
                                Employee = so.employee.fio,
                                so.planned_date,
                                so.completed_date,
                                so.order_status,
                                so.total_amount
                            }).ToList();
                        break;
                    case "Платежи":
                        dataGridView.DataSource = _context.payments
                            .Include(p => p.ServiceOrder)
                            .Select(p => new
                            {
                                p.payments_id,
                                p.order_id,
                                p.amount,
                                p.payment_date,
                                p.payment_method,
                                p.payment_status
                            }).ToList();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentTable))
            {
                MessageBox.Show("Выберите таблицу", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // добавление таблиц для записей
            switch (_currentTable)
            {
                case "Поставщики":
                    var supplierForm = new EditForm<supplier>(null, _context);
                    supplierForm.ShowDialog();
                    break;
                case "Детали":
                    var partForm = new EditForm<Part>(null, _context);
                    partForm.ShowDialog();
                    break;
                case "Клиенты":
                    var clientForm = new EditForm<client>(null, _context);
                    clientForm.ShowDialog();
                    break;
                case "Автомобили":
                    var carForm = new EditForm<Car>(null, _context);
                    carForm.ShowDialog();
                    break;
                case "Сотрудники":
                    var employeeForm = new EditForm<Employee>(null, _context);
                    employeeForm.ShowDialog();
                    break;
                case "Услуги":
                    var serviceForm = new EditForm<Service>(null, _context);
                    serviceForm.ShowDialog(); 
                    break;
                case "Заказы":
                    var orderForm = new EditForm<ServiceOrder>(null, _context); 
                    orderForm.ShowDialog();
                    break;
                case "Платежи":
                    var paymentForm = new EditForm<Payment>(null, _context);
                    paymentForm.ShowDialog();
                    break;
            }

            LoadData();

        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись для редактирования", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var selectedId = (int)dataGridView.SelectedRows[0].Cells[0].Value;

                switch (_currentTable)
                {
                    case "Поставщики":
                        var supplier = _context.suppliers.Find(selectedId);
                        if (supplier != null)
                        {
                            var supplierForm = new EditForm<supplier>(supplier, _context);
                            supplierForm.ShowDialog();
                        }
                        break;
                    case "Детали":
                        var part = _context.parts.Find(selectedId);
                        if (part != null)
                        {
                            var partForm = new EditForm<Part>(part, _context);
                            partForm.ShowDialog();
                        }
                        break;
                    case "Клиенты":
                        var client = _context.clients.Find(selectedId);
                        if (client != null)
                        {
                            var clientForm = new EditForm<client>(client, _context);
                            clientForm.ShowDialog();
                        }
                        break;
                    case "Автомобили":
                        var car = _context.cars.Find(selectedId);
                        if (car != null)
                        {
                            var carForm = new EditForm<Car>(car, _context);
                            carForm.ShowDialog();
                        }
                        break;
                    case "Сотрудники":
                        var employee = _context.employees.Find(selectedId);
                        if (employee != null)
                        {
                            var employeeForm = new EditForm<Employee>(employee, _context);
                            employeeForm.ShowDialog();
                        }
                        break;
                    case "Услуги":
                        var service = _context.services.Find(selectedId);
                        if (service != null)
                        {
                            var serviceForm = new EditForm<Service>(service, _context);
                            serviceForm.ShowDialog();
                        }
                        break;
                    case "Заказы":
                        var order = _context.service_orders.Find(selectedId);
                        if (order != null)
                        {
                            var orderForm = new EditForm<ServiceOrder>(order, _context);
                            orderForm.ShowDialog();
                        }
                        break;
                    case "Платежи":
                        var payment = _context.payments.Find(selectedId);
                        if (payment != null)
                        {
                            var paymentForm = new EditForm<Payment>(payment, _context);
                            paymentForm.ShowDialog();
                        }
                        break;
                }

                LoadData(); 
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("Удалить выбранную запись?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    var selectedId = (int)dataGridView.SelectedRows[0].Cells[0].Value;

                    switch (_currentTable)
                    {
                        case "Поставщики":
                            var supplier = _context.suppliers.Find(selectedId);
                            _context.suppliers.Remove(supplier);
                            break;
                        case "Детали":
                            var part = _context.parts.Find(selectedId);
                            _context.parts.Remove(part);
                            break;
                        case "Клиенты":
                            var client = _context.clients.Find(selectedId);
                            _context.clients.Remove(client);
                            break;
                        case "Автомобили":
                            var car = _context.cars.Find(selectedId);
                            _context.cars.Remove(car);
                            break;
                        case "Сотрудники":
                            var employee = _context.employees.Find(selectedId);
                            _context.employees.Remove(employee);
                            break;
                        case "Услуги":
                            var service = _context.services.Find(selectedId);
                            _context.services.Remove(service);
                            break;
                        case "Заказы":
                            var order = _context.service_orders.Find(selectedId);
                            _context.service_orders.Remove(order);
                            break;
                        case "Платежи":
                            var payment = _context.payments.Find(selectedId);
                            _context.payments.Remove(payment);
                            break;
                    }

                    _context.SaveChanges();
                    LoadData();

                    MessageBox.Show("Запись успешно удалена", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            var searchText = Microsoft.VisualBasic.Interaction.InputBox("Введите текст для поиска:", "Поиск");
            if (!string.IsNullOrEmpty(searchText))
            {

            }
        }

        private void BtnReport1_Click(object sender, EventArgs e)
        {
            var reportForm = new ReportForm("Отчет по заказам", 1);
            reportForm.ShowDialog();
        }

        private void BtnReport2_Click(object sender, EventArgs e)
        {
            var reportForm = new ReportForm("Отчет по деталям", 2);
            reportForm.ShowDialog();
        }

        private void BtnReport3_Click(object sender, EventArgs e)
        {
            var reportForm = new ReportForm("Отчет по клиентам", 3);
            reportForm.ShowDialog();
        }

        private void BtnComplexForm_Click(object sender, EventArgs e)
        {
            var orderForm = new ServiceOrderForm(_context);
            orderForm.ShowDialog();
        }


        //var btnFilter = new Button
        //{
        //    Text = "Фильтр",
        //    Location = new System.Drawing.Point(580, 10),
        //    Size = new System.Drawing.Size(100, 30)
        //};
        //btnFilter.Click += BtnFilter_Click;

        //var btnSort = new Button
        //{
        //    Text = "Сортировка",
        //    Location = new System.Drawing.Point(690, 10),
        //    Size = new System.Drawing.Size(100, 30)
        //};
        //btnSort.Click += BtnSort_Click;

    }
}