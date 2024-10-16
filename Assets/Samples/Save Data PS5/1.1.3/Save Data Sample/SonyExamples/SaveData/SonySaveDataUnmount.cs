﻿
using System.Collections.Generic;


#if UNITY_PS5

using Unity.SaveData.PS5;
using Unity.SaveData.PS5.Core;
using Unity.SaveData.PS5.Mount;
using UnityEngine;

public class SonySaveDataUnmount
{

    public SonySaveDataUnmount()
    {
        Initialize();
    }

    public void Initialize()
    {
        
    }

    public void MenuUserProfiles()
    {
        List<Mounting.MountPoint> mountPoints = Mounting.ActiveMountPoints;

        Mounting.MountPoint mp = SonySaveDataMain.GetMountPoint();

        bool isEnabled = (mp != null);

        string moutPointToolTip = "";

        if (mp != null)
        {
            moutPointToolTip = " Use this on mount point \"" + mp.PathName.Data + "\".";
        }

        int userId = User.GetActiveUserId;

        for (int i = 0; i < mountPoints.Count; i++)
        {
            if (mountPoints[i].IsMounted == true && mountPoints[i].UserId == userId)
            {
                string menuName = "Unmount ";
                menuName += mountPoints[i].PathName.Data;

                /*if (m_MenuUnmount.AddItem(menuName, "Unmount this save data"))
                {
                    Unmount(mountPoints[i]);
                }else if (m_MenuUnmount.AddItem($"{menuName} (Commit)", "Unmount this save data using Commit Unmount Mode"))
                {
                    Unmount(mountPoints[i], Mounting.UnmountMode.Commit);
                }else if(m_MenuUnmount.AddItem($"{menuName} (Backup Async)", "Unmount this save data using Backup Async Unmount Mode"))
                {
                    Unmount(mountPoints[i], Mounting.UnmountMode.BackupAsync);
                }*/
            }
        }
    }

    public void Unmount(Mounting.MountPoint mp, Mounting.UnmountMode unmountMode = Mounting.UnmountMode.Default)
    {
        try
        {
            Mounting.UnmountRequest request = new Mounting.UnmountRequest();

            if (mp == null) return;

            request.UserId = User.GetActiveUserId;
            request.MountPointName = mp.PathName;
            request.UnmountMode = unmountMode;

            EmptyResponse response = new EmptyResponse();

            OnScreenLog.Add("Unmounting = " + request.MountPointName.Data);

            int requestId = Mounting.Unmount(request, response);

            OnScreenLog.Add("Unmount Async : Request Id = " + requestId);
        }
        catch (SaveDataException e)
        {
            OnScreenLog.AddError("Exception : " + e.ExtendedMessage);
        }
    }

    public void OnAsyncEvent(SaveDataCallbackEvent callbackEvent)
    {
        switch (callbackEvent.ApiCalled)
        {
            case FunctionTypes.Unmount:
                {
                    EmptyResponse response = callbackEvent.Response as EmptyResponse;

                    if (response != null && response.ReturnCode == ReturnCodes.SUCCESS)
                    {

                    }
                }
                break;
            case FunctionTypes.NotificationUnmountWithBackup:
                {
                    UnmountWithBackupNotification response = callbackEvent.Response as UnmountWithBackupNotification;

                    if (response != null)
                    {
                        OnScreenLog.Add("UserId : 0x" + response.UserId.ToString("X8"));
                        OnScreenLog.Add("DirName : " + response.DirName.Data);
                    }
                }
                break;
            case FunctionTypes.NotificationBackup:
                {
                    BackupNotification response = callbackEvent.Response as BackupNotification;

                    if (response != null)
                    {
                        OnScreenLog.Add("UserId : 0x" + response.UserId.ToString("X8"));
                        OnScreenLog.Add("DirName : " + response.DirName.Data);
                    }
                }
                break;
        }
    }

}
#endif
