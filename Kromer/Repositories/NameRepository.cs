using Kromer.Data;
using Kromer.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Kromer.Repositories;

public class NameRepository(KromerContext context)
{
    public async Task<IList<NameDto>> GetAddressNamesAsync(string address, int limit = 50, int offset = 0)
    {
        var names = await context.Names
            .Where(q => EF.Functions.ILike(q.Owner, address))
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
        
        return names.Select(NameDto.FromEntity).ToList();
    }

    public async Task<int> CountAddressNamesAsync(string address)
    {
        return  await context.Names.CountAsync(q => EF.Functions.ILike(q.Owner, address));
    }
}