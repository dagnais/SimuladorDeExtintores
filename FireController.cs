/*
Titulo: "SimuladorDeExtintores"
Hecho en el año:2019 
-----
Title: "SimuladorDeExtintores"
Made in the year: 2019
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireController : MonoBehaviour
{
    public bool isProof;//prueba

    public Enumeradores.TypeMaterial typeNode;
    //f0= no prendido, f25 prendido al 25% , f0W= apagado por matafuego
    
    public Material[] materialStates;// 0 f0, 1 f25, 2 f50, 3 f75 , 4 f100, 5 f0w

    [HideInInspector]
    public int id;

    [HideInInspector]
    public Enumeradores.State stateNode;
    
    [HideInInspector]
    public Neighbor neighbor;// x right, y left;

    [HideInInspector]
    public int fireSpeed = 1;

    [HideInInspector]
    public bool isWater;
    
    public IEnumerator incrementTemp, checkState100f;

    #region variables privadas
    Transform _burnDecal;
    
    int _temperature;
    FireManager _manager;
    MeshRenderer _renderer;
    bool _isCheckState100f;
    float _timeCheck = 1;
    BoxCollider _collider;
    float _timeIncTemp;
    PersistentData _data;
    FireSPData _fireData;//0 25, 1 50, 2 75, 3 100
    #endregion

    /// <summary>
    /// configuracion de los sistemas de partículas de fuego y humo segun el estado del nodo.
    /// </summary>
    /// <param name="stateInt">estado actual del nodo</param>
    void SetFireData(int stateInt)
    {
        switch (stateInt)
        {
            case 0:
                transform.GetChild(0).gameObject.SetActive(false);
                break;

            case 1:
                transform.GetChild(0).gameObject.SetActive(true);
                _fireData = new FireSPData(transform.GetChild(0).gameObject, 0.2f, -0.03f, new Vector2(0.55f, 0.95f),
           new Vector2(0.8f, 1.66f), new Vector2(0.2f, 0.8f),false, new Vector2(.5f, 1.0f));
                break;

            case 2:
                transform.GetChild(0).gameObject.SetActive(true);
                _fireData = new FireSPData(transform.GetChild(0).gameObject, 0.3f, -0.05f, new Vector2(0.8f, 1.05f),
           new Vector2(1.1f, 1.8f), new Vector2(0.2f, 0.8f),false, new Vector2(.6f, 1.2f));
                break;

            case 3:
                transform.GetChild(0).gameObject.SetActive(true);
                _fireData = new FireSPData(transform.GetChild(0).gameObject, 0.4f, -0.06f, new Vector2(1.05f, 1.3f),
           new Vector2(1.3f, 2.05f), new Vector2(0.4f, 1.2f),false, new Vector2(.9f, 1.8f));
                break;

            case 4:
                transform.GetChild(0).gameObject.SetActive(true);
                _fireData = new FireSPData(transform.GetChild(0).gameObject, 0.5f, -0.1f, new Vector2(1.3f, 1.55f),
           new Vector2(1.5f, 2.3f), new Vector2(0.6f, 1.4f),true, new Vector2(.1f,.2f));
                break;
        }
    }

    /// <summary>
    /// Carga todas las variables que son requeridas
    /// </summary>
    void Load()
    {
        //Fire_A | BurnDecal
        _burnDecal = transform.GetChild(0).transform.GetChild(4);
        if (_burnDecal == null)
            Debug.Log("burnDecal no encontrado: " + transform.GetChild(0).name);

        neighbor = GetComponent<Neighbor>();
        if (neighbor == null)
            Debug.LogError("neighbor no encontrado");

        _data = FindObjectOfType<PersistentData>();
        if (_data == null)
            Debug.LogError("Persistent data no encontrado");

        _collider = GetComponent<BoxCollider>();
        if (_collider == null)
            Debug.Log("_collider no encontrado");

        _manager = FindObjectOfType<FireManager>();
        if (_manager == null)
            Debug.LogError("_manager no encontrado");

        _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null)
            Debug.LogError("_renderer no encontrado");
    }

    /// <summary>
    /// Es la configuracion de precarga de cada nodo
    /// </summary>
    public void PreConfigNode()
    {
        Load();
        incrementTemp = I_IncrementTemperature();
        checkState100f = I_CheckTemp100f();
        SetFireData(0);
        if (id == 0)
        {
            Debug.LogError("El id del nodo no puede ser cero");
            return;
        }
        _timeIncTemp = _manager.timeIncTemp;
        _temperature = 0;
        ChangeState(Enumeradores.State.f0);
    }

    /// <summary>
    /// Método expuesto para llamar incrementador de temperatura del nodo.
    /// </summary>
    public void StartIncrementTemp()
    {
        StartCoroutine(I_IncrementTemperature());
    }

    void Update()
    {
        if (_isCheckState100f)
        {
            _isCheckState100f = false;
        }
    }

    /// <summary>
    /// si no está corriendo la corrutina de verificacion de temperatura, inicia esa corrutina.
    /// </summary>
    void RecheckingState100f()
    {
        StartCoroutine(checkState100f);
    }

    /// <summary>
    /// Cambia los estado del fuego segun el parámetro pasado.
    /// </summary>
    /// <param name="newState">estado al cual deseas cambiar</param>
    public void ChangeState(Enumeradores.State newState)
    {
        switch (newState)
        {
            case Enumeradores.State.f0:// estado inicial
                stateNode = newState;
                _renderer.material = materialStates[0];
                _collider.enabled = true;
                SetFireData(0);
                break;

            case Enumeradores.State.f0W://apagado, no quemado
                if (stateNode == Enumeradores.State.f0)
                {
                    return;
                }
                stateNode = newState;
                _renderer.material = materialStates[5];
                _collider.enabled = false;

                if (_burnDecal.parent != null)
                    _burnDecal.parent = null;

                SetFireData(0);
                StopCoroutine(I_IncrementTemperature());
                break;

            case Enumeradores.State.f25:

                if (isWater)
                {
                    Invoke("Dryoff", 10);
                    return;
                }
                    
                //estados permitidos f0 f0w y f50
                if (stateNode != Enumeradores.State.f0 && stateNode != Enumeradores.State.f0W && 
                    stateNode != Enumeradores.State.f50)
                {
                    return;
                }

                stateNode = newState;
                _renderer.material = materialStates[1];
                _collider.enabled = true;
                SetFireData(1);

                StartCoroutine(I_IncrementTemperature());

                break;

            case Enumeradores.State.f50:
                if (stateNode != Enumeradores.State.f25 && stateNode != Enumeradores.State.f75)
                {
                    return;
                }
                stateNode = newState;
                _renderer.material = materialStates[2];
                _collider.enabled = true;
                SetFireData(2);

                break;

            case Enumeradores.State.f75:
                if (stateNode != Enumeradores.State.f50 && stateNode != Enumeradores.State.f100)
                {
                    return;
                }
                stateNode = newState;
                _renderer.material = materialStates[3];
                _collider.enabled = true;
                SetFireData(3);

                _manager.OnNeighborNodes(id);
                break;

            case Enumeradores.State.f100:
                if (stateNode != Enumeradores.State.f75)
                    return;
                stateNode = newState;
                _renderer.material = materialStates[4];
                _collider.enabled = true;
                SetFireData(4);
                StartCoroutine(checkState100f);
                StopCoroutine(I_IncrementTemperature());
                break;
        }
    }
    /// <summary>
    /// Secar el nodo mojado
    /// </summary>
    void Dryoff()
    {
        isWater = false;
        _timeIncTemp += 0.1f;
        ChangeState(Enumeradores.State.f25);
    }

    /// <summary>
    /// Aumenta la temperatura del nodo de fuego
    /// </summary>
    /// <returns></returns>
    IEnumerator I_IncrementTemperature()
    {
        if (isWater || stateNode == Enumeradores.State.f100)
        {
            StopCoroutine(I_IncrementTemperature());
            yield return null;
        }
        else
        {
            while (_temperature < 100 && (stateNode != Enumeradores.State.f0 &&
                stateNode != Enumeradores.State.f0W && stateNode != Enumeradores.State.f100))
            {
                yield return new WaitForSeconds(_timeIncTemp);

                if(!isProof)
                    _temperature += fireSpeed;

                ChooseState();
            }
            StopCoroutine(I_IncrementTemperature());
            yield return null;
        }
    }

    /// <summary>
    /// elige el estado en base a la temperatura actual
    /// </summary>
    void ChooseState()
    {
        if (_temperature > 0 && _temperature < 26)
        {
            if(stateNode!= Enumeradores.State.f25)
                ChangeState(Enumeradores.State.f25);
        }
        else
        {
            if (_temperature > 25 && _temperature < 51)
            {
                if (stateNode != Enumeradores.State.f50)
                    ChangeState(Enumeradores.State.f50);
            }
            else
            {
                if (_temperature > 50 && _temperature < 76)
                {
                    if (stateNode != Enumeradores.State.f75)
                        ChangeState(Enumeradores.State.f75);
                }
                else
                {
                    if (_temperature > 75)
                    {
                        if (stateNode != Enumeradores.State.f100)
                            ChangeState(Enumeradores.State.f100);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Apaga el fuego
    /// </summary>
    /// <param name="type"></param>
    public void FireDecrement(Enumeradores.TypeExtintor type)
    {
        if (type == Enumeradores.TypeExtintor.ABC)
        {
            switch (typeNode)
            {
                case Enumeradores.TypeMaterial.solido:
                    if(_data.currentResistence== Enumeradores.resistence.poca)
                        _temperature -= 18;
                    if (_data.currentResistence == Enumeradores.resistence.intermedia)
                        _temperature -= 9;
                    if (_data.currentResistence == Enumeradores.resistence.mucha)
                        _temperature -= 5;
                    break;
                case Enumeradores.TypeMaterial.liquido:
                    if (_data.currentResistence == Enumeradores.resistence.poca)
                        _temperature -= 60;
                    if (_data.currentResistence == Enumeradores.resistence.intermedia)
                        _temperature -= 30;
                    if (_data.currentResistence == Enumeradores.resistence.mucha)
                        _temperature -= 15;

                    break;
                case Enumeradores.TypeMaterial.electrico:
                    if (_data.currentResistence == Enumeradores.resistence.poca)
                        _temperature -= 27;
                    if (_data.currentResistence == Enumeradores.resistence.intermedia)
                        _temperature -= 14;
                    if (_data.currentResistence == Enumeradores.resistence.mucha)
                        _temperature -= 7;
                    
                    break;
            }
        }

        if (type == Enumeradores.TypeExtintor.BC)
        {
            switch (typeNode)
            {
                case Enumeradores.TypeMaterial.solido:
                    _temperature -= 0;
                    break;
                case Enumeradores.TypeMaterial.liquido:
                    if (_data.currentResistence == Enumeradores.resistence.poca)
                        _temperature -= 54;
                    if (_data.currentResistence == Enumeradores.resistence.intermedia)
                        _temperature -= 27;
                    if (_data.currentResistence == Enumeradores.resistence.mucha)
                        _temperature -= 14;

                    break;
                case Enumeradores.TypeMaterial.electrico:
                    if (_data.currentResistence == Enumeradores.resistence.poca)
                        _temperature -= 60; 
                    if (_data.currentResistence == Enumeradores.resistence.intermedia)
                        _temperature -= 30; 
                    if (_data.currentResistence == Enumeradores.resistence.mucha)
                        _temperature -= 15;
                    break;
            }
        }

        if (type == Enumeradores.TypeExtintor.A)
        {
            switch (typeNode)
            {
                case Enumeradores.TypeMaterial.solido:
                    if (_data.currentResistence == Enumeradores.resistence.poca)
                        _temperature -= 60;
                    if (_data.currentResistence == Enumeradores.resistence.intermedia)
                        _temperature -= 30;
                    if (_data.currentResistence == Enumeradores.resistence.mucha)
                        _temperature -= 15;
                    break;
                case Enumeradores.TypeMaterial.liquido:
                    if (_data.currentResistence == Enumeradores.resistence.poca)
                        _temperature += 10;
                    if (_data.currentResistence == Enumeradores.resistence.intermedia)
                        _temperature += 20;
                    if (_data.currentResistence == Enumeradores.resistence.mucha)
                        _temperature += 30;
                    break;
                case Enumeradores.TypeMaterial.electrico:
                    _manager.ExplosionFire(transform.position);
                    _temperature += 100;
                    Invoke("Finish", 1);
                    break;
            }
        }

        if (_temperature<0)
            _temperature = 0;
        StopCoroutine(I_IncrementTemperature());
        if (_temperature <= 0)
        {
            ChangeState(Enumeradores.State.f0W);
            isWater = true;
        }
    }

    /// <summary>
    /// Llama a la finalizacion de la simulacion
    /// </summary>
    void Finish()
    {
        Simulation_Controller sim = _manager.GetComponent<Simulation_Controller>();
        FindObjectOfType<PersistentData>().SetResult(false, sim.bonus,sim.comments, sim.sceneType, sim.goodComments);
        sim.Finish();
    }

    /// <summary>
    /// Verifica si el nodo esta en 100 de temperatura para prender a un nodo vecino.
    /// </summary>
    /// <returns></returns>
    IEnumerator I_CheckTemp100f()
    {
        int count = 0;
        while (stateNode == Enumeradores.State.f100)
        {
            yield return new WaitForSeconds(1);
            count++;
            if (count>= 2)
            {
                if (_manager.OnNeighborByTemp(id))
                {
                    yield return new WaitForSeconds(_timeCheck);
                    RecheckingState100f();
                }
                else
                {
                    RecheckingState100f();
                }
                count = 0;
            }
        }
    }
}

/// <summary>
/// Clase que maneja el sistema de partículas del fuego y humo.
/// </summary>
public class FireSPData
{
    public GameObject target;
    public float SpSize, SpGravity;

    public Vector2 glowSize;

    public Vector2 smokeLifetime, smokeSize;

    public Vector2 sparksLifetime;

    public FireSPData(GameObject tg, float spSize, float spGravity, Vector2 glowsize, Vector2 smLife, Vector2 smSize, bool BigSmoke, Vector2 sparks_ar)
    {
        target = tg;
        SpSize = spSize;
        SpGravity = spGravity;
        glowSize = glowsize;
        smokeLifetime = smLife;
        smokeSize = smSize;
        sparksLifetime = sparks_ar;

        ParticleSystem sp = target.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = sp.main;
        main.startSize = SpSize;
        main.gravityModifier = SpGravity;

        sp = target.transform.GetChild(0).gameObject.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule glow = sp.main;
        glow.startSizeX = glowSize.x;
        glow.startSizeY = glowSize.y;

        if (!BigSmoke)
        {
            target.transform.GetChild(1).gameObject.SetActive(true);
            target.transform.GetChild(2).gameObject.SetActive(false);
            sp = target.transform.GetChild(1).gameObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule smoke = sp.main;
            smoke.startLifetime = new ParticleSystem.MinMaxCurve(smokeLifetime.x, smokeLifetime.y);
            smoke.startSizeX = smokeSize.x;
            smoke.startSizeY = smokeSize.y;
        }
        else
        {
            target.transform.GetChild(1).gameObject.SetActive(false);
            target.transform.GetChild(2).gameObject.SetActive(true);
            sp = target.transform.GetChild(2).gameObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule smoke2 = sp.main;
            smoke2.startLifetime = new ParticleSystem.MinMaxCurve(smokeLifetime.x*15, smokeLifetime.y*15);
            smoke2.startSizeX = smokeSize.x;
            smoke2.startSizeY = smokeSize.y;
        }

        sp = target.transform.GetChild(3).gameObject.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule sparks = sp.main;
        sparks.startLifetime = new ParticleSystem.MinMaxCurve(sparksLifetime.x, sparksLifetime.y);
    }
}

