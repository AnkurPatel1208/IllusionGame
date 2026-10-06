using System.Collections.Generic;
using CameraControl;
using Gameplay;
using Grid;
using Player;
using UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Level {
    /// <summary>
    /// Constructs the elegant, serene "Level 1 – The Perspective Bridge" environment
    /// with the iconic Monument Valley Garden diorama aesthetic:
    /// - Original Level 1 puzzle geometry, pathfinding nodes, player Ida, and perspective bridge
    /// - Dedicated exit platform terrace behind and left of the Gatehouse Room with complete level tile
    /// - 100% solid, grounded structural connections (zero floating roof, pillar, or dome objects)
    /// - Pristine pastel color harmony (Terracotta-Peach monuments, Warm Ivory paths, Mint accents, Burnished Gold domes)
    /// - Warm golden parchment sky (#F5E8A2) with gentle vignette framing
    /// - Tiered circular diorama grass meadow base comfortably larger than the level foundations
    /// - Clean and crisp: no small clutter or pegs on the floor
    /// - Bright, warm, serene Monument Valley sunlight
    /// </summary>
    public class Level1Builder : MonoBehaviour {
        [Header("Monument Valley Palette")]
        [SerializeField] private Color monumentPeachColor = new Color(0.93f, 0.62f, 0.55f);
        [SerializeField] private Color pathIvoryColor = Color.white;
        [SerializeField] private Color trimMintColor = new Color(0.40f, 0.74f, 0.70f);
        [SerializeField] private Color accentGoldColor = new Color(0.96f, 0.78f, 0.32f);
        [SerializeField] private Color foundationSandColor = new Color(0.85f, 0.52f, 0.48f);
        [SerializeField] private Color grassLushColor = new Color(0.72f, 0.84f, 0.32f);
        [SerializeField] private Color grassEdgeColor = new Color(0.80f, 0.88f, 0.40f);
        [SerializeField] private Color skyColor = new Color(0.965f, 0.902f, 0.612f);

        [Header("Illusion Geometry Config")]
        [SerializeField] private float illusionYaw = 45f;
        [SerializeField] private float illusionPitch = 30f;
        [SerializeField] private float depthOffset = 4.0f;

        [Header("Meadow Base Height")]
        [SerializeField] private float waterLevelY = -4.50f;

        [Header("Build Config")]
        [Tooltip("If false, the level is baked directly as scene objects in the editor")]
        [SerializeField] private bool buildOnStart = false;

        public bool BuildOnStart {
            get => buildOnStart;
            set => buildOnStart = value;
        }

        public void AutoAssignDungeonAssets() {
            pathIvoryColor = Color.white;
        }

        // Procedural Monument Valley Materials
        private Material wallPeachMat;
        private Material pathIvoryMat;
        private Material trimMintMat;
        private Material goldMat;
        private Material foundationSandMat;
        private Material grassLushMat;
        private Material grassEdgeMat;
        private Material doorIndigoMat;
        private Material doorSunlightMat;
        private Material idaDressMat;
        private Material idaSkinMat;
        private Material treeTrunkMat;
        private Material treeFoliageMat;

#if UNITY_EDITOR
        [ContextMenu("Bake Level 1 To Scene")]
        public void BakeLevelToScene() {
            buildOnStart = false;
            BuildLevel();
            EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("<color=green>[Level1Builder]</color> Level 1 baked into scene hierarchy!");
        }

        [ContextMenu("Clear Baked Level")]
        public void ClearBakedLevel() {
            GameObject existing = GameObject.Find("Level1_Environment");
            if (existing != null) {
                DestroyImmediate(existing);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
#endif

        private void Start() {
            if (buildOnStart) {
                BuildLevel();
            }
        }

        public void BuildLevel() {
            // Clean up old environment root if re-baking
            GameObject existing = GameObject.Find("Level1_Environment");
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
            Quaternion illusionRot = Quaternion.Euler(illusionPitch, illusionYaw, 0f);
            Vector3 camForward = illusionRot * Vector3.forward;
            Vector3 illusionOffset = camForward * depthOffset;

            // 2. Build Clean Diorama Garden Meadow (Circular tiered lawn comfortably larger than level)
            BuildGardenMeadow(rootT);

            // 3. Build Platform 1 (Starting Citadel, Gatehouse Room & Arched Exit Portal)
            GameObject p1Root = new GameObject("Platform_1_StartArea");
            p1Root.transform.SetParent(rootT, false);

            var p1Nodes = new Dictionary<Vector2Int, PathNode>();

            // Courtyard 3x2: x in [-2, 0], z in [0, 1]
            for (int x = -2; x <= 0; x++) {
                for (int z = 0; z <= 1; z++) {
                    p1Nodes[new Vector2Int(x, z)] = CreateTile(p1Root.transform, new Vector3(x, 0, z), $"Tile_P1_{x}_{z}");
                }
            }

            // Bridge Arm 1: Extends along +X (x = 1, 2 at z = 0)
            var b1_1 = CreateTile(p1Root.transform, new Vector3(1, 0, 0), "Tile_B1_1");
            var b1_Tip = CreateTile(p1Root.transform, new Vector3(2, 0, 0), "Tile_B1_2_Tip");
            p1Nodes[new Vector2Int(1, 0)] = b1_1;
            p1Nodes[new Vector2Int(2, 0)] = b1_Tip;

            ConnectGridNeighbors(p1Nodes);

            // ========================================================
            // EXIT PATH & PLATFORM TERRACE (Behind Door)
            // ========================================================
            // 1. Vestibule threshold tile inside the tower doorway (at x = -1, z = 2)
            var vestibuleNode = CreateTile(p1Root.transform, new Vector3(-1f, 0f, 2f), "Tile_P1_Vestibule");

            // 2. Side portal passage tile connecting tower interior to terrace (at x = -2, z = 2)
            var terraceEntryNode = CreateTile(p1Root.transform, new Vector3(-2f, 0f, 2f), "Tile_P1_Terrace_Entry");

            // 3. Dedicated Complete Level Tile on the terrace (at x = -3, z = 2)
            var exitPlatformNode = CreateCompleteLevelTile(p1Root.transform, new Vector3(-3f, 0f, 2f), "Tile_P1_ExitPlatform");

            // 4. North extension tiles of the 2x2 terrace platform (at z = 3)
            var terraceN1 = CreateTile(p1Root.transform, new Vector3(-2f, 0f, 3f), "Tile_P1_Terrace_N1");
            var terraceN2 = CreateTile(p1Root.transform, new Vector3(-3f, 0f, 3f), "Tile_P1_Terrace_N2");

            // Connect exit path nodes explicitly (so courtyard (-2, 1) cannot bypass the door)
            PathNode entranceNode = p1Nodes[new Vector2Int(-1, 1)]; // Front courtyard tile
            entranceNode.AddNeighbor(vestibuleNode);
            vestibuleNode.AddNeighbor(entranceNode);

            vestibuleNode.AddNeighbor(terraceEntryNode);
            terraceEntryNode.AddNeighbor(vestibuleNode);

            terraceEntryNode.AddNeighbor(exitPlatformNode);
            exitPlatformNode.AddNeighbor(terraceEntryNode);

            terraceEntryNode.AddNeighbor(terraceN1);
            terraceN1.AddNeighbor(terraceEntryNode);

            exitPlatformNode.AddNeighbor(terraceN2);
            terraceN2.AddNeighbor(exitPlatformNode);

            terraceN1.AddNeighbor(terraceN2);
            terraceN2.AddNeighbor(terraceN1);

            List<PathNode> lockedExitNodes = new List<PathNode> { vestibuleNode, terraceEntryNode, terraceN1, terraceN2 };

            // Cantilever geometric brackets under Bridge Arm 1
            BuildCantileverBracketsP1(p1Root.transform);

            // Stepped descending staircase on left flank
            BuildPlatform1Staircase(p1Root.transform);

            // Monolithic tiered foundation citadel under Platform 1 and Exit Terrace
            BuildPlatform1Foundation(p1Root.transform);

            // Platform 1 Architecture: Gatehouse Tower Room, Arched Portal, Exit Terrace Platform, Finials
            BuildPlatform1Architecture(p1Root.transform, exitPlatformNode, entranceNode, lockedExitNodes);

            // 4. Build Platform 2 (Right Sacred Key Sanctuary)
            GameObject p2Root = new GameObject("Platform_2_KeyArea");
            p2Root.transform.SetParent(rootT, false);

            var p2Nodes = new Dictionary<Vector2Int, PathNode>();

            // Bridge Arm 2: visual x = 3, 4, 5 at z = 0 displaced by illusionOffset
            var b2_Start = CreateTile(p2Root.transform, new Vector3(3, 0, 0) + illusionOffset, "Tile_B2_3_Start");
            var b2_4 = CreateTile(p2Root.transform, new Vector3(4, 0, 0) + illusionOffset, "Tile_B2_4");
            var b2_5 = CreateTile(p2Root.transform, new Vector3(5, 0, 0) + illusionOffset, "Tile_B2_5");
            p2Nodes[new Vector2Int(3, 0)] = b2_Start;
            p2Nodes[new Vector2Int(4, 0)] = b2_4;
            p2Nodes[new Vector2Int(5, 0)] = b2_5;

            // Platform 2 Courtyard: visual x in [6, 7], z in [0, 1] plus (5, 1)
            p2Nodes[new Vector2Int(5, 1)] = CreateTile(p2Root.transform, new Vector3(5, 0, 1) + illusionOffset, "Tile_P2_5_1");
            for (int x = 6; x <= 7; x++) {
                for (int z = 0; z <= 1; z++) {
                    Vector3 worldPos = new Vector3(x, 0, z) + illusionOffset;
                    p2Nodes[new Vector2Int(x, z)] = CreateTile(p2Root.transform, worldPos, $"Tile_P2_{x}_{z}");
                }
            }

            ConnectGridNeighbors(p2Nodes);

            // Cantilever geometric brackets under Bridge Arm 2
            BuildCantileverBracketsP2(p2Root.transform, illusionOffset);

            // Monolithic tiered foundation citadel under Platform 2
            BuildPlatform2Foundation(p2Root.transform, illusionOffset);

            // Platform 2 Architecture: Domed Sanctuary Pavilion, Stepped Terrace
            BuildPlatform2Architecture(p2Root.transform, illusionOffset);

            // 5. Perspective Bridge Edge (connecting b1_Tip and b2_Start)
            GameObject edgeObj = new GameObject("PerspectiveBridge_Edge");
            edgeObj.transform.SetParent(rootT, false);
            var persEdge = edgeObj.AddComponent<PerspectiveEdge>();
            persEdge.Initialize(b1_Tip, b2_Start, illusionYaw, 3.5f);

            // 6. Sacred Geometric Key Item on Platform 2 (Floating cleanly above tile without bulky pedestal)
            PathNode keyNode = p2Nodes[new Vector2Int(6, 1)];
            CreateKeyItemAndAltar(p2Root.transform, keyNode);

            // 7. Player Character (Princess Ida Silhouette)
            PathNode startNode = p1Nodes[new Vector2Int(-1, 0)];
            CreatePlayerCharacter(rootT, startNode);

            // 8. Setup Camera, Monument Valley Lighting, and UI Managers
            SetupCameraAndLighting(rootT, illusionOffset);

            Debug.Log("<color=green>[Level1Builder]</color> Level 1 successfully built with exit platform terrace and clean key artifact!");
        }

        private void CreateMaterials() {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material MakeMat(Color c, float smoothness = 0.15f, float metallic = 0f, string name = "Mat") {
                var m = new Material(litShader) { color = c, name = name };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Metallic", metallic);
                return m;
            }

            wallPeachMat = MakeMat(monumentPeachColor, 0.18f, 0f, "M_MV_PeachWall");
            pathIvoryColor = Color.white;
            pathIvoryMat = MakeMat(Color.white, 0.12f, 0f, "M_MV_IvoryPath");
            trimMintMat = MakeMat(trimMintColor, 0.20f, 0f, "M_MV_MintTrim");
            foundationSandMat = MakeMat(foundationSandColor, 0.15f, 0f, "M_MV_SandFoundation");
            grassLushMat = MakeMat(grassLushColor, 0.08f, 0f, "M_MV_GrassLush");
            grassEdgeMat = MakeMat(grassEdgeColor, 0.08f, 0f, "M_MV_GrassEdge");

            goldMat = MakeMat(accentGoldColor, 0.65f, 0.50f, "M_MV_BurnishedGold");
            goldMat.EnableKeyword("_EMISSION");
            goldMat.SetColor("_EmissionColor", accentGoldColor * 1.5f);

            doorIndigoMat = MakeMat(new Color(0.12f, 0.18f, 0.28f), 0.25f, 0f, "M_MV_DoorIndigo");

            doorSunlightMat = MakeMat(new Color(1.0f, 0.95f, 0.82f), 0.10f, 0f, "M_MV_DoorSunlight");
            doorSunlightMat.EnableKeyword("_EMISSION");
            doorSunlightMat.SetColor("_EmissionColor", new Color(1.0f, 0.92f, 0.70f) * 3.5f);

            idaDressMat = MakeMat(new Color(0.98f, 0.98f, 0.98f), 0.10f, 0f, "M_MV_IdaDress");
            idaSkinMat = MakeMat(new Color(1.0f, 0.88f, 0.82f), 0.10f, 0f, "M_MV_IdaSkin");

            treeTrunkMat = MakeMat(new Color(0.92f, 0.90f, 0.85f), 0.15f, 0f, "M_MV_TreeTrunk");
            treeFoliageMat = MakeMat(new Color(0.48f, 0.76f, 0.66f), 0.15f, 0f, "M_MV_TreeFoliage");
        }

        private void BuildGardenMeadow(Transform parent) {
            GameObject meadowRoot = new GameObject("Monument_Garden_Meadow");
            meadowRoot.transform.SetParent(parent, false);

            Vector3 center = new Vector3(3.10f, waterLevelY, 2.00f);

            // Tier 1 - Wide base skirt (subtle gentle rim)
            CreateCylinder(meadowRoot.transform, center + new Vector3(0, -0.15f, 0), new Vector3(26.0f, 0.25f, 22.0f), grassEdgeMat, "Meadow_Base_Skirt");

            // Tier 2 - Main manicured circular/oval lawn
            CreateCylinder(meadowRoot.transform, center + new Vector3(0, 0.05f, 0), new Vector3(22.5f, 0.35f, 18.5f), grassLushMat, "Meadow_Main_Lawn");

            // Tier 3 - Central gentle elevation mound directly under foundations
            CreateCylinder(meadowRoot.transform, center + new Vector3(0, 0.22f, 0), new Vector3(18.5f, 0.22f, 15.0f), grassLushMat, "Meadow_Center_Elevation");
        }

        private void BuildCantileverBracketsP1(Transform parent) {
            GameObject wedgeRoot = new GameObject("CantileverBrackets_P1");
            wedgeRoot.transform.SetParent(parent, false);

            // Under Tile_B1_1 and Tile_B1_2_Tip
            CreateBlock(wedgeRoot.transform, new Vector3(1.0f, -0.25f, 0.0f), new Vector3(0.96f, 0.30f, 0.96f), trimMintMat, "Bracket_1_0");
            CreateBlock(wedgeRoot.transform, new Vector3(2.0f, -0.25f, 0.0f), new Vector3(0.96f, 0.30f, 0.96f), trimMintMat, "Bracket_2_0");

            // Stepped corbel wedge supporting the arm
            CreateBlock(wedgeRoot.transform, new Vector3(1.25f, -0.60f, 0.0f), new Vector3(1.70f, 0.42f, 0.90f), wallPeachMat, "Corbel_Tier_1");
            CreateBlock(wedgeRoot.transform, new Vector3(0.70f, -1.00f, 0.0f), new Vector3(1.30f, 0.42f, 0.90f), wallPeachMat, "Corbel_Tier_2");
            CreateBlock(wedgeRoot.transform, new Vector3(0.20f, -1.40f, 0.0f), new Vector3(0.90f, 0.42f, 0.90f), trimMintMat, "Corbel_Toe");
        }

        private void BuildCantileverBracketsP2(Transform parent, Vector3 offset) {
            GameObject wedgeRoot = new GameObject("CantileverBrackets_P2");
            wedgeRoot.transform.SetParent(parent, false);

            CreateBlock(wedgeRoot.transform, new Vector3(3.0f, -0.25f, 0.0f) + offset, new Vector3(0.96f, 0.30f, 0.96f), trimMintMat, "Bracket_3_0");
            CreateBlock(wedgeRoot.transform, new Vector3(4.0f, -0.25f, 0.0f) + offset, new Vector3(0.96f, 0.30f, 0.96f), trimMintMat, "Bracket_4_0");
            CreateBlock(wedgeRoot.transform, new Vector3(5.0f, -0.25f, 0.0f) + offset, new Vector3(0.96f, 0.30f, 0.96f), trimMintMat, "Bracket_5_0");

            CreateBlock(wedgeRoot.transform, new Vector3(3.75f, -0.60f, 0.0f) + offset, new Vector3(1.70f, 0.42f, 0.90f), wallPeachMat, "Corbel_P2_Tier_1");
            CreateBlock(wedgeRoot.transform, new Vector3(4.30f, -1.00f, 0.0f) + offset, new Vector3(1.30f, 0.42f, 0.90f), wallPeachMat, "Corbel_P2_Tier_2");
            CreateBlock(wedgeRoot.transform, new Vector3(4.80f, -1.40f, 0.0f) + offset, new Vector3(0.90f, 0.42f, 0.90f), trimMintMat, "Corbel_P2_Toe");
        }

        private void BuildPlatform1Staircase(Transform parent) {
            GameObject stairsRoot = new GameObject("Left_Staircase");
            stairsRoot.transform.SetParent(parent, false);

            CreateBlock(stairsRoot.transform, new Vector3(-2.65f, -0.15f, 0.0f), new Vector3(0.40f, 0.15f, 0.96f), pathIvoryMat, "Stair_Step_1");
            CreateBlock(stairsRoot.transform, new Vector3(-3.05f, -0.30f, 0.0f), new Vector3(0.40f, 0.15f, 0.96f), pathIvoryMat, "Stair_Step_2");
            CreateBlock(stairsRoot.transform, new Vector3(-3.45f, -0.45f, 0.0f), new Vector3(0.40f, 0.15f, 0.96f), pathIvoryMat, "Stair_Step_3");

            // Viewing balcony platform at stair base (top surface at Y = -0.95 + 0.425 = -0.525f)
            CreateBlock(stairsRoot.transform, new Vector3(-3.25f, -0.95f, 0.0f), new Vector3(1.45f, 0.85f, 1.15f), wallPeachMat, "Stairs_Plinth");

            // Decorative corner pillar resting flush on plinth (from Y = -0.525f to +0.35f, height = 0.875f)
            CreateCylinder(stairsRoot.transform, new Vector3(-3.90f, -0.0875f, 0.0f), new Vector3(0.24f, 0.875f, 0.24f), trimMintMat, "Stair_Finial_Pillar");
            CreateSphere(stairsRoot.transform, new Vector3(-3.90f, 0.48f, 0.0f), new Vector3(0.32f, 0.32f, 0.32f), goldMat, "Stair_Finial_Ball");
        }

        private void BuildPlatform1Foundation(Transform parent) {
            GameObject fRoot = new GameObject("Foundation_Citadel_P1");
            fRoot.transform.SetParent(parent, false);

            float waterY = waterLevelY;
            Vector3 fCenter = new Vector3(-1.0f, 0, 1.50f);

            // Tier 1: Just below the courtyard slabs (Y: 0.0 to -1.30, supports courtyard and tower)
            CreateBlock(fRoot.transform, new Vector3(fCenter.x, -0.65f, fCenter.z), new Vector3(3.20f, 1.30f, 4.00f), wallPeachMat, "Foundation_Tier_1");

            // Dedicated foundation corner buttresses directly supporting the front facade pilasters
            CreateBlock(fRoot.transform, new Vector3(-2.55f, -0.65f, 1.35f), new Vector3(0.48f, 1.30f, 0.48f), wallPeachMat, "Foundation_P1_Buttress_L");
            CreateBlock(fRoot.transform, new Vector3(0.55f, -0.65f, 1.35f), new Vector3(0.48f, 1.30f, 0.48f), wallPeachMat, "Foundation_P1_Buttress_R");

            // Mint trim belt 1
            CreateBlock(fRoot.transform, new Vector3(fCenter.x, -1.35f, fCenter.z), new Vector3(3.35f, 0.14f, 4.15f), trimMintMat, "Foundation_Belt_1");

            // Tier 2: Middle shaft (Y: -1.40 to -3.00)
            CreateBlock(fRoot.transform, new Vector3(fCenter.x, -2.20f, fCenter.z), new Vector3(3.45f, 1.60f, 4.25f), foundationSandMat, "Foundation_Tier_2");

            // Mint trim belt 2
            CreateBlock(fRoot.transform, new Vector3(fCenter.x, -3.05f, fCenter.z), new Vector3(3.60f, 0.14f, 4.40f), trimMintMat, "Foundation_Belt_2");

            // Tier 3: Submerged bedrock plinth anchored into meadow (Y: -3.10 to waterY - 0.40f)
            float tier3Height = (-3.10f) - (waterY - 0.40f);
            float tier3CenterY = -3.10f - (tier3Height * 0.5f);
            CreateBlock(fRoot.transform, new Vector3(fCenter.x, tier3CenterY, fCenter.z), new Vector3(3.85f, tier3Height, 4.60f), wallPeachMat, "Foundation_Plinth_Meadow");

            // ========================================================
            // Dedicated Foundation Citadel Bastion under Exit Terrace Platform
            // (Seamlessly joins main citadel foundation, descending into lawn)
            // ========================================================
            Vector3 terraceCenter = new Vector3(-2.60f, 0, 2.50f);
            CreateBlock(fRoot.transform, new Vector3(terraceCenter.x, -0.65f, terraceCenter.z), new Vector3(2.10f, 1.30f, 2.30f), wallPeachMat, "Foundation_Terrace_Tier_1");
            CreateBlock(fRoot.transform, new Vector3(terraceCenter.x, -1.35f, terraceCenter.z), new Vector3(2.25f, 0.14f, 2.45f), trimMintMat, "Foundation_Terrace_Belt_1");
            CreateBlock(fRoot.transform, new Vector3(terraceCenter.x, -2.20f, terraceCenter.z), new Vector3(2.35f, 1.60f, 2.55f), foundationSandMat, "Foundation_Terrace_Tier_2");
            CreateBlock(fRoot.transform, new Vector3(terraceCenter.x, -3.05f, terraceCenter.z), new Vector3(2.45f, 0.14f, 2.65f), trimMintMat, "Foundation_Terrace_Belt_2");
            CreateBlock(fRoot.transform, new Vector3(terraceCenter.x, tier3CenterY, terraceCenter.z), new Vector3(2.65f, tier3Height, 2.85f), wallPeachMat, "Foundation_Terrace_Plinth_Meadow");
        }

        private void BuildPlatform2Foundation(Transform parent, Vector3 offset) {
            GameObject fRoot = new GameObject("Foundation_Citadel_P2");
            fRoot.transform.SetParent(parent, false);

            float waterY = waterLevelY;

            // 1. Foundation under Playable Courtyard (Z = 0.5f)
            CreateBlock(fRoot.transform, new Vector3(6.0f, -0.70f, 0.5f) + offset, new Vector3(3.10f, 1.40f, 2.10f), wallPeachMat, "Foundation_P2_Tier_1");
            CreateBlock(fRoot.transform, new Vector3(6.0f, -1.45f, 0.5f) + offset, new Vector3(3.25f, 0.14f, 2.25f), trimMintMat, "Foundation_P2_Belt_1");

            // Dedicated foundation bastion piers supporting the two corner obelisks solidly from below
            Vector3 obeliskL_Pos = new Vector3(4.60f, 0f, 1.25f) + offset;
            Vector3 obeliskR_Pos = new Vector3(7.40f, 0f, -0.25f) + offset;
            CreateBlock(fRoot.transform, new Vector3(obeliskL_Pos.x, -0.70f, obeliskL_Pos.z), new Vector3(0.50f, 1.40f, 0.50f), wallPeachMat, "Foundation_P2_ObeliskPier_L");
            CreateBlock(fRoot.transform, new Vector3(obeliskL_Pos.x, -1.45f, obeliskL_Pos.z), new Vector3(0.54f, 0.14f, 0.54f), trimMintMat, "Foundation_P2_ObeliskBelt_L");
            CreateBlock(fRoot.transform, new Vector3(obeliskR_Pos.x, -0.70f, obeliskR_Pos.z), new Vector3(0.50f, 1.40f, 0.50f), wallPeachMat, "Foundation_P2_ObeliskPier_R");
            CreateBlock(fRoot.transform, new Vector3(obeliskR_Pos.x, -1.45f, obeliskR_Pos.z), new Vector3(0.54f, 0.14f, 0.54f), trimMintMat, "Foundation_P2_ObeliskBelt_R");

            // Tier 2 entering meadow
            float worldBaseY = -2.00f;
            float p2Tier2Height = (worldBaseY - 1.50f) - (waterY - 0.40f);
            float p2Tier2CenterY = -1.50f - (p2Tier2Height * 0.5f);
            CreateBlock(fRoot.transform, new Vector3(6.0f, p2Tier2CenterY, 0.5f) + offset, new Vector3(3.45f, p2Tier2Height, 2.45f), foundationSandMat, "Foundation_P2_Plinth_Meadow");

            // 2. Colossal Stone Bastion under Elevated Sanctuary Terrace (Z = 2.65f)
            float terraceWorldY = 0.50f + offset.y; // -1.50f
            float bastionHeight = terraceWorldY - (waterY - 0.40f);
            float bastionCenterLocalY = 0.50f - (bastionHeight * 0.5f);
            Vector3 bastionPos = new Vector3(6.0f, bastionCenterLocalY, 2.65f) + offset;
            CreateBlock(fRoot.transform, bastionPos, new Vector3(4.10f, bastionHeight, 1.90f), wallPeachMat, "Foundation_P2_Terrace_Bastion");
        }

        private void BuildPlatform1Architecture(Transform parent, PathNode exitNode, PathNode entranceNode, List<PathNode> additionalLockedNodes) {
            GameObject archRoot = new GameObject("Architecture_P1");
            archRoot.transform.SetParent(parent, false);

            // ==========================================
            // 1. GATEHOUSE TOWER ROOM BEHIND THE DOOR
            // ==========================================
            GameObject roomRoot = new GameObject("Gatehouse_Room_P1");
            roomRoot.transform.SetParent(archRoot.transform, false);

            Vector3 roomCenter = new Vector3(-1.0f, 0f, 2.30f);
            float roomW = 3.10f;
            float roomD = 1.90f;
            float roomH = 3.00f;
            float wallThick = 0.32f;

            // Room Floor Paving (inside room under vestibule tile)
            CreateBlock(roomRoot.transform, new Vector3(roomCenter.x, -0.04f, roomCenter.z), new Vector3(roomW - 0.2f, 0.08f, roomD - 0.2f), wallPeachMat, "Room_Floor_Base");

            // Back Wall (at Z = 2.30 + 0.95 - 0.16 = 3.09f)
            CreateBlock(roomRoot.transform, new Vector3(roomCenter.x, roomH * 0.5f, roomCenter.z + (roomD * 0.5f) - (wallThick * 0.5f)), new Vector3(roomW, roomH, wallThick), wallPeachMat, "Room_BackWall");

            // Right Wall (at X = -1.0 + 1.55 - 0.16 = 0.39f)
            CreateBlock(roomRoot.transform, new Vector3(roomCenter.x + (roomW * 0.5f) - (wallThick * 0.5f), roomH * 0.5f, roomCenter.z), new Vector3(wallThick, roomH, roomD), wallPeachMat, "Room_RightWall");

            // Left Wall with Arched Portal Opening leading onto the Exit Terrace (at X = -2.39f)
            float leftWallX = roomCenter.x - (roomW * 0.5f) + (wallThick * 0.5f);
            float lintelH = roomH - 1.85f;
            // Front pier of left wall (from Z = 1.35 to 1.55)
            CreateBlock(roomRoot.transform, new Vector3(leftWallX, roomH * 0.5f, 1.45f), new Vector3(wallThick, roomH, 0.20f), wallPeachMat, "Room_LeftWall_Pier_Front");
            // Back pier of left wall (from Z = 2.45 to 3.25)
            CreateBlock(roomRoot.transform, new Vector3(leftWallX, roomH * 0.5f, 2.85f), new Vector3(wallThick, roomH, 0.80f), wallPeachMat, "Room_LeftWall_Pier_Back");
            // Lintel over the side arched opening (spanning Z = 1.55 to 2.45)
            CreateBlock(roomRoot.transform, new Vector3(leftWallX, 1.85f + (lintelH * 0.5f), 2.00f), new Vector3(wallThick, lintelH, 0.90f), wallPeachMat, "Room_LeftWall_Lintel");

            // Front Wall Facade with Arched Doorway Opening (at Z = 1.36f)
            float frontZ = roomCenter.z - (roomD * 0.5f) + (wallThick * 0.5f);
            float pierW = ((roomW - wallThick * 2f) - 1.00f) * 0.5f;
            float pierLeftX = roomCenter.x - 0.50f - (pierW * 0.5f);
            float pierRightX = roomCenter.x + 0.50f + (pierW * 0.5f);
            CreateBlock(roomRoot.transform, new Vector3(pierLeftX, roomH * 0.5f, frontZ), new Vector3(pierW, roomH, wallThick), wallPeachMat, "Room_FrontWall_L");
            CreateBlock(roomRoot.transform, new Vector3(pierRightX, roomH * 0.5f, frontZ), new Vector3(pierW, roomH, wallThick), wallPeachMat, "Room_FrontWall_R");

            // Front Lintel above doorway (from Y = 1.85f to Y = 3.00f)
            CreateBlock(roomRoot.transform, new Vector3(roomCenter.x, 1.85f + (lintelH * 0.5f), frontZ), new Vector3(1.10f, lintelH, wallThick), wallPeachMat, "Room_Lintel");

            // Interior Warm Sanctuary Glow Light pouring through doorway and side arch
            GameObject roomLightObj = new GameObject("Room_Interior_Light");
            roomLightObj.transform.SetParent(roomRoot.transform, false);
            roomLightObj.transform.position = new Vector3(roomCenter.x, 1.30f, roomCenter.z);
            var rLight = roomLightObj.AddComponent<Light>();
            rLight.type = LightType.Point;
            rLight.color = new Color(1.0f, 0.94f, 0.78f);
            rLight.intensity = 2.8f;
            rLight.range = 5.0f;
            rLight.shadows = LightShadows.None;

            // Room Ceiling & Cornice Trim (solidly connects to walls, no gap!)
            CreateBlock(roomRoot.transform, new Vector3(roomCenter.x, roomH + 0.08f, roomCenter.z), new Vector3(roomW + 0.15f, 0.16f, roomD + 0.15f), pathIvoryMat, "Room_Roof_Slab");
            CreateBlock(roomRoot.transform, new Vector3(roomCenter.x, roomH + 0.22f, roomCenter.z), new Vector3(roomW + 0.30f, 0.12f, roomD + 0.30f), trimMintMat, "Room_Cornice");

            // Grand Golden Pavilion Dome on Room Roof (grounded on roof cornice)
            float roofDomeBaseY = roomH + 0.28f;
            CreateBlock(roomRoot.transform, new Vector3(roomCenter.x, roofDomeBaseY + 0.10f, roomCenter.z), new Vector3(1.70f, 0.20f, 1.50f), wallPeachMat, "Roof_Dome_Plinth");
            CreateBlock(roomRoot.transform, new Vector3(roomCenter.x, roofDomeBaseY + 0.24f, roomCenter.z), new Vector3(1.80f, 0.08f, 1.60f), trimMintMat, "Roof_Dome_Rim");
            CreateSphere(roomRoot.transform, new Vector3(roomCenter.x, roofDomeBaseY + 0.75f, roomCenter.z), new Vector3(1.50f, 0.95f, 1.30f), goldMat, "Roof_Golden_Dome");
            CreateCylinder(roomRoot.transform, new Vector3(roomCenter.x, roofDomeBaseY + 1.50f, roomCenter.z), new Vector3(0.10f, 0.60f, 0.10f), goldMat, "Roof_Dome_Spire");
            CreateSphere(roomRoot.transform, new Vector3(roomCenter.x, roofDomeBaseY + 1.85f, roomCenter.z), new Vector3(0.20f, 0.20f, 0.20f), goldMat, "Roof_Dome_Finial");

            // ==========================================
            // 1B. EXIT TERRACE PLATFORM (Where drawn by user)
            // ==========================================
            GameObject terraceRoot = new GameObject("Exit_Terrace_P1");
            terraceRoot.transform.SetParent(archRoot.transform, false);

            // Floor base slab supporting the terrace tiles
            CreateBlock(terraceRoot.transform, new Vector3(-2.60f, -0.05f, 2.50f), new Vector3(2.10f, 0.10f, 2.20f), wallPeachMat, "Terrace_Base_Slab");

            // Perimeter mint balustrade rims on outer edges
            CreateBlock(terraceRoot.transform, new Vector3(-3.55f, 0.08f, 2.50f), new Vector3(0.12f, 0.16f, 2.10f), trimMintMat, "Terrace_Parapet_West");
            CreateBlock(terraceRoot.transform, new Vector3(-2.60f, 0.08f, 3.55f), new Vector3(2.00f, 0.16f, 0.12f), trimMintMat, "Terrace_Parapet_North");

            // Decorative Corner Finials on outer terrace corners
            CreateBlock(terraceRoot.transform, new Vector3(-3.55f, 0.15f, 1.45f), new Vector3(0.24f, 0.30f, 0.24f), trimMintMat, "Terrace_Post_SW");
            CreateSphere(terraceRoot.transform, new Vector3(-3.55f, 0.38f, 1.45f), new Vector3(0.16f, 0.16f, 0.16f), goldMat, "Terrace_Post_SW_Ball");

            CreateBlock(terraceRoot.transform, new Vector3(-3.55f, 0.15f, 3.55f), new Vector3(0.24f, 0.30f, 0.24f), trimMintMat, "Terrace_Post_NW");
            CreateSphere(terraceRoot.transform, new Vector3(-3.55f, 0.38f, 3.55f), new Vector3(0.16f, 0.16f, 0.16f), goldMat, "Terrace_Post_NW_Ball");

            CreateBlock(terraceRoot.transform, new Vector3(-1.60f, 0.15f, 3.55f), new Vector3(0.24f, 0.30f, 0.24f), trimMintMat, "Terrace_Post_NE");
            CreateSphere(terraceRoot.transform, new Vector3(-1.60f, 0.38f, 3.55f), new Vector3(0.16f, 0.16f, 0.16f), goldMat, "Terrace_Post_NE_Ball");

            // ==========================================
            // 2. CORNER PILASTER COLUMNS & ROOF FINIALS
            // ==========================================
            float cornerX_L = roomCenter.x - (roomW * 0.5f); // -2.55f (exact wall outer edge)
            float cornerX_R = roomCenter.x + (roomW * 0.5f); // +0.55f (exact wall outer edge)
            float cornerZ_F = roomCenter.z - (roomD * 0.5f); // +1.35f (exact front outer edge)
            float cornerZ_B = roomCenter.z + (roomD * 0.5f); // +3.25f (exact back outer edge)

            Vector3[] cornerPositions = new Vector3[] {
                new Vector3(cornerX_L, 0f, cornerZ_F),
                new Vector3(cornerX_R, 0f, cornerZ_F),
                new Vector3(cornerX_L, 0f, cornerZ_B),
                new Vector3(cornerX_R, 0f, cornerZ_B)
            };
            string[] cornerNames = new string[] { "Corner_FrontL", "Corner_FrontR", "Corner_BackL", "Corner_BackR" };

            float roofCorniceTopY = roomH + 0.28f; // 3.28f (top of Room_Cornice)

            // Grounded Front Facade Pilasters (only on the two front corners facing courtyard)
            CreateCornerPilaster(archRoot.transform, cornerPositions[0], roomH, 0.20f, wallPeachMat, trimMintMat, cornerNames[0] + "_Pilaster");
            CreateCornerPilaster(archRoot.transform, cornerPositions[1], roomH, 0.20f, wallPeachMat, trimMintMat, cornerNames[1] + "_Pilaster");

            // Symmetrical Golden Corner Finials atop Roof Cornice (all 4 corners at Y = 3.28f)
            for (int i = 0; i < cornerPositions.Length; i++) {
                CreateRoofCornerFinial(archRoot.transform, new Vector3(cornerPositions[i].x, roofCorniceTopY, cornerPositions[i].z), 0.18f, goldMat, trimMintMat, cornerNames[i] + "_Finial");
            }

            // ==========================================
            // 3. FRONT ARCHED DOORWAY FRAME & COLUMNS
            // ==========================================
            Vector3 portalPos = new Vector3(-1.0f, 0.0f, 1.45f);

            // Column Plinth Bases (Grounded Mint)
            CreateBlock(archRoot.transform, portalPos + new Vector3(-0.50f, 0.075f, -0.05f), new Vector3(0.26f, 0.15f, 0.26f), trimMintMat, "Portal_Col_Base_L");
            CreateBlock(archRoot.transform, portalPos + new Vector3(0.50f, 0.075f, -0.05f), new Vector3(0.26f, 0.15f, 0.26f), trimMintMat, "Portal_Col_Base_R");

            // Columns (Warm Ivory) grounded from Y = 0.15f to 2.05f (height = 1.90f)
            CreateCylinder(archRoot.transform, portalPos + new Vector3(-0.50f, 1.10f, -0.05f), new Vector3(0.20f, 1.90f, 0.20f), pathIvoryMat, "Portal_Col_L");
            CreateCylinder(archRoot.transform, portalPos + new Vector3(0.50f, 1.10f, -0.05f), new Vector3(0.20f, 1.90f, 0.20f), pathIvoryMat, "Portal_Col_R");

            // Column Capitals (Burnished Gold)
            CreateSphere(archRoot.transform, portalPos + new Vector3(-0.50f, 2.12f, -0.05f), new Vector3(0.26f, 0.18f, 0.26f), goldMat, "Portal_Col_Cap_L");
            CreateSphere(archRoot.transform, portalPos + new Vector3(0.50f, 2.12f, -0.05f), new Vector3(0.26f, 0.18f, 0.26f), goldMat, "Portal_Col_Cap_R");

            // Mint Arch Entablature
            CreateBlock(archRoot.transform, portalPos + new Vector3(0, 2.27f, -0.05f), new Vector3(1.36f, 0.18f, 0.28f), trimMintMat, "Portal_Entablature");

            // Golden Semicircular Dome Pediment
            CreateSphere(archRoot.transform, portalPos + new Vector3(0, 2.50f, -0.05f), new Vector3(0.80f, 0.40f, 0.26f), goldMat, "Portal_Dome_Pediment");

            // Golden Needle Spire
            CreateCylinder(archRoot.transform, portalPos + new Vector3(0, 2.85f, -0.05f), new Vector3(0.08f, 0.35f, 0.08f), goldMat, "Portal_Spire");
            CreateSphere(archRoot.transform, portalPos + new Vector3(0, 3.08f, -0.05f), new Vector3(0.14f, 0.14f, 0.14f), goldMat, "Portal_Spire_Finial");

            // Exit Door Logic & Visuals (links the front courtyard tile to the exit terrace platform)
            CreateExitDoor(archRoot.transform, exitNode, entranceNode, additionalLockedNodes);
        }

        private void BuildPlatform2Architecture(Transform parent, Vector3 offset) {
            GameObject archRoot = new GameObject("Architecture_P2");
            archRoot.transform.SetParent(parent, false);

            // 1. Ascending Stepped Stairs to Elevated Sanctuary Terrace
            Vector3 stairsPos = new Vector3(6.0f, 0.0f, 1.75f) + offset;
            CreateBlock(archRoot.transform, stairsPos + new Vector3(0, 0.12f, -0.20f), new Vector3(1.80f, 0.12f, 0.38f), pathIvoryMat, "Sanctuary_Step_1");
            CreateBlock(archRoot.transform, stairsPos + new Vector3(0, 0.24f, 0.18f), new Vector3(1.80f, 0.12f, 0.38f), pathIvoryMat, "Sanctuary_Step_2");
            CreateBlock(archRoot.transform, stairsPos + new Vector3(0, 0.36f, 0.56f), new Vector3(1.80f, 0.12f, 0.38f), pathIvoryMat, "Sanctuary_Step_3");

            // 2. Elevated Sanctuary Terrace
            Vector3 terracePos = new Vector3(6.0f, 0.50f, 2.65f) + offset;
            CreateBlock(archRoot.transform, terracePos, new Vector3(4.0f, 0.30f, 1.80f), pathIvoryMat, "Sanctuary_Terrace");

            // 3. Open-Air Domed Colonnade Pavilion
            Vector3 pavCenter = terracePos + new Vector3(0, 0.15f, 0.15f);

            // 4 Slender Pavilion Pillars (Grounded flush on terrace, no air gaps!)
            float colHalfW = 1.35f;
            float colHalfD = 0.55f;
            Vector3[] colOffsets = new Vector3[] {
                new Vector3(-colHalfW, 0, -colHalfD),
                new Vector3(-colHalfW, 0, colHalfD),
                new Vector3(colHalfW, 0, -colHalfD),
                new Vector3(colHalfW, 0, colHalfD),
            };

            float pillarHeight = 1.70f;
            float pillarCenterY = pillarHeight * 0.5f;
            for (int i = 0; i < colOffsets.Length; i++) {
                Vector3 colPos = pavCenter + colOffsets[i];
                CreateCylinder(archRoot.transform, colPos + new Vector3(0, pillarCenterY, 0), new Vector3(0.24f, pillarHeight, 0.24f), pathIvoryMat, $"Pavilion_Col_{i}");
                CreateSphere(archRoot.transform, colPos + new Vector3(0, pillarHeight + 0.10f, 0), new Vector3(0.30f, 0.22f, 0.30f), goldMat, $"Pavilion_Col_Cap_{i}");
            }

            // Mint Entablature Cornice (bottom touches the pillar caps)
            float corniceBottomY = pillarHeight + 0.20f;
            float corniceHeight = 0.22f;
            float corniceCenterY = corniceBottomY + (corniceHeight * 0.5f);
            CreateBlock(archRoot.transform, pavCenter + new Vector3(0, corniceCenterY, 0), new Vector3(3.20f, corniceHeight, 1.50f), trimMintMat, "Pavilion_Cornice");

            // Grand Golden Pavilion Dome (rests directly on top of the cornice)
            float domeBottomY = corniceCenterY + (corniceHeight * 0.5f);
            float domeHeight = 1.25f;
            float domeCenterY = domeBottomY + (domeHeight * 0.5f);
            CreateSphere(archRoot.transform, pavCenter + new Vector3(0, domeCenterY, 0), new Vector3(2.20f, domeHeight, 1.50f), goldMat, "Pavilion_Dome");

            // Golden Needle Spire (rests directly on top of the dome)
            float domeTopY = domeBottomY + domeHeight;
            float spireH = 0.65f;
            float spireCenterY = domeTopY + (spireH * 0.5f);
            CreateCylinder(archRoot.transform, pavCenter + new Vector3(0, spireCenterY, 0), new Vector3(0.10f, spireH, 0.10f), goldMat, "Pavilion_Spire");
            CreateSphere(archRoot.transform, pavCenter + new Vector3(0, domeTopY + spireH + 0.10f, 0), new Vector3(0.22f, 0.22f, 0.22f), goldMat, "Pavilion_Spire_Finial");

            // Flanking Ornamental Obelisks on Platform 2 (squarely aligned with supporting foundation piers)
            CreateObelisk(archRoot.transform, new Vector3(4.60f, 0f, 1.25f) + offset, "Obelisk_P2_L");
            CreateObelisk(archRoot.transform, new Vector3(7.40f, 0f, -0.25f) + offset, "Obelisk_P2_R");
        }

        private void CreateCornerPilaster(Transform parent, Vector3 basePos, float totalHeight, float radius, Material bodyMat, Material trimMat, string name) {
            GameObject col = new GameObject(name);
            col.transform.SetParent(parent, false);
            col.transform.position = basePos;

            // 1. Plinth Base (Mint square plinth, rests squarely on foundation)
            float baseH = 0.18f;
            CreateBlock(col.transform, new Vector3(0, baseH * 0.5f, 0), new Vector3(radius * 2.4f, baseH, radius * 2.4f), trimMat, "Base");

            // 2. Column Shaft (Peach cylindrical pillar)
            float capH = 0.16f;
            float shaftH = totalHeight - baseH - capH;
            float shaftCenterY = baseH + (shaftH * 0.5f);
            CreateCylinder(col.transform, new Vector3(0, shaftCenterY, 0), new Vector3(radius * 2f, shaftH, radius * 2f), bodyMat, "Shaft");

            // 3. Capital Collar (Mint trim collar meeting underside of roof slab flush)
            float capCenterY = totalHeight - (capH * 0.5f);
            CreateCylinder(col.transform, new Vector3(0, capCenterY, 0), new Vector3(radius * 2.3f, capH, radius * 2.3f), trimMat, "Capital");
        }

        private void CreateRoofCornerFinial(Transform parent, Vector3 roofPos, float radius, Material goldMat, Material trimMat, string name) {
            GameObject finial = new GameObject(name);
            finial.transform.SetParent(parent, false);
            finial.transform.position = roofPos;

            // 1. Pedestal Plinth on roof (Mint disk)
            float plinthH = 0.08f;
            CreateCylinder(finial.transform, new Vector3(0, plinthH * 0.5f, 0), new Vector3(radius * 2.2f, plinthH, radius * 2.2f), trimMat, "Plinth");

            // 2. Golden Spherical Dome
            float domeD = radius * 2.0f;
            float domeH = radius * 1.7f;
            float domeCenterY = plinthH + (domeH * 0.5f);
            CreateSphere(finial.transform, new Vector3(0, domeCenterY, 0), new Vector3(domeD, domeH, domeD), goldMat, "Dome");

            // 3. Golden Needle Spire
            float spireH = 0.52f;
            float spireCenterY = plinthH + domeH + (spireH * 0.5f);
            CreateCylinder(finial.transform, new Vector3(0, spireCenterY, 0), new Vector3(0.07f, spireH, 0.07f), goldMat, "Spire");

            // 4. Golden Ball Finial Tip
            float tipCenterY = plinthH + domeH + spireH + 0.06f;
            CreateSphere(finial.transform, new Vector3(0, tipCenterY, 0), new Vector3(0.14f, 0.14f, 0.14f), goldMat, "Finial_Tip");
        }

        private void CreateObelisk(Transform parent, Vector3 worldPos, string name) {
            GameObject obelisk = new GameObject(name);
            obelisk.transform.SetParent(parent, false);
            obelisk.transform.position = worldPos;

            // Stepped base (rests squarely and solidly on foundation pier)
            CreateBlock(obelisk.transform, new Vector3(0, 0.10f, 0), new Vector3(0.44f, 0.20f, 0.44f), trimMintMat, "Base");
            // Slender shaft
            CreateBlock(obelisk.transform, new Vector3(0, 0.675f, 0), new Vector3(0.32f, 0.95f, 0.32f), wallPeachMat, "Shaft");
            // Golden pyramidal cap / finial
            CreateSphere(obelisk.transform, new Vector3(0, 1.25f, 0), new Vector3(0.28f, 0.28f, 0.28f), goldMat, "Finial");
        }

        private void CreateKeyItemAndAltar(Transform parent, PathNode node) {
            GameObject keyRoot = new GameObject("Key_Pedestal_And_Item");
            keyRoot.transform.SetParent(parent, false);
            keyRoot.transform.position = node.transform.position;

            // Pedestal base cylinders removed per user request:
            // Allows character to step cleanly onto the tile without mesh mixing awkwardly with the pedestal!

            // Floating Radiant Star Artifact (Rotating geometric polyhedron)
            GameObject keyVisual = new GameObject("Key_Star_Artifact");
            keyVisual.transform.SetParent(keyRoot.transform, false);
            keyVisual.transform.localPosition = new Vector3(0f, 0.85f, 0f);

            // Central golden octahedron core (nested tilted cubes)
            CreateBlock(keyVisual.transform, Vector3.zero, new Vector3(0.32f, 0.32f, 0.32f), goldMat, "Core_1", Quaternion.Euler(45f, 45f, 0f));
            CreateBlock(keyVisual.transform, Vector3.zero, new Vector3(0.32f, 0.32f, 0.32f), goldMat, "Core_2", Quaternion.Euler(0f, 45f, 45f));

            // Golden halo ring
            CreateCylinder(keyVisual.transform, new Vector3(0, 0.20f, 0), new Vector3(0.24f, 0.06f, 0.24f), goldMat, "Halo_Ring");

            // Radiant golden aura point light
            GameObject keyLightObj = new GameObject("KeyAura_Light");
            keyLightObj.transform.SetParent(keyVisual.transform, false);
            keyLightObj.transform.localPosition = Vector3.zero;
            var kLight = keyLightObj.AddComponent<Light>();
            kLight.type = LightType.Point;
            kLight.color = new Color(1.0f, 0.88f, 0.35f);
            kLight.intensity = 3.5f;
            kLight.range = 5.0f;
            kLight.shadows = LightShadows.None;

            var keyComp = keyRoot.AddComponent<KeyItem>();
            keyComp.Initialize(node);
            SetPrivateField(keyComp, "visualModel", keyVisual);
        }

        private void CreateExitDoor(Transform parent, PathNode exitPlatformNode, PathNode entranceNode, List<PathNode> additionalLockedNodes = null) {
            GameObject doorRoot = new GameObject("ExitDoor");
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.position = new Vector3(-1.0f, 0f, 1.45f);

            // Door Hinge Pivot on the LEFT side of the doorway frame (x = -0.42f)
            GameObject doorHinge = new GameObject("Door_Left_Hinge");
            doorHinge.transform.SetParent(doorRoot.transform, false);
            doorHinge.transform.localPosition = new Vector3(-0.42f, 0f, 0f);

            // Closed Door Leaf (Serene Deep Indigo)
            // Sized 0.84f width. Left edge sits right at the hinge pivot!
            GameObject closedDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            closedDoor.name = "ClosedDoor";
            closedDoor.transform.SetParent(doorHinge.transform, false);
            closedDoor.transform.localPosition = new Vector3(0.42f, 0.95f, 0f);
            closedDoor.transform.localScale = new Vector3(0.84f, 1.76f, 0.08f);
            closedDoor.GetComponent<Renderer>().sharedMaterial = doorIndigoMat;
            var cdCol = closedDoor.GetComponent<Collider>();
            if (cdCol != null) DestroyImmediate(cdCol);

            // Golden door handle emblem near the right opening edge of the door leaf (front & back)
            CreateCylinder(closedDoor.transform, new Vector3(0.32f, 0.0f, -0.05f), new Vector3(0.08f, 0.04f, 0.08f), goldMat, "DoorHandle_Front", Quaternion.Euler(90f, 0f, 0f));
            CreateCylinder(closedDoor.transform, new Vector3(0.32f, 0.0f, 0.05f), new Vector3(0.08f, 0.04f, 0.08f), goldMat, "DoorHandle_Back", Quaternion.Euler(90f, 0f, 0f));

            // Interior warm portal glow quad behind door opening
            GameObject openPortal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            openPortal.name = "OpenPortal_Glow";
            openPortal.transform.SetParent(doorRoot.transform, false);
            openPortal.transform.localPosition = new Vector3(0.0f, 0.95f, 0.05f);
            openPortal.transform.localScale = new Vector3(0.80f, 1.70f, 1.0f);
            openPortal.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            openPortal.GetComponent<Renderer>().sharedMaterial = doorSunlightMat;
            var pCol = openPortal.GetComponent<Collider>();
            if (pCol != null) DestroyImmediate(pCol);
            openPortal.SetActive(false);

            // Warm interior spill point light
            GameObject doorLightObj = new GameObject("DoorLight");
            doorLightObj.transform.SetParent(doorRoot.transform, false);
            doorLightObj.transform.localPosition = new Vector3(0.0f, 1.0f, 0.2f);
            var dLight = doorLightObj.AddComponent<Light>();
            dLight.type = LightType.Point;
            dLight.color = new Color(1.0f, 0.95f, 0.70f);
            dLight.intensity = 0f;
            dLight.range = 3.5f;
            dLight.shadows = LightShadows.None;

            var exitDoor = doorRoot.AddComponent<ExitDoor>();
            // closedDoorVisual is the LEFT HINGE so it swings open outward towards the courtyard!
            SetPrivateField(exitDoor, "closedDoorVisual", doorHinge);
            SetPrivateField(exitDoor, "openPortalLight", openPortal);
            SetPrivateField(exitDoor, "doorLight", dLight);
            SetPrivateField(exitDoor, "openAngleY", 90f);
            exitDoor.Initialize(exitPlatformNode, entranceNode, additionalLockedNodes);
        }

        private void CreatePlayerCharacter(Transform parent, PathNode startNode) {
            GameObject playerObj = new GameObject("PlayerCharacter");
            playerObj.transform.SetParent(parent, false);
            playerObj.transform.position = startNode.WalkPosition;

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(playerObj.transform, false);
            visualRoot.transform.localPosition = Vector3.zero;

            // Stylized Princess Ida Character (Pure Geometric Beauty)
            // 1. Pristine White Conical/Cylindrical Dress
            GameObject dress = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dress.name = "Ida_Dress";
            dress.transform.SetParent(visualRoot.transform, false);
            dress.transform.localPosition = new Vector3(0, 0.35f, 0);
            dress.transform.localScale = new Vector3(0.28f, 0.35f, 0.28f);
            dress.GetComponent<Renderer>().sharedMaterial = idaDressMat;
            var dCol = dress.GetComponent<Collider>();
            if (dCol != null) DestroyImmediate(dCol);

            // 2. Minimalist Head (Sphere)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Ida_Head";
            head.transform.SetParent(visualRoot.transform, false);
            head.transform.localPosition = new Vector3(0, 0.75f, 0);
            head.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            head.GetComponent<Renderer>().sharedMaterial = idaSkinMat;
            var hCol = head.GetComponent<Collider>();
            if (hCol != null) DestroyImmediate(hCol);

            // 3. Tall Conical White Hat
            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "Ida_Hat";
            hat.transform.SetParent(visualRoot.transform, false);
            hat.transform.localPosition = new Vector3(0, 0.98f, 0);
            hat.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            hat.GetComponent<Renderer>().sharedMaterial = idaDressMat;
            var hatCol = hat.GetComponent<Collider>();
            if (hatCol != null) DestroyImmediate(hatCol);

            // Hat Tip Sphere
            GameObject hatTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hatTip.name = "Ida_Hat_Tip";
            hatTip.transform.SetParent(visualRoot.transform, false);
            hatTip.transform.localPosition = new Vector3(0, 1.20f, 0);
            hatTip.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            hatTip.GetComponent<Renderer>().sharedMaterial = idaDressMat;
            var htCol = hatTip.GetComponent<Collider>();
            if (htCol != null) DestroyImmediate(htCol);

            // 4. Golden Circular Brooch Emblem
            GameObject brooch = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            brooch.name = "Ida_Brooch";
            brooch.transform.SetParent(visualRoot.transform, false);
            brooch.transform.localPosition = new Vector3(0, 0.62f, -0.13f);
            brooch.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            brooch.GetComponent<Renderer>().sharedMaterial = goldMat;
            var bCol = brooch.GetComponent<Collider>();
            if (bCol != null) DestroyImmediate(bCol);

            var playerComp = playerObj.AddComponent<PlayerController>();
            playerComp.SetCurrentNode(startNode);
            SetPrivateField(playerComp, "visualRoot", visualRoot.transform);
        }

        private void SetupCameraAndLighting(Transform parent, Vector3 illusionOffset) {
            Camera mainCam = Camera.main;
            if (mainCam == null) {
                var camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            mainCam.orthographic = true;
            mainCam.orthographicSize = 6.0f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            // Warm golden parchment cream background (#F5E8A2)
            mainCam.backgroundColor = skyColor;

            // URP Camera Post-Processing & Anti-Aliasing
            var camData = mainCam.GetUniversalAdditionalCameraData();
            if (camData != null) {
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                camData.antialiasingQuality = AntialiasingQuality.High;
            }

            // Crisp vector-like geometry (no murky fog!)
            RenderSettings.fog = false;

            // Soft pastel warm ambient fill
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.74f, 0.65f);

            var camController = mainCam.GetComponent<IsometricCameraController>();
            if (camController == null) {
                camController = mainCam.gameObject.AddComponent<IsometricCameraController>();
            }

            // Pivot target at visual center ~ (2.5, 0.5, 0.5)
            GameObject pivotObj = GameObject.Find("PuzzlePivot");
            if (pivotObj == null) {
                pivotObj = new GameObject("PuzzlePivot");
                pivotObj.transform.SetParent(parent, false);
                pivotObj.transform.position = new Vector3(2.5f, 0.5f, 0.5f);
            }
            SetPrivateField(camController, "currentYaw", illusionYaw);
            SetPrivateField(camController, "targetYaw", illusionYaw);
            SetPrivateField(camController, "currentSnapIndex", 0);
            camController.SetPivotTarget(pivotObj.transform, Vector3.zero);

            var alignMgr = mainCam.GetComponent<PerspectiveAlignmentManager>();
            if (alignMgr == null) {
                alignMgr = mainCam.gameObject.AddComponent<PerspectiveAlignmentManager>();
            }
            alignMgr.Configure(illusionYaw, 3.5f);

            // Main Directional Sunlight (Warm, sharp Monument Valley shadows)
            Light mainLight = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) {
                if (l.type == LightType.Directional) {
                    mainLight = l;
                    break;
                }
            }
            if (mainLight == null) {
                var lObj = new GameObject("Directional Light");
                mainLight = lObj.AddComponent<Light>();
                mainLight.type = LightType.Directional;
            }
            mainLight.transform.rotation = Quaternion.Euler(50f, -40f, 0f);
            mainLight.color = new Color(1.0f, 0.96f, 0.88f);
            mainLight.intensity = 1.35f;
            mainLight.shadows = LightShadows.Hard;
            mainLight.shadowStrength = 0.45f;

            // Sky / Fill Light (Soft mint-sky fill for pastel architectural contrast)
            GameObject fillObj = GameObject.Find("Fill_Light");
            if (fillObj == null) {
                fillObj = new GameObject("Fill_Light");
                fillObj.transform.SetParent(parent, false);
            }
            var fillLight = fillObj.GetComponent<Light>();
            if (fillLight == null) fillLight = fillObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.transform.rotation = Quaternion.Euler(-30f, 140f, 0f);
            fillLight.color = new Color(0.70f, 0.85f, 0.90f);
            fillLight.intensity = 0.55f;
            fillLight.shadows = LightShadows.None;

            // Setup UI Managers
            GameObject uiObj = GameObject.Find("GameUI");
            if (uiObj == null) {
                uiObj = new GameObject("GameUI");
                uiObj.transform.SetParent(parent, false);
            }
            if (uiObj.GetComponent<GameUI>() == null) uiObj.AddComponent<GameUI>();
            if (uiObj.GetComponent<LevelManager>() == null) uiObj.AddComponent<LevelManager>();
        }

        private PathNode CreateTile(Transform parent, Vector3 position, string name) {
            GameObject tileObj = new GameObject(name);
            tileObj.transform.SetParent(parent, false);
            tileObj.transform.position = position;

            // Visual ivory walkway slab
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Tile_Slab";
            slab.transform.SetParent(tileObj.transform, false);
            slab.transform.localPosition = new Vector3(0f, -0.01f, 0f);
            slab.transform.localScale = new Vector3(0.96f, 0.20f, 0.96f);

            var sCol = slab.GetComponent<Collider>();
            if (sCol != null) DestroyImmediate(sCol);

            var rend = slab.GetComponent<Renderer>();
            if (rend != null && pathIvoryMat != null) rend.sharedMaterial = pathIvoryMat;

            // Subtle mint edge border
            CreateBlock(tileObj.transform, new Vector3(0f, -0.10f, 0f), new Vector3(0.98f, 0.06f, 0.98f), trimMintMat, "Tile_Border");

            var box = tileObj.AddComponent<BoxCollider>();
            box.size = new Vector3(1.0f, 0.25f, 1.0f);
            box.center = Vector3.zero;

            var node = tileObj.AddComponent<PathNode>();
            SetPrivateField(node, "standingOffset", new Vector3(0f, 0.10f, 0f));
            SetPrivateField(node, "tileRenderer", rend);
            SetPrivateField(node, "normalColor", Color.white);

            return node;
        }

        private PathNode CreateCompleteLevelTile(Transform parent, Vector3 position, string name) {
            PathNode node = CreateTile(parent, position, name);

            // Sacred Monument Valley Goal Dais:
            // 1. Outer Golden Medallion Ring
            CreateCylinder(node.transform, new Vector3(0f, 0.095f, 0f), new Vector3(0.72f, 0.02f, 0.72f), goldMat, "Goal_OuterRing");
            // 2. Inner Mint Rosette Ring
            CreateCylinder(node.transform, new Vector3(0f, 0.105f, 0f), new Vector3(0.50f, 0.02f, 0.50f), trimMintMat, "Goal_InnerRing");
            // 3. Central Burnished Gold Medallion Dais
            CreateCylinder(node.transform, new Vector3(0f, 0.115f, 0f), new Vector3(0.32f, 0.02f, 0.32f), goldMat, "Goal_CenterDais");

            // 4. Geometric 'X' Cross Emblem (honoring the user's hand-drawn 'X')
            CreateBlock(node.transform, new Vector3(0f, 0.125f, 0f), new Vector3(0.38f, 0.02f, 0.07f), trimMintMat, "Goal_Cross_1", Quaternion.Euler(0f, 45f, 0f));
            CreateBlock(node.transform, new Vector3(0f, 0.125f, 0f), new Vector3(0.38f, 0.02f, 0.07f), trimMintMat, "Goal_Cross_2", Quaternion.Euler(0f, -45f, 0f));

            // 5. Golden Goal Aura Point Light
            GameObject goalLight = new GameObject("Goal_Aura_Light");
            goalLight.transform.SetParent(node.transform, false);
            goalLight.transform.localPosition = new Vector3(0f, 0.60f, 0f);
            var gLight = goalLight.AddComponent<Light>();
            gLight.type = LightType.Point;
            gLight.color = new Color(1.0f, 0.88f, 0.40f);
            gLight.intensity = 2.4f;
            gLight.range = 3.5f;
            gLight.shadows = LightShadows.None;

            SetPrivateField(node, "standingOffset", new Vector3(0f, 0.13f, 0f));
            return node;
        }

        private GameObject CreateBlock(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name = "Block", Quaternion? localRot = null) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (localRot.HasValue) {
                go.transform.localRotation = localRot.Value;
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            return go;
        }

        private GameObject CreateCylinder(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name = "Cylinder", Quaternion? localRot = null) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            // Cylinder primitive height is 2.0 along Y, so half the localScale.y to match world units
            go.transform.localScale = new Vector3(localScale.x, localScale.y * 0.5f, localScale.z);
            if (localRot.HasValue) {
                go.transform.localRotation = localRot.Value;
            }
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            return go;
        }

        private GameObject CreateSphere(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name = "Sphere") {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = mat;

            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            return go;
        }

        private static void ConnectGridNeighbors(Dictionary<Vector2Int, PathNode> nodes) {
            Vector2Int[] dirs = new Vector2Int[] {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                Vector2Int.down
            };

            foreach (var kvp in nodes) {
                Vector2Int pos = kvp.Key;
                PathNode current = kvp.Value;

                foreach (var d in dirs) {
                    Vector2Int neighborPos = pos + d;
                    if (nodes.TryGetValue(neighborPos, out PathNode neighbor)) {
                        current.AddNeighbor(neighbor);
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
