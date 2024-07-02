


#if UNITY_PS5

using Unity.SaveData.PS5;
using Unity.SaveData.PS5.Backup;
using Unity.SaveData.PS5.Core;

public class SonySaveDataBackup
{

    public SonySaveDataBackup()
    {
        Initialize();
    }

    public void Initialize()
    {
        
    }

    public void MenuUserProfiles()
    {

        bool isEnabled = SaveDataDirNames.HasCurrentDirName();

        string dirName = SaveDataDirNames.GetCurrentDirName();

        string dirNameToolTip = "";

        if (isEnabled == true)
        {
            dirNameToolTip = " Use this on directory name \"" + dirName + "\".";
        }
    }

    /// <summary>
    /// Creates a backup for the current save directory. Backup for current save will be skipped if save was created with rollback enabled (see Mounting.MountRequest() for more details).
    /// </summary>
    public void Backup()
    {
        try
        {
            Backups.BackupRequest request = new Backups.BackupRequest();

            DirName dirName = new DirName();
            dirName.Data = SaveDataDirNames.GetCurrentDirName();

            request.UserId = User.GetActiveUserId;
            request.DirName = dirName;

            EmptyResponse response = new EmptyResponse();

            int requestId = Backups.Backup(request, response);

            OnScreenLog.Add("Backup Async : Request Id = " + requestId);
        }
        catch (SaveDataException e)
        {
            OnScreenLog.AddError("Exception : " + e.ExtendedMessage);
        }
    }

    public void OnAsyncEvent(SaveDataCallbackEvent callbackEvent)
    {
        //switch (callbackEvent.ApiCalled)
        //{
        //}
    }

}
#endif
