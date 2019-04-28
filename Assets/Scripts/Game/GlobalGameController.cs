﻿using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GlobalGameController : MonoBehaviour {
    public static GlobalGameController globalInstance {
        get;
        private set;
    }

    public HumanPartDefinition[] bodyPartDatabase;
    public RoboticPartDefinition[] roboticPartDatabase;

    public List<BodyPartGlobalState> currentPlayerBodyState = new List<BodyPartGlobalState>(10); // probably should be a hash set

    public BodyPartGlobalState currentlyOperatingBodyPart;
    public GameObject currentPlayer;

    public List<CanGetHurtTile> currentGoals = new List<CanGetHurtTile>(10);
    public float currentMoney = 0;
    public float currentBlood;
    public float currentImunity;

    private GameObject bloodText;
    private GameObject imunityText;

    public HumanPartDefinition FindHumanPartDefinition(string partName) {
        return this
            .bodyPartDatabase
            .FirstOrDefault(
                part => part.bodyPartName == partName
            );
    }

    public RoboticPartDefinition FindRoboticPartDefinition(string partName) {
        return this
            .roboticPartDatabase
            .FirstOrDefault(
                part => part.bodyPartName == partName
            );
    }

    public void StartOperation() {
        var currentlySelectedPart = PartHighlighter.instance.selectedBodyPart;

        if (currentlySelectedPart.currentType == BodyPartType.Robotic) return;

        this.currentlyOperatingBodyPart = this
            .currentPlayerBodyState
            .FirstOrDefault(part => part.bodyPartName == currentlySelectedPart.partName);

        var definition = this.FindHumanPartDefinition(currentlySelectedPart.partName);
        if (definition.bodyPartName != default(HumanPartDefinition).bodyPartName) {
            this.currentBlood = definition.blood;
            this.currentImunity = definition.imunity;
        }

        switch (currentlySelectedPart.partName) {
            case "Upper Left Arm":
            case "Lower Left Arm":
            case "Upper Right Arm":
            case "Lower Right Arm":
                this.MoveIntoOperationScene("ArmOperation");
                break;

            case "Eyes":
                this.MoveIntoOperationScene("EyeOperation");
                break;

            case "Head":
            case "Neck":
                this.MoveIntoOperationScene("HeadOperation");
                break;

            case "Torso":
                this.MoveIntoOperationScene("TorsoOperation");
                break;

            case "Right Leg":
            case "Left Leg":
                this.MoveIntoOperationScene("LegOperation");
                break;


            default:
                Debug.LogWarning("Operation level not defined for body part " + currentlySelectedPart.partName);
                break;
        }
    }

    public void KillPlayer() {
        if (!this.currentPlayer) return;

        Debug.Log("Killing the player");

        ParticleController.instance.SpawnKillParticles(this.currentPlayer.transform.position, 5);
        Destroy(this.currentPlayer);
    }

    public void CompleteGoal(CanGetHurtTile goal) {
        if (!goal.isGoal) return;

        this.currentGoals.Remove(goal);

        Debug.Log("Goal completed " + goal.gameObject.name);
        Debug.Log("Remaining goals " + this.currentGoals.Count);

        if (this.currentGoals.Count == 0) {
            this.MoveIntoBodyPartSelectScene();
        }
    }

    public void Awake() {
        if (!this.InitializeInstance()) return;

        SceneManager.activeSceneChanged += this.OnSceneLoad;
    }

    public void Update() {
        if (this.bloodText) {
            this.bloodText.GetComponent<Text>().text = "Remaining blood: " + Mathf.Round(this.currentBlood * 100f) / 100f;
        }

        if (this.imunityText) {
            this.imunityText.GetComponent<Text>().text = "Imunity: " + Mathf.Round(this.currentImunity * 100f) / 100f;
        }

        if (this.currentPlayer && this.currentBlood <= 0) {
            this.KillPlayer();
        }
    }

    private bool InitializeInstance() {
        if (!GlobalGameController.globalInstance) {
            GlobalGameController.globalInstance = this;
        } else  {
            Destroy(this.gameObject);
            return false;
        }

        DontDestroyOnLoad(this.gameObject);
        return true;
    }

    private void MoveIntoBodyPartSelectScene() {
        var wasSuccessfull = this.currentGoals.Count == 0;

        this.currentGoals.Clear();
        this.currentPlayer = null;

        if (this.currentlyOperatingBodyPart.bodyPartName != default(BodyPartGlobalState).bodyPartName) {
            if (wasSuccessfull) {
                if (this.currentlyOperatingBodyPart.currentType == BodyPartType.Missing) {
                    Debug.LogError("IMPLEMENT ME");
                } else if (this.currentlyOperatingBodyPart.currentType == BodyPartType.Human) {
                    try {
                        var index = this.currentPlayerBodyState.FindIndex(part => part.bodyPartName == this.currentlyOperatingBodyPart.bodyPartName);
                        var partState = this.currentPlayerBodyState[index];
                        
                        this.currentPlayerBodyState.RemoveAt(index);

                        partState.currentType = BodyPartType.Robotic;
                        this.currentPlayerBodyState.Add(partState);

                        this.currentMoney += this.FindHumanPartDefinition(this.currentlyOperatingBodyPart.bodyPartName).price;
                    }
                    catch {
                        Debug.Log("Missing body part state " + this.currentlyOperatingBodyPart.bodyPartName);
                    }
                }
            }
        }

        SceneManager.LoadScene("BodyPartSelectScene");
    }

    private void MoveIntoOperationScene(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoad(Scene _, Scene loadedScene) {
        this.currentPlayer = GameObject.FindWithTag("Player");
        this.bloodText = GameObject.Find("BloodText");
        this.imunityText = GameObject.Find("ImunityText");

        var money = GameObject.FindWithTag("MoneyCounter");
        if (money) {
            Debug.Log("Setting money text to " + this.currentMoney);
            money.GetComponent<Text>().text = "$" + Mathf.Round(this.currentMoney * 100) / 100f;
        }
    }
}
