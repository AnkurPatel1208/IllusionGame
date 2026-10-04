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
    /// inspired by the iconic aesthetic of Monument Valley:
    /// - 100% pure Unity 3D primitive geometry (Cubes, Cylinders, Spheres, Quads)
    /// - Pristine pastel color harmony (Terracotta-Peach monuments, Warm Ivory paths, Mint accents, Burnished Gold domes)
    /// - Tranquil reflection pool with floating geometric lily pads and lotus buds
    /// - Slender minaret towers with golden domes, needle spires, and impossible Escher stairways
    /// - Princess Ida stylized player character (white gown, conical hat, golden brooch)
    /// - Sacred geometric star artifact on Platform 2 and majestic arched exit portal on Platform 1
    /// - Optical illusion perspective bridge (45° Yaw, 30° Pitch, depthOffset 4.0)
    /// </summary>
    public class Level1Builder : MonoBehaviour {
        [Header("Monument Valley Palette")]
        [SerializeField] private Color monumentPeachColor = new Color(0.93f, 0.62f, 0.55f);
        [SerializeField] private Color pathIvoryColor = new Color(0.97f, 0.95f, 0.91f);
        [SerializeField] private Color trimMintColor = new Color(0.40f, 0.74f, 0.70f);
        [SerializeField] private Color accentGoldColor = new Color(0.96f, 0.78f, 0.32f);
        [SerializeField] private Color foundationSandColor = new Color(0.85f, 0.52f, 0.48f);
        [SerializeField] private Color waterPoolColor = new Color(0.20f, 0.40f, 0.48f);
        [SerializeField] private Color skyColor = new Color(0.24f, 0.42f, 0.50f);

        [Header("Illusion Geometry Config")]
        [SerializeField] private float illusionYaw = 45f;
        [SerializeField] private float illusionPitch = 30f;
        [SerializeField] private float depthOffset = 4.0f;

        [Header("Water Basin Height")]
        [SerializeField] private float waterLevelY = -4.50f;

        [Header("Build Config")]
        [Tooltip("If false, the level is baked directly as scene objects in the editor")]
        [SerializeField] private bool buildOnStart = false;

        // Procedural Monument Valley Materials
        private Material wallPeachMat;
        private Material pathIvoryMat;
        private Material trimMintMat;
        private Material goldMat;
        private Material foundationSandMat;
        private Material waterMat;
        private Material waterPadMat;
        private Material doorIndigoMat;
        private Material doorSunlightMat;
        private Material idaDressMat;
        private Material idaSkinMat;
        private Material treeTrunkMat;
        private Material treeFoliageMat;

        public bool BuildOnStart {
            get => buildOnStart;
            set => buildOnStart = value;
        }

        private void Start() {
            if (buildOnStart) {
                var existing = GameObject.Find("Level1_Environment");
                if (existing == null) {
                    BuildLevel();
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Assets")]
        public void AutoAssignDungeonAssets() {
            // Maintained for editor menu compatibility; all geometry is now 100% procedural Unity primitives!
            EditorUtility.SetDirty(this);
        }
#endif

        [ContextMenu("Build Level 1")]
        public void BuildLevel() {
            // Safely clear existing level root if present
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
            Quaternion illusionRot = Quaternion.Euler(illusionPitch, illusionYaw, 0f);
            Vector3 camForward = illusionRot * Vector3.forward;
            Vector3 illusionOffset = camForward * depthOffset;

            // 2. Build Serene Reflection Pool Basin (Water plane, floating geometric lily pads)
            BuildWaterBasin(rootT);

            // 3. Build Surrounding Monument Valley Architecture (Minaret Spire Towers, Impossible Stairs, Geometric Trees)
            BuildSurroundingMonuments(rootT);

            // 4. Build Platform 1 (Starting Citadel & Grand Exit Portal)
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

            // Cantilever geometric brackets under Bridge Arm 1
            BuildCantileverBracketsP1(p1Root.transform);

            // Stepped descending staircase on left flank
            BuildPlatform1Staircase(p1Root.transform);

            // Monolithic tiered foundation citadel under Platform 1
            BuildPlatform1Foundation(p1Root.transform);

            // Platform 1 Architecture: Grand Arched Gateway Portal, Minarets, Planters
            BuildPlatform1Architecture(p1Root.transform, p1Nodes[new Vector2Int(-1, 1)]);

            // 5. Build Platform 2 (Right Sacred Key Sanctuary)
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

            // 6. Perspective Bridge Edge (connecting b1_Tip and b2_Start)
            GameObject edgeObj = new GameObject("PerspectiveBridge_Edge");
            edgeObj.transform.SetParent(rootT, false);
            var persEdge = edgeObj.AddComponent<PerspectiveEdge>();
            persEdge.Initialize(b1_Tip, b2_Start, illusionYaw, 3.5f);

            // 7. Sacred Geometric Key Item on Platform 2
            PathNode keyNode = p2Nodes[new Vector2Int(6, 1)];
            CreateKeyItemAndAltar(p2Root.transform, keyNode);

            // 8. Player Character (Princess Ida Silhouette)
            PathNode startNode = p1Nodes[new Vector2Int(-1, 0)];
            CreatePlayerCharacter(rootT, startNode);

            // 9. Setup Camera, Monument Valley Lighting, and UI Managers
            SetupCameraAndLighting(rootT, illusionOffset);

            Debug.Log("<color=green>[Level1Builder]</color> Monument Valley environment successfully built purely with Unity 3D primitives!");
        }

        private void CreateMaterials() {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material MakeMat(Color c, float smoothness = 0.15f, float metallic = 0f, string name = "Mat") {
                var m = new Material(litShader) { color = c, name = name };
                m.SetFloat("_Smoothness", smoothness);
                m.SetFloat("_Metallic", metallic);
                return m;
            }

            wallPeachMat = MakeMat(monumentPeachColor, 0.18f, 0f, "M_MV_PeachWall");
            pathIvoryMat = MakeMat(pathIvoryColor, 0.12f, 0f, "M_MV_IvoryPath");
            trimMintMat = MakeMat(trimMintColor, 0.20f, 0f, "M_MV_MintTrim");
            foundationSandMat = MakeMat(foundationSandColor, 0.15f, 0f, "M_MV_SandFoundation");
            waterMat = MakeMat(waterPoolColor, 0.82f, 0.10f, "M_MV_TranquilWater");
            waterPadMat = MakeMat(new Color(0.32f, 0.65f, 0.60f), 0.25f, 0f, "M_MV_WaterPad");

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

        private void BuildWaterBasin(Transform parent) {
            GameObject waterRoot = new GameObject("Monument_Water_Basin");
            waterRoot.transform.SetParent(parent, false);

            float waterY = waterLevelY;

            // Expansive serene water reflection plane (60 x 60 units)
            CreateBlock(waterRoot.transform, new Vector3(2.5f, waterY - 0.20f, 2.5f), new Vector3(60f, 0.40f, 60f), waterMat, "Water_Surface");

            // Submerged stepped perimeter frame
            CreateBlock(waterRoot.transform, new Vector3(2.5f, waterY - 0.50f, -14.0f), new Vector3(44f, 0.60f, 2.0f), trimMintMat, "Water_Frame_South");
            CreateBlock(waterRoot.transform, new Vector3(2.5f, waterY - 0.50f, 20.0f), new Vector3(44f, 0.60f, 2.0f), trimMintMat, "Water_Frame_North");
            CreateBlock(waterRoot.transform, new Vector3(-18.0f, waterY - 0.50f, 3.0f), new Vector3(2.0f, 0.60f, 36f), trimMintMat, "Water_Frame_West");
            CreateBlock(waterRoot.transform, new Vector3(22.0f, waterY - 0.50f, 3.0f), new Vector3(2.0f, 0.60f, 36f), trimMintMat, "Water_Frame_East");

            // Floating geometric lily pads with lotus buds around the citadel islands
            Vector3[] padPositions = new Vector3[] {
                new Vector3(-3.5f, waterY + 0.02f, -1.8f),
                new Vector3(1.2f, waterY + 0.02f, -2.4f),
                new Vector3(6.8f, waterY + 0.02f, -1.5f),
                new Vector3(9.5f, waterY + 0.02f, 2.2f),
                new Vector3(-4.8f, waterY + 0.02f, 3.2f),
                new Vector3(2.5f, waterY + 0.02f, 6.5f),
                new Vector3(7.8f, waterY + 0.02f, 6.2f),
                new Vector3(-1.2f, waterY + 0.02f, 8.5f),
            };

            for (int i = 0; i < padPositions.Length; i++) {
                float rad = 0.55f + ((i % 3) * 0.15f);
                CreateFloatingLilyPad(waterRoot.transform, padPositions[i], rad, $"LilyPad_{i}");
            }
        }

        private void BuildSurroundingMonuments(Transform parent) {
            GameObject surroundRoot = new GameObject("Surrounding_Monuments");
            surroundRoot.transform.SetParent(parent, false);

            float waterY = waterLevelY;

            // Distant Monument Valley Minaret Spire Towers rising serenely from the water
            CreateMinaretTower(surroundRoot.transform, new Vector3(-7.5f, waterY, 11.5f), 13.5f, 0.85f, wallPeachMat, goldMat, trimMintMat, "Minaret_Tower_L");
            CreateMinaretTower(surroundRoot.transform, new Vector3(-2.2f, waterY, 14.5f), 16.0f, 1.0f, wallPeachMat, goldMat, trimMintMat, "Minaret_Tower_CenterL");
            CreateMinaretTower(surroundRoot.transform, new Vector3(6.5f, waterY, 15.0f), 15.5f, 1.0f, wallPeachMat, goldMat, trimMintMat, "Minaret_Tower_CenterR");
            CreateMinaretTower(surroundRoot.transform, new Vector3(12.5f, waterY, 11.0f), 13.0f, 0.85f, wallPeachMat, goldMat, trimMintMat, "Minaret_Tower_R");

            // Flanking viewing pavilions in the distance
            CreateMinaretTower(surroundRoot.transform, new Vector3(-12.0f, waterY, 3.5f), 10.5f, 0.75f, foundationSandMat, goldMat, trimMintMat, "Minaret_Flank_L");
            CreateMinaretTower(surroundRoot.transform, new Vector3(16.5f, waterY, 4.0f), 10.5f, 0.75f, foundationSandMat, goldMat, trimMintMat, "Minaret_Flank_R");

            // Impossible Escher-esque Geometric Stairways mounted on the towers
            CreateImpossibleStairs(surroundRoot.transform, new Vector3(-6.2f, waterY + 4.5f, 11.0f), new Vector3(0.35f, 0.35f, 0), 7, new Vector3(0.40f, 0.15f, 0.90f), pathIvoryMat, "Escher_Stairs_L");
            CreateImpossibleStairs(surroundRoot.transform, new Vector3(5.2f, waterY + 5.0f, 14.0f), new Vector3(-0.35f, 0.35f, 0), 8, new Vector3(0.40f, 0.15f, 0.90f), pathIvoryMat, "Escher_Stairs_R");

            // Stylized Minimalist Geometric Trees
            CreateGeometricTree(surroundRoot.transform, new Vector3(-5.2f, waterY, 2.5f), 1.1f, "MV_Tree_L1");
            CreateGeometricTree(surroundRoot.transform, new Vector3(-8.5f, waterY, 6.0f), 1.3f, "MV_Tree_L2");
            CreateGeometricTree(surroundRoot.transform, new Vector3(10.5f, waterY, 3.2f), 1.2f, "MV_Tree_R1");
            CreateGeometricTree(surroundRoot.transform, new Vector3(14.0f, waterY, 7.5f), 1.3f, "MV_Tree_R2");
            CreateGeometricTree(surroundRoot.transform, new Vector3(0.5f, waterY, 13.0f), 1.4f, "MV_Tree_Back");

            // Floating dreamlike geometric accent cubes tilted at 45°
            CreateFloatingCube(surroundRoot.transform, new Vector3(-4.5f, 3.8f, 7.5f), 0.70f, trimMintMat, "Floating_Cube_1");
            CreateFloatingCube(surroundRoot.transform, new Vector3(9.2f, 4.2f, 8.5f), 0.80f, wallPeachMat, "Floating_Cube_2");
            CreateFloatingCube(surroundRoot.transform, new Vector3(2.5f, 5.2f, 11.5f), 0.65f, goldMat, "Floating_Cube_3");
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

            // Viewing balcony platform at stair base
            CreateBlock(stairsRoot.transform, new Vector3(-3.25f, -0.95f, 0.0f), new Vector3(1.45f, 0.85f, 1.15f), wallPeachMat, "Stairs_Plinth");

            // Decorative corner pillar with golden finial
            CreateCylinder(stairsRoot.transform, new Vector3(-3.90f, -0.05f, 0.0f), new Vector3(0.25f, 0.95f, 0.25f), trimMintMat, "Stair_Finial_Pillar");
            CreateSphere(stairsRoot.transform, new Vector3(-3.90f, 0.48f, 0.0f), new Vector3(0.32f, 0.32f, 0.32f), goldMat, "Stair_Finial_Ball");
        }

        private void BuildPlatform1Foundation(Transform parent) {
            GameObject fRoot = new GameObject("Foundation_Citadel_P1");
            fRoot.transform.SetParent(parent, false);

            float waterY = waterLevelY;

            // Tier 1: Just below the courtyard slabs (Y: 0.0 to -1.30)
            CreateBlock(fRoot.transform, new Vector3(-1.0f, -0.65f, 0.65f), new Vector3(3.20f, 1.30f, 2.30f), wallPeachMat, "Foundation_Tier_1");

            // Mint trim belt
            CreateBlock(fRoot.transform, new Vector3(-1.0f, -1.35f, 0.65f), new Vector3(3.35f, 0.14f, 2.45f), trimMintMat, "Foundation_Belt_1");

            // Tier 2: Middle shaft (Y: -1.40 to -3.00)
            CreateBlock(fRoot.transform, new Vector3(-1.0f, -2.20f, 0.65f), new Vector3(3.45f, 1.60f, 2.50f), foundationSandMat, "Foundation_Tier_2");

            // Mint trim belt 2
            CreateBlock(fRoot.transform, new Vector3(-1.0f, -3.05f, 0.65f), new Vector3(3.60f, 0.14f, 2.65f), trimMintMat, "Foundation_Belt_2");

            // Tier 3: Submerged bedrock plinth anchored into water (Y: -3.10 to waterY - 0.40f)
            float tier3Height = (-3.10f) - (waterY - 0.40f);
            float tier3CenterY = -3.10f - (tier3Height * 0.5f);
            CreateBlock(fRoot.transform, new Vector3(-1.0f, tier3CenterY, 0.65f), new Vector3(3.85f, tier3Height, 2.90f), wallPeachMat, "Foundation_Plinth_Water");
        }

        private void BuildPlatform2Foundation(Transform parent, Vector3 offset) {
            GameObject fRoot = new GameObject("Foundation_Citadel_P2");
            fRoot.transform.SetParent(parent, false);

            float waterY = waterLevelY;

            // 1. Foundation under Playable Courtyard (Z = 0.5f)
            // Local Y: 0.0 to -1.40 (World Y: -2.00 to -3.40)
            CreateBlock(fRoot.transform, new Vector3(6.0f, -0.70f, 0.5f) + offset, new Vector3(3.10f, 1.40f, 2.10f), wallPeachMat, "Foundation_P2_Tier_1");
            CreateBlock(fRoot.transform, new Vector3(6.0f, -1.45f, 0.5f) + offset, new Vector3(3.25f, 0.14f, 2.25f), trimMintMat, "Foundation_P2_Belt_1");

            // Tier 2 entering water (World Y: -3.50 to waterY - 0.40f)
            float worldBaseY = -2.00f;
            float p2Tier2Height = (worldBaseY - 1.50f) - (waterY - 0.40f);
            float p2Tier2CenterY = -1.50f - (p2Tier2Height * 0.5f);
            CreateBlock(fRoot.transform, new Vector3(6.0f, p2Tier2CenterY, 0.5f) + offset, new Vector3(3.45f, p2Tier2Height, 2.45f), foundationSandMat, "Foundation_P2_Plinth_Water");

            // 2. Colossal Stone Bastion under Elevated Sanctuary Terrace (Z = 2.65f)
            float terraceWorldY = 0.50f + offset.y; // -1.50f
            float bastionHeight = terraceWorldY - (waterY - 0.40f);
            float bastionCenterLocalY = 0.50f - (bastionHeight * 0.5f);
            Vector3 bastionPos = new Vector3(6.0f, bastionCenterLocalY, 2.65f) + offset;
            CreateBlock(fRoot.transform, bastionPos, new Vector3(4.10f, bastionHeight, 1.90f), wallPeachMat, "Foundation_P2_Terrace_Bastion");
        }

        private void BuildPlatform1Architecture(Transform parent, PathNode exitNode) {
            GameObject archRoot = new GameObject("Architecture_P1");
            archRoot.transform.SetParent(parent, false);

            // Grand Monument Valley Arched Gateway Portal
            Vector3 portalPos = new Vector3(-1.0f, 0.0f, 1.45f);

            // Left and Right slender portal minaret columns
            CreateCylinder(archRoot.transform, portalPos + new Vector3(-0.55f, 1.10f, 0), new Vector3(0.26f, 1.10f, 0.26f), pathIvoryMat, "Portal_Col_L");
            CreateSphere(archRoot.transform, portalPos + new Vector3(-0.55f, 2.25f, 0), new Vector3(0.34f, 0.34f, 0.34f), goldMat, "Portal_Col_Cap_L");

            CreateCylinder(archRoot.transform, portalPos + new Vector3(0.55f, 1.10f, 0), new Vector3(0.26f, 1.10f, 0.26f), pathIvoryMat, "Portal_Col_R");
            CreateSphere(archRoot.transform, portalPos + new Vector3(0.55f, 2.25f, 0), new Vector3(0.34f, 0.34f, 0.34f), goldMat, "Portal_Col_Cap_R");

            // Mint Arch Entablature
            CreateBlock(archRoot.transform, portalPos + new Vector3(0, 2.38f, 0), new Vector3(1.50f, 0.26f, 0.40f), trimMintMat, "Portal_Entablature");

            // Golden Semicircular Dome Pediment
            CreateSphere(archRoot.transform, portalPos + new Vector3(0, 2.62f, 0), new Vector3(0.85f, 0.50f, 0.38f), goldMat, "Portal_Dome_Pediment");

            // Golden Needle Spire
            CreateCylinder(archRoot.transform, portalPos + new Vector3(0, 3.10f, 0), new Vector3(0.08f, 0.45f, 0.08f), goldMat, "Portal_Spire");
            CreateSphere(archRoot.transform, portalPos + new Vector3(0, 3.58f, 0), new Vector3(0.18f, 0.18f, 0.18f), goldMat, "Portal_Spire_Finial");

            // Flanking Corner Minarets on Platform 1
            CreateMinaretTower(archRoot.transform, new Vector3(-2.35f, 0f, 1.45f), 4.2f, 0.30f, wallPeachMat, goldMat, trimMintMat, "Portal_Minaret_L");
            CreateMinaretTower(archRoot.transform, new Vector3(0.35f, 0f, 1.45f), 4.2f, 0.30f, wallPeachMat, goldMat, trimMintMat, "Portal_Minaret_R");

            // Minimalist geometric topiary planters on courtyard flanks
            CreateTopiaryPlanter(archRoot.transform, new Vector3(-2.25f, 0f, 0.45f), "Planter_P1_1");
            CreateTopiaryPlanter(archRoot.transform, new Vector3(-2.25f, 0f, -0.45f), "Planter_P1_2");

            // Exit Door Logic & Visuals
            CreateExitDoor(archRoot.transform, exitNode);
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

            // 4 Slender Pavilion Pillars
            float colHalfW = 1.35f;
            float colHalfD = 0.55f;
            Vector3[] colOffsets = new Vector3[] {
                new Vector3(-colHalfW, 0, -colHalfD),
                new Vector3(-colHalfW, 0, colHalfD),
                new Vector3(colHalfW, 0, -colHalfD),
                new Vector3(colHalfW, 0, colHalfD),
            };

            for (int i = 0; i < colOffsets.Length; i++) {
                Vector3 colPos = pavCenter + colOffsets[i];
                CreateCylinder(archRoot.transform, colPos + new Vector3(0, 0.85f, 0), new Vector3(0.22f, 0.85f, 0.22f), pathIvoryMat, $"Pavilion_Col_{i}");
                CreateSphere(archRoot.transform, colPos + new Vector3(0, 1.75f, 0), new Vector3(0.30f, 0.30f, 0.30f), goldMat, $"Pavilion_Col_Cap_{i}");
            }

            // Mint Entablature Cornice
            CreateBlock(archRoot.transform, pavCenter + new Vector3(0, 1.90f, 0), new Vector3(3.20f, 0.22f, 1.50f), trimMintMat, "Pavilion_Cornice");

            // Grand Golden Pavilion Dome
            CreateSphere(archRoot.transform, pavCenter + new Vector3(0, 2.50f, 0), new Vector3(2.20f, 1.25f, 1.50f), goldMat, "Pavilion_Dome");

            // Golden Needle Spire
            CreateCylinder(archRoot.transform, pavCenter + new Vector3(0, 3.40f, 0), new Vector3(0.10f, 0.65f, 0.10f), goldMat, "Pavilion_Spire");
            CreateSphere(archRoot.transform, pavCenter + new Vector3(0, 4.10f, 0), new Vector3(0.24f, 0.24f, 0.24f), goldMat, "Pavilion_Spire_Finial");

            // Flanking Ornamental Obelisks on Platform 2
            CreateObelisk(archRoot.transform, new Vector3(4.4f, 0f, 1.25f) + offset, "Obelisk_P2_L");
            CreateObelisk(archRoot.transform, new Vector3(7.6f, 0f, -0.25f) + offset, "Obelisk_P2_R");
        }

        private void CreateTopiaryPlanter(Transform parent, Vector3 localPos, string name) {
            GameObject planter = new GameObject(name);
            planter.transform.SetParent(parent, false);
            planter.transform.localPosition = localPos;

            // Clean white cylinder pot
            CreateCylinder(planter.transform, new Vector3(0, 0.18f, 0), new Vector3(0.36f, 0.18f, 0.36f), pathIvoryMat, "Pot");
            // Mint rim
            CreateCylinder(planter.transform, new Vector3(0, 0.36f, 0), new Vector3(0.40f, 0.04f, 0.40f), trimMintMat, "Rim");
            // Pastel sphere foliage
            CreateSphere(planter.transform, new Vector3(0, 0.60f, 0), new Vector3(0.46f, 0.46f, 0.46f), treeFoliageMat, "Topiary");
        }

        private void CreateObelisk(Transform parent, Vector3 worldPos, string name) {
            GameObject obelisk = new GameObject(name);
            obelisk.transform.SetParent(parent, false);
            obelisk.transform.position = worldPos;

            // Stepped base
            CreateBlock(obelisk.transform, new Vector3(0, 0.12f, 0), new Vector3(0.46f, 0.24f, 0.46f), trimMintMat, "Base");
            // Slender shaft
            CreateBlock(obelisk.transform, new Vector3(0, 0.70f, 0), new Vector3(0.32f, 0.95f, 0.32f), wallPeachMat, "Shaft");
            // Golden pyramidal cap / finial
            CreateSphere(obelisk.transform, new Vector3(0, 1.25f, 0), new Vector3(0.28f, 0.32f, 0.28f), goldMat, "Finial");
        }

        private void CreateMinaretTower(Transform parent, Vector3 position, float totalHeight, float radius, Material bodyMat, Material domeMat, Material trimMat, string name) {
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent, false);
            tower.transform.position = position;

            float shaftHeight = totalHeight - 1.8f;
            // Cylindrical tower shaft
            CreateCylinder(tower.transform, new Vector3(0, shaftHeight * 0.5f, 0), new Vector3(radius * 2f, shaftHeight * 0.5f, radius * 2f), bodyMat, "Tower_Shaft");

            // Base plinth
            CreateBlock(tower.transform, new Vector3(0, 0.4f, 0), new Vector3(radius * 2.6f, 0.8f, radius * 2.6f), trimMat, "Tower_Base");

            // Balcony collar
            CreateCylinder(tower.transform, new Vector3(0, shaftHeight, 0), new Vector3(radius * 2.4f, 0.25f, radius * 2.4f), trimMat, "Tower_Balcony");

            // Dome
            float domeY = shaftHeight + (radius * 0.9f);
            CreateSphere(tower.transform, new Vector3(0, domeY, 0), new Vector3(radius * 2.2f, radius * 1.8f, radius * 2.2f), domeMat, "Tower_Dome");

            // Golden needle spire
            float spireY = domeY + (radius * 0.9f) + 0.5f;
            CreateCylinder(tower.transform, new Vector3(0, spireY, 0), new Vector3(0.08f, 0.55f, 0.08f), domeMat, "Tower_Spire");
            CreateSphere(tower.transform, new Vector3(0, spireY + 0.58f, 0), new Vector3(0.18f, 0.18f, 0.18f), domeMat, "Tower_Spire_Finial");
        }

        private void CreateGeometricTree(Transform parent, Vector3 position, float scale, string name) {
            GameObject tree = new GameObject(name);
            tree.transform.SetParent(parent, false);
            tree.transform.position = position;

            // Slender white trunk
            CreateCylinder(tree.transform, new Vector3(0, 0.8f * scale, 0), new Vector3(0.12f * scale, 0.8f * scale, 0.12f * scale), treeTrunkMat, "Trunk");

            // Geometric spherical foliage
            CreateSphere(tree.transform, new Vector3(0, 1.8f * scale, 0), new Vector3(0.9f * scale, 0.9f * scale, 0.9f * scale), treeFoliageMat, "Foliage_Base");
            CreateSphere(tree.transform, new Vector3(0, 2.3f * scale, 0), new Vector3(0.65f * scale, 0.65f * scale, 0.65f * scale), trimMintMat, "Foliage_Top");
        }

        private void CreateFloatingLilyPad(Transform parent, Vector3 position, float radius, string name) {
            CreateCylinder(parent, position, new Vector3(radius * 2f, 0.04f, radius * 2f), waterPadMat, name);
            // Center white/gold lotus bud
            CreateSphere(parent, position + new Vector3(0, 0.06f, 0), new Vector3(0.24f, 0.14f, 0.24f), pathIvoryMat, name + "_Flower");
            CreateSphere(parent, position + new Vector3(0, 0.10f, 0), new Vector3(0.10f, 0.10f, 0.10f), goldMat, name + "_Center");
        }

        private void CreateImpossibleStairs(Transform parent, Vector3 startPos, Vector3 stepDelta, int stepCount, Vector3 stepSize, Material mat, string name) {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            for (int i = 0; i < stepCount; i++) {
                Vector3 pos = startPos + (stepDelta * i);
                CreateBlock(group.transform, pos, stepSize, mat, $"Step_{i}");
            }
        }

        private void CreateFloatingCube(Transform parent, Vector3 position, float size, Material mat, string name) {
            CreateBlock(parent, position, Vector3.one * size, mat, name, Quaternion.Euler(45f, 45f, 0f));
        }

        private void CreateKeyItemAndAltar(Transform parent, PathNode node) {
            GameObject keyRoot = new GameObject("Key_Pedestal_And_Item");
            keyRoot.transform.SetParent(parent, false);
            keyRoot.transform.position = node.transform.position;

            // 1. Stepped Circular Geometric Pedestal
            CreateCylinder(keyRoot.transform, new Vector3(0, 0.08f, 0), new Vector3(0.85f, 0.08f, 0.85f), trimMintMat, "Pedestal_Base");
            CreateCylinder(keyRoot.transform, new Vector3(0, 0.22f, 0), new Vector3(0.68f, 0.12f, 0.68f), pathIvoryMat, "Pedestal_Mid");
            CreateCylinder(keyRoot.transform, new Vector3(0, 0.38f, 0), new Vector3(0.50f, 0.12f, 0.50f), goldMat, "Pedestal_Top");

            // 2. Floating Rotating Sacred Geometric Star Artifact
            GameObject keyVisual = new GameObject("KeyVisual");
            keyVisual.transform.SetParent(keyRoot.transform, false);
            keyVisual.transform.localPosition = new Vector3(0, 0.95f, 0);

            // Central golden octahedron / diamond
            CreateBlock(keyVisual.transform, Vector3.zero, Vector3.one * 0.35f, goldMat, "Star_Core", Quaternion.Euler(45f, 45f, 45f));
            // Interlocking mint jewel cube
            CreateBlock(keyVisual.transform, Vector3.zero, Vector3.one * 0.26f, trimMintMat, "Star_Inner", Quaternion.Euler(0f, 45f, 0f));
            // Golden halo ring
            CreateCylinder(keyVisual.transform, new Vector3(0, 0.20f, 0), new Vector3(0.24f, 0.03f, 0.24f), goldMat, "Halo_Ring");

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

        private void CreateExitDoor(Transform parent, PathNode node) {
            GameObject doorRoot = new GameObject("ExitDoor");
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.position = node.transform.position;

            Vector3 archCenter = node.transform.position + new Vector3(0f, 0f, 0.45f);

            // Closed Door Leaf (Serene Deep Indigo)
            GameObject closedDoor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            closedDoor.name = "ClosedDoor";
            closedDoor.transform.SetParent(doorRoot.transform, false);
            closedDoor.transform.position = archCenter + new Vector3(0, 0.95f, -0.05f);
            closedDoor.transform.localScale = new Vector3(0.88f, 1.70f, 0.10f);
            closedDoor.GetComponent<Renderer>().sharedMaterial = doorIndigoMat;
            var cdCol = closedDoor.GetComponent<Collider>();
            if (cdCol != null) DestroyImmediate(cdCol);

            // Golden door handle emblem
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = "DoorHandle";
            handle.transform.SetParent(closedDoor.transform, false);
            handle.transform.localPosition = new Vector3(0.25f, 0f, -0.55f);
            handle.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            handle.GetComponent<Renderer>().sharedMaterial = goldMat;
            var hCol = handle.GetComponent<Collider>();
            if (hCol != null) DestroyImmediate(hCol);

            // Open Door Portal Quad (Radiant Glowing Warm Ivory pouring light)
            GameObject openPortal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            openPortal.name = "OpenPortal";
            openPortal.transform.SetParent(doorRoot.transform, false);
            openPortal.transform.position = archCenter + new Vector3(0f, 1.05f, 0.05f);
            openPortal.transform.localScale = new Vector3(1.15f, 2.05f, 1f);
            openPortal.transform.rotation = Quaternion.identity;
            openPortal.GetComponent<Renderer>().sharedMaterial = doorSunlightMat;
            var qCol = openPortal.GetComponent<Collider>();
            if (qCol != null) DestroyImmediate(qCol);
            openPortal.SetActive(false);

            // Portal Light
            GameObject doorLightObj = new GameObject("DoorLight");
            doorLightObj.transform.SetParent(doorRoot.transform, false);
            doorLightObj.transform.position = archCenter + new Vector3(0f, 1.05f, 0.2f);
            var dLight = doorLightObj.AddComponent<Light>();
            dLight.type = LightType.Point;
            dLight.color = new Color(1.0f, 0.94f, 0.75f);
            dLight.intensity = 0f;
            dLight.range = 5f;
            dLight.shadows = LightShadows.None;

            var exitDoor = doorRoot.AddComponent<ExitDoor>();
            SetPrivateField(exitDoor, "closedDoorVisual", closedDoor);
            SetPrivateField(exitDoor, "openDoorVisual", openPortal);
            SetPrivateField(exitDoor, "doorLight", dLight);
            exitDoor.Initialize(node);
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
            // Serene Monument Valley Sky Background
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

            // Soft pastel sky ambient fill (illuminates unlit sides with gentle cyan/lavender glow)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.54f, 0.64f);

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

            // Level Manager & UI
            GameObject mgrObj = GameObject.Find("LevelManager");
            if (mgrObj == null) {
                mgrObj = new GameObject("LevelManager");
                mgrObj.transform.SetParent(parent, false);
            }
            var lvlMgr = mgrObj.GetComponent<LevelManager>();
            if (lvlMgr == null) {
                lvlMgr = mgrObj.AddComponent<LevelManager>();
            }

            var uiComp = mgrObj.GetComponent<GameUI>();
            if (uiComp == null) {
                uiComp = mgrObj.AddComponent<GameUI>();
            }
            uiComp.Initialize(camController, lvlMgr);

            // 1. Warm Creamy Directional Sunlight
            var dirLight = GameObject.Find("Directional Light");
            if (dirLight != null) {
                var l = dirLight.GetComponent<Light>();
                if (l != null) {
                    l.color = new Color(1.0f, 0.95f, 0.88f);
                    l.intensity = 1.15f;
                    l.shadows = LightShadows.Soft;
                    l.shadowStrength = 0.60f; // Soft, gentle pastel shadows
                    l.shadowBias = 0.02f;
                    l.shadowNormalBias = 0.25f;
                }
                dirLight.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            }

            // 2. Clean Level Accent Spotlight
            GameObject spotP1Obj = GameObject.Find("Level_Spotlight_P1");
            if (spotP1Obj == null) {
                spotP1Obj = new GameObject("Level_Spotlight_P1");
                spotP1Obj.transform.SetParent(parent, false);
                var sP1 = spotP1Obj.AddComponent<Light>();
                sP1.type = LightType.Spot;
            }
            var sP1Light = spotP1Obj.GetComponent<Light>();
            sP1Light.color = new Color(1.0f, 0.95f, 0.88f);
            sP1Light.intensity = 2.2f;
            sP1Light.range = 14f;
            sP1Light.spotAngle = 65f;
            sP1Light.innerSpotAngle = 38f;
            sP1Light.shadows = LightShadows.None;
            spotP1Obj.transform.position = new Vector3(-1.0f, 6.5f, 0.5f);
            spotP1Obj.transform.rotation = Quaternion.Euler(75f, -30f, 0f);

            GameObject spotBridgeObj = GameObject.Find("Level_Spotlight_Bridge");
            if (spotBridgeObj == null) {
                spotBridgeObj = new GameObject("Level_Spotlight_Bridge");
                spotBridgeObj.transform.SetParent(parent, false);
                var sB = spotBridgeObj.AddComponent<Light>();
                sB.type = LightType.Spot;
            }
            var sBLight = spotBridgeObj.GetComponent<Light>();
            sBLight.color = new Color(1.0f, 0.95f, 0.88f);
            sBLight.intensity = 1.8f;
            sBLight.range = 12f;
            sBLight.spotAngle = 55f;
            sBLight.innerSpotAngle = 32f;
            sBLight.shadows = LightShadows.None;
            spotBridgeObj.transform.position = new Vector3(2.5f, 5.5f, 0.0f);
            spotBridgeObj.transform.rotation = Quaternion.Euler(80f, 0f, 0f);

            GameObject spotP2Obj = GameObject.Find("Level_Spotlight_P2");
            if (spotP2Obj == null) {
                spotP2Obj = new GameObject("Level_Spotlight_P2");
                spotP2Obj.transform.SetParent(parent, false);
                var sP2 = spotP2Obj.AddComponent<Light>();
                sP2.type = LightType.Spot;
            }
            var sP2Light = spotP2Obj.GetComponent<Light>();
            sP2Light.color = new Color(1.0f, 0.95f, 0.88f);
            sP2Light.intensity = 2.2f;
            sP2Light.range = 14f;
            sP2Light.spotAngle = 65f;
            sP2Light.innerSpotAngle = 38f;
            sP2Light.shadows = LightShadows.None;
            spotP2Obj.transform.position = new Vector3(6.0f, 4.5f, 1.0f) + illusionOffset;
            spotP2Obj.transform.rotation = Quaternion.Euler(75f, -30f, 0f);

            // Clean up any old unused lights
            var oldFill = GameObject.Find("Atmospheric_Fill_Light");
            if (oldFill != null) DestroyImmediate(oldFill);
            var oldFloor = GameObject.Find("Subterranean_Floor_Light");
            if (oldFloor != null) DestroyImmediate(oldFloor);

            // 3. Post-Processing Volume (ACES Tonemapping & Subtle Bloom)
            SetupPostProcessing(parent);
        }

        private void SetupPostProcessing(Transform parent) {
            GameObject volObj = GameObject.Find("PostProcess_Volume");
            if (volObj == null) {
                volObj = new GameObject("PostProcess_Volume");
                volObj.transform.SetParent(parent, false);
            }
            var vol = volObj.GetComponent<Volume>();
            if (vol == null) vol = volObj.AddComponent<Volume>();
            vol.isGlobal = true;

            var profile = vol.sharedProfile;
            if (profile == null) {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                profile.name = "MonumentValley_Profile";
                vol.sharedProfile = profile;
            }

            // ACES Tonemapping for rich filmic pastel palette
            Tonemapping tone;
            if (!profile.TryGet(out tone)) tone = profile.Add<Tonemapping>(true);
            tone.active = true;
            tone.mode.overrideState = true;
            tone.mode.value = TonemappingMode.ACES;

            // Subtle gentle bloom on gold and portal
            Bloom bloom;
            if (!profile.TryGet(out bloom)) bloom = profile.Add<Bloom>(true);
            bloom.active = true;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.92f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 1.0f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(1.0f, 0.95f, 0.85f);

            // Color adjustments
            ColorAdjustments ca;
            if (!profile.TryGet(out ca)) ca = profile.Add<ColorAdjustments>(true);
            ca.active = true;
            ca.contrast.overrideState = true;
            ca.contrast.value = 15f;
            ca.saturation.overrideState = true;
            ca.saturation.value = 12f;
            ca.postExposure.overrideState = true;
            ca.postExposure.value = 0.10f;
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

            return node;
        }

        private GameObject CreateBlock(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name = "Block", Quaternion? localRot = null) {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPos;
            block.transform.localScale = localScale;
            if (localRot.HasValue) block.transform.localRotation = localRot.Value;

            var col = block.GetComponent<Collider>();
            if (col != null) {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            if (mat != null) {
                block.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return block;
        }

        private GameObject CreateCylinder(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name = "Cylinder", Quaternion? localRot = null) {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = name;
            cyl.transform.SetParent(parent, false);
            cyl.transform.localPosition = localPos;
            cyl.transform.localScale = localScale;
            if (localRot.HasValue) cyl.transform.localRotation = localRot.Value;

            var col = cyl.GetComponent<Collider>();
            if (col != null) {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            if (mat != null) {
                cyl.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return cyl;
        }

        private GameObject CreateSphere(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name = "Sphere") {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPos;
            sphere.transform.localScale = localScale;

            var col = sphere.GetComponent<Collider>();
            if (col != null) {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            if (mat != null) {
                sphere.GetComponent<Renderer>().sharedMaterial = mat;
            }
            return sphere;
        }

        private static void ConnectGridNeighbors(Dictionary<Vector2Int, PathNode> nodes) {
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var kvp in nodes) {
                Vector2Int coord = kvp.Key;
                PathNode node = kvp.Value;

                for (int d = 0; d < dirs.Length; d++) {
                    Vector2Int neighborCoord = coord + dirs[d];
                    if (nodes.TryGetValue(neighborCoord, out PathNode neighbor)) {
                        node.AddNeighbor(neighbor);
                    }
                }
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value) {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            if (field != null) {
                field.SetValue(target, value);
            }
        }
    }
}
