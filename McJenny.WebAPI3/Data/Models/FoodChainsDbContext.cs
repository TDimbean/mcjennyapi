//using System;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata;

//namespace McJenny.WebAPI.Data.Models
//{
//    public partial class FoodChainsDbContext : DbContext
//    {
//        public FoodChainsDbContext()
//        {
//        }

//        public FoodChainsDbContext(DbContextOptions<FoodChainsDbContext> options)
//            : base(options)
//        {
//        }

//        public virtual DbSet<DishRequirement> DishRequirements { get; set; }
//        public virtual DbSet<Dish> Dishes { get; set; }
//        public virtual DbSet<Employee> Employees { get; set; }
//        public virtual DbSet<Location> Locations { get; set; }
//        public virtual DbSet<Management> Managements { get; set; }
//        public virtual DbSet<MenuItem> MenuItems { get; set; }
//        public virtual DbSet<Menu> Menus { get; set; }
//        public virtual DbSet<Position> Positions { get; set; }
//        public virtual DbSet<Schedule> Schedules { get; set; }
//        public virtual DbSet<SupplierStock> SupplierStocks { get; set; }
//        public virtual DbSet<Supplier> Suppliers { get; set; }
//        public virtual DbSet<SupplyCategory> SupplyCategories { get; set; }
//        public virtual DbSet<SupplyLink> SupplyLinks { get; set; }

//        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//        {
//            if (!optionsBuilder.IsConfigured)
//            {
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
//                optionsBuilder.UseSqlServer("Server=(localdb)\\ProjectsV13;Database=FoodChainsDb;Trusted_Connection=True;MultipleActiveResultSets=True;");
//            }
//        }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            modelBuilder.Entity<DishRequirement>(entity =>
//            {
//                entity.HasKey(e => e.DishRequirementId)
//                    .HasName("PK__DishRequ__ED720B639A03ABF4");

//                entity.HasOne(d => d.Dish)
//                    .WithMany(p => p.DishRequirements)
//                    .HasForeignKey(d => d.DishId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__DishRequi__DishI__2D27B809");

//                entity.HasOne(d => d.SupplyCategory)
//                    .WithMany(p => p.DishRequirements)
//                    .HasForeignKey(d => d.SupplyCategoryId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__DishRequi__Suppl__2E1BDC42");
//            });

//            modelBuilder.Entity<Dish>(entity =>
//            {
//                entity.HasKey(e => e.DishId)
//                    .HasName("PK__Dishes__18834F500BC41A70");

//                entity.Property(e => e.Name)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);
//            });

//            modelBuilder.Entity<Employee>(entity =>
//            {
//                entity.HasKey(e => e.EmployeeId)
//                    .HasName("PK__Employee__7AD04F1110A53261");

//                entity.Property(e => e.FirstName)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.LastName)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.StartedOn).HasColumnType("date");

//                entity.HasOne(d => d.Location)
//                    .WithMany(p => p.Employees)
//                    .HasForeignKey(d => d.LocationId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__Employees__Locat__4222D4EF");

//                entity.HasOne(d => d.Position)
//                    .WithMany(p => p.Employees)
//                    .HasForeignKey(d => d.PositionId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__Employees__Posit__4316F928");
//            });

//            modelBuilder.Entity<Location>(entity =>
//            {
//                entity.HasKey(e => e.LocationId)
//                    .HasName("PK__Location__E7FEA49700ABE218");

//                entity.Property(e => e.AbreviatedCountry)
//                    .IsRequired()
//                    .HasMaxLength(4)
//                    .IsUnicode(false);

//                entity.Property(e => e.AbreviatedState)
//                    .IsRequired()
//                    .HasMaxLength(4)
//                    .IsUnicode(false);

//                entity.Property(e => e.City)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.Country)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.OpenSince).HasColumnType("date");

//                entity.Property(e => e.State)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.Street)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.HasOne(d => d.Menu)
//                    .WithMany(p => p.Locations)
//                    .HasForeignKey(d => d.MenuId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__Locations__MenuI__3A81B327");

//                entity.HasOne(d => d.Schedule)
//                    .WithMany(p => p.Locations)
//                    .HasForeignKey(d => d.ScheduleId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__Locations__Sched__3B75D760");
//            });

//            modelBuilder.Entity<Management>(entity =>
//            {
//                entity.HasKey(e => e.ManagementId)
//                    .HasName("PK__Manageme__D21F1B36FC64B9B0");

//                entity.HasOne(d => d.Location)
//                    .WithMany(p => p.Managements)
//                    .HasForeignKey(d => d.LocationId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__Managemen__Locat__3D2915A8");

//                entity.HasOne(d => d.Manager)
//                    .WithMany(p => p.Managements)
//                    .HasForeignKey(d => d.ManagerId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__Managemen__Manag__3E1D39E1");
//            });

//            modelBuilder.Entity<MenuItem>(entity =>
//            {
//                entity.HasKey(e => e.MenuItemId)
//                    .HasName("PK__MenuItem__8943F7227D86CE89");

//                entity.HasOne(d => d.Dish)
//                    .WithMany(p => p.MenuItems)
//                    .HasForeignKey(d => d.DishId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__MenuItems__DishI__32E0915F");

//                entity.HasOne(d => d.Menu)
//                    .WithMany(p => p.MenuItems)
//                    .HasForeignKey(d => d.MenuId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__MenuItems__MenuI__31EC6D26");
//            });

//            modelBuilder.Entity<Menu>(entity =>
//            {
//                entity.HasKey(e => e.MenuId)
//                    .HasName("PK__Menus__C99ED23034A5F84C");
//            });

//            modelBuilder.Entity<Position>(entity =>
//            {
//                entity.HasKey(e => e.PositionId)
//                    .HasName("PK__Position__60BB9A79332E7138");

//                entity.Property(e => e.Title)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.Wage).HasColumnType("decimal(5, 2)");
//            });

//            modelBuilder.Entity<Schedule>(entity =>
//            {
//                entity.HasKey(e => e.ScheduleId)
//                    .HasName("PK__Schedule__9C8A5B4999C2A39C");

//                entity.Property(e => e.TimeTable)
//                    .IsRequired()
//                    .HasMaxLength(200)
//                    .IsUnicode(false);
//            });

//            modelBuilder.Entity<SupplierStock>(entity =>
//            {
//                entity.HasKey(e => e.SupplierStockId)
//                    .HasName("PK__Supplier__0BF737A2E217E9CC");

//                entity.HasOne(d => d.Supplier)
//                    .WithMany(p => p.SupplierStocks)
//                    .HasForeignKey(d => d.SupplierId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__SupplierS__Suppl__47DBAE45");

//                entity.HasOne(d => d.SupplyCategory)
//                    .WithMany(p => p.SupplierStocks)
//                    .HasForeignKey(d => d.SupplyCategoryId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__SupplierS__Suppl__48CFD27E");
//            });

//            modelBuilder.Entity<Supplier>(entity =>
//            {
//                entity.HasKey(e => e.SupplierId)
//                    .HasName("PK__Supplier__4BE666B4C16C4FAD");

//                entity.Property(e => e.AbreviatedCountry)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.AbreviatedState)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.City)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.Country)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.Name)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);

//                entity.Property(e => e.State)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);
//            });

//            modelBuilder.Entity<SupplyCategory>(entity =>
//            {
//                entity.HasKey(e => e.SupplyCategoryId)
//                    .HasName("PK__SupplyCa__5ED6BB76001B222D");

//                entity.Property(e => e.Name)
//                    .IsRequired()
//                    .HasMaxLength(50)
//                    .IsUnicode(false);
//            });

//            modelBuilder.Entity<SupplyLink>(entity =>
//            {
//                entity.HasKey(e => e.SupplyLinkId)
//                    .HasName("PK__SupplyLi__2D122135B479CA73");

//                entity.HasOne(d => d.Location)
//                    .WithMany(p => p.SupplyLinks)
//                    .HasForeignKey(d => d.LocationId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__SupplyLin__Locat__17036CC0");

//                entity.HasOne(d => d.Supplier)
//                    .WithMany(p => p.SupplyLinks)
//                    .HasForeignKey(d => d.SupplierId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__SupplyLin__Suppl__160F4887");

//                entity.HasOne(d => d.SupplyCategory)
//                    .WithMany(p => p.SupplyLinks)
//                    .HasForeignKey(d => d.SupplyCategoryId)
//                    .OnDelete(DeleteBehavior.ClientSetNull)
//                    .HasConstraintName("FK__SupplyLin__Suppl__17F790F9");
//            });

//            OnModelCreatingPartial(modelBuilder);
//        }

//        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
//    }
//}
