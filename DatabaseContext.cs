using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace AutoServiceClient
{
    public class DatabaseContext : DbContext
    {
        public DbSet<supplier> suppliers { get; set; }
        public DbSet<Part> parts { get; set; }
        public DbSet<client> clients { get; set; }
        public DbSet<Car> cars { get; set; }
        public DbSet<Employee> employees { get; set; }
        public DbSet<Service> services { get; set; }
        public DbSet<ServiceOrder> service_orders { get; set; }
        public DbSet<OrderService> order_services { get; set; }
        public DbSet<OrderPart> order_parts { get; set; }
        public DbSet<Payment> payments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=autoservice;Username=postgres;Password=student");
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(
                            new ValueConverter<DateTime, DateTime>(
                                v => v.ToUniversalTime(),
                                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                }
            }
            // Конфигурация отношений
            modelBuilder.Entity<Part>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Parts)
                .HasForeignKey(p => p.supplier_id)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Car>()
                .HasOne(c => c.Client)
                .WithMany(cl => cl.Cars)
                .HasForeignKey(c => c.clients_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServiceOrder>()
                .HasOne(so => so.Car)
                .WithMany(c => c.ServiceOrders)
                .HasForeignKey(so => so.car_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ServiceOrder>()
                .HasOne(so => so.employee)
                .WithMany(e => e.ServiceOrders)
                .HasForeignKey(so => so.employee_id);

            modelBuilder.Entity<OrderService>()
                .HasOne(os => os.ServiceOrder)
                .WithMany(so => so.OrderServices)
                .HasForeignKey(os => os.order_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderPart>()
                .HasOne(op => op.ServiceOrder)
                .WithMany(so => so.OrderParts)
                .HasForeignKey(op => op.order_id)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ServiceOrder)
                .WithMany(so => so.Payments)
                .HasForeignKey(p => p.order_id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // Модели данных
    public class supplier
    {
        [Key]
        public int supplier_id { get; set; }
        public string company_name { get; set; }
        public string phone { get; set; }
        public string supplier_address { get; set; }
        public string email { get; set; }
        public int rating { get; set; } = 3;
        public bool is_active { get; set; } = true;
        public ICollection<Part> Parts { get; set; }
    }

    public class Part
    {
        [Key]
        public int part_id { get; set; }
        public string part_name { get; set; }
        public string part_number { get; set; }
        public string parts_description { get; set; }
        public decimal price { get; set; }
        public int stock_quantity { get; set; } = 0;
        public int min_stock_level { get; set; } = 5;
        public int? supplier_id { get; set; }
        public supplier Supplier { get; set; }
        public ICollection<OrderPart> OrderParts { get; set; }
    }

    public class client
    {
        [Key]
        public int clients_id { get; set; }
        public string fio { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public DateTime date_of_admission { get; set; } = DateTime.Now;
        public int discount { get; set; } = 0;
        public string client_status { get; set; } = "active";
        public ICollection<Car> Cars { get; set; }
    }

    public class Car
    {
        [Key]
        public int car_id { get; set; }
        public int clients_id { get; set; }
        public client Client { get; set; }
        public string plate { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string vin { get; set; }
        public string color { get; set; }
        public int mileage { get; set; }
        public int year_of_manufacture { get; set; }
        public string car_status { get; set; } = "active";
        public ICollection<ServiceOrder> ServiceOrders { get; set; }
    }

    public class Employee
    {
        [Key]
        public int employee_id { get; set; }
        public string fio { get; set; }
        public string position { get; set; }
        public decimal salary { get; set; }
        public DateTime hire_date { get; set; } = DateTime.Now;
        public bool is_active { get; set; } = true;
        public string email { get; set; }
        public ICollection<ServiceOrder> ServiceOrders { get; set; }
    }

    public class Service
    {
        [Key]
        public int service_id { get; set; }
        public string service_name { get; set; }
        public decimal price { get; set; }
        public int duration_minutes { get; set; } = 60;
        public string service_category { get; set; } = "maintenance";
        public ICollection<OrderService> OrderServices { get; set; }
    }

    public class ServiceOrder
    {
        [Key]
        public int order_id { get; set; }
        public int car_id { get; set; }
        public Car Car { get; set; }
        public int? employee_id { get; set; }
        public Employee employee { get; set; }
        public DateTime planned_date { get; set; }
        public DateTime? completed_date { get; set; }
        public string order_status { get; set; } = "created";
        public decimal total_amount { get; set; }
        public DateTime created_at { get; set; } = DateTime.Now;
        public ICollection<OrderService> OrderServices { get; set; }
        public ICollection<OrderPart> OrderParts { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }

    public class OrderService
    {
        [Key]
        public int order_service_id { get; set; }
        public int order_id { get; set; }
        public ServiceOrder ServiceOrder { get; set; }
        public int service_id { get; set; }
        public Service Service { get; set; }
        public int quantity { get; set; } = 1;
        public decimal unit_price { get; set; }
    }

    public class OrderPart
    {
        [Key]
        public int order_part_id { get; set; }
        public int order_id { get; set; }
        public ServiceOrder ServiceOrder { get; set; }
        public int part_id { get; set; }
        public Part Part { get; set; }
        public int quantity { get; set; } = 1;
        public decimal unit_price { get; set; }
    }

    public class Payment
    {
        [Key]
        public int payments_id { get; set; }
        public int order_id { get; set; }
        public ServiceOrder ServiceOrder { get; set; }
        public DateTime payment_date { get; set; } = DateTime.Now;
        public decimal amount { get; set; }
        public string payment_method { get; set; }
        public string payment_status { get; set; } = "completed";
        public string reference_number { get; set; }
    }
}