#define USE_ASYNC_HANDLING

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Unity.SaveData.PS5.Core;
using Unity.SaveData.PS5.Mount;
using Unity.SaveData.PS5.Info;
using Unity.SaveData.PS5.Dialog;
using Unity.SaveData.PS5.Initialization;

// Save/Load process

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;

using UnityEngine.InputSystem.Layouts;
using System.Runtime.InteropServices;

using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.DualShock;
#endif

#if UNITY_PS5

using Unity.SaveData.PS5;

#if ENABLE_INPUT_SYSTEM
// IMPORTANT: State layout must match with GamepadInputStatePS5 in native.
[StructLayout(LayoutKind.Explicit, Size = 4)]
internal struct GamepadStatePS5Lite : IInputStateTypeInfo
{
    public FourCC format  => new FourCC('P', '4', 'G', 'P');

    [InputControl(name = "buttonNorth", bit = 12)]
    [InputControl(name = "buttonEast", bit = 13)]
    [InputControl(name = "buttonSouth", bit = 14)]
    [InputControl(name = "buttonWest", bit = 15)]
    [InputControl(name = "dpad", layout = "Dpad", sizeInBits = 4, bit = 4)]
    [InputControl(name = "dpad/up", bit = 4)]
    [InputControl(name = "dpad/right", bit = 5)]
    [InputControl(name = "dpad/down", bit = 6)]
    [InputControl(name = "dpad/left", bit = 7)]
    [FieldOffset(0)] public uint buttons;

    [InputControl(layout = "Stick")] [FieldOffset(4)] public Vector2 leftStick;
    [InputControl(layout = "Stick")] [FieldOffset(12)] public Vector2 rightStick;
    [InputControl] [FieldOffset(20)] public float leftTrigger;
    [InputControl] [FieldOffset(24)] public float rightTrigger;
}

[InputControlLayout(stateType = typeof(GamepadStatePS5Lite), displayName = "PS5 DualSense (on PS5)")]
//[Scripting.Preserve]
class DualSenseGamepadLite : DualShockGamepad {}
#endif

public class SonySaveDataMain : MonoBehaviour
{
    SonySaveDataMount m_Mount;
    SonySaveDataUnmount m_Unmount;
    SonySaveDataDelete m_Delete;
    SonySaveDataSearch m_Search;
    SonySaveDataBackup m_Backup;
    SonySaveDataFileOps m_FileOps;

    public Material iconMaterial;

    static SonySaveDataMain singleton;

    static public UInt64 TestBlockSize = Mounting.MountRequest.BLOCKS_MIN + ((1024 * 1024 * 45) / Mounting.MountRequest.BLOCK_SIZE);

    static public void StartSaveDataCoroutine(IEnumerator routine)
    {
        singleton.StartCoroutine(routine);
    }

    void OnSystemServiceFlagEvent(UnityEngine.PS5.Utility.SystemServiceFlag flagindex, bool newValue)
    {
        //OnScreenLog.AddError("Flagindex = " + flagindex + ", Value = " + newValue);

        // Whenever the SystemUiOverlaid is true when it is possible the PS button has been used to open the "Quick Menu".
        // If "Close Application" is used while a save data is in a read/write state then the PS5 will hang waiting for something to close those mount points.
        // Therefore it is very important for this sample app to make sure these mount points are closed.
        // un normal circumstances this won't happen in a real app as mounting for read/write and then unmounting should be handled in a timely manor and unmounting shouldn't require any interaction
        // with the user.
        if(flagindex == UnityEngine.PS5.Utility.SystemServiceFlag.SystemUiOverlaid)
        {
            List<Mounting.MountPoint> mountPoints = Mounting.ActiveMountPoints;

            for (int i = 0; i < mountPoints.Count; i++)
            {
                var mp = mountPoints[i];

                if (mp.IsMounted == true && (mp.MountMode & Mounting.MountModeFlags.ReadWrite) != 0 )
                {
                    //OnScreenLog.AddWarning("Automatically Unmounting " + mp.DirName.Data);
                    m_Unmount.Unmount(mp);
                }
            }
        }
    }

    void Start()
	{
        singleton = this;

#if ENABLE_INPUT_SYSTEM
        InputSystem.RegisterLayout<DualSenseGamepadLite>("PS5DualSenseGamepad",
            matches: new UnityEngine.InputSystem.Layouts.InputDeviceMatcher()
                .WithInterface("PS5")
                .WithDeviceClass("PS5DualShockGamepad"));
#endif

        // Initialize the NP Toolkit.
        //OnScreenLog.Add("Initializing SaveData");

#if UNITY_PS5
		//OnScreenLog.Add(System.String.Format("Initial UserId:0x{0:X}  Primary UserId:0x{1:X}", UnityEngine.PS5.Utility.initialUserId, UnityEngine.PS5.Utility.primaryUserId));
#endif

        m_Mount = new SonySaveDataMount();
        m_Unmount = new SonySaveDataUnmount();
        m_Delete = new SonySaveDataDelete();
        m_Search = new SonySaveDataSearch();
        m_Backup = new SonySaveDataBackup();
        m_FileOps = new SonySaveDataFileOps();

        m_Mount.iconMaterial = iconMaterial;

        m_Mount.screenShotHelper = GetComponent<SaveIconWithScreenShot>();

        UnityEngine.PS5.Utility.onSystemServiceFlagEvent += OnSystemServiceFlagEvent;

        Initialize();
    }

    void Initialize()
    {
        Main.OnAsyncEvent += Main_OnAsyncEvent;

        InitializeSaveData();

        GamePad[] gamePads = GetComponents<GamePad>();

        User.Initialize(gamePads);

        //OutputInstructions();
    }

    void OutputInstructions()
    {
        /*OnScreenLog.Add("Caution:");
        OnScreenLog.Add("Save data mounted in read / write mode must not be mounted for more");
        OnScreenLog.Add("than 15 seconds according to TRC R4098.The C# mount point object has");
        OnScreenLog.Add("an estimated timer on it but only use this as a guide.");

        OnScreenLog.AddNewLine();
        OnScreenLog.Add("To correctly test R4098 do the following: ");

        OnScreenLog.Add("   1) On the home screen, set \"Debug Settings\"-> \"System\"-> \"TRC Check Notifications\" to \"Enable\"");
        OnScreenLog.Add("   2) On the home screen, set \"Debug Settings\"-> \"Game\"-> \"SaveData\"-> \"Debug Notification\" to  \"On\"");
        OnScreenLog.Add("   3) Restart this sample app. ");
        OnScreenLog.Add("   4) Select the \"Mount\" button.");
        OnScreenLog.Add("   5) Select the \"Create Random Directory Name\" button.");
        OnScreenLog.Add("   6) Wait for 15 seconds or more.");
        OnScreenLog.Add("   7) Observe the error message displayed by the PS5 system.");

        OnScreenLog.AddNewLine();

        OnScreenLog.Add("When first running this sample use \"Generate New Directory Name\" to create a few initial directory names to use.");
        OnScreenLog.Add("Use the left and right Dpad buttons to change which is the currently active directory name.");
        OnScreenLog.Add("Then use \"Mount\" -> \"Mount Read/Write\" to create the currently active directory name.");

        OnScreenLog.AddNewLine();

        OnScreenLog.Add("If you already have created some directory names use \"Search\" to populate the directory name list.");

        OnScreenLog.AddNewLine();

        OnScreenLog.Add("Note that each user can mount the same directory name, but will be provided separate mount points.");
        OnScreenLog.Add("The mount point list will show the currently active mount point which is used by some of the methods.");
        OnScreenLog.Add("The active mount point will be indicated and is based on the current directory name and the current user.");
        OnScreenLog.Add("To quickly unmount save data press the Right Trigger. This will unmount all mount point for that user.");

        OnScreenLog.AddNewLine();
        OnScreenLog.AddWarning("Important: If you press the PS button or open any dialog this sample with automatically unmount any");
        OnScreenLog.AddWarning("           save data that has been mounted as Read/Write. This is because if you open the 'Quick Menu' and");
        OnScreenLog.AddWarning("           use 'Close Application' the sample mustn't have any read/write mounted save data, otherwise the close");
        OnScreenLog.AddWarning("           will hang and the console will need to be forcefully rebooted.");*/
        //OnScreenLog.AddNewLine();
    }

    // NOTE : This is called on the "SaveData" thread and is independent of the Behaviour update.
    private void Main_OnAsyncEvent(SaveDataCallbackEvent callbackEvent)
    {
        //OnScreenLog.Add("API Called = (" + callbackEvent.ApiCalled + ") : Request Id = (" + callbackEvent.RequestId + ") : Calling User Id = (0x" + callbackEvent.UserId.ToString("X8") + ")");

        HandleAsynEvent(callbackEvent);
    }

    private void HandleAsynEvent(SaveDataCallbackEvent callbackEvent)
    {
        try
        {
            if (callbackEvent.Response != null)
            {
                if (callbackEvent.Response.ReturnCodeValue < 0)
                {
                    //OnScreenLog.AddError("Response : " + callbackEvent.Response.ConvertReturnCodeToString(callbackEvent.ApiCalled));
                }
                else
                {
                    //OnScreenLog.Add("Response : " + callbackEvent.Response.ConvertReturnCodeToString(callbackEvent.ApiCalled));
                }

                if (callbackEvent.Response.Exception != null)
                {
                    if (callbackEvent.Response.Exception is SaveDataException)
                    {
                        //OnScreenLog.AddError("Response Exception: " + ((SaveDataException)callbackEvent.Response.Exception).ExtendedMessage);
                    }
                    else
                    {
                        //OnScreenLog.AddError("Response Exception: " + callbackEvent.Response.Exception.Message);
                    }
                }
            }

            m_Mount.OnAsyncEvent(callbackEvent);
            m_Unmount.OnAsyncEvent(callbackEvent);
            m_Delete.OnAsyncEvent(callbackEvent);
            m_Search.OnAsyncEvent(callbackEvent);
            m_Backup.OnAsyncEvent(callbackEvent);
            m_FileOps.OnAsyncEvent(callbackEvent);

            //OnScreenLog.AddNewLine();
        }
        catch (SaveDataException e)
        {
            //OnScreenLog.AddError("Main_OnAsyncEvent SaveData Exception = " + e.ExtendedMessage);
            //Console.Error.WriteLine(e.ExtendedMessage); // Output to the PS5 Stderr TTY
            //Console.Error.WriteLine(e.StackTrace); // Output to the PS5 Stderr TTY
        }
        catch (Exception e)
        {
            //OnScreenLog.AddError("Main_OnAsyncEvent General Exception = " + e.Message);
            //OnScreenLog.AddError(e.StackTrace);
            //Console.Error.WriteLine(e.StackTrace); // Output to the PS5 Stderr TTY
        }
    }

    void MenuMain()
    {
            

            /*if (m_MenuMain.AddItem("Mount", initResult.Initialized == true))
            {
                m_MenuStack.PushMenu(m_Mount.GetMenu());
            }

            if (m_MenuMain.AddItem("Unmount", isValid == true))
            {
                m_MenuStack.PushMenu(m_Unmount.GetMenu());
            }

            if (m_MenuMain.AddItem("Example File Operations", isValid == true))
            {
                m_MenuStack.PushMenu(m_FileOps.GetMenu());
            }

            if (m_MenuMain.AddItem("Delete", isValid == true))
            {
                m_MenuStack.PushMenu(m_Delete.GetMenu());
            }

            if (m_MenuMain.AddItem("Backup", isValid == true))
            {
                m_MenuStack.PushMenu(m_Backup.GetMenu());
            }*/

	}

    public void StartAutoSave()
    {
        // Get the user id for the saves
        int userId = User.GetActiveUserId;

        // Create the new item for the saves dialog list
        Dialogs.NewItem newItem = new Dialogs.NewItem();

        newItem.IconPath = "/app0/Media/StreamingAssets/SaveIcon.png";
        newItem.Title = "Autosave - " + OnScreenLog.FrameCount;

        // The directory name for a new savedata
        DirName newDirName = new DirName();
        newDirName.Data = "Autosave";

        // Should the savedata be backed up automaitcally when the savedata is unmounted.
        bool backup = true;

        // What size should a new save data be created.
        UInt64 newSaveDataBlocks = SonySaveDataMain.TestBlockSize;

        // Parameters to use for the savedata
        SaveDataParams saveDataParams = new SaveDataParams();

        saveDataParams.Title = newItem.Title;
        saveDataParams.SubTitle = "Subtitle for auto-save " + OnScreenLog.FrameCount;
        saveDataParams.Detail = "Details for auto-save " + OnScreenLog.FrameCount;
        saveDataParams.UserParam = (uint)OnScreenLog.FrameCount;

        // Actual custom file operation to perform on the savedata, once it is mounted.
        ExampleWriteFilesRequest fileRequest = new ExampleWriteFilesRequest();
        fileRequest.myTestData = "This is some text test data which will be written to an auto-save file. " + OnScreenLog.FrameCount;
        fileRequest.myOtherTestData = "This is some more text which is written to another auto-save file. " + OnScreenLog.FrameCount;
        fileRequest.IgnoreCallback = false; // In this example get a async callback once the file operations are complete

        ExampleWriteFilesResponse fileResponse = new ExampleWriteFilesResponse();

        SonySaveDataMain.StartSaveDataCoroutine(SaveData.AutoSaveProcess.StartAutoSaveProcess(userId, newItem, newDirName, newSaveDataBlocks, saveDataParams, fileRequest, fileResponse, backup, HandleAutoSaveError));
    }

    public void StartAutoSaveLoad()
    {
        // Get the user id for the saves
        int userId = User.GetActiveUserId;

        DirName dirName = new DirName();
        dirName.Data = "Autosave";

        ExampleReadFilesRequest fileRequest = new ExampleReadFilesRequest();
        fileRequest.IgnoreCallback = false; // In this example get a async callback once the file operations are complete
        ExampleReadFilesResponse fileResponse = new ExampleReadFilesResponse();

        SonySaveDataMain.StartSaveDataCoroutine(SaveData.AutoSaveProcess.StartAutoSaveLoadProcess(userId, dirName, fileRequest, fileResponse, HandleAutoSaveError));
    }

    void HandleAutoSaveError(uint errorCode)
    {
        if ( errorCode == (uint)ReturnCodes.DATA_ERROR_NO_SPACE_FS )
        {
            //OnScreenLog.AddError("There is no space available for the auto-save");
        }
        else if (errorCode == (uint)ReturnCodes.SAVE_DATA_ERROR_BROKEN)
        {
            //OnScreenLog.AddError("The auto-save file is corrupt");
            // At this point the title should inform the user their auto-save is corrupt and then decide how best to handle that situation.
            // If the save has a backup that could be restored or the title could choose the next oldest save data to load instead.
            // How this is handled will be up to the title.
        }
    }

    public InitResult initResult;

    void InitializeSaveData()
    {
        try
        {
            InitSettings settings = new InitSettings();

            settings.Affinity = ThreadAffinity.Core5;

            initResult = Main.Initialize(settings);

            if (initResult.Initialized == true)
            {
                //OnScreenLog.Add("SaveData Initialized ");
                //OnScreenLog.Add("Plugin SDK Version : " + initResult.SceSDKVersion.ToString());
                //OnScreenLog.Add("Plugin DLL Version : " + initResult.DllVersion.ToString());
            }
            else
            {
                //OnScreenLog.Add("SaveData not initialized ");
            }
        }
        catch (SaveDataException e)
        {
            //OnScreenLog.AddError("Exception During Initialization : " + e.ExtendedMessage);
        }
#if UNITY_EDITOR
        catch (DllNotFoundException e)
        {
            //OnScreenLog.AddError("Missing DLL Expection : " + e.Message);
            //OnScreenLog.AddError("The sample APP will not run in the editor.");
        }
#endif

        //OnScreenLog.AddNewLine();
    }

    void TerminateLibrary()
    {
        try
        {
            Main.Terminate();

            initResult = new InitResult(); // Reset the initResult.
        }
        catch (SaveDataException e)
        {
            //OnScreenLog.AddError("Exception During Termination : " + e.ExtendedMessage);
        }
    }

    public static Mounting.MountPoint GetMountPoint()
    {
        if (GamePad.activeGamePad == null)
        {
            return null;
        }

        List<Mounting.MountPoint> mountPoints = Mounting.ActiveMountPoints;
        if (mountPoints.Count == 0)
        {
            return null;
        }

        // Find the first active mount point that matches the current user and the current directory name.
        int userId = User.GetActiveUserId;
        string dirName = SaveDataDirNames.GetCurrentDirName();

        for (int i = 0; i < mountPoints.Count; i++)
        {
            if (mountPoints[i].IsMounted == true)
            {
                if (mountPoints[i].UserId == userId &&
                     mountPoints[i].DirName.Data == dirName)
                {
                    return mountPoints[i];
                }
            }
        }

        return null;
    }


}
#endif
