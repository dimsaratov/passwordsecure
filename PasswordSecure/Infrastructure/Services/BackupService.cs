using System.IO;
using PasswordSecure.Application.Providers;
using PasswordSecure.Application.Services;
using PasswordSecure.DomainModel;

namespace PasswordSecure.Infrastructure.Services;

public partial class BackupService : IBackupService
{
	public BackupService(
		IFileAccessProvider fileAccessProvider,
		IDateTimeProvider dateTimeProvider)
	{
		_fileAccessProvider = fileAccessProvider;
		_dateTimeProvider = dateTimeProvider;
	}

	public void BackupFile(string filePath)
	{
		try
		{
			if (!ExistsFile(filePath))
			{
				return;
			}

            BackupInfo backupInfo = GetBackupInfo(filePath);

			CreateFolderIfNecessary(backupInfo.BackupFolderPath);
			_fileAccessProvider.CopyFile(filePath, backupInfo.BackupFilePath);
		}
		catch
		{
		}
	}

	private const string BackupFolderSuffix = "Backup";

	private readonly IFileAccessProvider _fileAccessProvider;
	private readonly IDateTimeProvider _dateTimeProvider;

	private static bool ExistsFile(string filePath) => Path.Exists(filePath);

	private static void CreateFolderIfNecessary(string backupFolderPath)
	{
		if (!Directory.Exists(backupFolderPath))
		{
			Directory.CreateDirectory(backupFolderPath);
		}
	}

	private BackupInfo GetBackupInfo(string filePath)
	{
        string now = _dateTimeProvider.Now;

        string fileName = Path.GetFileName(filePath);
        string fileExtension = Path.GetExtension(fileName);

        string backupFolderRootPath = Path.GetDirectoryName(filePath)!;
        string backupFolderPrefix = Path.GetFileNameWithoutExtension(fileName);
        string backupFolderName = $"{backupFolderPrefix}_{BackupFolderSuffix}";
        string backupFolderPath = Path.Combine(
			backupFolderRootPath, backupFolderName);

        string backupFileName = $"{backupFolderPrefix}_{now}{fileExtension}";
        string backupFilePath = Path.Combine(backupFolderPath, backupFileName);

		var backupInfo = new BackupInfo(
			backupFolderPath,
			backupFilePath,
			backupFolderPrefix,
			fileExtension);

		return backupInfo;
	}
}
