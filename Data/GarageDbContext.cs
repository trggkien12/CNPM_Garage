using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Models;

namespace AutoGarageManager.Data
{
    public class GarageDbContext : DbContext
    {
        public GarageDbContext(DbContextOptions<GarageDbContext> options)
            : base(options)
        {
        }
        public DbSet<Supplier> Suppliers { get; set; }
        
        public DbSet<Warranty> Warranties { get; set; }

        public DbSet<Customer> Customers { get; set; }

        public DbSet<Car> Cars { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Service> Services { get; set; }

        public DbSet<SparePart> SpareParts { get; set; }

        public DbSet<RepairOrder> RepairOrders { get; set; }

        public DbSet<RepairDetail> RepairDetails { get; set; }

        public DbSet<RepairPart> RepairParts { get; set; }

        public DbSet<Invoice> Invoices { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Appointment> Appointments { get; set; }
        

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure snake_case naming convention
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                entity.SetTableName(ToSnakeCase(entity.GetTableName()));

                foreach (var property in entity.GetProperties())
                {
                    property.SetColumnName(ToSnakeCase(property.Name));
                }

                foreach (var key in entity.GetKeys())
                {
                    key.SetName(ToSnakeCase(key.GetName()));
                }

                foreach (var foreignKey in entity.GetForeignKeys())
                {
                    foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()));
                }

                foreach (var index in entity.GetIndexes())
                {
                    index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()));
                }
            }

            // Add indexes for commonly searched columns
            modelBuilder.Entity<Car>()
                .HasIndex(c => c.LicensePlate)
                .HasDatabaseName("idx_cars_license_plate");

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.Email)
                .HasDatabaseName("idx_customers_email");

            modelBuilder.Entity<Customer>()
                .HasIndex(c => c.PhoneNumber)
                .HasDatabaseName("idx_customers_phone_number");

            modelBuilder.Entity<RepairOrder>()
                .HasIndex(r => r.Status)
                .HasDatabaseName("idx_repair_orders_status");
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.RepairOrderId)
                .IsUnique()
                .HasDatabaseName("idx_invoices_repair_order_id");

            modelBuilder.Entity<Appointment>()
                .HasIndex(a => a.AppointmentDate)
                .HasDatabaseName("idx_appointments_appointment_date");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.InvoiceId)
                .HasDatabaseName("idx_payments_invoice_id");

            modelBuilder.Entity<Payment>()
                .HasIndex(p => p.Status)
                .HasDatabaseName("idx_payments_status");

            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.Status)
                .HasDatabaseName("idx_invoices_status");

        }

        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var result = new System.Text.StringBuilder();
            result.Append(char.ToLower(input[0]));

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    result.Append('_');
                    result.Append(char.ToLower(input[i]));
                }
                else
                {
                    result.Append(input[i]);
                }
            }

            return result.ToString();
        }
    }
}
