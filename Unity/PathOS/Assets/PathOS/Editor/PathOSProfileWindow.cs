using System.Collections;
using System.Collections.Generic;
using System.IO;
using PathOS;
using UnityEditor;
using UnityEngine;

/*
PathOSProfileWindow.cs
PathOSProfileWindow (c) Nine Penguins (Samantha Stahlke) 2019
*/

public class PathOSProfileWindow : EditorWindow
{
    public const string editorPrefsID = "PathOSAgentProfiles";
    public static char[] newlineSep = { '\n' };

    public static List<AgentProfile> profiles;

    private List<string> profileNames = new List<string>();

    private int profileIndex = 0;
    private AgentProfile curProfile = new AgentProfile();

   // [MenuItem("Window/PathOS Profiles")]
   // public static void ShowWindow()
   // {
   //     EditorWindow window = EditorWindow.GetWindow(typeof(PathOSProfileWindow), true,
   //         "PathOS Agent Profiles");
   //
   //     window.minSize = new Vector2(420.0f, 345.0f);
   // }

    private void OnEnable()
    {
        profiles = new List<AgentProfile>();
        ReadPrefsData();

        if (profileIndex < profiles.Count)
            curProfile.Copy(profiles[profileIndex]);
    }

    private void OnDisable()
    {
        WritePrefsData();
    }

    private void OnDestroy()
    {
        WritePrefsData();
    }

    private static void WritePrefsData()
    {
        string prefsData = GetProfileString();
        EditorPrefs.SetString(editorPrefsID, prefsData);
    }

    private static string GetProfileString()
    {
        string profileJson = "";

        for (int i = 0; i < profiles.Count; ++i)
        {
            profileJson += JsonUtility.ToJson(profiles[i], false);
            profileJson += "\n";
        }

        return profileJson;
    }

    public static void ReadPrefsData()
    {
        if(EditorPrefs.HasKey(editorPrefsID))
        {
            string prefsData = EditorPrefs.GetString(editorPrefsID);
            profiles = ReadProfileString(prefsData);
        }
        else
        {
            Debug.Log("No PathOS profile data found in Editor preferences. Loading defaults...");
            profiles = new List<AgentProfile>();

            string defaultPrefsFile = Application.dataPath
                + "/PathOS/Settings/default-profiles.profiles";

            LoadProfilesFromFile(defaultPrefsFile);

            WritePrefsData();
        }
    }

    private static List<AgentProfile> ReadProfileString(string json)
    {
        List<AgentProfile> result = new List<AgentProfile>();

        string[] profileJson = json.Split(newlineSep,
            System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < profileJson.Length; ++i)
        {
            AgentProfile newProfile = JsonUtility.FromJson(
                profileJson[i], typeof(AgentProfile)) as AgentProfile;

            result.Add(newProfile);
        }

        return result;
    }

    private static void LoadProfilesFromFile(string path)
    {
        if (path == "")
            return;

        if(!File.Exists(path) || path.Length < 8 
            || path.Substring(path.Length - 8) != "profiles")
        {
            NPDebug.LogError("Could not load agent profiles! " +
                "PathOS agent profiles can only be imported from a " +
                "valid local .profiles file.");

            return;
        }

        StreamReader sr = new StreamReader(path);
        string profileJson = "";
        string line = "";

        while((line = sr.ReadLine()) != null)
        {
            profileJson += line + "\n";
        }

        sr.Close();

        profiles = ReadProfileString(profileJson);
        NPDebug.LogMessage("Loaded PathOS agent profiles.", typeof(PathOSProfileWindow));
    }

    private void WriteProfilesToFile(string path)
    {
        if (path == "")
            return;

        StreamWriter sw = new StreamWriter(path);
        sw.Write(GetProfileString());
        sw.Close();
    }

    public void OnWindowOpen()
    {
        if(GUILayout.Button("Import Profiles..."))
        {
            string importPath = EditorUtility.OpenFilePanel("Import Agent Profiles...",
                Application.dataPath, "profiles");

            LoadProfilesFromFile(importPath);
            profileIndex = 0;

            if (profileIndex > profiles.Count)
                curProfile.Copy(profiles[profileIndex]);
        }

        if(GUILayout.Button("Export Profiles..."))
        {
            string exportPath = EditorUtility.SaveFilePanel("Export Agent Profiles...",
                Application.dataPath, "my-agent-profiles", "profiles");

            WriteProfilesToFile(exportPath);
        }

        profileNames.Clear();

        for(int i = 0; i < profiles.Count; ++i)
        {
            profileNames.Add(profiles[i].name);
        }

        if(profileNames.Count == 0)
            profileNames.Add("--");

        EditorGUILayout.BeginHorizontal();

        bool copyProfile = false;

        EditorGUI.BeginChangeCheck();
        profileIndex = EditorGUILayout.Popup("View Profile: ", 
            profileIndex, profileNames.ToArray());

        copyProfile = EditorGUI.EndChangeCheck();

        if(copyProfile && profileIndex < profiles.Count)
        {
            curProfile.Copy(profiles[profileIndex]);
        }

        if(GUILayout.Button("Add New"))
        { 
            profiles.Add(new AgentProfile());
            profileIndex = profiles.Count - 1;
            curProfile.Clear();
        }

        EditorGUILayout.EndHorizontal();

        if(profileIndex < profiles.Count)
        {
            curProfile.name = EditorGUILayout.TextField("Profile Name:", curProfile.name);

            PathOS.EditorUI.FullMinMaxSlider("Experience Scale",
                ref curProfile.expRange.min, ref curProfile.expRange.max);

            for (int i = 0; i < curProfile.heuristicRanges.Count; ++i)
            {
                HeuristicRange hr = curProfile.heuristicRanges[i];

                PathOS.EditorUI.FullMinMaxSlider(UI.heuristicLabels[hr.heuristic],
                    ref hr.range.min, ref hr.range.max);
            }

            if (GUILayout.Button("Apply Changes"))
                profiles[profileIndex].Copy(curProfile);

            if (GUILayout.Button("Delete Profile"))
            {
                profiles.RemoveAt(profileIndex);

                if (profileIndex < profiles.Count)
                    curProfile.Copy(profiles[profileIndex]);
            }
        }
        

    }
}
