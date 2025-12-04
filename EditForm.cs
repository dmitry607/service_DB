using AutoServiceClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace ServiceApp
{
    public partial class EditForm<T> : Form where T : class, new()
    {
        private DatabaseContext _context;
        private T _entity;
        private bool _isEditMode;

        public EditForm(T entity, DatabaseContext context)
        {
            InitializeComponent();
            _context = context;
            _entity = entity ?? new T();
            _isEditMode = entity != null;
            InitializeForm();
        }

        private void InitializeComponent()
        {
            this.Text = $"Редактирование ({typeof(T).Name})";
            this.Size = new System.Drawing.Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Панель с кнопками
            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = SystemColors.Control
            };

            var btnSave = new Button
            {
                Text = "Сохранить",
                Location = new System.Drawing.Point(300, 10),
                Size = new System.Drawing.Size(80, 30)
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Отмена",
                Location = new System.Drawing.Point(390, 10),
                Size = new System.Drawing.Size(80, 30)
            };
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            panel.Controls.AddRange(new Control[] { btnSave, btnCancel });

            // Панель для полей ввода
            var contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };

            this.Controls.Add(contentPanel);
            this.Controls.Add(panel);
            this.contentPanel = contentPanel;
        }

        private Panel contentPanel;

        private void InitializeForm()
        {
            // очистка панели
            contentPanel.Controls.Clear();

            // установка заголовка формы
            this.Text = _isEditMode ? $"Редактирование {typeof(T).Name}" : $"Добавление {typeof(T).Name}";

            // Создание полей ввода на основе свойств типа T
            var properties = typeof(T).GetProperties()
                .Where(p => p.CanWrite && !p.Name.Contains("Id") && !p.PropertyType.Name.Contains("Collection"))
                .ToList();

            int y = 20;

            foreach (var prop in properties)
            {
                // прпоуск навигационных свойств
                if (prop.PropertyType.FullName?.Contains("ServiceApp") == true &&
                    !prop.PropertyType.IsValueType && prop.PropertyType != typeof(string))
                    continue;

                var label = new Label
                {
                    Text = GetDisplayName(prop.Name) + ":",
                    Location = new System.Drawing.Point(20, y),
                    Size = new System.Drawing.Size(150, 25),
                    TextAlign = System.Drawing.ContentAlignment.MiddleRight
                };

                Control inputControl = CreateInputControl(prop, y);
                inputControl.Tag = prop; // сохранение информацию о свойстве

                contentPanel.Controls.Add(label);
                contentPanel.Controls.Add(inputControl);

                y += 35;
            }
        }

        private string GetDisplayName(string propertyName)
        {
            //  преобразование имен свойств в читаемый вид
            return propertyName switch
            {
                "FIO" => "ФИО",
                "CompanyName" => "Название компании",
                "Phone" => "Телефон",
                "Email" => "E-mail",
                "Address" => "Адрес",
                "Price" => "Цена",
                "StockQuantity" => "Количество на складе",
                "MinStockLevel" => "Минимальный запас",
                "PartNumber" => "Номер детали",
                "PartName" => "Название детали",
                "PartsDescription" => "Описание",
                "Rating" => "Рейтинг",
                "IsActive" => "Активен",
                "DateOfAdmission" => "Дата регистрации",
                "Discount" => "Скидка",
                "ClientStatus" => "Статус клиента",
                "Plate" => "Номерной знак",
                "Brand" => "Марка",
                "Model" => "Модель",
                "Vin" => "VIN",
                "Color" => "Цвет",
                "Mileage" => "Пробег",
                "YearOfManufacture" => "Год выпуска",
                "CarStatus" => "Статус автомобиля",
                "Position" => "Должность",
                "Salary" => "Зарплата",
                "HireDate" => "Дата найма",
                "ServiceName" => "Название услуги",
                "DurationMinutes" => "Длительность (мин)",
                "ServiceCategory" => "Категория услуги",
                "PlannedDate" => "Планируемая дата",
                "CompletedDate" => "Дата завершения",
                "OrderStatus" => "Статус заказа",
                "TotalAmount" => "Общая сумма",
                "CreatedAt" => "Дата создания",
                "PaymentDate" => "Дата оплаты",
                "Amount" => "Сумма",
                "PaymentMethod" => "Способ оплаты",
                "PaymentStatus" => "Статус оплаты",
                "ReferenceNumber" => "Референс номер",
                _ => propertyName
            };
        }

        private Control CreateInputControl(PropertyInfo prop, int y)
        {
            var value = prop.GetValue(_entity);
            var propType = prop.PropertyType;

            if (propType == typeof(string))
            {
                var textBox = new TextBox
                {
                    Location = new System.Drawing.Point(180, y),
                    Size = new System.Drawing.Size(250, 25),
                    Text = value?.ToString() ?? ""
                };

                // Особые случаи для длинных текстов
                if (prop.Name.Contains("Description") || prop.Name.Contains("Address"))
                {
                    textBox.Multiline = true;
                    textBox.Height = 60;
                    textBox.ScrollBars = ScrollBars.Vertical;
                }

                return textBox;
            }
            else if (propType == typeof(int) || propType == typeof(decimal))
            {
                var numeric = new NumericUpDown
                {
                    Location = new System.Drawing.Point(180, y),
                    Size = new System.Drawing.Size(120, 25),
                    DecimalPlaces = propType == typeof(decimal) ? 2 : 0,
                    Minimum = -9999999,
                    Maximum = 9999999
                };

                if (value != null)
                {
                    numeric.Value = Convert.ToDecimal(value);
                }

                // Ограничения для специфичных полей
                if (prop.Name.Contains("Rating"))
                {
                    numeric.Minimum = 1;
                    numeric.Maximum = 5;
                }
                else if (prop.Name.Contains("Discount"))
                {
                    numeric.Minimum = 0;
                    numeric.Maximum = 50;
                }
                else if (prop.Name.Contains("Stock") || prop.Name.Contains("Quantity") || prop.Name.Contains("Mileage"))
                {
                    numeric.Minimum = 0;
                }

                return numeric;
            }
            else if (propType == typeof(bool))
            {
                return new CheckBox
                {
                    Location = new System.Drawing.Point(180, y),
                    Size = new System.Drawing.Size(100, 25),
                    Checked = value != null && (bool)value,
                    Text = ""
                };
            }
            else if (propType == typeof(DateTime) || propType == typeof(DateTime?))
            {
                var datePicker = new DateTimePicker
                {
                    Location = new System.Drawing.Point(180, y),
                    Size = new System.Drawing.Size(200, 25),
                    Format = DateTimePickerFormat.Short
                };

                if (value != null)
                {
                    datePicker.Value = (DateTime)value;
                }
                else if (propType == typeof(DateTime))
                {
                    datePicker.Value = DateTime.Now;
                }

                return datePicker;
            }
            else if (propType.IsEnum)
            {
                var comboBox = new ComboBox
                {
                    Location = new System.Drawing.Point(180, y),
                    Size = new System.Drawing.Size(200, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                var enumValues = Enum.GetValues(propType);
                foreach (var enumValue in enumValues)
                {
                    comboBox.Items.Add(enumValue);
                }

                if (value != null)
                {
                    comboBox.SelectedItem = value;
                }
                else if (comboBox.Items.Count > 0)
                {
                    comboBox.SelectedIndex = 0;
                }

                return comboBox;
            }

            // По умолчанию TextBox
            return new TextBox
            {
                Location = new System.Drawing.Point(180, y),
                Size = new System.Drawing.Size(250, 25),
                Text = value?.ToString() ?? ""
            };
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Сохранение значений из контролов в объект
                for (int i = 0; i < contentPanel.Controls.Count; i += 2)
                {
                    if (i + 1 < contentPanel.Controls.Count)
                    {
                        var control = contentPanel.Controls[i + 1];
                        if (control.Tag is PropertyInfo prop)
                        {
                            SetPropertyValue(prop, control);
                        }
                    }
                }

                // Сохранение в БД
                if (!_isEditMode)
                {
                    _context.Set<T>().Add(_entity);
                }
                else
                {
                    _context.Entry(_entity).State = EntityState.Modified;
                }

                _context.SaveChanges();

                MessageBox.Show("Данные сохранены успешно!", "Успех",
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

        private void SetPropertyValue(PropertyInfo prop, Control control)
        {
            var propType = prop.PropertyType;

            try
            {
                if (control is TextBox textBox)
                {
                    if (propType == typeof(string))
                    {
                        prop.SetValue(_entity, textBox.Text);
                    }
                    else if (propType == typeof(int) && int.TryParse(textBox.Text, out int intValue))
                    {
                        prop.SetValue(_entity, intValue);
                    }
                    else if (propType == typeof(decimal) && decimal.TryParse(textBox.Text, out decimal decValue))
                    {
                        prop.SetValue(_entity, decValue);
                    }
                }
                else if (control is NumericUpDown numericUpDown)
                {
                    if (propType == typeof(int))
                    {
                        prop.SetValue(_entity, (int)numericUpDown.Value);
                    }
                    else if (propType == typeof(decimal))
                    {
                        prop.SetValue(_entity, numericUpDown.Value);
                    }
                }
                else if (control is CheckBox checkBox)
                {
                    prop.SetValue(_entity, checkBox.Checked);
                }
                else if (control is DateTimePicker dateTimePicker)
                {
                    prop.SetValue(_entity, dateTimePicker.Value);
                }
                else if (control is ComboBox comboBox && comboBox.SelectedItem != null)
                {
                    prop.SetValue(_entity, comboBox.SelectedItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка установки значения для свойства {prop.Name}: {ex.Message}");
            }
        }
    }
}