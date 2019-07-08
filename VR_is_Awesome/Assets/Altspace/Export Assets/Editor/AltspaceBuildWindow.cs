#if UNITY_EDITOR
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using Ionic.Zip;
using System.Threading;

[Serializable]
public class SpaceTemplate
{
    public string Name;

    public string Space_template_id;

    public SpaceTemplate(string name, string id)
    {
        Name = name;
        Space_template_id = id;
    }
}

[Serializable]
public class Kit
{
    public string Name;

    public string Kit_ID;

    public Kit(string name, string id)
    {
        Name = name;
        Kit_ID = id;
    }
}



public class AltspaceBuildWindow : EditorWindow
{

    private const string macAssetBundleFolder = "OSX",
                    pcAssetBundleFolder = "Windows",
                    androidAssetBundleFolder = "Android";

    private string tempSceneFile = "";
    private string outAssetBundleName = "";
    private string saveLocation = string.Empty;
    private string email = string.Empty;
    private string password = string.Empty;

    private List<GameObject> scene = new List<GameObject>();

    private string m_teleportLayer;
    private LayerMask NavLayerMask;

    private MonoBehaviour monoBehavior;

    [SerializeField]
    private UserPreferences _userPrefs;

    private Texture2D logoImage;

    private Vector2 m_templateScrollPosition;

    private Vector2 m_panelScrollPosition;

    private Vector3 m_rotationOverride;

    private int m_projectTypeIndex;

    private Rect m_templateRect = new Rect(1,70,249,150);

    private List<SpaceTemplate> spaceTemplates = new List<SpaceTemplate>();

    private List<Kit> kits = new List<Kit>();

    private List<string> spaceTemplateNames = new List<string>();

    private List<string> kitNames = new List<string>();

    private List<string> m_loadedPrefabDirectories = new List<string>();

    private string m_progressMessage;
    private float m_progress;
    private float m_goal;

    private int selectedTemplateIndex;

    private string m_prefabName;
    private string m_prefabFolderName;

    private GameObject m_selectedKitPrefab;

    private bool isUserSignedIn;

    private bool m_creatingTemplate;
    private bool m_creatingKit;
    private bool m_packageScreenshots = true;

    private bool BuildAndroidKit = true;
    private bool BuildWindowsKit = true;

    private Vector2 m_prefabKitsScroll;

    private bool userHasTemplates;

    private bool userHasPrefabs;

    private bool nullProjectName;
    private bool nullAssetName;

    private bool showPassword;

    private bool rememberUserLogin;

    private string ProjectName = "template";

    public bool IsAndroidBuildTarget = true, IsWindowsBuildTarget = true, IsOSXBuildTarget = true;

    private event Action onTemplateLoadComplete;

    [MenuItem("AltspaceVR/Build Settings")]
    public static void ShowWindow()
    {

        EditorWindow.GetWindow<AltspaceBuildWindow>(false, "AltspaceVR Build Settings");


    }

    public string CopyPasteControls(int controlID)
    {
        if(controlID == GUIUtility.keyboardControl)
        {
            if(Event.current.type == EventType.KeyUp && (Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Command))
            {
                if(Event.current.keyCode == KeyCode.C)
                {
                    Event.current.Use();
                    TextEditor tEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    tEditor.Copy();
                }
                else if(Event.current.keyCode == KeyCode.V)
                {
                    Event.current.Use();
                    TextEditor tEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    tEditor.Paste();
                    return tEditor.text;
                }
            }
        }

        return null;
    }

    public string TextField(string value, params GUILayoutOption[] options)
    {
        int textFieldID = GUIUtility.GetControlID("TextField".GetHashCode(), FocusType.Keyboard) + 1;
        if (textFieldID == 0)
            return value;

        value = CopyPasteControls(textFieldID) ?? value;

        return GUILayout.TextField(value);
    }

    public string PasswordField(string value, params GUILayoutOption[] options)
    {
        int textFieldID = GUIUtility.GetControlID("TextField".GetHashCode(), FocusType.Keyboard) + 1;
        if (textFieldID == 0)
            return value;

        value = CopyPasteControls(textFieldID) ?? value;

        return GUILayout.PasswordField(value,'*');
    }



    private void OnGUI()
    {
        

        m_teleportLayer = LayerMask.LayerToName(14);



        if (_userPrefs == null)
        {
            _userPrefs = (UserPreferences)Resources.Load("User Preferences");

            if (_userPrefs)
            {
                IsWindowsBuildTarget = _userPrefs.IsWindowsBuild;
                IsAndroidBuildTarget = _userPrefs.IsAndroidBuild;
                ProjectName = _userPrefs.ProjectName;
                saveLocation = _userPrefs.ExportPath;
                email = _userPrefs.Email;
                password = _userPrefs.Password;
            }
        }

        if (_userPrefs)
        {
            _userPrefs.IsWindowsBuild = IsWindowsBuildTarget;
            _userPrefs.IsAndroidBuild = IsAndroidBuildTarget;
            _userPrefs.ProjectName = ProjectName;
            //_userPrefs.ExportPath = saveLocation;
        }

        if (logoImage == null)
        {
            logoImage = Resources.Load("logo", typeof(Texture2D)) as Texture2D;
        }

        this.minSize = new Vector2(300, 600);
        this.maxSize = new Vector2(300, 600);

        GUILayout.Space(10);

        GUIStyle logoStyle = new GUIStyle();

        logoStyle.fixedWidth = 250;
        logoStyle.fixedHeight = 75;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(logoImage, logoStyle);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        CheckAutenticationCookie();



        if (!isUserSignedIn)
        {
            spaceTemplateNames.Clear();
            spaceTemplateNames.TrimExcess();

            spaceTemplates.Clear();
            spaceTemplates.TrimExcess();

            userHasTemplates = false;

            GUILayout.Space(10);

            GUILayout.Label("Log in", EditorStyles.centeredGreyMiniLabel);

            GUILayout.Space(10);

            GUILayout.Label("Email", EditorStyles.boldLabel);
            email = GUILayout.TextField(email);

            GUILayout.Space(5);

            GUILayout.Label("Password", EditorStyles.boldLabel);
            if(!showPassword)
            {
                password = PasswordField(password);
                
            }
            else
            {
                password = TextField(password);
            }

            GUILayout.Space(10);

            GUILayout.Label("Show Password?", EditorStyles.boldLabel);
            showPassword = GUILayout.Toggle(showPassword, "Yes");

            GUILayout.Space(10);

            GUILayout.Label("Remember Login Credentials?", EditorStyles.boldLabel);
            rememberUserLogin = GUILayout.Toggle(rememberUserLogin, "Yes");

            GUILayout.Space(10);

            if (GUILayout.Button("Sign In"))
            {
                SignInToAltspaceVR();
            }

            m_creatingTemplate = true;
        }
        else
        {

            GUILayout.Label("Tool", EditorStyles.boldLabel);
            //GUILayout.BeginHorizontal();
            //m_creatingTemplate = GUILayout.Toggle(m_creatingTemplate, "Template");
            //m_creatingKit = GUILayout.Toggle(m_creatingKit, "Kit");
            //GUILayout.EndHorizontal();

            m_projectTypeIndex = EditorGUILayout.Popup(m_projectTypeIndex, new string[] { "Template", "Kit" });

            if(m_projectTypeIndex == 0)
            {
                m_creatingTemplate = true;
                m_creatingKit = false;
            }
            else
            {
                m_creatingKit = true;
                m_creatingTemplate = false;
            }


            GUILayout.Space(20);

            m_panelScrollPosition = EditorGUILayout.BeginScrollView(m_panelScrollPosition);

            if(m_creatingTemplate)
            {
                

                GUILayout.Label("Template", EditorStyles.boldLabel);

                if (!userHasTemplates)
                {
                    GUILayout.Space(10);

                    GUILayout.Label("No templates loaded. Load your template(s), \nor create a new template");

                    GUILayout.Space(10);
                }

                if (GUILayout.Button("Load your Templates"))
                {
                    GetSpaceTemplates();
                }

                if (userHasTemplates)
                {
                    GUILayout.Space(20);



                    BeginWindows();

                    m_templateRect = GUILayout.Window(1, m_templateRect, (windowID) =>
                    {
                        var options = spaceTemplateNames.ToArray();
                        //selectedTemplateIndex = EditorGUILayout.Popup("Select a template", selectedTemplateIndex, options);

                        GUILayout.Space(10);

                        GUILayout.Label("Selected Template: " + options[selectedTemplateIndex], EditorStyles.boldLabel);

                        GUILayout.Space(10);

                        m_templateScrollPosition = EditorGUILayout.BeginScrollView(m_templateScrollPosition, GUILayout.Width(275), GUILayout.Height(100));

                        foreach (var o in options)
                        {
                            if (GUILayout.Button(o, EditorStyles.miniButton))
                            {
                                selectedTemplateIndex = options.ToList().FindIndex(x => x.Equals(o));
                            }
                        }
                        EditorGUILayout.EndScrollView();

                    }, "Loaded Templates", GUILayout.Width(250));

                    EndWindows();

                    GUILayout.Space(200);
                }
                else
                {
                    GUILayout.Space(20);
                }




                if (GUILayout.Button("Create a new Template"))
                {
                    Application.OpenURL("https://account.altvr.com/space_templates/new");
                }

                GUILayout.Space(20);

                if (userHasTemplates)
                {
                    GUILayout.Label("Platform Options", EditorStyles.boldLabel);

                    IsWindowsBuildTarget = GUILayout.Toggle(IsWindowsBuildTarget, "Build for Windows?");

                    IsAndroidBuildTarget = GUILayout.Toggle(IsAndroidBuildTarget, "Build for Android?");

                    GUILayout.Space(20);

                    if (GUILayout.Button("Build"))
                    {
                        var filePath = EditorUtility.SaveFilePanel("Save Template", saveLocation, "template", "");

                        FileInfo fileInfo = new FileInfo(filePath);

                        ProjectName = fileInfo.Name;

                        saveLocation = fileInfo.Directory.FullName;

                        if (string.IsNullOrEmpty(ProjectName))
                        {
                            Debug.LogError("Template name can't be empty. Please provide a valid name.");
                            return;
                        }

                        //check to see if there is already a zip.

                        if (File.Exists(Path.Combine(saveLocation, ProjectName + ".zip")))
                        {
                            File.Delete(Path.Combine(saveLocation, ProjectName + ".zip"));
                        }


                        //send info to environment export tool

                        EnvironmentExportTool.Instance.assetBundleName = ProjectName;
                        EnvironmentExportTool.Instance.buildPCAssetBundle = IsWindowsBuildTarget;
                        EnvironmentExportTool.Instance.buildAndroidAssetBundle = IsAndroidBuildTarget;

                        outAssetBundleName = ProjectName;

                        BuildNewEnvironment(() =>
                        {
                            if (!InitializeFilePaths(ProjectName))
                            {
                                Debug.LogError("Unable to save out scene");
                            }

                            EditorApplication.SaveScene(EditorApplication.currentScene);

                            //save out tmp scene
                            if (!SaveOutScene())
                            {
                                //Debug.LogError("Ran into error trying to save out scene.");
                                SendErrorMessageToSlack("Ran into error trying to save out scene.");
                            }
                            if (EnvironmentExportTool.Instance.buildPCAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.PC))
                            {
                                //Debug.LogError("Unable to save out asset bundle for PC (Windows).");
                                SendErrorMessageToSlack("Unable to save out asset bundle for PC (Windows).");
                            }
                            if (EnvironmentExportTool.Instance.buildMacAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.MAC))
                            {
                                //Debug.LogError("Unable to save out asset bundle for Mac.");
                                SendErrorMessageToSlack("Unable to save out asset bundle for Mac.");
                            }
                            if (EnvironmentExportTool.Instance.buildAndroidAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.ANDROID))
                            {
                                //Debug.LogError("Unable to save out asset bundle for Android.");
                                SendErrorMessageToSlack("Unable to save out asset bundle for Android.");
                            }


                            ZipAssetBundles(false);
                        });
                    }

                    GUILayout.Space(20);

                    if (GUILayout.Button("Build & Upload"))
                    {
                        saveLocation = Directory.GetParent(Application.dataPath).FullName;

                        ProjectName = "template";

                        //check to see if there is already a zip.

                        if (File.Exists(Path.Combine(saveLocation, ProjectName + ".zip")))
                        {
                            File.Delete(Path.Combine(saveLocation, ProjectName + ".zip"));
                        }


                        //send info to environment export tool

                        EnvironmentExportTool.Instance.assetBundleName = ProjectName;
                        EnvironmentExportTool.Instance.buildPCAssetBundle = IsWindowsBuildTarget;
                        EnvironmentExportTool.Instance.buildAndroidAssetBundle = IsAndroidBuildTarget;

                        outAssetBundleName = ProjectName;

                        BuildNewEnvironment(() =>
                        {
                            if (!InitializeFilePaths(ProjectName))
                            {
                                Debug.LogError("Unable to save out scene");
                            }

                            EditorApplication.SaveScene(EditorApplication.currentScene);

                            //save out tmp scene
                            if (!SaveOutScene())
                            {
                                //Debug.LogError("Ran into error trying to save out scene.");
                                SendErrorMessageToSlack("Ran into error trying to save out scene.");
                            }
                            if (EnvironmentExportTool.Instance.buildPCAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.PC))
                            {
                                //Debug.LogError("Unable to save out asset bundle for PC (Windows).");
                                SendErrorMessageToSlack("Unable to save out asset bundle for PC (Windows).");
                            }
                            if (EnvironmentExportTool.Instance.buildMacAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.MAC))
                            {
                                //Debug.LogError("Unable to save out asset bundle for Mac.");
                                SendErrorMessageToSlack("Unable to save out asset bundle for Mac.");
                            }
                            if (EnvironmentExportTool.Instance.buildAndroidAssetBundle && !SaveOutAssetBundle(EnvironmentExportTool.Platform.ANDROID))
                            {
                                //Debug.LogError("Unable to save out asset bundle for Android.");
                                SendErrorMessageToSlack("Unable to save out asset bundle for Android.");
                            }


                            ZipAssetBundles();
                        });

                    }

                    GUILayout.Space(20);
                }

                if (GUILayout.Button("Sign Out"))
                {
                    SignOutOfAltspaceVR();
                }
            }
            else if(m_creatingKit)
            {
                GUILayout.Space(10);

                GUILayout.Label("Kit Folder Name", EditorStyles.boldLabel);
                m_prefabFolderName = EditorGUILayout.TextField(m_prefabFolderName);

                GUILayout.Space(10);

                GUILayout.Label("Kits require a special formatting before they work correctly in Worlds. " +
                    "Select one or more gameobject before clicking this button to convert each gameobject to a Kit Prefab. " +
                    "Once the conversion is done, your new Kit Prefabs can be found in the prefabs folder under their Kit Folder Name.", EditorStyles.wordWrappedLabel);

                GUILayout.Space(10);

                GUILayout.Label("Kit Asset Name", EditorStyles.boldLabel);
                m_prefabName = EditorGUILayout.TextField(m_prefabName);

                GUILayout.Space(10);

                //GUILayout.Label("Rotation Override", EditorStyles.boldLabel);
                m_rotationOverride = EditorGUILayout.Vector3Field("Rotation Override", m_rotationOverride);

                GUILayout.Space(20);


                if (GUILayout.Button("Convert GameObject(s) to Kit Prefab"))
                {
                    if (string.IsNullOrEmpty(m_prefabFolderName))
                    {
                        nullProjectName = true;
                        return;
                    }

                    nullProjectName = false;

                    if(string.IsNullOrEmpty(m_prefabName))
                    {
                        nullAssetName = true;
                        return;
                    }

                    nullAssetName = false;

                    FormatGameObjectsToKit(m_prefabFolderName, m_prefabName);
                }

                GUILayout.Space(5);

                

                if (nullAssetName)
                {
                    GUILayout.Label("Please provide a Kit Asset Name.",EditorStyles.helpBox);
                }

                if(nullProjectName)
                {
                    
                    GUILayout.Label("Please provide a Kit Folder Name.",EditorStyles.helpBox);
                }

                //GUI.contentColor = Color.black;

                GUILayout.Space(20);


                GUILayout.Label("Click here to create a new kit. This button will direct you to the New Kits webpage.", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Create a new Kit"))
                {
                    Application.OpenURL("https://account.altvr.com/kits/new");
                }

                GUILayout.Space(20);

                GUILayout.Label("Platform Options", EditorStyles.boldLabel);

                BuildWindowsKit = GUILayout.Toggle(BuildWindowsKit, "Build Kit for Windows?");
                BuildAndroidKit = GUILayout.Toggle(BuildAndroidKit, "Build Kit for Android?");

                GUILayout.Space(10);

                GUILayout.Label("Package the generated screenshots into the Kit build.", EditorStyles.wordWrappedLabel);
                m_packageScreenshots = GUILayout.Toggle(m_packageScreenshots, "Package Generated Screenshots");


                GUILayout.Space(20);

                //reload to see if there are any prefabs in the prefab folder

                if(GUILayout.Button("Load Kit Prefab Directories"))
                {
                    string prefabPath = "Assets/Prefabs/";

                    m_loadedPrefabDirectories.Clear();
                    m_loadedPrefabDirectories.TrimExcess();

                    var directories = Directory.GetDirectories(prefabPath);

                    m_loadedPrefabDirectories = directories.ToList();

                    userHasPrefabs = directories.Length > 0;
                }


                if (userHasPrefabs)
                {
                    GUILayout.Space(20);

                    GUILayout.Label("Select a Prefab Kit folder to Build", EditorStyles.boldLabel);

                    GUILayout.Space(20);

                    m_prefabKitsScroll = EditorGUILayout.BeginScrollView(m_prefabKitsScroll,GUILayout.Height(100));

                    
                    foreach (var x in m_loadedPrefabDirectories)
                    {
                        EditorGUILayout.BeginHorizontal();

                        string dName = x.Remove(0, 15);
                        GUILayout.Label(dName,EditorStyles.boldLabel);
                        
                        if(GUILayout.Button("Build",GUILayout.Width(100)))
                        {
                            BuildKit(x, dName);
                        }

                        EditorGUILayout.EndHorizontal();
                    }


                    EditorGUILayout.EndScrollView();

                    GUILayout.Space(50);
                }

                GUILayout.Space(20);

                if (GUILayout.Button("Sign Out"))
                {
                    SignOutOfAltspaceVR();
                }

                GUILayout.Space(10);
            }
            

            EditorGUILayout.EndScrollView();
        }

        //curl -v -d "user[email]=myemail@gmail.com&user[password]=1234567" https://account.altvr.com/users/sign_in.json -c cookie
        GUILayout.Label("Template Uploader Version: 0.8.1", EditorStyles.centeredGreyMiniLabel);

        
    }



    private void SignInToAltspaceVR()
    {
        //check unity version
        //Debug.Log(Application.unityVersion);

        if (!Application.unityVersion.Equals("2018.1.9f2"))
        {
            //Debug.LogError("This is the wrong version of Unity. Please download version 2018.1.9f2 to continue");

            SendErrorMessageToSlack("This is the wrong version of Unity. Please download version 2018.1.9f2 to continue");

            return;
        }

        if(rememberUserLogin)
        {
            _userPrefs.Email = email;
            _userPrefs.Password = password;
            _userPrefs.RememberUserLogin = true;
        }
        else
        {
            _userPrefs.Email = string.Empty;
            _userPrefs.Password = string.Empty;
            _userPrefs.RememberUserLogin = false;
        }

        var curlLocation = Application.dataPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "curl.exe";
        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";
        var signInCMD = "&curl - v - d \"user[email]=amadden1990@gmail.com&user[password]=yungKnox21\" https://account.altvr.com/users/sign_in.json -c cookie";
        var setExePath = "/c start cd " + envPath;

        var batchCMD = new StringBuilder();
        batchCMD.Append("curl -v -d \"user[email]=")
            .Append(email)
            .Append("&user[password]=")
            .Append(password)
            .Append("\" https://account.altvr.com/users/sign_in.json -c cookie");

        var cmdLines = new List<string>();
        cmdLines.Add("echo off");
        cmdLines.Add("title Sign In to AltspaceVR");
        cmdLines.Add(batchCMD.ToString());

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "signin.bat"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "signin.bat");
        }

        File.WriteAllLines(envPath + Path.DirectorySeparatorChar + "signin.bat", cmdLines.ToArray());

        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = @envPath;
        process.FileName = "cmd.exe";
        process.Arguments = "/c signin.bat";
        System.Diagnostics.Process.Start(process);

    }

    private void SignOutOfAltspaceVR()
    {
        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        File.Delete(envPath + Path.DirectorySeparatorChar + "cookie");

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "signin.bat"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "signin.bat");
        }

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "upload.bat"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "upload.bat");
        }

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "space_templates"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "space_templates");
        }

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "space_templates.json"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "space_templates.json");
        }

        m_rotationOverride = Vector3.zero;
        m_loadedPrefabDirectories.Clear();

        m_prefabFolderName = string.Empty;
        m_prefabName = string.Empty;

        isUserSignedIn = false;

        userHasTemplates = false;
        userHasPrefabs = false;
        m_packageScreenshots = true;
        nullAssetName = false;
        nullProjectName = false;

        if(!rememberUserLogin)
        {
            _userPrefs.Email = string.Empty;
            _userPrefs.Password = string.Empty;
        }
    }

    private void CheckAutenticationCookie()
    {
        isUserSignedIn = File.Exists(Application.dataPath + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar + "cookie");
    }

    private void GetSpaceTemplates()
    {
        spaceTemplateNames.Clear();
        spaceTemplateNames.TrimExcess();

        spaceTemplates.Clear();
        spaceTemplates.TrimExcess();

        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        //get the template cookie
        if (File.Exists(envPath + Path.DirectorySeparatorChar + "space_templates"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "space_templates");
        }

        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = envPath;
        process.FileName = "cmd.exe";
        process.Arguments = "/c space_templates.bat";

        var currentProcess = System.Diagnostics.Process.Start(process);

        currentProcess.EnableRaisingEvents = true;

        currentProcess.WaitForExit();

        OnTemplatesLoaded();
    }

    private void GetKits()
    {
        kitNames.Clear();
        kitNames.TrimExcess();

        kits.Clear();
        kits.TrimExcess();

        
    }

    private void FormatGameObjectsToKit(string folderName,string kitName)
    {
        

        var prefabPath = "Assets/Prefabs/" + folderName + "/";
        var prefabPreviewPath = "Assets/Prefabs/" + folderName + "/Screenshots/";

        var gos = Selection.gameObjects.ToList();
        m_goal = gos.Count - 1;
        m_progress = 0;

        Thread.Sleep(1000);

        if (gos.Count > 1)
        {
           
            int index = 0;

            m_progressMessage = "Creating " + kitName + "_" + index.ToString("00");

            foreach(var go in gos)
            {
                string originalName = go.name;
                m_progress = index;

                EditorUtility.DisplayProgressBar("Converting Gameobjects to Kit Prefabs","Please wait...",gos.Count / gos.Count);

                var parent = new GameObject(kitName + "_" + index.ToString("00"));

                go.transform.SetParent(parent.transform);


                var child = parent.transform.GetChild(0);
                child.name = "model";
                

                var meshFilter = child.GetComponent<MeshFilter>() ? child.GetComponent<MeshFilter>() : null;

                var collider = new GameObject("collision");
                

                collider.layer = 14;

                collider.transform.SetParent(parent.transform);
                

                child.transform.SetAsFirstSibling();

                if (meshFilter != null)
                {
                    collider.AddComponent<MeshCollider>();
                    collider.GetComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
                    collider.GetComponent<MeshCollider>().convex = true;
                }
                else
                {
                    collider.AddComponent<BoxCollider>();
                }

                if (!Directory.Exists(prefabPath))
                {
                    Directory.CreateDirectory(prefabPath);
                }

                //var originalGO = Instantiate(go, go.transform.position, go.transform.rotation);
                //originalGO.name = originalName;

                for (int i = 0; i < parent.transform.childCount; i++)
                {
                    parent.transform.GetChild(i).transform.localPosition = new Vector3(0, 0, 0);
                    parent.transform.GetChild(i).transform.localEulerAngles = m_rotationOverride;
                    parent.transform.GetChild(i).transform.localScale = new Vector3(1,1,1);
                }

                var prefab = PrefabUtility.CreatePrefab(prefabPath + kitName + "_" + index.ToString("00") + ".prefab", parent);

                AssetDatabase.Refresh();


                

                DestroyImmediate(parent);

                

                var assetPreview = AssetPreview.GetAssetPreview(prefab);

                if (!Directory.Exists(prefabPreviewPath))
                {
                    Directory.CreateDirectory(prefabPreviewPath);
                }


                var referencePixel = assetPreview.GetPixel(0, 0);

                Texture2D screenshot = new Texture2D(128, 128, TextureFormat.RGBA32, false);

                for (var x = 0; x < 128; x++)
                {
                    for (var y = 0; y < 128; y++)
                    {
                        var pixel = assetPreview.GetPixel(x, y);

                        if (pixel == referencePixel)
                        {
                            screenshot.SetPixel(x, y, Color.clear);
                        }
                        else
                        {
                            screenshot.SetPixel(x, y, pixel);
                        }

                    }
                }

                screenshot.alphaIsTransparency = true;
                screenshot.Apply();

                var bytes = screenshot.EncodeToPNG();

                File.WriteAllBytes(prefabPreviewPath + kitName + "_" + index.ToString("00") + ".png", bytes);

                AssetDatabase.Refresh();

                index++;
            }
            
            EditorUtility.ClearProgressBar();

            m_goal = 0;
        }
        else if(gos.Count == 1)
        {
            string originalName = gos[0].name;

            var parent = new GameObject(kitName);

            gos[0].transform.SetParent(parent.transform);

            var child = parent.transform.GetChild(0);
            child.name = "model";

            var meshFilter = child.GetComponent<MeshFilter>() ? child.GetComponent<MeshFilter>() : null;

            var collider = new GameObject("collider");

            collider.layer = 14;

            collider.transform.SetParent(parent.transform);

            child.transform.SetAsFirstSibling();
            
            if(meshFilter != null)
            {
                collider.AddComponent<MeshCollider>();
                collider.GetComponent<MeshCollider>().sharedMesh = meshFilter.sharedMesh;
                collider.GetComponent<MeshCollider>().convex = true;
            }
            else
            {
                collider.AddComponent<BoxCollider>();
            }

            if(!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
            }

            //var originalGO = Instantiate(gos[0], gos[0].transform.position, gos[0].transform.rotation);
            //originalGO.name = originalName;

            for (int i = 0; i < parent.transform.childCount; i++)
            {
                parent.transform.GetChild(i).transform.localPosition = new Vector3(0, 0, 0);
                parent.transform.GetChild(i).transform.localEulerAngles = m_rotationOverride;
                parent.transform.GetChild(i).transform.localScale = new Vector3(1, 1, 1);
            }

            var prefab = PrefabUtility.CreatePrefab(prefabPath + kitName + ".prefab", parent);

            AssetDatabase.Refresh();

            

            DestroyImmediate(parent);

            

            var assetPreview = AssetPreview.GetAssetPreview(prefab);

            if (!Directory.Exists(prefabPreviewPath))
            {
                Directory.CreateDirectory(prefabPreviewPath);
            }


            var referencePixel = assetPreview.GetPixel(0, 0);

            Texture2D screenshot = new Texture2D(128, 128, TextureFormat.RGBA32, false);

            for(var x = 0; x < 128; x++)
            {
                for(var y = 0; y < 128; y++)
                {
                    var pixel = assetPreview.GetPixel(x, y);

                    if(pixel == referencePixel)
                    {
                        screenshot.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        screenshot.SetPixel(x, y, pixel);
                    }

                }
            }

            screenshot.alphaIsTransparency = true;
            screenshot.Apply();

            var bytes = screenshot.EncodeToPNG();

            File.WriteAllBytes(prefabPreviewPath + kitName + ".png", bytes);

            AssetDatabase.Refresh();
        }

        ShowNotification(new GUIContent("Kit Assets Created!"));
    }

    private void OnTemplatesLoaded()
    {
        Debug.Log("get templates");
        ParseSpaceTemplateFromJSON();
    }

    private void CurrentProcess_Exited(object sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void ParseSpaceTemplateFromJSON()
    {

        var envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        if (!File.Exists(envPath + Path.DirectorySeparatorChar + "space_templates.json"))
            return;

        string jsonText = string.Empty;

        try
        {
            using (StreamReader reader = File.OpenText(envPath + Path.DirectorySeparatorChar + "space_templates.json"))
            {
                string s = string.Empty;
                while ((s = reader.ReadLine()) != null)
                {
                    jsonText += s;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning(ex.ToString());
        }

        SimpleJSON.JSONNode data = SimpleJSON.JSON.Parse(jsonText);

        var templates = data["space_templates"].AsArray.Linq.ToList();

        var templateNames = new List<string>();
        var templateIds = new List<string>();



        foreach (var x in templates)
        {

            templateNames.Add(x.Value["name"].Value);

            templateIds.Add(x.Value["space_template_id"].Value);
        }

        for (int i = 0; i < templateNames.Count; i++)
        {
            spaceTemplates.Add(new SpaceTemplate(templateNames[i], templateIds[i]));
            spaceTemplateNames.Add(templateNames[i]);
        }

        Debug.Log("Space Templates: " + spaceTemplateNames.Count);

        if (spaceTemplates.Count > 0)
        {

            userHasTemplates = true;
        }
        else
        {
            userHasTemplates = false;
        }

    }


    private string RemoveSpaces(string value)
    {
        return value.Replace(" ", string.Empty);
    }

    private void BuildNewEnvironment(Action onComplete)
    {


        if (GameObject.Find("Environment") == null)
        {
            //get all the gameobjects in the scene
            if (scene != null)
            {
                scene.Clear();
                scene.TrimExcess();
            }

            scene = FindObjectsOfType<GameObject>().ToList();

            if (scene.Count == 0)
            {
                Debug.LogError("There is nothing to export");
                return;
            }

            //remove the gameobjects that already have parents.
            var removeItems = new List<GameObject>();
            foreach (var go in scene)
            {
                if (go.transform.parent != null)
                {
                    removeItems.Add(go);
                }

            }

            foreach (var go in removeItems)
            {
                scene.Remove(go);
            }

            var environment = new GameObject("Environment");

            scene.ForEach(go => go.transform.SetParent(environment.transform));

            environment.layer = 14;

            for (int i = 0; i < environment.transform.childCount; i++)
            {
                var child = environment.transform.GetChild(i).gameObject;

                child.gameObject.layer = 14;

                if (child.transform.childCount > 0)
                {
                    for (int x = 0; x < child.transform.childCount; x++)
                    {
                        var superChild = child.transform.GetChild(x).gameObject;

                        superChild.layer = 14;

                    }
                }
            }
        }
        else
        {
            var unParentedObjects = FindObjectsOfType<GameObject>().ToList().FindAll(x => x.transform.parent == null);

            var environment = GameObject.Find("Environment");

            foreach (var x in unParentedObjects)
            {
                x.transform.SetParent(environment.transform);
            }

            environment.layer = 14;

            for (int i = 0; i < environment.transform.childCount; i++)
            {
                var child = environment.transform.GetChild(i).gameObject;

                child.gameObject.layer = 14;

                if (child.transform.childCount > 0)
                {
                    for (int x = 0; x < child.transform.childCount; x++)
                    {
                        var superChild = child.transform.GetChild(x).gameObject;

                        superChild.layer = 14;

                    }
                }
            }
        }


        var cameras = FindObjectsOfType<Camera>().ToList();

        foreach (var x in cameras)
            DestroyImmediate(x.gameObject);



        if (onComplete != null) onComplete();
    }

    private void RemoveEnvironment()
    {
        foreach (var go in scene)
        {
            go.transform.parent = null;
        }

        var environment = GameObject.Find("Environment");

        if (environment != null)
        {
            DestroyImmediate(environment);
        }
    }

    public static EnvironmentExportTool.Platform GetCurrentPlatform()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
            return EnvironmentExportTool.Platform.PC;
        else
            return EnvironmentExportTool.Platform.MAC;
    }


    public bool InitializeFilePaths(string assetBundleName)
    {
        if (assetBundleName == "")
        {
            Debug.LogError("Must provide a name or path for unity file to be save and uploaded!");
            return false;
        }
        outAssetBundleName = assetBundleName;
        string exportDirectory = GetExportDirectory();
        if (!Directory.Exists(exportDirectory))
        {
            Directory.CreateDirectory(exportDirectory);
        }

        tempSceneFile = exportDirectory + Path.DirectorySeparatorChar + assetBundleName + ".unity";

        return true;
    }

    private string GetExportDirectory()
    {
        string[] exportDirParts = { "Assets", "Altspace", "Export" };
        return string.Join(Path.DirectorySeparatorChar.ToString(), exportDirParts);
    }

    private string AssetBundleDirForPlatform(EnvironmentExportTool.Platform platform)
    {
        var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        var tempPath = projectDirectory + Path.DirectorySeparatorChar + "AltspaceTemp";
        //create the head directory
        if (!Directory.Exists(Path.Combine(tempPath, "AssetBundles")))
        {
            Directory.CreateDirectory(Path.Combine(tempPath, "AssetBundles"));
        }

        string exportDir = string.Empty;
        if (platform == EnvironmentExportTool.Platform.MAC)
        {
            exportDir = macAssetBundleFolder;
        }
        else if (platform == EnvironmentExportTool.Platform.ANDROID)
        {
            exportDir = androidAssetBundleFolder;
        }

        return exportDir != string.Empty ? tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + exportDir + Path.DirectorySeparatorChar :
            tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar;
    }

    public bool SaveOutScene()
    {
        var originalActiveScene = EditorSceneManager.GetActiveScene();
        string originalActiveScenePath = originalActiveScene.path;
        EditorSceneManager.SaveScene(originalActiveScene);
        bool success = false;
        success = EditorSceneManager.SaveScene(originalActiveScene, tempSceneFile);

        if (success)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(); // necessary?
                                     // EditorSceneManager.CloseScene(originalActiveScene, true); //TODO UNITYUPGRADE
            var tempScene = EditorSceneManager.OpenScene(tempSceneFile);
            EditorSceneManager.SetActiveScene(tempScene);
            DestroyImmediate(GameObject.Find("Tools"));
            EditorSceneManager.SaveScene(tempScene);
            // EditorSceneManager.CloseScene(tempScene, true); //TODO UNITYUPGRADE
            originalActiveScene = EditorSceneManager.OpenScene(originalActiveScenePath);
            EditorSceneManager.SetActiveScene(originalActiveScene);
        }
        return success;
    }

    public bool SaveOutAssetBundle(EnvironmentExportTool.Platform platform, bool shouldCompress = true)
    {
        if (tempSceneFile.Length != 0)
        {
            string[] scenes = { tempSceneFile };
            AssetBundleBuild[] buildMap = { new AssetBundleBuild() };

            buildMap[0].assetNames = scenes;
            buildMap[0].assetBundleName = outAssetBundleName;
            buildMap[0].assetBundleVariant = "unity2018_1";
            BuildTarget target = BuildTarget.StandaloneWindows;
            if (platform == EnvironmentExportTool.Platform.MAC)
            {
                //target = BuildTarget.StandaloneOSX;
            }
            else if (platform == EnvironmentExportTool.Platform.ANDROID)
            {
                target = BuildTarget.Android;
            }
            string outputDir = AssetBundleDirForPlatform(platform);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            BuildAssetBundleOptions options = shouldCompress ? BuildAssetBundleOptions.None : BuildAssetBundleOptions.UncompressedAssetBundle;

            BuildPipeline.BuildAssetBundles(outputDir, buildMap, options, target);
            



            //Debug.Log("Asset Bundle exported to: " + outputDir + ".");
            return true;
        }

        return false;
    }

    private void RenameAssetBundles(Action onComplete)
    {
        var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
        var tempPath = projectDirectory + Path.DirectorySeparatorChar + "AltspaceTemp";

        string[] windowFiles = new string[] { };
        string[] androidFiles = new string[] { };

        if (Directory.Exists(Path.Combine(tempPath, "AssetBundles")))
            windowFiles = Directory.GetFiles(Path.Combine(tempPath, "AssetBundles"));

        if (Directory.Exists(Path.Combine(Path.Combine(tempPath, "AssetBundles"), "Android")))
            androidFiles = Directory.GetFiles(Path.Combine(Path.Combine(tempPath, "AssetBundles"), "Android"));


        foreach (var file in windowFiles)
        {
            Debug.Log("Renaming: " + file);

            if (Path.GetFileName(file).Equals("AssetBundles"))
            {
                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals("AssetBundles.manifest"))
            {
                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals(ProjectName + ".unity2018_1"))
            {

                var newFileName = tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + ProjectName;


                //File.Move(file, newFileName);
                File.Copy(@file, @newFileName);

                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals(ProjectName + ".unity2018_1.manifest"))
            {
                var newFileName = tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + ProjectName + ".manifest";

                File.Copy(@file, @newFileName);
                File.Delete(@file);
            }
        }

        foreach (var file in androidFiles)
        {
            Debug.Log("Renaming: " + file);

            if (Path.GetFileName(file).Equals("Android"))
            {
                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals("Android.manifest"))
            {
                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals(ProjectName + ".unity2018_1"))
            {

                var newFileName = tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + "Android" + Path.DirectorySeparatorChar + ProjectName;


                //File.Move(file, newFileName);
                File.Copy(@file, @newFileName);

                File.Delete(@file);
            }

            else if (Path.GetFileName(file).Equals(ProjectName + ".unity2018_1.manifest"))
            {
                var newFileName = tempPath + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + "Android" + Path.DirectorySeparatorChar + ProjectName + ".manifest";

                File.Copy(@file, @newFileName);
                File.Delete(@file);
            }
        }

        if (onComplete != null) onComplete();
    }


    private void BuildKit(string path, string fileName)
    {

        var saveLocation = Directory.GetParent(Application.dataPath).FullName;


        //remove any shitty folders that could screw this crap up
        if (Directory.Exists(saveLocation + Path.DirectorySeparatorChar + "AssetBundle"))
        {
            Directory.Delete(@saveLocation + Path.DirectorySeparatorChar + "AssetBundle", true);
        }

        if (Directory.Exists(saveLocation + Path.DirectorySeparatorChar + fileName))
        {
            Directory.Delete(@saveLocation + Path.DirectorySeparatorChar + fileName, true);
        }


        AssetBundleBuild[] abb = { new AssetBundleBuild() };
        abb[0].assetBundleName = fileName;

        var fileNames = Directory.GetFiles("Assets/Prefabs/" + fileName);

        abb[0].assetNames = fileNames;

        if(!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + "AssetBundle"))
        {
            Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + "AssetBundle");
        }

        if(BuildWindowsKit)
        {
            BuildPipeline.BuildAssetBundles(saveLocation + Path.DirectorySeparatorChar + "AssetBundle", abb, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
        }

        if(BuildAndroidKit)
        {
            if(!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + "AssetBundle" + Path.DirectorySeparatorChar + "Android"))
            {
                Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + "AssetBundle" + Path.DirectorySeparatorChar + "Android"); 
            }

            BuildPipeline.BuildAssetBundles(saveLocation + Path.DirectorySeparatorChar + "AssetBundle" + Path.DirectorySeparatorChar + "Android", abb, BuildAssetBundleOptions.None, BuildTarget.Android);
        }


        //rename the parent folder and delete the unessessary items

        //get the screenshots folder
        var screenshotsFolderPath = "Assets/Prefabs/" + fileName + "/Screenshots";

        List<string> screenshotList = new List<string>();

        if(m_packageScreenshots)
        {
            screenshotList = Directory.GetFiles(screenshotsFolderPath).ToList();
        }
      
        if (!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles"))
            Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles");

        if(m_packageScreenshots)
        {
            if (!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "Screenshots"))
                Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "Screenshots");
        }

        if(BuildAndroidKit)
        {
            if (!Directory.Exists(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + "Android"))
                Directory.CreateDirectory(saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles" + Path.DirectorySeparatorChar + "Android");
        }
        

        var oldLocation = saveLocation + Path.DirectorySeparatorChar + "AssetBundle";
        var newLocation = saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "AssetBundles";
        var screenshotLocation = saveLocation + Path.DirectorySeparatorChar + fileName + Path.DirectorySeparatorChar + "Screenshots";

        if(BuildWindowsKit)
        {
            var windowFiles = Directory.GetFiles(oldLocation);

            foreach (var x in windowFiles)
            {
                var fileShortName = x.Remove(0, oldLocation.Length);

                File.Copy(@x, @newLocation + fileShortName);
                File.Delete(@x);
            }
        }

        if(m_packageScreenshots)
        {
            foreach(var x in screenshotList)
            {
                var fileShortName = x.Remove(0, screenshotsFolderPath.Length);

                File.Copy(@x, screenshotLocation + fileShortName);
            }

            var newScreenshotList = Directory.GetFiles(screenshotLocation);

            foreach(var x in newScreenshotList)
            {
                if(x.Contains(".meta"))
                {
                    File.Delete(@x);
                }
            }
        }
        
        if(BuildAndroidKit)
        {
            var androidFiles = Directory.GetFiles(oldLocation + Path.DirectorySeparatorChar + "Android");

            foreach (var x in androidFiles)
            {
                var fileShortName = x.Remove(0, oldLocation.Length + 8);
                File.Copy(@x, @newLocation + Path.DirectorySeparatorChar + "Android" + Path.DirectorySeparatorChar + fileShortName);
                File.Delete(@x);
            }
        }
        
        if(Directory.Exists(oldLocation + Path.DirectorySeparatorChar + "Android"))
            Directory.Delete(@oldLocation + Path.DirectorySeparatorChar + "Android",true);

        if(Directory.Exists(oldLocation))
            Directory.Delete(@oldLocation,true);

        ZipFile zipFile = new ZipFile();
        zipFile.AddDirectory(saveLocation + Path.DirectorySeparatorChar + fileName);

        if(File.Exists(saveLocation + Path.DirectorySeparatorChar + fileName.ToLower() + ".zip"))
        {
            File.Delete(saveLocation + Path.DirectorySeparatorChar + fileName.ToLower() + ".zip");
        }

        zipFile.Save(saveLocation + Path.DirectorySeparatorChar + fileName.ToLower() + ".zip");

        Directory.Delete(newLocation, true);

        System.Threading.Thread.Sleep(3000);

        Directory.Delete(saveLocation + Path.DirectorySeparatorChar + fileName, true);

        System.Diagnostics.Process.Start("explorer.exe", saveLocation);
    }


    private void ZipAssetBundles(bool upload = true)
    {
        RenameAssetBundles(() =>
        {
            //string assetPath = Path.Combine(saveLocation, "AssetBundles");
            string envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";
            var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
            var tempPath = projectDirectory + Path.DirectorySeparatorChar + "AltspaceTemp";

            ZipFile zipFile = new ZipFile();
            zipFile.AddDirectory(tempPath);

            zipFile.Save(saveLocation + Path.DirectorySeparatorChar + ProjectName + ".zip");

            Directory.Delete(tempPath, true);
            //write an async method here to wait for the file to be done zipping.

            System.Threading.Thread.Sleep(3000);

            if(upload)
            {
                UploadToAltspaceVR();
            }
            else
            {
                ShowNotification(new GUIContent("Build Complete!"));
                //EditorUtility.RevealInFinder(saveLocation + Path.DirectorySeparatorChar);
                System.Diagnostics.Process.Start("explorer.exe", saveLocation);
            }
           
        });
    }


    private void UploadToAltspaceVR()
    {
        if (spaceTemplates == null || spaceTemplates.Count == 0)
        {
            SendErrorMessageToSlack("You are trying to upload your space without a target template. Please load an existing template, or create a new one.");

            return;
        }

        string envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";
        //string cmd = "curl - v - b cookie - X PUT - F 'space_template[zip]=@jimmycube.zip' https://account.altvr.com/api/space_templates/<space_template_id>.json";

        var cmd = new StringBuilder();
        cmd.Append("curl -v -b cookie -X PUT -F ")
            .Append("\"space_template[zip]=@")
            .Append(saveLocation + Path.DirectorySeparatorChar + ProjectName + ".zip\" ")
            .Append("https://account.altvr.com/api/space_templates/")
            .Append(spaceTemplates[selectedTemplateIndex].Space_template_id)
            .Append(".json");

        List<string> batCMDs = new List<string>();

        batCMDs.Add("echo off");
        batCMDs.Add(cmd.ToString());

        File.WriteAllLines(envPath + Path.DirectorySeparatorChar + "upload.bat", batCMDs.ToArray());


        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = envPath;
        process.FileName = "cmd.exe";
        process.Arguments = "/c upload.bat";
        var processInfo = System.Diagnostics.Process.Start(process);
        processInfo.WaitForExit();

        ShowNotification(new GUIContent("Upload Complete!"));
        //SendErrorMessageToSlack(processInfo.StandardError.ReadToEnd());

    }

    /// <summary>
    /// Send an error message to AltspaceVR's unity-uploader-errors channel on Slack.
    /// </summary>
    /// <param name="errorMessage"></param>
    public void SendErrorMessageToSlack(string errorMessage)
    {
        string envPath = Application.dataPath + Path.DirectorySeparatorChar + "Plugins";

        var cmd = new StringBuilder();
        cmd.Append("curl -X POST --data-urlencode \"payload={\\\"channel\\\": \\\"#unity-uploader-errors\\\", \\\"username\\\": \\\"webhookbot\\\", \\\"text\\\": \\\"")
            .Append(errorMessage)
            .Append("\\\"}\" ")
            .Append("https://hooks.slack.com/services/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");

        List<string> batCMDs = new List<string>();

        batCMDs.Add("echo off");
        batCMDs.Add(cmd.ToString());

        if (File.Exists(envPath + Path.DirectorySeparatorChar + "error.bat"))
        {
            File.Delete(envPath + Path.DirectorySeparatorChar + "error.bat");
        }

        File.WriteAllLines(envPath + Path.DirectorySeparatorChar + "error.bat", batCMDs.ToArray());

        var process = new System.Diagnostics.ProcessStartInfo();
        process.WorkingDirectory = envPath;
        process.FileName = "cmd.exe";
        process.Arguments = "/c error.bat";
        System.Diagnostics.Process.Start(process);

        Debug.LogError(errorMessage);
    }
}
#endif



