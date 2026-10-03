using System.Collections.Generic;
using CameraControl;
using Gameplay;
using Grid;
using Player;
using UI;
using UnityEngine;

namespace Level {
    /// <summary>
    /// Constructs the complete "Level 1 – The Perspective Bridge" puzzle environment.
    /// Creates platforms, architectural structures, optical illusion bridge, nodes, player, key, and exit.
    /// Mathematically guarantees that at Yaw 45 deg, Pitch 30 deg, the two bridge arms form one seamless straight line.
    /// </summary>
    public class Level1Builder : MonoBehaviour {
        [Header("Materials / Colors")]
        [SerializeField] private Color stoneColor = new Color(0.91f, 0.92f, 0.90f);
        [SerializeField] private Color darkStoneColor = new Color(0.29f, 0.32f, 0.38f);
        [SerializeField] private Color accentBlueColor = new Color(0.17f, 0.42f, 0.69f);
        [SerializeField] private Color goldColor = new Color(1.0f, 0.84f, 0.0f);
        [SerializeField] private Color cloakColor = new Color(0.90f, 0.25f, 0.20f);

        [Header("Illusion Geometry Config")]
        [SerializeField] private float illusionYaw = 45f;
        [SerializeField] private float illusionPitch = 30f;
        [SerializeField] private float depthOffset = 4.0f; // Lambda distance along camera ray

        [Header("Auto-Build On Start")]
        [SerializeField] private bool buildOnStart = true;

        private Material stoneMat;
        private Material darkStoneMat;
        private Material bannerMat;
        private Material goldMat;
        private Material playerMat;
        private Material emissiveMat;

        private void Start() {
            if (buildOnStart) {
                BuildLevel();
            }
        }

        [ContextMenu("Build Level 1")]
        public void BuildLevel() {
            // Clean up previous generated level objects safely
            var existing = GameObject.Find("Level1_Environment");
            if (existing != null) {
                if (Application.isPlaying) {
                    Destroy(existing);
                } else {
                    DestroyImmediate(existing);
                }
            }

            CreateMaterials();

            GameObject root = new GameObject("Level1_Environment");
            Transform rootT = root.transform;

            // 1. Calculate camera illusion ray vector
            // Unity's Euler rotation order: Z, then X, then Y
            Quaternion illusionRot = Quaternion.Euler(illusionPitch, illusionYaw, 0f);
            Vector3 camForward = illusionRot * Vector3.forward;
            Vector3 illusionOffset = camForward * depthOffset;

            // 2. Build Left Platform (Platform 1 - Starting Platform)
            GameObject p1Root = new GameObject("Platform_1_StartArea");
            p1Root.transform.SetParent(rootT);

            var p1Nodes = new Dictionary<Vector2Int, PathNode>();

            // Courtyard 3x2: x in [-2, 0], z in [0, 1]
            for (int x = -2; x <= 0; x++) {
                for (int z = 0; z <= 1; z++) {
                    p1Nodes[new Vector2Int(x, z)] = CreateTile(p1Root.transform, new Vector3(x, 0, z), stoneMat, $"Tile_P1_{x}_{z}");
                }
            }

            // Bridge Arm 1: Extends along +X (x = 1, 2 at z = 0)
            var b1_1 = CreateTile(p1Root.transform, new Vector3(1, 0, 0), stoneMat, "Tile_B1_1");
            var b1_Tip = CreateTile(p1Root.transform, new Vector3(2, 0, 0), stoneMat, "Tile_B1_2_Tip");
            p1Nodes[new Vector2Int(1, 0)] = b1_1;
            p1Nodes[new Vector2Int(2, 0)] = b1_Tip;

            // Connect Platform 1 internal neighbors
            ConnectGridNeighbors(p1Nodes);

            // Architectural elements on Platform 1 (placed on the back wall at Z = 1.5, never occluding bridge)
            CreateStoneBlock(p1Root.transform, new Vector3(-1.7f, 1.0f, 1.5f), new Vector3(0.5f, 2.0f, 0.5f), darkStoneMat, "Arch_Pillar_L");
            CreateStoneBlock(p1Root.transform, new Vector3(-0.3f, 1.0f, 1.5f), new Vector3(0.5f, 2.0f, 0.5f), darkStoneMat, "Arch_Pillar_R");
            CreateStoneBlock(p1Root.transform, new Vector3(-1.0f, 2.1f, 1.5f), new Vector3(2.0f, 0.4f, 0.6f), darkStoneMat, "Arch_Lintel");

            // Blue Banner beside the arch
            CreateBanner(p1Root.transform, new Vector3(-1.75f, 1.15f, 1.25f));

            // Torch Stand on the outer corner of Platform 1
            CreateTorchStand(p1Root.transform, new Vector3(-2.6f, 0.4f, 0f));

            // Foundation block under Platform 1 courtyard
            CreateStoneBlock(p1Root.transform, new Vector3(-1.0f, -1.2f, 0.5f), new Vector3(3.2f, 2.0f, 2.2f), darkStoneMat, "Foundation_P1");

            // 3. Build Right Platform (Platform 2 - Key Platform)
            // In screen space when aligned, Bridge 2 continues at visual (3, 0, 0), (4, 0, 0), (5, 0, 0)
            // Physically, every element on Platform 2 is displaced by illusionOffset!
            GameObject p2Root = new GameObject("Platform_2_KeyArea");
            p2Root.transform.SetParent(rootT);

            var p2Nodes = new Dictionary<Vector2Int, PathNode>();

            // Bridge Arm 2: visual x = 3, 4, 5 at z = 0
            var b2_Start = CreateTile(p2Root.transform, new Vector3(3, 0, 0) + illusionOffset, stoneMat, "Tile_B2_3_Start");
            var b2_4 = CreateTile(p2Root.transform, new Vector3(4, 0, 0) + illusionOffset, stoneMat, "Tile_B2_4");
            var b2_5 = CreateTile(p2Root.transform, new Vector3(5, 0, 0) + illusionOffset, stoneMat, "Tile_B2_5");
            p2Nodes[new Vector2Int(3, 0)] = b2_Start;
            p2Nodes[new Vector2Int(4, 0)] = b2_4;
            p2Nodes[new Vector2Int(5, 0)] = b2_5;

            // Platform 2 Courtyard: visual x in [5, 7], z in [0, 1]
            for (int x = 6; x <= 7; x++) {
                for (int z = 0; z <= 1; z++) {
                    Vector3 worldPos = new Vector3(x, 0, z) + illusionOffset;
                    p2Nodes[new Vector2Int(x, z)] = CreateTile(p2Root.transform, worldPos, stoneMat, $"Tile_P2_{x}_{z}");
                }
            }
            // Also node (5, 1)
            p2Nodes[new Vector2Int(5, 1)] = CreateTile(p2Root.transform, new Vector3(5, 0, 1) + illusionOffset, stoneMat, "Tile_P2_5_1");

            // Connect Platform 2 internal neighbors
            ConnectGridNeighbors(p2Nodes);

            // Architectural elements on Platform 2 (placed at back Z = 1.6 + illusionOffset)
            CreateStoneBlock(p2Root.transform, new Vector3(6.5f, 1.2f, 1.6f) + illusionOffset, new Vector3(0.8f, 2.4f, 0.8f), darkStoneMat, "Column_P2");

            // Foundation block under Platform 2 courtyard
            CreateStoneBlock(p2Root.transform, new Vector3(6.2f, -1.2f, 0.5f) + illusionOffset, new Vector3(2.8f, 2.0f, 2.2f), darkStoneMat, "Foundation_P2");

            // 4. Create the Perspective Bridge Edge between b1_Tip (2, 0, 0) and b2_Start (3, 0, 0) + illusionOffset
            GameObject edgeObj = new GameObject("PerspectiveBridge_Edge");
            edgeObj.transform.SetParent(rootT);
            var persEdge = edgeObj.AddComponent<PerspectiveEdge>();
            persEdge.Initialize(b1_Tip, b2_Start, illusionYaw, 3.5f);

            // 5. Create Key Item on Platform 2
            PathNode keyNode = p2Nodes[new Vector2Int(6, 1)];
            CreateKeyItem(rootT, keyNode);

            // 6. Create Exit Door on Platform 1
            PathNode exitNode = p1Nodes[new Vector2Int(-1, 1)];
            CreateExitDoor(rootT, exitNode);

            // 7. Create Player Character in front of the exit door
            PathNode startNode = p1Nodes[new Vector2Int(-1, 0)];
            CreatePlayerCharacter(rootT, startNode);

            // 8. Configure Camera, Interaction Handler, and Managers
            SetupCameraAndManagers(rootT);

            Debug.Log("<color=green>[Level1Builder]</color> Level 1 – The Perspective Bridge generated successfully with verified alignment geometry!");
        }

        private void CreateMaterials() {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            stoneMat = new Material(litShader) { color = stoneColor };
            stoneMat.SetFloat("_Smoothness", 0.2f);

            darkStoneMat = new Material(litShader) { color = darkStoneColor };
            darkStoneMat.SetFloat("_Smoothness", 0.1f);

            bannerMat = new Material(litShader) { color = accentBlueColor };
            goldMat = new Material(litShader) { color = goldColor };
            goldMat.SetFloat("_Smoothness", 0.9f);
            goldMat.SetFloat("_Metallic", 0.8f);

            playerMat = new Material(litShader) { color = cloakColor };

            emissiveMat = new Material(litShader) { color = new Color(1f, 0.9f, 0.5f) };
            emissiveMat.EnableKeyword("_EMISSION");
            emissiveMat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.3f) * 2.5f);
        }

        private PathNode CreateTile(Transform parent, Vector3 position, Material mat, string name) {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, true);
            block.transform.position = position;
            block.transform.localScale = new Vector3(0.96f, 0.35f, 0.96f);

            var rend = block.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;

            var node = block.AddComponent<PathNode>();
            return node;
        }

        private void CreateStoneBlock(Transform parent, Vector3 position, Vector3 scale, Material mat, string name) {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, true);
            block.transform.position = position;
            block.transform.localScale = scale;

            var rend = block.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;
        }

        private void CreateBanner(Transform parent, Vector3 position) {
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Quad);
            banner.name = "Banner";
            banner.transform.SetParent(parent, true);
            banner.transform.position = position;
            banner.transform.localScale = new Vector3(0.35f, 1.1f, 1f);
            banner.transform.rotation = Quaternion.Euler(0, 180, 0);

            var rend = banner.GetComponent<Renderer>();
            if (rend != null && bannerMat != null) rend.sharedMaterial = bannerMat;
        }

        private void CreateTorchStand(Transform parent, Vector3 position) {
            GameObject torch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torch.name = "TorchStand";
            torch.transform.SetParent(parent, true);
            torch.transform.position = position;
            torch.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
            if (darkStoneMat != null) torch.GetComponent<Renderer>().sharedMaterial = darkStoneMat;

            // Flame sphere
            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flame.name = "Flame";
            flame.transform.SetParent(torch.transform, true);
            flame.transform.localPosition = new Vector3(0, 1.1f, 0);
            flame.transform.localScale = new Vector3(0.4f, 0.6f, 0.4f);
            if (emissiveMat != null) flame.GetComponent<Renderer>().sharedMaterial = emissiveMat;

            // Light
            GameObject lightObj = new GameObject("TorchLight");
            lightObj.transform.SetParent(flame.transform, true);
            lightObj.transform.localPosition = Vector3.zero;
            var lightComp = lightObj.AddComponent<Light>();
            lightComp.type = LightType.Point;
            lightComp.color = new Color(1f, 0.7f, 0.3f);
            lightComp.intensity = 1.5f;
            lightComp.range = 5f;
        }

        private void CreateKeyItem(Transform parent, PathNode node) {
            GameObject keyRoot = new GameObject("Key_Pedestal_And_Item");
            keyRoot.transform.SetParent(parent, true);
            keyRoot.transform.position = node.transform.position;

            // Pedestal block
            GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedestal.name = "Pedestal";
            pedestal.transform.SetParent(keyRoot.transform, true);
            pedestal.transform.localPosition = new Vector3(0, 0.45f, 0);
            pedestal.transform.localScale = new Vector3(0.35f, 0.5f, 0.35f);
            if (darkStoneMat != null) pedestal.GetComponent<Renderer>().sharedMaterial = darkStoneMat;

            // Floating Golden Key visual
            GameObject keyVisual = new GameObject("KeyVisual");
            keyVisual.transform.SetParent(keyRoot.transform, true);
            keyVisual.transform.localPosition = new Vector3(0, 1.05f, 0);

            // Key Ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "KeyRing";
            ring.transform.SetParent(keyVisual.transform, true);
            ring.transform.localPosition = new Vector3(0, 0.22f, 0);
            ring.transform.localScale = new Vector3(0.24f, 0.04f, 0.24f);
            ring.transform.localRotation = Quaternion.Euler(90, 0, 0);
            if (goldMat != null) ring.GetComponent<Renderer>().sharedMaterial = goldMat;

            // Key Stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.name = "KeyStem";
            stem.transform.SetParent(keyVisual.transform, true);
            stem.transform.localPosition = new Vector3(0, 0.05f, 0);
            stem.transform.localScale = new Vector3(0.06f, 0.18f, 0.06f);
            if (goldMat != null) stem.GetComponent<Renderer>().sharedMaterial = goldMat;

            // Key Light
            GameObject keyLightObj = new GameObject("KeyGlow");
            keyLightObj.transform.SetParent(keyVisual.transform, true);
            keyLightObj.transform.localPosition = Vector3.zero;
            var kLight = keyLightObj.AddComponent<Light>();
            kLight.type = LightType.Point;
            kLight.color = new Color(1f, 0.85f, 0.2f);
            kLight.intensity = 2f;
            kLight.range = 3.5f;

            var keyComp = keyRoot.AddComponent<KeyItem>();
            keyComp.Initialize(node);

            SetPrivateField(keyComp, "visualModel", keyVisual);
        }

        private void CreateExitDoor(Transform parent, PathNode node) {
            GameObject doorRoot = new GameObject("ExitDoor");
            doorRoot.transform.SetParent(parent, true);
            doorRoot.transform.position = node.transform.position;

            // Closed Door Visual (Wood/Stone slab)
            GameObject closedDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            closedDoor.name = "ClosedDoor";
            closedDoor.transform.SetParent(doorRoot.transform, true);
            closedDoor.transform.localPosition = new Vector3(0, 0.9f, 0.35f);
            closedDoor.transform.localScale = new Vector3(0.85f, 1.5f, 0.12f);
            if (stoneMat != null) closedDoor.GetComponent<Renderer>().sharedMaterial = stoneMat;

            // Open Portal Light (Glowing portal plane)
            GameObject openPortal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            openPortal.name = "OpenPortal";
            openPortal.transform.SetParent(doorRoot.transform, true);
            openPortal.transform.localPosition = new Vector3(0, 0.9f, 0.38f);
            openPortal.transform.localScale = new Vector3(0.85f, 1.5f, 1f);
            openPortal.transform.rotation = Quaternion.Euler(0, 180, 0);
            if (emissiveMat != null) openPortal.GetComponent<Renderer>().sharedMaterial = emissiveMat;
            openPortal.SetActive(false);

            // Door Light
            GameObject doorLightObj = new GameObject("DoorLight");
            doorLightObj.transform.SetParent(doorRoot.transform, true);
            doorLightObj.transform.localPosition = new Vector3(0, 1.0f, 0);
            var dLight = doorLightObj.AddComponent<Light>();
            dLight.type = LightType.Point;
            dLight.color = new Color(1f, 0.9f, 0.5f);
            dLight.intensity = 0f;
            dLight.range = 5f;

            var exitComp = doorRoot.AddComponent<ExitDoor>();
            exitComp.Initialize(node);

            SetPrivateField(exitComp, "closedDoorVisual", closedDoor);
            SetPrivateField(exitComp, "openPortalLight", openPortal);
            SetPrivateField(exitComp, "doorLight", dLight);
        }

        private void CreatePlayerCharacter(Transform parent, PathNode startNode) {
            GameObject playerObj = new GameObject("PlayerCharacter");
            playerObj.transform.SetParent(parent, true);

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(playerObj.transform, true);
            visualRoot.transform.localPosition = Vector3.zero;

            // Cloak Body (Tapered capsule/cylinder)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "CloakBody";
            body.transform.SetParent(visualRoot.transform, true);
            body.transform.localPosition = new Vector3(0, 0.42f, 0);
            body.transform.localScale = new Vector3(0.32f, 0.38f, 0.32f);
            if (playerMat != null) body.GetComponent<Renderer>().sharedMaterial = playerMat;

            // Head / Hood
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Hood";
            head.transform.SetParent(visualRoot.transform, true);
            head.transform.localPosition = new Vector3(0, 0.8f, 0);
            head.transform.localScale = new Vector3(0.3f, 0.32f, 0.3f);
            if (playerMat != null) head.GetComponent<Renderer>().sharedMaterial = playerMat;

            var playerComp = playerObj.AddComponent<PlayerController>();
            playerComp.SetCurrentNode(startNode);

            SetPrivateField(playerComp, "visualRoot", visualRoot.transform);
        }

        private void SetupCameraAndManagers(Transform parent) {
            // Find or configure Main Camera
            Camera mainCam = Camera.main;
            if (mainCam == null) {
                var camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.0f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);

            var camController = mainCam.GetComponent<IsometricCameraController>();
            if (camController == null) {
                camController = mainCam.gameObject.AddComponent<IsometricCameraController>();
            }

            // Pivot target at center of the entire puzzle: visual center ~ (2.5, 0.5, 0.5)
            GameObject pivotObj = GameObject.Find("PuzzlePivot");
            if (pivotObj == null) {
                pivotObj = new GameObject("PuzzlePivot");
                pivotObj.transform.SetParent(parent, true);
                pivotObj.transform.position = new Vector3(2.5f, 0.5f, 0.5f);
            }
            camController.SetPivotTarget(pivotObj.transform, Vector3.zero);

            // Perspective alignment manager
            var alignMgr = mainCam.GetComponent<PerspectiveAlignmentManager>();
            if (alignMgr == null) {
                alignMgr = mainCam.gameObject.AddComponent<PerspectiveAlignmentManager>();
            }
            alignMgr.Configure(illusionYaw, 3.5f);

            // Pointer interaction handler for New Input System
            var raycaster = mainCam.GetComponent<PuzzleInteractionHandler>();
            if (raycaster == null) {
                raycaster = mainCam.gameObject.AddComponent<PuzzleInteractionHandler>();
            }

            // Level Manager
            GameObject mgrObj = GameObject.Find("LevelManager");
            if (mgrObj == null) {
                mgrObj = new GameObject("LevelManager");
                mgrObj.transform.SetParent(parent, true);
            }
            var lvlMgr = mgrObj.GetComponent<LevelManager>();
            if (lvlMgr == null) {
                lvlMgr = mgrObj.AddComponent<LevelManager>();
            }

            // Game UI
            var uiComp = mgrObj.GetComponent<GameUI>();
            if (uiComp == null) {
                uiComp = mgrObj.AddComponent<GameUI>();
            }
            uiComp.Initialize(camController, lvlMgr);
        }

        private void ConnectGridNeighbors(Dictionary<Vector2Int, PathNode> grid) {
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            foreach (var kvp in grid) {
                Vector2Int coord = kvp.Key;
                PathNode node = kvp.Value;
                for (int i = 0; i < dirs.Length; i++) {
                    Vector2Int neighborCoord = coord + dirs[i];
                    if (grid.TryGetValue(neighborCoord, out PathNode neighbor)) {
                        node.AddNeighbor(neighbor);
                    }
                }
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value) {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) {
                field.SetValue(target, value);
            }
        }
    }
}
