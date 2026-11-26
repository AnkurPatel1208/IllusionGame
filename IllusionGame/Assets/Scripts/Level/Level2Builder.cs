using System.Collections.Generic;
using CameraControl;
using Core.Events;
using Gameplay;
using Grid;
using Mechanics;
using Player;
using UI;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Level {
    /// <summary>
    /// Constructs Level 2 – "The Rotary Citadel" with pure Monument Valley architectural elegance:
    /// - 100% solid, grounded monolithic architecture (Zero floating slabs, awkward gaps, or clutter)
    /// - Concentric terraced lush green diorama meadow hill base
    /// - Symmetrical Twin Citadel Towers with open-air 4-pillar pavilions and golden onion domes
    /// - Ground Courtyard with 3-arch Moorish arcade and ascending flank staircases
    /// - Central Rotary Gear Bridge with interactive brass gear wheel
    ///   - 0°: Connects Left Tower to Right Tower across the citadel
    ///   - 90°: Swings to connect Left Tower to the elevated Secret Key Balcony
    /// - Elevated Secret Key Balcony with collectible Sacred Golden Key
    /// - Optical Illusion Perspective Bridge to the High Summit Sanctuary:
    ///   - When camera yaw aligns at 45°, the perspective collapses the depth gap!
    ///   - PerspectiveEdge connects the Citadel to the Grand Summit Staircase!
    /// - Grand Summit Staircase climbing to the Radiant Golden Exit Portal and Onion Cupola
    /// - Pure aesthetic fidelity matching the approved concept design (Level2_Design_Concept_v2.jpg)
    /// </summary>
    public class Level2Builder : MonoBehaviour {
        [Header("Monument Valley Palette")]
        [SerializeField] private Color monumentPeachColor = new Color(0.92f, 0.60f, 0.52f);
        [SerializeField] private Color wallShadowPeachColor = new Color(0.82f, 0.50f, 0.44f);
        [SerializeField] private Color pathIvoryColor = Color.white;
        [SerializeField] private Color trimMintColor = new Color(0.40f, 0.74f, 0.70f);
        [SerializeField] private Color accentGoldColor = new Color(0.96f, 0.78f, 0.32f);
        [SerializeField] private Color foundationSandColor = new Color(0.85f, 0.52f, 0.46f);
        [SerializeField] private Color grassLushColor = new Color(0.70f, 0.82f, 0.30f);
        [SerializeField] private Color grassEdgeColor = new Color(0.78f, 0.86f, 0.35f);
        [SerializeField] private Color skyColor = new Color(0.965f, 0.902f, 0.612f);

        [Header("Camera & Framing")]
        [SerializeField] private float waterLevelY = -4.50f;
        [SerializeField] private float camOrthoSize = 6.8f;
        [SerializeField] private float camPitch = 30f;
        [SerializeField] private float camYaw = 45f;
        [SerializeField] private Vector3 camPivotCenter = new Vector3(0.6f, 2.4f, 0.6f);

        [Header("Illusion Geometry")]
        [SerializeField] private float depthOffset = 3.6f;

        [Header("Build Config")]
        [SerializeField] private bool buildOnStart = false;

        // Procedural Materials
        private Material wallPeachMat;
        private Material wallShadowMat;
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

#if UNITY_EDITOR
        [ContextMenu("Bake Level 2 To Scene")]
        public void BakeLevel() {
            BuildLevel();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("<color=green>[Level2Builder]</color> Level 2 baked successfully into scene hierarchy!");
        }

        [ContextMenu("Clear Baked Level")]
        public void ClearBakedLevel() {
            GameObject existing = GameObject.Find("Level2_Environment");
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
            GameObject existing = GameObject.Find("Level2_Environment");
            if (existing != null) {
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }

            CreateMaterials();

            GameObject root = new GameObject("Level2_Environment");
            Transform rootT = root.transform;

            // 1. Concentric Stepped Circular Meadow Lawn Hill
            BuildGardenMeadow(rootT);

            // 2. Monolithic Citadel Base & Foundations
            BuildCitadelFoundations(rootT);

            // 3. Ground Courtyard (Spawn Plaza, 3-Arch Arcade, & Flank Staircases)
            PathNode spawnNode, leftTowerLandingNode, rightTowerLandingNode;
            BuildGroundCourtyardAndStairs(rootT, out spawnNode, out leftTowerLandingNode, out rightTowerLandingNode);

            // 4. Twin Citadel Towers (Left and Right Flank Towers)
            PathNode leftBridgeMountNode, rightBridgeMountNode;
            BuildTwinTowers(rootT, leftTowerLandingNode, rightTowerLandingNode, out leftBridgeMountNode, out rightBridgeMountNode);

            // 5. Secret Key Balcony & Sacred Golden Key
            PathNode keyBalconyNode;
            BuildKeyBalcony(rootT, out keyBalconyNode);

            // 6. Central Rotary Gear Bridge Mechanism
            BuildRotaryGearBridge(rootT, leftBridgeMountNode, rightBridgeMountNode, keyBalconyNode);

            // 7. Right Tower Walkway, Perspective Illusion Seam & High Summit Sanctuary
            BuildSummitIllusionAndSanctuary(rootT, rightBridgeMountNode);

            // 8. Player Character (Princess Ida)
            BuildPlayerCharacter(rootT, spawnNode);

            // 9. Camera, Lighting, Interaction & UI Managers
            SetupCameraLightingAndManagers(rootT);

            Debug.Log("<color=green>[Level2Builder]</color> Level 2 successfully built with authentic Monument Valley architecture and hybrid illusion mechanics!");
        }

        private void CreateMaterials() {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (litShader == null) litShader = Shader.Find("Standard");

            Material MakeMat(Color col, float smooth = 0.15f, float metallic = 0f, string name = "M_Temp") {
                var m = new Material(litShader) { name = name };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                else if (m.HasProperty("_Color")) m.SetColor("_Color", col);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
                return m;
            }

            wallPeachMat = MakeMat(monumentPeachColor, 0.12f, 0f, "M_MV_PeachWall");
            wallShadowMat = MakeMat(wallShadowPeachColor, 0.10f, 0f, "M_MV_ShadowPeach");
            pathIvoryMat = MakeMat(pathIvoryColor, 0.08f, 0f, "M_MV_IvoryPath");
            trimMintMat = MakeMat(trimMintColor, 0.18f, 0f, "M_MV_MintTrim");
            foundationSandMat = MakeMat(foundationSandColor, 0.12f, 0f, "M_MV_SandFoundation");
            grassLushMat = MakeMat(grassLushColor, 0.06f, 0f, "M_MV_GrassLush");
            grassEdgeMat = MakeMat(grassEdgeColor, 0.06f, 0f, "M_MV_GrassEdge");

            goldMat = MakeMat(accentGoldColor, 0.40f, 0.35f, "M_MV_BurnishedGold");
            goldMat.EnableKeyword("_EMISSION");
            goldMat.SetColor("_EmissionColor", accentGoldColor * 1.6f);

            doorIndigoMat = MakeMat(new Color(0.12f, 0.16f, 0.24f), 0.20f, 0f, "M_MV_DoorIndigo");

            doorSunlightMat = MakeMat(new Color(1.0f, 0.95f, 0.80f), 0.10f, 0f, "M_MV_DoorSunlight");
            doorSunlightMat.EnableKeyword("_EMISSION");
            doorSunlightMat.SetColor("_EmissionColor", new Color(1.0f, 0.92f, 0.65f) * 3.0f);

            idaDressMat = MakeMat(new Color(0.98f, 0.98f, 0.98f), 0.08f, 0f, "M_MV_IdaDress");
            idaSkinMat = MakeMat(new Color(1.0f, 0.88f, 0.82f), 0.08f, 0f, "M_MV_IdaSkin");
        }

        private void BuildGardenMeadow(Transform parent) {
            GameObject meadowRoot = new GameObject("Monument_Garden_Meadow");
            meadowRoot.transform.SetParent(parent, false);

            Vector3 center = new Vector3(camPivotCenter.x, waterLevelY, camPivotCenter.z);

            CreateCylinder(meadowRoot.transform, center + new Vector3(0, -0.40f, 0), new Vector3(22.0f, 0.60f, 22.0f), grassEdgeMat, "Hill_Tier_Base");
            CreateCylinder(meadowRoot.transform, center + new Vector3(0, 0.35f, 0), new Vector3(18.0f, 0.60f, 18.0f), grassLushMat, "Hill_Tier_1");
            CreateCylinder(meadowRoot.transform, center + new Vector3(0, 1.10f, 0), new Vector3(14.5f, 0.60f, 14.5f), grassEdgeMat, "Hill_Tier_2");
            CreateCylinder(meadowRoot.transform, center + new Vector3(0, 1.85f, 0), new Vector3(11.0f, 0.60f, 11.0f), grassLushMat, "Hill_Tier_3");
            CreateCylinder(meadowRoot.transform, center + new Vector3(0, 2.50f, 0), new Vector3(8.0f, 0.50f, 8.0f), grassLushMat, "Hill_Tier_Top");
        }

        private void BuildCitadelFoundations(Transform parent) {
            GameObject fRoot = new GameObject("Citadel_Foundations");
            fRoot.transform.SetParent(parent, false);
            Transform ft = fRoot.transform;

            // Monolithic Solid Plinth Mass supporting entire citadel footprint (Y: -2.40 to 0.0)
            Vector3 center = new Vector3(0.6f, -1.2f, 0.6f);
            CreateBlock(ft, center, new Vector3(7.4f, 2.4f, 7.4f), foundationSandMat, "Plinth_Sand_Mass");
            CreateBlock(ft, new Vector3(center.x, -0.1f, center.z), new Vector3(7.0f, 0.4f, 7.0f), wallPeachMat, "Plinth_Peach_Mass");
            CreateBlock(ft, new Vector3(center.x, 0.02f, center.z), new Vector3(7.1f, 0.06f, 7.1f), trimMintMat, "Plinth_Mint_Belt");
        }

        private void BuildGroundCourtyardAndStairs(Transform parent, out PathNode spawnNode, out PathNode leftLanding, out PathNode rightLanding) {
            GameObject cRoot = new GameObject("Ground_Courtyard_And_Stairs");
            cRoot.transform.SetParent(parent, false);
            Transform ct = cRoot.transform;

            // 1. Central Diamond Spawn Plaza at (0, 0, 0)
            CreateBlock(ct, new Vector3(0f, -0.2f, 0f), new Vector3(1.6f, 0.4f, 1.6f), wallPeachMat, "Spawn_Plaza_Slab");
            CreateBlock(ct, new Vector3(0f, 0.01f, 0f), new Vector3(1.64f, 0.04f, 1.64f), trimMintMat, "Spawn_Plaza_Trim");

            var nSpawn = CreateTile(ct, new Vector3(0f, 0.05f, 0f), "Tile_Spawn");
            spawnNode = nSpawn;

            // 2. Moorish 3-Arch Arcade Facade behind Ida
            GameObject arcade = new GameObject("Central_Arcade_Facade");
            arcade.transform.SetParent(ct, false);
            arcade.transform.position = new Vector3(0.6f, 0f, 0.6f);
            arcade.transform.rotation = Quaternion.Euler(0f, -45f, 0f);

            float arcW = 2.4f;
            float arcH = 1.8f;
            CreateBlock(arcade.transform, new Vector3(0f, arcH * 0.5f, 0.2f), new Vector3(arcW, arcH, 0.4f), wallPeachMat, "Arcade_Wall_Solid");

            float[] archXs = new float[] { -0.7f, 0.0f, 0.7f };
            for (int i = 0; i < archXs.Length; i++) {
                float ax = archXs[i];
                CreateBlock(arcade.transform, new Vector3(ax, 0.65f, 0.05f), new Vector3(0.40f, 1.10f, 0.25f), doorIndigoMat, $"Arch_Alcove_{i}");
                CreateCylinder(arcade.transform, new Vector3(ax, 1.20f, 0.05f), new Vector3(0.42f, 0.26f, 0.42f), trimMintMat, $"Arch_Crown_{i}", Quaternion.Euler(90f, 0f, 0f));
            }

            // 3. Left Flank Staircase (Climbing along -X, +Z to Left Tower)
            CreateBlock(ct, new Vector3(-0.9f, 1.0f, 0.9f), new Vector3(1.4f, 2.0f, 1.4f), wallPeachMat, "Left_Stair_Solid_Riser", Quaternion.Euler(0f, -45f, 0f));
            CreateBlock(ct, new Vector3(-1.3f, 1.3f, 1.3f), new Vector3(0.14f, 2.4f, 2.4f), wallPeachMat, "Left_Stair_CheekWall", Quaternion.Euler(0f, -45f, 0f));

            int steps = 5;
            PathNode prevLeft = nSpawn;
            for (int s = 1; s <= steps; s++) {
                float t = (float)s / steps;
                float sx = Mathf.Lerp(0f, -1.8f, t);
                float sy = Mathf.Lerp(0.05f, 2.40f, t);
                float sz = Mathf.Lerp(0f, 1.8f, t);
                var sNode = CreateStepTile(ct, new Vector3(sx, sy, sz), $"Step_Left_{s}");
                prevLeft.AddNeighbor(sNode); sNode.AddNeighbor(prevLeft);
                prevLeft = sNode;
            }
            leftLanding = prevLeft;

            // 4. Right Flank Staircase (Climbing along +X, -Z to Right Tower)
            CreateBlock(ct, new Vector3(0.9f, 1.0f, -0.9f), new Vector3(1.4f, 2.0f, 1.4f), wallPeachMat, "Right_Stair_Solid_Riser", Quaternion.Euler(0f, -45f, 0f));
            CreateBlock(ct, new Vector3(1.3f, 1.3f, -1.3f), new Vector3(0.14f, 2.4f, 2.4f), wallPeachMat, "Right_Stair_CheekWall", Quaternion.Euler(0f, -45f, 0f));

            PathNode prevRight = nSpawn;
            for (int s = 1; s <= steps; s++) {
                float t = (float)s / steps;
                float sx = Mathf.Lerp(0f, 1.8f, t);
                float sy = Mathf.Lerp(0.05f, 2.40f, t);
                float sz = Mathf.Lerp(0f, -1.8f, t);
                var sNode = CreateStepTile(ct, new Vector3(sx, sy, sz), $"Step_Right_{s}");
                prevRight.AddNeighbor(sNode); sNode.AddNeighbor(prevRight);
                prevRight = sNode;
            }
            rightLanding = prevRight;
        }

        private void BuildTwinTowers(Transform parent, PathNode leftLanding, PathNode rightLanding, out PathNode leftBridgeMount, out PathNode rightBridgeMount) {
            GameObject tRoot = new GameObject("Twin_Citadel_Towers");
            tRoot.transform.SetParent(parent, false);
            Transform tt = tRoot.transform;

            // ==========================================
            // 1. LEFT CITADEL TOWER (At -1.8, 1.8)
            // ==========================================
            Vector3 leftTowerPos = new Vector3(-1.8f, 0f, 1.8f);
            CreateBlock(tt, leftTowerPos + new Vector3(0, 1.8f, 0), new Vector3(1.5f, 3.6f, 1.5f), wallPeachMat, "Left_Tower_Shaft");
            CreateBlock(tt, leftTowerPos + new Vector3(0, 3.65f, 0), new Vector3(1.64f, 0.14f, 1.64f), trimMintMat, "Left_Tower_Cornice");
            BuildTowerPavilion(tt, leftTowerPos + new Vector3(0, 3.72f, 0), "Left_Tower_Pavilion");
            CreateObelisk(tt, leftTowerPos + new Vector3(-0.65f, 2.40f, 0.65f), "Obelisk_Left_Tower");

            // Bridge launch terrace
            var nLeftTerrace = CreateTile(tt, new Vector3(-1.0f, 2.40f, 1.0f), "Tile_Left_Tower_Terrace");
            leftLanding.AddNeighbor(nLeftTerrace); nLeftTerrace.AddNeighbor(leftLanding);
            leftBridgeMount = nLeftTerrace;

            // ==========================================
            // 2. RIGHT CITADEL TOWER (At 1.8, -1.8)
            // ==========================================
            Vector3 rightTowerPos = new Vector3(1.8f, 0f, -1.8f);
            CreateBlock(tt, rightTowerPos + new Vector3(0, 1.8f, 0), new Vector3(1.5f, 3.6f, 1.5f), wallPeachMat, "Right_Tower_Shaft");
            CreateBlock(tt, rightTowerPos + new Vector3(0, 3.65f, 0), new Vector3(1.64f, 0.14f, 1.64f), trimMintMat, "Right_Tower_Cornice");
            BuildTowerPavilion(tt, rightTowerPos + new Vector3(0, 3.72f, 0), "Right_Tower_Pavilion");
            CreateObelisk(tt, rightTowerPos + new Vector3(0.65f, 2.40f, -0.65f), "Obelisk_Right_Tower");

            // Bridge landing terrace
            var nRightTerrace = CreateTile(tt, new Vector3(1.0f, 2.40f, -1.0f), "Tile_Right_Tower_Terrace");
            rightLanding.AddNeighbor(nRightTerrace); nRightTerrace.AddNeighbor(rightLanding);
            rightBridgeMount = nRightTerrace;
        }

        private void BuildTowerPavilion(Transform parent, Vector3 basePos, string name) {
            GameObject pav = new GameObject(name);
            pav.transform.SetParent(parent, false);
            pav.transform.position = basePos;

            float w = 1.10f;
            float colH = 1.10f;
            float colR = 0.12f;

            Vector3[] colOffsets = new Vector3[] {
                new Vector3(-w*0.42f, 0, -w*0.42f),
                new Vector3( w*0.42f, 0, -w*0.42f),
                new Vector3(-w*0.42f, 0,  w*0.42f),
                new Vector3( w*0.42f, 0,  w*0.42f)
            };
            for (int i = 0; i < 4; i++) {
                CreateBlock(pav.transform, colOffsets[i] + new Vector3(0, colH * 0.5f, 0), new Vector3(colR, colH, colR), trimMintMat, $"Pillar_{i+1}");
            }

            CreateBlock(pav.transform, new Vector3(0, colH - 0.08f, -w*0.42f), new Vector3(w*0.84f, 0.14f, colR), wallPeachMat, "Lintel_F");
            CreateBlock(pav.transform, new Vector3(0, colH - 0.08f,  w*0.42f), new Vector3(w*0.84f, 0.14f, colR), wallPeachMat, "Lintel_B");
            CreateBlock(pav.transform, new Vector3(-w*0.42f, colH - 0.08f, 0), new Vector3(colR, 0.14f, w*0.84f), wallPeachMat, "Lintel_L");
            CreateBlock(pav.transform, new Vector3( w*0.42f, colH - 0.08f, 0), new Vector3(colR, 0.14f, w*0.84f), wallPeachMat, "Lintel_R");

            float roofY = colH + 0.08f;
            CreateBlock(pav.transform, new Vector3(0, roofY, 0), new Vector3(w + 0.15f, 0.14f, w + 0.15f), trimMintMat, "Roof_Entablature");

            // Burnished Golden Onion Dome
            float domeBaseY = roofY + 0.07f;
            CreateSphere(pav.transform, new Vector3(0, domeBaseY + 0.38f, 0), new Vector3(0.95f, 0.70f, 0.95f), goldMat, "Onion_Bulb");
            CreateCylinder(pav.transform, new Vector3(0, domeBaseY + 0.75f, 0), new Vector3(0.40f, 0.35f, 0.40f), goldMat, "Onion_Taper");
            CreateCylinder(pav.transform, new Vector3(0, domeBaseY + 1.05f, 0), new Vector3(0.08f, 0.45f, 0.08f), goldMat, "Needle_Spire");
            CreateSphere(pav.transform, new Vector3(0, domeBaseY + 1.32f, 0), new Vector3(0.18f, 0.18f, 0.18f), goldMat, "Finial_Ball");
        }

        private void BuildKeyBalcony(Transform parent, out PathNode keyBalconyNode) {
            GameObject bRoot = new GameObject("Key_Balcony_Terrace");
            bRoot.transform.SetParent(parent, false);

            // Balcony is positioned at (1.0, 2.4, 3.0), where the rotary bridge arm swings at 90°!
            Vector3 balcPos = new Vector3(1.0f, 2.40f, 3.0f);
            bRoot.transform.position = balcPos;

            // Solid Balcony Slab & Tower Shaft down to foundation (Zero floating!)
            CreateBlock(bRoot.transform, new Vector3(0f, -0.2f, 0f), new Vector3(1.4f, 0.4f, 1.4f), wallPeachMat, "Balcony_Slab");
            CreateBlock(bRoot.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.44f, 0.04f, 1.44f), trimMintMat, "Balcony_Trim");
            CreateBlock(bRoot.transform, new Vector3(0f, -1.40f, 0f), new Vector3(1.30f, 2.00f, 1.30f), wallPeachMat, "Balcony_Tower_Shaft");
            CreateBlock(bRoot.transform, new Vector3(0f, -2.60f, 0f), new Vector3(1.60f, 0.80f, 1.60f), foundationSandMat, "Balcony_Plinth_Solid");

            // Mint Balustrades
            CreateBlock(bRoot.transform, new Vector3(-0.65f, 0.25f, 0f), new Vector3(0.08f, 0.45f, 1.35f), trimMintMat, "Rail_L");
            CreateBlock(bRoot.transform, new Vector3(0f, 0.25f, 0.65f), new Vector3(1.35f, 0.45f, 0.08f), trimMintMat, "Rail_B");

            // Domed Canopy
            CreateBlock(bRoot.transform, new Vector3(0.50f, 0.65f, 0.50f), new Vector3(0.10f, 0.90f, 0.10f), trimMintMat, "Canopy_Col_R");
            CreateBlock(bRoot.transform, new Vector3(-0.50f, 0.65f, 0.50f), new Vector3(0.10f, 0.90f, 0.10f), trimMintMat, "Canopy_Col_L");
            CreateBlock(bRoot.transform, new Vector3(0f, 1.15f, 0.50f), new Vector3(1.1f, 0.12f, 0.30f), trimMintMat, "Canopy_Lintel");
            CreateSphere(bRoot.transform, new Vector3(0f, 1.45f, 0.50f), new Vector3(0.65f, 0.50f, 0.65f), goldMat, "Canopy_Dome");

            var keyNode = CreateTile(bRoot.transform, new Vector3(0f, 0.05f, 0f), "Tile_Key_Balcony");
            keyBalconyNode = keyNode;

            // Sacred Golden Key Item
            GameObject keyRoot = new GameObject("Sacred_Key_Item");
            keyRoot.transform.SetParent(bRoot.transform, false);
            keyRoot.transform.localPosition = new Vector3(0f, 0.70f, 0f);

            GameObject keyVisual = new GameObject("Key_Visual");
            keyVisual.transform.SetParent(keyRoot.transform, false);
            CreateBlock(keyVisual.transform, Vector3.zero, new Vector3(0.30f, 0.30f, 0.30f), goldMat, "Key_Core1", Quaternion.Euler(45f, 45f, 0f));
            CreateBlock(keyVisual.transform, Vector3.zero, new Vector3(0.30f, 0.30f, 0.30f), goldMat, "Key_Core2", Quaternion.Euler(0f, 45f, 45f));
            CreateCylinder(keyVisual.transform, new Vector3(0, 0.18f, 0), new Vector3(0.22f, 0.05f, 0.22f), goldMat, "Key_Halo");

            GameObject kLightObj = new GameObject("Key_Aura_Light");
            kLightObj.transform.SetParent(keyRoot.transform, false);
            var kLight = kLightObj.AddComponent<Light>();
            kLight.type = LightType.Point;
            kLight.color = new Color(1.0f, 0.92f, 0.45f);
            kLight.intensity = 3.2f;
            kLight.range = 4.0f;

            var keyComp = keyRoot.AddComponent<KeyItem>();
            keyComp.Initialize(keyNode);
            SetPrivateField(keyComp, "visualModel", keyVisual);
        }

        private void BuildRotaryGearBridge(Transform parent, PathNode leftMount, PathNode rightMount, PathNode keyBalconyNode) {
            GameObject bridgeRoot = new GameObject("Rotary_Gear_Bridge_Mechanism");
            bridgeRoot.transform.SetParent(parent, false);

            // Pivot of bridge is at Left Tower Terrace (-1.0, 2.4, 1.0)
            Vector3 pivotWorldPos = new Vector3(-1.0f, 2.40f, 1.0f);
            bridgeRoot.transform.position = pivotWorldPos;

            // Solid Supporting Arch Pier under the bridge span
            CreateBlock(parent, new Vector3(0f, 1.20f, 0f), new Vector3(1.8f, 2.40f, 1.8f), wallPeachMat, "Bridge_Support_Pier");

            GameObject pivotObj = new GameObject("Bridge_Pivot");
            pivotObj.transform.SetParent(bridgeRoot.transform, false);
            pivotObj.transform.localPosition = Vector3.zero;

            // Bridge arm spans along (2.0, 0, -2.0) towards Right Tower at 0°!
            // In local space of pivot:
            // Beam length = 2.83m. We orient the beam along local X by rotating the beam root.
            GameObject beamVisual = new GameObject("Bridge_Beam_Visual");
            beamVisual.transform.SetParent(pivotObj.transform, false);
            // Default span points towards (1, 0, -1) which is yaw -45°
            beamVisual.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);

            float beamLen = 2.80f;
            // Beam is centered at half length along X
            CreateBlock(beamVisual.transform, new Vector3(beamLen * 0.5f, -0.15f, 0f), new Vector3(beamLen, 0.30f, 0.85f), wallPeachMat, "Bridge_Beam");
            CreateBlock(beamVisual.transform, new Vector3(beamLen * 0.5f, 0.02f, 0f), new Vector3(beamLen + 0.04f, 0.04f, 0.89f), trimMintMat, "Bridge_Beam_Trim");

            // Walkable PathNodes along the beam in local beamVisual space:
            var bNode1 = CreateTile(beamVisual.transform, new Vector3(beamLen * 0.28f, 0.05f, 0f), "Bridge_Node_1");
            var bNode2 = CreateTile(beamVisual.transform, new Vector3(beamLen * 0.52f, 0.05f, 0f), "Bridge_Node_2");
            var bNode3 = CreateTile(beamVisual.transform, new Vector3(beamLen * 0.78f, 0.05f, 0f), "Bridge_Node_3");
            bNode1.AddNeighbor(bNode2); bNode2.AddNeighbor(bNode1);
            bNode2.AddNeighbor(bNode3); bNode3.AddNeighbor(bNode2);

            List<PathNode> bridgeNodes = new List<PathNode> { bNode1, bNode2, bNode3 };

            // Central Brass Rotary Gear Wheel mounted on the front face of the beam
            GameObject gearObj = new GameObject("Rotary_Gear_Wheel");
            gearObj.transform.SetParent(beamVisual.transform, false);
            gearObj.transform.localPosition = new Vector3(beamLen * 0.52f, -0.15f, -0.48f);

            CreateCylinder(gearObj.transform, Vector3.zero, new Vector3(0.55f, 0.16f, 0.55f), goldMat, "Gear_Hub", Quaternion.Euler(90f, 0f, 0f));
            CreateCylinder(gearObj.transform, new Vector3(0, 0, -0.06f), new Vector3(0.24f, 0.18f, 0.24f), trimMintMat, "Gear_Inner_Ring", Quaternion.Euler(90f, 0f, 0f));

            int cogs = 8;
            float r = 0.32f;
            for (int c = 0; c < cogs; c++) {
                float a = c * (360f / cogs) * Mathf.Deg2Rad;
                Vector3 cogPos = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                float cogRot = c * (360f / cogs);
                CreateBlock(gearObj.transform, cogPos, new Vector3(0.10f, 0.10f, 0.16f), goldMat, $"Cog_{c}", Quaternion.Euler(0f, 0f, cogRot));
            }

            var boxCol = gearObj.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(0.9f, 0.9f, 0.4f);

            var controller = bridgeRoot.AddComponent<RotaryBridgeController>();
            controller.SetupBridge(pivotObj.transform, gearObj.transform, bridgeNodes, leftMount, rightMount, keyBalconyNode, 0f);
        }

        private void BuildSummitIllusionAndSanctuary(Transform parent, PathNode rightTerrace) {
            // 1. Right Citadel Walkway extending from right terrace towards the perspective seam
            GameObject walkwayRoot = new GameObject("Citadel_Right_Walkway");
            walkwayRoot.transform.SetParent(parent, false);

            var wNode1 = CreateTile(walkwayRoot.transform, new Vector3(1.0f, 2.40f, -0.2f), "Tile_Right_Walkway_1");
            var wNode2 = CreateTile(walkwayRoot.transform, new Vector3(1.0f, 2.40f, 0.6f), "Tile_Right_Walkway_2_Seam");
            rightTerrace.AddNeighbor(wNode1); wNode1.AddNeighbor(rightTerrace);
            wNode1.AddNeighbor(wNode2); wNode2.AddNeighbor(wNode1);

            // Supporting solid wall under the walkway
            CreateBlock(walkwayRoot.transform, new Vector3(1.0f, 1.20f, 0.2f), new Vector3(0.85f, 2.40f, 1.8f), wallPeachMat, "Walkway_Solid_Wall");

            // =========================================================================
            // 2. High Summit Sanctuary displaced along the camera ray (Perspective Illusion!)
            // =========================================================================
            Quaternion illusionRot = Quaternion.Euler(camPitch, camYaw, 0f);
            Vector3 camForward = illusionRot * Vector3.forward;
            Vector3 illusionOffset = camForward * depthOffset;

            GameObject sRoot = new GameObject("High_Summit_Sanctuary_Illusion");
            sRoot.transform.SetParent(parent, false);
            sRoot.transform.position = illusionOffset;

            Transform st = sRoot.transform;

            // Visual position of the Summit Staircase Base is (0.2, 2.40, 0.6)
            // On screen at Yaw 45° Pitch 30°, (0.2, 2.40, 0.6) + illusionOffset appears DIRECTLY ADJACENT to (1.0, 2.40, 0.6)!
            Vector3 stairStartPos = new Vector3(0.2f, 2.40f, 0.6f);
            var nStairBase = CreateTile(st, stairStartPos, "Tile_Summit_Stair_Start");

            // 3. Perspective Bridge Edge connecting wNode2 (citadel) to nStairBase (summit)!
            GameObject edgeObj = new GameObject("Summit_Perspective_Edge");
            edgeObj.transform.SetParent(parent, false);
            var persEdge = edgeObj.AddComponent<PerspectiveEdge>();
            persEdge.Initialize(wNode2, nStairBase, camYaw, 3.5f);

            // 4. Grand Central Staircase climbing to the High Summit Platform
            Vector3 platformVisualPos = new Vector3(1.0f, 4.80f, 1.4f);
            float summitPlatformY = platformVisualPos.y;

            // Solid Monolithic Central Citadel Tower Shaft under the Summit
            // Sits solidly on the meadow ground (accounting for illusionOffset Y)
            float shaftHeight = summitPlatformY + 2.0f;
            CreateBlock(st, platformVisualPos + new Vector3(0, -shaftHeight * 0.5f, 0), new Vector3(2.6f, shaftHeight, 2.6f), wallPeachMat, "Summit_Tower_Shaft_Solid");
            CreateBlock(st, platformVisualPos + new Vector3(0, -0.10f, 0), new Vector3(2.8f, 0.20f, 2.8f), trimMintMat, "Summit_Shaft_Cornice");

            // Staircase steps
            int stairSteps = 5;
            PathNode prevStep = nStairBase;
            for (int s = 1; s <= stairSteps; s++) {
                float t = (float)s / stairSteps;
                Vector3 stepPos = Vector3.Lerp(stairStartPos + new Vector3(0, 0.1f, 0), platformVisualPos + new Vector3(-0.4f, 0.05f, -0.4f), t);
                var sNode = CreateStepTile(st, stepPos, $"Summit_Step_{s}");
                prevStep.AddNeighbor(sNode); sNode.AddNeighbor(prevStep);
                prevStep = sNode;
            }

            // 5. High Summit Goal Platform & Dais
            CreateBlock(st, platformVisualPos + new Vector3(0f, -0.20f, 0f), new Vector3(2.4f, 0.40f, 2.4f), wallPeachMat, "Summit_Platform_Base");
            CreateBlock(st, platformVisualPos + new Vector3(0f, 0.02f, 0f), new Vector3(2.44f, 0.04f, 2.44f), trimMintMat, "Summit_Platform_Trim");

            var goalTile = CreateCompleteLevelTile(st, platformVisualPos + new Vector3(0f, 0.05f, 0f), "Tile_Summit_Complete");
            prevStep.AddNeighbor(goalTile); goalTile.AddNeighbor(prevStep);

            // Flanking Ornamental Obelisks on Summit
            CreateObelisk(st, platformVisualPos + new Vector3(-0.95f, 0f, 0.95f), "Obelisk_Summit_L");
            CreateObelisk(st, platformVisualPos + new Vector3( 0.95f, 0f, -0.95f), "Obelisk_Summit_R");

            // 6. Radiant Golden Monumental Exit Portal Doorway & Cupola Dome
            GameObject portal = new GameObject("Summit_Exit_Portal");
            portal.transform.SetParent(st, false);
            portal.transform.position = platformVisualPos + new Vector3(0.45f, 0f, 0.45f);
            portal.transform.rotation = Quaternion.Euler(0f, -45f, 0f);

            CreateBlock(portal.transform, new Vector3(-0.55f, 1.1f, 0f), new Vector3(0.22f, 2.2f, 0.25f), trimMintMat, "Portal_Pillar_L");
            CreateBlock(portal.transform, new Vector3( 0.55f, 1.1f, 0f), new Vector3(0.22f, 2.2f, 0.25f), trimMintMat, "Portal_Pillar_R");
            CreateBlock(portal.transform, new Vector3(0f, 2.25f, 0f), new Vector3(1.32f, 0.20f, 0.28f), trimMintMat, "Portal_Lintel");

            // Radiant Sunlight Doorway Core
            CreateBlock(portal.transform, new Vector3(0f, 1.1f, 0.02f), new Vector3(0.88f, 2.05f, 0.08f), doorSunlightMat, "Portal_Sunlight_Core");

            // Golden Cupola Tower atop Portal
            float cupolaBaseY = 2.40f;
            CreateBlock(portal.transform, new Vector3(0f, cupolaBaseY + 0.35f, 0f), new Vector3(1.4f, 0.70f, 1.4f), wallPeachMat, "Cupola_Shaft");
            CreateBlock(portal.transform, new Vector3(0f, cupolaBaseY + 0.75f, 0f), new Vector3(1.5f, 0.12f, 1.5f), trimMintMat, "Cupola_Cornice");

            float sDomeY = cupolaBaseY + 0.82f;
            CreateSphere(portal.transform, new Vector3(0f, sDomeY + 0.55f, 0f), new Vector3(1.30f, 0.90f, 1.30f), goldMat, "Summit_Onion_Bulb");
            CreateCylinder(portal.transform, new Vector3(0f, sDomeY + 1.10f, 0f), new Vector3(0.50f, 0.45f, 0.50f), goldMat, "Summit_Onion_Taper");
            CreateCylinder(portal.transform, new Vector3(0f, sDomeY + 1.50f, 0f), new Vector3(0.10f, 0.55f, 0.10f), goldMat, "Summit_Needle_Spire");
            CreateSphere(portal.transform, new Vector3(0f, sDomeY + 1.82f, 0f), new Vector3(0.24f, 0.24f, 0.24f), goldMat, "Summit_Finial_Ball");

            // Warm Radiant Light streaming from Portal
            GameObject pLightObj = new GameObject("Portal_Radiant_Light");
            pLightObj.transform.SetParent(portal.transform, false);
            pLightObj.transform.localPosition = new Vector3(0f, 1.2f, -0.6f);
            var pLight = pLightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.96f, 0.72f);
            pLight.intensity = 4.0f;
            pLight.range = 6.0f;

            // Wire ExitDoor & LevelGoal
            var exitDoor = portal.AddComponent<ExitDoor>();
            exitDoor.Initialize(goalTile);

            var goalComp = goalTile.gameObject.AddComponent<LevelGoal>();
            goalComp.Configure(goalTile, "Level 2 – The Rotary Citadel");
        }

        private void BuildPlayerCharacter(Transform parent, PathNode startNode) {
            GameObject playerObj = new GameObject("Player_Ida");
            playerObj.transform.SetParent(parent, false);
            playerObj.transform.position = startNode.WalkPosition;

            GameObject visualRoot = new GameObject("VisualRoot");
            visualRoot.transform.SetParent(playerObj.transform, false);
            visualRoot.transform.localPosition = Vector3.zero;

            // Princess Ida Stylized Silhouette
            GameObject dress = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dress.name = "Ida_Dress";
            dress.transform.SetParent(visualRoot.transform, false);
            dress.transform.localPosition = new Vector3(0, 0.35f, 0);
            dress.transform.localScale = new Vector3(0.28f, 0.35f, 0.28f);
            dress.GetComponent<Renderer>().sharedMaterial = idaDressMat;
            var dCol = dress.GetComponent<Collider>();
            if (dCol != null) DestroyImmediate(dCol);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Ida_Head";
            head.transform.SetParent(visualRoot.transform, false);
            head.transform.localPosition = new Vector3(0, 0.75f, 0);
            head.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
            head.GetComponent<Renderer>().sharedMaterial = idaSkinMat;
            var hCol = head.GetComponent<Collider>();
            if (hCol != null) DestroyImmediate(hCol);

            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "Ida_Hat";
            hat.transform.SetParent(visualRoot.transform, false);
            hat.transform.localPosition = new Vector3(0, 0.98f, 0);
            hat.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            hat.GetComponent<Renderer>().sharedMaterial = idaDressMat;
            var hatCol = hat.GetComponent<Collider>();
            if (hatCol != null) DestroyImmediate(hatCol);

            GameObject hatTip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hatTip.name = "Ida_Hat_Tip";
            hatTip.transform.SetParent(visualRoot.transform, false);
            hatTip.transform.localPosition = new Vector3(0, 1.20f, 0);
            hatTip.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            hatTip.GetComponent<Renderer>().sharedMaterial = idaDressMat;
            var htCol = hatTip.GetComponent<Collider>();
            if (htCol != null) DestroyImmediate(htCol);

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

        private void SetupCameraLightingAndManagers(Transform parent) {
            Camera mainCam = Camera.main;
            if (mainCam == null) {
                var camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            mainCam.orthographic = true;
            mainCam.orthographicSize = camOrthoSize;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = skyColor;

            var camData = mainCam.GetUniversalAdditionalCameraData();
            if (camData != null) {
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;
            }

            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.78f, 0.74f, 0.65f);

            var camController = mainCam.GetComponent<IsometricCameraController>();
            if (camController == null) {
                camController = mainCam.gameObject.AddComponent<IsometricCameraController>();
            }

            GameObject pivotObj = GameObject.Find("PuzzlePivot_L2");
            if (pivotObj == null) {
                pivotObj = new GameObject("PuzzlePivot_L2");
                pivotObj.transform.SetParent(parent, false);
            }
            pivotObj.transform.position = camPivotCenter;

            SetPrivateField(camController, "currentYaw", camYaw);
            SetPrivateField(camController, "targetYaw", camYaw);
            SetPrivateField(camController, "currentSnapIndex", 0);
            SetPrivateField(camController, "pitchAngle", camPitch);
            SetPrivateField(camController, "orthographicSize", camOrthoSize);
            SetPrivateField(camController, "minOrthoSize", camOrthoSize);
            SetPrivateField(camController, "targetVisibleWidth", 8.2f);
            SetPrivateField(camController, "pivotOffset", Vector3.zero);
            camController.SetPivotTarget(pivotObj.transform, Vector3.zero);
            camController.SetPortraitSettings(false, 8.2f, camOrthoSize);

            Quaternion camRot = Quaternion.Euler(camPitch, camYaw, 0f);
            mainCam.transform.rotation = camRot;
            mainCam.transform.position = camPivotCenter + (camRot * Vector3.back) * 25f;
            camController.UpdateCameraTransform();

            // Perspective Alignment Manager
            var alignMgr = mainCam.GetComponent<PerspectiveAlignmentManager>();
            if (alignMgr == null) {
                alignMgr = mainCam.gameObject.AddComponent<PerspectiveAlignmentManager>();
            }
            alignMgr.Configure(camYaw, 3.5f);

            // Interaction Handler
            var interactHandler = mainCam.GetComponent<PuzzleInteractionHandler>();
            if (interactHandler == null) {
                interactHandler = mainCam.gameObject.AddComponent<PuzzleInteractionHandler>();
            }

            // Directional Sunlight (Warm, sharp Monument Valley shadows)
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

            // Soft Fill Light
            GameObject fillObj = GameObject.Find("Fill_Light_L2");
            if (fillObj == null) {
                fillObj = new GameObject("Fill_Light_L2");
                fillObj.transform.SetParent(parent, false);
            }
            var fillLight = fillObj.GetComponent<Light>();
            if (fillLight == null) fillLight = fillObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.transform.rotation = Quaternion.Euler(-30f, 140f, 0f);
            fillLight.color = new Color(0.70f, 0.85f, 0.90f);
            fillLight.intensity = 0.55f;
            fillLight.shadows = LightShadows.None;

            // UI & Level Managers
            GameObject uiObj = GameObject.Find("GameUI");
            if (uiObj == null) {
                uiObj = new GameObject("GameUI");
                uiObj.transform.SetParent(parent, false);
            }
            var gameUI = uiObj.GetComponent<GameUI>();
            if (gameUI == null) gameUI = uiObj.AddComponent<GameUI>();

            var levelMgr = uiObj.GetComponent<LevelManager>();
            if (levelMgr == null) levelMgr = uiObj.AddComponent<LevelManager>();
            SetPrivateField(levelMgr, "levelName", "Level 2 – The Rotary Citadel");
            gameUI.Initialize(camController, levelMgr);
        }

        private void CreateObelisk(Transform parent, Vector3 worldPos, string name) {
            GameObject obelisk = new GameObject(name);
            obelisk.transform.SetParent(parent, false);
            obelisk.transform.position = worldPos;

            CreateBlock(obelisk.transform, new Vector3(0, 0.10f, 0), new Vector3(0.44f, 0.20f, 0.44f), trimMintMat, "Base");
            CreateBlock(obelisk.transform, new Vector3(0, 0.65f, 0), new Vector3(0.30f, 0.90f, 0.30f), wallPeachMat, "Shaft");
            CreateSphere(obelisk.transform, new Vector3(0, 1.20f, 0), new Vector3(0.28f, 0.28f, 0.28f), goldMat, "Finial");
        }

        private PathNode CreateTile(Transform parent, Vector3 localPos, string name) {
            GameObject tileObj = new GameObject(name);
            tileObj.transform.SetParent(parent, false);
            tileObj.transform.localPosition = localPos;

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Tile_Slab";
            slab.transform.SetParent(tileObj.transform, false);
            slab.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            slab.transform.localScale = new Vector3(0.72f, 0.08f, 0.72f);

            var sCol = slab.GetComponent<Collider>();
            if (sCol != null) DestroyImmediate(sCol);

            var rend = slab.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = pathIvoryMat;

            CreateBlock(tileObj.transform, new Vector3(0f, -0.01f, 0f), new Vector3(0.76f, 0.04f, 0.76f), trimMintMat, "Tile_Border");

            var box = tileObj.AddComponent<BoxCollider>();
            box.size = new Vector3(0.80f, 0.20f, 0.80f);
            box.center = new Vector3(0f, 0.04f, 0f);

            var node = tileObj.AddComponent<PathNode>();
            SetPrivateField(node, "standingOffset", new Vector3(0f, 0.10f, 0f));
            SetPrivateField(node, "tileRenderer", rend);
            SetPrivateField(node, "normalColor", Color.white);
            return node;
        }

        private PathNode CreateStepTile(Transform parent, Vector3 localPos, string name) {
            GameObject stepObj = new GameObject(name);
            stepObj.transform.SetParent(parent, false);
            stepObj.transform.localPosition = localPos;

            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "Step_Tread";
            slab.transform.SetParent(stepObj.transform, false);
            slab.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            slab.transform.localScale = new Vector3(0.65f, 0.06f, 0.65f);

            var sCol = slab.GetComponent<Collider>();
            if (sCol != null) DestroyImmediate(sCol);

            var rend = slab.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = pathIvoryMat;

            var box = stepObj.AddComponent<BoxCollider>();
            box.size = new Vector3(0.70f, 0.15f, 0.70f);
            box.center = new Vector3(0f, 0.02f, 0f);

            var node = stepObj.AddComponent<PathNode>();
            SetPrivateField(node, "standingOffset", new Vector3(0f, 0.08f, 0f));
            SetPrivateField(node, "tileRenderer", rend);
            SetPrivateField(node, "normalColor", Color.white);
            return node;
        }

        private PathNode CreateCompleteLevelTile(Transform parent, Vector3 localPos, string name) {
            var node = CreateTile(parent, localPos, name);

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Goal_Ring";
            ring.transform.SetParent(node.transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.082f, 0f);
            ring.transform.localScale = new Vector3(0.60f, 0.015f, 0.60f);
            ring.GetComponent<Renderer>().sharedMaterial = goldMat;
            var rCol = ring.GetComponent<Collider>();
            if (rCol != null) DestroyImmediate(rCol);

            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Goal_Eye";
            eye.transform.SetParent(node.transform, false);
            eye.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            eye.transform.localScale = new Vector3(0.24f, 0.04f, 0.24f);
            eye.GetComponent<Renderer>().sharedMaterial = goldMat;
            var eCol = eye.GetComponent<Collider>();
            if (eCol != null) DestroyImmediate(eCol);

            return node;
        }

        private GameObject CreateBlock(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name, Quaternion? localRot = null) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (localRot.HasValue) go.transform.localRotation = localRot.Value;

            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
            return go;
        }

        private GameObject CreateCylinder(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name, Quaternion? localRot = null) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (localRot.HasValue) go.transform.localRotation = localRot.Value;

            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
            return go;
        }

        private GameObject CreateSphere(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, string name) {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var col = go.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);

            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
            return go;
        }

        private void SetPrivateField(object target, string fieldName, object value) {
            if (target == null) return;
            var type = target.GetType();
            while (type != null) {
                var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (field != null) {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
        }
    }
}
