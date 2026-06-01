using System.IO;
using Perpectivas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PerpectivasEditor
{
    public static class ParadoxLabSceneBuilder
    {
        private const string RootFolder = "Assets/perpectivas";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string ScenePath = RootFolder + "/ParadoxLab.unity";
        private const string RenderTexturePath = RootFolder + "/RT_AlternateDimension.renderTexture";
        private const string VolumeProfilePath = RootFolder + "/ParadoxLabVolumeProfile.asset";
        private const int AlternateLayer = 30;

        [MenuItem("Tools/Perpectivas/Create Paradox Lab Scene")]
        public static void CreateScene()
        {
            EnsureFolders();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Physics.gravity = new Vector3(0f, -9.81f, 0f);

            MaterialSet materials = CreateMaterials();
            CreateLightingAndPostProcessing();

            GameObject root = new GameObject("Paradox Lab");
            PlayerBuild playerBuild = CreatePlayer(root.transform);
            Camera isoCamera = CreateIsometricCamera(root.transform);
            CreateCameraSwitcher(playerBuild.Player, playerBuild.FirstPersonCamera, isoCamera);

            CreateMainLabShell(root.transform, materials);
            CreatePuzzleOne(root.transform, materials);
            CreatePuzzleTwo(root.transform, materials, playerBuild.FirstPersonCamera);
            CreatePuzzleThree(root.transform, materials, playerBuild.FirstPersonCamera);
            CreateFinalRoom(root.transform, materials);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Selection.activeObject = sceneAsset;
            Debug.Log("[Perpectivas] Paradox Lab creado en " + ScenePath);
        }

        public static void CreateSceneFromCommandLine()
        {
            CreateScene();
            EditorApplication.Exit(0);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
                AssetDatabase.CreateFolder("Assets", "perpectivas");

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                AssetDatabase.CreateFolder(RootFolder, "Materials");

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "perpectivas"));
        }

        private static PlayerBuild CreatePlayer(Transform parent)
        {
            GameObject player = new GameObject("Player_ParadoxLab");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(0f, 0.05f, 0f);

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 1.85f;
            controller.radius = 0.34f;
            controller.center = new Vector3(0f, 0.92f, 0f);

            GameObject cameraObject = new GameObject("Camera_Main_FirstPerson");
            cameraObject.transform.SetParent(player.transform);
            cameraObject.transform.localPosition = new Vector3(0f, 1.68f, 0f);
            cameraObject.transform.localRotation = Quaternion.identity;

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 150f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.cullingMask &= ~(1 << AlternateLayer);
            cameraObject.AddComponent<AudioListener>();

            ParadoxFirstPersonController firstPerson = player.AddComponent<ParadoxFirstPersonController>();
            PerspectiveScaleGrabber grabber = player.AddComponent<PerspectiveScaleGrabber>();
            ParadoxInteractor interactor = player.AddComponent<ParadoxInteractor>();

            SetObject(firstPerson, "playerCamera", camera);
            SetObject(grabber, "grabCamera", camera);
            SetObject(interactor, "player", firstPerson);
            SetObject(interactor, "interactionCamera", camera);

            return new PlayerBuild(player, camera);
        }

        private static Camera CreateIsometricCamera(Transform parent)
        {
            GameObject cameraObject = new GameObject("Camera_Isometric_TAB");
            cameraObject.transform.SetParent(parent);
            cameraObject.transform.position = new Vector3(0f, 25f, -25f);
            cameraObject.transform.rotation = Quaternion.Euler(35f, 45f, 0f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 55f;
            camera.farClipPlane = 180f;
            camera.enabled = false;
            camera.cullingMask &= ~(1 << AlternateLayer);

            AudioListener listener = cameraObject.AddComponent<AudioListener>();
            listener.enabled = false;

            return camera;
        }

        private static void CreateCameraSwitcher(GameObject player, Camera firstPersonCamera, Camera isometricCamera)
        {
            ParadoxCameraSwitcher switcher = player.AddComponent<ParadoxCameraSwitcher>();
            SetObject(switcher, "firstPersonCamera", firstPersonCamera);
            SetObject(switcher, "isometricCamera", isometricCamera);
        }

        private static void CreateLightingAndPostProcessing()
        {
            GameObject sun = new GameObject("Directional Light - Cold Lab");
            Light sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.intensity = 0.65f;
            sunLight.color = new Color(0.72f, 0.84f, 1f);
            sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.045f, 0.05f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.02f, 0.025f, 0.04f);
            RenderSettings.fogDensity = 0.012f;

            CreatePointLight("Neon Blue Key", new Vector3(-7f, 4f, 18f), new Color(0.1f, 0.65f, 1f), 8f, 22f);
            CreatePointLight("Neon Magenta Key", new Vector3(7f, 4f, 38f), new Color(0.95f, 0.15f, 1f), 7f, 22f);
            CreatePointLight("Neon Cyan Final", new Vector3(0f, 5f, 68f), new Color(0.1f, 1f, 0.9f), 8f, 24f);

            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            }

            if (!profile.TryGet(out Bloom bloom))
                bloom = profile.Add<Bloom>(true);

            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(0.62f);

            if (!profile.TryGet(out ChromaticAberration chromaticAberration))
                chromaticAberration = profile.Add<ChromaticAberration>(true);

            chromaticAberration.intensity.Override(0.18f);

            if (!profile.TryGet(out LensDistortion lensDistortion))
                lensDistortion = profile.Add<LensDistortion>(true);

            lensDistortion.intensity.Override(-0.08f);

            if (!profile.TryGet(out ColorAdjustments colorAdjustments))
                colorAdjustments = profile.Add<ColorAdjustments>(true);

            colorAdjustments.contrast.Override(18f);
            colorAdjustments.saturation.Override(-6f);
            colorAdjustments.colorFilter.Override(new Color(0.84f, 0.92f, 1f));

            GameObject volumeObject = new GameObject("Global Post Processing");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private static void CreateMainLabShell(Transform parent, MaterialSet materials)
        {
            GameObject shell = new GameObject("Main Lab Shell");
            shell.transform.SetParent(parent);

            CreateCube("Floor_CompleteLab", new Vector3(0f, -0.1f, 36f), new Vector3(18f, 0.2f, 82f), materials.Floor, shell.transform);
            CreateCube("Ceiling_CompleteLab", new Vector3(0f, 7.2f, 36f), new Vector3(18f, 0.2f, 82f), materials.Ceiling, shell.transform);
            CreateCube("Wall_Left", new Vector3(-9.1f, 3.5f, 36f), new Vector3(0.25f, 7f, 82f), materials.Wall, shell.transform);
            CreateCube("Wall_Right", new Vector3(9.1f, 3.5f, 36f), new Vector3(0.25f, 7f, 82f), materials.Wall, shell.transform);
            CreateCube("Wall_Start", new Vector3(0f, 3.5f, -5f), new Vector3(18f, 7f, 0.25f), materials.Wall, shell.transform);
            CreateCube("Wall_End", new Vector3(0f, 3.5f, 77f), new Vector3(18f, 7f, 0.25f), materials.Wall, shell.transform);

            for (int i = 0; i < 7; i++)
            {
                float z = 2f + i * 11f;
                CreateCube("Neon_Runway_" + i, new Vector3(-8.85f, 0.04f, z), new Vector3(0.08f, 0.08f, 7.5f), materials.BlueNeon, shell.transform);
                CreateCube("Neon_Runway_R_" + i, new Vector3(8.85f, 0.04f, z + 5f), new Vector3(0.08f, 0.08f, 7.5f), materials.MagentaNeon, shell.transform);
            }

            CreateText("Controls_Label", "WASD mover | Mouse mirar | Click izq toma/suelta | E interactuar | TAB isometrica", new Vector3(0f, 2.35f, -3.9f), Quaternion.Euler(0f, 0f, 0f), 0.22f, shell.transform);
        }

        private static void CreatePuzzleOne(Transform parent, MaterialSet materials)
        {
            GameObject puzzle = new GameObject("Puzzle 1 - Perspective Scale");
            puzzle.transform.SetParent(parent);

            CreateText("Puzzle1_Label", "Puzzle 1: escala por perspectiva", new Vector3(0f, 2.5f, 4.2f), Quaternion.Euler(0f, 180f, 0f), 0.28f, puzzle.transform);
            CreateCube("Projection_Wall", new Vector3(0f, 2.2f, 14.5f), new Vector3(14f, 4.4f, 0.35f), materials.ProjectionWall, puzzle.transform);
            CreateCube("Raised_Platform_Target", new Vector3(0f, 4.05f, 19.2f), new Vector3(5f, 0.35f, 5f), materials.Platform, puzzle.transform);
            CreateCube("Elevated_Door_Frame_Left", new Vector3(-2.1f, 5.4f, 21.85f), new Vector3(0.32f, 2.9f, 0.35f), materials.DoorFrame, puzzle.transform);
            CreateCube("Elevated_Door_Frame_Right", new Vector3(2.1f, 5.4f, 21.85f), new Vector3(0.32f, 2.9f, 0.35f), materials.DoorFrame, puzzle.transform);
            CreateCube("Elevated_Door_Frame_Top", new Vector3(0f, 6.72f, 21.85f), new Vector3(4.2f, 0.32f, 0.35f), materials.DoorFrame, puzzle.transform);

            GameObject cube = CreateCube("Perspective_Cube_GrabMe", new Vector3(-3f, 0.65f, 7f), Vector3.one, materials.ScaleCube, puzzle.transform);
            cube.AddComponent<Rigidbody>();
            cube.AddComponent<PerspectiveScalable>();
        }

        private static DoorController CreatePuzzleTwo(Transform parent, MaterialSet materials, Camera firstPersonCamera)
        {
            GameObject puzzle = new GameObject("Puzzle 2 - Alternate Dimension Monitor");
            puzzle.transform.SetParent(parent);

            CreateText("Puzzle2_Label", "Puzzle 2: boton visible solo en monitor", new Vector3(0f, 2.5f, 25.1f), Quaternion.Euler(0f, 180f, 0f), 0.26f, puzzle.transform);
            CreateCube("WorldA_Barrier_Left", new Vector3(-5.9f, 2.1f, 43f), new Vector3(6.2f, 4.2f, 0.3f), materials.DoorFrame, puzzle.transform);
            CreateCube("WorldA_Barrier_Right", new Vector3(5.9f, 2.1f, 43f), new Vector3(6.2f, 4.2f, 0.3f), materials.DoorFrame, puzzle.transform);
            CreateCube("WorldA_Barrier_Top", new Vector3(0f, 4.25f, 43f), new Vector3(5.6f, 0.55f, 0.3f), materials.DoorFrame, puzzle.transform);
            DoorController door = CreateDoor("Door_WorldA_OpenFromMonitor", new Vector3(0f, 2.05f, 42.82f), new Vector3(2.4f, 3.4f, 0.28f), materials.Door, puzzle.transform);

            GameObject monitor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            monitor.name = "Monitor_RenderTexture_WorldB";
            monitor.transform.SetParent(puzzle.transform);
            monitor.transform.position = new Vector3(0f, 2.2f, 29.2f);
            monitor.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            monitor.transform.localScale = new Vector3(4.4f, 2.5f, 1f);
            monitor.GetComponent<Renderer>().sharedMaterial = materials.Monitor;

            CreateCube("Monitor_Frame_Top", new Vector3(0f, 3.54f, 29.18f), new Vector3(4.75f, 0.12f, 0.12f), materials.Black, puzzle.transform);
            CreateCube("Monitor_Frame_Bottom", new Vector3(0f, 0.86f, 29.18f), new Vector3(4.75f, 0.12f, 0.12f), materials.Black, puzzle.transform);
            CreateCube("Monitor_Frame_Left", new Vector3(-2.36f, 2.2f, 29.18f), new Vector3(0.12f, 2.75f, 0.12f), materials.Black, puzzle.transform);
            CreateCube("Monitor_Frame_Right", new Vector3(2.36f, 2.2f, 29.18f), new Vector3(0.12f, 2.75f, 0.12f), materials.Black, puzzle.transform);

            GameObject dimensionRoot = new GameObject("World_B_AlternateOnly");
            dimensionRoot.transform.SetParent(puzzle.transform);
            dimensionRoot.transform.position = new Vector3(45f, 0f, 31f);

            CreateCube("WorldB_Floor", new Vector3(45f, -0.1f, 31f), new Vector3(12f, 0.2f, 12f), materials.AltFloor, dimensionRoot.transform, AlternateLayer);
            CreateCube("WorldB_BackWall", new Vector3(45f, 2.8f, 36.8f), new Vector3(12f, 5.6f, 0.25f), materials.AltWall, dimensionRoot.transform, AlternateLayer);
            CreateCube("WorldB_LeftWall", new Vector3(39f, 2.8f, 31f), new Vector3(0.25f, 5.6f, 12f), materials.AltWall, dimensionRoot.transform, AlternateLayer);
            CreateCube("WorldB_RightWall", new Vector3(51f, 2.8f, 31f), new Vector3(0.25f, 5.6f, 12f), materials.AltWall, dimensionRoot.transform, AlternateLayer);

            GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            button.name = "Button_B_OnlyInRenderTexture";
            button.transform.SetParent(dimensionRoot.transform);
            button.transform.position = new Vector3(45f, 0.25f, 35.4f);
            button.transform.localScale = new Vector3(0.85f, 0.18f, 0.85f);
            button.GetComponent<Renderer>().sharedMaterial = materials.Button;
            button.layer = AlternateLayer;
            button.AddComponent<AlternateDimensionButton>();

            GameObject cameraObject = new GameObject("Camera_B_AlternateDimension");
            cameraObject.transform.SetParent(puzzle.transform);
            cameraObject.transform.position = new Vector3(45f, 2.4f, 25.4f);
            cameraObject.transform.LookAt(new Vector3(45f, 0.6f, 35.4f));

            Camera alternateCamera = cameraObject.AddComponent<Camera>();
            alternateCamera.fieldOfView = 48f;
            alternateCamera.nearClipPlane = 0.03f;
            alternateCamera.farClipPlane = 80f;
            alternateCamera.clearFlags = CameraClearFlags.SolidColor;
            alternateCamera.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
            alternateCamera.cullingMask = 1 << AlternateLayer;

            RenderTexture renderTexture = CreateRenderTextureAsset();
            AlternateDimensionManager manager = puzzle.AddComponent<AlternateDimensionManager>();
            SetObject(manager, "alternateCamera", alternateCamera);
            SetObject(manager, "worldADoor", door);
            SetInt(manager, "alternateButtonMask", 1 << AlternateLayer);

            MonitorInteraction interaction = monitor.AddComponent<MonitorInteraction>();
            SetObject(interaction, "dimensionManager", manager);

            RenderTextureController textureController = monitor.AddComponent<RenderTextureController>();
            SetObject(textureController, "sourceCamera", alternateCamera);
            SetObject(textureController, "monitorRenderer", monitor.GetComponent<Renderer>());
            SetObject(textureController, "renderTexture", renderTexture);
            textureController.Configure();

            return door;
        }

        private static void CreatePuzzleThree(Transform parent, MaterialSet materials, Camera firstPersonCamera)
        {
            GameObject puzzle = new GameObject("Puzzle 3 - Impossible Geometry");
            puzzle.transform.SetParent(parent);

            CreateText("Puzzle3_Label", "Puzzle 3: alinea la mirada para crear puente", new Vector3(0f, 2.5f, 47.1f), Quaternion.Euler(0f, 180f, 0f), 0.24f, puzzle.transform);
            CreateCube("Platform_A_Separated", new Vector3(-4.6f, 0.2f, 53f), new Vector3(4.6f, 0.4f, 4.2f), materials.Platform, puzzle.transform);
            CreateCube("Platform_B_Separated", new Vector3(4.4f, 0.2f, 58.3f), new Vector3(4.6f, 0.4f, 4.2f), materials.Platform, puzzle.transform);

            GameObject viewPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            viewPoint.name = "Alignment_ViewPoint_StandHere";
            viewPoint.transform.SetParent(puzzle.transform);
            viewPoint.transform.position = new Vector3(-5.9f, 0.03f, 49.6f);
            viewPoint.transform.localScale = new Vector3(1.3f, 0.03f, 1.3f);
            viewPoint.GetComponent<Renderer>().sharedMaterial = materials.BlueNeon;

            GameObject target = new GameObject("Alignment_Target_LookHere");
            target.transform.SetParent(puzzle.transform);
            target.transform.position = new Vector3(0.15f, 1.42f, 55.7f);

            GameObject bridge = CreateCube("ImpossibleBridge_ColliderAppears", new Vector3(0f, 0.55f, 55.6f), new Vector3(9.2f, 0.24f, 1.75f), materials.ImpossibleBridge, puzzle.transform);
            ImpossibleBridge impossibleBridge = bridge.AddComponent<ImpossibleBridge>();
            SetObject(impossibleBridge, "playerCamera", firstPersonCamera);
            SetObject(impossibleBridge, "requiredViewPoint", viewPoint.transform);
            SetObject(impossibleBridge, "alignmentTarget", target.transform);
            SetObject(impossibleBridge, "bridgeCollider", bridge.GetComponent<Collider>());
            SetObject(impossibleBridge, "bridgeRenderer", bridge.GetComponent<Renderer>());
        }

        private static void CreateFinalRoom(Transform parent, MaterialSet materials)
        {
            GameObject finalRoom = new GameObject("Final Room - Combined Mechanics");
            finalRoom.transform.SetParent(parent);

            CreateText("Final_Label", "Sala final: escala, monitor, gravedad y puente imposible", new Vector3(0f, 2.5f, 63.2f), Quaternion.Euler(0f, 180f, 0f), 0.22f, finalRoom.transform);

            GameObject gravityBase = CreateCube("GravitySwitch_Base", new Vector3(-5.7f, 0.55f, 64.2f), new Vector3(1.2f, 1.1f, 1.2f), materials.DoorFrame, finalRoom.transform);
            GameObject lever = CreateCube("GravitySwitch_Lever", new Vector3(-5.7f, 1.35f, 64.2f), new Vector3(0.22f, 0.22f, 1.2f), materials.MagentaNeon, finalRoom.transform);
            GravitySwitch gravitySwitch = gravityBase.AddComponent<GravitySwitch>();
            SetObject(gravitySwitch, "animatedLever", lever.transform);

            CreateCube("WallFloor_WhenGravityRight", new Vector3(7.7f, 3.2f, 67.6f), new Vector3(0.32f, 6.4f, 11f), materials.Platform, finalRoom.transform);
            CreateCube("WallFloor_WhenGravityLeft", new Vector3(-7.7f, 3.2f, 67.6f), new Vector3(0.32f, 6.4f, 11f), materials.Platform, finalRoom.transform);
            CreateCube("Final_Camera_Ledge", new Vector3(0f, 5.15f, 66.8f), new Vector3(4.4f, 0.32f, 3.8f), materials.Platform, finalRoom.transform);

            GameObject finalCube = CreateCube("Final_PerspectiveCube", new Vector3(3.3f, 0.65f, 62.8f), Vector3.one, materials.ScaleCube, finalRoom.transform);
            finalCube.AddComponent<Rigidbody>();
            finalCube.AddComponent<PerspectiveScalable>();

            GameObject exit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            exit.name = "ExitPortal_Trigger";
            exit.transform.SetParent(finalRoom.transform);
            exit.transform.position = new Vector3(0f, 1.5f, 73.8f);
            exit.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            exit.transform.localScale = new Vector3(1.4f, 0.08f, 1.4f);
            exit.GetComponent<Renderer>().sharedMaterial = materials.Portal;
            Collider exitCollider = exit.GetComponent<Collider>();
            exitCollider.isTrigger = true;
            exit.AddComponent<ExitPortal>();

        }

        private static DoorController CreateDoor(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject doorRoot = new GameObject(name);
            doorRoot.transform.SetParent(parent);
            doorRoot.transform.position = position;

            GameObject panel = CreateCube(name + "_Panel", position, scale, material, doorRoot.transform);
            panel.transform.localPosition = Vector3.zero;

            DoorController door = doorRoot.AddComponent<DoorController>();
            SetObject(door, "doorPanel", panel.transform);

            return door;
        }

        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent, int layer = 0)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            cube.layer = layer;

            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;

            return cube;
        }

        private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static void CreateText(string name, string text, Vector3 position, Quaternion rotation, float size, Transform parent)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent);
            textObject.transform.position = position;
            textObject.transform.rotation = rotation;

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = size;
            textMesh.fontSize = 64;
            textMesh.color = Color.white;
        }

        private static RenderTexture CreateRenderTextureAsset()
        {
            RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(RenderTexturePath);
            if (texture != null)
                return texture;

            texture = new RenderTexture(1024, 576, 24)
            {
                name = "RT_AlternateDimension"
            };

            AssetDatabase.CreateAsset(texture, RenderTexturePath);
            return texture;
        }

        private static MaterialSet CreateMaterials()
        {
            return new MaterialSet
            {
                Floor = CreateMaterial("M_Floor_DarkGraphite", new Color(0.08f, 0.09f, 0.11f)),
                Ceiling = CreateMaterial("M_Ceiling_Lab", new Color(0.055f, 0.06f, 0.075f)),
                Wall = CreateMaterial("M_Wall_CoolConcrete", new Color(0.16f, 0.18f, 0.22f)),
                ProjectionWall = CreateMaterial("M_ProjectionWall_Matte", new Color(0.72f, 0.76f, 0.82f)),
                Platform = CreateMaterial("M_Platform_WhiteSteel", new Color(0.62f, 0.68f, 0.74f)),
                DoorFrame = CreateMaterial("M_DoorFrame_Dark", new Color(0.04f, 0.045f, 0.055f)),
                Door = CreateMaterial("M_Door_CyanLocked", new Color(0.08f, 0.55f, 0.75f), new Color(0.04f, 0.5f, 0.9f), 1.3f),
                ScaleCube = CreateMaterial("M_PerspectiveCube", new Color(0.95f, 0.9f, 0.35f), new Color(0.9f, 0.55f, 0.1f), 1.1f),
                Monitor = CreateMaterial("M_Monitor_RenderTexture", Color.black, Color.white, 1.2f),
                Button = CreateMaterial("M_Button_AltMagenta", new Color(1f, 0.12f, 0.8f), new Color(1f, 0.12f, 0.8f), 2.2f),
                AltFloor = CreateMaterial("M_WorldB_Floor", new Color(0.12f, 0.04f, 0.18f)),
                AltWall = CreateMaterial("M_WorldB_Wall", new Color(0.2f, 0.08f, 0.28f), new Color(0.22f, 0.05f, 0.45f), 0.8f),
                BlueNeon = CreateMaterial("M_Neon_Blue", new Color(0.04f, 0.48f, 1f), new Color(0.04f, 0.48f, 1f), 3.5f),
                MagentaNeon = CreateMaterial("M_Neon_Magenta", new Color(1f, 0.1f, 0.85f), new Color(1f, 0.1f, 0.85f), 3.2f),
                Black = CreateMaterial("M_Black_Glass", new Color(0.005f, 0.006f, 0.008f)),
                ImpossibleBridge = CreateMaterial("M_ImpossibleBridge_Cyan", new Color(0.1f, 1f, 0.88f), new Color(0.05f, 1f, 0.85f), 2.4f),
                Portal = CreateMaterial("M_ExitPortal", new Color(0.1f, 1f, 0.75f), new Color(0.1f, 1f, 0.75f), 4f)
            };
        }

        private static Material CreateMaterial(string name, Color baseColor)
        {
            return CreateMaterial(name, baseColor, Color.black, 0f);
        }

        private static Material CreateMaterial(string name, Color baseColor, Color emissionColor, float emissionIntensity)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");

                material = new Material(shader)
                {
                    name = name
                };

                AssetDatabase.CreateAsset(material, path);
            }

            material.color = baseColor;

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", baseColor);

            if (emissionIntensity > 0f && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor * emissionIntensity);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogWarning("[Perpectivas] No se encontro propiedad: " + propertyName + " en " + target.name);
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                Debug.LogWarning("[Perpectivas] No se encontro propiedad: " + propertyName + " en " + target.name);
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct PlayerBuild
        {
            public PlayerBuild(GameObject player, Camera firstPersonCamera)
            {
                Player = player;
                FirstPersonCamera = firstPersonCamera;
            }

            public GameObject Player { get; }
            public Camera FirstPersonCamera { get; }
        }

        private sealed class MaterialSet
        {
            public Material Floor;
            public Material Ceiling;
            public Material Wall;
            public Material ProjectionWall;
            public Material Platform;
            public Material DoorFrame;
            public Material Door;
            public Material ScaleCube;
            public Material Monitor;
            public Material Button;
            public Material AltFloor;
            public Material AltWall;
            public Material BlueNeon;
            public Material MagentaNeon;
            public Material Black;
            public Material ImpossibleBridge;
            public Material Portal;
        }
    }
}
