using System;

namespace PasswordSecure.DomainModel.CustomEventArgs;

public class AccountEntryCollectionEventArgs(
    AccountEntryCollection? accountEntryCollection, bool hasChanged) : EventArgs
{
    public AccountEntryCollection? AccountEntryCollection { get; } = accountEntryCollection;

    public bool HasChanged { get; } = hasChanged;
}
