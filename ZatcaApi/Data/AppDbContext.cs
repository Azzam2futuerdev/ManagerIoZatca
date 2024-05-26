using Microsoft.EntityFrameworkCore;
using ZatcaApi.Models;

public class AppDbContext : DbContext
{
    public DbSet<ApprovedInvoice> ApprovedInvoices { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
