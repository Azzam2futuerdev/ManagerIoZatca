using Microsoft.EntityFrameworkCore;
using ZatcaApi.Models;

namespace ZatcaApi.Repositories
{
    public class ApprovedInvoiceRepository : IRepository<ApprovedInvoice>
    {
        private readonly AppDbContext _context;

        public ApprovedInvoiceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ApprovedInvoice>> GetAll()
        {
            return await _context.ApprovedInvoices.ToListAsync();
        }

        public async Task<ApprovedInvoice> GetById(int id)
        {
            return await _context.ApprovedInvoices.FindAsync(id);
        }

        public async Task Update(ApprovedInvoice entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        //public async Task Delete(int id)
        //{
        //    var invoiceLog = await _context.ApprovedInvoices.FindAsync(id);
        //    if (invoiceLog != null)
        //    {
        //        _context.ApprovedInvoices.Remove(invoiceLog);
        //        await _context.SaveChangesAsync();
        //    }
        //}
        public async Task<IEnumerable<ApprovedInvoice>> GetPaged(int page, int pageSize)
        {
            return await _context.ApprovedInvoices
                                .OrderByDescending(log => log.Timestamp)
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();
        }

    }
}
