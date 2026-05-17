using Microsoft.EntityFrameworkCore;
using AutoGarageManager.Data;

namespace AutoGarageManager.Services
{
    public class RepairOrderService
    {
        private readonly GarageDbContext _context;

        public RepairOrderService(GarageDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> CalculateServiceCost(int repairOrderId)
        {
            return await _context.RepairDetails
                .Where(x => x.RepairOrderId == repairOrderId)
                .SumAsync(x => x.Price);
        }

        public async Task<decimal> CalculatePartCost(int repairOrderId)
        {
            return await _context.RepairParts
                .Where(x => x.RepairOrderId == repairOrderId)
                .SumAsync(x => x.TotalPrice);
        }

        public async Task<decimal> CalculateTotalCost(int repairOrderId)
        {
            var serviceCost = await CalculateServiceCost(repairOrderId);

            var partCost = await CalculatePartCost(repairOrderId);

            return serviceCost + partCost;
        }
    }
}