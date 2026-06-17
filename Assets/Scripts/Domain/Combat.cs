using System.Collections.Generic;
using UnityEngine;

public class Combat {
    List<string> enemies;
    List<string> weapons;
    List<string> portraits;
    List<string> tutorials;
    string nextDialogue;
    int round;
    bool isSolo = false;
    string soloArguments = "";
    string companion = "none";

    public Combat(string combatFile) {
        TextAsset combat = Resources.Load<TextAsset>("Combats/" + combatFile);
        List<string> lines = new List<string>(combat.text.Split('\n'));
        enemies = new List<string>();
        weapons = new List<string>();
        portraits = new List<string>();
        tutorials = new List<string>();

        foreach (string line in lines) {
            if (line.Trim().Length == 0) {
                continue;
            }

            string[] lineParts = line.Split(':');
            if (lineParts[0] == "enemy")
            {
                string enemyText = lineParts[1].Trim();
                string[] legacyParts = enemyText.Split(' ');
                int legacyEnemyId;
                if (legacyParts.Length > 1 && int.TryParse(legacyParts[0], out legacyEnemyId)) {
                    enemies.Add(legacyParts[0].Trim());
                    weapons.Add(legacyParts[1].Trim());
                    portraits.Add(legacyParts.Length > 2 ? legacyParts[2].Trim() : "");
                }
                else {
                    string[] enemyNames = enemyText.Split(',');
                    foreach (string name in enemyNames)
                    {
                        enemies.Add(name.Trim());
                        portraits.Add("");
                    }
                }
            }
            else if (lineParts[0] == "weapons") {
                string[] weaponNames = lineParts[1].Split(',');
                foreach (string name in weaponNames)
                {
                    weapons.Add(name.Trim());
                }
            }
            else if (lineParts[0] == "dialogue")
            {
                nextDialogue = lineParts[1].Trim();
            }
            else if (lineParts[0] == "solo")
            {
                isSolo = true;
                soloArguments = lineParts[1];
            }
            else if (lineParts[0] == "companion")
            {
                companion = lineParts[1].Trim();
            }
            else if (lineParts[0] == "tutorial")
            {
                tutorials.Add(lineParts[1].Trim());
            }
        }
        round = 0;
    }

    public string getEnemy(int round) {
        if (round >= enemies.Count) {
            return "";
        }
        else {
            return enemies[round];
        }
    }
    public string getWeapon(int round) {
        if (round >= weapons.Count) {
            return "";
        }
        else {
            return weapons[round];
        }
    }

    public string getPortrait(int round) {
        if (round >= enemies.Count) {
            return "";
        }
        else {
            return portraits[round];
        }
    }

    public void advanceRound() {
        round++;
    }

    public int getRound() {
        return round;
    }

    public bool getIsSolo() {
        return isSolo;
    }

    public string getSoloArguments() {
        return soloArguments;
    }

    public string getNextDialogue() {
        return nextDialogue;
    }

    public int getTotalRounds() {
        if (!isSolo) {
            return enemies.Count;
        }
        else {
            return 1;
        }
    }

    public string getCompanion() {
        return companion;
    }

    public void removeCompanion() {
        companion = "none";
    }

    public List<string> getTutorials() {
        return tutorials;
    }

}
