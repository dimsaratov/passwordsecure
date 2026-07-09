using System.Threading.Tasks;

using PasswordSecure.Application.Services;
using PasswordSecure.DomainModel;

namespace PasswordSecure.Infrastructure.Services;

public class TaskDecoratorDataAccessService(IDataAccessService dataAccessService) : IDataAccessService
{
    public async Task<AccountEntryCollection?> ReadAccountEntries(AccessParams accessParams)
    {
        Task<AccountEntryCollection?> readAccountEntriesTask = Task.Run(
            () => _dataAccessService.ReadAccountEntries(accessParams));
        AccountEntryCollection? accountEntries = await readAccountEntriesTask;
        return accountEntries;
    }

    public async Task SaveAccountEntries(AccessParams accessParams, AccountEntryCollection accountEntryCollection)
    {
        var saveAccountEntriesTask = Task.Run(
            () => _dataAccessService.SaveAccountEntries(accessParams, accountEntryCollection));

        await saveAccountEntriesTask;
    }

    private readonly IDataAccessService _dataAccessService = dataAccessService;
}
