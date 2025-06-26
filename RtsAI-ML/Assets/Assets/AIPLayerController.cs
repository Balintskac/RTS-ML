using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;

public class AIPLayerController : Agent
{
    Dictionary<string, Dictionary<string, int>> Building = new Dictionary<string, Dictionary<string, int>>() {
       { "house",new Dictionary<string,int>(){ { "gold", 10 }, { "wood", 20 },  } },
       { "hunter",new Dictionary<string,int>(){ { "gold", 20 }, { "wood", 30 },  } },
       { "hero",new Dictionary<string,int>(){ { "gold", 50 }, { "wood", 50 }, } },
       { "main",new Dictionary<string,int>(){ { "gold", 100 }, { "wood", 100 }, } },
    };

    Dictionary<string, Dictionary<string, int>> units = new Dictionary<string, Dictionary<string, int>>() {
       { "worker",new Dictionary<string,int>(){ { "gold", 30 }, { "wood", 0 } } },
       { "hunter",new Dictionary<string,int>(){ { "gold", 25 }, { "wood", 15 } } },
       { "archer",new Dictionary<string,int>(){ { "gold", 20 }, { "wood", 25 } } },
       { "hero",new Dictionary<string,int>(){ { "gold", 20 }, { "wood", 30 } } },
       { "unit_lvl",new Dictionary<string,int>(){ { "gold", 100 }, { "wood", 50 } } },
    };

    Dictionary<string, Dictionary<string, int>> strategy = new Dictionary<string, Dictionary<string, int>>() {
       { "big",new Dictionary<string,int>(){ { "worker", 5}, { "hunter", 1 }, { "archer", 6 }, { "hero", 0 }, {"gold", 50 }, { "wood", 50 }, { "defense", 2 } } },
       { "fast",new Dictionary<string,int>(){ { "worker", 3}, { "hunter", 3 }, { "archer", 0 }, { "hero", 1 }, { "gold", 30 }, { "wood", 30 }, { "defense", 1 } } },
       { "strong",new Dictionary<string,int>(){ { "worker", 5}, { "hunter", 6 } , { "archer", 6 }, { "hero", 1 }, { "gold", 75 }, { "wood", 75 }, { "defense", 4 } }  },
    };


    public Dictionary<string, int> money;
    public float counter;
    public GameObject unit;
    public GameObject buildingUnit;
    public GameObject building;
    public List<GameObject> buildings;
    public List<GameObject> unitsAll;
    public int randomChoice;
    public GameObject[] heatmaps;
    public int heroLvl;
    public bool unitLvl;
    public int amountMax = 5;
    public HeatMapCounter heatMapCounter;
    public string AIStrategy;
    public int playerGoldMine;
    public float attackTimer;
    public float buildNewMainTimer;
    public bool type;
    public string enemyTag;
    public string AITag;
    public List<HeatMap> lastMatches = new List<HeatMap>();

    void Start()
    {
        // Time.timeScale = 2;
        attackTimer = 30f;
        buildNewMainTimer = 300f;
        counter = 3f;
        randomChoice = Random.Range(1, 4);
        money = new Dictionary<string, int>(){
            { "gold", 100 },
            { "wood", 100 }
        };

        if (type)
        {
            enemyTag = "enemy ";
            AITag = "";
        }
        else
        {
            enemyTag = "";
            AITag = "enemy ";
        }

        heatMapCounter = GameObject.Find("Terrain").GetComponent<HeatMapCounter>();

        int random = Random.Range(1, 3);

        if (random == 1)
        {
            AIStrategy = "big";
        }
        else if (random == 2)
        {
            AIStrategy = "fast";
        }
        else
        {
            AIStrategy = "strong";
        }
    }

    void Update()
    {
        #region COUNTER & RANDOM VALUE INIT
        if (counter > 0)
        {
            counter -= Time.deltaTime;
        }
        else
        {
            randomChoice = Random.Range(1, 4);
            counter = Random.Range(10f, 20f);
        
        #endregion

            #region MAIN BUILDING SPAWN
            if (buildNewMainTimer > 0)
            {
                buildNewMainTimer -= Time.deltaTime;
            }
            else
            {
                buildNewMainTimer = Random.Range(240, 360);
            }

            if (GameObject.FindGameObjectsWithTag(AITag + "main building").Length == 0 && buildNewMainTimer < 1)
            {
                List<HeatMap> goldMine = heatMapCounter.goldMines;
                bool emptyGold = true;

                foreach (var g in goldMine)
                {
                    foreach (var r in GameObject.FindGameObjectsWithTag(AITag + "worker"))
                    {
                        if (Vector3.Distance(new Vector3(g.x + 25, 0, g.y + 25), r.gameObject.transform.position) < 50)
                        {
                            emptyGold = false;
                        }
                    }

                    foreach (var r in GameObject.FindGameObjectsWithTag("main building"))
                    {
                        if (Vector3.Distance(new Vector3(g.x + 25, 0, g.y + 25), r.gameObject.transform.position) < 50)
                        {
                            emptyGold = false;
                        }
                    }

                    if (goldMine != null && buyItem(Building["main"]) && emptyGold)
                    {
                        Instantiate(buildings.Find(x => x.tag == AITag + "main building"), new Vector3(g.x + 25, 0, g.y + 25), Quaternion.identity);
                    }
                }
            }
            #endregion

            #region HUNTER SPAWN
            if (strategy[AIStrategy]["hunter"] > GameObject.FindGameObjectsWithTag(AITag + "hunter").Length &&
         GameObject.FindGameObjectsWithTag(AITag + "hunter building").Length == 1)
            {
                GameObject building = findMainBuilding(AITag + "hunter building");
                building.GetComponent<BuildingUnit>().spawn = unitsAll.Find(x => x.tag == AITag + "hunter");
                building.GetComponent<BuildingUnit>().spawnCount++;
            }
            #endregion

            #region HUNTER BUILDING SPAWN

            if (strategy[AIStrategy]["worker"] > GameObject.FindGameObjectsWithTag(AITag + "worker").Length)
            {
                GameObject building = findMainBuilding(AITag + "main building");
                building.GetComponent<BuildingUnit>().spawn = unitsAll.Find(x => x.tag == AITag + "worker");
                building.GetComponent<BuildingUnit>().spawnCount++;
            }

            if (GameObject.FindGameObjectsWithTag(AITag + "hunter building").Length == 0)
            {
                GameObject newBuildings = findMainBuilding(AITag + "main building");
                if (newBuildings != null && buyItem(Building["hunter"]))
                {
                    Instantiate(buildings.Find(x => x.tag == AITag + "hunter building"), new Vector3(newBuildings.transform.position.x + 30, newBuildings.transform.position.y, newBuildings.transform.position.z + 10), Quaternion.identity);
                }
            }

            #endregion

            #region HUNTER STRATEGY 
            if (strategy[AIStrategy]["hunter"] <= GameObject.FindGameObjectsWithTag(AITag + "hunter").Length)
            {
                bool emptyGold = false;
                foreach (var g in heatMapCounter.goldMines)
                {
                    emptyGold = false;

                    foreach (var r in GameObject.FindGameObjectsWithTag(AITag + "worker"))
                    {
                        if (Vector3.Distance(new Vector3(g.x + 25, 0, g.y + 25), r.gameObject.transform.position) < 50)
                        {
                            emptyGold = true;
                        }
                    }

                    foreach (var r in GameObject.FindGameObjectsWithTag(enemyTag + "worker"))
                    {
                        if (Vector3.Distance(new Vector3(g.x + 25, 0, g.y + 25), r.gameObject.transform.position) < 50)
                        {
                            emptyGold = true;
                        }
                    }

                    foreach (var r in GameObject.FindGameObjectsWithTag("random enemy"))
                    {
                        if (Vector3.Distance(new Vector3(g.x + 25, 0, g.y + 25), r.gameObject.transform.position) < 50)
                        {
                            emptyGold = true;
                        }
                    }
                }

                GameObject[] enemyHunter = GameObject.FindGameObjectsWithTag(AITag + "hunter");
                if (emptyGold)
                {
                    foreach (var g in GameObject.FindGameObjectsWithTag("random enemy"))
                    {

                        foreach (var h in enemyHunter)
                        {
                            if (Vector3.Distance(g.gameObject.transform.position, h.transform.position) < 300)
                            {
                                if (!h.GetComponent<UnitController>().enemyfound)
                                {
                                    h.GetComponent<UnitController>().agent.SetDestination(new Vector3(g.gameObject.transform.position.x, g.gameObject.transform.position.y, g.gameObject.transform.position.z));

                                }
                            }
                        }
                    }
                }

                /*
                if (true)
                {
                    foreach (var g in GameObject.FindGameObjectsWithTag(enemyTag + "main building"))
                    {

                        foreach (var h in enemyHunters)
                        {
                            if (Vector3.Distance(g.gameObject.transform.position, h.transform.position) < 500)
                            {
                                if (!h.GetComponent<UnitController>().enemyfound)
                                {
                                    h.GetComponent<UnitController>().agent.SetDestination(new Vector3(g.gameObject.transform.position.x, g.gameObject.transform.position.y, g.gameObject.transform.position.z));

                                }
                            }
                        }
                    }

                    foreach (var g in GameObject.FindGameObjectsWithTag(enemyTag + "hunter building"))
                    {

                        foreach (var h in enemyHunters)
                        {
                            if (Vector3.Distance(g.gameObject.transform.position, h.transform.position) < 500)
                            {
                                if (!h.GetComponent<UnitController>().enemyfound)
                                {
                                    h.GetComponent<UnitController>().agent.SetDestination(new Vector3(g.gameObject.transform.position.x, g.gameObject.transform.position.y, g.gameObject.transform.position.z));

                                }
                            }
                        }
                    }
                }*/
            }
            #endregion

            #region DEFENSE
            GameObject newBuilding = findMainBuilding(AITag + "main building");
            GameObject[] enemyHunters = GameObject.FindGameObjectsWithTag(AITag + "hunter");
            if (checkEnemyAttack(newBuilding))
            {
                foreach (var h in enemyHunters)
                {
                    if (!h.GetComponent<UnitController>().enemyfound)
                    {
                        h.GetComponent<UnitController>().agent.SetDestination(newBuilding.transform.position);
                    }
                }

            }
            #endregion

            #region WORKER GET TO WORK
            foreach (var w in GameObject.FindGameObjectsWithTag(AITag + "worker"))
            {
                if (w.GetComponent<WorkingHandler>().target == "")
                {
                    foreach (var g in GameObject.FindGameObjectsWithTag("gold mine"))
                    {
                        if (Vector3.Distance(g.gameObject.transform.position, w.transform.position) < 30)
                        {
                            w.GetComponent<WorkingHandler>().target = "gold mine";
                            w.GetComponent<UnitController>().agent.SetDestination(new Vector3(g.gameObject.transform.position.x, g.gameObject.transform.position.y, g.gameObject.transform.position.z));
                        }
                    }
                }
            }
            #endregion

            #region HEATMAP STRATEGY
            if (heatMapCounter.heatMapSize >= 4)
            {
                float randomValue = Random.Range(0.3f, 0.5f);
                //Debug.Log(randomValue);
                HeatMap randomMap = heatMapCounter.GetSumMapByProbability(randomValue, (enemyTag == "enemy") ? true : false);
                GameObject[] hs = GameObject.FindGameObjectsWithTag(AITag + "hunter");
                List<GameObject> hunters = hs.OfType<GameObject>().ToList();
                if (randomMap != null)
                {
                    if (hunters.Count > 0)
                    {
                        foreach (var g in hunters)
                        {
                            if (!g.GetComponent<UnitController>().enemyfound)
                            {
                                g.GetComponent<UnitController>().agent.SetDestination(new Vector3(randomMap.x + 25, 0, randomMap.y + 25));
                            }
                        }
                    }
                }
            }
            #endregion
        }
    }

    void changeStrategy()
    {
        int enemyZone = 0;
        for (int i = 0; i < heatMapCounter.goldMines.Count; i++)
        {
            for (int j = 0; j < heatMapCounter.heatmap.Count; j++)
            {
                if (heatMapCounter.heatmap[j].x == heatMapCounter.goldMines[i].x && heatMapCounter.heatmap[j].y == heatMapCounter.goldMines[i].y)
                {
                    //  Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube), new Vector3(heatMapCounter.heatmap[j].x, 0, heatMapCounter.heatmap[j].y), Quaternion.identity);
                    if (j - 1 > 0)
                    {
                        if (heatMapCounter.heatmap[j - 1].playerCount > 0)
                        {
                            enemyZone++;
                         
                        }
                    }

                    if (j - 9 > 0)
                    {
                        if (heatMapCounter.heatmap[j - 9].playerCount > 0)
                        {
                            enemyZone++;
                        }
                    }
                    if (j - 8 > 0)
                    {
                        if (heatMapCounter.heatmap[j - 8].playerCount > 0)
                        {
                            enemyZone++;
                        }
                    }

                    if (j - 7 > 0)
                    {
                        if (heatMapCounter.heatmap[j - 7].playerCount > 0)
                        {
                            enemyZone++;
                        }
                    }

                    if (heatMapCounter.heatmap[j].playerCount > 0)
                    {
                        enemyZone++;

                    }

                    if (j + 8 < heatMapCounter.heatmap.Count)
                    {
                        if (heatMapCounter.heatmap[j + 8].playerCount > 0)
                        {
                            enemyZone++;
                        }
                    }

                    if (j + 7 < heatMapCounter.heatmap.Count)
                    {
                        if (heatMapCounter.heatmap[j + 7].playerCount > 0)
                        {
                            enemyZone++;
                        }
                    }

                    if (j + 9 < heatMapCounter.heatmap.Count)
                    {
                        if (heatMapCounter.heatmap[j + 9].playerCount > 0)
                        {
                            enemyZone++;
                            
                        }
                    }


                    if (j + 1 < heatMapCounter.heatmap.Count)
                    {
                        if (heatMapCounter.heatmap[j + 1].playerCount > 0)
                        {
                            enemyZone++;
                        }
                    }
                }
            }
        }
        if (enemyZone >= 2) AIStrategy = "fast";

        // big army strategy
        int zone = 0;
        for (int i = 0; i < heatMapCounter.heatmap.Count; i++)
        {
        
            foreach (var r in GameObject.FindGameObjectsWithTag(AITag + "main building"))
            {
                if (Vector3.Distance(new Vector3(heatMapCounter.heatmap[i].x + 25, 0, heatMapCounter.heatmap[i].y + 25), r.gameObject.transform.position) < 50)
                {
                    if (i - 1 > 0)
                    {
                        if (heatMapCounter.heatmap[i - 1].playerCount > 0)
                        {
                            zone++;
                        }
                    }

                    if (i - 9 > 0)
                    {
                        if (heatMapCounter.heatmap[i - 9].playerCount > 0)
                        {
                            zone++;
                        }
                    }
                    if (i - 8 > 0)
                    {
                        if (heatMapCounter.heatmap[i - 8].playerCount > 0)
                        {
                            zone++;
                        }
                    }

                    if (i - 7 > 0)
                    {
                        if (heatMapCounter.heatmap[i - 7].playerCount > 0)
                        {
                            zone++;
                        }
                    }

                    if (heatMapCounter.heatmap[i].playerCount > 0)
                    {
                        zone++;

                    }

                    if (i + 8 < heatMapCounter.heatmap.Count)
                    {
                        if (heatMapCounter.heatmap[i + 8].playerCount > 0)
                        {
                            zone++;
                            
                        }
                    }

                    if (i + 7 < heatMapCounter.heatmap.Count)
                    {
                        if (heatMapCounter.heatmap[i + 7].playerCount > 0)
                        {
                            zone++;
                          
                        }
                    }

                    if (i + 9 < heatMapCounter.heatmap.Count)
                    {
                        if (heatMapCounter.heatmap[i + 9].playerCount > 0)
                        {
                            zone++;
                           
                        }
                    }


                    if (i + 1 < heatMapCounter.heatmap.Count)
                    {
                        if (heatMapCounter.heatmap[i + 1].playerCount > 0)
                        {
                            zone++;
                        
                        }
                    }
                }
            }
            //Debug.Log(zone);
            if (enemyZone >= 4) AIStrategy = "big";
        }



    }

    double calculateAttackProbability()
    {
        int numberOfPlayerStrikerUnits = GameObject.FindGameObjectsWithTag(AITag + "hunter").Length + GameObject.FindGameObjectsWithTag(AITag + "hero").Length + GameObject.FindGameObjectsWithTag(AITag + "archery").Length;
        int numberOfAIStrikerUnits = GameObject.FindGameObjectsWithTag(enemyTag + "hunter").Length + GameObject.FindGameObjectsWithTag(enemyTag + "hero").Length + GameObject.FindGameObjectsWithTag(enemyTag + "archery").Length;
        int AmountOfPlayerStrikerUnits = 0;

        if (GameObject.FindGameObjectsWithTag(AITag + "hunter").Length > 0) AmountOfPlayerStrikerUnits += GameObject.FindGameObjectsWithTag("hunter")[0].GetComponent<UnitSkills>().damage;
        if (GameObject.FindGameObjectsWithTag(AITag + "archery").Length > 0) AmountOfPlayerStrikerUnits += GameObject.FindGameObjectsWithTag(AITag + "archery")[0].GetComponent<UnitSkills>().damage;
        if (GameObject.FindGameObjectsWithTag(AITag + "hero").Length > 0) AmountOfPlayerStrikerUnits += GameObject.FindGameObjectsWithTag(AITag + "hero")[0].GetComponent<UnitSkills>().damage;

        int AmountOfAIStrikerUnits = 0;

        //  Debug.Log(enemyTag);
        //Debug.Log(numberOfAIStrikerUnits);
        if (GameObject.FindGameObjectsWithTag(enemyTag + "hunter").Length > 0) AmountOfAIStrikerUnits += GameObject.FindGameObjectsWithTag(enemyTag + "hunter")[0].GetComponent<UnitSkills>().damage;
        if (GameObject.FindGameObjectsWithTag(enemyTag + "archery").Length > 0) AmountOfAIStrikerUnits += GameObject.FindGameObjectsWithTag(enemyTag + "archery")[0].GetComponent<UnitSkills>().damage;
        if (GameObject.FindGameObjectsWithTag(enemyTag + "hero").Length > 0) AmountOfAIStrikerUnits += GameObject.FindGameObjectsWithTag(enemyTag + "hero")[0].GetComponent<UnitSkills>().damage;
        // Debug.Log(AmountOfAIStrikerUnits);
        //  Debug.Log(AmountOfPlayerStrikerUnits);
        if (AmountOfPlayerStrikerUnits + AmountOfAIStrikerUnits == 0) return 0;
        if (numberOfAIStrikerUnits + numberOfPlayerStrikerUnits == 0) return 0;
        double AIProbabN = AmountOfPlayerStrikerUnits / (AmountOfPlayerStrikerUnits + AmountOfAIStrikerUnits);
        double AIProbabA = numberOfPlayerStrikerUnits / (numberOfAIStrikerUnits + numberOfPlayerStrikerUnits);
        Debug.Log(AIProbabN);
        Debug.Log(AIProbabA);
        return (float)((AIProbabN + AIProbabA) / 2);
    }

    int calculateAttackProbabilityRandomEnemy()
    {
        int numberOfPlayerStrikerUnits = GameObject.FindGameObjectsWithTag("hunter").Length + GameObject.FindGameObjectsWithTag("Player").Length + GameObject.FindGameObjectsWithTag("archery").Length;
        int numberOfAIStrikerUnits = GameObject.FindGameObjectsWithTag("random enemy").Length;

        int AmountOfAIStrikerUnits = GameObject.FindGameObjectsWithTag("random enemy")[0].GetComponent<UnitSkills>().damage;
        //  int AmountOfPlayerStrikerUnits = GameObject.FindGameObjectsWithTag("hunter")[0].GetComponent<UnitSkills>().damage
        //                              + GameObject.FindGameObjectsWithTag("hero")[0].GetComponent<UnitSkills>().damage
        //                              + GameObject.FindGameObjectsWithTag("archery")[0].GetComponent<UnitSkills>().damage;
        //
        //  int AIProbabN = AmountOfAIStrikerUnits / (AmountOfPlayerStrikerUnits + AmountOfAIStrikerUnits);

        if (numberOfAIStrikerUnits + numberOfPlayerStrikerUnits == 0) return 0;
        int AIProbabA = numberOfAIStrikerUnits / (numberOfAIStrikerUnits + numberOfPlayerStrikerUnits);

        return AIProbabA;
    }

    bool checkEnemyAttack(GameObject building)
    {
        if (building != null)
        {
            foreach (var g in GameObject.FindGameObjectsWithTag(enemyTag + "hunter"))
            {
                if (Vector3.Distance(g.gameObject.transform.position, building.gameObject.transform.position) < 100)
                {
                    return true;
                }
            }

            foreach (var g in GameObject.FindGameObjectsWithTag(enemyTag + "worker"))
            {
                if (Vector3.Distance(g.gameObject.transform.position, building.gameObject.transform.position) < 100)
                {
                    return true;
                }
            }

            foreach (var g in GameObject.FindGameObjectsWithTag("random enemy"))
            {
                if (Vector3.Distance(g.gameObject.transform.position, building.gameObject.transform.position) < 100)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void moveUnits(GameObject[] unitType, GameObject enemyType)
    {
        foreach (GameObject h in unitType)
        {
            h.GetComponent<UnitController>().agent.SetDestination(enemyType.gameObject.transform.position);
        }
    }

    bool buyItem(Dictionary<string, int> item)
    {
        if (item["gold"] <= money["gold"])
                // && item["wood"] <= money["wood"])
        {
            money["gold"] = money["gold"] - item["gold"];
           // money["wood"] = money["wood"] - item["wood"];
            return true;
        }
        return false;
    }

    GameObject findMainBuilding(string buildingType)
    {
        if (GameObject.FindGameObjectsWithTag(buildingType).Length > 0) return GameObject.FindGameObjectsWithTag(buildingType)[0];
        return null;
    }
}

