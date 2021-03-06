﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ionic.Zip;

public class QuestLoader {

    public static Dictionary<string, Quest> GetQuests()
    {
        Dictionary<string, Quest> quests = new Dictionary<string, Quest>();

        Game game = Game.Get();
        string dataLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "/Valkyrie";
        mkDir(dataLocation);
        List<string> questDirectories = GetQuests(dataLocation);


        if (Application.isEditor)
        {
            dataLocation = Application.dataPath + "/../quests/";
        }
        else
        {
            dataLocation = Application.dataPath + "/quests/";
        }
        questDirectories.AddRange(GetQuests(dataLocation));

        questDirectories.AddRange(GetQuests(Path.GetTempPath() + "Valkyrie"));

        foreach (string p in questDirectories)
        {
            Quest q = new Quest(p);
            if (!q.name.Equals("") && q.type.Equals(Game.Get().gameType.TypeName()))
            {
                bool expansionsOK = true;
                foreach (string s in q.packs)
                {
                    if (!game.cd.GetEnabledPackIDs().Contains(s))
                    {
                        expansionsOK = false;
                    }
                }
                if (expansionsOK) quests.Add(p, q);
            }
        }

        return quests;
    }

    public static Dictionary<string, Quest> GetUserQuests()
    {
        Dictionary<string, Quest> quests = new Dictionary<string, Quest>();

        CleanTemp();

        string dataLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "/Valkyrie";
        mkDir(dataLocation);
        List<string> questDirectories = GetQuests(dataLocation);

        questDirectories.AddRange(GetQuests(Path.GetTempPath() + "Valkyrie"));

        foreach (string p in questDirectories)
        {
            Quest q = new Quest(p);
            if (!q.name.Equals("") && q.type.Equals(Game.Get().gameType.TypeName()))
            {
                quests.Add(p, q);
            }
        }

        return quests;
    }

    public static Dictionary<string, Quest> GetUserUnpackedQuests()
    {
        Dictionary<string, Quest> quests = new Dictionary<string, Quest>();

        string dataLocation = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + "/Valkyrie";
        mkDir(dataLocation);
        List<string> questDirectories = GetQuests(dataLocation);

        foreach (string p in questDirectories)
        {
            Quest q = new Quest(p);
            if (!q.name.Equals("") && q.type.Equals(Game.Get().gameType.TypeName()))
            {
                quests.Add(p, q);
            }
        }

        return quests;
    }

    public static List<string> GetQuests(string path)
    {
        List<string> quests = new List<string>();

        if (!Directory.Exists(path))
        {
            return quests;
        }

        List<string> questDirectories = DirList(path);
        foreach (string p in questDirectories)
        {
            // All packs must have a quest.ini, otherwise ignore
            if (File.Exists(p + "/quest.ini"))
            {
                    quests.Add(p);
            }
        }

        string[] archives = Directory.GetFiles(path, "*.valkyrie", SearchOption.AllDirectories);
        foreach (string f in archives)
        {
            mkDir(Path.GetTempPath() + "/Valkyrie");
            string extractedPath = Path.GetTempPath() + "Valkyrie/" + Path.GetFileName(f);
            if (Directory.Exists(extractedPath))
            {
                try
                {
                    Directory.Delete(extractedPath, true);
                }
                catch (System.Exception)
                {
                    Debug.Log("Warning: Unable to remove old temporary files: " + extractedPath);
                }
            }
            mkDir(extractedPath);

            try
            {
                ZipFile zip = ZipFile.Read(f);
                zip.ExtractAll(extractedPath);
            }
            catch (System.Exception)
            {
                Debug.Log("Warning: Unable to read file: " + extractedPath);
            }
        }

        return quests;
    }

    public static void mkDir(string p)
    {
        if (!Directory.Exists(p))
        {
            try
            {
                Directory.CreateDirectory(p);
            }
            catch (System.Exception)
            {
                Debug.Log("Error: Unable to create directory: " + p);
                Application.Quit();
            }
        }
    }

    public static List<string> DirList(string path)
    {
        return DirList(path, new List<string>());
    }

    public static List<string> DirList(string path, List<string> l)
    {
        List<string> list = new List<string>(l);

        foreach (string s in Directory.GetDirectories(path))
        {
            list = DirList(s, list);
            list.Add(s);
        }

        return list;
    }

    public static void CleanTemp()
    {
        // Nothing to do if no temporary files
        if (!Directory.Exists(Path.GetTempPath() + "/Valkyrie"))
        {
            return;
        }

        try
        {
            Directory.Delete(Path.GetTempPath() + "/Valkyrie", true);
        }
        catch (System.Exception)
        {
            Debug.Log("Warning: Unable to remove temporary files.");
        }
    }

    public class Quest
    {
        public string path;
        public string name = "";
        public string description;
        public string type;
        public string[] packs;

        public Quest(string p)
        {
            path = p;
            IniData d = IniRead.ReadFromIni(p + "/quest.ini");
            if (d == null)
            {
                Debug.Log("Warning: Invalid quest:" + p + "/quest.ini!");
                return;
            }

            type = d.Get("Quest", "type");
            if (type.Length == 0)
            {
                // Default to D2E to support historical quests
                type = "D2E";
            }

            name = d.Get("Quest", "name");
            if (name.Equals(""))
            {
                Debug.Log("Warning: Failed to get name data out of " + p + "/content_pack.ini!");
                return;
            }

            if (d.Get("Quest", "packs").Length > 0)
            {
                packs = d.Get("Quest", "packs").Split(' ');
            }
            else
            {
                packs = new string[0];
            }

            // Missing description is OK
            description = d.Get("Quest", "description");
        }
    }
}
