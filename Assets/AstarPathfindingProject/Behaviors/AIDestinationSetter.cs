using UnityEngine;
using System.Collections;

namespace Pathfinding {
    [UniqueComponent(tag = "ai.destination")]
    [HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_a_i_destination_setter.php")]
    public class AIDestinationSetter : VersionedMonoBehaviour {
        
        [Tooltip("The parent object to scan for a child tagged 'Player'")]
        public Transform parentToScan; 
        
        public Transform target;
        IAstarAI ai;

        void OnEnable () {
            ai = GetComponent<IAstarAI>();
            if (ai != null) ai.onSearchPath += Update;
        }

        void OnDisable () {
            if (ai != null) ai.onSearchPath -= Update;
        }

        void Update () {
            // 1. If we don't have a target yet, keep scanning the parent for it
            if (target == null && parentToScan != null) {
                FindPlayerChild();
            }

            // 2. If we DO have a target, update the AI's destination
            if (target != null && ai != null) {
                ai.destination = target.position;
            }
        }

        void FindPlayerChild() {
            // Look through every child attached to the parent
            foreach (Transform child in parentToScan.GetComponentsInChildren<Transform>()) {
                if (child.CompareTag("Player")) {
                    target = child; // Assigns it so you can see it in the Inspector
                    
                    // Prints a message in the Unity Console so you know exactly what it grabbed
                    Debug.Log("AIDestinationSetter: Target acquired! Now chasing -> " + target.name);
                    break; // Stop searching once we find it
                }
            }
        }
    }
}