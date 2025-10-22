CREATE TABLE suppliers(
    supplier_id SERIAL Primary key,
    company_name VARCHAR(100) not null,
    phone varchar(20),
    supplier_address text,
    email VARCHAR(100),
    -- Добавляем ограничения
    rating INTEGER CHECK (rating >= 1 AND rating <= 5) DEFAULT 3,
    is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE parts(
    part_id SERIAL Primary key,
    part_name VARCHAR(100) not null,
    part_number varchar(50) unique,
    parts_description text,
    price numeric(7, 2) CHECK (price > 0),
    stock_quantity INTEGER DEFAULT 0 CHECK (stock_quantity >= 0),
    min_stock_level INTEGER DEFAULT 5 CHECK (min_stock_level >= 0),
    supplier_id integer references suppliers(supplier_id) ON DELETE SET NULL
);

CREATE TABLE clients(
    clients_id SERIAL Primary key,
    FIO varchar(100) NOT NULL,
    email varchar(100) UNIQUE,
    phone varchar(15) UNIQUE,
    date_of_admission timestamp DEFAULT CURRENT_TIMESTAMP,
    -- Добавляем ограничения
    discount INTEGER CHECK (discount >= 0 AND discount <= 50) DEFAULT 0,
    client_status VARCHAR(20) DEFAULT 'active' CHECK (client_status IN ('active', 'inactive', 'vip'))
);

CREATE TABLE cars(
    car_id serial primary key,
    clients_id INTEGER REFERENCES clients(clients_id) ON DELETE CASCADE,
    plate varchar(10) UNIQUE,
    brand varchar(50) NOT NULL,
    model varchar(50) NOT NULL,
    vin varchar(17) UNIQUE,
    color varchar(30),
    mileage integer CHECK (mileage >= 0),
    year_of_manufacture INTEGER CHECK (year_of_manufacture >= 1990 AND year_of_manufacture <= EXTRACT(YEAR FROM CURRENT_DATE) + 1),
    car_status VARCHAR(20) DEFAULT 'active' CHECK (car_status IN ('active', 'sold', 'written_off'))
);

CREATE TABLE employees(
    employee_id SERIAL Primary key,
    FIO varchar(100) NOT NULL,
    position varchar(50) NOT NULL,
    salary NUMERIC(8,2) CHECK (salary >= 0),
    hire_date DATE DEFAULT CURRENT_DATE,
    is_active boolean DEFAULT TRUE,
    email VARCHAR(100) UNIQUE
);

CREATE TABLE services(
  service_id SERIAL Primary key,
  service_name varchar(100) NOT NULL UNIQUE,
  price numeric (7, 2) CHECK (price >= 0),
  duration_minutes INTEGER CHECK (duration_minutes > 0) DEFAULT 60,
  service_category VARCHAR(50) DEFAULT 'maintenance'
);

CREATE TABLE service_orders(
    order_id SERIAL Primary key,
    car_id integer references cars(car_id) ON DELETE CASCADE,
    employee_id integer references employees(employee_id),
    planned_date DATE NOT NULL,
    completed_date TIMESTAMP,
    order_status varchar(20) DEFAULT 'created' CHECK (order_status IN ('created', 'in_progress', 'completed', 'cancelled')),
    total_amount numeric(10, 2) CHECK (total_amount >= 0),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE order_services(
    order_service_id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES service_orders(order_id) ON DELETE CASCADE,
    service_id INTEGER REFERENCES services(service_id),
    quantity INTEGER DEFAULT 1 CHECK (quantity > 0),
    unit_price NUMERIC(7,2) CHECK (unit_price >= 0)
);

CREATE TABLE order_parts(
    order_part_id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES service_orders(order_id) ON DELETE CASCADE,
    part_id INTEGER REFERENCES parts(part_id),
    quantity INTEGER DEFAULT 1 CHECK (quantity > 0),
    unit_price NUMERIC(7,2) CHECK (unit_price >= 0)
);

CREATE TABLE payments(
    payments_id SERIAL Primary key,
    order_id integer references service_orders(order_id) ON DELETE CASCADE,
    payment_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    amount numeric(10, 2) not null CHECK (amount > 0),
    payment_method varchar(20) CHECK (payment_method IN ('cash', 'card', 'transfer')),
    payment_status varchar(20) default 'completed' CHECK (payment_status IN ('pending', 'completed', 'failed', 'refunded')),
    reference_number VARCHAR(50) UNIQUE
);

-- INDEX

-- cars
CREATE INDEX idx_cars_clients_id ON cars(clients_id);
CREATE INDEX idx_cars_brand_model ON cars(brand, model);
CREATE INDEX idx_cars_plate ON cars(plate);

-- service_orders
CREATE INDEX idx_orders_car_id ON service_orders(car_id);
CREATE INDEX idx_orders_employee_id ON service_orders(employee_id);
CREATE INDEX idx_orders_status ON service_orders(order_status);
CREATE INDEX idx_orders_dates ON service_orders(planned_date, completed_date);

-- order_services
CREATE INDEX idx_order_services_order_id ON order_services(order_id);
CREATE INDEX idx_order_services_service_id ON order_services(service_id);

-- order_parts
CREATE INDEX idx_order_parts_order_id ON order_parts(order_id);
CREATE INDEX idx_order_parts_part_id ON order_parts(part_id);

--  parts
CREATE INDEX idx_parts_supplier_id ON parts(supplier_id);
CREATE INDEX idx_parts_stock ON parts(stock_quantity) WHERE stock_quantity < min_stock_level;

-- payments
CREATE INDEX idx_payments_order_id ON payments(order_id);
CREATE INDEX idx_payments_date ON payments(payment_date);

--VIEW

CREATE VIEW active_clients_view AS
SELECT clients_id, FIO, email, phone, date_of_admission, discount
FROM clients
WHERE client_status = 'active';

CREATE VIEW order_details_view AS
SELECT 
    so.order_id,
    c.FIO as client_name,
    car.brand,
    car.model,
    car.plate,
    e.FIO as employee_name,
    so.planned_date,
    so.completed_date,
    so.order_status,
    so.total_amount
FROM service_orders so
JOIN cars car ON so.car_id = car.car_id
JOIN clients c ON car.clients_id = c.clients_id
JOIN employees e ON so.employee_id = e.employee_id;

CREATE VIEW employee_statistics_view AS
SELECT 
    e.employee_id,
    e.FIO,
    e.position,
    COUNT(so.order_id) as total_orders,
    SUM(so.total_amount) as total_revenue,
    AVG(so.total_amount) as avg_order_amount
FROM employees e
LEFT JOIN service_orders so ON e.employee_id = so.employee_id
WHERE e.is_active = true
GROUP BY e.employee_id, e.FIO, e.position
HAVING COUNT(so.order_id) > 0;

CREATE VIEW inventory_alert_view AS
SELECT 
    p.part_id,
    p.part_name,
    p.part_number,
    p.stock_quantity,
    p.min_stock_level,
    s.company_name as supplier,
    s.phone as supplier_phone
FROM parts p
JOIN suppliers s ON p.supplier_id = s.supplier_id
WHERE p.stock_quantity <= p.min_stock_level;

-- TRIGGERS

--триггер для автоматического обновления общей суммы заказа при изменении услуг или запчастей
CREATE OR REPLACE FUNCTION update_order_total()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        UPDATE service_orders 
        SET total_amount = (
            SELECT COALESCE(SUM(os.quantity * os.unit_price), 0) + COALESCE(SUM(op.quantity * op.unit_price), 0)
            FROM order_services os 
            LEFT JOIN order_parts op ON os.order_id = op.order_id
            WHERE os.order_id = OLD.order_id OR op.order_id = OLD.order_id
        )
        WHERE order_id = OLD.order_id;
    ELSE
        UPDATE service_orders 
        SET total_amount = (
            SELECT COALESCE(SUM(os.quantity * os.unit_price), 0) + COALESCE(SUM(op.quantity * op.unit_price), 0)
            FROM order_services os 
            LEFT JOIN order_parts op ON os.order_id = op.order_id
            WHERE os.order_id = NEW.order_id OR op.order_id = NEW.order_id
        )
        WHERE order_id = NEW.order_id;
    END IF;
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_order_total_services
    AFTER INSERT OR UPDATE OR DELETE ON order_services
    FOR EACH ROW
    EXECUTE FUNCTION update_order_total();

CREATE TRIGGER trigger_update_order_total_parts
    AFTER INSERT OR UPDATE OR DELETE ON order_parts
    FOR EACH ROW
    EXECUTE FUNCTION update_order_total();

--триггер для автоматического обновления статуса заказа при завершении
CREATE OR REPLACE FUNCTION auto_complete_order()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.completed_date IS NOT NULL AND OLD.completed_date IS NULL THEN
        NEW.order_status = 'completed';
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_auto_complete_order
    BEFORE UPDATE ON service_orders
    FOR EACH ROW
    EXECUTE FUNCTION auto_complete_order();

-- Триггер для ведения истории изменений статусов заказов
CREATE TABLE order_status_history (
    history_id SERIAL PRIMARY KEY,
    order_id INTEGER REFERENCES service_orders(order_id),
    old_status VARCHAR(20),
    new_status VARCHAR(20),
    change_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    changed_by INTEGER REFERENCES employees(employee_id)
);

CREATE OR REPLACE FUNCTION log_order_status_change()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.order_status IS DISTINCT FROM NEW.order_status THEN
        INSERT INTO order_status_history (order_id, old_status, new_status, change_date)
        VALUES (NEW.order_id, OLD.order_status, NEW.order_status, CURRENT_TIMESTAMP);
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_log_order_status_change
    AFTER UPDATE ON service_orders
    FOR EACH ROW
    EXECUTE FUNCTION log_order_status_change();

--Тригер для уменьшения количества запчастей на складе
CREATE OR REPLACE FUNCTION update_parts_stock()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE parts 
        SET stock_quantity = stock_quantity - NEW.quantity
        WHERE part_id = NEW.part_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE parts 
        SET stock_quantity = stock_quantity + OLD.quantity
        WHERE part_id = OLD.part_id;
    ELSIF TG_OP = 'UPDATE' THEN
        UPDATE parts 
        SET stock_quantity = stock_quantity + OLD.quantity - NEW.quantity
        WHERE part_id = NEW.part_id;
    END IF;
    RETURN COALESCE(NEW, OLD);
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_parts_stock
    AFTER INSERT OR UPDATE OR DELETE ON order_parts
    FOR EACH ROW
    EXECUTE FUNCTION update_parts_stock();