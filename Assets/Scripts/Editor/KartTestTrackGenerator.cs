using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BendStarKart.Editor
{
    public static class KartTestTrackGenerator
    {
        private const string TestScenePath = "Assets/Scenes/RaceTrack.unity";
        private const string RaceScenePath = "Assets/Scenes/StarloopRace.unity";
        private const string SettingsPath = "Assets/Settings";
        private const float RoadHalfWidth = 9f;
        private const float GrassOuterWidth = 15f;
        private const int SamplesPerControl = 10;

        // X、Y、Z：依次包含长直道、跳台、宽弯、U 弯、连续 S 弯与高低坡。
        private static readonly Vector3[] Controls =
        {
            new Vector3(-72, 0, -55), new Vector3(-35, 0, -55),
            new Vector3(-12, 0, -55), new Vector3(1, 3, -55),
            new Vector3(11, 3, -55), new Vector3(25, 0, -55),
            new Vector3(68, 0, -55), new Vector3(96, 0, -35),
            new Vector3(101, 0, 3), new Vector3(79, 0, 30),
            new Vector3(45, 0, 30), new Vector3(20, 0, 29),
            new Vector3(5, 0, 43), new Vector3(18, 0, 64),
            new Vector3(53, 0, 71), new Vector3(88, 0, 72),
            new Vector3(105, 0, 96), new Vector3(75, 2, 112),
            new Vector3(30, 4, 108), new Vector3(-16, 3, 94),
            new Vector3(-43, 1, 79), new Vector3(-28, 0, 60),
            new Vector3(-53, 0, 39), new Vector3(-77, 0, 50),
            new Vector3(-103, 0, 27), new Vector3(-106, 0, -18),
            new Vector3(-91, 0, -49)
        };

        // 正式赛道采用无交叉闭环：南侧起步长直道、东侧 U 弯、北侧宽弯和西侧 S 弯。
        // 跳台由连续路面的起伏形成，不能在高速行车线上挖空碰撞网格。
        private static readonly Vector3[] RaceControls =
        {
            // 把起终点放在三点共线的直道中段，车头不会指向外侧护栏。
            new Vector3(-65, 0, -60), new Vector3(-30, 0, -60),
            new Vector3(0, 2, -60),
            new Vector3(18, 2, -60), new Vector3(42, 0, -60),
            new Vector3(88, 0, -60), new Vector3(119, 0, -43),
            new Vector3(132, 0, -12), new Vector3(129, 0, 18),
            new Vector3(112, 0, 44), new Vector3(85, 0, 58),
            new Vector3(48, 0, 58), new Vector3(20, 0, 62),
            new Vector3(-8, 0, 52), new Vector3(-32, 0, 65),
            new Vector3(-51, 1, 88), new Vector3(-80, 3, 102),
            new Vector3(-111, 1, 91), new Vector3(-131, 0, 66),
            new Vector3(-141, 0, 28), new Vector3(-137, 0, -12),
            new Vector3(-122, 0, -44), new Vector3(-100, 0, -60)
        };

        [MenuItem("Tools/Kart/Generate Test Track")]
        public static void Generate() { GenerateScene(false); }

        [MenuItem("Tools/Kart/Generate Race Track")]
        public static void GenerateRace() { GenerateScene(true); }

        private static void GenerateScene(bool formalRace)
        {
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(SettingsPath);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            KartTuning tuning = GetOrCreateTuning();
            InputActionAsset input = CreateInputActions();
            Material road = GetOrCreateMaterial("Road", new Color(0.27f, 0.36f, 0.42f));
            Material grass = GetOrCreateMaterial("Grass", new Color(0.32f, 0.67f, 0.33f));
            Material rail = GetOrCreateMaterial("Rail", new Color(0.98f, 0.43f, 0.21f));
            Material white = GetOrCreateMaterial("White", new Color(0.94f, 0.93f, 0.77f));
            Material bodyMaterial = GetOrCreateMaterial("KartBody", new Color(0.14f, 0.76f, 0.91f));
            Material dark = GetOrCreateMaterial("KartDark", new Color(0.11f, 0.15f, 0.21f));
            Material outline = GetOrCreateSpecialMaterial("KartOutline", "BendStarKart/Outline",
                "_OutlineColor", new Color(0.05f, 0.08f, 0.12f));
            Material tireMark = GetOrCreateSpecialMaterial("TireMark", "BendStarKart/FX",
                "_BaseColor", new Color(0.06f, 0.08f, 0.1f, 0.46f));
            Material sparks = GetOrCreateSpecialMaterial("DriftSparks", "BendStarKart/FX",
                "_BaseColor", new Color(1f, 0.52f, 0.08f, 0.9f));
            Material flame = GetOrCreateSpecialMaterial("NitroFlame", "BendStarKart/FX",
                "_BaseColor", new Color(0.17f, 0.85f, 1f, 0.75f));
            PhysicsMaterial lowFriction = GetOrCreatePhysicsMaterial();

            GameObject root = new GameObject(formalRace ? "弯星巡回赛道" : "弯星测试赛道");
            List<Vector3> path = BuildPath(formalRace);
            BuildSurface(root.transform, "沥青赛道", path, -RoadHalfWidth, RoadHalfWidth,
                road, false, formalRace);
            BuildSurface(root.transform, "左侧草地", path, -GrassOuterWidth, -RoadHalfWidth,
                grass, true, formalRace);
            BuildSurface(root.transform, "右侧草地", path, RoadHalfWidth, GrassOuterWidth,
                grass, true, formalRace);
            BuildRails(root.transform, path, rail, lowFriction);
            KartTrackBounds bounds = root.AddComponent<KartTrackBounds>();
            bounds.Configure(path.ToArray(), GrassOuterWidth);
            KartCheckpoint[] checkpoints = BuildCheckpoints(root.transform, path);
            Transform[] waypoints = BuildWaypoints(root.transform, path);
            BuildMarkers(root.transform, path, white);
            BuildEnvironment(grass);
            if (formalRace)
                BuildFormalDecorations(root.transform, path, rail, white, dark);

            Vector3 spawn = path[0] + Vector3.up * 0.8f;
            Quaternion spawnRotation = Quaternion.LookRotation(Tangent(path, 0), Vector3.up);
            KartController kart = BuildKart(spawn, spawnRotation, tuning, input,
                bodyMaterial, dark, white, lowFriction, outline,
                tireMark, sparks, flame);
            kart.ConfigureTrackBounds(bounds);
            BuildCamera(kart);
            RaceManager race = null;
            if (formalRace)
            {
                KartAiDriver[] opponents = BuildAiKarts(path.ToArray(),
                    kart.GetComponent<RaceStartSignal>(), lowFriction, dark, white,
                    outline);
                race = new GameObject("三圈竞速流程").AddComponent<RaceManager>();
                race.Configure(kart, kart.GetComponent<RaceStartSignal>(), checkpoints,
                    path.ToArray(), opponents);
                for (int i = 0; i < checkpoints.Length; i++)
                    checkpoints[i].ConfigureRace(race, i);
            }
            GameObject ui = new GameObject("调参与状态 UI");
            ui.AddComponent<KartTuningPanel>().Configure(kart, kart.GetComponent<KartInput>());
            ui.AddComponent<KartBoostHud>().Configure(kart.GetComponent<KartBoost>(),
                kart.GetComponent<RaceStartSignal>());
            if (formalRace)
                ui.AddComponent<RaceHud>().Configure(race, kart, waypoints);

            Selection.activeGameObject = kart.gameObject;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            string scenePath = formalRace ? RaceScenePath : TestScenePath;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), scenePath);
            if (formalRace) AddSceneToBuild(scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("弯星卡丁：已生成 " + scenePath + "。打开场景并按 Play 驾驶。");
        }

        private static void AddSceneToBuild(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.path == scenePath)
                {
                    scene.enabled = true;
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static KartTuning GetOrCreateTuning()
        {
            const string path = SettingsPath + "/KartTuning.asset";
            KartTuning asset = AssetDatabase.LoadAssetAtPath<KartTuning>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<KartTuning>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static InputActionAsset CreateInputActions()
        {
            const string path = SettingsPath + "/KartControls.inputactions";
            // 已改绑的资源必须保留，重复生成赛道不会覆盖它。
            InputActionAsset existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            if (existing != null) return existing;
            InputActionAsset actions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap map = actions.AddActionMap("Drive");
            map.AddAction("Accelerate", InputActionType.Button).AddBinding("<Keyboard>/upArrow");
            map.AddAction("Brake", InputActionType.Button).AddBinding("<Keyboard>/downArrow");
            InputAction steer = map.AddAction("Steer", InputActionType.Value);
            steer.AddCompositeBinding("1DAxis")
                .With("negative", "<Keyboard>/leftArrow")
                .With("positive", "<Keyboard>/rightArrow");
            map.AddAction("Drift", InputActionType.Button).AddBinding("<Keyboard>/leftShift");
            InputAction nitro = map.AddAction("Nitro", InputActionType.Button);
            nitro.AddBinding("<Keyboard>/leftCtrl");
            nitro.AddBinding("<Keyboard>/space");
            map.AddAction("Reset", InputActionType.Button).AddBinding("<Keyboard>/r");
            map.AddAction("Pause", InputActionType.Button).AddBinding("<Keyboard>/escape");
            map.AddAction("Tuning", InputActionType.Button).AddBinding("<Keyboard>/f1");
            File.WriteAllText(path, actions.ToJson());
            Object.DestroyImmediate(actions);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = SettingsPath + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader) { color = color, name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material GetOrCreateSpecialMaterial(string name,
            string shaderName, string colorProperty, Color color)
        {
            string path = SettingsPath + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
                throw new System.InvalidOperationException("缺少 URP Shader：" + shaderName);
            material = new Material(shader) { name = name };
            material.SetColor(colorProperty, color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static PhysicsMaterial GetOrCreatePhysicsMaterial()
        {
            const string path = SettingsPath + "/LowFriction.physicMaterial";
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material != null) return material;
            material = new PhysicsMaterial("LowFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static List<Vector3> BuildPath(bool formalRace)
        {
            Vector3[] controls = formalRace ? RaceControls : Controls;
            var points = new List<Vector3>(controls.Length * SamplesPerControl);
            for (int i = 0; i < controls.Length; i++)
            {
                for (int j = 0; j < SamplesPerControl; j++)
                {
                    float t = j / (float)SamplesPerControl;
                    points.Add(Catmull(
                        controls[(i - 1 + controls.Length) % controls.Length],
                        controls[i], controls[(i + 1) % controls.Length],
                        controls[(i + 2) % controls.Length], t));
                }
            }
            return points;
        }

        private static Vector3 Catmull(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            return 0.5f * (2f * b + (-a + c) * t +
                (2f * a - 5f * b + 4f * c - d) * t * t +
                (-a + 3f * b - 3f * c + d) * t * t * t);
        }

        private static Vector3 Tangent(List<Vector3> path, int index)
        {
            Vector3 direction = path[(index + 1) % path.Count] -
                path[(index - 1 + path.Count) % path.Count];
            direction.y = 0f;
            return direction.normalized;
        }

        private static Vector3 Right(List<Vector3> path, int index)
        {
            return Vector3.Cross(Vector3.up, Tangent(path, index)).normalized;
        }

        private static void BuildSurface(Transform parent, string name, List<Vector3> path,
            float leftOffset, float rightOffset, Material material, bool isGrass,
            bool formalRace)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < path.Count; i++)
            {
                int next = (i + 1) % path.Count;
                Vector3 a = path[i] + Right(path, i) * leftOffset;
                Vector3 b = path[i] + Right(path, i) * rightOffset;
                Vector3 c = path[next] + Right(path, next) * leftOffset;
                Vector3 d = path[next] + Right(path, next) * rightOffset;
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                triangles.Add(start + 1); triangles.Add(start + 2); triangles.Add(start + 3);
            }
            Mesh mesh = new Mesh { name = name };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            string pathName = SettingsPath + "/" +
                (formalRace ? "Race" : "Test") + (isGrass ? name : "Road") + ".asset";
            AssetDatabase.DeleteAsset(pathName);
            AssetDatabase.CreateAsset(mesh, pathName);
            GameObject objectWithMesh = new GameObject(name);
            objectWithMesh.transform.SetParent(parent);
            objectWithMesh.AddComponent<MeshFilter>().sharedMesh = mesh;
            objectWithMesh.AddComponent<MeshRenderer>().sharedMaterial = material;
            objectWithMesh.AddComponent<MeshCollider>().sharedMesh = mesh;
            objectWithMesh.AddComponent<KartSurface>().SetGrass(isGrass);
        }

        private static void BuildRails(Transform parent, List<Vector3> path,
            Material material, PhysicsMaterial lowFriction)
        {
            GameObject rails = new GameObject("双侧连续护栏 · 低摩擦碰撞体");
            rails.transform.SetParent(parent);
            for (int side = -1; side <= 1; side += 2)
            {
                var vertices = new List<Vector3>(path.Count * 4);
                var triangles = new List<int>(path.Count * 12);
                for (int i = 0; i < path.Count; i++)
                {
                    int next = (i + 1) % path.Count;
                    Vector3 a = path[i] + Right(path, i) * side * (GrassOuterWidth + 0.2f);
                    Vector3 b = path[next] + Right(path, next) * side * (GrassOuterWidth + 0.2f);
                    int start = vertices.Count;
                    // 护栏向赛道下方延伸，跳台落地时也不能从栏杆下钻出。
                    vertices.Add(a + Vector3.down * 1.5f);
                    vertices.Add(a + Vector3.up * 2.1f);
                    vertices.Add(b + Vector3.down * 1.5f);
                    vertices.Add(b + Vector3.up * 2.1f);
                    triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                    triangles.Add(start + 1); triangles.Add(start + 3); triangles.Add(start + 2);
                    triangles.Add(start + 2); triangles.Add(start + 1); triangles.Add(start);
                    triangles.Add(start + 2); triangles.Add(start + 3); triangles.Add(start + 1);
                }
                Mesh mesh = new Mesh { name = side < 0 ? "左连续护栏" : "右连续护栏" };
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                string assetPath = SettingsPath + (side < 0 ? "/LeftRail.asset" : "/RightRail.asset");
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(mesh, assetPath);
                GameObject rail = new GameObject(mesh.name);
                rail.transform.SetParent(rails.transform);
                rail.AddComponent<MeshFilter>().sharedMesh = mesh;
                rail.AddComponent<MeshRenderer>().sharedMaterial = material;
                rail.AddComponent<MeshCollider>().sharedMesh = mesh;
                rail.GetComponent<MeshCollider>().material = lowFriction;
            }
        }

        private static KartCheckpoint[] BuildCheckpoints(Transform parent, List<Vector3> path)
        {
            GameObject checkpointRoot = new GameObject("检查点 · 顺序通过与 R 重置");
            checkpointRoot.transform.SetParent(parent);
            KartCheckpoint[] checkpoints = new KartCheckpoint[10];
            for (int i = 0; i < 10; i++)
            {
                int index = i * path.Count / 10;
                Quaternion rotation = Quaternion.LookRotation(Tangent(path, index), Vector3.up);
                GameObject trigger = new GameObject("CP " + i);
                trigger.transform.SetParent(checkpointRoot.transform);
                trigger.transform.SetPositionAndRotation(path[index] + Vector3.up, rotation);
                BoxCollider box = trigger.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(RoadHalfWidth * 2f, 3f, 1.2f);
                GameObject spawn = new GameObject("重生点");
                spawn.transform.SetParent(trigger.transform);
                spawn.transform.SetPositionAndRotation(path[index], rotation);
                checkpoints[i] = trigger.AddComponent<KartCheckpoint>();
                checkpoints[i].SetRespawnPoint(spawn.transform);
            }
            return checkpoints;
        }

        private static Transform[] BuildWaypoints(Transform parent, List<Vector3> path)
        {
            GameObject waypoints = new GameObject("AI 路径点 · 阶段 4 使用");
            waypoints.transform.SetParent(parent);
            var result = new List<Transform>();
            for (int i = 0; i < path.Count; i += 6)
            {
                GameObject point = new GameObject("Waypoint " + (i / 6).ToString("D2"));
                point.transform.SetParent(waypoints.transform);
                point.transform.SetPositionAndRotation(path[i],
                    Quaternion.LookRotation(Tangent(path, i), Vector3.up));
                result.Add(point.transform);
            }
            return result.ToArray();
        }

        private static void BuildFormalDecorations(Transform parent, List<Vector3> path,
            Material accent, Material white, Material dark)
        {
            GameObject decorations = new GameObject("正式赛道装饰 · 路缘与起跑拱门");
            decorations.transform.SetParent(parent);
            for (int i = 0; i < path.Count; i += 5)
            {
                int next = (i + 5) % path.Count;
                Vector3 middle = (path[i] + path[next]) * 0.5f;
                Vector3 direction = Tangent(path, i);
                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                float length = Vector3.Distance(path[i], path[next]);
                Material color = (i / 5) % 2 == 0 ? white : accent;
                for (int side = -1; side <= 1; side += 2)
                    AddPart(decorations.transform, "交替路缘标线", PrimitiveType.Cube,
                        middle + Right(path, i) * side * (RoadHalfWidth - 0.4f)
                        + Vector3.up * 0.06f,
                        new Vector3(0.7f, 0.09f, length), color, rotation);
            }

            Quaternion startRotation = Quaternion.LookRotation(Tangent(path, 0), Vector3.up);
            Vector3 start = path[0];
            for (int side = -1; side <= 1; side += 2)
                AddPart(decorations.transform, "起跑拱门立柱", PrimitiveType.Cube,
                    start + Right(path, 0) * side * 8.3f + Vector3.up * 2.6f,
                    new Vector3(0.85f, 5.2f, 0.85f), accent, startRotation);
            AddPart(decorations.transform, "起跑拱门横梁", PrimitiveType.Cube,
                start + Vector3.up * 5.1f, new Vector3(17.6f, 0.9f, 1f),
                white, startRotation);

            for (int i = 0; i < path.Count; i += 20)
            {
                Vector3 site = path[i] + Right(path, i) * 19f;
                AddPart(decorations.transform, "低多边形树干", PrimitiveType.Cylinder,
                    site + Vector3.up * 1.3f, new Vector3(0.35f, 1.3f, 0.35f), dark);
                AddPart(decorations.transform, "低多边形树冠", PrimitiveType.Sphere,
                    site + Vector3.up * 3.25f, new Vector3(2.7f, 2.8f, 2.7f), accent);
            }
        }

        private static void BuildMarkers(Transform parent, List<Vector3> path, Material white)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "起终点线";
            line.transform.SetParent(parent);
            line.transform.SetPositionAndRotation(path[0] + Vector3.up * 0.045f,
                Quaternion.LookRotation(Tangent(path, 0), Vector3.up));
            line.transform.localScale = new Vector3(RoadHalfWidth * 2f, 0.07f, 1.1f);
            line.GetComponent<MeshRenderer>().sharedMaterial = white;
            Object.DestroyImmediate(line.GetComponent<Collider>());
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "跳台提示牌";
            sign.transform.SetParent(parent);
            sign.transform.position = path[18] + Right(path, 18) * 16f
                + Vector3.up * 2f;
            sign.transform.localScale = new Vector3(5f, 2f, 0.25f);
            sign.GetComponent<MeshRenderer>().sharedMaterial = white;
            Object.DestroyImmediate(sign.GetComponent<Collider>());
        }

        private static void BuildEnvironment(Material grass)
        {
            RenderSettings.ambientLight = new Color(0.66f, 0.72f, 0.78f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.66f, 0.84f, 0.96f);
            RenderSettings.fogDensity = 0.002f;
            GameObject lightObject = new GameObject("太阳");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.6f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "场外草原";
            backdrop.transform.position = new Vector3(0f, -4.2f, 25f);
            backdrop.transform.localScale = new Vector3(350f, 1f, 300f);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = grass;
            backdrop.AddComponent<KartSurface>().SetGrass(true);
        }

        private static KartController BuildKart(Vector3 position, Quaternion rotation,
            KartTuning tuning, InputActionAsset actions, Material paint,
            Material dark, Material white, PhysicsMaterial lowFriction,
            Material outline, Material tireMark, Material sparks, Material flame)
        {
            GameObject kart = new GameObject("玩家 · 原创 Q 版卡丁车");
            kart.transform.SetPositionAndRotation(position, rotation);
            Rigidbody body = kart.AddComponent<Rigidbody>();
            body.mass = 180f;
            body.useGravity = true;
            BoxCollider collider = kart.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.55f, 0.65f, 2.1f);
            collider.material = lowFriction;
            KartInput kartInput = kart.AddComponent<KartInput>();
            kartInput.SetActions(actions);
            kart.AddComponent<KartDrift>();
            KartController controller = kart.AddComponent<KartController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("tuningAsset").objectReferenceValue = tuning;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            kart.AddComponent<RaceStartSignal>();
            kart.AddComponent<KartBoost>();

            AddPart(kart.transform, "圆润底盘", PrimitiveType.Cube,
                new Vector3(0f, 0.1f, 0f), new Vector3(1.65f, 0.45f, 2.35f), paint);
            AddPart(kart.transform, "车头", PrimitiveType.Cube,
                new Vector3(0f, 0.23f, 0.74f), new Vector3(1.45f, 0.32f, 0.72f), white);
            AddPart(kart.transform, "座椅", PrimitiveType.Cube,
                new Vector3(0f, 0.47f, -0.45f), new Vector3(0.9f, 0.62f, 0.65f), dark);
            AddPart(kart.transform, "方向盘", PrimitiveType.Cylinder,
                new Vector3(0f, 0.61f, 0.24f), new Vector3(0.28f, 0.06f, 0.28f), dark);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    AddPart(kart.transform, "车轮", PrimitiveType.Cylinder,
                        new Vector3(x * 0.85f, 0.05f, z * 0.72f),
                        new Vector3(0.34f, 0.13f, 0.34f), dark,
                        Quaternion.Euler(0f, 0f, 90f));
            kart.AddComponent<KartOutline>().Apply(outline);
            Transform leftRear = AddAnchor(kart.transform, "左后轮特效锚点",
                new Vector3(-0.7f, -0.28f, -0.76f));
            Transform rightRear = AddAnchor(kart.transform, "右后轮特效锚点",
                new Vector3(0.7f, -0.28f, -0.76f));
            kart.AddComponent<KartEffects>().Configure(leftRear, rightRear,
                tireMark, sparks, flame);
            return controller;
        }

        private static KartAiDriver[] BuildAiKarts(Vector3[] route,
            RaceStartSignal signal, PhysicsMaterial lowFriction,
            Material dark, Material white, Material outline)
        {
            Color[] palette =
            {
                new Color(0.83f, 0.42f, 0.96f),
                new Color(1f, 0.74f, 0.17f),
                new Color(1f, 0.39f, 0.43f)
            };
            string[] names = { "紫曜", "金闪", "珊瑚" };
            KartAiDriver[] opponents = new KartAiDriver[3];
            for (int i = 0; i < opponents.Length; i++)
            {
                Material paint = GetOrCreateMaterial("AiKart" + i, palette[i]);
                GameObject kart = new GameObject("AI " + (i + 1) + " · " + names[i]);
                kart.transform.position = route[0] + Vector3.up * 0.36f;
                Rigidbody body = kart.AddComponent<Rigidbody>();
                body.mass = 180f;
                BoxCollider collider = kart.AddComponent<BoxCollider>();
                collider.size = new Vector3(1.55f, 0.65f, 2.1f);
                collider.material = lowFriction;
                AddPart(kart.transform, "车身", PrimitiveType.Cube,
                    new Vector3(0f, 0.1f, 0f), new Vector3(1.65f, 0.45f, 2.35f), paint);
                AddPart(kart.transform, "车头", PrimitiveType.Cube,
                    new Vector3(0f, 0.23f, 0.75f), new Vector3(1.45f, 0.32f, 0.72f), white);
                AddPart(kart.transform, "座椅", PrimitiveType.Cube,
                    new Vector3(0f, 0.47f, -0.43f), new Vector3(0.9f, 0.62f, 0.65f), dark);
                for (int x = -1; x <= 1; x += 2)
                    for (int z = -1; z <= 1; z += 2)
                        AddPart(kart.transform, "车轮", PrimitiveType.Cylinder,
                            new Vector3(x * 0.85f, 0.05f, z * 0.72f),
                            new Vector3(0.34f, 0.13f, 0.34f), dark,
                            Quaternion.Euler(0f, 0f, 90f));
                kart.AddComponent<KartOutline>().Apply(outline);
                opponents[i] = kart.AddComponent<KartAiDriver>();
                opponents[i].Configure(route, signal, 3, 28f + i * 2.2f,
                    (i - 1) * 2.2f, 9f + i * 7f);
            }
            return opponents;
        }

        private static Transform AddAnchor(Transform parent, string name, Vector3 offset)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = offset;
            return anchor.transform;
        }

        private static void AddPart(Transform parent, string name, PrimitiveType type,
            Vector3 offset, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent);
            part.transform.localPosition = offset;
            part.transform.localRotation = rotation ?? Quaternion.identity;
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
        }

        private static void BuildCamera(KartController kart)
        {
            GameObject cameraObject = new GameObject("追尾相机");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = kart.transform.position - kart.transform.forward * 7f
                + Vector3.up * 3.2f;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.66f, 0.84f, 0.96f);
            camera.farClipPlane = 500f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<KartChaseCamera>().SetTarget(kart);
        }
    }
}
