using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UObject = UnityEngine.Object;

using UnityEngine;
using UnityEngine.SceneManagement;
using System.ComponentModel.Design.Serialization;
using HutongGames.PlayMaker.Actions;
using System.Linq;
using System.Reflection;
using Modding.Converters;
using System.IO;
using Newtonsoft.Json;
using IL.InControl;
using UnityEngine.Assertions.Must;
using GlobalEnums;
using static GameManager;
using HutongGames.Utility;
using System.Threading;
using System.Linq.Expressions;

namespace LumaflyMap {

    public class LumaflyMap : Mod {
        internal static LumaflyMap Instance;


        public void reportAll(GameObject it, string indent)
        {
            Log(indent + "name: " + it.name + ", active=" + it.activeSelf + ", " + it.activeInHierarchy);
            Log(indent + " components:");
            var cs = it.GetComponents<Component>();
            for(int i = 0; i < cs.Length; i++) {
                Log(indent + "  " + cs[i].GetType().Name);
            }
            Log(indent + " children:");
            for(int i = 0; i < it.transform.childCount; i++) {
                reportAll(it.transform.GetChild(i).gameObject, indent + "  ");
            }
        }

        public void reportAllCurrentScene() {
            var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var rs = s.GetRootGameObjects();
            for(var i = 0; i < rs.Length; i++) {
                reportAll(rs[i], "");
            }
        }

        public GameObject findInHierarchy(GameObject o, string[] names, int beginI) {
            if(beginI >= names.Length) return o;
            string name = names[beginI];
            var ct = o.transform.Find(name);
            if(ct == null) return null;
            return findInHierarchy(ct.gameObject, names, beginI + 1);
        }

        // 1. Searches inactive as well. 2. Can specify scene
        public GameObject find2(Scene s, string path) {
            if(path[0] != '/') throw new Exception("Not absolute path");
            var names = path.Split('/');
            if(names.Length <= 1) return null;
            var rs = s.GetRootGameObjects();
            foreach(var r in rs) {
                if(r.name == names[1]) return findInHierarchy(r, names, 2);
            }
            return null;
        }

        public static string path(GameObject obj) {
            string path = "/" + obj.name;
            while (obj.transform.parent != null) {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        public struct LampData {
            public string brk; // path to GameObject with Breakable. Empty string if none
        }
        public struct SpecialData {}

        public struct EnemyData {}

        public  class SceneObjects {
            public Dictionary<string, LampData> lamps;
            public Dictionary<string, EnemyData> enemies;
            public Dictionary<string, EnemyData> beamMiners;
            public Dictionary<string, SpecialData> chests;
            public Dictionary<string, SpecialData> chandeliers;
            // BREAKABLE walls
            public Dictionary<string, LampData> lampsOnWalls;
        }


        public Dictionary<string, SceneObjects> allItems;


        public void processAll(GameObject it, Action<GameObject> action) {
            action(it);
            for(int i = 0; i < it.transform.childCount; i++) {
                processAll(it.transform.GetChild(i).gameObject, action);
            }
        }

        public override string GetVersion() => "no";


        public class Anyception : Exception {
            public dynamic payload;
            public Anyception(dynamic payload) : base() {
                this.payload = payload;
            }
        }

        class CustomText : MonoBehaviour {
            public void LateUpdate() {
                var t = this.gameObject.GetComponent<TextMesh>();
                t.offsetZ = -5;
                t.color = Color.white;
            }
        }

        struct vec2 {
            public float x, y;

            public vec2() { }
            public vec2(float xx, float yy) { x = xx; y = yy; }
            public vec2(Vector2 a) { x = a.x; y = a.y; }
        }

        class Bounds {
            public vec2 origin;
            public vec2 size;
        }

        class Objs {
            public Dictionary<string, vec2> positions;
            public Bounds sceneBounds;
        }

        class Coords {
            public vec2 min;
            public vec2 max;
        }

        class ObjPos {
            public Dictionary<string, Objs> objs;
            public Dictionary<string, Coords> mapScenes;
        }

        static string seerScene = "RestingGrounds_07";
        static string seer = "/Dream Moth/Dream Dialogue";

        IEnumerator process(GameMap gameMap) {
            /*for (int index1 = 0; index1 < gameMap.transform.childCount; ++index1) {
                GameObject areaObj = gameMap.transform.GetChild(index1).gameObject;
                Log(" " + areaObj.name + " " + areaObj.layer);
                for (int index2 = 0; index2 < areaObj.transform.childCount; ++index2) {
                    GameObject roomObj = areaObj.transform.GetChild(index2).gameObject;
                    Log("   " + roomObj.name + " " + roomObj.layer);
                }
            }*/

            yield return new WaitForSeconds(10);

            yield return null;

            var allData = new ObjPos{ objs = new Dictionary<string, Objs>(), mapScenes = new Dictionary<string, Coords>() };

            try {
                for (int index1 = 0; index1 < gameMap.transform.childCount; ++index1) {
                    GameObject areaObj = gameMap.transform.GetChild(index1).gameObject;
                    for (int index2 = 0; index2 < areaObj.transform.childCount; ++index2) {
                        var roomTransform = areaObj.transform.GetChild(index2);
                        try {
                        GameObject roomObj = roomTransform.gameObject;
                        var name = roomObj.name;
                        var sprite = roomObj.GetComponent<SpriteRenderer>();

                        allData.mapScenes.Add(name, new Coords{
                            min = new vec2(sprite.bounds.min),
                            max = new vec2(sprite.bounds.max)
                        });
                        }
                        catch(Exception e) {
                            LogWarn("Skipping " + (roomTransform != null ? roomTransform.name : "null") + ", probably not a scene? " + e);
                        }
                    }
                }
            }
            catch(Exception e) {
                LogError(e);
            }

            var ii = 0;
            foreach(var pair in allItems) {
                Log("Processing scene " + pair.Key + " (" + (ii + 1) + " of " + allItems.Count + ")");
                ii++;
                var res = new Objs { positions = new Dictionary<string, vec2>() };
                var v = pair.Value;


                UnityEngine.SceneManagement.SceneManager.LoadScene(pair.Key);
                yield return null;

                try {
                    var s = UnityEngine.SceneManagement.SceneManager.GetSceneByName(pair.Key);

                    tk2dTileMap tilemap = null;

                    GameObject[] rootGameObjects = s.GetRootGameObjects();
                    int num2 = 0;
                    while (tilemap == null && num2 < rootGameObjects.Length) {
                        var obj = rootGameObjects[num2];
                        if (obj.CompareTag("TileMap")) tilemap = obj.GetComponent<tk2dTileMap>();
                        num2++;
                    }

                    res.sceneBounds = new Bounds { origin = new vec2(), size = new vec2(tilemap.width, tilemap.height) };
                    var add = (string path) => {
                        var obj = find2(s, path);
                        if(obj == null) {
                            LogError("No object " + path + " in " + s.name);
                            return;
                        }
                        res.positions.Add(path, new vec2(obj.transform.position.x, obj.transform.position.y));
                    };

                    foreach(var it in v.lamps) add(it.Key);
                    foreach(var it in v.enemies) add(it.Key);
                    foreach(var it in v.beamMiners) add(it.Key);
                    foreach(var it in v.chests) add(it.Key);
                    foreach(var it in v.chandeliers) add(it.Key);
                    foreach(var it in v.lampsOnWalls) add(it.Key);

                    if(s.name == seerScene) {
                        add(seer);
                    }

                    allData.objs.Add(s.name, res);
                }
                catch(Exception e) {
                    LogError(e);
                }
            }

            try {
            var resS = Newtonsoft.Json.JsonConvert.SerializeObject(allData);
            File.WriteAllText(path, resS);
            Log("Done!");
            }
            catch(Exception e) {
                LogError("Writing error: " + e);
            }
        }

        public IEnumerator doStuff() {
            /*
            // Update lumafly locations
            // Do at your own risk! Grants some achievements (e.g. speedrun).
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            var scenes = new Scene[sceneCount];
            Log("there's " + sceneCount + " scenes.");
            var result = new Dictionary<string, SceneObjects>();
            for(int i = 0; i < sceneCount; i++) {
                UnityEngine.SceneManagement.SceneManager.LoadScene(i);
                yield return null; // we DO need to await for individual scenes and not load all + wait + process all
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(i);
                Log("  scene " + i + " is named " + s.name);
                SceneObjects sr;
                try {
                    sr = saveSceneObjects(s);
                }
                catch(Exception e) {
                    LogError("Error: " + e);
                    yield break;
                }
                if(sr.lamps.Count > 0 || sr.enemies.Count > 0 || sr.beamMiners.Count > 0 || sr.chests.Count > 0 || sr.chandeliers.Count > 0) {
                    result.Add(s.name, sr);
                }
                UnityEngine.SceneManagement.SceneManager.UnloadScene(i);
                //var roots = s.GetRootGameObjects();
                //for (int j = 0; j < roots.Length; j++) {
                //    reportAll(roots[j], "");
                //}
            }

            var resS = Newtonsoft.Json.JsonConvert.SerializeObject(result);
            File.WriteAllText(path, resS);

            UnityEngine.Application.Quit(0);
            */

            /*
            // Extract lumafly icon
            // Do at your own risk! Grants some achievements (e.g. speedrun).
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            var scenes = new Scene[sceneCount];
            Log("there's " + sceneCount + " scenes.");
            var result = new Dictionary<string, SceneObjects>();
            for(int i = 0; i < sceneCount; i++) {
                UnityEngine.SceneManagement.SceneManager.LoadScene(i);
                yield return null; // we DO need to await for individual scenes and not load all + wait + process all
                var s = UnityEngine.SceneManagement.SceneManager.GetSceneByBuildIndex(i);
                Log("  scene " + i + " is named " + s.name);
                var roots = s.GetRootGameObjects();
                try {
                    for(var j = 0; j < roots.Length; j++) {
                        processAll(roots[j], it => {
                            if(it.name.StartsWith("shop_lamp_bug")) throw new Anyception(it);
                        });
                    }
                }
                catch(Anyception a) {
                    GameObject bug = a.payload as GameObject;
                    var sp = bug.GetComponent<SpriteRenderer>().sprite;
                    // texture atlas. Cannot be cropped here because none of the sprite's rect's have the right coords...
                    var array = duplicateTexture(sp.texture).EncodeToPNG();
                    using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                        fileStream.Write(array, 0, array.Length);
                    }
                    Log("Extracted texture successfully");
                    break;
                }
                UnityEngine.SceneManagement.SceneManager.UnloadScene(i);
            }

            UnityEngine.Application.Quit(0);
            */

            string listStr = null;
            using(var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("list")) {
                var arr = new byte[s.Length];
                s.Read(arr, 0, arr.Length);
                listStr = System.Text.Encoding.UTF8.GetString(arr);
            }
            allItems = JsonConvert.DeserializeObject<Dictionary<string, SceneObjects>>(listStr);

            On.GameMap.Start += (orig, self) => {
                orig(self);
                GameManager.instance.StartCoroutine(this.process(self));
            };


            yield break;
        }

        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
        {
            Log("Initializing");

            Instance = this;
            GameManager.instance.StartCoroutine(doStuff());

            Log("Initialized");
        }
    }

    class UpdateWhenActive : MonoBehaviour {
        public event EventHandler onEnable;
        public void OnEnable() {
            onEnable?.Invoke(this, EventArgs.Empty);
        }
    }

    /*
    class ModUpdate : MonoBehaviour {
        int curRoomI = 0;
        string[] scenes = LumaflyKnight.Instance.data.allItems.Keys.ToArray();

        public void Update() {
            GameObject hero = GameManager.instance?.hero_ctrl?.gameObject;
            if (Input.GetKeyDown(KeyCode.Q)) {
               var list = new List<LumaflyKnight.Entry>();

               Action<GameObject> insert = (newObj) => {
                    var diff = (newObj.gameObject.transform.position - hero.transform.position);
                    var sqDist = diff.sqrMagnitude;

                    var newEntry = new LumaflyKnight.Entry(sqDist, newObj);
                    int index = list.BinarySearch(newEntry, Comparer<LumaflyKnight.Entry>.Create((a, b) => a.SqDist.CompareTo(b.SqDist)));

                    // If not found, BinarySearch returns a negative index that is the bitwise complement of the next larger element's index
                    if (index < 0) list.Add(newEntry);
                    else list.Insert(index, newEntry);

                    if(list.Count > 100) list.RemoveAt(list.Count - 1);
               };

               var s = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
               var rs = s.GetRootGameObjects();
               for(var i = 0; i < rs.Length; i++) {
                    LumaflyKnight.Instance.processAll(rs[i], insert);
               }

               LumaflyKnight.Instance.Log("Objects near the player:");
               for(var i = 0; i < list.Count; i++) {
                    var it = list[i].Obj;
                    LumaflyKnight.Instance.Log(i + ". name: " + LumaflyKnight.path(it));
                    LumaflyKnight.Instance.Log(" components:");
                    var cs = it.GetComponents<Component>();
                    for(int j = 0; j < cs.Length; j++) {
                        LumaflyKnight.Instance.Log("  " + cs[j].GetType().Name);
                    }
                    LumaflyKnight.Instance.Log("");
               }
               for(var i = 0; i < list.Count; i++) {
                    GameObject.Destroy(list[i].Obj);
                }
            }

            if (Input.GetKeyDown(KeyCode.P)) {
                LumaflyKnight.Instance.reportAllCurrentScene();
            }
        }
    }
    */
}
