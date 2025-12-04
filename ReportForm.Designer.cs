using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AutoServiceClient
{
    public partial class ReportForm : Form
    {
        private DatabaseContext _context;
        private int _reportType;

        public ReportForm(string title, int reportType)
        {
            InitializeComponent(title);
            _context = new DatabaseContext();
            _reportType = reportType;
            InitializeReportControls();
        }

        private void InitializeComponent(string title)
        {
            this.Text = title;
            this.Size = new System.Drawing.Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // панель параметров
            var panelParams = new Panel
            {
                Dock = DockStyle.Top,
                Height = 120,
                BackColor = Color.LightGray,
                Padding = new Padding(10)
            };

            // DataGridView для отчета
            dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            this.Controls.Add(dataGridView);
            this.Controls.Add(panelParams);

            this.dataGridView = dataGridView;
            this.panelParams = panelParams;
        }

        private DataGridView dataGridView;
        private Panel panelParams;

        private void InitializeReportControls()
        {
            panelParams.Controls.Clear();

            switch (_reportType)
            {
                case 1: // отчет по заказам
                    InitializeOrderReport();
                    break;
                case 2: // Отчет по деталям
                    InitializePartsReport();
                    break;
                case 3: // отчёт по клиентам
                    InitializeClientsReport();
                    break;
            }
        }

        private void InitializeOrderReport()
        {
            var lblDateFrom = new Label { Text = "С:", Location = new System.Drawing.Point(10, 20), Size = new System.Drawing.Size(30, 25) };
            var dtpFrom = new DateTimePicker { Location = new System.Drawing.Point(40, 17), Size = new System.Drawing.Size(150, 25), Value = DateTime.Now.AddMonths(-1) };

            var lblDateTo = new Label { Text = "По:", Location = new System.Drawing.Point(200, 20), Size = new System.Drawing.Size(30, 25) };
            var dtpTo = new DateTimePicker { Location = new System.Drawing.Point(230, 17), Size = new System.Drawing.Size(150, 25), Value = DateTime.Now };

            var lblStatus = new Label { Text = "Статус:", Location = new System.Drawing.Point(400, 20), Size = new System.Drawing.Size(50, 25) };
            var cmbStatus = new ComboBox
            {
                Location = new System.Drawing.Point(450, 17),
                Size = new System.Drawing.Size(150, 25),
                Items = { "Все", "created", "in_progress", "completed", "cancelled" }
            };
            cmbStatus.SelectedIndex = 0;

            var btnGenerate = new Button
            {
                Text = "Сформировать отчет",
                Location = new System.Drawing.Point(620, 15),
                Size = new System.Drawing.Size(150, 30),
                BackColor = Color.LightBlue
            };
            btnGenerate.Click += (s, e) => GenerateOrderReport(dtpFrom.Value, dtpTo.Value,
                cmbStatus.SelectedItem.ToString());

            panelParams.Controls.AddRange(new Control[] { lblDateFrom, dtpFrom, lblDateTo, dtpTo,
                lblStatus, cmbStatus, btnGenerate });

            GenerateOrderReport(dtpFrom.Value, dtpTo.Value, "Все");
        }

        private void GenerateOrderReport(DateTime from, DateTime to, string status)
        {
            try
            {
                var query = _context.service_orders
                    .Include(so => so.Car)
                    .ThenInclude(c => c.Client)
                    .Include(so => so.employee)
                    .Include(so => so.OrderServices)
                    .ThenInclude(os => os.Service)
                    .Include(so => so.OrderParts)
                    .ThenInclude(op => op.Part)
                    .Where(so => so.planned_date >= from && so.planned_date <= to);

                if (status != "Все")
                    query = query.Where(so => so.order_status == status);

                var orders = query.ToList();

                // Группировка по сотрудникам
                var reportData = orders
                    .GroupBy(so => so.employee.fio)
                    .Select(g => new
                    {
                        Сотрудник = g.Key,
                        КоличествоЗаказов = g.Count(),
                        СуммаЗаказов = g.Sum(o => o.total_amount),
                        СреднийЧек = g.Average(o => o.total_amount),
                        Завершенные = g.Count(o => o.order_status == "completed"),
                        ВРаботе = g.Count(o => o.order_status == "in_progress")
                    })
                    .ToList();

                dataGridView.DataSource = reportData;

                // добавление итогов
                AddSummaryRow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializePartsReport()
        {
            var lblSupplier = new Label { Text = "Поставщик:", Location = new System.Drawing.Point(10, 20), Size = new System.Drawing.Size(70, 25) };
            var cmbSupplier = new ComboBox
            {
                Location = new System.Drawing.Point(80, 17),
                Size = new System.Drawing.Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // загрузка поставщиков
            var suppliers = _context.suppliers.ToList();
            cmbSupplier.Items.Add("Все");
            foreach (var supplier in suppliers)
                cmbSupplier.Items.Add(supplier.company_name);
            cmbSupplier.SelectedIndex = 0;

            var lblMinStock = new Label { Text = "миниммальный остаток:", Location = new System.Drawing.Point(300, 20), Size = new System.Drawing.Size(80, 25) };
            var numMinStock = new NumericUpDown { Location = new System.Drawing.Point(380, 17), Size = new System.Drawing.Size(100, 25), Value = 5 };

            var btnGenerate = new Button
            {
                Text = "сформировать отчет",
                Location = new System.Drawing.Point(500, 15),
                Size = new System.Drawing.Size(150, 30),
                BackColor = Color.LightBlue
            };
            btnGenerate.Click += (s, e) => GeneratePartsReport(cmbSupplier.SelectedItem.ToString(),
                (int)numMinStock.Value);

            panelParams.Controls.AddRange(new Control[] { lblSupplier, cmbSupplier, lblMinStock,
                numMinStock, btnGenerate });

            GeneratePartsReport("Все", 5);
        }

        private void GeneratePartsReport(string supplier, int minStock)
        {
            try
            {
                var query = _context.parts
                    .Include(p => p.Supplier)
                    .Include(p => p.OrderParts)
                    .AsQueryable();

                if (supplier != "Все")
                    query = query.Where(p => p.Supplier.company_name == supplier);

                // группировка по поставщикам
                var reportData = query
                    .GroupBy(p => p.Supplier.company_name)
                    .Select(g => new
                    {
                        Поставщик = g.Key,
                        КоличествоДеталей = g.Count(),
                        ОбщаяСтоимость = g.Sum(p => p.price * p.stock_quantity),
                        СредняяЦена = g.Average(p => p.price),
                        НизкийОстаток = g.Count(p => p.stock_quantity <= p.min_stock_level),
                        ПопулярныеДетали = string.Join(", ", g.OrderByDescending(p => p.OrderParts.Count)
                            .Take(3).Select(p => p.part_name))
                    })
                    .ToList();

                dataGridView.DataSource = reportData;
                AddSummaryRow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошика формирования отчета {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeClientsReport()
        {
            var lblPeriod = new Label { Text = "Период (мес):", Location = new System.Drawing.Point(10, 20), Size = new System.Drawing.Size(80, 25) };
            var numPeriod = new NumericUpDown { Location = new System.Drawing.Point(90, 17), Size = new System.Drawing.Size(80, 25), Minimum = 1, Maximum = 24, Value = 6 };

            var lblStatus = new Label { Text = "Статус:", Location = new System.Drawing.Point(200, 20), Size = new System.Drawing.Size(50, 25) };
            var cmbStatus = new ComboBox
            {
                Location = new System.Drawing.Point(250, 17),
                Size = new System.Drawing.Size(150, 25),
                Items = { "Все", "active", "inactive", "vip" }
            };
            cmbStatus.SelectedIndex = 0;

            var btnGenerate = new Button
            {
                Text = "сформировать отчет",
                Location = new System.Drawing.Point(420, 15),
                Size = new System.Drawing.Size(150, 30),
                BackColor = Color.LightBlue
            };
            btnGenerate.Click += (s, e) => GenerateClientsReport((int)numPeriod.Value,
                cmbStatus.SelectedItem.ToString());

            panelParams.Controls.AddRange(new Control[] { lblPeriod, numPeriod, lblStatus,
                cmbStatus, btnGenerate });

            GenerateClientsReport(6, "Все");
        }

        private void GenerateClientsReport(int months, string status)
        {
            try
            {
                var dateFrom = DateTime.Now.AddMonths(-months);

                var query = _context.clients
                    .Include(c => c.Cars)
                    .ThenInclude(car => car.ServiceOrders)
                    .Where(c => c.date_of_admission >= dateFrom);

                if (status != "Все")
                    query = query.Where(c => c.client_status == status);

                // группировка по статусу клиента
                var reportData = query
                    .GroupBy(c => c.client_status)
                    .Select(g => new
                    {
                        Статус = g.Key,
                        КоличествоКлиентов = g.Count(),
                        СредняяСкидка = g.Average(c => c.discount),
                        КоличествоАвтомобилей = g.Sum(c => c.Cars.Count),
                        ОбщаяСуммаЗаказов = g.SelectMany(c => c.Cars)
                            .SelectMany(car => car.ServiceOrders)
                            .Sum(so => so.total_amount),
                        СреднийЧек = g.SelectMany(c => c.Cars)
                            .SelectMany(car => car.ServiceOrders)
                            .Average(so => so.total_amount)
                    })
                    .ToList();

                dataGridView.DataSource = reportData;
                AddSummaryRow();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка формирования отчета: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddSummaryRow()
        {
            if (dataGridView.Rows.Count > 0)
            {
                dataGridView.Rows.Add();
                var row = dataGridView.Rows[dataGridView.Rows.Count - 1];

                // вычисление итогов
                decimal totalSum = 0;
                int totalCount = 0;

                for (int i = 0; i < dataGridView.Rows.Count - 1; i++)
                {
                    if (dataGridView.Rows[i].Cells[2].Value != null)
                    {
                        totalSum += Convert.ToDecimal(dataGridView.Rows[i].Cells[2].Value);
                        totalCount += Convert.ToInt32(dataGridView.Rows[i].Cells[1].Value);
                    }
                }

                row.Cells[0].Value = "ИТОГО:";
                row.Cells[1].Value = totalCount;
                row.Cells[2].Value = totalSum;

                // стиль для итоговой строки
                row.DefaultCellStyle.BackColor = Color.LightYellow;
                row.DefaultCellStyle.Font = new Font(dataGridView.Font, FontStyle.Bold);
            }
        }
    }
}