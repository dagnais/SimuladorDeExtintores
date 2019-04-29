/*
Titulo: "SimuladorDeExtintores"
Hecho en el año:2019 
-----
Title: "SimuladorDeExtintores"
Made in the year: 2019
*/
using UnityEngine;
using UnityEngine.SceneManagement;

public class Simulation_Controller : MonoBehaviour
{
    public enum SceneType
    {
        oficina=0,
        almacen=1,
        taller=2,
        cocina=3
    }
    public SceneType sceneType;
    public bool isTimeLimit;
    public Enumeradores.TypeExtintor correctType;

    [HideInInspector]
    public bool bonus;

    [HideInInspector]
    public string comments;

    [HideInInspector]
    public bool goodComments;

    #region variables privadas
    Transform _avatarVR;
    PersistentData _data;
    int _timeLimit = 180;
    FireManager _manager;
    int _time;
    bool _isFirst;
    #endregion
    void Load()
    {
        _avatarVR = GameObject.Find("AvatarVR").transform;
        if (_avatarVR == null)
            Debug.LogError("AvatarVR no encontrado");

        _data = FindObjectOfType<PersistentData>();
        if (_data == null)
            Debug.LogError("persisten data no encontrado");

        _manager = GetComponent<FireManager>();
        if (_manager == null)
            Debug.LogError("_manager no encontrado");
    }

    void Start()
    {
        Load();
        SetPositionAvatar();
        InvokeRepeating("CheckStateNodes", 5, 5);
        if(isTimeLimit)
            InvokeRepeating("Timening", 1, 1);

    }

    void SetPositionAvatar()
    {
        _avatarVR.position += _data.positions[(int)sceneType];
    }

    void Timening()
    {
        _time++;
        if (_time >= _timeLimit)
        {
            SetComments("SE HA ACABADO EL TIEMPO, DEBES REALIZARLO MÁS RÁPIDO");
            if (FindObjectOfType<PersistentData>() == null)
                CancelInvoke("Timening");
            else
            {
                goodComments = false;
                FindObjectOfType<PersistentData>().SetResult(false, bonus, comments,sceneType,goodComments);
                Finish();
            }
        }
    }

    void CheckStateNodes()
    {
        int count = 0;
        for (int i = 0; i < _manager.nodesInstanced.Count; i++)
        {
            if (_manager.nodesInstanced[i].stateNode == Enumeradores.State.f0W ||
                _manager.nodesInstanced[i].stateNode == Enumeradores.State.f0)
                count++;

            if (count == _manager.nodesInstanced.Count)
            {
                if(FindObjectOfType<PersistentData>()!=null)
                {
                    FindObjectOfType<PersistentData>().SetResult(true, bonus, comments, sceneType, goodComments);
                    Finish();
                }
                
            }
        }
    }
    public void Finish()
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject obj = GameObject.Find("New Game Object");
            if (obj != null)
                Destroy(obj);
        }
        SceneManager.LoadScene("Simulacion_Finalizada");
    }

    public void SetComments(string comm)
    {
        comments = comm;
    }

    public void SetBonus(bool value)
    {
        if (_isFirst)
            bonus = false;
        else
            bonus = value;
    }

    public void NotAlarm()
    {
        _isFirst = true;
        bonus = false;
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Keypad0))
            _data.currentDifficulty = Enumeradores.difficulty.facil;
        if (Input.GetKeyUp(KeyCode.Keypad1))
            _data.currentDifficulty = Enumeradores.difficulty.normal;
        if (Input.GetKeyUp(KeyCode.Keypad2))
            _data.currentDifficulty = Enumeradores.difficulty.dificil;
    }
}
