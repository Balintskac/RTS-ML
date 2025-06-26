using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct Strategy
{
   // public double probability;
    public System.Action<int> doStrategy;
    public string name;
}

public class RtsAIAgent : Agent
{
    public GameObject cloneEnemyBuilding;
    public HeatMapCounter heatMapFields;
    public GameObject worker;
    public GameObject hunter;
    public GameObject building;
    public int gold;
    public string AIStrategy;
    public string AITag;
    public string enemyTag;
    public int enemyBuildingHit;
    public int buildingHit;
    public RtsAIAgent enemy;
    public List<GameObject> hunterList;
    public List<GameObject> enemyHunterList;
    public bool isPressed;
    public int number = 0;

    public List<GameObject> workerList;
    public List<GameObject> enemyWorkerList;

    Dictionary<string, Dictionary<string, int>> units = new Dictionary<string, Dictionary<string, int>>() {
       { "worker",new Dictionary<string,int>(){ { "gold", 100 }, { "wood", 0 } } },
       { "hunter",new Dictionary<string,int>(){ { "gold", 200 }, { "wood", 15 } } },
    };

    //Random time delay
    //Random choose
    //Random worker to work
    //Delay of money

    Dictionary<string, Dictionary<string, int>> strategy = new Dictionary<string, Dictionary<string, int>>() {
       { "worker",new Dictionary<string,int>(){ { "worker", 5}, { "hunter", 2 }, { "archer", 6 }, { "hero", 0 }, {"gold", 50 }, { "wood", 50 }, { "defense", 2 } } },
       { "fast",new Dictionary<string,int>(){ { "worker", 2}, { "hunter", 2 }, { "archer", 0 }, { "hero", 1 }, { "gold", 30 }, { "wood", 30 }, { "defense", 0 } } },
       { "defense",new Dictionary<string,int>(){ { "worker", 1}, { "hunter", 5 } , { "archer", 6 }, { "hero", 1 }, { "gold", 75 }, { "wood", 75 }, { "defense", 4 } }  },
       { "move",new Dictionary<string,int>(){ { "worker", 2}, { "hunter", 4 } , { "archer", 6 }, { "hero", 1 }, { "gold", 75 }, { "wood", 75 }, { "defense", 4 } }  },
       { "attack",new Dictionary<string,int>(){ { "worker", 3 }, { "hunter", 3 } , { "archer", 6 }, { "hero", 1 }, { "gold", 75 }, { "wood", 75 }, { "defense", 4 } }  },
    };

    public List<Strategy> strategies;


    bool buyItem(Dictionary<string, int> item)
    {
        if (item["gold"] <= gold)
        {
            gold = gold - item["gold"];
            return true;
        }
        return false;
    }

    GameObject findMainBuilding(string buildingType)
    {
        if (GameObject.FindGameObjectsWithTag(buildingType).Length > 0) return GameObject.FindGameObjectsWithTag(buildingType)[0];
        return null;
    }


    public int strategyByGradient(double Penz, double TamadoHadero, double VedekezoHadero, double MunkasSzam, double EpuletHP,
          double EllenfelPenz, double EllenfelTamadoHadero, double EllenfelVedekezoHadero, double EllenfelMunkasSzam, double EllenfelEpuletHP)
    {

        int strategyChoice = 0;
        List<double> Elosztasok = new List<double>();
        List<double> Hasonlitandok = new List<double>();
        Hasonlitandok.Add(Penz); Hasonlitandok.Add(EllenfelPenz);
        Hasonlitandok.Add(TamadoHadero); Hasonlitandok.Add(EllenfelTamadoHadero);
        Hasonlitandok.Add(VedekezoHadero); Hasonlitandok.Add(EllenfelVedekezoHadero);
        Hasonlitandok.Add(MunkasSzam); Hasonlitandok.Add(EllenfelMunkasSzam);
        Hasonlitandok.Add(EpuletHP); Hasonlitandok.Add(EllenfelEpuletHP);
        Hasonlitandok.Add(TamadoHadero); Hasonlitandok.Add(EllenfelTamadoHadero + EllenfelVedekezoHadero);
        Hasonlitandok.Add(TamadoHadero + VedekezoHadero); Hasonlitandok.Add(EllenfelTamadoHadero);

        //Elosztás = Saját / (Saját + Ellefél)
        for (int i = 0; i < Hasonlitandok.Count; i++)
        {
            if (Hasonlitandok[i] == 0 && Hasonlitandok[i + 1] == 0)
            {
                Elosztasok.Add(0.5);
            }
            else if (Hasonlitandok[i] == 0)
            {
                Elosztasok.Add(0);
            }
            else if (Hasonlitandok[i + 1] == 0)
            {
                Elosztasok.Add(1);
            }
            else
            {
                double AktualisErtek = Hasonlitandok[i] / (Hasonlitandok[i] + (Hasonlitandok[i + 1]));
                Elosztasok.Add(AktualisErtek);
            }
            i++;
        }
        //    Debug.Log(Elosztasok.ElementAt(1));
        //   Debug.Log(Elosztasok.ElementAt(5));
        Debug.Log("támadó ellenfél: " +Elosztasok.ElementAt(1));
        Debug.Log("munkás: " + Elosztasok.ElementAt(3));
        Debug.Log("pénz: " + Elosztasok.ElementAt(0));
        //   Debug.Log(Elosztasok.ElementAt(5));
        // 0 = Pénz,
        // 1 = Támadás,
        // 2 = Védés,
        // 3 = Munkás,
        // 4 = HP,
        // 5 = Támadó/EllenfélTeljes,
        // 6 = Teljes/EllenfélTámadó

        // Épület erõsítés és munkás gyártás
        if ((Elosztasok.ElementAt(0) < 0.3 && Elosztasok.ElementAt(4) < 0.3) 
            || (Elosztasok.ElementAt(3) < 0.5  && Elosztasok.ElementAt(0) >= 0.5))
            strategyChoice = 0;
        // Védekezés és harcos gyártás
        if (Elosztasok.ElementAt(1) < 0.6 || VedekezoHadero < 3 || Elosztasok.ElementAt(2) < 0.5)
            strategyChoice = 1;
        // terjeszkdés
        if (Elosztasok.ElementAt(4) > 0.7
            && (Elosztasok.ElementAt(1) <= 0.5))
            strategyChoice = 2;
        // Támadás és elterelés
        if (Elosztasok.ElementAt(2) > 0.6 && Elosztasok.ElementAt(1) > 0.6 && Elosztasok.ElementAt(3) >= 0.5)
            strategyChoice = 3;
        // Épület támadás
        if (//Elosztasok.ElementAt(0) >= 0.6 &&
            Elosztasok.ElementAt(1) > 0.6
            && Elosztasok.ElementAt(4) >= 0.5 && Elosztasok.ElementAt(2) > 0.6)
            strategyChoice = 4;
        //Default védekezés / erõsödés = 0
        return strategyChoice;
    }

    public override void Initialize()
    {
        heatMapFields = GameObject.Find("Heatmap").GetComponent<HeatMapCounter>();
        gold = 100;

      //  enemy = GameObject.Find((enemyTag.Equals("enemy ")) ? "AI player" : "AI").GetComponent<RtsAIAgent>();

        hunterList = new List<GameObject>();
        enemyHunterList = enemy.hunterList;
        workerList = new List<GameObject>();
        enemyWorkerList = enemy.workerList;

        List<string> choose = new List<string>() { "fast", "defense", "worker", "attack", "move" };
        AIStrategy = choose[Random.Range(0, choose.Count)];
        strategies = new List<Strategy>();
        strategies.Add(new Strategy() { doStrategy = (x) => { WorkerStrategy(); }, name = "worker" });
        strategies.Add(new Strategy() { doStrategy = (x) => { defenseStrategy(); }, name = "defense" });
        strategies.Add(new Strategy() { doStrategy = (x) => { moveStrategy(); }, name = "move" });
        strategies.Add(new Strategy() { doStrategy = (x) => { attackStrategy(); }, name = "attack" });
        strategies.Add(new Strategy() { doStrategy = (x) => { fastStrategy(); }, name = "fast" });
        //Time.timeScale = 2;
    }

    private void Update()
    {
        enemyHunterList = enemy.hunterList;
        enemyWorkerList = enemy.workerList;
        if (hunterList.Count > 0)
        {
            GameObject delete = hunterList.Find(x => x.tag == "dead");
            hunterList.Remove(delete);
            Destroy(delete);
        }
        if (workerList.Count > 0)
        {
            GameObject delete = workerList.Find(x => x.tag == "dead");
            workerList.Remove(workerList.Find(x => x.tag == "dead"));
            Destroy(delete);
        }

    }

    public float buildingGradient(bool strategyType)
    {
        float gradBulding = 0.0f;
        // AI player
        float hunterClose = hunterList
           .OfType<GameObject>()
           .ToList()
           .Aggregate(0.0f, (acc, x) => acc + Vector3.Distance(cloneEnemyBuilding.transform.position, cloneEnemyBuilding.transform.position));

        int enemyHunterDefense = enemyHunterList
            .OfType<GameObject>()
            .ToList()
            .Aggregate(0, (acc, x) =>
            {
                if (Vector3.Distance(x.transform.position, building.transform.position) < 100)
                {
                    acc++;
                }
                return acc;
            });

        hunterClose = hunterClose / hunterList.Count;

        enemyHunterDefense = enemyHunterDefense / enemyHunterList.Count;

        float BuldingHP = building.GetComponent<BuildingUnit>().health / building.GetComponent<BuildingUnit>().maxHp;

        // AI enemy
        float hunterCloseEnemy = hunterList
            .OfType<GameObject>()
            .ToList()
            .Aggregate(0.0f, (acc, x) => acc + Vector3.Distance(cloneEnemyBuilding.transform.position, cloneEnemyBuilding.transform.position));

        int enemyHunterDefenseEnemy = enemyHunterList
            .OfType<GameObject>()
            .ToList()
            .Aggregate(0, (acc, x) =>
            {
                if (Vector3.Distance(x.transform.position, building.transform.position) < 100)
                {
                    acc++;
                }
                return acc;
            });

        hunterCloseEnemy = hunterCloseEnemy / hunterList.Count;

        enemyHunterDefenseEnemy = enemyHunterDefenseEnemy / enemyHunterList.Count;

        float BuldingHPEnemy = cloneEnemyBuilding.GetComponent<BuildingUnit>().health / cloneEnemyBuilding.GetComponent<BuildingUnit>().maxHp;

        float gradHunterAttack = 0.0f;
        float gradHunterDefense = 0.0f;
        float gradBuildingHP = 0.0f;

        if (strategyType)
        {
            gradHunterAttack = hunterClose / (hunterCloseEnemy + hunterClose);
            gradHunterDefense = enemyHunterDefense / (enemyHunterDefense + enemyHunterDefenseEnemy);
            gradBuildingHP = BuldingHP / (BuldingHPEnemy + BuldingHP);
        }
        else
        {
            gradHunterAttack = hunterCloseEnemy / (hunterCloseEnemy + hunterClose);
            gradHunterDefense = enemyHunterDefenseEnemy / (enemyHunterDefense + enemyHunterDefenseEnemy);
            gradBuildingHP = BuldingHPEnemy / (BuldingHPEnemy + BuldingHP);
        }
        // szorzók hp, attack, defense
        // avg hp * worker avg
        // avghunter * 100m
        // avg defense
        gradBulding = (gradHunterAttack + gradHunterDefense + gradBuildingHP) / 3;

        return gradBulding;
    }

    public double gradientCalculation(int g = 0, int h = 0, int w = 0, int eh = 0, int ew = 0, bool check = false)
    {
        int enemy_gold = enemy.gold;
        int enemy_worker_number = enemyWorkerList.Count;
        int enemy_hunter_number = enemyHunterList.Count;
        // int enemy_heat_map_attack = GameObject.FindGameObjectsWithTag("heat map 2").Length;
        // int enemy_heat_map_defence = GameObject.FindGameObjectsWithTag("heat map 2").Length;
        // int enemy_time_to_finish = 0; // strategy steps remaining to finish 
        int my_gold = gold;
        int worker_number = workerList.Count;
        int hunter_number = hunterList.Count;
        // int heat_map_attack = GameObject.FindGameObjectsWithTag("heat map 1").Length;
        //int heat_map_defence = GameObject.FindGameObjectsWithTag("heat map 1").Length;
        // int time_to_finish = 0; // strategy steps remaining to finish 
        // building defense number of hunters around
        // close to the building for hunter
        // hp
        if (check)
        {
            my_gold -= g;
            worker_number = w;
            hunter_number = h;
            enemy_worker_number = ew;
            enemy_hunter_number = eh;
        }

        // TEST 1
        double goldCalc = 0.5;
        double workerCalc = 0.5;
        double hunterCalc = 0.5;
        if (my_gold + enemy_gold > 0)
        {
            double d = (my_gold + enemy_gold);

            if (goldCalc + (my_gold / d) >= 1.0)
            {
                workerCalc = my_gold / d;
            }
            else
            {
                goldCalc += (my_gold / d);
            }
        }

        if (worker_number + enemy_worker_number > 0)
        {

            double d = (worker_number + enemy_worker_number);
            // Debug.Log("worker = " + worker_number / g);
            if (workerCalc + (worker_number / d) >= 1.0)
            {
                workerCalc = worker_number / d;
            }
            else
            {
                workerCalc += (worker_number / d);
            }

        }

        if (hunter_number + enemy_hunter_number > 0)
        {

            double d = (hunter_number + enemy_hunter_number);
            if (hunterCalc + (hunter_number / d) >= 1.0)
            {
                hunterCalc = hunter_number / d;
            }
            else
            {
                hunterCalc += (hunter_number / d);
            }
        }
        // Debug.Log((goldCalc + workerCalc + hunterCalc) / 3);
        return (goldCalc + workerCalc + hunterCalc) / 3;
    }
    public int targetStrategy()
    {
        int enemy_gold = enemy.gold;
        int my_gold = gold;
        int numberOfWorker = workerList.Count;
        int numberOfWorkerEnemy = enemyWorkerList.Count;
        double hunterClose = hunterList
         .OfType<GameObject>()
         .ToList()
         .Aggregate(300.0f, (acc, x) => {
             if (acc > Vector3.Distance(x.transform.position, cloneEnemyBuilding.transform.position))
                 acc = Vector3.Distance(x.transform.position, cloneEnemyBuilding.transform.position);
             return acc;
         });
      // if(hunterList.Count > 0) hunterClose = hunterClose / hunterList.Count;
        int enemyHunterDefense = hunterList
            .OfType<GameObject>()
            .ToList()
            .Aggregate(0, (acc, x) =>
            {
                if (Vector3.Distance(x.transform.position, building.transform.position) < 100)
                {
                    acc++;
                }
                return acc;
            });

        //   hunterClose = hunterClose / hunterList.Count;

        //  enemyHunterDefense = enemyHunterDefense / enemyHunterList.Length;

        float BuldingHP = building.GetComponent<BuildingUnit>().health / building.GetComponent<BuildingUnit>().maxHp;

        // AI enemy
        double hunterCloseEnemy = hunterList
            .OfType<GameObject>()
            .ToList()
             .Aggregate(300.0f, (acc, x) => {
                 if (acc > Vector3.Distance(x.transform.position, building.transform.position))
                     acc = Vector3.Distance(x.transform.position, building.transform.position);
                 return acc;
             });
        int enemyHunterDefenseEnemy = enemyHunterList
            .OfType<GameObject>()
            .ToList()
            .Aggregate(0, (acc, x) =>
            {
                if (Vector3.Distance(x.transform.position, building.transform.position) < 100)
                {
                    acc++;
                }
                return acc;
            });
        float BuldingHPEnemy = cloneEnemyBuilding.GetComponent<BuildingUnit>().health / cloneEnemyBuilding.GetComponent<BuildingUnit>().maxHp;
        // Debug.Log("Hunter close" + hunterClose);
        // Debug.Log("enemy Hunter close" + hunterCloseEnemy);
        //  Debug.Log("Hunter def" + enemyHunterDefense);
        // Debug.Log("enemy Hunter def" + enemyHunterDefenseEnemy);
        return strategyByGradient(my_gold, hunterClose, enemyHunterDefense, numberOfWorker,
              gold, hunterCloseEnemy, hunterCloseEnemy, enemyHunterDefenseEnemy, numberOfWorkerEnemy, BuldingHPEnemy);
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(workerList.Count);
        sensor.AddObservation(hunterList.Count);
        sensor.AddObservation(hunterList
         .Aggregate(0.0f, (acc, x) => {
             if (acc > Vector3.Distance(x.transform.position, cloneEnemyBuilding.transform.position))
                 acc = Vector3.Distance(x.transform.position, cloneEnemyBuilding.transform.position);
             return acc;
         }));
        sensor.AddObservation(hunterList
            .Aggregate(0, (acc, x) =>
            {
                if (Vector3.Distance(x.transform.position, building.transform.position) < 100)
                {
                    acc++;
                }
                return acc;
            }));
        sensor.AddObservation(building.GetComponent<BuildingUnit>().health/ building.GetComponent<BuildingUnit>().maxHp);
        sensor.AddObservation((gold + enemy.gold > 0)? gold/(gold + enemy.gold): 0);

        sensor.AddObservation(enemyWorkerList.Count);
        sensor.AddObservation(enemyHunterList.Count);
        sensor.AddObservation(cloneEnemyBuilding.GetComponent<BuildingUnit>().health/ cloneEnemyBuilding.GetComponent<BuildingUnit>().maxHp);
        sensor.AddObservation((gold + enemy.gold > 0) ? enemy.gold / (gold + enemy.gold) : 0);
        sensor.AddObservation(enemyHunterList
       .Aggregate(0.0f, (acc, x) => {
           if (acc > Vector3.Distance(x.transform.position, building.transform.position))
               acc = Vector3.Distance(x.transform.position, building.transform.position);
           return acc;
       }));
        sensor.AddObservation(enemyHunterList
       .Aggregate(0, (acc, x) =>
       {
           if (Vector3.Distance(x.transform.position, cloneEnemyBuilding.transform.position) < 100)
           {
               acc++;
           }
           return acc;
       }));
    }
    // money waiting calc
    // units avg, heat map reserved, money, defence, time to finish
    // hunter ??
    // worker
    // time ?? wait money = 100, hunter 50 * 2, hunters go
    // time ?? wait money = 50, hunter 50, hunters go
    // time ?? hunter go

    //RESULT:
    // time = worker * money & hunter distance enemy bulding
    // heatmap -> near to building count
    // enemy hunters
    // money -> worker number
    // enemy worker check & hunter check
    // hunters far, number, defense
    public void attackStrategy()
    {
        // Debug.Log("-----------------WORKER STRATEGY-----------------");

        // select x number of hunter to move
        // make horde
        if (strategy[AIStrategy]["hunter"] <= hunterList.Count &&
             cloneEnemyBuilding != null)
        {
            int random = Random.Range(0, 1);
            int random2 = Random.Range(0, 3);

            if (random2 == 2)
            {
                List<GameObject> hs = hunterList.OfType<GameObject>().ToList();
                foreach (var h in hs)
                {
                    if (h.GetComponent<UnitController>().enemyfound)
                    {
                        h.GetComponent<EnemyDetector>().enemySearched = new List<GameObject>();
                        h.GetComponent<UnitController>().enemyfound = false;
                        h.GetComponent<EnemyDetector>().workerFind = false;
                    }
                }
            }
            if (enemyWorkerList.Count > 0)
            {
                if (random == 1)
                {
                    GameObject h = hunterList[Random.Range(0, hunterList.Count)];
                    GameObject enemy = enemyWorkerList[0];
                    (int x, int y) = heatMapFields.getCoordsByPosition((int)enemy.transform.position.x, (int)enemy.transform.position.z);
                    h.GetComponent<UnitController>().agent.SetDestination(new Vector3(x * 50, 0, y * 50));
                    h.GetComponent<EnemyDetector>().workerFind = true;
                }
                else
                {
                    GameObject enemy = enemyWorkerList[0];
                    List<GameObject> hs = hunterList.OfType<GameObject>().ToList();
                    (int x, int y) = heatMapFields.getCoordsByPosition((int)enemy.transform.position.x, (int)enemy.transform.position.z);
                    foreach (var h in hs.Take(Random.Range(0, hunterList.Count)))
                    {
                        h.GetComponent<UnitController>().agent.SetDestination(new Vector3(x * 50, 0, y * 50));
                        h.GetComponent<EnemyDetector>().workerFind = true;
                    }
                }
            }
        }
    }
    public void WorkerStrategy()
    {
        // Debug.Log("-----------------STRONG STRATEGY-----------------");


        if (strategy[AIStrategy]["worker"] > workerList.Count
            && buyItem(units["worker"]))
        {
            // GameObject building = findMainBuilding(AITag + "main building");
            // building.GetComponent<BuildingUnit>().spawn = hunter;
            workerList.Add(Instantiate(worker, new Vector3(building.transform.position.x + Random.Range(10, 15), building.transform.position.y, building.transform.position.z + Random.Range(10, 15)), Quaternion.identity));
        }

        // select x number of hunter to move
        // make horde
        int random = Random.Range(1, 6);
        if (workerList.Count > 0 &&
             building != null)
        {
            // int random = Random.Range(1, 5);

            if (random == 0)
            {
                if (building.GetComponent<BuildingUnit>().health != building.GetComponent<BuildingUnit>().maxHp)
                {
                    (int x, int y) = heatMapFields.getCoordsByPosition((int)building.transform.position.x, (int)building.transform.position.z);
                    List<GameObject> hs = workerList.OfType<GameObject>().ToList();
                    foreach (var h in hs.Take(Random.Range(0, hs.Count)))
                    {
                        if (h.GetComponent<WorkingHandler>().target != "build")
                        {
                            h.GetComponent<WorkingHandler>().target = "build";
                            h.GetComponent<WorkingHandler>().goldMine = null;
                            h.GetComponent<UnitController>().agent.SetDestination(new Vector3(building.transform.position.x, 0, building.transform.position.z));
                        }
                    }
                }
            }
            if (random > 0)
            {
                if (building.GetComponent<BuildingUnit>().health != building.GetComponent<BuildingUnit>().maxHp)
                {
                    (int x, int y) = heatMapFields.getCoordsByPosition((int)building.transform.position.x, (int)building.transform.position.z);
                    List<GameObject> hs = workerList.OfType<GameObject>().ToList();
                    foreach (var h in hs.Take(Random.Range(0, hs.Count)))
                    {
                        if (h.GetComponent<WorkingHandler>().target == "build")
                        {
                            h.GetComponent<WorkingHandler>().target = "gold mine";
                        }
                    }
                }
            }

        }
    }
    public void moveStrategy()
    {
        //Debug.Log("-----------------FAST STRATEGY GRADIENT-----------------");
        if (strategy[AIStrategy]["worker"] > workerList.Count
            && buyItem(units["worker"]))
        {
            //  GameObject building = findMainBuilding(AITag + "main building");
            // building.GetComponent<BuildingUnit>().spawn = worker;
            workerList.Add(Instantiate(worker, new Vector3(building.transform.position.x + Random.Range(10, 15), building.transform.position.y, building.transform.position.z + Random.Range(10, 15)), Quaternion.identity));
        }

        if (strategy[AIStrategy]["hunter"] > hunterList.Count
            && buyItem(units["hunter"]))
        {
            //  GameObject building = findMainBuilding(AITag + "main building");
            //  building.GetComponent<BuildingUnit>().spawn = hunter;
            hunterList.Add(Instantiate(hunter, new Vector3(building.transform.position.x + Random.Range(10, 15), building.transform.position.y, building.transform.position.z + Random.Range(10, 15)), Quaternion.identity));
        }

        if (strategy[AIStrategy]["hunter"] <= hunterList.Count)
        {
            // int random = Random.Range(1, 5);

            //  if (random == 2)
            //    {
            List<GameObject> hs = hunterList.OfType<GameObject>().ToList();
            (int x, int y) = heatMapFields.getCoordsByPosition((int)cloneEnemyBuilding.transform.position.x, (int)cloneEnemyBuilding.transform.position.z);
            float randomX = Random.Range(0, 5);
            float randomY = Random.Range(0, 5);
            float randomAttack = Random.Range(0, 3);

            foreach (var h in hs.Take(2))
            {
                (int hx, int hy) = heatMapFields.getCoordsByPosition((int)h.transform.position.x, (int)h.transform.position.z);
                if (hx < x) hx += (int)randomX;
                if (hy > y) hy -= (int)randomY;
                if (hx > x) hx -= (int)randomX;
                if (hy < y) hy += (int)randomY;
                h.GetComponent<UnitController>().agent.SetDestination(new Vector3(hx * 50, 0, hy * 50));
                if (randomAttack > 0)
                {
                    h.GetComponent<EnemyDetector>().hunterFind = true;
                    h.GetComponent<EnemyDetector>().workerFind = true;
                }
            }
            // }
        }

    }
    public void defenseStrategy()
    {
        // Debug.Log("-----------------DEFENSE STRATEGY-----------------");
        if (strategy[AIStrategy]["hunter"] > hunterList.Count
            && buyItem(units["hunter"]))
        {
            hunterList.Add(Instantiate(hunter, new Vector3(building.transform.position.x + Random.Range(10, 15), building.transform.position.y, building.transform.position.z + Random.Range(10, 15)), Quaternion.identity));
        }

        // select x number of hunter to move
        // make horde
        if (strategy[AIStrategy]["hunter"] <= hunterList.Count)
        {
            int random = Random.Range(1, 3);

            if (random == 2)
            {
                List<GameObject> hs = hunterList.OfType<GameObject>().ToList();
                List<GameObject> enemies = enemyHunterList.OfType<GameObject>().ToList();
                GameObject enemy = null;
                foreach (GameObject e in enemies)
                {
                    if (Vector3.Distance(e.transform.position, building.transform.position) <= 150)
                    {
                        enemy = e;
                    }
                }


                if (hs.Count > 0 && enemy != null)
                {
                    int randomAttack = Random.Range(0, 1);

                    if (randomAttack == 1)
                    {
                        hs[Random.Range(0, 1)].GetComponent<UnitController>().agent.SetDestination(new Vector3(enemy.transform.position.x, 0, enemy.transform.position.y));
                        hs[Random.Range(0, 1)].GetComponent<EnemyDetector>().hunterFind = true;
                    }
                    else
                    {
                        foreach (var h in hs)
                        {
                            if (!h.GetComponent<EnemyDetector>().hunterFind)
                            {
                                h.GetComponent<UnitController>().agent.SetDestination(new Vector3(enemy.transform.position.x, 0, enemy.transform.position.y));
                                h.GetComponent<EnemyDetector>().hunterFind = true;
                            }
                        }
                    }
                }
            }
            else
            {
                List<GameObject> hs = hunterList.OfType<GameObject>().ToList();
                (int x, int y) = heatMapFields.getCoordsByPosition((int)building.transform.position.x, (int)building.transform.position.z);
                foreach (var h in hs.Take(Random.Range(0, hs.Count)))
                {
                    h.GetComponent<UnitController>().agent.SetDestination(new Vector3(x * 50, 0, y * 50));
                    h.GetComponent<EnemyDetector>().hunterFind = true;
                }
            }
        }

    }
    public void fastStrategy()
    {
        //Debug.Log("-----------------FAST STRATEGY GRADIENT-----------------");
        if (strategy[AIStrategy]["hunter"] > hunterList.Count
            && buyItem(units["hunter"]))
        {
            //GameObject building = findMainBuilding(AITag + "main building");
            //building.GetComponent<BuildingUnit>().spawn = hunter;
            hunterList.Add(Instantiate(hunter, new Vector3(building.transform.position.x + Random.Range(10, 15), building.transform.position.y, building.transform.position.z + Random.Range(10, 15)), Quaternion.identity));
        }

        // select x number of hunter to move
        // make horde
        if (strategy[AIStrategy]["hunter"] >= hunterList.Count)
        {
            int random = Random.Range(1, 3);

            if (random > 1 && hunterList.Count > 0)
            {
                List<GameObject> hs = hunterList.OfType<GameObject>().ToList();
                (int x, int y) = heatMapFields.getCoordsByPosition((int)cloneEnemyBuilding.transform.position.x, (int)cloneEnemyBuilding.transform.position.z);
                foreach (var h in hs)
                {
                    if (h.GetComponent<UnitController>().enemyfound && h.GetComponent<UnitController>().agent.velocity == new Vector3(0, 0, 0))
                    {
                        float randomX = Random.Range(0, 3);
                        float randomY = Random.Range(0, 3);
                        //   x -= (int)randomX;
                        //  y += (int)randomY;
                        h.GetComponent<EnemyDetector>().enemySearched = new List<GameObject>();
                        h.GetComponent<UnitController>().enemyfound = false;
                        h.GetComponent<EnemyDetector>().buildingFind = false;
                        //  h.GetComponent<UnitController>().agent.SetDestination(new Vector3(x * 50, 0, y * 50));
                    }
                }
            }

            if (random == 1 && hunterList.Count >= 2)
            {
                GameObject h = hunterList[Random.Range(0, 1)];
                if (h.GetComponent<UnitController>().agent.velocity == new Vector3(0, 0, 0))
                {
                    (int x, int y) = heatMapFields.getCoordsByPosition((int)cloneEnemyBuilding.transform.position.x, (int)cloneEnemyBuilding.transform.position.z);
                    h.GetComponent<UnitController>().agent.SetDestination(new Vector3(x * 50, 0, y * 50));
                    h.GetComponent<EnemyDetector>().buildingFind = true;
                }
            }

            if (random == 2 && hunterList.Count >= 1)
            {
                foreach (var h in hunterList)
                {
                    if (!h.GetComponent<UnitController>().enemyfound && h.GetComponent<UnitController>().agent.velocity == new Vector3(0, 0, 0))
                    {
                        (int x, int y) = heatMapFields.getCoordsByPosition((int)cloneEnemyBuilding.transform.position.x, (int)cloneEnemyBuilding.transform.position.z);
                        h.GetComponent<UnitController>().agent.SetDestination(new Vector3(x * 50, 0, y * 50));
                        h.GetComponent<EnemyDetector>().buildingFind = true;
                    }
                }
            }
            /*   else
            {
                List<GameObject> hs = hunterList.OfType<GameObject>().ToList();
                (int x, int y) = heatMapFields.getCoordsByPosition((int)cloneEnemyBuilding.transform.position.x, (int)cloneEnemyBuilding.transform.position.z);
                //Debug.Log(x + " " + y);
                foreach (var h in hs)
                {
                    float randomX = Random.Range(1, 3);
                    float randomY = Random.Range(1, 3);
                    x -= (int)randomX;
                    y += (int)randomY;
                    h.GetComponent<UnitController>().agent.SetDestination(new Vector3(x * 50, 0, y * 50));
                }
            }*/
        }

    }

    public void getHeatMapCoord(GameObject hunter, int x, int y)
    {
        if (heatMapFields.heatmap.Exists(heat => heat.x == 50 * x && heat.y == 50 * y))
        {
            GameObject.FindGameObjectWithTag(AITag + "hunter").GetComponent<UnitController>().agent.SetDestination(new Vector3(50 * x, 0, 50 * y));
        }
    }
    public override void OnActionReceived(float[] vectorAction)
    {
        Debug.Log("output: "+vectorAction[0]);
        int predict = (int)vectorAction[0]; //Random.Range(0, 3);
        /* if (!GameObject.FindGameObjectWithTag("hunter").GetComponent<UnitController>().enemyfound && GameObject.FindGameObjectWithTag("hunter").GetComponent<UnitController>().agent.velocity == new Vector3(0.0f, 0.0f, 0.0f))
         {
             getHeatMapCoord(null, (int)vectorAction[0], (int)vectorAction[1]);
         }
         else if (GameObject.FindGameObjectWithTag("hunter").GetComponent<UnitController>().enemyfound)
         {
             Debug.Log("COMPLETED");
             AddReward(1f);
             // ResetAgent();
             EndEpisode();
         }*/
        // reward = hunters kill * 0.05
        // reward = closetobuilding * hunter number * 0.1
        // reward = hp * hunter number attack * 0.2
        // if enemy hunter close < hunter close and strong and hp < enemy hp
        // rankStrategyList();
        AIStrategy = strategies[predict].name;
        strategies[predict].doStrategy(0);
        // + 0.5 ha jó stratégiát választ az adott pillanatban
        // + 3 ha nyerni tud
        // Mindig választ statégiát (akkor nem változik semmmi, folytatja)
        // Ha vált stratégiát, akkor lesz jelentõsége
        // Debug.Log("gradient = " + gradientCalculation());
        //  Debug.Log("Gold " + AITag+ ": "    + gold);
        //  Debug.Log("Strategy " + AITag + ": " + AIStrategy);

        //Választás jutalom / büntetés
        Debug.Log("TARGET: " + targetStrategy());
        if (targetStrategy() != predict)
        {
            AddReward(-1f / MaxStep);
        }
        else
        {
            AddReward(1f);
        }

        // Épület támadás 
        //Jutalom
        if (cloneEnemyBuilding.tag != "dead")
        {
            if (cloneEnemyBuilding.GetComponent<BuildingUnit>().health < enemyBuildingHit)
            {
                enemyBuildingHit = cloneEnemyBuilding.GetComponent<BuildingUnit>().health;
                AddReward(2f);
            }

            if (cloneEnemyBuilding.GetComponent<BuildingUnit>().health > enemyBuildingHit)
            {
                enemyBuildingHit = cloneEnemyBuilding.GetComponent<BuildingUnit>().health;
            }
        }
        else
        {
            AddReward(3f);
            ResetAgent();
            enemy.ResetAgent();
        }

        // Épület védekezés
        // Büntetés
        if (building.tag != "dead")
        {
            if (building.GetComponent<BuildingUnit>().health < buildingHit)
            {
                buildingHit = building.GetComponent<BuildingUnit>().health;
                AddReward(-0.5f);
            }

            if (building.GetComponent<BuildingUnit>().health > buildingHit)
            {
                buildingHit = building.GetComponent<BuildingUnit>().health;
            }
        }
        else
        {
            AddReward(-1f);
            ResetAgent();
            enemy.ResetAgent();
        }
    }

    void rankStrategyList()
    {
        fastStrategy();
        defenseStrategy();
        //moveStrategy(true);
        // attackStrategy(true);
        //StrongStrategy(true);
        //  Debug.Log("fast = " + strategies.First(x => x.name == "fast").probability);
        //Debug.Log("defense = " + strategies.First(x => x.name == "defense").probability);
        //  strategies.Sort((s1, s2) => s1.probability.CompareTo(s2.probability));
    }

    void ResetAgent()
    {
        building.GetComponent<BuildingUnit>().health = building.GetComponent<BuildingUnit>().maxHp;
        cloneEnemyBuilding.GetComponent<BuildingUnit>().health = cloneEnemyBuilding.GetComponent<BuildingUnit>().maxHp;
        building.GetComponent<BuildingUnit>().healthBarImage.transform.localScale = new Vector3((float)Mathf.Min(Mathf.Max(building.GetComponent<BuildingUnit>().health, 0),building.GetComponent<BuildingUnit>().maxHp) / (float)building.GetComponent<BuildingUnit>().maxHp, 1, 1);
        cloneEnemyBuilding.GetComponent<BuildingUnit>().healthBarImage.transform.localScale = new Vector3((float)Mathf.Min(Mathf.Max(cloneEnemyBuilding.GetComponent<BuildingUnit>().health, 0), cloneEnemyBuilding.GetComponent<BuildingUnit>().maxHp) / (float)cloneEnemyBuilding.GetComponent<BuildingUnit>().maxHp, 1, 1);
        building.tag = AITag + "main building";
        cloneEnemyBuilding.tag = enemyTag + "main building";
        hunterList.ForEach(x => {
            Destroy(x.transform.gameObject);
        });
        workerList.ForEach(x => {
            Destroy(x.transform.gameObject);
        });
        enemy.hunterList.ForEach(x => {
            Destroy(x.transform.gameObject);
        });
        enemy.workerList.ForEach(x => {
            Destroy(x.transform.gameObject);
        });
        hunterList.Clear();
        workerList.Clear();
        enemy.hunterList.Clear();
        enemy.workerList.Clear();
        hunterList = new List<GameObject>();
        enemy.hunterList = new List<GameObject>();
        enemyHunterList = enemy.hunterList;
        workerList = new List<GameObject>();
        enemy.workerList = new List<GameObject>();
        enemyWorkerList = enemy.workerList;
        gold = 100;
        enemy.gold = 100;
    }
    public override void Heuristic(float[] actionsOut)
    {
        /*  if (Input.GetKey(KeyCode.Alpha0))
          {
              actionsOut[0] = 0;
          }
          else if (Input.GetKey(KeyCode.Alpha1))
          {
              actionsOut[0] = 1;
          }
          else if (Input.GetKey(KeyCode.Alpha2))
          {
              actionsOut[0] = 2;
          }
          else if (Input.GetKey(KeyCode.Alpha3))
          {
              actionsOut[0] = 3;
          }
          else if (Input.GetKey(KeyCode.Alpha4))
          {
              actionsOut[0] = 4;
          }*/
        actionsOut[0] = targetStrategy();
        enemy.OnActionReceived(new float[] { Random.Range(0,5)});
    }
}
/*  public override void Heuristic(float[] actionsOut)
 {
     actionsOut[0] = Input.GetKey(KeyCode.Alpha0) ? 1 : 0;
     actionsOut[1] = Input.GetKey(KeyCode.Alpha1) ? 1 : 0;
     actionsOut[2] = Input.GetKey(KeyCode.Alpha2) ? 1 : 0;
     actionsOut[3] = Input.GetKey(KeyCode.Alpha3) ? 1 : 0;
     actionsOut[4] = Input.GetKey(KeyCode.Alpha4) ? 1 : 0;

 }
    public override void OnEpisodeBegin()
    {
        building.GetComponent<BuildingUnit>().health = building.GetComponent<BuildingUnit>().maxHp;
        cloneEnemyBuilding.GetComponent<BuildingUnit>().health = cloneEnemyBuilding.GetComponent<BuildingUnit>().maxHp;
        building.tag = "main building";
        cloneEnemyBuilding.tag = "enemy main building";
        hunterList.ForEach(x => {
            Destroy(x.transform.gameObject);
        });
        workerList.ForEach(x => {
            Destroy(x.transform.gameObject);
        });
        hunterList.Clear();
        workerList.Clear();
        hunterList = new List<GameObject>();
        enemyHunterList = enemy.hunterList;
        workerList = new List<GameObject>();
        enemyWorkerList = enemy.workerList;
        gold = 100;
        enemy.gold = 100;
    }
 */
