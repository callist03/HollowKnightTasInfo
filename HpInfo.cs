﻿using System;
using System.Collections.Generic;
using System.Text;
using GlobalEnums;
using UnityEngine;

namespace HollowKnightTasInfo {
    internal record HpData {
        private readonly GameObject gameObject;
        private readonly PlayMakerFSM fsm;
        private readonly int maxHp;
        private int Hp => fsm.FsmVariables.GetFsmInt("HP").Value;

        public HpData(GameObject gameObject, PlayMakerFSM fsm) {
            this.gameObject = gameObject;
            this.fsm = fsm;
            maxHp = Hp;
        }

        public override string ToString() {
            if (Camera.main == null || !gameObject.activeInHierarchy || Hp <= 0) {
                return string.Empty;
            }

            Vector2 enemyPos = Camera.main.WorldToScreenPoint(gameObject.transform.position);
            enemyPos.y = Screen.height - enemyPos.y;

            int x = (int) enemyPos.x;
            int y = (int) enemyPos.y;

            if (x < 0 || x > Screen.width || y < 0 || y > Screen.height) {
                return string.Empty;
            }

            return $"{x},{y},{Hp}/{maxHp}";
        }
    }

    public static class HpInfo {
        private static bool init;
        private static readonly Dictionary<GameObject, HpData> EnemyPool = new();

        public static void Init(GameManager gameManager) {
            if (init) {
                return;
            }

            init = true;

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += (scene, nextScene) => {
                EnemyPool.Clear();

                if (gameManager.IsNonGameplayScene()) {
                    return;
                }

                try {
                    GameObject[] rootGameObjects = nextScene.GetRootGameObjects();
                    foreach (GameObject gameObject in rootGameObjects) {
                        TryAddEnemy(gameObject);
                    }
                } catch (Exception e) {
                    Debug.Log(e);
                }
            };
        }

        private static void TryAddEnemy(GameObject gameObject) {
            if (!EnemyPool.ContainsKey(gameObject)
                && ((PhysLayers) gameObject.layer is PhysLayers.ENEMIES or PhysLayers.HERO_ATTACK || gameObject.tag == "Boss")
                && !IgnoreObject(gameObject)) {
                PlayMakerFSM playMakerFsm = FSMUtility.LocateFSM(gameObject, "health_manager_enemy");
                if (playMakerFsm == null) {
                    playMakerFsm = FSMUtility.LocateFSM(gameObject, "health_manager");
                }

                if (playMakerFsm != null) {
                    EnemyPool.Add(gameObject, new HpData(gameObject, playMakerFsm));
                }
            }

            foreach (Transform childTransform in gameObject.transform) {
                TryAddEnemy(childTransform.gameObject);
            }
        }

        private static bool IgnoreObject(GameObject gameObject) {
            string name = gameObject.name;
            if (name.IndexOf("Hornet Barb", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Needle Tink", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("worm", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Laser Turret", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Deep Spikes", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        private static string GetInfo() {
            StringBuilder result = new();

            foreach (HpData hpData in EnemyPool.Values) {
                string hpInfo = hpData.ToString();
                if (string.IsNullOrEmpty(hpInfo)) {
                    continue;
                }

                result.Append(result.Length > 0 ? $",{hpData}" : $"HP:{hpData}");
            }

            return result.ToString();
        }

        public static void TryAppendHpInfo(GameManager gameManager, StringBuilder infoBuilder) {
            if (gameManager.gameState == GameState.PLAYING) {
                try {
                    string hpInfo = GetInfo();
                    if (!string.IsNullOrEmpty(hpInfo)) {
                        infoBuilder.AppendLine(hpInfo);
                    }
                } catch (Exception e) {
                    Debug.Log(e);
                }
            }
        }
    }
}