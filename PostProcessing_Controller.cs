/*
Titulo: "SimuladorDeExtintores"
Hecho en el año:2019 
-----
Title: "SimuladorDeExtintores"
Made in the year: 2019
*/
using UnityEngine;
using UnityEngine.PostProcessing;

public class PostProcessing_Controller : MonoBehaviour
{
    public float altura=1;
    public AudioClip[] audios;

    #region variables privadas
    float _targetDepth, _targetVignette;
    FireManager _manager;
    bool _isLerpDepth, _isLerpVignette;
    float _countDepth, _countVignette;
    AudioSource _audio;
    DepthOfFieldModel.Settings _depth;
    VignetteModel.Settings _vignette;
    PostProcessingBehaviour _post;
    PostProcessingProfile _profile;
    #endregion

    void Load()
    {
        _manager = FindObjectOfType<FireManager>();
        if (_manager == null)
            Debug.LogError("manager no encontrado");

        _post = GetComponent<PostProcessingBehaviour>();
        if (_post == null)
            Debug.LogError("post no encontrado");
    }

    private void Awake()
    {
        Load();
        
        _profile = _post.profile;
        _depth = _profile.depthOfField.settings;
        _vignette = _profile.vignette.settings;

        _vignette.intensity = 0.15f;
        _depth.focusDistance = 0.45f;

        _profile.vignette.settings = _vignette;
        _profile.depthOfField.settings = _depth;
        _post.profile = _profile;
    }

    private void Start()
    {
        _audio = GetComponent<AudioSource>();
        InvokeRepeating("CheckStateNodes", 15, 15);
    }

    /// <summary>
    /// Cambia el estado de la camara segun la cantidad de humo que hay en la escena
    /// </summary>
    /// <param name="valueD">Efecto Depth</param>
    /// <param name="valueV">Efecto Vignette</param>
    public void ChangeSmokeState(float valueD, float valueV)
    {
        
        _profile = _post.profile;
        _depth = _profile.depthOfField.settings;
        _vignette = _profile.vignette.settings;
        _targetDepth = valueD;
        _targetVignette = valueV;

        _countDepth = 0;
        _countVignette = 0;

        _isLerpDepth = true;
        _isLerpVignette=true;
    }

    private void Update()
    {
        if (_isLerpDepth)
        {
            LerpingDepth();
        }
        if (_isLerpVignette)
        {
            LerpingVignette();
        }
    }

    /// <summary>
    /// Iterando efecto depth de la camara
    /// </summary>
    void LerpingDepth()
    {
        _depth.focusDistance = Mathf.Lerp(_depth.focusDistance, _targetDepth, _countDepth);
        _profile.depthOfField.settings = _depth;
        _post.profile = _profile;

        _countDepth += 0.001f * Time.deltaTime;
        if (_countDepth >= 1)
        {
            _countDepth = 0;
            _isLerpDepth = false;
        }
    }

    /// <summary>
    /// Iterando efecto vignette de la camara
    /// </summary>
    void LerpingVignette()
    {
        _vignette.intensity = Mathf.Lerp(_vignette.intensity, _targetVignette, _countVignette);
        _profile.vignette.settings = _vignette;
        _post.profile = _profile;

        _countVignette += 0.001f * Time.deltaTime;
        if (_countVignette >= 1)
        {
            _countVignette = 0;
            _isLerpVignette = false;
        }
    }

    /// <summary>
    /// Verifica la cantidad de humo en la escena y reacciona al respecto
    /// </summary>
    public void CheckStateNodes()
    {
        int count = 0;

        for (int i = 0; i < _manager.nodesInstanced.Count; i++)
        {
            if (_manager.nodesInstanced[i].stateNode == Enumeradores.State.f100)
                count++;

            if (count > 10)
            {
                if (_depth.focusDistance != 0.25f)
                {
                    i = _manager.nodesInstanced.Count;
                    ChangeSmokeState(0.25f, 0.7f);
                }
                
                if(_audio!=null && transform.localPosition.y> altura )
                {
                    _audio.clip = audios[Random.Range(0, audios.Length - 1)];
                    _audio.Play();
                }
                break;
            }
        }
        if (count <= 10 && _depth.focusDistance != 0.45f)
        {
            ChangeSmokeState(0.45f,0.15f);
        }
    }
}
