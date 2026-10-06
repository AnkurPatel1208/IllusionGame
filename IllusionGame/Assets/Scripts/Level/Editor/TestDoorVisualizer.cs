#if UNITY_EDITOR
using System.IO;
using Gameplay;
using Level;
using UnityEditor;
using UnityEngine;

namespace EditorTools {
    public static class TestDoorVisualizer {
        [MenuItem("Illusion Game/Capture Door Open Visuals")]
        public static void CaptureDoorOpenVisuals() {
            var doorHinge = GameObject.Find("Door_Left_Hinge");
            var openPortal = GameObject.Find("OpenPortal_Glow") ?? GameObject.Find("OpenPortal");
            var doorLight = GameObject.Find("DoorLight");
            
            if (doorHinge != null) {
                // Set door to open position (90 deg outward around Y)
                doorHinge.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            if (openPortal != null) {
                openPortal.SetActive(true);
            }
            if (doorLight != null) {
                var l = doorLight.GetComponent<Light>();
                if (l != null) l.intensity = 2.5f;
            }

            // Capture screenshots
            Level1EditorMenu.CaptureScreenshot();

            // Save copies with specific names
            if (File.Exists("screenshot_landscape.png")) {
                File.Copy("screenshot_landscape.png", "door_open_outward_landscape.png", true);
            }
            if (File.Exists("screenshot_level1.png")) {
                File.Copy("screenshot_level1.png", "door_open_outward_portrait.png", true);
            }
            if (File.Exists("screenshot_rotated.png")) {
                File.Copy("screenshot_rotated.png", "door_open_outward_rotated.png", true);
            }

            // Restore door to closed
            if (doorHinge != null) {
                doorHinge.transform.localRotation = Quaternion.identity;
            }
            if (openPortal != null) {
                openPortal.SetActive(false);
            }
            if (doorLight != null) {
                var l = doorLight.GetComponent<Light>();
                if (l != null) l.intensity = 0f;
            }

            Debug.Log("<color=green>[TestDoorVisualizer]</color> Captured door open outward screenshots successfully!");
        }
    }
}
#endif
