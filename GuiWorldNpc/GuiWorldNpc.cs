using Harmony12;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityModManagerNet;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection.Emit;
using System.Text;

namespace GuiWorldNpc
{
    public class Settings : UnityModManager.ModSettings
    {
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            UnityModManager.ModSettings.Save<Settings>(this, modEntry);
        }
        public float scrollSpeed = 15;//滑动速度
        public bool open = true; //使用鬼的世界NPC
        public int numberOfColumns = 1;//一行显示几个

        public float a = 10;
        public float b = 10;
        public float c = 20;
    }
    public static class Main
    {
        public static bool OnChangeList;
        public static bool showNpcInfo;

        public static bool enabled;
        public static Settings settings;
        public static UnityModManager.ModEntry.ModLogger Logger;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            #region 基础设置
            settings = Settings.Load<Settings>(modEntry);
            Logger = modEntry.Logger;
            modEntry.OnToggle = OnToggle;
            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            HarmonyInstance harmony = HarmonyInstance.Create(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            #endregion
            return true;
        }

        static string title = "鬼的世界NPC";
        public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;
            return true;
        }
        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            GUILayout.Label(title, GUILayout.Width(300));
            Main.settings.open = GUILayout.Toggle(Main.settings.open, "鬼的世界NPC");
            Main.settings.open = !GUILayout.Toggle(!Main.settings.open, "原版世界NPC");
            GUILayout.Label("滑动速度↓↓↓↓↓");
            int speed;
            if(int.TryParse(GUILayout.TextField(settings.scrollSpeed.ToString()), out speed))
            {
                settings.scrollSpeed = speed;
            }
            //GUILayout.Label("每行数量↓↓↓↓↓<color=#F63333>修改后需要重启游戏</color>");
            //int count;
            //if(int.TryParse(GUILayout.TextField(settings.numberOfColumns.ToString()), out count))
            //{
            //    settings.numberOfColumns = count;
            //}
            GUILayout.Label("<color=#F63333>如果出现BUG影响正常使用请在游戏目录Mods文件夹下删除GuiWorldNpc然后等待更新</color>");

            //float.TryParse(GUILayout.TextField(settings.a.ToString()), out settings.a);
            //float.TryParse(GUILayout.TextField(settings.b.ToString()), out settings.b);
            //float.TryParse(GUILayout.TextField(settings.c.ToString()), out settings.c);
            //GUILayout.Label(Screen.width.ToString()+ Screen.height.ToString());
        }


        [HarmonyPatch(typeof(SetPlaceActor), "ShowEventMassage", new Type[] { typeof(int), typeof(int) })]
        public static class SetPlaceActor_ShowEventMassage_Patch
        {
            public static bool Prefix()
            {
                Main.Logger.Log("点击了NPC");
                var t = WorldMapSystem_UpdatePlaceActor_Patch.actorHolder.transform;
                GuiBaseUI.Main.LogAllChild(t, true);
                return true;
            }
        }


            [HarmonyPatch(typeof(WorldMapSystem), "UpdatePlaceActor", new Type[] { typeof(int), typeof(int) })]
        public static class WorldMapSystem_UpdatePlaceActor_Patch
        {
            public static NewWorldNpc actorHolder;

            static FieldInfo m_showPlaceActorTyp;
            // 反射私有字段
            public static int showPlaceActorTyp
            {
                get {
                    if(m_showPlaceActorTyp == null)
                    {
                        m_showPlaceActorTyp = typeof(WorldMapSystem).GetField("showPlaceActorTyp", BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                    int value = 1;
                    try
                    {
                        value = (int)m_showPlaceActorTyp.GetValue(WorldMapSystem.instance);
                    }
                    catch
                    {
                        Main.Logger.Log("反射出错");
                    }
                    
                    return value;
                }
            }

            private static void UpdatePlaceActor(int partId, int placeId)
            {
                showNpcInfo  = DateFile.instance.mianPlaceId == WorldMapSystem.instance.choosePlaceId || WorldMapSystem.instance.playerNeighbor.Contains(WorldMapSystem.instance.choosePlaceId);
                List<int> list;
                int num = showPlaceActorTyp;
                switch (num)
                {
                    case 3:
                        list = new List<int>(DateFile.instance.HaveActor(partId, placeId, false, false, true, true));
                        break;
                    case 2:
                        list = new List<int>(DateFile.instance.HaveActor(partId, placeId, false, true, false, true));
                        break;
                    case 1:
                    default:
                        list = new List<int>(DateFile.instance.HaveActor(partId, placeId, true, false, false, true));
                        break;
                }


                List<int> list2 = new List<int>();
                if (list.Count > 0)
                {
                    Dictionary<int, int> dictionary = new Dictionary<int, int>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        int id = list[i];
                        int num2 = int.Parse(DateFile.instance.GetActorDate(id, 20, false));
                        dictionary.Add(list[i], Mathf.Abs(num2) * ((num2 <= 0) ? 15000 : 10000) - int.Parse(DateFile.instance.GetActorDate(id, 11, false)));
                    }
                    List<KeyValuePair<int, int>> list3 = new List<KeyValuePair<int, int>>(dictionary);
                    list3.Sort((KeyValuePair<int, int> s1, KeyValuePair<int, int> s2) => s1.Value.CompareTo(s2.Value));
                    foreach (KeyValuePair<int, int> keyValuePair in list3)
                    {
                        list2.Add(keyValuePair.Key);
                    }

                    // list2是最终要显示的数据列表
                    //if (false) // 这段将采用大数据无限滚动
                    //{
                    //    int num3 = 0;
                    //    int childCount = WorldMapSystem.instance.actorHolder.childCount;
                    //    for (int j = 0; j < list2.Count; j++)
                    //    {
                    //        int num4 = list2[j];
                    //        num3++;
                    //        GameObject gameObject;
                    //        if (childCount >= num3)
                    //        {
                    //            gameObject = WorldMapSystem.instance.actorHolder.GetChild(num3 - 1).gameObject;
                    //            gameObject.SetActive(true);
                    //        }
                    //        else
                    //        {
                    //            gameObject = UnityEngine.Object.Instantiate<GameObject>(WorldMapSystem.instance.actorIcon, Vector3.zero, Quaternion.identity);
                    //            gameObject.transform.SetParent(WorldMapSystem.instance.actorHolder, false);
                    //        }
                    //        gameObject.name = "Actor," + num4;
                    //        gameObject.GetComponent<SetPlaceActor>().SetActor(num4, showNpcInfo);
                    //    }
                    //    if (childCount > num3)
                    //    {
                    //        for (int k = num3; k < childCount; k++)
                    //        {
                    //            WorldMapSystem.instance.actorHolder.GetChild(k).gameObject.SetActive(false);
                    //        }
                    //    }
                    //}
                    UIMove.instance.PlaceActorUimove(true);
                    if (partId == DateFile.instance.mianPartId)
                    {
                        WorldMapSystem.instance.worldMapPlaces[placeId].UpdatePlaceActors();
                    }
                }
                else
                {
                    for (int l = 0; l < WorldMapSystem.instance.actorHolder.childCount; l++)
                    {
                        WorldMapSystem.instance.actorHolder.GetChild(l).gameObject.SetActive(false);
                    }
                    UIMove.instance.PlaceActorUimove(false);
                }
                actorHolder.data = list2.ToArray();
            }


            private static void InitUI(int partId, int placeId)
            {
                actorHolder = WorldMapSystem.instance.actorHolder.parent.gameObject.AddComponent<NewWorldNpc>();
                actorHolder.Init(partId, placeId);
            }

            public static bool Prefix(int partId, int placeId)
            {
                Main.Logger.Log("刷新npc行动者");
                //if (!Main.settings.open)
                //{
                //    return true;
                //}
                if (actorHolder == null)
                {
                    InitUI(partId, placeId);
                }
                UpdatePlaceActor(partId, placeId);

                return false;


                bool show = DateFile.instance.mianPlaceId == WorldMapSystem.instance.choosePlaceId || WorldMapSystem.instance.playerNeighbor.Contains(WorldMapSystem.instance.choosePlaceId);
                List<int> list = new List<int>();
                int num = showPlaceActorTyp;
                if (num != 1)
                {
                    if (num != 2)
                    {
                        if (num == 3)
                        {
                            list = new List<int>(DateFile.instance.HaveActor(partId, placeId, false, false, true, true));
                        }
                    }
                    else
                    {
                        list = new List<int>(DateFile.instance.HaveActor(partId, placeId, false, true, false, true));
                    }
                }
                else
                {
                    list = new List<int>(DateFile.instance.HaveActor(partId, placeId, true, false, false, true));
                }
                if (list.Count > 0)
                {
                    List<int> list2 = new List<int>();
                    Dictionary<int, int> dictionary = new Dictionary<int, int>();
                    for (int i = 0; i < list.Count; i++)
                    {
                        int id = list[i];
                        int num2 = int.Parse(DateFile.instance.GetActorDate(id, 20, false));
                        dictionary.Add(list[i], Mathf.Abs(num2) * ((num2 <= 0) ? 15000 : 10000) - int.Parse(DateFile.instance.GetActorDate(id, 11, false)));
                    }
                    List<KeyValuePair<int, int>> list3 = new List<KeyValuePair<int, int>>(dictionary);
                    list3.Sort((KeyValuePair<int, int> s1, KeyValuePair<int, int> s2) => s1.Value.CompareTo(s2.Value));
                    foreach (KeyValuePair<int, int> keyValuePair in list3)
                    {
                        list2.Add(keyValuePair.Key);
                    }
                    int num3 = 0;
                    int childCount = WorldMapSystem.instance.actorHolder.childCount;
                    for (int j = 0; j < list2.Count; j++)
                    {
                        int num4 = list2[j];
                        num3++;
                        GameObject gameObject;
                        if (childCount >= num3)
                        {
                            gameObject = WorldMapSystem.instance.actorHolder.GetChild(num3 - 1).gameObject;
                            gameObject.SetActive(true);
                        }
                        else
                        {
                            gameObject = UnityEngine.Object.Instantiate<GameObject>(WorldMapSystem.instance.actorIcon, Vector3.zero, Quaternion.identity);
                            gameObject.transform.SetParent(WorldMapSystem.instance.actorHolder, false);
                        }
                        gameObject.name = "Actor," + num4;
                        gameObject.GetComponent<SetPlaceActor>().SetActor(num4, show);
                    }
                    if (childCount > num3)
                    {
                        for (int k = num3; k < childCount; k++)
                        {
                            WorldMapSystem.instance.actorHolder.GetChild(k).gameObject.SetActive(false);
                        }
                    }
                    UIMove.instance.PlaceActorUimove(true);
                    if (partId == DateFile.instance.mianPartId)
                    {
                        WorldMapSystem.instance.worldMapPlaces[placeId].UpdatePlaceActors();
                    }
                }
                else
                {
                    for (int l = 0; l < WorldMapSystem.instance.actorHolder.childCount; l++)
                    {
                        WorldMapSystem.instance.actorHolder.GetChild(l).gameObject.SetActive(false);
                    }
                    UIMove.instance.PlaceActorUimove(false);
                }
                return false;
            }
        }
    }
}