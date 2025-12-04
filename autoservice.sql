
-- Таблица suppliers (Поставщики)
CREATE TABLE suppliers(
    supplier_id SERIAL PRIMARY KEY,
    company_name VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    supplier_address TEXT,
    email VARCHAR(100),
    rating INTEGER CHECK (rating >= 1 AND rating <= 5) DEFAULT 3,
    is_active BOOLEAN DEFAULT TRUE
);

-- Таблица parts (Детали)
CREATE TABLE parts(
    part_id SERIAL PRIMARY KEY,
    part_name VARCHAR(100) NOT NULL,
    part_number VARCHAR(50) UNIQUE,
    parts_description TEXT,
    price NUMERIC(7, 2) CHECK (price > 0),
    stock_quantity INTEGER DEFAULT 0 CHECK (stock_quantity >= 0),
    min_stock_level INTEGER DEFAULT 5 CHECK (min_stock_level >= 0),
    supplier_id INTEGER REFERENCES suppliers(supplier_id) ON DELETE SET NULL
);

-- Таблица clients (Клиенты)
CREATE TABLE clients(
    clients_id SERIAL PRIMARY KEY,
    fio VARCHAR(100) NOT NULL,
    email VARCHAR(100) UNIQUE,
    phone VARCHAR(15) UNIQUE,
    date_of_admission TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    discount INTEGER CHECK (discount >= 0 AND discount <= 50) DEFAULT 0,
    client_status VARCHAR(20) DEFAULT 'active' CHECK (client_status IN ('active', 'inactive', 'vip'))
);

-- Таблица cars (Автомобили)
CREATE TABLE cars(
    car_id SERIAL PRIMARY KEY,
    clients_id INTEGER REFERENCES clients(clients_id) ON DELETE CASCADE,
    plate VARCHAR(10) UNIQUE,
    brand VARCHAR(50) NOT NULL,
    model VARCHAR(50) NOT NULL,
    vin VARCHAR(17) UNIQUE,
    color VARCHAR(30),
    mileage INTEGER CHECK (mileage >= 0),
    year_of_manufacture INTEGER CHECK (year_of_manufacture >= 1990 AND year_of_manufacture <= EXTRACT(YEAR FROM CURRENT_DATE) + 1),
    car_status VARCHAR(20) DEFAULT 'active' CHECK (car_status IN ('active', 'sold', 'written_off'))
);

-- Таблица employees (Сотрудники)
CREATE TABLE employees(
    employee_id SERIAL PRIMARY KEY,
    fio VARCHAR(100) NOT NULL,
    position VARCHAR(50) NOT NULL,
    salary NUMERIC(8,2) CHECK (salary >= 0),
    hire_date DATE DEFAULT CURRENT_DATE,
    is_active BOOLEAN DEFAULT TRUE,
    email VARCHAR(100) UNIQUE
);

-- Таблица services (Услуги)
CREATE TABLE services(
    service_id SERIAL PRIMARY KEY,
    service_name VARCHAR(100) NOT NULL UNIQUE,
    price NUMERIC(7, 2) CHECK (price >= 0),
    duration_minutes INTEGER CHECK (duration_minutes > 0) DEFAULT 60,
    service_category VARCHAR(50) DEFAULT 'maintenance'
);

-- Таблица service_orders (Заказы на обслуживание)
CREATE TABLE service_orders(
    order_id SERIAL PRIMARY KEY,
    car_id INTEGER REFERENCES cars(car_id) ON DELETE CASCADE,
    employee_id INTEGER REFERENCES employees(employee_id),
    planned_date DATE NOT NULL,
    completed_date TIMESTAMP,
    order_status VARCHAR(20) DEFAULT 'created' CHECK (order_status IN ('created', 'in_progress', 'completed', 'cancelled')),
    total_amount NUMERIC(10, 2) CHECK (total_amount >= 0),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Таблица order_services (Услуги в заказе)
CREATE TABLE order_services(
    order_service_id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES service_orders(order_id) ON DELETE CASCADE,
    service_id INTEGER REFERENCES services(service_id),
    quantity INTEGER DEFAULT 1 CHECK (quantity > 0),
    unit_price NUMERIC(7,2) CHECK (unit_price >= 0)
);

-- Таблица order_parts (Детали в заказе)
CREATE TABLE order_parts(
    order_part_id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES service_orders(order_id) ON DELETE CASCADE,
    part_id INTEGER REFERENCES parts(part_id),
    quantity INTEGER DEFAULT 1 CHECK (quantity > 0),
    unit_price NUMERIC(7,2) CHECK (unit_price >= 0)
);

-- Таблица payments (Платежи)
CREATE TABLE payments(
    payments_id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES service_orders(order_id) ON DELETE CASCADE,
    payment_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    amount NUMERIC(10, 2) NOT NULL CHECK (amount > 0),
    payment_method VARCHAR(20) CHECK (payment_method IN ('cash', 'card', 'transfer')),
    payment_status VARCHAR(20) DEFAULT 'completed' CHECK (payment_status IN ('pending', 'completed', 'failed', 'refunded')),
    reference_number VARCHAR(50) UNIQUE
);

-- Создание индексов для улучшения производительности
CREATE INDEX idx_parts_supplier_id ON parts(supplier_id);
CREATE INDEX idx_cars_clients_id ON cars(clients_id);
CREATE INDEX idx_service_orders_car_id ON service_orders(car_id);
CREATE INDEX idx_service_orders_employee_id ON service_orders(employee_id);
CREATE INDEX idx_order_services_order_id ON order_services(order_id);
CREATE INDEX idx_order_services_service_id ON order_services(service_id);
CREATE INDEX idx_order_parts_order_id ON order_parts(order_id);
CREATE INDEX idx_order_parts_part_id ON order_parts(part_id);
CREATE INDEX idx_payments_order_id ON payments(order_id);

-- Вставка тестовых данных
INSERT INTO suppliers (company_name, phone, email, rating, is_active) VALUES
('Автозапчасти ООО', '+7 (495) 123-45-67', 'info@avtozapchasti.ru', 5, true),
('Детали-Мир', '+7 (495) 987-65-43', 'sales@detali-mir.ru', 4, true),
('АвтоТехСнаб', '+7 (495) 555-44-33', 'order@autotehsnab.ru', 3, true);

INSERT INTO parts (part_name, part_number, price, stock_quantity, supplier_id) VALUES
('Масляный фильтр', 'FIL-001', 1500.00, 25, 1),
('Воздушный фильтр', 'FIL-002', 1200.00, 30, 1),
('Тормозные колодки', 'BRAKE-001', 4500.00, 15, 2),
('Аккумулятор 60Ah', 'BATT-001', 8500.00, 8, 3);

INSERT INTO clients (fio, email, phone, discount, client_status) VALUES
('Иванов Иван Иванович', 'ivanov@mail.ru', '+7 (916) 111-22-33', 5, 'active'),
('Петров Петр Петрович', 'petrov@mail.ru', '+7 (916) 222-33-44', 10, 'vip'),
('Сидорова Анна Сергеевна', 'sidorova@mail.ru', '+7 (916) 333-44-55', 0, 'active');

INSERT INTO cars (clients_id, plate, brand, model, year_of_manufacture, mileage) VALUES
(1, 'А123ВС777', 'Toyota', 'Camry', 2018, 45000),
(2, 'В456ОР777', 'BMW', 'X5', 2020, 25000),
(3, 'С789ТУ777', 'Hyundai', 'Solaris', 2019, 60000);

INSERT INTO employees (fio, position, salary, email, is_active) VALUES
('Сергеев Сергей Сергеевич', 'Механик', 60000.00, 'sergeev@autoservice.ru', true),
('Васильев Василий Васильевич', 'Мастер', 80000.00, 'vasiliev@autoservice.ru', true),
('Алексеев Алексей Алексеевич', 'Консультант', 50000.00, 'alexeev@autoservice.ru', true);

INSERT INTO services (service_name, price, duration_minutes) VALUES
('Замена масла', 2000.00, 60),
('Замена тормозных колодок', 4000.00, 120),
('Диагностика двигателя', 3000.00, 90),
('Замена аккумулятора', 1500.00, 45);

-- Создание представлений для удобства
CREATE VIEW vw_supplier_parts AS
SELECT s.company_name, p.part_name, p.part_number, p.price, p.stock_quantity
FROM suppliers s
JOIN parts p ON s.supplier_id = p.supplier_id;

CREATE VIEW vw_client_cars AS
SELECT c.fio, c.phone, cr.plate, cr.brand, cr.model, cr.year_of_manufacture
FROM clients c
JOIN cars cr ON c.clients_id = cr.clients_id;

-- Сообщение об успешном создании
\echo 'База данных "autoservice" успешно создана!'
\echo ''
\echo 'Проверка таблиц:'
\dt